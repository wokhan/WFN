using CommunityToolkit.Mvvm.ComponentModel;

using System.Collections.Generic;
using System.ComponentModel;
using Wokhan.WindowsFirewallNotifier.Common.UI.ViewModels;

namespace Wokhan.WindowsFirewallNotifier.Notifier.Helpers;

public partial class CurrentConn : LoggedConnection, INotifyPropertyChanged
{
    //private string _currentAppPkgId;
    //TODO: rename as it's not something "current"
    public string CurrentAppPkgId { get; set; }// => this.GetOrSetAsyncValue(() => ProcessHelper.GetAppPkgId(Pid), NotifyPropertyChanged, nameof(_currentAppPkgId));

    //private string _currentLocalUserOwner;
    //TODO: rename as it's not something "current"
    public string CurrentLocalUserOwner { get; set; }// => this.GetOrSetAsyncValue(() => ProcessHelper.GetLocalUserOwner(Pid), NotifyPropertyChanged, nameof(_currentLocalUserOwner));

    public SortedSet<int> LocalPortArray { get; } = [];
    
    [ObservableProperty]
    private int _tentativesCounter = 1;
}
