using System.Collections.Generic;
using System.ComponentModel;

using Wokhan.ComponentModel.Extensions;
using Wokhan.WindowsFirewallNotifier.Common.Config;
using Wokhan.WindowsFirewallNotifier.Common.Core;
using Wokhan.WindowsFirewallNotifier.Common.Processes;
using Wokhan.WindowsFirewallNotifier.Common.UI.ViewModels;

namespace Wokhan.WindowsFirewallNotifier.Notifier.Helpers
{
    public class CurrentConn : LogEntryViewModel, INotifyPropertyChanged
    {
        //private string _currentAppPkgId;
        //TODO: rename as it's not something "current"
        public string CurrentAppPkgId { get; set; }// => this.GetOrSetAsyncValue(() => ProcessHelper.GetAppPkgId(Pid), NotifyPropertyChanged, nameof(_currentAppPkgId));

        //private string _currentLocalUserOwner;
        //TODO: rename as it's not something "current"
        public string CurrentLocalUserOwner { get; set; }// => this.GetOrSetAsyncValue(() => ProcessHelper.GetLocalUserOwner(Pid), NotifyPropertyChanged, nameof(_currentLocalUserOwner));
        //TODO: rename as it's not something "current"
        public string CurrentService { get; set; }
        //TODO: rename as it's not something "current"
        public string CurrentServiceDesc { get; set; }
        public SortedSet<int> LocalPortArray { get; } = new SortedSet<int>();
        
        //public string TargetInfoUrl => $"http://whois.domaintools.com/{Target}";  // uses captcha validation :(
        //public string TargetInfoUrl => $"https://bgpview.io/ip/{Target}";
        public string TargetInfoUrl => string.Format(Settings.Default.TargetInfoUrl, TargetIP);  // eg: $"https://bgpview.io/ip/{Target}"
        public string TargetPortUrl => string.Format(Settings.Default.TargetPortUrl, TargetPort); // eg: $"https://www.speedguide.net/port.php?port={TargetPort}"

        private string _resolvedHost;
        public string ResolvedHost
        {
            get => _resolvedHost;
            set => this.SetValue(ref _resolvedHost, value, NotifyPropertyChanged);
        }

        //TODO: remove since it's now useless
        public string[] PossibleServices { get; set; }
        //TODO: remove since it's now useless
        public string[] PossibleServicesDesc { get; set; }

        private int _tentativesCounter = 1;
        public int TentativesCounter
        {
            get => _tentativesCounter;
            set => this.SetValue(ref _tentativesCounter, value, NotifyPropertyChanged);
        }
    }
}
