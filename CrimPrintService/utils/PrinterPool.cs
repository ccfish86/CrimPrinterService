using CrimPrintService.utils;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using TaishanPrintService.models;

namespace TaishanPrintService.utils
{
    /// <summary>
    /// 打印队列
    /// </summary>
    class PrinterPool
    {
        static PrinterPool _printerPool;
        public  static  PrinterPool GetPrinterPool()
        {
            if (_printerPool == null)
            {
                _printerPool = new PrinterPool();
            }
            return _printerPool;
        }

        /// <summary>
        /// 打印图片
        /// </summary>
        /// <param name="printerSetting"></param>
        /// <param name="imageList"></param>
        public void printImage(PrinterSetting printerSetting, string id, List<string> imageList, Action<string> sendToServer)
        {
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
                CrimLogger.InfoLog("print begin" + id);
                var printResult = new PrintServerPdfPrintResult
                {
                    ID = id,
                    PrintState = 2
                };
                PrintServerAck br = new PrintServerAck
                {
                    Seq = id,
                    Type = 106,
                    Data = JsonConvert.SerializeObject(printResult)
                };
                sendToServer(JsonConvert.SerializeObject(br));
            };

            // 结束打印事件
            printDoc.EndPrint += (object sender, PrintEventArgs e) =>
            {
                CrimLogger.InfoLog("print end [" + id + "]: ");
                var printResult = new PrintServerPdfPrintResult
                {
                    ID = id,
                    PrintState = 3
                };
                PrintServerAck er = new PrintServerAck
                {
                    Seq = id,
                    Type = 106,
                    Data = JsonConvert.SerializeObject(printResult)
                };
                sendToServer(JsonConvert.SerializeObject(er));
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

                string image = imageList[page - 1];
                // Image i = Image.FromFile(image);
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(image);
                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse()) //获得响应) 
                {
                    if (response.StatusCode != HttpStatusCode.OK)
                    {

                        //WsResponse<KeyValuePair<string, string>> rerr = new WsResponse<KeyValuePair<string, string>>();
                        //rerr.Seq = seq;
                        //rerr.Code = "NET_ERR";
                        //rerr.Data = new KeyValuePair<string, string>(image.Key, "error:" + response.StatusCode);
                        //string msgerr = JsonConvert.SerializeObject(rerr);
                        //Send(msgerr);
                        CrimLogger.ErrorLog("print error [" + id + "]: " + image);
                        CrimLogger.ErrorLog("print error [" + response.StatusCode + "]: " + response.StatusDescription);
                        var printResult = new PrintServerPdfPrintResult
                        {
                            ID = id,
                            PrintState = 6
                        };
                        PrintServerAck er = new PrintServerAck
                        {
                            Seq = id,
                            Code = response.StatusCode.ToString(),
                            Message = response.StatusDescription,
                            Type = 106,
                            Data = JsonConvert.SerializeObject(printResult)
                        };
                        sendToServer(JsonConvert.SerializeObject(er));
                    }
                    else
                    {
                        using (Stream stream = response.GetResponseStream())
                        {
                            Image img = Image.FromStream(stream);///实例化,得到img

                            if (printerSetting.AutoSize)
                            {
                                // e.MarginBounds
                                e.MarginBounds.Offset(0, 0);
                                //Console.WriteLine(e.MarginBounds);
                                //Rectangle rect = new Rectangle(x, y, w - x - xr, h - y - yb);
                                //e.Graphics.DrawImage(img, rect);
                                e.Graphics.DrawImage(img, e.MarginBounds);
                                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                                e.Graphics.DrawImage(img, e.Graphics.VisibleClipBounds);
                            }
                            else
                            {
                                e.MarginBounds.Offset(0, 0);
                                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                                e.Graphics.DrawImage(img, e.PageSettings.HardMarginX + x, e.PageSettings.HardMarginY + y, w, h);
                            }
                        }
                        CrimLogger.InfoLog("printed " + id + "  data:" + image);
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
        }

        ConcurrentDictionary<String, Queue<PrintQueueItem>> PrintQueueMap = new ConcurrentDictionary<String, Queue<PrintQueueItem>>(); //实例化一个队列

        public object PrintSetting { get; private set; }
    }
}
