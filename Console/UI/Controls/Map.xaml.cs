using Microsoft.Maps.MapControl.WPF;

using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

using Wokhan.WindowsFirewallNotifier.Console.ViewModels;

namespace Wokhan.WindowsFirewallNotifier.Console.UI.Controls
{
    public partial class Map : UserControl, INotifyPropertyChanged
    {
        public ObservableCollection<Connection> Connections { get => (ObservableCollection<Connection>)GetValue(ConnectionsProperty); set => SetValue(ConnectionsProperty, value); }
        public static readonly DependencyProperty ConnectionsProperty = DependencyProperty.Register(nameof(Connections), typeof(ObservableCollection<Connection>), typeof(Map));

        public Location CurrentCoordinates { get; private set; }

        private bool _isFullRouteDisplayed;
        public bool IsFullRouteDisplayed
        {
            get { return _isFullRouteDisplayed; }
            set { _isFullRouteDisplayed = value; NotifyPropertyChanged(); }
        }

        private void NotifyPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public bool IsAerial
        {
            get { return _mode is AerialMode; }
            set { Mode = (value ? new AerialMode(true) : (MapMode)new RoadMode()); NotifyPropertyChanged(); }
        }

        private MapMode _mode = new RoadMode();
        private object locker = new object();

        public event PropertyChangedEventHandler PropertyChanged;

        public MapMode Mode
        {
            get { return _mode; }
            set { _mode = value; NotifyPropertyChanged(); }
        }

        public ObservableCollection<GeoConnection2> ConnectionsRoutes { get; } = new ObservableCollection<GeoConnection2>();

        public Map()
        {
            this.Loaded += Map_Loaded;

            InitializeComponent();
            BindingOperations.EnableCollectionSynchronization(ConnectionsRoutes, locker);

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
                CurrentCoordinates = GeoConnection2.CurrentCoordinates;
            }
            catch
            {
                // TODO: add log
                CurrentCoordinates = new Location(0, 0);
            }

            ProgressStack.Visibility = Visibility.Collapsed;
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
    }
}