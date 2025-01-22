using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaishanPrintService.models
{
    /// <summary>
    /// 打印消息
    /// </summary>
    class PrintServerAck
    {
        /// <summary>
        /// 类型
        /// </summary>
        [JsonProperty("type")]
        public int Type { get; set; }
        /// <summary>
        /// 命令SEQ
        /// </summary>
        [JsonProperty("seq")]
        public string Seq { get; set; }
        /// <summary>
        /// 错误编码
        /// </summary>
        [JsonProperty("code")]
        public string Code { get; set; }
        /// <summary>
        /// 错误内容
        /// </summary>
        [JsonProperty("message")]
        public string Message { get; set; }
        /// <summary>
        /// 业务数据
        /// </summary>
        [JsonProperty("data")]
        public string Data
        {
            get;
            set;
        }
    }

    class  PrintServerPdfPrintResult
    {
        [JsonProperty("id")]
        public string ID { get; set; }
        [JsonProperty("printState")]
        public int PrintState { get; set; }
    }
}
