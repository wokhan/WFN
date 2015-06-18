using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Media;
using Wokhan.WindowsFirewallNotifier.Common;
using Wokhan.WindowsFirewallNotifier.Common.Helpers;

namespace Wokhan.WindowsFirewallNotifier.Console
{
    public partial class App : Application
    {
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
            this.MainWindow.Close();
        }
    }
}
