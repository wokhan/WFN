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
using Wokhan.WindowsFirewallNotifier.Common.UI.ViewModels;
using Wokhan.WindowsFirewallNotifier.Console.Helpers;

namespace Wokhan.WindowsFirewallNotifier.Console.ViewModels;

public partial class MonitoredConnection : ConnectionBaseInfo
{
    public string Owner { get; private set; }

    public DateTime LastSeen { get; private set; }

    [ObservableProperty]
    private bool _isSelected;

    [ObservableProperty]
    private bool _isNew;
    
    [ObservableProperty]
    private bool _isDying;

    [ObservableProperty]
    private bool _isDead;

    [ObservableProperty]
    private string? _lastError;

    [ObservableProperty]
    private string? _state;

    [ObservableProperty]
    private Color _color = Colors.Black;

    [ObservableProperty]
    private ulong _inboundBandwidth;

    [ObservableProperty]
    private ulong _outboundBandwidth;

    [ObservableProperty]
    private bool _isAccessDenied;


    private readonly Connection rawConnection;

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
    public IEnumerable<GeoLocation>? StraightRoute => _straightRoute ??= ComputeStraightRoute();


    private static IEnumerable<GeoLocation> NoLocation = Enumerable.Empty<GeoLocation>();

    private IEnumerable<GeoLocation> ComputeStraightRoute()
    {
        if (rawConnection.IsLoopback || Protocol == "UDP" && State != "ESTABLISHED" || Owner is null || Coordinates is null || GeoLocationHelper.CurrentCoordinates is null)
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

    public MonitoredConnection(Connection ownerMod)
    {
        rawConnection = ownerMod;

        IsNew = true;

        Pid = ownerMod.OwningPid;
        SourceIP = ownerMod.LocalAddress.ToString();
        SourcePort = ownerMod.LocalPort.ToString();
        CreationTime = ownerMod.CreationTime ?? DateTime.Now;
        Protocol = ownerMod.Protocol;
        TargetIP = ownerMod.RemoteAddress.ToString();
        TargetPort = (ownerMod.RemotePort == -1 ? String.Empty : ownerMod.RemotePort.ToString());
        LastSeen = DateTime.Now;

        _isAccessDenied = Protocol != "TCP";
        //this._state = Enum.GetName(typeof(ConnectionStatus), ownerMod.State);

        if (Pid is 0 or 4)
        {
            FileName = Properties.Resources.Connection_ProcessFile_System;
            Path = Properties.Resources.Connection_ProcessFile_System;
            Owner = Properties.Resources.Connection_ProcessOwner_System;
        }
        else
        {
            try
            {
                //TODO: check if this is solely to retrieve the owner's executable path as we already have the exe in Connection.cs through GetOwningModule
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

    internal void UpdateValues(Connection b)
    {
        //lvi.LocalAddress = b.LocalAddress;
        //lvi.Protocol = b.Protocol;
        var remoteIP = b.RemoteAddress.ToString();
        if (this.TargetIP != remoteIP)
        {
            TargetIP = remoteIP;
            // Force reset the target host name by setting it to null (it will be recomputed next)
            TargetHostName = null;
        }

        TargetPort = (b.RemotePort == -1 ? String.Empty : b.RemotePort.ToString());
        State = Enum.GetName(typeof(ConnectionStatus), b.State);
        if (!_isAccessDenied)
        {
            // TODO: Should use an object here (embedding all parameters as fields)
            (InboundBandwidth, OutboundBandwidth) = rawConnection.GetEstimatedBandwidth(ref _isAccessDenied);
        }

        LastSeen = DateTime.Now;
    }
}
