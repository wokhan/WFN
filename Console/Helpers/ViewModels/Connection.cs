using System;
using System.Windows.Media.Imaging;
using Wokhan.WindowsFirewallNotifier.Common.Helpers;
using System.Collections.Generic;
using Wokhan.Core.ComponentModel;
using Wokhan.ComponentModel.Extensions;
using Wokhan.WindowsFirewallNotifier.Common.IO.Files;
using Wokhan.WindowsFirewallNotifier.Common.Net.IP;
using Wokhan.WindowsFirewallNotifier.Common.Net.DNS;
using System.Windows.Media;

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
            this.rawConnection = ownerMod;

            PID = ownerMod.OwningPid;
            IsNew = true;
            this._localPort = ownerMod.LocalPort.ToString();
            this.CreationTime = ownerMod.CreationTime;
            this._localAddress = ownerMod.LocalAddress;
            this._protocol = ownerMod.Protocol;
            this._remoteAddress = ownerMod.RemoteAddress;
            this._remotePort = (ownerMod.RemotePort == -1 ? String.Empty : ownerMod.RemotePort.ToString());
            this.LastSeen = DateTime.Now;
            //this._state = Enum.GetName(typeof(ConnectionStatus), ownerMod.State);

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

            GroupKey = $"{ProcName} ({Path}) - [{PID}]";
        }

        private bool TryEnableStats()
        {
            try
            {
                if (this.rawConnection is MIB_TCPROW_OWNER_MODULE)
                {
                    rawrow = ((MIB_TCPROW_OWNER_MODULE)this.rawConnection).ToTCPRow();
                    TCPHelper.EnsureStatsAreEnabled((TCPHelper.MIB_TCPROW)rawrow);
                }
                else if (this.rawConnection is MIB_TCP6ROW_OWNER_MODULE)
                {
                    rawrow = ((MIB_TCP6ROW_OWNER_MODULE)this.rawConnection).ToTCPRow();
                    TCP6Helper.EnsureStatsAreEnabled((TCP6Helper.MIB_TCP6ROW)rawrow);
                }

                statsEnabled = true;
            }
            catch
            {
                IsAccessDenied = true;
            }

            return false;
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

        private readonly IConnectionOwnerInfo rawConnection;
        private object rawrow;

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
                _ = DnsResolver.ResolveIpAddress(this._remoteAddress, entry => RemoteHostName = entry.DisplayText);
            }

            RemotePort = (b.RemotePort == -1 ? String.Empty : b.RemotePort.ToString());
            State = Enum.GetName(typeof(ConnectionStatus), b.State);
            if (b.State == ConnectionStatus.ESTABLISHED)
            {
                if (!statsEnabled)
                {
                    TryEnableStats();
                }
                EstimateBandwidth();
            }

            LastSeen = DateTime.Now;
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
                {
                    _ = DnsResolver.ResolveIpAddress(_localAddress, entry => LocalHostName = entry.DisplayText);
                }
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
                {
                    _ = DnsResolver.ResolveIpAddress(_remoteAddress, entry => RemoteHostName = entry.DisplayText);
                }
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

        private Brush _brush = Brushes.Black;
        public Brush Brush
        {
            get => _brush;
            set => this.SetValue(ref _brush, value, NotifyPropertyChanged);
        }

        private double _inboundBandwidth;
        public double InboundBandwidth { get => _inboundBandwidth; private set => this.SetValue(ref _inboundBandwidth, value, NotifyPropertyChanged); }

        private double _outboundBandwidth;
        public double OutboundBandwidth { get => _outboundBandwidth; private set => this.SetValue(ref _outboundBandwidth, value, NotifyPropertyChanged); }

        private bool statsEnabled;
        private void EstimateBandwidth()
        {
            if (!statsEnabled)
                return;

            if (rawrow != null && !IsAccessDenied)
            {
                var bandwidth = (rawrow is TCPHelper.MIB_TCPROW ? TCPHelper.GetTCPBandwidth((TCPHelper.MIB_TCPROW)rawrow) : TCP6Helper.GetTCPBandwidth((TCP6Helper.MIB_TCP6ROW)rawrow));
                InboundBandwidth = bandwidth.InboundBandwidth;
                OutboundBandwidth = bandwidth.OutboundBandwidth;
            }
            else
            {
                InboundBandwidth = 0;
                OutboundBandwidth = 0;
            }
        }

    }
}
