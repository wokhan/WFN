using LiveChartsCore.SkiaSharpView;

using SkiaSharp.Views.WPF;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Timers;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

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

    public Connections()
    {
        UpdateConnectionsColors();

        BindingOperations.EnableCollectionSynchronization(AllConnections, uisynclocker);

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
        foreach (var c in IPHelper.GetAllConnections())
        {
            AddOrUpdateConnection(c);
        }

        // Start from the end as it's easier to handle removal from a collection when what you removed doesn't impact the actual index / count
        for (int i = AllConnections.Count - 1; i >= 0; i--)
        {
            var item = AllConnections[i];
            var elapsed = DateTime.Now.Subtract(item.LastSeen).TotalMilliseconds;
            if (elapsed > ConnectionTimeoutRemove)
            {
                lock (locker)
                    AllConnections.Remove(item);
            }
            else if (elapsed > ConnectionTimeoutDying)
            {
                item.IsDying = true;
            }
            else if (DateTime.Now.Subtract(item.CreationTime).TotalMilliseconds > ConnectionTimeoutNew)
            {
                item.IsNew = false;
            }
        }
        
        if (graph.IsVisible) graph.UpdateGraph();
        //if (map.IsVisible) map.UpdateMap();
    }


    private void AddOrUpdateConnection(Connection connectionInfo)
    {
        MonitoredConnection? lvi;
        // TEMP: test to avoid enumerating while modifying (might result in a deadlock, to test carefully!)
        lock (locker)
            lvi = AllConnections.FirstOrDefault(l => l.Pid == connectionInfo.OwningPid && l.Protocol == connectionInfo.Protocol && l.SourcePort == connectionInfo.LocalPort.ToString());

        if (lvi is not null)
        {
            lvi.UpdateValues(connectionInfo);
        }
        else
        {
            lock (locker)
                AllConnections.Add(new MonitoredConnection(connectionInfo) { Color = Colors[AllConnections.Count % Colors.Count] });
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

        lock (locker)
            for (var i = 0; i < AllConnections.Count; i++)
            {
                AllConnections[i].Color = Colors[i % Colors.Count];
            }
    }
}