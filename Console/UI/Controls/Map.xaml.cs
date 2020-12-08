using Microsoft.Maps.MapControl.WPF;

using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Device.Location;

using Wokhan.WindowsFirewallNotifier.Console.ViewModels;
using Wokhan.WindowsFirewallNotifier.Common.Logging;
using Wokhan.ComponentModel.Extensions;

namespace Wokhan.WindowsFirewallNotifier.Console.UI.Controls
{
    public partial class Map : UserControl, INotifyPropertyChanged, IDisposable
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private GeoCoordinateWatcher geoWatcher;

        private object locker = new object();


        public static readonly DependencyProperty ConnectionsProperty = DependencyProperty.Register(nameof(Connections), typeof(ObservableCollection<Connection>), typeof(Map));
        public ObservableCollection<Connection> Connections
        {
            get => (ObservableCollection<Connection>)GetValue(ConnectionsProperty);
            set => SetValue(ConnectionsProperty, value);
        }

        public string CurrentIP => GeoConnection2.CurrentIP.ToString();

        private Location _currentCoordinates;
        public Location CurrentCoordinates
        {
            get => _currentCoordinates;
            private set => this.SetValue(ref _currentCoordinates, value, NotifyPropertyChanged);
        }

        private bool _isFullRouteDisplayed;
        public bool IsFullRouteDisplayed
        {
            get => _isFullRouteDisplayed;
            set => this.SetValue(ref _isFullRouteDisplayed, value, NotifyPropertyChanged);
        }

        private void NotifyPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public bool IsAerial
        {
            get => _mode is AerialMode;
            set { Mode = (value ? new AerialMode(true) : (MapMode)new RoadMode()); NotifyPropertyChanged(); }
        }


        private MapMode _mode = new RoadMode();
        public MapMode Mode
        {
            get => _mode;
            set => this.SetValue(ref _mode, value, NotifyPropertyChanged);
        }

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
                if (CurrentCoordinates is null)
                {
                    CurrentCoordinates = GeoConnection2.CurrentCoordinates;
                }
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
                    foreach (var c in Connections.Where(co => (co.Protocol == "UDP" || co.State == "ESTABLISHED") && IsValid(co.TargetIP) && co.Owner != null))
                    {
                        AddOrUpdateConnection(c);
                    }

                    //TODO: there has to be a better way
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
            GeoConnection2 existingRoute = ConnectionsRoutes.FirstOrDefault(l => l.Connection.TargetIP.Equals(b.TargetIP));
            if (existingRoute is null)
            {
                ConnectionsRoutes.Add(new GeoConnection2(b));
            }
        }

        public void Dispose()
        {
            geoWatcher?.Dispose();
        }
    }
}