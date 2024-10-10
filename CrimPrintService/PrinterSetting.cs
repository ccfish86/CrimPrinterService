using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CrimPrintService
{
    public class PrinterSetting : INotifyPropertyChanged
    {
        string id;
        string printer;

        bool isDefault;
        string paper;
        int marginTop;
        int marginLeft;
        int marginRight;
        int marginBottom;

        bool landscape;

        bool autoSize;

        public string Paper
        {
            get { return paper; }
            set
            {
                paper = value;
                OnPropertyChanged(nameof(Paper));
            }
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
                OnPropertyChanged(nameof(Id));
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
                OnPropertyChanged(nameof(Printer));
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
                OnPropertyChanged(nameof(MarginTop));
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
                OnPropertyChanged(nameof(MarginLeft));
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
                OnPropertyChanged(nameof(Landscape));
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
                OnPropertyChanged(nameof(IsDefault));
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
                OnPropertyChanged(nameof(MarginRight));
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
                OnPropertyChanged(nameof(MarginBottom));
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
                OnPropertyChanged(nameof(AutoSize));
            }
        }
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
