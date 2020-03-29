using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using Wokhan.WindowsFirewallNotifier.Common.Helpers;
using Wokhan.WindowsFirewallNotifier.Console.Helpers;
using System.Diagnostics;
using System.IO;
using System;
using Wokhan.WindowsFirewallNotifier.Common.Config;
using Wokhan.WindowsFirewallNotifier.Common.Processes;
using static Wokhan.WindowsFirewallNotifier.Common.Net.WFP.FirewallHelper;

namespace Wokhan.WindowsFirewallNotifier.Console.UI.Pages
{
    /// <summary>
    /// Logique d'interaction pour Status.xaml
    /// </summary>
    public partial class Status : Page, INotifyPropertyChanged
    {
        FirewallStatusWrapper status = new FirewallStatusWrapper();

        bool isInstalled = false;

        private string _lastMessage;
        public string LastMessage
        {
            get { return _lastMessage; }
            private set { _lastMessage = value; NotifyPropertyChanged(nameof(LastMessage)); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public Status()
        {
            InitializeComponent();
            init();
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            //PrivateIsOutBlockedNotif is also valid for public and domain
            status.PublicIsOutBlockedNotif = status.PrivateIsOutBlockedNotif;
            status.DomainIsOutBlockedNotif = status.PrivateIsOutBlockedNotif;
            if (status.PrivateIsOutBlockedNotif == false)
            {
                //if not blocked, allowed must be true
                if (status.PrivateIsOutBlocked == false)
                    status.PrivateIsOutAllowed = true;
                if (status.PublicIsOutBlocked == false)
                    status.PublicIsOutAllowed = true;
                if (status.DomainIsOutBlocked == false)
                    status.DomainIsOutAllowed = true;
            }
            status.Save();

            bool checkResult(Func<bool> boolFunction, string okMsg, string errorMsg)
            {
                try
                {
                    bool success = boolFunction.Invoke();
                    LastMessage = success ? okMsg : errorMsg;
                    LogHelper.Debug($"{boolFunction.Method.Name}: {LastMessage}");
                    return success;
                }
                catch (Exception ex)
                {
                    LogHelper.Error(ex.Message, ex);
                    LastMessage = $"{errorMsg}: {ex.Message}";
                    return false;
                }
            }

            if (!isInstalled)
            {
                if (!InstallHelper.Install(checkResult))
                {
                    return;
                }
            }
            else if (isInstalled)
            {
                if (!InstallHelper.InstallCheck(checkResult))
                {
                    return;
                }
            }

            init();
        }

        // TODO: remove?
        private static bool isEnabled(FirewallStatusWrapper status)
        {
            return status.PrivateIsEnabled || status.DomainIsEnabled || status.PublicIsEnabled;
        }


        // TODO: remove?
        private static bool isBlockAndPromptEnabledInProfile(FirewallStatusWrapper status)
        {
            return ((status.PrivateIsEnabled && status.PrivateIsOutBlockedNotif)
                       || (status.PublicIsEnabled && status.PublicIsOutBlockedNotif)
                       || (status.DomainIsEnabled && status.DomainIsOutBlockedNotif));
        }

        // TODO: remove?
        private static bool IsBlockAndPromptEnabled(FirewallStatusWrapper status)
        {
            return status.PrivateIsOutBlockedNotif || status.PublicIsOutBlockedNotif || status.DomainIsOutBlockedNotif;
        }

        private void btnRevert_Click(object sender, RoutedEventArgs e)
        {
            Settings.Default.IsInstalled = false;
            Settings.Default.Save();

            init();
        }

        private void init()
        {
            isInstalled = InstallHelper.IsInstalled();

            status = new FirewallStatusWrapper();

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
            messsageInfoPanel.DataContext = this;
        }

        private void btnRestartAdmin_Click(object sender, RoutedEventArgs e)
        {
            ((App)Application.Current).RestartAsAdmin();
        }

        private void btnTestNotif_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Does not show if mimimized to tray
            //ProcessHelper.StartOrRestoreToForeground(ProcessNames.Notifier);  
            Process.Start(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ProcessNames.Notifier.FileName));
        }
    }
}
