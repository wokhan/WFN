﻿using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Media;
using Wokhan.WindowsFirewallNotifier.Common.Helpers;

namespace Wokhan.WindowsFirewallNotifier.Notifier.Helpers
{
    public class CurrentConn : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public string Description { get; set; }
        public string ProductName { get; set; }
        public string Company { get; internal set; }

        private ImageSource _icon;
        public ImageSource Icon
        {
            get { return _icon; }
            set { _icon = value; NotifyPropertyChanged("Icon"); }

        }

        public string CurrentPath { get; set; }
        public string CurrentAppPkgId { get; set; }
        public string CurrentLocalUserOwner { get; set; }
        public string CurrentService { get; set; }
        public string CurrentServiceDesc { get; set; }
        public string RuleName { get; set; }
        private List<int> _localPortArray = new List<int>();
        public List<int> LocalPortArray { get { return _localPortArray; } }
        public string LocalPort { get; set; }
        public string Target { get; set; }
        public string TargetInfoUrl => $"http://whois.domaintools.com/{Target}";
        public string TargetPort { get; set; }
        public string TargetPortUrl => $"https://www.speedguide.net/port.php?port={TargetPort}";
        public int Protocol { get; set; }
        public string ProtocolAsString { get { return FirewallHelper.getProtocolAsString(Protocol); } }

        private string _resolvedHost = null; //FIXME: Is this being used???
        public string ResolvedHost
        {
            get { return _resolvedHost; }
            set { _resolvedHost = value; NotifyPropertyChanged("ResolvedHost"); }
        }

        public string[] PossibleServices { get; set; }
        public string[] PossibleServicesDesc { get; set; }

        private int _tentativesCounter = 1;
        public int TentativesCounter
        {
            get { return _tentativesCounter; }
            set { _tentativesCounter = value; NotifyPropertyChanged("TentativesCounter"); }
        }
    }

}
