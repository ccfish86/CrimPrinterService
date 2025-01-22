using CrimPrintService.utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace CrimPrintService
{
    public class PrintStateNotice
    {

        public static void Notice(string url, string key, string purl)
        {
            if (string.IsNullOrEmpty(url))
            {
                return;
            }
            string req = string.Format(@"{0}?k={1}&p={2}", url, key, Uri.EscapeUriString(purl));
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(req);
            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse()) //获得响应) 
            {
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    CrimLogger.InfoLog("print state noticed " + purl);
                }
                else
                {
                    CrimLogger.ErrorLog("print state noticed error " + purl);
                }
            }
        }
    }
}
