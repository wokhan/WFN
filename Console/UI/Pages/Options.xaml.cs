using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Wokhan.WindowsFirewallNotifier.Common;
using System.Linq;
using Wokhan.WindowsFirewallNotifier.Common.Helpers;
using System.Windows.Threading;
using Wokhan.WindowsFirewallNotifier.Console.Helpers;

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

        public string SharedConfigurationPath { get; set; } = CustomSettingsProvider.SharedConfigurationPath;
        public string UserConfigurationPath { get; set; } = CustomSettingsProvider.UserConfigurationPath;
        public string UserLocalConfigurationPath { get; set; } = CustomSettingsProvider.UserLocalConfigurationPath;

        public Options()
        {
            InitializeComponent();
        }

        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            Settings.Default.FirstRun = true;   // reset the flag to log os info again once
            Settings.Default.Save();
            InstallHelper.SetAuditPolConnection(enableSuccess: Settings.Default.AuditPolEnableSuccessEvent, enableFailure:true);
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            Settings.Default.Reload();
        }

        private void btnTestNotif_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Notifier.exe"));
        }

        private void btnRestartAdmin_Click(object sender, RoutedEventArgs e)
        {
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
            //Settings.Default.AlwaysRunAs = true;
        }

        private void txtUserLocalConfigurationPath_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            ProcessHelper.StartShellExecutable("explorer.exe", UserLocalConfigurationPath, true);
        }

        private void txtUserConfigurationPath_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            ProcessHelper.StartShellExecutable("explorer.exe", UserConfigurationPath, true);
        }

        private void txtSharedConfigurationPath_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            ProcessHelper.StartShellExecutable("explorer.exe", SharedConfigurationPath, true);
        }
    }
}
