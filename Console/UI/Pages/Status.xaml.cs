using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using Wokhan.WindowsFirewallNotifier.Common.Helpers;
using Wokhan.WindowsFirewallNotifier.Console.Helpers;

namespace Wokhan.WindowsFirewallNotifier.Console.UI.Pages
{
    /// <summary>
    /// Logique d'interaction pour Status.xaml
    /// </summary>
    public partial class Status : Page, INotifyPropertyChanged
    {
        FirewallHelper.FirewallStatusWrapper status = new FirewallHelper.FirewallStatusWrapper();

        bool isInstalled = false;

        private string _lastMessage;
        public string LastMessage
        {
            get { return _lastMessage; }
            private set { _lastMessage = value; NotifyPropertyChanged("LastMessage"); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(string caller)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(caller));
            }
        }

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
                InstallHelper.EnableProgram(true, callback);
            }
            else if (isInstalled && (!status.PrivateIsEnabled || !status.PrivateIsOutBlockedNotif) && (!status.PublicIsEnabled || !status.PublicIsOutBlockedNotif) && (!status.DomainIsEnabled || !status.DomainIsOutBlockedNotif))
            {
                InstallHelper.RemoveProgram(true, callback);
            }

            init();
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

            stackOptions.DataContext = status;
        }

        private void callback(bool isSuccess, string details)
        {
            LastMessage = details;
        }


        private void btnRestartAdmin_Click(object sender, RoutedEventArgs e)
        {
            ((App)Application.Current).RestartAsAdmin();
        }
    }
}
