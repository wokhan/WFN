using System;
using System.Windows;
using Wokhan.WindowsFirewallNotifier.Common;
using Wokhan.WindowsFirewallNotifier.Common.Helpers;

namespace Wokhan.WindowsFirewallNotifier.Console
{
    public partial class App : Application
    {
        public App() : base()
        {
            LogHelper.Debug("Starting Console: " + Environment.CommandLine);
            CommonHelper.OverrideSettingsFile("WFN.config");

            if (Settings.Default.AlwaysRunAs && !UacHelper.CheckProcessElevated())
            {
                RestartAsAdmin();
            }

            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            this.DispatcherUnhandledException += Current_DispatcherUnhandledException;
        }

        private void Current_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            MessageBox.Show(e.Exception.Message, Common.Resources.MSG_DLG_ERR_TITLE, MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            MessageBox.Show(((Exception)e.ExceptionObject).Message, Common.Resources.MSG_DLG_ERR_TITLE, MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private bool? _isElevated = null;
        public bool IsElevated
        {
            get
            {
                if (_isElevated == null) { _isElevated = UacHelper.CheckProcessElevated(); }
                return _isElevated.Value;
            }
        }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            if (Settings.Default.AccentColor != null)
            {
                Resources["AccentColorBrush"] = Settings.Default.AccentColor;
            }
        }

        internal void RestartAsAdmin()
        {
            ProcessHelper.ElevateCurrentProcess();
            Environment.Exit(0);
        }
    }
}
