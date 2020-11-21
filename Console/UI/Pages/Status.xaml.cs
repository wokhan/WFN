using System.Windows;
using System.Windows.Controls;
using System.Diagnostics;
using System.IO;
using System;
using Wokhan.WindowsFirewallNotifier.Common.Config;
using Wokhan.WindowsFirewallNotifier.Common.Processes;
using Wokhan.WindowsFirewallNotifier.Console.ViewModels;
using System.Collections.ObjectModel;
using Wokhan.WindowsFirewallNotifier.Common.Logging;

namespace Wokhan.WindowsFirewallNotifier.Console.UI.Pages
{
    /// <summary>
    /// Logique d'interaction pour Status.xaml
    /// </summary>
    public partial class Status : Page
    {
        FirewallStatusWrapper status = new FirewallStatusWrapper();

        public ObservableCollection<string> Messages { get; } = new ObservableCollection<string>();

        public Status()
        {
            InitializeComponent();

            this.Loaded += (sender, args) => init();
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            Settings.Default.Save();
            Messages.Add("=== Applying modifications ===");
            status.Save(RunAndLogResult);
            init();
        }

        private bool RunAndLogResult(Func<bool> func, string okMsg, string errorMsg)
        {
            try
            {
                bool success = func();
                var msg = success ? okMsg : errorMsg;
                Messages.Add($"{DateTime.Now} - {msg}");

                LogHelper.Debug($"{func.Method.Name}: {msg}");

                return success;
            }
            catch (Exception ex)
            {
                LogHelper.Error(ex.Message, ex);

                Messages.Add($"{DateTime.Now} - {errorMsg}: {ex.Message}");
                return false;
            }
        }

        private void btnRevert_Click(object sender, RoutedEventArgs e)
        {
            //TODO: not implemented properly
            Settings.Default.IsInstalled = false;
            Settings.Default.Save();

            init();
        }

        private void init()
        {
            status = new FirewallStatusWrapper();

            stackOptions.DataContext = status;
            messsageInfoPanel.DataContext = this;
        }

        private void btnTestNotif_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Does not show if mimimized to tray
            //ProcessHelper.StartOrRestoreToForeground(ProcessNames.Notifier);  
            Process.Start(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ProcessNames.Notifier.FileName));
        }
    }
}
