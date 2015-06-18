using System.Windows;
using System.Windows.Controls;
using Wokhan.WindowsFirewallNotifier.Common.Helpers;
using Wokhan.WindowsFirewallNotifier.Console.Helpers;

namespace Wokhan.WindowsFirewallNotifier.Console.UI.Pages
{
    /// <summary>
    /// Logique d'interaction pour Status.xaml
    /// </summary>
    public partial class Status : Page
    {
        FirewallHelper.FirewallStatusWrapper status = new FirewallHelper.FirewallStatusWrapper();

        bool isInstalled = false;

        public Status()
        {
            InitializeComponent();

            init();
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            status.Save();

            if (!isInstalled && 
                ((status.PrivateIsEnabled && status.PrivateIsOutBlockedNotif)
                || (status.PublicIsEnabled && status.PublicIsOutBlockedNotif)
                || (status.DomainIsEnabled && status.DomainIsOutBlockedNotif)))
            {
                InstallHelper.EnableProgram(true);
            }
            else if (isInstalled && (!status.PrivateIsEnabled || !status.PrivateIsOutBlockedNotif) && (!status.PublicIsEnabled || !status.PublicIsOutBlockedNotif) && (!status.DomainIsEnabled || !status.DomainIsOutBlockedNotif))
            {
                InstallHelper.RemoveProgram(true, callback, callback);
            }
        }

        private void btnRevert_Click(object sender, RoutedEventArgs e)
        {
            init();
        }

        private void init()
        {
            isInstalled = InstallHelper.IsInstalled();

            status = new FirewallHelper.FirewallStatusWrapper();

            if (status.PrivateIsOutBlocked && isInstalled)
            {
                status.PrivateIsOutBlockedNotif = true;
                status.PrivateIsOutBlocked = false;
            }

            if (status.PublicIsOutBlocked && isInstalled)
            {
                status.PublicIsOutBlockedNotif = true;
                status.PublicIsOutBlocked = false;
            }

            if (status.DomainIsOutBlocked && isInstalled)
            {
                status.DomainIsOutBlockedNotif = true;
                status.DomainIsOutBlocked = false;
            }

            this.DataContext = status;
        }

        private void callback(string title, string details)
        {
            MessageBox.Show(details, title);
        }


        private void btnRestartAdmin_Click(object sender, RoutedEventArgs e)
        {
            ((App)Application.Current).RestartAsAdmin();
        }
    }
}
