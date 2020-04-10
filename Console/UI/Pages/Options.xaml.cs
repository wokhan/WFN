using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Linq;
using Wokhan.WindowsFirewallNotifier.Common.Helpers;
using System.Windows.Threading;
using Wokhan.WindowsFirewallNotifier.Console.Helpers;
using Wokhan.WindowsFirewallNotifier.Common.Config;

namespace Wokhan.WindowsFirewallNotifier.Console.UI.Pages
{
    /// <summary>
    /// Interaction logic for Settings.xaml
    /// </summary>
    public partial class Options : Page
    {
        private Dictionary<string, Brush> _colors = typeof(Colors).GetProperties(BindingFlags.Public | BindingFlags.Static).ToDictionary(c => c.Name, c => (Brush)new SolidColorBrush((Color)c.GetValue(null)));
        public Dictionary<string, Brush> Colors { get { return _colors; } }

        public SolidColorBrush AccentColor
        {
            get { return (SolidColorBrush)Application.Current.Resources["AccentColorBrush"]; }
            set { Application.Current.Resources["AccentColorBrush"] = value; Settings.Default.AccentColor = value; }
        }

        public string ExeConfigurationPath { get; set; } = formatFileMissing(CustomSettingsProvider.ExeConfigurationPath);
        public string RoamingConfigurationPath { get; set; } = formatFileMissing(CustomSettingsProvider.RoamingConfigurationPath);
        public string UserLocalConfigurationPath { get; set; } = formatFileMissing(CustomSettingsProvider.UserLocalConfigurationPath);

        public Options()
        {
            InitializeComponent();
        }

        private static string formatFileMissing(string path)
        {
            return (System.IO.File.Exists(path)) ? path : String.Concat("not found: ", path, "");
        }
        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            Settings.Default.FirstRun = true;   // reset the flag to log os info again once
            Settings.Default.Save();
            InstallHelper.SetAuditPolConnection(enableSuccess: Settings.Default.AuditPolEnableSuccessEvent, enableFailure: true);  // always turn this on for now so that security log and notifier works
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

        private void txtUserLocalConfigurationPath_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (File.Exists(UserLocalConfigurationPath)) { ProcessHelper.StartShellExecutable("explorer.exe", UserLocalConfigurationPath, true); }
        }

        private void txtUserConfigurationPath_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (File.Exists(RoamingConfigurationPath)) { ProcessHelper.StartShellExecutable("explorer.exe", RoamingConfigurationPath, true); }
        }

        private void txtSharedConfigurationPath_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (File.Exists(ExeConfigurationPath)) { ProcessHelper.StartShellExecutable("explorer.exe", ExeConfigurationPath, true); }
        }
    }
}
