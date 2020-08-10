using LiveCharts;
using LiveCharts.Defaults;
using LiveCharts.Geared;

using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

using Wokhan.Core.Core;
using Wokhan.WindowsFirewallNotifier.Console.Helpers.ViewModels;

namespace Wokhan.WindowsFirewallNotifier.Console.UI.Controls
{

    /// <summary>
    /// Logique d'interaction pour BandwidthGraph.xaml
    /// </summary>
    public partial class BandwidthGraph : UserControl, INotifyPropertyChanged
    {
        public ObservableCollection<Connection> Connections { get => (ObservableCollection<Connection>)GetValue(ConnectionsProperty); set => SetValue(ConnectionsProperty, value); }
        public static readonly DependencyProperty ConnectionsProperty = DependencyProperty.Register(nameof(Connections), typeof(ObservableCollection<Connection>), typeof(BandwidthGraph));

        public event PropertyChangedEventHandler PropertyChanged;

        public SeriesCollection SeriesCollection { get; } = new SeriesCollection();
        public Func<double, string> YFormatter { get; set; } = (y) => UnitFormatter.FormatBytes(y, "ps");
        public Func<double, string> XFormatter { get; set; } = (x) => new DateTime((long)x).ToString(DateTimeFormatInfo.CurrentInfo.LongTimePattern);
        public long Start => DateTime.Now.AddSeconds(-10).Ticks;
        public long End => DateTime.Now.Ticks;

        public BandwidthGraph()
        {
            InitializeComponent();
        }

        public void UpdateGraph()
        {
            var datetime = DateTime.Now;

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Start)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(End)));

            var localConnections = Dispatcher.Invoke(() => Connections.GroupBy(connection => connection.GroupKey).ToList());
            //Dispatcher.Invoke(() =>
            {
                foreach (var connectionGroup in localConnections)
                {
                    var seriesInTitle = $"{connectionGroup.Key}#In";
                    var seriesOutTitle = $"{connectionGroup.Key}#Out";
                    var seriesIn = Dispatcher.Invoke(() => SeriesCollection.FirstOrDefault(s => s.Title == seriesInTitle));
                    var seriesOut = Dispatcher.Invoke(() => SeriesCollection.FirstOrDefault(s => s.Title == seriesOutTitle));

                    if (seriesIn is null)
                    {
                        SeriesCollection.Add(seriesIn = Dispatcher.Invoke(() => new GLineSeries() { Title = seriesInTitle, Values = new GearedValues<DateTimePoint>() }));
                        SeriesCollection.Add(seriesOut = Dispatcher.Invoke(() => new GLineSeries() { Title = seriesOutTitle, Values = new GearedValues<DateTimePoint>() }));
                    }

                    seriesIn.Values.Add(new DateTimePoint(datetime, connectionGroup.Sum(connection => connection.InboundBandwidth)));
                    seriesOut.Values.Add(new DateTimePoint(datetime, connectionGroup.Sum(connection => connection.OutboundBandwidth)));
                }
            }//);
        }
    }
}
