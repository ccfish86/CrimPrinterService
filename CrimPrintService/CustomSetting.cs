using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

        public event PropertyChangedEventHandler PropertyChanged;
        public void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
       

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

        public string SelectedID { get; set; }

        public ObservableCollection<PrinterSetting> Printers {
            get;
            set;
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
