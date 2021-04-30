using System;
using System.Collections.Generic;

using System.Windows;
using System.Drawing.Printing;
using WebSocketSharp.Server;
using WebSocketSharp;
using Newtonsoft.Json;
using System.Text.RegularExpressions;
using System.IO;
using System.Drawing;
using System.Net;
using System.Collections.ObjectModel;
using System.Collections;
using System.Threading;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Diagnostics;

namespace CrimPrintService
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {

        public MainWindow()
        {
            InitializeComponent();


        }

        private void AfterInit()
        {
            _setting = loadSetting();
        }

        private CustomSetting loadSetting()
        {
            Logger.InfoLog("load settings");
            if (File.Exists("setting.json")) {
                string str = File.ReadAllText("setting.json");
                return JsonConvert.DeserializeObject<CustomSetting>(str);
            }

            var config = new CustomSetting();
            config.Port = 9999;
            // config.MarginTop = 0;
            // config.MarginLeft = 0;
            // config.Landscape = false;
            // config.Paper = "1";
            config.Started = false;
            return config;
        }

        public int Port
        {
            get;
            set;
        }

        private void getPrinters()
        {
            PrintDocument printDocument = new PrintDocument();

            PrinterSettings.StringCollection printers = PrinterSettings.InstalledPrinters;
            List<string> result = new List<string>();
            foreach (string item in printers)
            {
                result.Add(item);
                Console.WriteLine(item);
            }
            if (Setting.Printers == null || Setting.Printers.Count == 0) {
                if (Setting.Printers == null)
                {
                    Setting.Printers = new ObservableCollection<PrinterSetting>();
                }
                // Setting.Printer = printDocument.PrinterSettings.PrinterName;
                // 配置默认打印机
                PrinterSetting p = new PrinterSetting();
                p.ID = "default";
                p.Printer = printDocument.PrinterSettings.PrinterName;
                p.MarginTop = 0;
                p.MarginLeft = 0;
                p.Landscape = false;
                p.Paper = "1";

                Setting.Printers.Add(p);
            }

            this.combBoxPrinter.ItemsSource = result;
        }

        private CustomSetting _setting;

        public  CustomSetting Setting {
            get {
                return _setting;
                }
            set
            {
                _setting = value;
            }
        }
        private void button_Click(object sender, RoutedEventArgs e)
        {
            String config = JsonConvert.SerializeObject(Setting);
            File.WriteAllText("setting.json", config);
        }

        private void buttonStart_Click(object sender, RoutedEventArgs e)
        {

            // 启动服务
            this.StartServer();
        }
        void StartServer()
        {

            // 启动服务
            try
            {
                Console.WriteLine("server start");
                wssv = new WebSocketServer("ws://0.0.0.0:" + Math.Min(Setting.Port, 65535));
                wssv.AddWebSocketService<Laputa>("/Laputa", (Laputa laputa) => {
                    laputa.PrintSetting = Setting;
                    laputa.Start();
                });
                Setting.Started = true;
                Console.WriteLine("server started");
                wssv.Start();
            }
            catch (InvalidOperationException ex)
            {
                Logger.ErrorLog("启动失败");
                Setting.Started = false;
                MessageBox.Show("启动失败！");
            }
        }
        WebSocketServer wssv;

        private void buttonStop_Click(object sender, RoutedEventArgs e)
        {
            if (wssv!=null) { 
                Setting.Started = false;
                wssv.Stop();
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            AfterInit();

            this.getPrinters();

            this.StackPanelMain.DataContext = Setting;

            // 启动服务
            this.StartServer();
        }

        private void listBoxPrinters_Selected(object sender, RoutedEventArgs e)
        {
            Console.WriteLine(e.Source);
        }

        private void buttonAdd_Click(object sender, RoutedEventArgs e)
        {
            // 添加打印机
            PrinterSetting p = new PrinterSetting();
            p.ID = "new";
            p.MarginTop = 0;
            p.MarginLeft = 0;
            p.Landscape = false;
            p.Paper = "1";
            Setting.Printers.Add(p);
            
            Setting.SelectedID = "new";
        }

        private void buttonDel_Click(object sender, RoutedEventArgs e)
        {
            foreach (PrinterSetting p in Setting.Printers)
            {
                if (p.ID == Setting.SelectedID)
                {
                    Setting.Printers.Remove(p);
                    break;
                }
            }
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {

            if (listBoxPrinters.SelectedIndex == -1)
            {
                return;
            }
            foreach (PrinterSetting p in Setting.Printers)
            {
                if (listBoxPrinters.SelectedItem == p)
                {
                    p.IsDefault = true;
                } else
                {
                    p.IsDefault = false;
                }
            }
        }

        private void menuItemDel_Checked(object sender, RoutedEventArgs e)
        {

            if (listBoxPrinters.SelectedIndex == -1)
            {
                return;
            }
            Setting.Printers.RemoveAt(listBoxPrinters.SelectedIndex);

            Console.WriteLine(listBoxPrinters.SelectedIndex);
        }
    }

    public class Laputa : WebSocketBehavior
    {
        protected override void OnMessage(MessageEventArgs e)
        {
            WsCommand command = JsonConvert.DeserializeObject<WsCommand>(e.Data);

            if (command.Command == "print")
            {
                #region print
                Logger.InfoLog("print " + command.Seq + "  data:" + command.Data);
                lock (this) {

                    if (command.Type == "image")
                    {
                        if (command.Data.StartsWith("["))
                        {
                            // 多个图片
                            List<KeyValuePair<string, string>> imageList = JsonConvert.DeserializeObject<List<KeyValuePair<string, string>>>(command.Data);
                            printImage(command.Seq, command.Printer, imageList);

                        }
                        else
                        {
                            printImage(command.Seq, command.Printer, command.Data);
                        }
                    }
                    else if (command.Type == "html")
                    {
                        if (command.Data.StartsWith("["))
                        {

                            // 多个html
                            List<KeyValuePair<string, string>> htmlList = JsonConvert.DeserializeObject<List<KeyValuePair<string, string>>>(command.Data);

                        }
                        else
                        {
                            printHtml(command.Seq, command.Data);
                        }
                    }
                    else
                    {
                        string extension = Path.GetExtension(command.Data);
                        if (Regex.IsMatch(extension, @"(jpg|jpeg|png|jfif)", RegexOptions.IgnoreCase))
                        {
                            // printImage(command.Seq, command.Printer, command.Data);

                            Queue<PrintQueueItem> PrintQueue;
                            string queueName = command.Printer;
                            if (PrintQueueMap.ContainsKey(queueName))
                            {
                                PrintQueue = PrintQueueMap[queueName];
                            } 
                            else
                            {
                                PrintQueue = new Queue<PrintQueueItem>();
                                PrintQueueMap.TryAdd(command.Printer, PrintQueue);
                            }

                            PrintQueueItem queueItem = new PrintQueueItem();
                            queueItem.Seq = command.Seq;
                            queueItem.Type = "image";
                            queueItem.Key = command.Seq;
                            queueItem.Value = command.Data;
                            PrintQueue.Enqueue(queueItem);

                            // 通知
                        }
                        else if (Regex.IsMatch(extension, @"(html|html)", RegexOptions.IgnoreCase))
                        {
                            printHtml(command.Seq, command.Data);
                        }
                    }
                }

                #endregion
            }
            else if (command.Command == "getPrinters")
            {
                #region getPriters 打印机列表
                PrinterSettings.StringCollection printers = PrinterSettings.InstalledPrinters;
                List<string> result = new List<string>();
                foreach (string item in printers)
                {
                    result.Add(item);
                }
                WsResponse<List<string>> chkr = new WsResponse<List<string>>();
                chkr.Seq = command.Seq;
                chkr.Data = result;
                Send(JsonConvert.SerializeObject(chkr));
                #endregion
            }
            else if (command.Command == "checkPrinter")
            {
                #region checkPrinter 查看打印机是否正常开启
                PrinterSettings.StringCollection printers = PrinterSettings.InstalledPrinters;
                bool hasThePrinter = false;
                foreach (string item in printers)
                {
                    if (command.Data == item)
                    {
                        hasThePrinter = true;
                        break;
                    }
                }
                WsResponse<string> er = new WsResponse<string>();
                er.Seq = command.Seq;
                if (hasThePrinter) { 
                    er.Data = "OK";
                }
                else
                {
                    er.Code = "NG";
                }
                Send(JsonConvert.SerializeObject(er));
                #endregion
            }
            else
            {
                #region error
                WsResponse<string> er = new WsResponse<string>();
                er.Seq = command.Seq;
                er.Code = "NG";
                er.Data = "命令" + command.Command + "不支持";
                Send(JsonConvert.SerializeObject(er));
                #endregion
            }
        }

        public void Start()//启动 
        {
            Thread thread = new Thread(threadStart);
            thread.IsBackground = true;
            thread.Start();
        }

        private void threadStart()
        {
            while (true)
            {

                foreach (string item in PrintQueueMap.Keys) {
                    Console.WriteLine("print queue~:" + item);
                    if (PrintQueueMap[item].Count > 0)
                    {
                        try
                        {
                            Logger.InfoLog("同步信息号量");
                            //同步信息号量  
                            Monitor.Enter(PrintQueueMap[item]);

                            // 这儿需要处理其他打印格式
                            PrintImageQueue(item, PrintQueueMap[item], (str) => {
                                Monitor.Exit(PrintQueueMap[item]);
                                Logger.InfoLog("结束同步:" + str);
                            });

                            //没有任务，休息3秒钟 
                            Thread.Sleep(1000);
                        }
                        catch (Exception ex)
                        {
                            Logger.InfoLog(ex.ToString());
                            Monitor.Exit(PrintQueueMap[item]);
                            //没有任务，休息3秒钟 
                            Thread.Sleep(3000);
                        }
                    }
                    else
                    {
                        //没有任务，休息3秒钟 
                        Thread.Sleep(3000);
                    }
                }

            }
        }

        private void PrintImageQueue(string printer, Queue<PrintQueueItem> printQueue, Action<string> action)
        {

            var sw = new Stopwatch();

            // 根据名称获取对应打印机
            PrinterSetting printerSetting = null;
            foreach (PrinterSetting ps in PrintSetting.Printers)
            {
                if (String.IsNullOrEmpty(printer) && ps.IsDefault)
                {
                    printerSetting = ps;
                    break;
                }
                if (ps.ID == printer || ps.Printer == printer)
                {
                    printerSetting = ps;
                    break;
                }
            }

            if (printerSetting == null)
            {
                // 未找到指定打印机
                sw.Stop();
                action("1");
                return;
            }

            // 设置打印时不再弹打印进度弹窗
            PrintController printController = new StandardPrintController();
            PrintDocument printDoc = new PrintDocument();
            printDoc.DefaultPageSettings.Landscape = printerSetting.Landscape;
            printDoc.PrinterSettings.PrinterName = printerSetting.Printer;
            printDoc.PrinterSettings.Copies = 1;
            //if (!String.IsNullOrEmpty(seq)) { 
            //    printDoc.PrinterSettings.PrintFileName = seq;
            //}
            // printDoc.OriginAtMargins = true;
            printDoc.PrintController = printController;
            // 开始打印事件
            printDoc.BeginPrint += (object sender, PrintEventArgs e) =>
            {
                Logger.InfoLog("print begin" );
                //WsResponse<string> br = new WsResponse<string>();
                //br.Data = "begin";
                //Send(JsonConvert.SerializeObject(br));
            };

            // 结束打印事件
            printDoc.EndPrint += (object sender, PrintEventArgs e) =>
            {
                Logger.InfoLog("print end ");
                //WsResponse<string> er = new WsResponse<string>();
                //er.Data = "end";
                //Send(JsonConvert.SerializeObject(er));
            };

            int page = 1;
            PrintQueueItem command = printQueue.Dequeue();
            // 打印，每一页执行一次该事件
            printDoc.PrintPage += (object sender, PrintPageEventArgs e) =>
            {
                // inch
                int x = toInchX100(printerSetting.MarginLeft);
                int y = toInchX100(printerSetting.MarginTop);
                // inch
                int xr = toInchX100(printerSetting.MarginRight);
                int yb = toInchX100(printerSetting.MarginBottom);

                int w = 300, h = 551;
                switch (printerSetting.Paper)
                {
                    case "1":
                        {
                            w = 300; // 285; // 300* 0.95
                            h = 511; // 485; // 511 * 0.95
                            break;
                        }

                    case "2":
                        {
                            w = toInchX100(100);
                            h = toInchX100(180);
                            break;
                        }
                    case "3":
                    default:
                        {
                            w = toInchX100(100);
                            h = toInchX100(180);
                        }
                        break;
                }
                e.PageSettings.Margins.Left = 0;
                e.PageSettings.Margins.Top = 0;
                e.PageSettings.Margins.Bottom = 0;
                e.PageSettings.Margins.Right = 0;
                e.PageSettings.PaperSize = new PaperSize("自定义", w, h);

                //foreach (KeyValuePair<string, string> image in imageList)
                //{

                // KeyValuePair<string, string> image = imageList[page - 1];
                // Image i = Image.FromFile(image);
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(command.Value);
                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse()) //获得响应) 
                {
                    if (response.StatusCode != HttpStatusCode.OK)
                    {

                        WsResponse<KeyValuePair<string, string>> rerr = new WsResponse<KeyValuePair<string, string>>();
                        rerr.Seq = command.Seq;
                        rerr.Code = "NET_ERR";
                        rerr.Data = new KeyValuePair<string, string>(command.Key, "error:" + response.StatusCode);
                        string msgerr = JsonConvert.SerializeObject(rerr);
                        Send(msgerr);
                    }
                    else
                    {
                        using (Stream stream = response.GetResponseStream())
                        {
                            Image img = Image.FromStream(stream);///实例化,得到img
                            //                Conole.wrimg.Width       img.Height
                            //Console.WriteLine(img.Width + "x" + img.Height);
                            //Console.WriteLine(e.PageSettings.HardMarginX + "x" + e.PageSettings.HardMarginY);

                            if (printerSetting.AutoSize)
                            {
                                // e.MarginBounds
                                e.MarginBounds.Offset(0, 0);
                                //Console.WriteLine(e.MarginBounds);
                                //Rectangle rect = new Rectangle(x, y, w - x - xr, h - y - yb);
                                //e.Graphics.DrawImage(img, rect);
                                e.Graphics.DrawImage(img, e.MarginBounds);
                            }
                            else
                            {
                                e.MarginBounds.Offset(0, 0);
                                e.Graphics.DrawImage(img, e.PageSettings.HardMarginX + x, e.PageSettings.HardMarginY + y, w, h);
                            }
                        }
                        Logger.InfoLog("print printed" + command.Key + "  data:" + command.Value);

                        WsResponse<KeyValuePair<string, string>> rimage = new WsResponse<KeyValuePair<string, string>>();
                        rimage.Seq = command.Seq;
                        rimage.Data = new KeyValuePair<string, string>(command.Key, "printed");
                        string msgImage = JsonConvert.SerializeObject(rimage);
                        Send(msgImage);
                    }
                }
                //}

                if (page < 100 && printQueue.Count > 0)
                {
                    // 每次最大连续打印100页，
                    // 表示还有下一页，
                    command = printQueue.Dequeue();
                    e.HasMorePages = true;
                }
                else
                {
                    e.HasMorePages = false;
                    sw.Stop();
                    action("2");
                }
                page++;
            };

            printDoc.Print();

            //WsResponse<string> r = new WsResponse<string>();
            //r.Seq = seq;
            //r.Data = "printed";
            //string msg = JsonConvert.SerializeObject(r);
            //Send(msg);
        }

        ConcurrentDictionary<String, Queue<PrintQueueItem>> PrintQueueMap = new ConcurrentDictionary<String, Queue<PrintQueueItem>>(); //实例化一个队列

        public CustomSetting PrintSetting
        {
            get;set;
        }



        /// <summary>
        /// 打印
        /// </summary>
        /// <param name="seq">序号，用于区分回调</param>
        /// <param name="printerName">打印机，如果不指定，则使用画面上的打印机</param>
        /// <param name="imageList">待打印图片列表</param>
        protected void printImage(string seq, string printer, List<KeyValuePair<string, string>> imageList)
        {
            // 根据名称获取对应打印机
            PrinterSetting printerSetting = null;
            foreach (PrinterSetting ps in PrintSetting.Printers)
            {
                if (String.IsNullOrEmpty(printer) && ps.IsDefault)
                {
                    printerSetting = ps;
                    break;
                }
                if (ps.ID == printer || ps.Printer == printer)
                {
                    printerSetting = ps;
                    break;
                }
            }

            if (printerSetting == null)
            {
                // 未找到指定打印机
                Logger.ErrorLog("未找到指定的打印机，请先确认配置");
                return;
            }

            // 设置打印时不再弹打印进度弹窗
            PrintController printController = new StandardPrintController();
            PrintDocument printDoc = new PrintDocument();
            printDoc.DefaultPageSettings.Landscape = printerSetting.Landscape;
            printDoc.PrinterSettings.PrinterName = printerSetting.Printer;
            printDoc.PrinterSettings.Copies = 1;
            //if (!String.IsNullOrEmpty(seq)) { 
            //    printDoc.PrinterSettings.PrintFileName = seq;
            //}
            // printDoc.OriginAtMargins = true;
            printDoc.PrintController = printController;
            // 开始打印事件
            printDoc.BeginPrint += (object sender, PrintEventArgs e) =>
            {
                Logger.InfoLog("print begin" + seq );
                WsResponse<string> br = new WsResponse<string>();
                br.Seq = seq;
                br.Data = "begin";
                Send(JsonConvert.SerializeObject(br));
            };

            // 结束打印事件
            printDoc.EndPrint += (object sender, PrintEventArgs e) =>
            {
                Logger.InfoLog("print end " + seq);
                WsResponse<string> er = new WsResponse<string>();
                er.Seq = seq;
                er.Data = "end";
                Send(JsonConvert.SerializeObject(er));
            };

            int page = 1;
            // 打印，每一页执行一次该事件
            printDoc.PrintPage += (object sender, PrintPageEventArgs e) =>
            {
                // inch
                int x = toInchX100(printerSetting.MarginLeft);
                int y = toInchX100(printerSetting.MarginTop);
                // inch
                int xr = toInchX100(printerSetting.MarginRight);
                int yb = toInchX100(printerSetting.MarginBottom);
                
                int w = 300, h = 551;
                switch (printerSetting.Paper)
                {
                    case "1":
                        {
                            w = 300; // 285; // 300* 0.95
                            h = 511; // 485; // 511 * 0.95
                            break;
                        }

                    case "2":
                        {
                            w = toInchX100(100);
                            h = toInchX100(180);
                            break;
                        }
                    case "3":
                    default:
                        {
                            w = toInchX100(100);
                            h = toInchX100(180);
                        }
                        break;
                }
                e.PageSettings.Margins.Left = 0;
                e.PageSettings.Margins.Top = 0;
                e.PageSettings.Margins.Bottom = 0;
                e.PageSettings.Margins.Right = 0;
                e.PageSettings.PaperSize = new PaperSize("自定义", w, h);

                //foreach (KeyValuePair<string, string> image in imageList)
                //{

                KeyValuePair<string, string> image = imageList[page - 1];
                // Image i = Image.FromFile(image);
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(image.Value);
                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse()) //获得响应) 
                {
                    if (response.StatusCode != HttpStatusCode.OK)
                    {

                        WsResponse<KeyValuePair<string, string>> rerr = new WsResponse<KeyValuePair<string, string>>();
                        rerr.Seq = seq;
                        rerr.Code = "NET_ERR";
                        rerr.Data = new KeyValuePair<string, string>(image.Key, "error:" + response.StatusCode);
                        string msgerr = JsonConvert.SerializeObject(rerr);
                        Send(msgerr);
                    } else {
                        using (Stream stream = response.GetResponseStream())
                        { 
                            Image img = Image.FromStream(stream);///实例化,得到img
                            //                Conole.wrimg.Width       img.Height
                            //Console.WriteLine(img.Width + "x" + img.Height);
                            //Console.WriteLine(e.PageSettings.HardMarginX + "x" + e.PageSettings.HardMarginY);

                            if (printerSetting.AutoSize)
                            {
                                // e.MarginBounds
                                e.MarginBounds.Offset(0, 0);
                                //Console.WriteLine(e.MarginBounds);
                                //Rectangle rect = new Rectangle(x, y, w - x - xr, h - y - yb);
                                //e.Graphics.DrawImage(img, rect);
                                e.Graphics.DrawImage(img, e.MarginBounds);
                            }
                            else
                            {
                                e.MarginBounds.Offset(0, 0);
                                e.Graphics.DrawImage(img, e.PageSettings.HardMarginX + x, e.PageSettings.HardMarginY + y, w, h);
                            }
                        }
                        Logger.InfoLog("print printed" + image.Key + "  data:" + image.Value);

                        WsResponse<KeyValuePair<string, string>> rimage = new WsResponse<KeyValuePair<string, string>>();
                        rimage.Seq = seq;
                        rimage.Data = new KeyValuePair<string, string>(image.Key, "printed");
                        string msgImage = JsonConvert.SerializeObject(rimage);
                        Send(msgImage);
                    }
                }
                //}

                if (page < imageList.Count)
                {
                    // 表示还有下一页，
                    e.HasMorePages = true;
                }
                else
                {
                    e.HasMorePages = false;
                }
                page++;
            };

            printDoc.Print();
            
            WsResponse<string> r = new WsResponse<string>();
            r.Seq = seq;
            r.Data = "printed";
            string msg = JsonConvert.SerializeObject(r);
            Send(msg);
        }
        
        protected void printImage(string seq, string printer, string image)
        { 
            List<KeyValuePair<string, string>> imageList = new List<KeyValuePair<string, string>>();
            imageList.Add(new KeyValuePair<string, string>(seq, image));
            printImage(seq, printer, imageList);
        }

        int toInchX100(int mm)
        {
            return (int)(mm * 3.937);
        }

        protected void printHtml(string seq, string html)
        {

            WsResponse<string> r = new WsResponse<string>();
            r.Seq = seq;
            r.Data = "暂不支持HTML";
            string msg = JsonConvert.SerializeObject(r);
            Send(msg);
        }

        protected override void OnOpen()
        {
            Console.WriteLine("opened");
            WsResponse<string> r = new WsResponse<string>();
            r.Data = "连接成功";
            string msg = JsonConvert.SerializeObject(r);
            Send(msg);
        }
    }

    public class WsResponse<T>
    {
        /// <summary>
        /// 命令SEQ
        /// </summary>
        public string Seq { get; set; }
        /// <summary>
        /// 错误编码
        /// </summary>
        public string Code { get; set; }
        /// <summary>
        /// 业务数据
        /// </summary>
        public T Data
        {
            get;
            set;
        }

    }

    public class WsCommand
    {
        public string Command
        {
            get;
            set;
        }

        public string Printer
        {
            get;set;
        }

        public string Seq { get; set; }

        public string Type { get; set; }

        public string Data {
            get;
            set;
        }
    }

    class PrintQueueItem{

        public string Seq { get; set; }

        public string Type { get; set; }

        public string Key { get; set; }

        public string Value { get; set; }

    }
}

