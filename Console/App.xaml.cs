using System;
using System.IO;
using System.Reflection;
using System.Windows;

using Wokhan.WindowsFirewallNotifier.Common.Config;
using Wokhan.WindowsFirewallNotifier.Common.Helpers;
using Wokhan.WindowsFirewallNotifier.Common.Logging;
using Wokhan.WindowsFirewallNotifier.Common.Processes;

namespace Wokhan.WindowsFirewallNotifier.Console
{
    public partial class App : Application
    {
        public App() : base()
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            this.DispatcherUnhandledException += Current_DispatcherUnhandledException;

            LogHelper.Debug("Starting Console: " + Environment.CommandLine);

            if (Settings.Default.AlwaysRunAs && !UAC.CheckProcessElevated())
            {
                RestartAsAdmin();
            }
        }

        private void Current_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            MessageBox.Show(e.Exception.Message, Common.Properties.Resources.MSG_DLG_ERR_TITLE, MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            MessageBox.Show(((Exception)e.ExceptionObject).Message, Common.Properties.Resources.MSG_DLG_ERR_TITLE, MessageBoxButton.OK, MessageBoxImage.Error);
        }

        public bool IsElevated { get; } = UAC.CheckProcessElevated();

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            if (Settings.Default.AccentColor != null)
            {
                Resources["AccentColorBrush"] = Settings.Default.AccentColor;
            }

            if (Settings.Default.ConsoleSizeWidth > 900)
            {
                Resources["ConsoleSizeWidth"] = Convert.ToDouble(Settings.Default.ConsoleSizeWidth);
            }

            if (Settings.Default.ConsoleSizeHeight > 600)
            {
                Resources["ConsoleSizeHeight"] = Convert.ToDouble(Settings.Default.ConsoleSizeHeight);
            }
        }

        internal void RestartAsAdmin()
        {
            if (System.Diagnostics.Debugger.IsAttached)
            {
                MessageBox.Show("WFN is currently being debugged with a non-admin Visual Studio instance.\r\nDebugger will be detached if you want to use admin-only features as it requires launching a new WFN instance.\r\nTo avoid this, please run Visual Studio with admin privileges.", "Non-admin debugger detected", MessageBoxButton.OK, MessageBoxImage.Information);
            }

            ProcessHelper.RunElevated(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), ProcessNames.WFN.FileName));
            Environment.Exit(0);
        }
    }
}
