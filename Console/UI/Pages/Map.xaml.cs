using Microsoft.Maps.MapControl.WPF;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Threading;
using Wokhan.WindowsFirewallNotifier.Common.Helpers;
using Wokhan.WindowsFirewallNotifier.Console.Helpers;
using Wokhan.WindowsFirewallNotifier.Console.Helpers.ViewModels;
using Wokhan.WindowsFirewallNotifier.Console.UI.Controls;

namespace Wokhan.WindowsFirewallNotifier.Console.UI.Pages
{
    /// <summary>
    /// Logique d'interaction pour Map.xaml
    /// </summary>
    public partial class Map : Page, INotifyPropertyChanged
    {
        public Location CurrentCoordinates { get { return GeoConnection2.CurrentCoordinates; } }

        public bool IsTrackingEnabled
        {
            get { return timer.IsEnabled; }
            set { timer.IsEnabled = value; }
        }

        private bool _isFullRouteDisplayed;
        public bool IsFullRouteDisplayed
        {
            get { return _isFullRouteDisplayed; }
            set { _isFullRouteDisplayed = value; NotifyPropertyChanged(nameof(IsFullRouteDisplayed)); }
        }

        public bool IsAerial
        {
            get { return _mode is AerialMode; }
            set { Mode = (value ? new AerialMode(true) : (MapMode)new RoadMode()); NotifyPropertyChanged(nameof(IsAerial)); }
        }

        private MapMode _mode = new RoadMode();
        public MapMode Mode
        {
            get { return _mode; }
            set { _mode = value; NotifyPropertyChanged(nameof(Mode)); }
        }

        public List<int> Intervals { get { return new List<int> { 1, 5, 10 }; } }

        private DispatcherTimer timer = new DispatcherTimer() { IsEnabled = true };

        private ObservableCollection<MapGroupedView> _connections = new ObservableCollection<MapGroupedView>();
        public ObservableCollection<MapGroupedView> Connections { get { return _connections; } }

        private ListCollectionView _connectionsView;
        public ListCollectionView ConnectionsView { get { return _connectionsView; } }

        private ObservableCollection<GeoConnection2> _connectionsRoutes = new ObservableCollection<GeoConnection2>();
        public ObservableCollection<GeoConnection2> ConnectionsRoutes { get { return _connectionsRoutes; } }

        private int _interval = 1;

        public event PropertyChangedEventHandler PropertyChanged;

        protected void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }


        public int Interval
        {
            get { return _interval; }
            set { _interval = value; timer.Interval = TimeSpan.FromSeconds(value); }
        }

        public Map()
        {
            _connectionsView = (ListCollectionView)CollectionViewSource.GetDefaultView(_connectionsRoutes);
            _connectionsView.IsLiveGrouping = true;
            _connectionsView.GroupDescriptions.Add(new PropertyGroupDescription("Owner"));
            
            InitializeComponent();

            timer.Interval = TimeSpan.FromSeconds(Interval);

            this.Loaded += Map_Loaded;
            this.Unloaded += Map_Unloaded;
        }

        private void Map_Unloaded(object sender, RoutedEventArgs e)
        {
            timer.Stop();
        }

        async void Map_Loaded(object sender, RoutedEventArgs e)
        {
            if (!GeoConnection2.CheckDB())
            {
                MessageBox.Show("The IP database cannot be found. The Map feature is disabled.", "Missing database");
                return;
            }
            var ok = await GeoConnection2.InitCache();

            initialPoint.SetValue(MapLayer.PositionProperty, CurrentCoordinates);

            ProgressStack.Visibility = Visibility.Collapsed;

            timer.Tick += timer_Tick;
            await Dispatcher.InvokeAsync(() => timer_Tick(null, null));
        }

        void timer_Tick(object sender, EventArgs e)
        {
            foreach (var c in IPHelper.GetAllConnections(true)
                                      .Where(co => co.State == IPHelper.MIB_TCP_STATE.ESTABLISHED && !co.IsLoopback && co.OwnerModule != null))
            {
                AddOrUpdateConnection(c);
            }

            CurrentMap.UpdateLayout();
            /*
            var killduration = Math.Max(5, 3 * _interval);
            var dieduration = Math.Max(2, 2 * _interval);
            for (int i = Connections.Count - 1; i >= 0; i--)
            {
                var item = Connections[i];
                double elapsed = DateTime.Now.Subtract(item.LastSeen).TotalSeconds;
                if (elapsed > killduration)
                {
                    Connections.Remove(item);
                }
                else if (elapsed > dieduration)
                {
                    item.IsDying = true;
                }
            }*/
        }

        private void AddOrUpdateConnection(IPHelper.I_OWNER_MODULE b)
        {
            var ic = _connectionsRoutes.Count % LineChart.ColorsDic.Count;
            var br = new SolidColorBrush(LineChart.ColorsDic[ic]);

            GeoConnection2 existingRoute = _connectionsRoutes.SingleOrDefault(l => l.RemoteAddress.Equals(b.RemoteAddress));
            if (existingRoute == null)
            {
                _connectionsRoutes.Add(new GeoConnection2(b) { Brush = br });
            }
        }

        private void btnGrpOwner_Checked(object sender, RoutedEventArgs e)
        {
            _connectionsView.GroupDescriptions.Clear();
            _connectionsView.GroupDescriptions.Add(new PropertyGroupDescription("Owner"));
        }

        private void btnGrpIP_Checked(object sender, RoutedEventArgs e)
        {
            _connectionsView.GroupDescriptions.Clear();
            _connectionsView.GroupDescriptions.Add(new PropertyGroupDescription("RemoteAddress"));
        }

        private void btnRestartAdmin_Click(object sender, RoutedEventArgs e)
        {
            ((App)Application.Current).RestartAsAdmin();
        }
    }
}