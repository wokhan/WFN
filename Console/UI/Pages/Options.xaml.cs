using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Linq;
using Wokhan.WindowsFirewallNotifier.Common.Helpers;
using Wokhan.WindowsFirewallNotifier.Console.Helpers;
using Wokhan.WindowsFirewallNotifier.Common.Config;

namespace Wokhan.WindowsFirewallNotifier.Console.UI.Pages
{
    /// <summary>
    /// Interaction logic for Settings.xaml
    /// </summary>
    public partial class Options : Page
    {
        public Dictionary<string, Brush> Colors { get; } = typeof(Colors).GetProperties(BindingFlags.Public | BindingFlags.Static).ToDictionary(c => c.Name, c => (Brush)new SolidColorBrush((Color)c.GetValue(null)));

        public Options()
        {
            InitializeComponent();
        }

        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            Settings.Default.FirstRun = true;   // reset the flag to log os info again once
            Settings.Default.Save();
            InstallHelper.SetAuditPolConnection(enableSuccess: Settings.Default.AuditPolEnableSuccessEvent, enableFailure:true);  // always turn this on for now so that security log and notifier works
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            Settings.Default.Reload();
        }

        private void btnTestNotif_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Notifier.exe"));
        }

        private void btnRestartAdmin_Click(object sender, RoutedEventArgs e)
        {
            // TODO: @wokhan to be removed for power users?
            ((App)Application.Current).RestartAsAdmin();
        }

        private void txtCurrentLogPath_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            ProcessHelper.StartShellExecutable("explorer.exe", LogHelper.CurrentLogsPath, true);
        }

        private void btnResetDefault_Click(object sender, RoutedEventArgs e)
        {
            Settings.Default.Reset();
            Settings.Default.FirstRun = true;
            Settings.Default.EnableVerboseLogging = false;
        }

        private void txtUserConfigurationPath_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            ProcessHelper.StartShellExecutable("explorer.exe", $"\"{Settings.Default.ConfigurationPath}\"", true);
        }
    }
}
