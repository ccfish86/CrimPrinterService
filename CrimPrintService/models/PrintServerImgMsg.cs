using Newtonsoft.Json;

using System.Collections.Generic;

namespace TaishanPrintService.models
{
    /// <summary>
    /// 打印快递单
    /// </summary>
    class PrintServerImgMsg
    {
        [JsonProperty("id")]
        public string ID { get; set; }

        [JsonProperty("printerId")]
        public string PrinterID { get; set; }

        [JsonProperty("files")]
        public List<string> Files { get; set; }
    }
}
