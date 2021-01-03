using System;
using System.Collections.Generic;
using Wokhan.ComponentModel.Extensions;
using Wokhan.WindowsFirewallNotifier.Common.Net.IP;
using System.Windows.Media;
using Wokhan.WindowsFirewallNotifier.Common.Processes;
using System.Threading.Tasks;
using Wokhan.WindowsFirewallNotifier.Common.UI.ViewModels;

namespace Wokhan.WindowsFirewallNotifier.Console.ViewModels
{
    // TODO: Inherit from LogEntryViewModel?
    public class Connection : ConnectionBaseInfo
    {
        public string Owner { get; private set; }

        /// <summary>
        /// Uses a cache for WMI information to avoid per-process costly queries.
        /// Warning: it has to be reset to null every time a new batch of processes will be handled, since it's not dynamically self-refreshed.
        /// </summary>
        public static Dictionary<int, string[]> LocalOwnerWMICache;

        public Connection(IConnectionOwnerInfo ownerMod)
        {
            rawConnection = ownerMod;

            IsNew = true;
            
            Pid = ownerMod.OwningPid;
            SourceIP = ownerMod.LocalAddress;
            SourcePort = ownerMod.LocalPort.ToString();
            CreationTime = ownerMod.CreationTime;
            Protocol = ownerMod.Protocol;
            TargetIP = ownerMod.RemoteAddress;
            TargetPort = (ownerMod.RemotePort == -1 ? String.Empty : ownerMod.RemotePort.ToString());
            LastSeen = DateTime.Now;
            //this._state = Enum.GetName(typeof(ConnectionStatus), ownerMod.State);

            try
            {
                // Mainly for non-admin users, could use Process.GetProcessById for admins...
                var r = ProcessHelper.GetProcessOwnerWMI((int)ownerMod.OwningPid, ref LocalOwnerWMICache);
                Path = r[1] ?? "Unknown"; //FIXME: Move to resources!
                FileName = r[0] ?? "Unknown"; //FIXME: Use something else?
            }
            catch
            {
                FileName = "[Unknown or closed process]"; //FIXME: Move to resources!
                Path = "Unresolved"; //FIXME: Use something else?
            }

            if (ownerMod.OwnerModule is null)
            {
                if (Pid == 0)
                {
                    FileName = "System";
                    Owner = "System";
                    Path = "-";
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
        }

        private bool TryEnableStats()
        {
            try
            {
                // Ignoring bandwidth measurement for loopbacks as it is meaningless anyway
                if (this.TargetIP == "127.0.0.1" || this.TargetIP == "::1")
                {
                    return false;
                }

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
                InboundBandwidth = 0;
                OutboundBandwidth = 0;
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

        private readonly IConnectionOwnerInfo rawConnection;
        private object rawrow;

        internal void UpdateValues(IConnectionOwnerInfo b)
        {
            //lvi.LocalAddress = b.LocalAddress;
            //lvi.Protocol = b.Protocol;
            if (this.TargetIP != b.RemoteAddress)
            {
                TargetIP = b.RemoteAddress;
                // Force reset the target host name by setting it to null (it will be recomputed next)
                TargetHostName = null;
            }

            TargetPort = (b.RemotePort == -1 ? String.Empty : b.RemotePort.ToString());
            State = Enum.GetName(typeof(ConnectionStatus), b.State);
            if (b.State == ConnectionStatus.ESTABLISHED && !IsAccessDenied)
            {
                if (!statsEnabled)
                {
                    TryEnableStats();
                }
                EstimateBandwidth();
            }
            else
            {
                InboundBandwidth = 0;
                OutboundBandwidth = 0;
            }

            LastSeen = DateTime.Now;
        }

        private string _state;
        public string State
        {
            get => _state;
            set => this.SetValue(ref _state, value, NotifyPropertyChanged);
        }


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

        private Color _color = Colors.Black;
        public Color Color
        {
            get => _color;
            set => this.SetValue(ref _color, value, NotifyPropertyChanged);
        }

        private ulong _inboundBandwidth;
        public ulong InboundBandwidth
        {
            get => _inboundBandwidth;
            private set => this.SetValue(ref _inboundBandwidth, value, NotifyPropertyChanged);
        }

        private ulong _outboundBandwidth;
        public ulong OutboundBandwidth
        {
            get => _outboundBandwidth;
            private set => this.SetValue(ref _outboundBandwidth, value, NotifyPropertyChanged);
        }

        private ulong _lastInboundReadValue;
        private ulong _lastOutboundReadValue;

        private bool statsEnabled;
        private void EstimateBandwidth()
        {
            if (!statsEnabled)
            {
                return;
            }

            Task.Run(() =>
            {
                try
                {
                    if (rawrow != null && !IsAccessDenied)
                    {
                        var bandwidth = (rawrow is TCPHelper.MIB_TCPROW ? TCPHelper.GetTCPBandwidth((TCPHelper.MIB_TCPROW)rawrow) : TCP6Helper.GetTCPBandwidth((TCP6Helper.MIB_TCP6ROW)rawrow));
                        // Fix according to https://docs.microsoft.com/en-us/windows/win32/api/iphlpapi/nf-iphlpapi-setpertcpconnectionestats
                        // One must subtract the previously read value to get the right one (as reenabling statistics doesn't work as before starting from Win 10 1709)
                        InboundBandwidth = bandwidth.InboundBandwidth >= _lastInboundReadValue ? bandwidth.InboundBandwidth - _lastInboundReadValue : bandwidth.InboundBandwidth;
                        OutboundBandwidth = bandwidth.OutboundBandwidth >= _lastOutboundReadValue ? bandwidth.OutboundBandwidth - _lastOutboundReadValue : bandwidth.OutboundBandwidth;
                        _lastInboundReadValue = bandwidth.InboundBandwidth;
                        _lastOutboundReadValue = bandwidth.OutboundBandwidth;
                        return;
                    }
                }
                catch
                {
                    //TODO: Add exception log
                }

                InboundBandwidth = 0;
                OutboundBandwidth = 0;
            });
        }

    }
}
