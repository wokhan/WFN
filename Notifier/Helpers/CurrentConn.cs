using System.Collections.Generic;
using System.ComponentModel;

using Wokhan.ComponentModel.Extensions;
using Wokhan.WindowsFirewallNotifier.Common.Config;
using Wokhan.WindowsFirewallNotifier.Common.UI.ViewModels;

namespace Wokhan.WindowsFirewallNotifier.Notifier.Helpers
{
    public class CurrentConn : LogEntryViewModel, INotifyPropertyChanged
    {
        public string CurrentAppPkgId { get; set; }
        public string CurrentLocalUserOwner { get; set; }
        public string CurrentService { get; set; }
        public string CurrentServiceDesc { get; set; }
        public SortedSet<int> LocalPortArray { get; } = new SortedSet<int>();
        
        //public string TargetInfoUrl => $"http://whois.domaintools.com/{Target}";  // uses captcha validation :(
        //public string TargetInfoUrl => $"https://bgpview.io/ip/{Target}";
        public string TargetInfoUrl => string.Format(Settings.Default.TargetInfoUrl, TargetIP);  // eg: $"https://bgpview.io/ip/{Target}"
        public string TargetPortUrl => string.Format(Settings.Default.TargetPortUrl, TargetPort); // eg: $"https://www.speedguide.net/port.php?port={TargetPort}"

        private string _resolvedHost;
        public string ResolvedHost
        {
            get { return _resolvedHost; }
            set => this.SetValue(ref _resolvedHost, value, NotifyPropertyChanged);
        }

        public string[] PossibleServices { get; set; }
        public string[] PossibleServicesDesc { get; set; }

        private int _tentativesCounter = 1;
        public int TentativesCounter
        {
            get { return _tentativesCounter; }
            set => this.SetValue(ref _tentativesCounter, value, NotifyPropertyChanged);
        }
    }
}
