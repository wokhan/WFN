using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

using Wokhan.ComponentModel.Extensions;
using Wokhan.WindowsFirewallNotifier.Common.Net.WFP;

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

        public bool PrivateIsOutBlockedNotif
        {
            get => _privateIsOutBlockedNotif;
            set
            {
                this.SetValue(ref _privateIsOutBlockedNotif, value, OnPropertyChanged);
                OnPropertyChanged(nameof(AllIsOutBlockedNotif));
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

        public bool PublicIsOutBlockedNotif
        {
            get => _publicIsOutBlockedNotif;
            set
            {
                this.SetValue(ref _publicIsOutBlockedNotif, value, OnPropertyChanged);
                OnPropertyChanged(nameof(AllIsOutBlockedNotif));
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

        public bool DomainIsOutBlockedNotif
        {
            get => _domainIsOutBlockedNotif;
            set
            {
                this.SetValue(ref _domainIsOutBlockedNotif, value, OnPropertyChanged);
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
                    PublicIsEnabled = PrivateIsEnabled = DomainIsEnabled = value.Value;
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
                PublicIsInBlocked = PrivateIsInBlocked = DomainIsInBlocked = value;
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
                PublicIsInAllowed = PrivateIsInAllowed = DomainIsInAllowed = value;
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
                PublicIsOutBlocked = PrivateIsOutBlocked = DomainIsOutBlocked = value;
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
                PublicIsOutAllowed = PrivateIsOutAllowed = DomainIsOutAllowed = value;
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
            PrivateIsOutAllowed = !PrivateIsOutBlocked && !PrivateIsOutBlockedNotif;

            PublicIsEnabled = publicInStatus != FirewallHelper.Status.DISABLED;
            PublicIsInBlocked = publicInStatus == FirewallHelper.Status.ENABLED_BLOCK;
            PublicIsInBlockedNotif = publicInStatus == FirewallHelper.Status.ENABLED_NOTIFY;
            PublicIsOutBlocked = publicOutStatus == FirewallHelper.Status.ENABLED_BLOCK;
            PublicIsOutAllowed = !PublicIsOutBlocked && !PublicIsOutBlockedNotif;

            DomainIsEnabled = domainInStatus != FirewallHelper.Status.DISABLED;
            DomainIsInBlocked = domainInStatus == FirewallHelper.Status.ENABLED_BLOCK;
            DomainIsInBlockedNotif = domainInStatus == FirewallHelper.Status.ENABLED_NOTIFY;
            DomainIsOutBlocked = domainOutStatus == FirewallHelper.Status.ENABLED_BLOCK;
            DomainIsOutAllowed = !DomainIsOutBlocked && !DomainIsOutBlockedNotif;
        }


        public void Save()
        {
            FirewallHelper.UpdatePrivatePolicy(PrivateIsEnabled, PrivateIsInBlockedNotif || PrivateIsInBlocked, PrivateIsOutBlockedNotif || PrivateIsOutBlocked, !PrivateIsInBlockedNotif);
            FirewallHelper.UpdatePublicPolicy(PublicIsEnabled, PublicIsInBlockedNotif || PublicIsInBlocked, PublicIsOutBlockedNotif || PublicIsOutBlocked, !PublicIsInBlockedNotif);
            FirewallHelper.UpdateDomainPolicy(DomainIsEnabled, DomainIsInBlockedNotif || DomainIsInBlocked, DomainIsOutBlockedNotif || DomainIsOutBlocked, !DomainIsInBlockedNotif);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
