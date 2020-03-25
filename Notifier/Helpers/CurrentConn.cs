using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Media;
using Wokhan.WindowsFirewallNotifier.Common.Config;
using Wokhan.WindowsFirewallNotifier.Common.IO.Files;
using Wokhan.WindowsFirewallNotifier.Common.Net.WFP;

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
            get
            {
                if (_icon is null)
                {
                    UpdateIcon();
                }
                return _icon;
            }
            set
            {
                if (_icon != value)
                {
                    _icon = value; NotifyPropertyChanged(nameof(Icon));
                }
            }

        }

        private async void UpdateIcon()
        {
            Icon = await IconHelper.GetIconAsync(CurrentPath).ConfigureAwait(false);
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
        //public string TargetInfoUrl => $"http://whois.domaintools.com/{Target}";  // uses captcha validation :(
        //public string TargetInfoUrl => $"https://bgpview.io/ip/{Target}";
        public string TargetInfoUrl => string.Format(Settings.Default.TargetInfoUrl, Target);  // eg: $"https://bgpview.io/ip/{Target}"
        public string TargetPort { get; set; }
        //public string TargetPortUrl => $"https://www.speedguide.net/port.php?port={TargetPort}";
        public string TargetPortUrl => string.Format(Settings.Default.TargetPortUrl, TargetPort); // eg: $"https://www.speedguide.net/port.php?port={TargetPort}"

        public int Protocol { get; set; }
        public string ProtocolAsString { get { return Common.Net.WFP.Protocol.GetProtocolAsString(Protocol); } }

        private string _resolvedHost = null;
        public string ResolvedHost
        {
            get { return _resolvedHost; }
            set { _resolvedHost = value; NotifyPropertyChanged(nameof(ResolvedHost)); }
        }

        public string[] PossibleServices { get; set; }
        public string[] PossibleServicesDesc { get; set; }

        private int _tentativesCounter = 1;
        public int TentativesCounter
        {
            get { return _tentativesCounter; }
            set { _tentativesCounter = value; NotifyPropertyChanged(nameof(TentativesCounter)); }
        }

    }

}
