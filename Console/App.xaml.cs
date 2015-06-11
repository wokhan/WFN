using System.Diagnostics;
using System.Windows;

namespace Wokhan.WindowsFirewallNotifier.Console
{
    public partial class App : Application
    {
        public bool IsElevated { get; set; }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            IsElevated = (e.Args.Length > 0 && e.Args[0] == "iselevated");
        }

        internal void RestartAsAdmin()
        {
            Process.Start("UACWrapper.exe");
            this.MainWindow.Close();
        }
    }
}
