using CommunityToolkit.Mvvm.ComponentModel;

using LiveChartsCore.Kernel;

using Microsoft.Maps.MapControl.WPF;

using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Threading;

using Wokhan.ComponentModel.Extensions;
using Wokhan.WindowsFirewallNotifier.Common.Net.IP;
using Wokhan.WindowsFirewallNotifier.Common.Net.IP.TCP;
using Wokhan.WindowsFirewallNotifier.Common.Processes;
using Wokhan.WindowsFirewallNotifier.Common.UI.ViewModels;
using Wokhan.WindowsFirewallNotifier.Console.Helpers;

namespace Wokhan.WindowsFirewallNotifier.Console.ViewModels;

public partial class Connection : ConnectionBaseInfo
{
    private Dispatcher dispatcher;
    public string Owner { get; private set; }

    #region Geolocation


    private Location _coordinates;
    public Location? Coordinates => this.GetOrSetValueAsync(() => GeoLocationHelper.IPToLocationAsync(TargetIP), ref _coordinates, OnCoordinatesPropertyChanged);

    private void OnCoordinatesPropertyChanged(string propertyName)
    {
        _straightRoute = null;

        OnPropertyChanged(nameof(Coordinates));
        OnPropertyChanged(nameof(StraightRoute));
    }
    

    private LocationCollection? _straightRoute;
    public LocationCollection? StraightRoute => this.GetOrSetValueAsync(() => ComputeStraightRoute(), ref _straightRoute, OnPropertyChanged);


    private static LocationCollection NoLocation = new();
    private LocationCollection ComputeStraightRoute()
    {
        if (Protocol != "UDP" && State != "ESTABLISHED" || !IPAddress.TryParse(TargetIP, out var _) || Owner is null || Coordinates is null || GeoLocationHelper.CurrentCoordinates is null)
        {
            return NoLocation;
        }

        return new LocationCollection() { GeoLocationHelper.CurrentCoordinates, Coordinates };
    }


    //TODO: should be defined on the Map side, not GeoConnection, as it relies on Bing Maps control. Or use a independent datamodel and map to Location in the Map class.
    private LocationCollection? _fullRoute;
    public LocationCollection? FullRoute => this.GetOrSetValueAsync(() => ComputeFullRoute(), ref _fullRoute, OnPropertyChanged);

    private async Task<LocationCollection?> ComputeFullRoute()
    {
        if (Protocol != "UDP" && State != "ESTABLISHED" || !IPAddress.TryParse(TargetIP, out var _) || Owner is null || GeoLocationHelper.CurrentCoordinates is null)
        {
            return NoLocation;
        }
        
        var loc = new LocationCollection() { GeoLocationHelper.CurrentCoordinates };
        var route = await IPHelper.GetFullRoute(TargetIP).ConfigureAwait(false);
        foreach (var ip in route)
        {
            var location = await GeoLocationHelper.IPToLocationAsync(ip);
            // Ignore unresolved locations
            if (location.Latitude != 0 && location.Longitude != 0)
            {
                loc.Add(location);
            }
        }
        return loc;
    }

    internal void UpdateStartingPoint()
    {
        _fullRoute = null;
        OnPropertyChanged(nameof(FullRoute));

        _straightRoute = null;
        OnPropertyChanged(nameof(StraightRoute));
    }

    #endregion

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
        CreationTime = ownerMod.CreationTime ?? DateTime.Now;
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

    public DateTime LastSeen { get; private set; }

    private readonly IConnectionOwnerInfo rawConnection;
    private ITcpRow? rawrow;

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
