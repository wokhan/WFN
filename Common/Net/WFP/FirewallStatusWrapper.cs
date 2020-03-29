using NetFwTypeLib;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Wokhan.WindowsFirewallNotifier.Common.Net.WFP
{
    public static partial class FirewallHelper
    {
        public class FirewallStatusWrapper : INotifyPropertyChanged
        {
            private static Dictionary<bool, string> _actions = new Dictionary<bool, string>{
                { true, "Block" },
                { false, "Allow"}
            };

            public static Dictionary<bool, string> Actions { get { return _actions; } }

            private enum Status
            {
                DISABLED,
                ENABLED_ALLOW,
                ENABLED_BLOCK,
                ENABLED_NOTIFY
            }

            private Status privateInStatus = Status.DISABLED;
            private Status domainInStatus = Status.DISABLED;
            private Status publicInStatus = Status.DISABLED;

            private Status privateOutStatus = Status.DISABLED;
            private Status domainOutStatus = Status.DISABLED;
            private Status publicOutStatus = Status.DISABLED;
            private bool _privateIsEnabled;
            private bool _privateIsInBlocked;
            private bool _privateIsOutBlocked;
            private bool _privateIsOutAllowed;
            private bool _privateIsInAllowed;
            private bool _privateIsInBlockedNotif;
            private bool _privateIsOutBlockedNotif;
            private bool _publicIsEnabled;
            private bool _publicIsInBlocked;
            private bool _publicIsOutBlocked;
            private bool _publicIsOutAllowed;
            private bool _publicIsInAllowed;
            private bool _publicIsInBlockedNotif;
            private bool _publicIsOutBlockedNotif;
            private bool _domainIsEnabled;
            private bool _domainIsInBlocked;
            private bool _domainIsOutBlocked;
            private bool _domainIsOutAllowed;
            private bool _domainIsInAllowed;
            private bool _domainIsInBlockedNotif;
            private bool _domainIsOutBlockedNotif;

            public bool PrivateIsEnabled
            {
                get => _privateIsEnabled;
                set
                {
                    if (value == _privateIsEnabled) return;
                    _privateIsEnabled = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(AllIsEnabled));
                }
            }

            public bool PrivateIsInBlocked
            {
                get => _privateIsInBlocked;
                set
                {
                    if (value == _privateIsInBlocked) return;
                    _privateIsInBlocked = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(AllIsInBlocked));
                }
            }

            public bool PrivateIsOutBlocked
            {
                get => _privateIsOutBlocked;
                set
                {
                    if (value == _privateIsOutBlocked) return;
                    _privateIsOutBlocked = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(AllIsOutBlocked));
                }
            }

            public bool PrivateIsOutAllowed
            {
                get => _privateIsOutAllowed;
                set
                {
                    if (value == _privateIsOutAllowed) return;
                    _privateIsOutAllowed = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(AllIsOutAllowed));
                }
            }

            public bool PrivateIsInAllowed
            {
                get => _privateIsInAllowed;
                set
                {
                    if (value == _privateIsInAllowed) return;
                    _privateIsInAllowed = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(AllIsInAllowed));
                }
            }

            public bool PrivateIsInBlockedNotif
            {
                get => _privateIsInBlockedNotif;
                set
                {
                    if (value == _privateIsInBlockedNotif) return;
                    _privateIsInBlockedNotif = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(AllIsInBlockedNotif));
                }
            }

            public bool PrivateIsOutBlockedNotif
            {
                get => _privateIsOutBlockedNotif;
                set
                {
                    if (value == _privateIsOutBlockedNotif) return;
                    _privateIsOutBlockedNotif = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(AllIsOutBlockedNotif));
                }
            }

            public bool PublicIsEnabled
            {
                get => _publicIsEnabled;
                set
                {
                    if (value == _publicIsEnabled) return;
                    _publicIsEnabled = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(AllIsEnabled));
                }
            }

            public bool PublicIsInBlocked
            {
                get => _publicIsInBlocked;
                set
                {
                    if (value == _publicIsInBlocked) return;
                    _publicIsInBlocked = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(AllIsInBlocked));
                }
            }

            public bool PublicIsOutBlocked
            {
                get => _publicIsOutBlocked;
                set
                {
                    if (value == _publicIsOutBlocked) return;
                    _publicIsOutBlocked = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(AllIsOutBlocked));
                }
            }

            public bool PublicIsOutAllowed
            {
                get => _publicIsOutAllowed;
                set
                {
                    if (value == _publicIsOutAllowed) return;
                    _publicIsOutAllowed = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(AllIsOutAllowed));
                }
            }

            public bool PublicIsInAllowed
            {
                get => _publicIsInAllowed;
                set
                {
                    if (value == _publicIsInAllowed) return;
                    _publicIsInAllowed = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(AllIsInAllowed));
                }
            }

            public bool PublicIsInBlockedNotif
            {
                get => _publicIsInBlockedNotif;
                set
                {
                    if (value == _publicIsInBlockedNotif) return;
                    _publicIsInBlockedNotif = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(AllIsInBlockedNotif));
                }
            }

            public bool PublicIsOutBlockedNotif
            {
                get => _publicIsOutBlockedNotif;
                set
                {
                    if (value == _publicIsOutBlockedNotif) return;
                    _publicIsOutBlockedNotif = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(AllIsOutBlockedNotif));
                }
            }

            public bool DomainIsEnabled
            {
                get => _domainIsEnabled;
                set
                {
                    if (value == _domainIsEnabled) return;
                    _domainIsEnabled = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(AllIsEnabled));
                }
            }

            public bool DomainIsInBlocked
            {
                get => _domainIsInBlocked;
                set
                {
                    if (value == _domainIsInBlocked) return;
                    _domainIsInBlocked = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(AllIsInBlocked));
                }
            }

            public bool DomainIsOutBlocked
            {
                get => _domainIsOutBlocked;
                set
                {
                    if (value == _domainIsOutBlocked) return;
                    _domainIsOutBlocked = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(AllIsOutBlocked));
                }
            }

            public bool DomainIsOutAllowed
            {
                get => _domainIsOutAllowed;
                set
                {
                    if (value == _domainIsOutAllowed) return;
                    _domainIsOutAllowed = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(AllIsOutAllowed));
                }
            }

            public bool DomainIsInAllowed
            {
                get => _domainIsInAllowed;
                set
                {
                    if (value == _domainIsInAllowed) return;
                    _domainIsInAllowed = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(AllIsInAllowed));
                }
            }

            public bool DomainIsInBlockedNotif
            {
                get => _domainIsInBlockedNotif;
                set
                {
                    if (value == _domainIsInBlockedNotif) return;
                    _domainIsInBlockedNotif = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(AllIsInBlockedNotif));
                }
            }

            public bool DomainIsOutBlockedNotif
            {
                get => _domainIsOutBlockedNotif;
                set
                {
                    if (value == _domainIsOutBlockedNotif) return;
                    _domainIsOutBlockedNotif = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(AllIsOutBlockedNotif));
                }
            }

            public bool? AllIsEnabled
            {
                get
                {
                    if (PublicIsEnabled == PrivateIsEnabled && PrivateIsEnabled == DomainIsEnabled)
                    {
                        return PublicIsEnabled;
                    }

                    return null;
                }
                set
                {
                    if (value != null)
                    {
                        var bValue = value.Value;
                        PublicIsEnabled = bValue;
                        PrivateIsEnabled = bValue;
                        DomainIsEnabled = bValue;
                        OnPropertyChanged(nameof(PublicIsEnabled));
                        OnPropertyChanged(nameof(PrivateIsEnabled));
                        OnPropertyChanged(nameof(DomainIsEnabled));
                    }
                }
            }

            public bool AllIsInBlocked
            {
                get => PublicIsInBlocked && PrivateIsInBlocked && DomainIsInBlocked;
                set
                {
                    PublicIsInBlocked = value;
                    PrivateIsInBlocked = value;
                    DomainIsInBlocked = value;
                    OnPropertyChanged(nameof(PublicIsInBlocked));
                    OnPropertyChanged(nameof(PrivateIsInBlocked));
                    OnPropertyChanged(nameof(DomainIsInBlocked));
                }
            }

            public bool AllIsInAllowed
            {
                get => PublicIsInAllowed && PrivateIsInAllowed && DomainIsInAllowed;
                set
                {
                    PublicIsInAllowed = value;
                    PrivateIsInAllowed = value;
                    DomainIsInAllowed = value;
                    OnPropertyChanged(nameof(PublicIsInAllowed));
                    OnPropertyChanged(nameof(PrivateIsInAllowed));
                    OnPropertyChanged(nameof(DomainIsInAllowed));
                }
            }

            public bool AllIsOutBlocked
            {
                get => PublicIsOutBlocked && PrivateIsOutBlocked && DomainIsOutBlocked;
                set
                {
                    PublicIsOutBlocked = value;
                    PrivateIsOutBlocked = value;
                    DomainIsOutBlocked = value;
                    OnPropertyChanged(nameof(PublicIsOutBlocked));
                    OnPropertyChanged(nameof(PrivateIsOutBlocked));
                    OnPropertyChanged(nameof(DomainIsOutBlocked));
                }
            }

            public bool AllIsOutAllowed
            {
                get => PublicIsOutAllowed && PrivateIsOutAllowed && DomainIsOutAllowed;
                set
                {
                    PublicIsOutAllowed = value;
                    PrivateIsOutAllowed = value;
                    DomainIsOutAllowed = value;
                    OnPropertyChanged(nameof(PublicIsOutAllowed));
                    OnPropertyChanged(nameof(PrivateIsOutAllowed));
                    OnPropertyChanged(nameof(DomainIsOutAllowed));
                }
            }

            public bool AllIsInBlockedNotif
            {
                get => PublicIsInBlockedNotif && PrivateIsInBlockedNotif && DomainIsInBlockedNotif;
                set
                {
                    PublicIsInBlockedNotif = value;
                    PrivateIsInBlockedNotif = value;
                    DomainIsInBlockedNotif = value;
                    OnPropertyChanged(nameof(PublicIsInBlockedNotif));
                    OnPropertyChanged(nameof(PrivateIsInBlockedNotif));
                    OnPropertyChanged(nameof(DomainIsInBlockedNotif));
                }
            }

            public bool AllIsOutBlockedNotif
            {
                get => PublicIsOutBlockedNotif && PrivateIsOutBlockedNotif && DomainIsOutBlockedNotif;
                set
                {
                    PublicIsOutBlockedNotif = value;
                    PrivateIsOutBlockedNotif = value;
                    DomainIsOutBlockedNotif = value;
                    OnPropertyChanged(nameof(PublicIsOutBlockedNotif));
                    OnPropertyChanged(nameof(PrivateIsOutBlockedNotif));
                    OnPropertyChanged(nameof(DomainIsOutBlockedNotif));
                }
            }

            public string CurrentProfile => GetCurrentProfileAsText();

            public FirewallStatusWrapper()
            {
                UpdateStatus(NET_FW_PROFILE_TYPE2_.NET_FW_PROFILE2_PRIVATE, ref privateInStatus, ref privateOutStatus);
                UpdateStatus(NET_FW_PROFILE_TYPE2_.NET_FW_PROFILE2_PUBLIC, ref publicInStatus, ref publicOutStatus);
                UpdateStatus(NET_FW_PROFILE_TYPE2_.NET_FW_PROFILE2_DOMAIN, ref domainInStatus, ref domainOutStatus);

                PrivateIsEnabled = privateInStatus != Status.DISABLED;
                PrivateIsInBlocked = privateInStatus == Status.ENABLED_BLOCK;
                PrivateIsInBlockedNotif = privateInStatus == Status.ENABLED_NOTIFY;
                PrivateIsOutBlocked = privateOutStatus == Status.ENABLED_BLOCK;
                PrivateIsOutAllowed = !PrivateIsOutBlocked && !PrivateIsOutBlockedNotif;

                PublicIsEnabled = publicInStatus != Status.DISABLED;
                PublicIsInBlocked = publicInStatus == Status.ENABLED_BLOCK;
                PublicIsInBlockedNotif = publicInStatus == Status.ENABLED_NOTIFY;
                PublicIsOutBlocked = publicOutStatus == Status.ENABLED_BLOCK;
                PublicIsOutAllowed = !PublicIsOutBlocked && !PublicIsOutBlockedNotif;

                DomainIsEnabled = domainInStatus != Status.DISABLED;
                DomainIsInBlocked = domainInStatus == Status.ENABLED_BLOCK;
                DomainIsInBlockedNotif = domainInStatus == Status.ENABLED_NOTIFY;
                DomainIsOutBlocked = domainOutStatus == Status.ENABLED_BLOCK;
                DomainIsOutAllowed = !DomainIsOutBlocked && !DomainIsOutBlockedNotif;
            }

            private void UpdateStatus(NET_FW_PROFILE_TYPE2_ profile, ref Status stat, ref Status statOut)
            {
                if (firewallPolicy.FirewallEnabled[profile])
                {
                    if (firewallPolicy.DefaultInboundAction[profile] == NET_FW_ACTION_.NET_FW_ACTION_BLOCK)
                    {
                        if (firewallPolicy.NotificationsDisabled[profile])
                        {
                            stat = Status.ENABLED_BLOCK;
                        }
                        else
                        {
                            stat = Status.ENABLED_NOTIFY;
                        }
                    }
                    else
                    {
                        stat = Status.ENABLED_ALLOW;
                    }

                    if (firewallPolicy.DefaultOutboundAction[profile] == NET_FW_ACTION_.NET_FW_ACTION_BLOCK)
                    {
                        statOut = Status.ENABLED_BLOCK;
                    }
                    else
                    {
                        statOut = Status.ENABLED_ALLOW;
                    }
                }
            }

            public void Save()
            {
                firewallPolicy.FirewallEnabled[NET_FW_PROFILE_TYPE2_.NET_FW_PROFILE2_PRIVATE] = PrivateIsEnabled;
                firewallPolicy.DefaultInboundAction[NET_FW_PROFILE_TYPE2_.NET_FW_PROFILE2_PRIVATE] = PrivateIsInBlockedNotif || PrivateIsInBlocked ? NET_FW_ACTION_.NET_FW_ACTION_BLOCK : NET_FW_ACTION_.NET_FW_ACTION_ALLOW;
                firewallPolicy.DefaultOutboundAction[NET_FW_PROFILE_TYPE2_.NET_FW_PROFILE2_PRIVATE] = PrivateIsOutBlockedNotif || PrivateIsOutBlocked ? NET_FW_ACTION_.NET_FW_ACTION_BLOCK : NET_FW_ACTION_.NET_FW_ACTION_ALLOW;
                firewallPolicy.NotificationsDisabled[NET_FW_PROFILE_TYPE2_.NET_FW_PROFILE2_PRIVATE] = !PrivateIsInBlockedNotif;

                firewallPolicy.FirewallEnabled[NET_FW_PROFILE_TYPE2_.NET_FW_PROFILE2_PUBLIC] = PublicIsEnabled;
                firewallPolicy.DefaultInboundAction[NET_FW_PROFILE_TYPE2_.NET_FW_PROFILE2_PUBLIC] = PublicIsInBlockedNotif || PublicIsInBlocked ? NET_FW_ACTION_.NET_FW_ACTION_BLOCK : NET_FW_ACTION_.NET_FW_ACTION_ALLOW;
                firewallPolicy.DefaultOutboundAction[NET_FW_PROFILE_TYPE2_.NET_FW_PROFILE2_PUBLIC] = PublicIsOutBlockedNotif || PublicIsOutBlocked ? NET_FW_ACTION_.NET_FW_ACTION_BLOCK : NET_FW_ACTION_.NET_FW_ACTION_ALLOW;
                firewallPolicy.NotificationsDisabled[NET_FW_PROFILE_TYPE2_.NET_FW_PROFILE2_PUBLIC] = !PublicIsInBlockedNotif;

                firewallPolicy.FirewallEnabled[NET_FW_PROFILE_TYPE2_.NET_FW_PROFILE2_DOMAIN] = DomainIsEnabled;
                firewallPolicy.DefaultInboundAction[NET_FW_PROFILE_TYPE2_.NET_FW_PROFILE2_DOMAIN] = DomainIsInBlockedNotif || DomainIsInBlocked ? NET_FW_ACTION_.NET_FW_ACTION_BLOCK : NET_FW_ACTION_.NET_FW_ACTION_ALLOW;
                firewallPolicy.DefaultOutboundAction[NET_FW_PROFILE_TYPE2_.NET_FW_PROFILE2_DOMAIN] = DomainIsOutBlockedNotif || DomainIsOutBlocked ? NET_FW_ACTION_.NET_FW_ACTION_BLOCK : NET_FW_ACTION_.NET_FW_ACTION_ALLOW;
                firewallPolicy.NotificationsDisabled[NET_FW_PROFILE_TYPE2_.NET_FW_PROFILE2_DOMAIN] = !DomainIsInBlockedNotif;
            }

            public event PropertyChangedEventHandler? PropertyChanged;

            protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}