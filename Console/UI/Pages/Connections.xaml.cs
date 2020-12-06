using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

using Wokhan.WindowsFirewallNotifier.Common.Net.IP;
using Wokhan.WindowsFirewallNotifier.Console.ViewModels;

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
        private readonly object uisynclocker = new object();

        public ObservableCollection<Connection> AllConnections { get; } = new ObservableCollection<Connection>();

        public Connections()
        {
            BindingOperations.EnableCollectionSynchronization(AllConnections, uisynclocker);

            InitializeComponent();
        }

        private void Components_VisibilityChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            firstRow.MaxHeight = connections.IsVisible ? double.PositiveInfinity : 1;
            separatorRow.MaxHeight = connections.IsVisible ? double.PositiveInfinity : 0;

            switch ((map.IsVisible, graph.IsVisible))
            {
                // All hidden
                case (false, false):
                    secondRow.MaxHeight = 0;
                    separatorRow.MaxHeight = 0;
                    graphColumn.MaxWidth = double.PositiveInfinity;
                    separatorColumn.MaxWidth = 0;
                    mapColumn.MaxWidth = 0;
                    break;

                // Map is visible
                case (true, false):
                    secondRow.MaxHeight = double.PositiveInfinity;
                    // Workaround: if set to 0, total width will be wrongly set
                    graphColumn.MaxWidth = 1;
                    separatorColumn.MaxWidth = 0;
                    mapColumn.MaxWidth = double.PositiveInfinity;
                    break;

                // Graph is visible
                case (false, true):
                    secondRow.MaxHeight = double.PositiveInfinity;
                    graphColumn.MaxWidth = double.PositiveInfinity;
                    separatorColumn.MaxWidth = 0;
                    mapColumn.MaxWidth = 0;
                    break;

                // Both are visible
                case (true, true):
                    secondRow.MaxHeight = double.PositiveInfinity;
                    graphColumn.MaxWidth = double.PositiveInfinity;
                    separatorColumn.MaxWidth = double.PositiveInfinity;
                    mapColumn.MaxWidth = double.PositiveInfinity;
                    break;
            }
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
                        lock (locker)
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


        //TODO: let the user pick a color palette for the bandwidth graph & connection
        private static List<Color> Colors = OxyPlot.OxyPalettes.Rainbow(64).Colors.Select(c => Color.FromArgb(c.A, c.R, c.G, c.B)).ToList();

        private void AddOrUpdateConnection(IConnectionOwnerInfo connectionInfo)
        {
            Connection lvi;
            // TEMP: test to avoid enumerating while modifying (might result in a deadlock, to test carefully!)
            lock (locker)
                lvi = AllConnections.FirstOrDefault(l => l.Pid == connectionInfo.OwningPid && l.Protocol == connectionInfo.Protocol && l.SourcePort == connectionInfo.LocalPort.ToString());

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
                    AllConnections.Add(new Connection(connectionInfo) { Color = Colors[AllConnections.Count % Colors.Count] });
            }
        }
    }
}