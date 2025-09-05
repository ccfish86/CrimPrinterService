using System;
using System.Globalization;
using System.Threading;
using System.Windows;
using System.Windows.Data;
using TaishanPrintService.utils;

namespace CrimPrintService
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : Application
    {
        private static Mutex _mutex;


        protected override void OnStartup(StartupEventArgs e)
        {
            bool createdNew;
            _mutex = new Mutex(true, "Global\\" + AppDomain.CurrentDomain.FriendlyName, out createdNew);
            if (!createdNew)
            {
                // 如果互斥量已存在，说明已有实例在运行
                MessageBox.Show("另一个实例已经在运行，请于系统托盘打开！");

                // 尝试激活已有实例
                try
                {
                    System.Diagnostics.Process current = System.Diagnostics.Process.GetCurrentProcess();
                    System.Diagnostics.Process[] processes = System.Diagnostics.Process.GetProcessesByName(current.ProcessName);
                    foreach (System.Diagnostics.Process process in processes)
                    {
                        if (process.Id != current.Id)
                        {
                            // 激活已有实例
                            NativeMethods.SetForegroundWindow(process.MainWindowHandle);
                            // NativeMethods.SwitchToThisWindow(process.MainWindowHandle, true);
                            break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("激活已有实例失败！" + ex.Message);
                }
                ApplicationVariable.DoubleExeced = true;
                Shutdown(); // 关闭当前实例
                return;
            }

            base.OnStartup(e);

            // 可以在这里添加你的启动逻辑
            _mutex.WaitOne();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            if (_mutex != null)
            {
                if (!ApplicationVariable.DoubleExeced) { 
                    _mutex.ReleaseMutex();
                }
                _mutex.Dispose();
            }

            base.OnExit(e);
        }
    }

    [ValueConversion(typeof(bool), typeof(bool))]
    public class InverseBoolConverter : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            if (targetType != typeof(bool))
            {
                throw new InvalidOperationException("The target must be a boolean");
            }

            return !(bool)value;
        }

        public object ConvertBack(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }


    [ValueConversion(typeof(bool), typeof(bool))]
    public class BoolRadioConverter : IValueConverter
    {
        public bool Inverse { get; set; }

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            bool boolValue = (bool)value;

            return this.Inverse ? !boolValue : boolValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            bool boolValue = (bool)value;

            if (!boolValue)
            {
                // We only care when the user clicks a radio button to select it.
                return null;
            }

            return !this.Inverse;
        }
    }


    [ValueConversion(typeof(bool), typeof(bool))]
    public class BoolToVisibilityConvert : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isVisible = (bool)value;
            return (isVisible) ? Visibility.Visible : Visibility.Hidden;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }


    [ValueConversion(typeof(int), typeof(string))]
    public class ConnectStateToDisplayConvert : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            int state = (int)value;
            if (state == 1)
            {
                return "连接中";
            }
            else if (state == 2)
            {
                return "已连接";

            }
            else if (state == 3)
            {
                return "连接失败";

            }
            else
            {
                return "未连接"; 
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    #endregion

}
