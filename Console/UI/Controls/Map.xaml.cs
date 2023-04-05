using CommunityToolkit.Mvvm.ComponentModel;

using Microsoft.Maps.MapControl.WPF;

using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

using Wokhan.WindowsFirewallNotifier.Console.Helpers;
using Wokhan.WindowsFirewallNotifier.Console.ViewModels;

namespace Wokhan.WindowsFirewallNotifier.Console.UI.Controls;

[ObservableObject]
public partial class Map : UserControl
{
    //private object locker = new();

    public static readonly DependencyProperty ConnectionsProperty = DependencyProperty.Register(nameof(Connections), typeof(ObservableCollection<MonitoredConnection>), typeof(Map));

    public ObservableCollection<MonitoredConnection> Connections
    {
        get => (ObservableCollection<MonitoredConnection>)GetValue(ConnectionsProperty);
        set => SetValue(ConnectionsProperty, value);
    }

    [ObservableProperty]
    private Location _currentCoordinates;

    [ObservableProperty]
    private bool _isFullRouteDisplayed;

    public bool IsAerial
    {
        get => Mode is AerialMode;
        set { Mode = (value ? new AerialMode(true) : new RoadMode()); }
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsAerial))]
    private MapMode _mode = new RoadMode();


    public Map()
    {
        this.Loaded += Map_Loaded;

        InitializeComponent();
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

        GeoLocationHelper.CurrentCoordinatesChanged += GeoLocationHelper_LocationChanged;

        ProgressStack.Visibility = Visibility.Collapsed;
    }

    //TODO: change for the Connections to update themselves when bound to the chart?
    private void GeoLocationHelper_LocationChanged(object? sender, EventArgs e)
    {
        foreach (var route in Connections)
        {
            route.UpdateStartingPoint();
        }
    }
}