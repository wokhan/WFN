using System;
using System.Windows.Media.Imaging;
using Wokhan.WindowsFirewallNotifier.Common.Helpers;
using System.Collections.Generic;
using Wokhan.Core.ComponentModel;
using Wokhan.ComponentModel.Extensions;
using Wokhan.WindowsFirewallNotifier.Common.IO.Files;
using Wokhan.WindowsFirewallNotifier.Common.Net.IP;
using Wokhan.WindowsFirewallNotifier.Common.Net.DNS;

namespace Wokhan.WindowsFirewallNotifier.Console.Helpers.ViewModels
{
    public class Connection : NotifierHelper
    {
        /// <summary>
        /// Uses a cache for WMI information to avoid per-process costly queries.
        /// Warning: it has to be reset to null every time a new batch of processes will be handled, since it's not dynamically self-refreshed.
        /// </summary>
        public static Dictionary<int, string[]> LocalOwnerWMICache = null;

        public Connection(IConnectionOwnerInfo ownerMod)
        {
            PID = ownerMod.OwningPid;
            IsNew = true;
            this._localPort = ownerMod.LocalPort.ToString();
            this.CreationTime = ownerMod.CreationTime;
            this._localAddress = ownerMod.LocalAddress;
            this._protocol = ownerMod.Protocol;
            this._remoteAddress = ownerMod.RemoteAddress;
            this._remotePort = (ownerMod.RemotePort == -1 ? String.Empty : ownerMod.RemotePort.ToString());
            this.LastSeen = DateTime.Now;
            this._state = Enum.GetName(typeof(ConnectionStatus), ownerMod.State);

            try
            {
                // Mainly for non-admin users, could use Process.GetProcessById for admins...
                var r = ProcessHelper.GetProcessOwnerWMI((int)ownerMod.OwningPid, ref LocalOwnerWMICache);
                Path = r[1] ?? "Unknown"; //FIXME: Move to resources!
                ProcName = r[0] ?? "Unknown"; //FIXME: Use something else?
            }
            catch
            {
                ProcName = "[Unknown or closed process]"; //FIXME: Move to resources!
                Path = "Unresolved"; //FIXME: Use something else?
            }

            if (ownerMod.OwnerModule is null)
            {
                if (PID == 0)
                {
                    ProcName = "System";
                    Owner = "System";
                    Path = "System";
                }
                else
                {
                    Owner = "Unknown";
                    Path = Path ?? "Unresolved";
                }
            }
            else
            {
                Owner = ownerMod.OwnerModule.ModuleName;
                IconPath = ownerMod.OwnerModule.ModulePath;
            }

            GroupKey = String.Format("{0} ({1}) - [{2}]", ProcName, Path, PID);
        }

        private bool _isAccessDenied;
        public bool IsAccessDenied
        {
            get => _isAccessDenied;
            set => this.SetValue(ref _isAccessDenied, value, NotifyPropertyChanged);
        }

        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set => this.SetValue(ref _isSelected, value, NotifyPropertyChanged);
        }

        private bool _isDead;
        public bool IsDead
        {
            get => _isDead;
            set => this.SetValue(ref _isDead, value, NotifyPropertyChanged);
        }

        private string _lastError;
        public string LastError
        {
            get => _lastError;
            set => this.SetValue(ref _lastError, value, NotifyPropertyChanged);
        }

        public string GroupKey { get; private set; }

        private BitmapSource _icon;
        public BitmapSource Icon
        {
            get
            {
                if (_icon is null) UpdateIcon();
                return _icon;
            }
            private set => this.SetValue(ref _icon, value, NotifyPropertyChanged);
        }

        private async void UpdateIcon()
        {
            Icon = await IconHelper.GetIconAsync(IconPath ?? Path).ConfigureAwait(false);
        }

        public uint PID { get; private set; }
        public string ProcName { get; private set; }
        public string Path { get; private set; }
        //public IEnumerable<FirewallHelper.Rule> FirewallRule { get { return FirewallHelper.GetMatchingRules(Path, Protocol, RemoteAddress, RemotePort, LocalPort, (Owner != ProcName ? new[] { Owner } : null), false).ToList(); } }

        internal void UpdateValues(IConnectionOwnerInfo b)
        {
            //lvi.LocalAddress = b.LocalAddress;
            //lvi.Protocol = b.Protocol;
            if (this.RemoteAddress != b.RemoteAddress)
            {
                DnsResolver.ResolveIpAddress(this._remoteAddress, entry => RemoteHostName = entry.DisplayText);
            }

            var newPort = (b.RemotePort == -1 ? String.Empty : b.RemotePort.ToString());
            if (this.RemotePort != newPort)
            {
                this.RemotePort = newPort;
            }

            var newState = Enum.GetName(typeof(ConnectionStatus), b.State);
            if (this.State != newState)
            {
                this.State = newState;
            }

            this.LastSeen = DateTime.Now;
        }

        private string _protocol;
        public string Protocol
        {
            get => _protocol;
            set => this.SetValue(ref _protocol, value, NotifyPropertyChanged);
        }

        private string _state;
        public string State
        {
            get => _state;
            set => this.SetValue(ref _state, value, NotifyPropertyChanged);
        }

        private string _localAddress;
        public string LocalAddress
        {
            get => _localAddress;
            set => this.SetValue(ref _localAddress, value, NotifyPropertyChanged);
        }

        private string _localHostName;
        public string LocalHostName
        {
            get
            {
                if (_localHostName is null)
                    DnsResolver.ResolveIpAddress(_localAddress, entry => LocalHostName = entry.DisplayText);
                return _localHostName;
            }
            set => this.SetValue(ref _localHostName, value, NotifyPropertyChanged);
        }


        private string _localPort;
        public string LocalPort
        {
            get => _localPort;
            set => this.SetValue(ref _localPort, value, NotifyPropertyChanged);
        }

        private string _remoteAddress;
        public string RemoteAddress
        {
            get => _remoteAddress;
            set => this.SetValue(ref _remoteAddress, value, NotifyPropertyChanged);
        }

        private string _remoteHostName;
        public string RemoteHostName
        {
            get
            {
                if (_remoteHostName is null)
                    DnsResolver.ResolveIpAddress(_remoteAddress, entry => RemoteHostName = entry.DisplayText);
                return _remoteHostName;
            }
            set => this.SetValue(ref _remoteHostName, value, NotifyPropertyChanged);
        }

        private string _remotePort;
        public string RemotePort
        {
            get { return _remotePort; }
            set { this.SetValue(ref _remotePort, value, NotifyPropertyChanged); }
        }

        public string Owner { get; private set; }
        public string IconPath { get; }
        public DateTime? CreationTime { get; set; }

        public DateTime LastSeen { get; set; }

        private bool _isDying;
        public bool IsDying
        {
            get => _isDying;
            set => this.SetValue(ref _isDying, value, NotifyPropertyChanged);
        }

        private bool _isNew;
        public bool IsNew
        {
            get => _isNew;
            set => this.SetValue(ref _isNew, value, NotifyPropertyChanged);
        }
    }
}
