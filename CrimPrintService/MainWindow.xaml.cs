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
        public static readonly DependencyProperty dpSetting = DependencyProperty.
           Register("Setting", typeof(CustomSetting), typeof(MainWindow));

        public MainWindow()
        {
            InitializeComponent();


            AfterInit();

            this.getPrinters();

            this.mainGrid.DataContext = Setting;

        }

        private void AfterInit()
        {
            _setting = loadSetting();
            _setting.Port = 9999;
            _setting.MarginTop = 0;
            _setting.MarginLeft = 0;
            _setting.Landscape = false;
            _setting.Paper = "1";
            _setting.Started = false;
        }

        private CustomSetting loadSetting()
        {
            if (File.Exists("setting.json")) {
                string str = File.ReadAllText("setting.json");
                return JsonConvert.DeserializeObject<CustomSetting>(str);
            }
            return new CustomSetting();
        }

        public int Port
        {
            get;
            set;
        }

        private void getPrinters()
        {
            PrintDocument printDocument = new PrintDocument();
            // printDocument.printDocument.; ;
            PrinterSettings.StringCollection printers = PrinterSettings.InstalledPrinters;
            List<string> result = new List<string>();
            foreach (string item in printers)
            {
                result.Add(item);
                Console.WriteLine(item);
            }

            Setting.Printer = printDocument.PrinterSettings.PrinterName;

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
            Console.WriteLine(this._setting.Port);
            Console.WriteLine(this.Port);
            String config = JsonConvert.SerializeObject(Setting);
            File.WriteAllText("setting.json", config);
        }

        private void buttonStart_Click(object sender, RoutedEventArgs e)
        {

            // 启动服务
            wssv = new WebSocketServer("ws://0.0.0.0:" + Setting.Port);
            wssv.AddWebSocketService<Laputa>("/Laputa",(Laputa laputa) => {
                laputa.PrintSetting = Setting;
            });
            try
            {
                Setting.Started = true;
                Console.WriteLine("server start");
                wssv.Start();
            }
            catch (InvalidOperationException ex)
            {
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
    }

    public class Laputa : WebSocketBehavior
    {
        protected override void OnMessage(MessageEventArgs e)
        {
            WsCommand command = JsonConvert.DeserializeObject<WsCommand>(e.Data);

            if (command.Command == "print") {
                lock(this) {
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

        public CustomSetting PrintSetting
        {
            get;set;
        }

        protected void printImage(string seq, string image)
        {
            PrintController printController = new StandardPrintController();
            PrintDocument printDoc = new PrintDocument();
            printDoc.DefaultPageSettings.Landscape = PrintSetting.Landscape;
            printDoc.PrinterSettings.PrinterName = PrintSetting.Printer;
            printDoc.PrinterSettings.Copies = 1;
            printDoc.PrintController = printController;
            printDoc.BeginPrint += (object sender, PrintEventArgs e) =>
            {
                WsResponse br = new WsResponse();
                br.Seq = seq;
                br.Data = "begin";
                Send(JsonConvert.SerializeObject(br));
            };
            printDoc.EndPrint += (object sender, PrintEventArgs e) =>
            {
                WsResponse er = new WsResponse();
                er.Seq = seq;
                er.Data = "end";
                Send(JsonConvert.SerializeObject(er));
            };
            printDoc.PrintPage += (object sender, PrintPageEventArgs e) =>
            {
                // inch
                int x = toInchX100(PrintSetting.MarginLeft);
                int y = toInchX100(PrintSetting.MarginTop);
                // inch
                int xr = toInchX100(PrintSetting.MarginRight);
                int yb = toInchX100(PrintSetting.MarginBottom);

                int w = 300, h = 551;
                switch(PrintSetting.Paper)
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

                printDoc.DefaultPageSettings.PaperSize = new PaperSize("自定义", w, h);//inch
                // Image i = Image.FromFile(image);
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(image);
                WebResponse response = request.GetResponse();//获得响应
                Image img = Image.FromStream(response.GetResponseStream());///实例化,得到img
                //                Conole.wrimg.Width       img.Height
                Console.WriteLine(img.Width + "x" + img.Height);

                if (PrintSetting.AutoSize)
                {
                    // e.MarginBounds
                    Console.WriteLine(e.MarginBounds);
                    Rectangle rect = new Rectangle(x, y, w - x - xr, h - y - yb);
                    e.Graphics.DrawImage(img, rect);
                } else
                {
                    e.Graphics.DrawImage(img, x, y, w, h);
                }
                e.HasMorePages = false;
            };
            printDoc.Print();

            WsResponse r = new WsResponse();
            r.Seq = seq;
            r.Data = "printed";
            string msg = JsonConvert.SerializeObject(r);
            Send(msg);
        }

        int toInchX100(int mm)
        {
            return (int)(mm * 3.937);
        }

        protected void printHtml(string seq, string html)
        {

            WsResponse r = new WsResponse();
            r.Seq = seq;
            r.Data = "暂不支持HTML";
            string msg = JsonConvert.SerializeObject(r);
            Send(msg);
        }

        protected override void OnOpen()
        {
            Console.WriteLine("opened");
            WsResponse r = new WsResponse();
            r.Data = "连接成功";
            string msg = JsonConvert.SerializeObject(r);
            Send(msg);
        }
    }

    public class WsResponse
    {
        public string Seq { get; set; }
        public string Data
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

        public string Data {
            get;
            set;
        }
    }
}
