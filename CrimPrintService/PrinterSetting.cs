using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CrimPrintService
{
    public class PrinterSetting
    {
        string id;
        string printer;

        bool isDefault;

        int marginTop;
        int marginLeft;
        int marginRight;
        int marginBottom;

        bool landscape;

        bool autoSize;

        public string ID
        {
            get
            {
                return Id;
            }

            set
            {
                Id = value;
            }
        }
        


        public string Paper
        {
            get;
            set;
        }

        public string Id
        {
            get
            {
                return id;
            }

            set
            {
                id = value;
            }
        }

        public string Printer
        {
            get
            {
                return printer;
            }

            set
            {
                printer = value;
            }
        }

        public int MarginTop
        {
            get
            {
                return marginTop;
            }

            set
            {
                marginTop = value;
            }
        }

        public int MarginLeft
        {
            get
            {
                return marginLeft;
            }

            set
            {
                marginLeft = value;
            }
        }

        public bool Landscape
        {
            get
            {
                return landscape;
            }

            set
            {
                landscape = value;
            }
        }

        public bool IsDefault
        {
            get
            {
                return isDefault;
            }

            set
            {
                isDefault = value;
            }
        }

        public int MarginRight
        {
            get
            {
                return marginRight;
            }

            set
            {
                marginRight = value;
            }
        }

        public int MarginBottom
        {
            get
            {
                return marginBottom;
            }

            set
            {
                marginBottom = value;
            }
        }

        public bool AutoSize
        {
            get
            {
                return autoSize;
            }

            set
            {
                autoSize = value;
            }
        }

    }
}
