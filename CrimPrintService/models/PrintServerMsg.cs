using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaishanPrintService.models
{
    /// <summary>
    /// 服务器消息
    /// </summary>
    class PrintServerMsg
    {
        [JsonProperty("data")]
        public string Data { get; set; }
        [JsonProperty("sender")]
        public string Sender { get; set; }
        [JsonProperty("type")]
        public int Type { get; set; }
    }
}
