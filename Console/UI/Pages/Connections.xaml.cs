using CommunityToolkit.Mvvm.ComponentModel;

using HarfBuzzSharp;

using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.Themes;

using SkiaSharp.Views.WPF;

using System.ComponentModel;
using System.Data;
using System.Drawing.Imaging;
using System.Timers;
using System.Windows;
using System.Windows.Data;

using System.Windows.Media;

using Wokhan.Collections;
using Wokhan.WindowsFirewallNotifier.Common.Config;
using Wokhan.WindowsFirewallNotifier.Common.Net.IP;

using Wokhan.WindowsFirewallNotifier.Common.UI.Themes;
using Wokhan.WindowsFirewallNotifier.Console.ViewModels;

namespace Wokhan.WindowsFirewallNotifier.Console.UI.Pages;

public partial class Connections : TimerBasedPage
{
    private const double ConnectionTimeoutRemove = 5000.0; //milliseconds
    private const double ConnectionTimeoutDying = 2000.0; //milliseconds
    private const double ConnectionTimeoutNew = 1000.0; //milliseconds

    //TODO: let the user pick a color palette for the bandwidth graph & connection
    private List<Color>? Colors;

    public GroupedObservableCollection<GroupedMonitoredConnections, MonitoredConnection> GroupedConnections { get; init; }

    public GroupedObservableCollectionView<GroupedMonitoredConnections, MonitoredConnection> ConnectionsView { get; init; }

    [ObservableProperty]
    private string _textFilter = String.Empty;

    partial void OnTextFilterChanged(string value) => ResetTextFilter();

    public Connections()
    {
        UpdateConnectionsColors();

        GroupedMonitoredConnections keyGetter(MonitoredConnection connection) => GroupedConnections!.Keys.FirstOrDefault(group => group.Path == connection.Path) ?? new GroupedMonitoredConnections(connection, Colors![GroupedConnections!.Count % Colors.Count]);

        GroupedConnections = new(keyGetter);
        ConnectionsView = new(GroupedConnections, keyGetter);
        ConnectionsView.SortDescriptions.Add(new SortDescription("FileName", ListSortDirection.Ascending));

        Settings.Default.PropertyChanged += SettingsChanged;

        InitializeComponent();

    }

    private void SettingsChanged(object? sender, PropertyChangedEventArgs? e)
    {
        if (e?.PropertyName == nameof(Settings.Theme))
        {
            UpdateConnectionsColors();
        }
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

    protected override void OnTimerTick(object? state, ElapsedEventArgs e)
    {
        foreach (var connection in IPHelper.GetAllConnections())
        {
            AddOrUpdateConnection(connection);
        }

        DateTime now = DateTime.Now;

        // Start from the end as it's easier to handle removal from a collection when what you removed doesn't impact the actual index / count (also a foreach would fail if we modify the collection)
        for (int j = GroupedConnections.Count - 1; j >= 0; j--)
        {
            var group = GroupedConnections[j];
            var inboundBandwidth = 0UL;
            var outboundBandwidth = 0UL;

            for (int i = group.Count - 1; i >= 0; i--)
            {
                var connection = group[i];

                var lastSeenMS = now.Subtract(connection.LastSeen).TotalMilliseconds;
                if (lastSeenMS > ConnectionTimeoutRemove)
                {
                    Dispatcher.Invoke(() =>
                    {
                        if (group.Remove(connection) && group.Count == 0)
                        {
                            GroupedConnections.Remove(group);
                        }
                    });
                }
                else if (lastSeenMS > ConnectionTimeoutDying)
                {
                    connection.IsDying = true;
                }
                else
                {
                    connection.IsDying = false;
                    if (connection.IsNew && DateTime.Now.Subtract(connection.CreationTime).TotalMilliseconds > ConnectionTimeoutNew)
                    {
                        connection.IsNew = false;
                    }
                }

                if (connection.IsMonitored)
                {
                    inboundBandwidth += connection.InboundBandwidth;
                    outboundBandwidth += connection.OutboundBandwidth;
                }
            }

            group.Key.InboundBandwidth = inboundBandwidth;
            group.Key.OutboundBandwidth = outboundBandwidth;
        }

        if (graph.IsVisible) graph.UpdateGraph();
        if (map.IsVisible) map.UpdateMap();
    }


    private void AddOrUpdateConnection(Connection connectionInfo)
    {
        MonitoredConnection? lvi = GroupedConnections.Values.FirstOrDefault(mconn => mconn.Matches(connectionInfo));

        if (lvi is not null)
        {
            lvi.UpdateValues(connectionInfo);
        }
        else
        {
            Dispatcher.Invoke(() =>
            {
                //using var defer = ConnectionsView.DeferRefresh();
                GroupedConnections.Add(new MonitoredConnection(connectionInfo, ConnectionTimeoutNew));
            });
        }
    }

    internal void UpdateConnectionsColors()
    {
        // TODO: temporary improvement for dark themes colors. I'll have to rework this anyway.
        Colors = Settings.Default.Theme switch
        {
            ThemeHelper.THEME_LIGHT => LiveChartsCore.Themes.ColorPalletes.FluentDesign.Select(c => c.AsSKColor().ToColor()).ToList(),
            ThemeHelper.THEME_DARK => LiveChartsCore.Themes.ColorPalletes.FluentDesign.Select(c => c.AsSKColor().ToColor()).ToList(),
            ThemeHelper.THEME_SYSTEM => [SystemColors.WindowTextColor],
            _ => LiveChartsCore.Themes.ColorPalletes.FluentDesign.Select(c => c.AsSKColor().ToColor()).ToList(),
        };

        // TODO: check for concurrent access issue when switching themes while updating
        // I removed the lock but it could have been useful here...
        if (GroupedConnections is not null)
        {
            for (var i = 0; i < GroupedConnections.Count; i++)
            {
                GroupedConnections[i].Key.Color = Colors[i % Colors.Count];
            }
        }
    }


    private bool _isResetTextFilterPending;
    internal async void ResetTextFilter()
    {
        if (!_isResetTextFilterPending)
        {
            _isResetTextFilterPending = true;
            await Task.Delay(500).ConfigureAwait(true);
            if (!string.IsNullOrWhiteSpace(TextFilter))
            {
                ConnectionsView!.Filter = ConnectionsView_Filter;
            }
            else
            {
                ConnectionsView!.Filter = null;
            }
            _isResetTextFilterPending = false;
        }
    }

    private bool ConnectionsView_Filter(object obj)
    {
        var connection = (MonitoredConnection)obj;

        // Note: do not use Remote Host, because this will trigger dns resolution over all entries
        // TODO: fix since we're now using ObservableGrouping (with already grouped collection)
        return ((connection.FileName?.Contains(TextFilter, StringComparison.OrdinalIgnoreCase) == true)
             || (connection.ServiceName?.Contains(TextFilter, StringComparison.OrdinalIgnoreCase) == true)
             || (connection.TargetIP?.StartsWith(TextFilter, StringComparison.Ordinal) == true)
             || connection.State.StartsWith(TextFilter, StringComparison.Ordinal)
             || connection.Protocol.Contains(TextFilter, StringComparison.OrdinalIgnoreCase)
             || connection.SourcePort.Contains(TextFilter));
    }
}