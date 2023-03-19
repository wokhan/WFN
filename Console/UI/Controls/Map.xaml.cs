using CommunityToolkit.Mvvm.ComponentModel;

using Microsoft.Maps.MapControl.WPF;

using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using Wokhan.WindowsFirewallNotifier.Console.Helpers;
using Wokhan.WindowsFirewallNotifier.Console.ViewModels;

namespace Wokhan.WindowsFirewallNotifier.Console.UI.Controls;

[ObservableObject]
public partial class Map : UserControl
{
    //private object locker = new();

    public static readonly DependencyProperty ConnectionsProperty = DependencyProperty.Register(nameof(Connections), typeof(ObservableCollection<Connection>), typeof(Map));

    public ObservableCollection<Connection> Connections
    {
        get => (ObservableCollection<Connection>)GetValue(ConnectionsProperty);
        set { SetValue(ConnectionsProperty, value); }// BindingOperations.EnableCollectionSynchronization(value, locker); }
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
            MessageBox.Show("The IP database cannot be found. The Map feature is disabled.", "Missing database");
            return;
        }

        await GeoLocationHelper.Init().ConfigureAwait(true);

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
        CurrentMap.UpdateLayout();
    }

    //public void UpdateMap()
    //{
    //    if (GeoLocationHelper.Initialized)
    //    {
    //        Dispatcher.Invoke(() =>
    //        {
    //            CurrentMap.UpdateLayout();
    //        });
    //    }
    //}

    //TODO: Temporary check for addresses validity (for mapping purpose only). Doesn't look like the right way to do this.
    private bool IsValid(string? remoteAddress)
    {
        return (!String.IsNullOrEmpty(remoteAddress)
            && remoteAddress != "127.0.0.1"
            && remoteAddress != "0.0.0.0"
            && remoteAddress != "::0");
    }
}