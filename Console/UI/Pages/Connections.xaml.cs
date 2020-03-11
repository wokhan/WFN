using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Threading;
using Wokhan.WindowsFirewallNotifier.Common.Helpers;
using Wokhan.WindowsFirewallNotifier.Console.Helpers.ViewModels;

namespace Wokhan.WindowsFirewallNotifier.Console.UI.Pages
{
    /// <summary>
    /// Interaction logic for Connections.xaml
    /// </summary>
    public partial class Connections : Page
    {
        private const double ConnectionTimeoutRemove = 5.0; //seconds
        private const double ConnectionTimeoutDying = 2.0; //seconds
        private const double ConnectionTimeoutNew = 1000.0; //milliseconds

        public bool IsTrackingEnabled
        {
            get { return timer.IsEnabled; }
            set { timer.IsEnabled = value; }
        }

        public List<int> Intervals => new List<int> { 1, 5, 10 };

        private DispatcherTimer timer = new DispatcherTimer() { IsEnabled = true };

        public ObservableCollection<Connection> lstConnections { get; } = new ObservableCollection<Connection>();

        public ListCollectionView connectionsView { get; set; }

        private int _interval = 1;
        public int Interval
        {
            get { return _interval; }
            set { _interval = value; timer.Interval = TimeSpan.FromSeconds(value); }
        }

        private bool running;

        public Connections()
        {
            //TODO: Use BindingOperations.EnableCollectionSynchronization(lstConnections, locker); instead of Dispatcher invocations

            connectionsView = (ListCollectionView)CollectionViewSource.GetDefaultView(lstConnections);
            connectionsView.GroupDescriptions.Add(new PropertyGroupDescription("GroupKey"));
            connectionsView.SortDescriptions.Add(new SortDescription("GroupKey", ListSortDirection.Ascending));

            InitializeComponent();

            timer.Interval = TimeSpan.FromSeconds(Interval);
            timer.Tick += timer_Tick;

            this.Loaded += Connections_Loaded;
            this.Unloaded += Connections_Unloaded;
        }

        private void Connections_Unloaded(object sender, RoutedEventArgs e)
        {
            timer.Stop();
        }

        void Connections_Loaded(object sender, RoutedEventArgs e)
        {
            timer_Tick(null, null);
        }

        async void timer_Tick(object sender, EventArgs e)
        {
            if (running)
            {
                return;
            }

            running = true;

            await Task.Run(() =>
            {
                // Resets the WMI cache (used for non admin users)
                Connection.LocalOwnerWMICache = null;
                foreach (var c in IPHelper.GetAllConnections())
                {
                    Dispatcher.Invoke(() => AddOrUpdateConnection(c));
                }

                for (int i = lstConnections.Count - 1; i >= 0; i--)
                {
                    var item = lstConnections[i];
                    double elapsed = DateTime.Now.Subtract(item.LastSeen).TotalSeconds;
                    if (elapsed > ConnectionTimeoutRemove)
                    {
                        Dispatcher.Invoke(() => lstConnections.Remove(item));
                    }
                    else if (elapsed > ConnectionTimeoutDying)
                    {
                        item.IsDying = true;
                    }
                }
            }).ConfigureAwait(false);

            running = false;
        }

        private void AddOrUpdateConnection(IPHelper.I_OWNER_MODULE b)
        {
            Connection lvi = lstConnections.SingleOrDefault(l => l.PID == b.OwningPid && l.Protocol == b.Protocol && l.LocalPort == b.LocalPort.ToString());

            if (lvi != null)
            {
                if (DateTime.Now.Subtract(lvi.LastSeen).TotalMilliseconds > ConnectionTimeoutNew)
                {
                    lvi.IsNew = false;
                }

                lvi.UpdateValues(b);
            }
            else
            {
                lstConnections.Add(new Connection(b));
            }
        }

        private void btnRestartAdmin_Click(object sender, RoutedEventArgs e)
        {
            ((App)Application.Current).RestartAsAdmin();
        }
    }
}