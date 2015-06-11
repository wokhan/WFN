using System;
using System.ComponentModel;
using System.Windows;
using Wokhan.WindowsFirewallNotifier.Common.Helpers;
using Wokhan.WindowsFirewallNotifier.Console.UI.Pages;

namespace Wokhan.WindowsFirewallNotifier.Console.UI.Windows
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        public bool IsAdmin { get { return ((App)Application.Current).IsElevated; } }

        private Uri previousUri = null;

        public void GoBack()
        {
            if (previousUri != null)
            {
                mainFrame.Navigate(previousUri);
            }
        }

        public MainWindow()
        {
            InitializeComponent();

            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            Application.Current.DispatcherUnhandledException += Current_DispatcherUnhandledException;
        }

        private void btnConnections_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            mainFrame.Navigate(new Connections());
        }

        private void btnRules_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            mainFrame.Navigate(new Rules());
        }

        private void btnEventsLog_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            mainFrame.Navigate(new EventsLog());
        }

        void Current_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            if (e.Exception is InvalidOperationException && e.Exception.InnerException != null && e.Exception.InnerException is Win32Exception && ((Win32Exception)e.Exception.InnerException).NativeErrorCode == 1314)
            {
                ForceDialog("You must run the Windows Firewall Notifier console as an administrator to be able to use this feature.", "Insufficiant privileges");
            }
            else
            {
                LogHelper.Error("Unexpected error", e.Exception);
                ForceDialog(e.Exception.Message, "Unexpected error");
            }
            e.Handled = true;
        }

        private async void ForceDialog(string p1, string p2)
        {
            //MessageBox.Show(p1, p2);
            /*var dial = await this.GetCurrentDialogAsync<BaseMetroDialog>();

            if (dial != null)
            {
                dial.Title = p2;
                dial.Content = p1;
                await this.HideMetroDialogAsync(dial);
            }
            else
            {
                await this.ShowMessageAsync(p2, p1, MessageDialogStyle.Affirmative);
            }*/
        }

        void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            LogHelper.Error("Unexpected error", (Exception)e.ExceptionObject);
            ForceDialog(((Exception)e.ExceptionObject).Message, "Unexpected error");
            //this.ShowMessageAsync("Unexpected error", ((Exception)e.ExceptionObject).Message, MessageDialogStyle.Affirmative);
        }

        private void btnSettings_Click(object sender, RoutedEventArgs e)
        {
            previousUri = mainFrame.CurrentSource;
            mainFrame.Navigate(new SettingsPage());
        }

        private void btnMonitor_Click(object sender, RoutedEventArgs e)
        {
            mainFrame.Navigate(new Monitor());
        }

        private void btnRestartAdmin_Click(object sender, RoutedEventArgs e)
        {
            ((App)Application.Current).RestartAsAdmin();
        }

        private void btnInfos_Click(object sender, RoutedEventArgs e)
        {
            mainFrame.Navigate(new AdapterInfo());
        }

        private void btnAbout_Click(object sender, RoutedEventArgs e)
        {
            mainFrame.Navigate(new About());
        }

        private void btnMap_Click(object sender, RoutedEventArgs e)
        {
            mainFrame.Navigate(new Map());
        }
    }
}
