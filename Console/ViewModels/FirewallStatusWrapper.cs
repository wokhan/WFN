using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

using Wokhan.ComponentModel.Extensions;
using Wokhan.WindowsFirewallNotifier.Common.Config;
using Wokhan.WindowsFirewallNotifier.Common.Net.WFP;
using Wokhan.WindowsFirewallNotifier.Console.Helpers;

namespace Wokhan.WindowsFirewallNotifier.Console.ViewModels
{
    public class FirewallStatusWrapper : INotifyPropertyChanged
    {
        public static Dictionary<bool, string> Actions { get; } = new Dictionary<bool, string>{
                { true, "Block" },
                { false, "Allow"}
            };


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

        private bool _privateIsEnabled;
        public bool PrivateIsEnabled
        {
            get => _privateIsEnabled;
            set
            {
                this.SetValue(ref _privateIsEnabled, value, OnPropertyChanged);
                OnPropertyChanged(nameof(AllIsEnabled));
            }
        }

        public bool PrivateIsInBlocked
        {
            get => _privateIsInBlocked;
            set
            {
                this.SetValue(ref _privateIsInBlocked, value, OnPropertyChanged);
                OnPropertyChanged(nameof(AllIsInBlocked));
            }
        }

        public bool PrivateIsOutBlocked
        {
            get => _privateIsOutBlocked;
            set
            {
                this.SetValue(ref _privateIsOutBlocked, value, OnPropertyChanged);
                OnPropertyChanged(nameof(AllIsOutBlocked));
                OnPropertyChanged(nameof(OneIsOutBlocked));
            }
        }

        public bool PrivateIsOutAllowed
        {
            get => _privateIsOutAllowed;
            set
            {
                this.SetValue(ref _privateIsOutAllowed, value, OnPropertyChanged);
                OnPropertyChanged(nameof(AllIsOutAllowed));
            }
        }

        public bool PrivateIsInAllowed
        {
            get => _privateIsInAllowed;
            set
            {
                this.SetValue(ref _privateIsInAllowed, value, OnPropertyChanged);
                OnPropertyChanged(nameof(AllIsInAllowed));
            }
        }

        public bool PrivateIsInBlockedNotif
        {
            get => _privateIsInBlockedNotif;
            set
            {
                this.SetValue(ref _privateIsInBlockedNotif, value, OnPropertyChanged);
                OnPropertyChanged(nameof(AllIsInBlockedNotif));
            }
        }

        public bool PublicIsEnabled
        {
            get => _publicIsEnabled;
            set
            {
                this.SetValue(ref _publicIsEnabled, value, OnPropertyChanged);
                OnPropertyChanged(nameof(AllIsEnabled));
            }
        }

        public bool PublicIsInBlocked
        {
            get => _publicIsInBlocked;
            set
            {
                this.SetValue(ref _publicIsInBlocked, value, OnPropertyChanged);
                OnPropertyChanged(nameof(AllIsInBlocked));
            }
        }

        public bool PublicIsOutBlocked
        {
            get => _publicIsOutBlocked;
            set
            {
                this.SetValue(ref _publicIsOutBlocked, value, OnPropertyChanged);
                OnPropertyChanged(nameof(AllIsOutBlocked));
                OnPropertyChanged(nameof(OneIsOutBlocked));
            }
        }

        public bool PublicIsOutAllowed
        {
            get => _publicIsOutAllowed;
            set
            {
                this.SetValue(ref _publicIsOutAllowed, value, OnPropertyChanged);
                OnPropertyChanged(nameof(AllIsOutAllowed));
            }
        }

        public bool PublicIsInAllowed
        {
            get => _publicIsInAllowed;
            set
            {
                this.SetValue(ref _publicIsInAllowed, value, OnPropertyChanged);
                OnPropertyChanged(nameof(AllIsInAllowed));
            }
        }

        public bool PublicIsInBlockedNotif
        {
            get => _publicIsInBlockedNotif;
            set
            {
                this.SetValue(ref _publicIsInBlockedNotif, value, OnPropertyChanged);
                OnPropertyChanged(nameof(AllIsInBlockedNotif));
            }
        }

        public bool DomainIsEnabled
        {
            get => _domainIsEnabled;
            set
            {
                this.SetValue(ref _domainIsEnabled, value, OnPropertyChanged);
                OnPropertyChanged(nameof(AllIsEnabled));
            }
        }

        public bool DomainIsInBlocked
        {
            get => _domainIsInBlocked;
            set
            {
                this.SetValue(ref _domainIsInBlocked, value, OnPropertyChanged);
                OnPropertyChanged(nameof(AllIsInBlocked));
            }
        }

        public bool DomainIsOutBlocked
        {
            get => _domainIsOutBlocked;
            set
            {
                this.SetValue(ref _domainIsOutBlocked, value, OnPropertyChanged);
                OnPropertyChanged(nameof(AllIsOutBlocked));
                OnPropertyChanged(nameof(OneIsOutBlocked));
            }
        }

        public bool DomainIsOutAllowed
        {
            get => _domainIsOutAllowed;
            set
            {
                this.SetValue(ref _domainIsOutAllowed, value, OnPropertyChanged);
                OnPropertyChanged(nameof(AllIsOutAllowed));
            }
        }

        public bool DomainIsInAllowed
        {
            get => _domainIsInAllowed;
            set
            {
                this.SetValue(ref _domainIsInAllowed, value, OnPropertyChanged);
                OnPropertyChanged(nameof(AllIsInAllowed));
            }
        }

        public bool DomainIsInBlockedNotif
        {
            get => _domainIsInBlockedNotif;
            set
            {
                this.SetValue(ref _domainIsInBlockedNotif, value, OnPropertyChanged);
                OnPropertyChanged(nameof(AllIsInBlockedNotif));
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
                    PublicIsEnabled = PrivateIsEnabled = DomainIsEnabled = value.Value;
                }
            }
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

        public bool OneIsOutBlocked
        {
            get => PrivateIsOutBlocked || PublicIsOutBlocked || DomainIsOutBlocked;
        }

        public bool CurrentProfileIsPublic => FirewallHelper.IsCurrentProfilePublic();
        public bool CurrentProfileIsPrivate => FirewallHelper.IsCurrentProfilePrivate();
        public bool CurrentProfileIsDomain => FirewallHelper.IsCurrentProfileDomain();

        public FirewallStatusWrapper()
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
            FirewallHelper.UpdatePrivatePolicy(PrivateIsEnabled, PrivateIsInBlockedNotif || PrivateIsInBlocked, PrivateIsOutBlocked, !PrivateIsInBlockedNotif);
            FirewallHelper.UpdatePublicPolicy(PublicIsEnabled, PublicIsInBlockedNotif || PublicIsInBlocked, PublicIsOutBlocked, !PublicIsInBlockedNotif);
            FirewallHelper.UpdateDomainPolicy(DomainIsEnabled, DomainIsInBlockedNotif || DomainIsInBlocked, DomainIsOutBlocked, !DomainIsInBlockedNotif);

            // Checking if Notifications are to be enabled or not
            if (Settings.Default.StartNotifierAfterLogin && (!PrivateIsEnabled || !PublicIsEnabled || !DomainIsEnabled))
            {
                if (!InstallHelper.IsInstalled())
                {
                    InstallHelper.Install(checkResult);
                }
                else
                {
                    InstallHelper.InstallCheck(checkResult);
                }
            }
            else
            {
                InstallHelper.Uninstall(checkResult);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
