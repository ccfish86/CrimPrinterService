using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaishanPrintService.models
{
    /// <summary>
    /// PDF打印消息 type = 5
    /// </summary>
    class PrintServerPdfMsg
    {
        [JsonProperty("id")]
        public string ID { get; set; }

        [JsonProperty("printerId")]
        public string PrinterID { get; set; }

        [JsonProperty("files")]
        public string[] Files { get; set; }

    }
}
