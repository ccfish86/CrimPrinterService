using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace CrimPrintService
{
   public class CustomSetting : INotifyPropertyChanged
    {
 
        public int Port
        {
            get;
            set;
        }

        string _paper;

        public event PropertyChangedEventHandler PropertyChanged;
        public void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        public string Paper {
            get
            {
                return _paper;
            }
            set
            {
                Console.WriteLine("change pager" + value);
                _paper = value;
            }
        }
        public string Printer {
            get;
            set;
        }

        public int MarginRight
        {
            get;
            set;
        }
        public int MarginTop
        {
            get;
            set;
        }

        public int MarginBottom
        {
            get;
            set;
        }



        public int MarginLeft
        {
            get;
            set;
        }

        public bool AutoSize { get; set; }

        public bool Started
        {
            get { return _started; }
            set {
                _started = value;
                if (PropertyChanged != null)
                {
                    Console.WriteLine("PropertyChanged: Started = " + value);
                    PropertyChanged(this, new PropertyChangedEventArgs("Started"));
                }
            }
        }

        public bool Landscape
        {
            get;set;
        }
        private bool _started;
        //public static readonly DependencyProperty StartedProperty = DependencyProperty.Register("Started", typeof(bool), typeof(MainWindow));
        //public bool Started
        //{
        //    get { return (bool)GetValue(StartedProperty); }
        //    set { SetValue(StartedProperty, value); }
        //}

    }
}
