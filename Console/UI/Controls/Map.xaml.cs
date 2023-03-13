using CommunityToolkit.Mvvm.ComponentModel;

using Microsoft.Maps.MapControl.WPF;

using System;
using System.Collections.ObjectModel;
using System.Device.Location;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

using System.Windows.Data;

using Wokhan.WindowsFirewallNotifier.Common.Logging;
using Wokhan.WindowsFirewallNotifier.Console.ViewModels;

namespace Wokhan.WindowsFirewallNotifier.Console.UI.Controls;

[ObservableObject]
public partial class Map : UserControl, IDisposable
{
    private GeoCoordinateWatcher geoWatcher;

    private object locker = new();


    public static readonly DependencyProperty ConnectionsProperty = DependencyProperty.Register(nameof(Connections), typeof(ObservableCollection<Connection>), typeof(Map));
    public ObservableCollection<Connection> Connections
    {
        get => (ObservableCollection<Connection>)GetValue(ConnectionsProperty);
        set => SetValue(ConnectionsProperty, value);
    }

    public string CurrentIP => GeoConnection2.CurrentIP.ToString();

    [ObservableProperty]
    private Location _currentCoordinates;

    [ObservableProperty]
    private bool _isFullRouteDisplayed;
    
    public bool IsAerial
    {
        get => _mode is AerialMode;
        set { Mode = (value ? new AerialMode(true) : (MapMode)new RoadMode()); }
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsAerial))]
    private MapMode _mode = new RoadMode();

    public ObservableCollection<GeoConnection2> ConnectionsRoutes { get; } = new ObservableCollection<GeoConnection2>();

    public Map()
    {
        this.Loaded += Map_Loaded;
        this.Unloaded += Map_Unloaded;

        InitializeComponent();
        BindingOperations.EnableCollectionSynchronization(ConnectionsRoutes, locker);

    }

    private void Map_Unloaded(object sender, RoutedEventArgs e)
    {
        geoWatcher?.Dispose();
    }

    async void Map_Loaded(object sender, RoutedEventArgs e)
    {

        if (!GeoConnection2.CheckDB())
        {
            MessageBox.Show("The IP database cannot be found. The Map feature is disabled.", "Missing database");
            return;
        }

        await GeoConnection2.InitCache().ConfigureAwait(true);

        try
        {
            geoWatcher = new GeoCoordinateWatcher();
            geoWatcher.PositionChanged += GeoWatcher_PositionChanged;
            geoWatcher.Start();
        }
        catch (Exception exc)
        {
            LogHelper.Error("Unable to use GeoCoordinateWatcher. Falling back to IP address based method.", exc);
        }

        try
        {
            CurrentCoordinates ??= GeoConnection2.CurrentCoordinates;
        }
        catch (Exception exc)
        {
            LogHelper.Error("Unable to determine GeoLocation from IP address. Using default location.", exc);

            CurrentCoordinates = new Location(0, 0);
        }

        ProgressStack.Visibility = Visibility.Collapsed;
    }

    private void GeoWatcher_PositionChanged(object sender, GeoPositionChangedEventArgs<GeoCoordinate> e)
    {
        if (!geoWatcher.Position.Location.IsUnknown)
        {
            CurrentCoordinates = new Location(geoWatcher.Position.Location.Latitude, geoWatcher.Position.Location.Longitude);
            UpdateAllRoutes();
        }
    }

    private void UpdateAllRoutes()
    {
        foreach (var route in ConnectionsRoutes)
        {
            route.UpdateStartingPoint(CurrentCoordinates);
        }
        CurrentMap.UpdateLayout();
    }

    public void UpdateMap()
    {
        if (GeoConnection2.Initialized)
        {
            Dispatcher.Invoke(() =>
            {
                foreach (var c in Connections.Where(co => (co.Protocol == "UDP" || co.State == "ESTABLISHED") && IsValid(co.TargetIP) && co.Owner is not null))
                {
                    AddOrUpdateConnection(c);
                }

                //TODO: there has to be a better way (using a weak CollectionChanged event listener maybe?)
                ConnectionsRoutes.Where(route => !Connections.Contains(route.Connection))
                                 .ToList()
                                 .ForEach(route => ConnectionsRoutes.Remove(route));
                
                CurrentMap.UpdateLayout();
            });
        }
    }

    //TODO: Temporary check for addresses validity (for mapping purpose only). Doesn't look like the right way to do this.
    private bool IsValid(string remoteAddress)
    {
        return (!String.IsNullOrEmpty(remoteAddress)
            && remoteAddress != "127.0.0.1"
            && remoteAddress != "0.0.0.0"
            && remoteAddress != "::0");
    }

    private void AddOrUpdateConnection(Connection b)
    {
        if (!ConnectionsRoutes.Any(route => route.Connection.TargetIP.Equals(b.TargetIP)))
        {
            ConnectionsRoutes.Add(new GeoConnection2(b));
        }
    }

    public void Dispose()
    {
        geoWatcher?.Dispose();
    }
}