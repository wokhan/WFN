using System;
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
        private bool _privateIsInBlocked;
        private bool _privateIsOutBlocked;
        private bool _privateIsOutAllowed;
        private bool _privateIsInAllowed;
        private bool _privateIsInBlockedNotif;
        private bool _publicIsEnabled;
        private bool _publicIsInBlocked;
        private bool _publicIsOutBlocked;
        private bool _publicIsOutAllowed;
        private bool _publicIsInAllowed;
        private bool _publicIsInBlockedNotif;
        private bool _domainIsEnabled;
        private bool _domainIsInBlocked;
        private bool _domainIsOutBlocked;
        private bool _domainIsOutAllowed;
        private bool _domainIsInAllowed;
        private bool _domainIsInBlockedNotif;

        private bool _privateIsEnabled;
        public bool PrivateIsEnabled
        {
            get => _privateIsEnabled;
            set => this.SetValue(ref _privateIsEnabled, value, OnSettingChanged);
        }

        public bool PrivateIsInBlocked
        {
            get => _privateIsInBlocked;
            set => this.SetValue(ref _privateIsInBlocked, value, OnSettingChanged);
        }

        public bool PrivateIsOutBlocked
        {
            get => _privateIsOutBlocked;
            set => this.SetValue(ref _privateIsOutBlocked, value, OnSettingChanged);
        }

        public bool PrivateIsOutAllowed
        {
            get => _privateIsOutAllowed;
            set => this.SetValue(ref _privateIsOutAllowed, value, OnSettingChanged);
        }

        public bool PrivateIsInAllowed
        {
            get => _privateIsInAllowed;
            set => this.SetValue(ref _privateIsInAllowed, value, OnSettingChanged);
        }

        public bool PrivateIsInBlockedNotif
        {
            get => _privateIsInBlockedNotif;
            set => this.SetValue(ref _privateIsInBlockedNotif, value, OnSettingChanged);
        }

        public bool PublicIsEnabled
        {
            get => _publicIsEnabled;
            set => this.SetValue(ref _publicIsEnabled, value, OnSettingChanged);
        }

        public bool PublicIsInBlocked
        {
            get => _publicIsInBlocked;
            set => this.SetValue(ref _publicIsInBlocked, value, OnSettingChanged);
        }

        public bool PublicIsOutBlocked
        {
            get => _publicIsOutBlocked;
            set => this.SetValue(ref _publicIsOutBlocked, value, OnSettingChanged);
        }

        public bool PublicIsOutAllowed
        {
            get => _publicIsOutAllowed;
            set => this.SetValue(ref _publicIsOutAllowed, value, OnSettingChanged);
        }

        public bool PublicIsInAllowed
        {
            get => _publicIsInAllowed;
            set => this.SetValue(ref _publicIsInAllowed, value, OnSettingChanged);
        }

        public bool PublicIsInBlockedNotif
        {
            get => _publicIsInBlockedNotif;
            set => this.SetValue(ref _publicIsInBlockedNotif, value, OnSettingChanged);
        }

        public bool DomainIsEnabled
        {
            get => _domainIsEnabled;
            set => this.SetValue(ref _domainIsEnabled, value, OnSettingChanged);
        }

        public bool DomainIsInBlocked
        {
            get => _domainIsInBlocked;
            set => this.SetValue(ref _domainIsInBlocked, value, OnSettingChanged);
        }

        public bool DomainIsOutBlocked
        {
            get => _domainIsOutBlocked;
            set => this.SetValue(ref _domainIsOutBlocked, value, OnSettingChanged);
        }

        public bool DomainIsOutAllowed
        {
            get => _domainIsOutAllowed;
            set => this.SetValue(ref _domainIsOutAllowed, value, OnSettingChanged);
        }

        public bool DomainIsInAllowed
        {
            get => _domainIsInAllowed;
            set => this.SetValue(ref _domainIsInAllowed, value, OnSettingChanged);
        }

        public bool DomainIsInBlockedNotif
        {
            get => _domainIsInBlockedNotif;
            set => this.SetValue(ref _domainIsInBlockedNotif, value, OnSettingChanged);
        }

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
        }

        private bool UpdateFirewallPolicies()
        {
            FirewallHelper.UpdatePrivatePolicy(PrivateIsEnabled, PrivateIsInBlockedNotif || PrivateIsInBlocked, PrivateIsOutBlocked, !PrivateIsInBlockedNotif);
            FirewallHelper.UpdatePublicPolicy(PublicIsEnabled, PublicIsInBlockedNotif || PublicIsInBlocked, PublicIsOutBlocked, !PublicIsInBlockedNotif);
            FirewallHelper.UpdateDomainPolicy(DomainIsEnabled, DomainIsInBlockedNotif || DomainIsInBlocked, DomainIsOutBlocked, !DomainIsInBlockedNotif);

            return true;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnSettingChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AllIsEnabled)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AllIsInAllowed)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AllIsInBlocked)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AllIsInBlockedNotif)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AllIsOutAllowed)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AllIsOutBlocked)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(OneIsOutBlocked)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
