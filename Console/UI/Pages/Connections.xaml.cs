using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;

using Wokhan.WindowsFirewallNotifier.Common.Net.IP;
using Wokhan.WindowsFirewallNotifier.Console.Helpers.ViewModels;

namespace Wokhan.WindowsFirewallNotifier.Console.UI.Pages
{
    /// <summary>
    /// Interaction logic for Connections.xaml
    /// </summary>
    public partial class Connections : TimerBasedPage
    {
        private const double ConnectionTimeoutRemove = 5.0; //seconds
        private const double ConnectionTimeoutDying = 2.0; //seconds
        private const double ConnectionTimeoutNew = 1000.0; //milliseconds

        private readonly object locker = new object();

        public ObservableCollection<Connection> AllConnections { get; } = new ObservableCollection<Connection>();
        public ListCollectionView connectionsView { get; set; }

        public Connections()
        {
            BindingOperations.EnableCollectionSynchronization(AllConnections, locker);

            connectionsView = (ListCollectionView)CollectionViewSource.GetDefaultView(AllConnections);
            connectionsView.GroupDescriptions.Add(new PropertyGroupDescription(nameof(Connection.GroupKey)));
            connectionsView.SortDescriptions.Add(new SortDescription(nameof(Connection.GroupKey), ListSortDirection.Ascending));

            InitializeComponent();
        }

        protected override async Task OnTimerTick(object sender, EventArgs e)
        {
            await Task.Run(() =>
            {
                foreach (var c in IPHelper.GetAllConnections())
                {
                    AddOrUpdateConnection(c);
                }

                for (int i = AllConnections.Count - 1; i >= 0; i--)
                {
                    var item = AllConnections[i];
                    double elapsed = DateTime.Now.Subtract(item.LastSeen).TotalSeconds;
                    if (elapsed > ConnectionTimeoutRemove)
                    {
                        AllConnections.Remove(item);
                    }
                    else if (elapsed > ConnectionTimeoutDying)
                    {
                        item.IsDying = true;
                    }
                }

                if (graph.IsVisible) graph.UpdateGraph();
                if (map.IsVisible) map.UpdateMap();
            }).ConfigureAwait(false);
        }

        private void AddOrUpdateConnection(IConnectionOwnerInfo connectionInfo)
        {
            Connection lvi;
            //TEMP: test to avoid enumerating while modifying (might result in a deadlock, to test carefully!)
            lock (locker)
                lvi = AllConnections.SingleOrDefault(l => l.PID == connectionInfo.OwningPid && l.Protocol == connectionInfo.Protocol && l.LocalPort == connectionInfo.LocalPort.ToString());

            if (lvi != null)
            {
                if (DateTime.Now.Subtract(lvi.LastSeen).TotalMilliseconds > ConnectionTimeoutNew)
                {
                    lvi.IsNew = false;
                }

                lvi.UpdateValues(connectionInfo);
            }
            else
            {
                lock (locker)
                    AllConnections.Add(new Connection(connectionInfo) { Brush = Brushes.Blue });
            }
        }
    }
}