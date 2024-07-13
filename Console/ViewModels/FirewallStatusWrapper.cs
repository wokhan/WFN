using CommunityToolkit.Mvvm.ComponentModel;

using System.Diagnostics.CodeAnalysis;

using Wokhan.WindowsFirewallNotifier.Common.Net.WFP;
using Wokhan.WindowsFirewallNotifier.Console.Helpers;

namespace Wokhan.WindowsFirewallNotifier.Console.ViewModels;

public partial class FirewallStatusWrapper : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AllIsEnabled))]
    private bool _privateIsEnabled;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AllIsInBlocked))]
    private bool _privateIsInBlocked;
    
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AllIsOutBlocked), nameof(OneIsOutBlocked))] 
    private bool _privateIsOutBlocked;
    
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AllIsOutAllowed), nameof(OneIsOutBlocked))]
    private bool _privateIsOutAllowed;
    
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AllIsInAllowed))] 
    private bool _privateIsInAllowed;
    
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AllIsInBlockedNotif))] 
    private bool _privateIsInBlockedNotif;
    


    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AllIsEnabled))]
    private bool _publicIsEnabled;
    
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AllIsInBlocked))]
    private bool _publicIsInBlocked;
    
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AllIsOutBlocked), nameof(OneIsOutBlocked))]
    private bool _publicIsOutBlocked;
    
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AllIsOutAllowed), nameof(OneIsOutBlocked))]
    private bool _publicIsOutAllowed;
    
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AllIsInAllowed))]
    private bool _publicIsInAllowed;
    
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AllIsInBlockedNotif))]
    private bool _publicIsInBlockedNotif;
    
    
    
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AllIsEnabled))]
    private bool _domainIsEnabled;
    
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AllIsInBlocked))]
    private bool _domainIsInBlocked;
    
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AllIsOutBlocked), nameof(OneIsOutBlocked))]
    private bool _domainIsOutBlocked;
    
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AllIsOutAllowed), nameof(OneIsOutBlocked))]
    private bool _domainIsOutAllowed;
    
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AllIsInAllowed))]
    private bool _domainIsInAllowed;
    
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AllIsInBlockedNotif))]
    private bool _domainIsInBlockedNotif;

    
    public bool AllIsEnabled
    {
        get => PublicIsEnabled && PrivateIsEnabled && DomainIsEnabled;
        set => PublicIsEnabled = PrivateIsEnabled = DomainIsEnabled = value;
    }

    public bool AllIsInBlocked
    {
        get => PublicIsInBlocked && PrivateIsInBlocked && DomainIsInBlocked;
        set => PublicIsInBlocked = PrivateIsInBlocked = DomainIsInBlocked = value;
    }

    public bool AllIsInAllowed
    {
        get => PublicIsInAllowed && PrivateIsInAllowed && DomainIsInAllowed;
        set => PublicIsInAllowed = PrivateIsInAllowed = DomainIsInAllowed = value;
    }

    public bool AllIsOutBlocked
    {
        get => PublicIsOutBlocked && PrivateIsOutBlocked && DomainIsOutBlocked;
        set => PublicIsOutBlocked = PrivateIsOutBlocked = DomainIsOutBlocked = value;
    }

    public bool AllIsOutAllowed
    {
        get => PublicIsOutAllowed && PrivateIsOutAllowed && DomainIsOutAllowed;
        set => PublicIsOutAllowed = PrivateIsOutAllowed = DomainIsOutAllowed = value;
    }

    public bool AllIsInBlockedNotif
    {
        get => PublicIsInBlockedNotif && PrivateIsInBlockedNotif && DomainIsInBlockedNotif;
        set => PublicIsInBlockedNotif = PrivateIsInBlockedNotif = DomainIsInBlockedNotif = value;
    }

    public bool OneIsOutBlocked => PrivateIsOutBlocked || PublicIsOutBlocked || DomainIsOutBlocked;
    public bool CurrentProfileIsPublic => FirewallHelper.IsCurrentProfilePublic();
    public bool CurrentProfileIsPrivate => FirewallHelper.IsCurrentProfilePrivate();
    public bool CurrentProfileIsDomain => FirewallHelper.IsCurrentProfileDomain();

    public FirewallStatusWrapper()
    {
        Init();
    }

    public void Init()
    {
        FirewallHelper.UpdatePrivateStatus(out FirewallHelper.Status privateInStatus, out FirewallHelper.Status privateOutStatus);
        FirewallHelper.UpdatePublicStatus(out FirewallHelper.Status publicInStatus, out FirewallHelper.Status publicOutStatus);
        FirewallHelper.UpdateDomainStatus(out FirewallHelper.Status domainInStatus, out FirewallHelper.Status domainOutStatus);

        PrivateIsEnabled = privateInStatus != FirewallHelper.Status.DISABLED;
        PrivateIsInBlocked = privateInStatus == FirewallHelper.Status.ENABLED_BLOCK;
        PrivateIsInBlockedNotif = privateInStatus == FirewallHelper.Status.ENABLED_NOTIFY;
        PrivateIsOutBlocked = privateOutStatus == FirewallHelper.Status.ENABLED_BLOCK;
        PrivateIsOutAllowed = !PrivateIsOutBlocked;

        PublicIsEnabled = publicInStatus != FirewallHelper.Status.DISABLED;
        PublicIsInBlocked = publicInStatus == FirewallHelper.Status.ENABLED_BLOCK;
        PublicIsInBlockedNotif = publicInStatus == FirewallHelper.Status.ENABLED_NOTIFY;
        PublicIsOutBlocked = publicOutStatus == FirewallHelper.Status.ENABLED_BLOCK;
        PublicIsOutAllowed = !PublicIsOutBlocked;

        DomainIsEnabled = domainInStatus != FirewallHelper.Status.DISABLED;
        DomainIsInBlocked = domainInStatus == FirewallHelper.Status.ENABLED_BLOCK;
        DomainIsInBlockedNotif = domainInStatus == FirewallHelper.Status.ENABLED_NOTIFY;
        DomainIsOutBlocked = domainOutStatus == FirewallHelper.Status.ENABLED_BLOCK;
        DomainIsOutAllowed = !DomainIsOutBlocked;
    }

    public void Save([param: NotNull] Func<Func<bool>, string, string, bool> checkResult)
    {
        if (!checkResult(UpdateFirewallPolicies, "Successfully applied the firewall settings", "Failed to apply the firewall settings!"))
        {
            return;
        }

        if ((PrivateIsEnabled && PrivateIsOutBlocked) || (PublicIsEnabled && PublicIsOutBlocked) || (DomainIsEnabled && DomainIsOutBlocked))
        {
            InstallHelper.Install(checkResult);
        }
        else
        {
            InstallHelper.Uninstall(checkResult);
        }

        Init();
    }

    private bool UpdateFirewallPolicies()
    {
        FirewallHelper.UpdatePrivatePolicy(PrivateIsEnabled, PrivateIsInBlockedNotif || PrivateIsInBlocked, PrivateIsOutBlocked, !PrivateIsInBlockedNotif);
        FirewallHelper.UpdatePublicPolicy(PublicIsEnabled, PublicIsInBlockedNotif || PublicIsInBlocked, PublicIsOutBlocked, !PublicIsInBlockedNotif);
        FirewallHelper.UpdateDomainPolicy(DomainIsEnabled, DomainIsInBlockedNotif || DomainIsInBlocked, DomainIsOutBlocked, !DomainIsInBlockedNotif);

        return true;
    }
}
