using CommunityToolkit.Mvvm.ComponentModel;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Media;

using Wokhan.ComponentModel.Extensions;
using Wokhan.WindowsFirewallNotifier.Common.Net.GeoLocation;
using Wokhan.WindowsFirewallNotifier.Common.Net.IP;
using Wokhan.WindowsFirewallNotifier.Common.Net.IP.TCP;
using Wokhan.WindowsFirewallNotifier.Common.UI.ViewModels;
using Wokhan.WindowsFirewallNotifier.Console.Helpers;

namespace Wokhan.WindowsFirewallNotifier.Console.ViewModels;

public partial class Connection : ConnectionBaseInfo
{
    public string Owner { get; private set; }

    public DateTime LastSeen { get; private set; }

    [ObservableProperty]
    private bool _isAccessDenied;

    [ObservableProperty]
    private bool _isSelected;

    [ObservableProperty]
    private bool _isDead;

    [ObservableProperty]
    private string? _lastError;

    [ObservableProperty]
    private string? _state;

    [ObservableProperty]
    private bool _isDying;

    [ObservableProperty]
    private bool _isNew;

    [ObservableProperty]
    private Color _color = Colors.Black;

    [ObservableProperty]
    private ulong _inboundBandwidth;

    [ObservableProperty]
    private ulong _outboundBandwidth;

    private readonly IConnectionOwnerInfo rawConnection;
    private ITcpRow? rawrow;

    #region Geolocation


    private GeoLocation? _coordinates;
    public GeoLocation? Coordinates => this.GetOrSetValueAsync(() => GeoLocationHelper.IPToLocationAsync(TargetIP), ref _coordinates, OnCoordinatesPropertyChanged);

    private void OnCoordinatesPropertyChanged(string propertyName)
    {
        // Reset both the computed fullRoute and straightRoute to force resolution since the target changed
        _straightRoute = null;
        _fullRoute = null;

        OnPropertyChanged(nameof(Coordinates));
        OnPropertyChanged(nameof(StraightRoute));
        OnPropertyChanged(nameof(FullRoute));
    }


    private IEnumerable<GeoLocation>? _straightRoute;
    public IEnumerable<GeoLocation>? StraightRoute => this.GetOrSetValueAsync(() => ComputeStraightRoute(), ref _straightRoute, OnPropertyChanged);


    private static IEnumerable<GeoLocation> NoLocation = Enumerable.Empty<GeoLocation>();

    private IEnumerable<GeoLocation> ComputeStraightRoute()
    {
        if (TargetIP is "127.0.0.1" or "::1" || Protocol == "UDP" && State != "ESTABLISHED" || Owner is null || Coordinates is null || GeoLocationHelper.CurrentCoordinates is null)
        {
            return NoLocation;
        }

        return new[] { GeoLocationHelper.CurrentCoordinates, Coordinates };
    }


    private IEnumerable<GeoLocation>? _fullRoute;
    public IEnumerable<GeoLocation>? FullRoute => this.GetOrSetValueAsync(() => ComputeFullRoute(), ref _fullRoute, OnPropertyChanged);

    private async Task<IEnumerable<GeoLocation>?> ComputeFullRoute()
    {
        if (Protocol == "UDP" || State != "ESTABLISHED" || Owner is null)
        {
            return NoLocation;
        }

        return await GeoLocationHelper.ComputeRoute(TargetIP).ConfigureAwait(false);
    }

    internal void UpdateStartingPoint()
    {
        _fullRoute = null;
        OnPropertyChanged(nameof(FullRoute));

        _straightRoute = null;
        OnPropertyChanged(nameof(StraightRoute));
    }

    #endregion

    public Connection(IConnectionOwnerInfo ownerMod)
    {
        rawConnection = ownerMod;

        IsNew = true;

        Pid = ownerMod.OwningPid;
        SourceIP = ownerMod.LocalAddress;
        SourcePort = ownerMod.LocalPort.ToString();
        CreationTime = ownerMod.CreationTime ?? DateTime.Now;
        Protocol = ownerMod.Protocol;
        TargetIP = ownerMod.RemoteAddress;
        TargetPort = (ownerMod.RemotePort == -1 ? String.Empty : ownerMod.RemotePort.ToString());
        LastSeen = DateTime.Now;
        //this._state = Enum.GetName(typeof(ConnectionStatus), ownerMod.State);

        if (Pid is 0 or 4)
        {
            FileName = Properties.Resources.Connection_ProcessFile_System;
            Owner = Properties.Resources.Connection_ProcessOwner_System;
        }
        else
        {
            try
            {
                var module = Process.GetProcessById((int)ownerMod.OwningPid)?.MainModule;
                if (module is not null)
                {
                    Path = module.FileName ?? Properties.Resources.Connection_ProcessPath_Unknown;
                    FileName = module.ModuleName ?? Properties.Resources.Connection_ProcessFile_Unknown;
                }
            }
            catch
            {
                FileName = Properties.Resources.Connection_ProcessFile_UnknownOrClosed;
                Path = Properties.Resources.Connection_ProcessPath_Unresolved;
            }

            if (ownerMod.OwnerModule is null)
            {
                Owner = Properties.Resources.Connection_ProcessOwner_Unknown;
                //Path = Path ?? Properties.Resources.Connection_ProcessPath_Unresolved;
            }
            else
            {
                Owner = ownerMod.OwnerModule.ModuleName;
                IconPath = ownerMod.OwnerModule.ModulePath;
            }
        }

        SetProductInfo();
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

            rawrow = this.rawConnection.ToTcpRow();
            rawrow.EnsureStats();

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
                if (rawrow is not null && !IsAccessDenied)
                {
                    var bandwidth = rawrow.GetTCPBandwidth();
                    // Fix according to https://docs.microsoft.com/en-us/windows/win32/api/iphlpapi/nf-iphlpapi-setpertcpconnectionestats
                    // One must subtract the previously read value to get the right one (as reenabling statistics doesn't work as before starting from Win 10 1709)
                    InboundBandwidth = bandwidth.InboundBandwidth >= _lastInboundReadValue ? bandwidth.InboundBandwidth - _lastInboundReadValue : bandwidth.InboundBandwidth;
                    OutboundBandwidth = bandwidth.OutboundBandwidth >= _lastOutboundReadValue ? bandwidth.OutboundBandwidth - _lastOutboundReadValue : bandwidth.OutboundBandwidth;
                    _lastInboundReadValue = bandwidth.InboundBandwidth;
                    _lastOutboundReadValue = bandwidth.OutboundBandwidth;
                }
            }
            catch
            {
                //TODO: Add exception log
                InboundBandwidth = 0;
                OutboundBandwidth = 0;
            }

        });
    }

}
