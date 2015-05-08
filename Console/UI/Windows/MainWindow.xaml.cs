using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Security.Principal;
using System.Windows;

namespace Wokhan.WindowsFirewallNotifier.Console.UI.Windows
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        public bool IsAdmin { get { return new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator); } }

        public MainWindow()
        {
            InitializeComponent();

            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            Application.Current.DispatcherUnhandledException += Current_DispatcherUnhandledException;
        }

        private void btnConnections_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            mainFrame.Navigate(new Uri("/UI/Pages/Connections.xaml", UriKind.Relative));
        }

        private void btnRules_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            mainFrame.Navigate(new Uri("/UI/Pages/Rules.xaml", UriKind.Relative));
        }

        private void btnEventsLog_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            mainFrame.Navigate(new Uri("/UI/Pages/EventsLog.xaml", UriKind.Relative));
        }

        void Current_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            if (e.Exception is InvalidOperationException && e.Exception.InnerException != null && e.Exception.InnerException is Win32Exception && ((Win32Exception)e.Exception.InnerException).NativeErrorCode == 1314)
            {
                ForceDialog("You must run the Windows Firewall Notifier console as an administrator to be able to use this feature.", "Insufficiant privileges");
            }
            else
            {
                ForceDialog(e.Exception.Message, "Unexpected error");
            }
            e.Handled = true;
        }

        private async void ForceDialog(string p1, string p2)
        {
            MessageBox.Show(p2, p1);
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
            ForceDialog(((Exception)e.ExceptionObject).Message, "Unexpected error");
            //this.ShowMessageAsync("Unexpected error", ((Exception)e.ExceptionObject).Message, MessageDialogStyle.Affirmative);
        }

        private void btnSettings_Click(object sender, RoutedEventArgs e)
        {
            new SettingsWindow().ShowDialog();
        }

        private void btnMonitor_Click(object sender, RoutedEventArgs e)
        {
            mainFrame.Navigate(new Uri("/UI/Pages/Monitor.xaml", UriKind.Relative));
        }
    }
}
