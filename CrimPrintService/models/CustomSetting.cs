using System;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace TaishanPrintService.models
{
    public class CustomSetting : INotifyPropertyChanged
    {
        /// <summary>
        /// 打印服务port
        /// </summary>
        int port;
        /// <summary>
        /// 启动本地服务
        /// </summary>
        bool startLocal;
        /// <summary>
        /// 连接URL
        /// </summary>
        string url;
        /// <summary>
        /// 连接KEY
        /// </summary>
        string key;
        /// <summary>
        /// 连接状态
        /// </summary>
        int state;

        public int Port
        {
            get { return port; }
            set
            {
                port = value;
                OnPropertyChanged(nameof(Port));
            }
        }

        public bool StartLocal
        {
            get { return startLocal; }
            set
            {
                startLocal = value;
                OnPropertyChanged(nameof(StartLocal));
            }
        }

        public string Url
        {
            get { return url; }
            set
            {
                url = value;
                OnPropertyChanged(nameof(Url));
            }
        }

        public string Key
        {
            get { return key; }
            set
            {
                key = value;
                OnPropertyChanged(nameof(Key));
            }
        }
        /// <summary>
        /// 状态
        /// 0 未连接 1 连接中 2 已连接 3 连接失败
        /// </summary>
        public int State
        {
            get { return state; }
            set
            {
                state = value;
                OnPropertyChanged(nameof(State));
            }
        }

        public string NoticeUrl
        {
            get; set;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public bool Started
        {
            get { return _started; }
            set
            {
                _started = value;
                if (PropertyChanged != null)
                {
                    Console.WriteLine("PropertyChanged: Started = " + value);
                    OnPropertyChanged(nameof(Started));
                }
            }
        }

        public string SelectedID { get; set; }

        public ObservableCollection<PrinterSetting> Printers
        {
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
