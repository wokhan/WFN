﻿using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using Wokhan.WindowsFirewallNotifier.Common;
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

            if (!isInstalled &&
                ((status.PrivateIsEnabled && status.PrivateIsOutBlockedNotif)
                || (status.PublicIsEnabled && status.PublicIsOutBlockedNotif)
                || (status.DomainIsEnabled && status.DomainIsOutBlockedNotif)))
            {
                if (Settings.Default.EnableServiceDetection)
                {
                    //todo: check result of tasklist for this process (should return N/A or similar)
                    var result = ProcessHelper.GetAllServices(System.Diagnostics.Process.GetCurrentProcess().Id);
                    if (result != null && result.Length >= 1)
                    {
                        //did not understand output of taskkill. disable service detection.
                        Settings.Default.EnableServiceDetection = false;
                        MessageBox.Show("WFN does not support your OS language. Please report issue to add \"" + result[0] + "\". Service detection has been disabled.", "Detecting services not supported");
                        Settings.Default.Save();
                    }
                }
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
