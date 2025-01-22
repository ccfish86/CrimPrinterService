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
using System.Diagnostics;
using System.Security.Authentication;
using TaishanPrintService.models;
using TaishanPrintService.utils;
using Spire.Pdf;
using Spire.Pdf.Print;
using CrimPrintService.utils;
using System.Reflection;
using System.Threading.Tasks;

namespace CrimPrintService
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {

        private System.Windows.Forms.NotifyIcon notifyIcon;

        public MainWindow()
        {
            InitializeComponent();
            InitializeSystemTrayIcon();
        }

        private void InitializeSystemTrayIcon()
        {
            this.notifyIcon = new System.Windows.Forms.NotifyIcon();

            // 设置图标
            this.notifyIcon.Icon = new System.Drawing.Icon("./printer.ico");

            // 设置提示文本
            this.notifyIcon.Text = "云打印";
            this.notifyIcon.Visible = true;

            // 设置点击事件
            this.notifyIcon.MouseClick += NotifyIcon_MouseClick;
        }

        private void NotifyIcon_MouseClick(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (e.Button == System.Windows.Forms.MouseButtons.Left)
            {
                // 当点击左键时显示或隐藏窗口
                if (this.Visibility == Visibility.Visible)
                {
                    this.Hide();
                }
                else
                {
                    this.Show();
                    this.Activate();
                }
            }
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            base.OnClosing(e);

            // 关闭窗口时同时退出应用程序
            this.notifyIcon.Dispose();
        }
        private void AfterInit()
        {
            _setting = loadSetting();
            _setting.State = 0;

            //处理HttpWebRequest访问https有安全证书的问题（ 请求被中止: 未能创建 SSL/TLS 安全通道。）
            ServicePointManager.ServerCertificateValidationCallback += (s, cert, chain, sslPolicyErrors) => true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;

            // 将版本号转换为字符串格式，并添加"v"前缀
            Version version = Assembly.GetExecutingAssembly().GetName().Version;
            string versionString = $"v{version.Major}.{version.Minor}.{version.Build}.{version.Revision}";
            this.Title = $"云打印服务：： {versionString}";
        }

        private CustomSetting loadSetting()
        {
            CrimLogger.InfoLog("load settings");
            if (File.Exists("setting.json")) {
                string str = File.ReadAllText("setting.json");
                return JsonConvert.DeserializeObject<CustomSetting>(str);
            }

            var config = new CustomSetting();
            config.Url = "wss://climber.dongyuejihua.com/ws/p/stream";
            config.Key = "";
            // config.MarginTop = 0;
            // config.MarginLeft = 0;
            // config.Landscape = false;
            // config.Paper = "1";
            config.Started = false;
            return config;
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
            if (_setting.Printers == null || _setting.Printers.Count == 0) {
                if (_setting.Printers == null)
                {
                    _setting.Printers = new ObservableCollection<PrinterSetting>();
                }
                // _setting.Printer = printDocument.PrinterSettings.PrinterName;
                // 配置默认打印机
                PrinterSetting p = new PrinterSetting();
                p.ID = "default";
                p.Printer = printDocument.PrinterSettings.PrinterName;
                p.MarginTop = 0;
                p.MarginLeft = 0;
                p.Landscape = false;
                p.Paper = "1";

                _setting.Printers.Add(p);
            }

            this.combBoxPrinter.ItemsSource = result;
        }

        private CustomSetting _setting;


        private void button_Click(object sender, RoutedEventArgs e)
        {
            saveSettings();
        }

        private void saveSettings()
        {
            String config = JsonConvert.SerializeObject(_setting);
            File.WriteAllText("setting.json", config);
        }

        private void buttonStart_Click(object sender, RoutedEventArgs e)
        {
            // 连接服务
            this.StartServer();
            this.connectServer();
        }

       void connectServer()
        {
            // 连接服务器
            _setting.Started = false;
            if (_webSocket != null&&_webSocket.ReadyState == WebSocketState.Open)
            {
                _setting.State = 2;
                _setting.Started = true;
                return;
            }
            if (!_setting.Url.IsNullOrEmpty() && !_setting.Key.IsNullOrEmpty())
            {
                _setting.State = 1;
                var url = string.Format("{0}?key={1}", this._setting.Url, this._setting.Key);
                _webSocket = new WebSocket(url);
                _webSocket.SslConfiguration.EnabledSslProtocols = SslProtocols.Default | SslProtocols.Tls11 | SslProtocols.Tls12;

                _webSocket.WaitTime = TimeSpan.FromSeconds(30);
                _webSocket.EmitOnPing = true;
                _webSocket.OnMessage += _webSocket_OnMessage;
                _webSocket.OnOpen += _webSocket_OnOpen;
                _webSocket.OnError += _webSocket_OnError; ;
                _webSocket.OnClose += this.OnWsClosed;
                try { 
                    _webSocket.Connect();
                }
                catch (Exception ex)
                {
                    CrimLogger.ErrorLog("连接异常", ex);
                    _setting.State = 3;

                    // 1分钟后重连
                    var t = Task.Delay(60000).ContinueWith((task) =>
                    {
                        // 后台重连
                        Thread thread = new Thread(reconnect);
                        thread.IsBackground = true;
                        thread.Start();
                    });
                    t.Start();
                }
                _setting.Started = true;

            }
        }

        private void _webSocket_OnOpen(object sender, EventArgs e)
        {
            // _webSocket.Ping();
            CrimLogger.InfoLog("连接成功");
            _setting.State = 2;
            wsopen = true;
            if (pingTimer == null)
            {
                pingTimer = new Timer(TimerTaskPingCallback, null, 20000, 40000);
            }
        }

        private void TimerTaskPingCallback(Object state)
        {
            if (_webSocket != null && _webSocket.ReadyState == WebSocketState.Open)
            {
                _webSocket.Ping();
            }
        }
        private void _webSocket_OnError(object sender, WebSocketSharp.ErrorEventArgs e)
        {
            CrimLogger.ErrorLog("Websocket error.", e.Exception);
        }

        private void _webSocket_OnMessage(object sender, MessageEventArgs e)
        {
            CrimLogger.InfoLog("收到消息" + e.Data);
            // 转json
            PrintServerMsg printMsg = JsonConvert.DeserializeObject< PrintServerMsg>(e.Data);
            if (printMsg.Type == 5)
            {
                // 打印PDF
                if (!printMsg.Data.IsNullOrEmpty())
                {
                    var pdfMsg = JsonConvert.DeserializeObject<PrintServerPdfMsg>(printMsg.Data);
                    this.doPrintPdf(pdfMsg);
                }
            }
            else if (printMsg.Type == 6)
            {
                // 打印快递单
                if (!printMsg.Data.IsNullOrEmpty())
                {
                    var imgMsg = JsonConvert.DeserializeObject<PrintServerImgMsg>(printMsg.Data);
                    this.doPrintImg(imgMsg);
                }
            }
            else if (printMsg.Type == 99)
            {
                // 异常
                wsopen = false;
                this.WsClose();
                MessageBox.Show(printMsg.Data);
            }
            else
            {
                CrimLogger.InfoLog("未知消息类型 请升级版本再试" + printMsg.Type);
            }
        }

        [STAThread]
        private void doPrintImg(PrintServerImgMsg imgMsg)
        {
            PrinterPool printerPool = PrinterPool.GetPrinterPool();
            var printerSetting = getPrinterSetting(imgMsg.PrinterID);
            if (printerSetting == null)
            {
                PrintServerAck er = new PrintServerAck();
                var printResult = new PrintServerPdfPrintResult();
                printResult.ID = imgMsg.ID;
                printResult.PrintState = 6;
                er.Seq = imgMsg.ID;
                er.Code = "404";
                er.Message = "无法找到对应的打印机" + imgMsg.PrinterID;
                er.Type = 106;
                er.Data = JsonConvert.SerializeObject(printResult);
                _webSocket.Send(JsonConvert.SerializeObject(er));
                return;
            }
            printerPool.printImage(printerSetting, imgMsg.ID, imgMsg.Files, _webSocket.Send);
        }

        private PrinterSetting getPrinterSetting(string printer)
        {
            // 根据名称获取对应打印机
            PrinterSetting printerSetting = null;
            foreach (PrinterSetting ps in _setting.Printers)
            {
                if (ps.ID == printer)
                {
                    printerSetting = ps;
                    break;
                }
            }

            if (printerSetting == null)
            {
                CrimLogger.ErrorLog("未找到指定的打印机" + printer);
                MessageBox.Show("未找到指定的打印机\n 请追加ID=" + printer + "的打印机");
                return null;
            }
            return printerSetting;
        }

        private void doPrintExpress(PrintServerPdfMsg expressMsg)
        {
            //var sw = new Stopwatch();
            CrimLogger.InfoLog("print " + expressMsg.ID + " "+ expressMsg.PrinterID);
            var printerSetting = getPrinterSetting(expressMsg.PrinterID);
            if (printerSetting == null)
            {
                PrintServerAck er = new PrintServerAck();
                var printResult = new PrintServerPdfPrintResult();
                printResult.ID = expressMsg.ID;
                printResult.PrintState = 6;
                er.Seq = expressMsg.ID;
                er.Code = "404";
                er.Message = "无法找到对应的打印机" + expressMsg.PrinterID;
                er.Type = 105;
                er.Data = JsonConvert.SerializeObject(printResult);
                _webSocket.Send(JsonConvert.SerializeObject(er));
                return;
            }
            foreach (var htmlUrl in expressMsg.Files)
            {
                using (PdfDocument pdfDoc = new PdfDocument())
                {
                    pdfDoc.LoadFromHTML(htmlUrl, false, false, true);
                    // 打印参数
                    PdfPrintSettings printSettings = new PdfPrintSettings();
                    if (printerSetting.Paper == "A4")
                    {
                        printSettings.PaperSize = PdfPaperSizes.A4;
                    }
                    else if (printerSetting.Paper == "A5")
                    {
                        printSettings.PaperSize = PdfPaperSizes.A5;
                    }
                    else if (printerSetting.Paper == "A6")
                    {
                        printSettings.PaperSize = PdfPaperSizes.A6;
                    }
                    else
                    {
                        var w = PageUtils.ToInchX100(76);
                        var h = PageUtils.ToInchX100(130);
                        printSettings.PaperSize = new PaperSize("E76", w, h);
                    }
                    printSettings.Landscape = printerSetting.Landscape;
                    printSettings.PrinterName = printerSetting.Printer;
                    printSettings.BeginPrint += (object sender, PrintEventArgs e) =>
                    {
                        // 打印开始事件
                        CrimLogger.InfoLog("print begin [" + expressMsg.ID + "]: " + htmlUrl);
                        var printResult = new PrintServerPdfPrintResult
                        {
                            ID = expressMsg.ID,
                            PrintState = 2
                        };
                        PrintServerAck br = new PrintServerAck
                        {
                            Seq = expressMsg.ID,
                            Type = 106,
                            Data = JsonConvert.SerializeObject(printResult)
                        };
                        _webSocket.Send(JsonConvert.SerializeObject(br));
                    };
                    printSettings.EndPrint += (object sender, PrintEventArgs e) =>
                    {
                        // 打印结束事件
                        CrimLogger.InfoLog("print end [" + expressMsg.ID + "]: " + htmlUrl);
                        var printResult = new PrintServerPdfPrintResult
                        {
                            ID = expressMsg.ID,
                            PrintState = 3
                        };
                        PrintServerAck er = new PrintServerAck
                        {
                            Seq = expressMsg.ID,
                            Type = 106,
                            Data = JsonConvert.SerializeObject(printResult)
                        };
                        _webSocket.Send(JsonConvert.SerializeObject(er));
                    };

                    pdfDoc.Print(printSettings);
                }
            }
        }
        /// <summary>
        /// PDF打印
        /// </summary>
        /// <param name="pdfMsg"></param>
        private void doPrintPdf(PrintServerPdfMsg pdfMsg) 
        {
            //var sw = new Stopwatch();
            var printerSetting = getPrinterSetting(pdfMsg.PrinterID);
            if (printerSetting == null)
            {
                PrintServerAck er = new PrintServerAck();
                var printResult = new PrintServerPdfPrintResult();
                printResult.ID = pdfMsg.ID;
                printResult.PrintState = 6;
                er.Seq = pdfMsg.ID;
                er.Code = "404";
                er.Message = "无法找到对应的打印机" + pdfMsg.PrinterID;
                er.Type = 105;
                er.Data = JsonConvert.SerializeObject(printResult);
                _webSocket.Send(JsonConvert.SerializeObject(er));
                return;
            }
            foreach (var pdfUrl in pdfMsg.Files) {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(pdfUrl);
                // 设置超时时间为5000毫秒
                // request.Timeout = 20000;
                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse()) //获得响应) 
                {
                    if (response.StatusCode != HttpStatusCode.OK)
                    {
                        // 打印打印失败
                        CrimLogger.ErrorLog("print error [" + pdfMsg.ID + "]: " + pdfUrl);
                        CrimLogger.ErrorLog("print error [" + response.StatusCode + "]: " + response.StatusDescription);
                        var printResult = new PrintServerPdfPrintResult
                        {
                            ID = pdfMsg.ID,
                            PrintState = 6
                        };
                        PrintServerAck er = new PrintServerAck
                        {
                            Seq = pdfMsg.ID,
                            Code = response.StatusCode.ToString(),
                            Message = response.StatusDescription,
                            Type = 105,
                            Data = JsonConvert.SerializeObject(printResult)
                        };
                        _webSocket.Send(JsonConvert.SerializeObject(er));
                    }
                    else
                    {
                        using (var stream = response.GetResponseStream())
                        {
                            using (PdfDocument pdfDoc = new PdfDocument()) { 
                                using (MemoryStream ms = new MemoryStream())
                                {
                                    stream.CopyTo(ms);
                                    pdfDoc.LoadFromStream(ms);
                                }
                                // 打印参数
                                PdfPrintSettings printSettings = new PdfPrintSettings();
                                if (printerSetting.Paper == "A4") { 
                                    printSettings.PaperSize = PdfPaperSizes.A4;
                                    printSettings.Landscape = printerSetting.Landscape;
                                }
                                else if (printerSetting.Paper == "A5")
                                {
                                    printSettings.PaperSize = PdfPaperSizes.A5;
                                    printSettings.Landscape = printerSetting.Landscape;
                                }
                                else if (printerSetting.Paper == "A6")
                                {
                                    printSettings.PaperSize = PdfPaperSizes.A6;
                                    printSettings.Landscape = printerSetting.Landscape;
                                }
                                else 
                                {
                                    var esize = PageUtils.PageToInchX100(printerSetting.Paper);
                                    printSettings.PaperSize = new PaperSize("USER", esize.Width, esize.Height);
                                }
                                printSettings.PrinterName = printerSetting.Printer;
                                printSettings.BeginPrint  += (object sender, PrintEventArgs e) =>
                                {
                                    // 打印开始事件
                                    CrimLogger.InfoLog("print begin [" + pdfMsg.ID + "]: " + pdfUrl);
                                    var printResult = new PrintServerPdfPrintResult
                                    {
                                        ID = pdfMsg.ID,
                                        PrintState = 2
                                    };
                                    PrintServerAck br = new PrintServerAck
                                    {
                                        Seq = pdfMsg.ID,
                                        Type = 105,
                                        Data = JsonConvert.SerializeObject(printResult)
                                    };
                                    _webSocket.Send(JsonConvert.SerializeObject(br));
                                };
                                printSettings.EndPrint += (object sender, PrintEventArgs e) =>
                                {
                                    // 打印结束事件
                                    CrimLogger.InfoLog("print end [" + pdfMsg.ID + "]: " + pdfUrl);
                                    var printResult = new PrintServerPdfPrintResult
                                    {
                                        ID = pdfMsg.ID,
                                        PrintState = 3
                                    };
                                    PrintServerAck er = new PrintServerAck();
                                    er.Seq = pdfMsg.ID;
                                    er.Type = 105;
                                    er.Data = JsonConvert.SerializeObject(printResult);
                                    _webSocket.Send(JsonConvert.SerializeObject(er));
                                };

                                pdfDoc.Print(printSettings);
                            }
                        }
                    }
                }
            }
        }

        private void OnWsClosed(object sender, CloseEventArgs e)
        {
            if (pingTimer != null) { 
                pingTimer.Dispose();
                pingTimer = null;
            }

            if (e.Code == 1006) { 
                _setting.State = 3;
            }
            else
            {
                _setting.State = 0;
            }
            CrimLogger.InfoLog("已断开:" + e.Code);

            // 后台重连
            Thread thread = new Thread(reconnect);
            thread.IsBackground = true;
            thread.Start();
        }

        private void reconnect()
        {
            var reconnTtl = 5000;
            while(wsopen && _webSocket.ReadyState != WebSocketState.Open) { 
                try
                {
                    _setting.State = 1;
                    CrimLogger.InfoLog("重连");
                    Thread.Sleep(reconnTtl);
                    _webSocket.Connect();
                    reconnTtl = 5000;
                    break;
                }
                catch (Exception ex)
                {
                    reconnTtl = reconnTtl * 2;
                    CrimLogger.InfoLog("重连失败:" + ex.Message);
                    CrimLogger.ErrorLog("重连失败", ex);
                }
            }
        }

        private WebSocket _webSocket;
        private Timer pingTimer;
        private bool wsopen;

        /// <summary>
        /// 启动打印服务
        /// </summary>
        void StartServer()
        {
            // 启动服务
            try
            {
                if (!_setting.StartLocal)
                {
                    return;
                }
                CrimLogger.InfoLog("server start");
                wssv = new WebSocketServer("ws://0.0.0.0:" + Math.Min(_setting.Port, 65535));
                wssv.AddWebSocketService<Laputa>("/Laputa", (Laputa laputa) =>
                {
                    laputa.PrintSetting = _setting;
                    laputa.Start();
                });
                _setting.Started = true;
                CrimLogger.InfoLog("server started");
                wssv.Start();
            }
            catch (InvalidOperationException ex)
            {
                CrimLogger.ErrorLog("启动失败");
                _setting.Started = false;
                MessageBox.Show("启动失败！");
            }
        }
        WebSocketServer wssv;

        private void buttonStop_Click(object sender, RoutedEventArgs e)
        {
            wsopen = false;
            this.WsClose();

            if (wssv != null)
            {
                _setting.Started = false;
                wssv.Stop();
            }
        }
        public void WsClose()
        {
            this._webSocket.OnClose -= this.OnWsClosed;
            this._webSocket.Close(CloseStatusCode.Normal, "close");
            _setting.State = 0;
            _setting.Started = false;
        }


        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            AfterInit();

            this.getPrinters();

            this.StackPanelMain.DataContext = _setting;

            // 启动服务
            this.StartServer();
            this.connectServer();
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
            _setting.Printers.Add(p);
            
            _setting.SelectedID = "new";
        }

        private void buttonDel_Click(object sender, RoutedEventArgs e)
        {
            foreach (PrinterSetting p in _setting.Printers)
            {
                if (p.ID == _setting.SelectedID)
                {
                    _setting.Printers.Remove(p);
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
            foreach (PrinterSetting p in _setting.Printers)
            {
                if (listBoxPrinters.SelectedItem == p)
                {
                    p.IsDefault = true;
                } else
                {
                    p.IsDefault = false;
                }
            }

            listBoxPrinters.UpdateLayout();
            saveSettings();
        }

        private void menuItemDel_Checked(object sender, RoutedEventArgs e)
        {
            if (listBoxPrinters.SelectedIndex == -1)
            {
                return;
            }
            _setting.Printers.RemoveAt(listBoxPrinters.SelectedIndex);

            Console.WriteLine(listBoxPrinters.SelectedIndex);
        }

        private void buttonPrinters_Click(object sender, RoutedEventArgs e)
        {
            getPrinters();
        }

        private void Window_StateChanged(object sender, EventArgs e)
        {
            if (this.WindowState == WindowState.Minimized)
            {
                // 最小化到系统托盘
                this.Hide();
                // 或者可以选择 this.ShowInTaskbar = false; 来隐藏任务栏图标
            }
            else if (this.WindowState == WindowState.Normal)
            {
                // 从系统托盘恢复
                this.Show();
                // 恢复显示在任务栏
                this.ShowInTaskbar = true;
            }
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
                CrimLogger.InfoLog("print " + command.Seq + "  data:" + command.Data);
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
                            // CrimLogger.InfoLog("同步信息号量");
                            //同步信息号量  
                            Monitor.Enter(PrintQueueMap[item]);

                            // 这儿需要处理其他打印格式
                            PrintImageQueue(item, PrintQueueMap[item], (str) => {
                                Monitor.Exit(PrintQueueMap[item]);
                                // CrimLogger.InfoLog("结束同步:" + str);
                            });

                            //没有任务，休息3秒钟 
                            Thread.Sleep(1000);
                        }
                        catch (Exception ex)
                        {
                            CrimLogger.InfoLog(ex.ToString());
                            Monitor.Exit(PrintQueueMap[item]);
                            //没有任务，休息1秒钟 
                            Thread.Sleep(1000);
                        }
                    }
                    else
                    {
                        //没有任务，休息3秒钟 
                        Thread.Sleep(1000);
                    }
                }
                Thread.Sleep(1000);
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
                CrimLogger.InfoLog("print begin" );
                //WsResponse<string> br = new WsResponse<string>();
                //br.Data = "begin";
                //Send(JsonConvert.SerializeObject(br));
            };

            // 结束打印事件
            printDoc.EndPrint += (object sender, PrintEventArgs e) =>
            {
                CrimLogger.InfoLog("print end ");
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
                int x = PageUtils.ToInchX100(printerSetting.MarginLeft);
                int y = PageUtils.ToInchX100(printerSetting.MarginTop);
                // inch
                int xr = PageUtils.ToInchX100(printerSetting.MarginRight);
                int yb = PageUtils.ToInchX100(printerSetting.MarginBottom);

                int w = 300, h = 551;
                var pagerInch100 = PageUtils.PageToInchX100(printerSetting.Paper);
                w = pagerInch100.Width;
                h = pagerInch100.Height;
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
                // 设置超时时间为5000毫秒
                // request.Timeout = 20000;
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
                        CrimLogger.InfoLog("printed " + command.Key + "  data:" + command.Value);


                        // 通知服务器端
                        PrintStateNotice.Notice(PrintSetting.NoticeUrl, command.Key, command.Value);

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
                CrimLogger.ErrorLog("未找到指定的打印机，请先确认配置");
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
                CrimLogger.InfoLog("print begin" + seq );
                WsResponse<string> br = new WsResponse<string>();
                br.Seq = seq;
                br.Data = "begin";
                Send(JsonConvert.SerializeObject(br));
            };

            // 结束打印事件
            printDoc.EndPrint += (object sender, PrintEventArgs e) =>
            {
                CrimLogger.InfoLog("print end " + seq);
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
                int x = PageUtils.ToInchX100(printerSetting.MarginLeft);
                int y = PageUtils.ToInchX100(printerSetting.MarginTop);
                // inch
                int xr = PageUtils.ToInchX100(printerSetting.MarginRight);
                int yb = PageUtils.ToInchX100(printerSetting.MarginBottom);
                
                int w = 300, h = 551;
                var pagerInch100 = PageUtils.PageToInchX100(printerSetting.Paper);
                w = pagerInch100.Width;
                h = pagerInch100.Height;
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
                // 设置超时时间为5000毫秒
                // request.Timeout = 20000;
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
                        CrimLogger.InfoLog("printed " + image.Key + "  data:" + image.Value);

                        // 通知服务器端
                        PrintStateNotice.Notice(PrintSetting.NoticeUrl, image.Key, image.Value);

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
                    // 清除资源
                    // printDoc.Dispose();
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
            try
            {
                Send(msg);
            }
            catch (Exception ex)
            {
                // 发送消息异常
                CrimLogger.ErrorLog("发送消息异常", ex);
            }
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
}

