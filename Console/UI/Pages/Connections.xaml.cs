using CommunityToolkit.Mvvm.ComponentModel;

using LiveChartsCore.SkiaSharpView;

using SkiaSharp.Views.WPF;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
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

    private readonly object locker = new();
    private readonly object uisynclocker = new();

    //TODO: let the user pick a color palette for the bandwidth graph & connection
    private List<Color>? Colors;

    public ObservableCollection<MonitoredConnection> AllConnections { get; } = new();

    public GroupedObservableCollection<GroupedMonitoredConnections, MonitoredConnection> GroupedConnections { get; init; }

    [ObservableProperty]
    private string? _textFilter = String.Empty;

    partial void OnTextFilterChanged(string? value) => ResetTextFilter();

    CollectionViewSource connectionsView;

    public Connections()
    {
        UpdateConnectionsColors();

        AllConnections.CollectionChanged += AllConnections_CollectionChanged;
        GroupedConnections = new(connection => new GroupedMonitoredConnections(connection, Colors![GroupedConnections!.Count % Colors.Count]));

        BindingOperations.EnableCollectionSynchronization(AllConnections, uisynclocker);

        Settings.Default.PropertyChanged += SettingsChanged;

        InitializeComponent();

        connectionsView = (CollectionViewSource)this.Resources["connectionsView"];
        connectionsView.GroupDescriptions.Add(new GroupedCollectionGroupDescription<GroupedMonitoredConnections, MonitoredConnection>());
    }

    private void AllConnections_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        Dispatcher.Invoke(() =>
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    foreach (MonitoredConnection item in e.NewItems!)
                    {
                        GroupedConnections.Add(item);
                    }
                    break;

                case NotifyCollectionChangedAction.Remove:
                    foreach (MonitoredConnection item in e.OldItems!)
                    {
                        var group = GroupedConnections.First(group => group.Key.Path == item.Path);
                        group.Remove(item);
                        if (group.Count() == 0)
                        {
                            GroupedConnections.Remove(group);
                        }
                    }
                    break;
            }
        });
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
        foreach (var c in IPHelper.GetAllConnections())
        {
            AddOrUpdateConnection(c);
        }

        // Start from the end as it's easier to handle removal from a collection when what you removed doesn't impact the actual index / count
        for (int i = AllConnections.Count - 1; i >= 0; i--)
        {
            var item = AllConnections[i];

            switch (DateTime.Now.Subtract(item.LastSeen).TotalMilliseconds)
            {
                case > ConnectionTimeoutRemove:
                    AllConnections.RemoveAt(i);
                    break;

                case > ConnectionTimeoutDying:
                    item.IsDying = true;
                    break;

                default:
                    if (DateTime.Now.Subtract(item.CreationTime).TotalMilliseconds > ConnectionTimeoutNew)
                    {
                        item.IsNew = false;
                    }
                    break;
            }
        }

        foreach(var group in GroupedConnections)
        {
            group.Key.UpdateBandwidth(group);
        }

        if (graph.IsVisible) graph.UpdateGraph();
        //if (map.IsVisible) map.UpdateMap();
    }


    private void AddOrUpdateConnection(Connection connectionInfo)
    {
        MonitoredConnection? lvi = AllConnections.FirstOrDefault(mconn => mconn.Matches(connectionInfo));

        if (lvi is not null)
        {
            lvi.UpdateValues(connectionInfo);
        }
        else
        {
            AllConnections.Add(new MonitoredConnection(connectionInfo));
        }
    }

    internal void UpdateConnectionsColors()
    {
        // TODO: temporary improvement for dark themes colors. I'll have to rework this anyway.
        Colors = Settings.Default.Theme switch
        {
            ThemeHelper.THEME_LIGHT => LiveChartsCore.Themes.ColorPalletes.FluentDesign.Select(c => c.AsSKColor().ToColor()).ToList(),
            ThemeHelper.THEME_DARK => LiveChartsCore.Themes.ColorPalletes.FluentDesign.Select(c => c.AsSKColor().ToColor()).ToList(),
            ThemeHelper.THEME_SYSTEM => new List<Color> { SystemColors.WindowTextColor },
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
                connectionsView!.Filter -= ConnectionsView_Filter;
                connectionsView.Filter += ConnectionsView_Filter; ;
            }
            else
            {
                connectionsView!.Filter -= ConnectionsView_Filter;
            }
            _isResetTextFilterPending = false;
        }
    }

    private void ConnectionsView_Filter(object sender, FilterEventArgs e)
    {
        e.Accepted = true;
        //var connection = (ObservableGrouping<GroupedMonitoredConnectionsX, MonitoredConnection>)e.Item;
        
        // Note: do not use Remote Host, because this will trigger dns resolution over all entries
        // TODO: fix since we're now using ObservableGrouping (with already grouped collection)
        //e.Accepted = ((connection.FileName?.Contains(TextFilter, StringComparison.OrdinalIgnoreCase) == true)
        //           || (connection.ServiceName?.Contains(TextFilter, StringComparison.OrdinalIgnoreCase) == true)
        //           || (connection.TargetIP?.StartsWith(TextFilter, StringComparison.Ordinal) == true)
        //           || connection.State.StartsWith(TextFilter, StringComparison.Ordinal)
        //           || connection.Protocol.Contains(TextFilter, StringComparison.OrdinalIgnoreCase));
    }
}