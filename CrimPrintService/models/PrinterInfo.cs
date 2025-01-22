using System.Collections.Generic;
using System.Drawing.Printing;

namespace TaishanPrintService.models
{
    class PrinterInfo
    {
        public string Name {        get;set;}
        public List<PaperSize> PaperSizes { get; set;}

    }
}
