using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Mapsui;
using Mapsui.Extensions;
using Mapsui.Layers;
using Mapsui.Nts;
using Mapsui.Nts.Extensions;
using Mapsui.Projections;
using Mapsui.Styles;
using Mapsui.Tiling;
using Mapsui.UI.Wpf.Extensions;
using Mapsui.Widgets.ButtonWidgets;
using Mapsui.Widgets.ScaleBar;

using NetTopologySuite.Geometries;

using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

using Wokhan.Collections;
using Wokhan.WindowsFirewallNotifier.Common.Net.GeoLocation;
using Wokhan.WindowsFirewallNotifier.Console.Helpers;
using Wokhan.WindowsFirewallNotifier.Console.ViewModels;

namespace Wokhan.WindowsFirewallNotifier.Console.UI.Controls;

[ObservableObject]
public partial class Map : UserControl
{
    //private object locker = new();

    public static readonly DependencyProperty ConnectionsProperty = DependencyProperty.Register(nameof(Connections), typeof(GroupedObservableCollection<GroupedMonitoredConnections, MonitoredConnection>), typeof(Map));

    public GroupedObservableCollection<GroupedMonitoredConnections, MonitoredConnection> Connections
    {
        get => (GroupedObservableCollection<GroupedMonitoredConnections, MonitoredConnection>)GetValue(ConnectionsProperty);
        set => SetValue(ConnectionsProperty, value);
    }

    //[ObservableProperty]
    //private Location _currentCoordinates;


    [ObservableProperty]
    private bool _isFullRouteDisplayed;

    private MyLocationLayer locationLayer;

    //public bool IsAerial
    //{
    //    get => Mode is AerialMode;
    //    set { Mode = (value ? new AerialMode(true) : new RoadMode()); }
    //}

    //[ObservableProperty]
    //[NotifyPropertyChangedFor(nameof(IsAerial))]
    //private MapMode _mode = new RoadMode();

    private ObservableCollection<GeometryFeature> routesFeatures = [];
    private ObservableCollection<IFeature> symbolsFeatures = [];

    public Map()
    {
        this.Loaded += Map_Loaded;
        this.IsVisibleChanged += Map_IsVisibleChanged;

        InitializeComponent();

        var routesLayer = new MemoryLayer("Routes") { Features = routesFeatures };
        var symbolsLayer = new MemoryLayer("Symbols") { Features = symbolsFeatures, IsMapInfoLayer = true, Style = null };

        locationLayer = new MyLocationLayer(map);

        map.Layers.Add(OpenStreetMap.CreateTileLayer());
        map.Layers.Add(routesLayer);
        map.Layers.Add(symbolsLayer);
        map.Layers.Add(locationLayer);

        //map.Widgets.Add(new MapInfoWidget(map));
        map.Widgets.Add(new ScaleBarWidget(map) { ScaleBarMode = ScaleBarMode.Both, Margin = new MRect(10) });
        map.Widgets.Add(new ZoomInOutWidget());
    }

    private void Map_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (IsVisible)
        {
            if (GeoLocationHelper.CurrentCoordinates is not null)
            {
                GeoLocationHelper_LocationChanged(null, null);
            }
            GeoLocationHelper.CurrentCoordinatesChanged += GeoLocationHelper_LocationChanged;
        }
        else
        {
            GeoLocationHelper.CurrentCoordinatesChanged -= GeoLocationHelper_LocationChanged;
        }
    }

    private void UpdateConnections()
    {
        if (Connections is null)
        {
            return;
        }

        // Not using a foreach here to easily bypass concurrent modifications (we don't care if we miss one connection, it will appear on next round)
        foreach (var connection in Connections.Values)
        {
            var id = connection.SourcePort.GetHashCode();

            var polylineFeature = routesFeatures.FirstOrDefault(geo => connection.Equals(geo["owner"]));
            if (polylineFeature is null)
            {
                polylineFeature = new GeometryFeature()
                {
                    ["owner"] = connection
                };
                routesFeatures.Add(polylineFeature);
            }

            IEnumerable<GeoLocation>? route = IsFullRouteDisplayed ? connection.FullRoute : connection.StraightRoute;

            // Route is not ready yet 
            if (route is null)
            {
                continue;
            }

            // We are excluding default (0,0) coordinate as it means proper coordinates weren't resolved
            var coordinates = route.Select(loc => SphericalMercator.FromLonLat(loc.Longitude.Value, loc.Latitude.Value).ToCoordinate()).Where(coord => coord.X != 0 && coord.Y != 0).ToArray();
            // If only one point has been found and points toward nowhere, we skip it.
            if (coordinates.Length <= 1)
            {
                continue;
            }

            // Route needs to be updated
            if (!route.GetHashCode().Equals(polylineFeature["routehash"]))
            {
                var color = Mapsui.Styles.Color.FromArgb(connection.Color.A, connection.Color.R, connection.Color.G, connection.Color.B);
                var brush = new Mapsui.Styles.Brush(color);

                polylineFeature.Geometry = new LineString(coordinates);
                polylineFeature.Styles = [new VectorStyle()
                {
                    Line = new Mapsui.Styles.Pen
                    {
                        PenStrokeCap = PenStrokeCap.Round,
                        Width = 2,
                        Color = color
                    },
                }];

                // Remove all symbols for the route since it has changed
                for (int i = symbolsFeatures.Count - 1; i >= 0; i--)
                {
                    var symbol = symbolsFeatures[i];
                    if (connection.Equals(symbol["owner"]))
                    {
                        symbolsFeatures.Remove(symbol);
                    }
                }

                for (int i = 1; i < coordinates.Length; i++)
                {
                    var coordinate = coordinates[i];
                    var symbolFeature = new PointFeature(coordinate.X, coordinate.Y)
                    {
                        ["owner"] = connection,
                        Styles = [new SymbolStyle {
                            Fill = brush,
                            //SymbolScale = connection.Coordinates?.AccuracyRadius / 100 ?? 0.8,
                            //UnitType = UnitType.WorldUnit,
                            SymbolScale = 0.8,
                            SymbolType = (i == coordinates.Length - 1 ? SymbolType.Triangle : SymbolType.Ellipse)
                        }]
                    };

                    symbolsFeatures.Add(symbolFeature);
                }


                polylineFeature["routehash"] = route.GetHashCode();
            }

            var style = (VectorStyle)polylineFeature.Styles.First();
            style.Line!.DashArray = connection.IsDying ? [0, 2] : [1];
            style.Opacity = connection.IsDead ? 0.5f : 1.0f;
            style.Outline = connection.IsSelected ? new Mapsui.Styles.Pen(Mapsui.Styles.Color.Red, 2) : null;

            var lastpoint = (PointFeature)symbolsFeatures.Last(symbol => connection.Equals(symbol["owner"]));
            var pointStyle = (SymbolStyle)lastpoint.Styles.First();
            if (connection.IsSelected)
            {
                if (!true.Equals(lastpoint["focused"]))
                {
                    pointStyle.Outline = new Mapsui.Styles.Pen(Mapsui.Styles.Color.Red, 4);
                    //pointStyle.SymbolScale = 1;
                    map.Navigator.FlyTo(lastpoint.Point, map.Navigator.Resolutions[0] * 1.5, 1000);
                    lastpoint["focused"] = true;
                }
            }
            else
            {
                pointStyle.Outline = null;
                pointStyle.SymbolScale = 0.5;
                lastpoint["focused"] = false;
            }
        }

        for (int i = routesFeatures.Count - 1; i >= 0; i--)
        {
            var route = routesFeatures[i];
            if (!Connections.Values.Any(connection => connection.Equals(route["owner"])))
            {
                routesFeatures.Remove(route);
                for (int j = symbolsFeatures.Count - 1; j >= 0; j--)
                {
                    var symbol = symbolsFeatures[j];
                    if (symbol["owner"]!.Equals(route["owner"]))
                    {
                        symbolsFeatures.Remove(symbol);
                    }
                }
            }
        }

        CurrentMap.Refresh();
    }

    async void Map_Loaded(object sender, RoutedEventArgs e)
    {
        if (!GeoLocationHelper.CheckDB())
        {
            ProgressMessage.Text = Properties.Resources.Map_MissingDBMessage;
            Progress.IsIndeterminate = false;
            Progress.Foreground = Brushes.OrangeRed;
            Progress.Value = 100;
            return;
        }

        await GeoLocationHelper.Init(true).ConfigureAwait(true);

        ProgressStack.Visibility = Visibility.Collapsed;
    }

    //TODO: change for the Connections to update themselves when bound to the chart?
    private void GeoLocationHelper_LocationChanged(object? sender, EventArgs e)
    {
        // Focus to current location only if there is no selecte
        if (activeConnectionCallout is null)
        {
            FocusOnCurrentLocation();
        }

        foreach (var connection in Connections.Values)
        {
            connection.UpdateStartingPoint();
        }

        UpdateConnections();
    }

    [RelayCommand]
    private void FocusOnCurrentLocation()
    {
        var coordinates = GeoLocationHelper.CurrentCoordinates;
        var loc = SphericalMercator.FromLonLat(coordinates.Longitude.Value, coordinates.Latitude.Value).ToMPoint();
        CurrentMap.Map.Navigator.FlyTo(loc, CurrentMap.Map.Navigator.Viewport.Resolution * 2, 1500);
        locationLayer.UpdateMyLocation(loc, false);
    }

    public void UpdateMap()
    {
        Dispatcher.Invoke(() => UpdateConnections());
    }

    MonitoredConnection? activeConnectionCallout = null;
    private void CurrentMap_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
    {
        var mapInfo = CurrentMap.GetMapInfo(e.GetPosition(CurrentMap).ToScreenPosition());
        var connection = (MonitoredConnection?)(mapInfo?.Feature as PointFeature)?["owner"];
        if (connection?.Equals(activeConnectionCallout) ?? false)
        {
            return;
        }

        if (connection is null)
        {
            infoPopup.Visibility = Visibility.Collapsed;
            infoPopup.DataContext = null;
            activeConnectionCallout = null;
            return;
        }

        infoPopup.DataContext = connection;

        Canvas.SetLeft(infoPopup, e.GetPosition(CurrentMap).X + 10);
        Canvas.SetTop(infoPopup, e.GetPosition(CurrentMap).Y);
        infoPopup.Visibility = Visibility.Visible;

        activeConnectionCallout = connection;
    }
}