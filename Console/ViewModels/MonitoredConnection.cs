using CommunityToolkit.Mvvm.ComponentModel;

using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Media;
using System.Windows.Shapes;

using Wokhan.ComponentModel.Extensions;
using Wokhan.WindowsFirewallNotifier.Common.IO.Files;
using Wokhan.WindowsFirewallNotifier.Common.Net.GeoLocation;
using Wokhan.WindowsFirewallNotifier.Common.Net.IP;
using Wokhan.WindowsFirewallNotifier.Common.Processes;
using Wokhan.WindowsFirewallNotifier.Common.UI.ViewModels;
using Wokhan.WindowsFirewallNotifier.Console.Helpers;

namespace Wokhan.WindowsFirewallNotifier.Console.ViewModels;

public partial class MonitoredConnection : ConnectionBaseInfo
{
    public string Owner { get; private set; }

    public string? WindowTitle { get; private set; }

    public DateTime LastSeen { get; private set; }

    [ObservableProperty]
    private bool _isSelected;

    [ObservableProperty]
    private bool _isNew = true;

    [ObservableProperty]
    private bool _isDying;

    [ObservableProperty]
    private bool _isDead;

    [ObservableProperty]
    private string? _lastError;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StateImage))]
    private string _state;

    public Path StateImage => (Path)((App)App.Current).TryFindResource("CONN_STATE_" + _state);

    partial void OnStateChanged(string value)
    {
        if (!IsMonitored)
        {
            IsMonitored = _rawConnection.TryEnableStats();
        }
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Brush))]
    private Color _color = Colors.Black;

    private Brush? _brush;
    public Brush Brush => _brush ??= new SolidColorBrush(_color);

    [ObservableProperty]
    private ulong _inboundBandwidth;

    [ObservableProperty]
    private ulong _outboundBandwidth;


    [ObservableProperty]
    private bool _isMonitored;


    private Connection _rawConnection;

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
        if (_rawConnection.IsLoopback || Protocol == "UDP" && State != "ESTABLISHED" || Owner is null || Coordinates is null || GeoLocationHelper.CurrentCoordinates is null)
        {
            return NoLocation;
        }

        return [GeoLocationHelper.CurrentCoordinates, Coordinates];
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

    public MonitoredConnection(Connection rawconnection, double? connectionTimeoutNew)
    {
        _rawConnection = rawconnection;

        State = rawconnection.State.ToString();

        if (connectionTimeoutNew is not null && rawconnection.CreationTime is not null)
        {
            IsNew = DateTime.Now.Subtract(rawconnection.CreationTime.Value).TotalMilliseconds <= connectionTimeoutNew;
        }
        LastSeen = DateTime.Now;
        Pid = rawconnection.OwningPid;
        SourceIP = rawconnection.LocalAddress.ToString();
        SourcePort = rawconnection.LocalPort.ToString();
        CreationTime = rawconnection.CreationTime ?? DateTime.Now;
        Protocol = rawconnection.Protocol;
        TargetIP = rawconnection.RemoteAddress.ToString();
        TargetPort = (rawconnection.RemotePort == -1 ? String.Empty : rawconnection.RemotePort.ToString());

        if (rawconnection.OwnerModule == Common.Net.IP.Owner.System)
        {
            FileName = Properties.Resources.Connection_ProcessFile_System;
            Path = Common.Net.IP.Owner.System.ModulePath;
            Owner = Common.Net.IP.Owner.System.ModuleName!;
            Icon = IconHelper.SystemIcon;
        }
        else
        {
            Owner = rawconnection.OwnerModule?.ModuleName ?? "Unknown";

            try
            {
                var process = Process.GetProcessById((int)rawconnection.OwningPid);

                // Not working yet (for Firefox at least, it returns the wrong title everytime, taking the active tab)
                // WindowTitle = process?.MainWindowTitle;

                var module = process?.MainModule;
                Path = module?.FileName ?? "Unknown";
                FileName = module?.ModuleName ?? Properties.Resources.Connection_ProcessFile_Unknown;
            }
            catch (Win32Exception we) when (we.NativeErrorCode == 5)
            {
                var r = ProcessHelper.GetProcessOwnerWMI(rawconnection.OwningPid);
                Path = r?.Path ?? "Unknown";
                FileName = r?.Name ?? Properties.Resources.Connection_ProcessFile_Unknown;
            }
        }

        SetProductInfo();
    }

    internal void UpdateValues(Connection updatedConnection)
    {
        LastSeen = DateTime.Now;

        // Update the underlying Connection object (remote address, and so on).
        _rawConnection.UpdateWith(updatedConnection);

        TargetIP = _rawConnection.RemoteAddress.ToString();
        TargetPort = (_rawConnection.RemotePort == -1 ? String.Empty : _rawConnection.RemotePort.ToString());
        State = _rawConnection.State.ToString();

        if (IsMonitored)
        {
            (InboundBandwidth, OutboundBandwidth, IsMonitored) = _rawConnection.GetEstimatedBandwidth();
        }
    }

    public bool Matches(Connection connectionInfo)
    {
        return Pid == connectionInfo.OwningPid && Protocol == connectionInfo.Protocol && SourcePort == connectionInfo.LocalPort.ToString();
    }
}
