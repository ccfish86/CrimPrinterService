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
            config.MarginTop = 0;
            config.MarginLeft = 0;
            config.Landscape = false;
            config.Paper = "1";
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
            if (Setting.Printer == null) { 
                Setting.Printer = printDocument.PrinterSettings.PrinterName;
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
                });
                Setting.Started = true;
                Console.WriteLine("server start");
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

            this.mainGrid.DataContext = Setting;

            // 启动服务
            this.StartServer();
        }
    }

    public class Laputa : WebSocketBehavior
    {
        protected override void OnMessage(MessageEventArgs e)
        {
            WsCommand command = JsonConvert.DeserializeObject<WsCommand>(e.Data);

            if (command.Command == "print") {
                Logger.InfoLog("print " + command.Seq + "  data:" + command.Data);
                lock(this) {
                    if (command.Type == "image")
                    {
                        if (command.Data.StartsWith("["))
                        {
                            // 多个图片
                            List<KeyValuePair<string, string>> imageList = JsonConvert.DeserializeObject<List<KeyValuePair<string, string>>>(command.Data);
                            printImage(command.Seq, imageList);

                        }
                        else
                        {
                            printImage(command.Seq, command.Data);
                        }
                    }
                    else if (command.Type == "html")
                    {
                        if (command.Data.StartsWith("["))
                        {

                            // 多个html
                            List<KeyValuePair < string, string>> htmlList = JsonConvert.DeserializeObject<List<KeyValuePair<string, string>>>(command.Data);

                        }
                        else
                        {
                            printHtml(command.Seq, command.Data);
                        }
                    }
                    else
                    { 
                        string extension = Path.GetExtension(command.Data);
                        if (Regex.IsMatch(extension, @"(jpg|jpeg|png|jfif)", RegexOptions.IgnoreCase)) {
                            printImage(command.Seq, command.Data);
                        } else if (Regex.IsMatch(extension, @"(html|html)", RegexOptions.IgnoreCase))
                        {
                            printHtml(command.Seq, command.Data);
                        }
                    }
                }
            }
        }

        public CustomSetting PrintSetting
        {
            get;set;
        }
        protected void printImage(string seq, List<KeyValuePair<string, string>> imageList)
        {

            PrintController printController = new StandardPrintController();
            PrintDocument printDoc = new PrintDocument();
            printDoc.DefaultPageSettings.Landscape = PrintSetting.Landscape;
            printDoc.PrinterSettings.PrinterName = PrintSetting.Printer;
            printDoc.PrinterSettings.Copies = 1;
            if (!String.IsNullOrEmpty(seq)) { 
                printDoc.PrinterSettings.PrintFileName = seq;
            }
            // printDoc.OriginAtMargins = true;
            printDoc.PrintController = printController;
            printDoc.BeginPrint += (object sender, PrintEventArgs e) =>
            {
                Logger.InfoLog("print begin" + seq );
                WsResponse<string> br = new WsResponse<string>();
                br.Seq = seq;
                br.Data = "begin";
                Send(JsonConvert.SerializeObject(br));
            };

            printDoc.EndPrint += (object sender, PrintEventArgs e) =>
            {
                Logger.InfoLog("print end " + seq);
                WsResponse<string> er = new WsResponse<string>();
                er.Seq = seq;
                er.Data = "end";
                Send(JsonConvert.SerializeObject(er));
            };
            int page = 1;

            printDoc.PrintPage += (object sender, PrintPageEventArgs e) =>
            {
                // inch
                int x = toInchX100(PrintSetting.MarginLeft);
                int y = toInchX100(PrintSetting.MarginTop);
                // inch
                int xr = toInchX100(PrintSetting.MarginRight);
                int yb = toInchX100(PrintSetting.MarginBottom);
                
                int w = 300, h = 551;
                switch (PrintSetting.Paper)
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
                    WebResponse response = request.GetResponse();//获得响应
                    Image img = Image.FromStream(response.GetResponseStream());///实例化,得到img
                    //                Conole.wrimg.Width       img.Height
                    Console.WriteLine(img.Width + "x" + img.Height);
                    Console.WriteLine(e.PageSettings.HardMarginX + "x" + e.PageSettings.HardMarginY);

                    if (PrintSetting.AutoSize)
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
                    Logger.InfoLog("print printed" + image.Key + "  data:" + image.Value);

                    WsResponse<KeyValuePair<string,string>> rimage = new WsResponse<KeyValuePair<string, string>>();
                    rimage.Seq = seq;
                    rimage.Data = new KeyValuePair<string, string>(image.Key, "printed");
                    string msgImage = JsonConvert.SerializeObject(rimage);
                    Send(msgImage);
      
                //}

                if (page < imageList.Count)
                {
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
        protected void printImage(string seq, string image)
        { 
            List<KeyValuePair<string, string>> imageList = new List<KeyValuePair<string, string>>();
            imageList.Add(new KeyValuePair<string, string>(seq, image));
            printImage(seq, imageList);
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
        public string Seq { get; set; }
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

        public string Seq { get; set; }

        public string Type { get; set; }

        public string Data {
            get;
            set;
        }
    }
}
