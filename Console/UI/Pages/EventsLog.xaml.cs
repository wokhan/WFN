
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

using Wokhan.UI.Extensions;
using Wokhan.WindowsFirewallNotifier.Common.Config;
using Wokhan.WindowsFirewallNotifier.Common.Helpers;
using Wokhan.WindowsFirewallNotifier.Common.Logging;
using Wokhan.WindowsFirewallNotifier.Common.Processes;
using Wokhan.WindowsFirewallNotifier.Common.Security;
using Wokhan.WindowsFirewallNotifier.Common.UI.ViewModels;

namespace Wokhan.WindowsFirewallNotifier.Console.UI.Pages;

[ObservableObject]
public sealed partial class EventsLog : Page, IDisposable
{
    public EventLogAsyncReader<LoggedConnection>? EventsReader { get; set; }

    [ObservableProperty]
    public ICollectionView? dataView;

    public int TCPOnlyOrAll
    {
        get => IsTCPOnlyEnabled ? 1 : 0;
        set => IsTCPOnlyEnabled = (value == 1);
    }

    public bool IsTCPOnlyEnabled
    {
        get => Settings.Default.FilterTcpOnlyEvents;
        set
        {
            if (IsTCPOnlyEnabled != value)
            {
                Settings.Default.FilterTcpOnlyEvents = value;
                Settings.Default.Save();
                ResetTcpFilter();
            }
        }
    }

    [ObservableProperty]
    private string _textFilter = String.Empty;
    partial void OnTextFilterChanged(string value) => ResetTextFilter();

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(LocateCommand))]
    private LoggedConnection? selectedItem;

    public EventsLog()
    {
        VirtualizedQueryableExtensions.Init(Dispatcher);

        if (UAC.CheckProcessElevated())
        {
            Loaded += (s, e) => StartHandlingSecurityLogEvents();
            Unloaded += (s, e) => StopHandlingSecurityLogEvents();
        }

        InitializeComponent();

        if (!Settings.Default.EnableDnsResolver)
        {
            RemoteHostCol.Visibility = Visibility.Hidden;
        }
    }

    private void StartHandlingSecurityLogEvents()
    {
        try
        {
            EventsReader?.Dispose();
            EventsReader = new EventLogAsyncReader<LoggedConnection>(EventLogAsyncReader.EVENTLOG_SECURITY, LoggedConnection.CreateFromEventLogEntry)
            {
                FilterPredicate = EventLogAsyncReader.IsFirewallEvent
            };
            OnPropertyChanged(nameof(EventsReader));

            // Fix for #159 - refreshing
            var x = EventsReader.Entries;
            DataView = CollectionViewSource.GetDefaultView(x);
        }
        catch (Exception exc)
        {
            LogHelper.Error("Unable to connect to the event log", exc);
            throw;
        }
    }

    private void StopHandlingSecurityLogEvents()
    {
        DataView = null;
        EventsReader?.Dispose();
        EventsReader = null;
    }

    public void Dispose()
    {
        DataView = null;
        EventsReader?.Dispose();
    }

    [RelayCommand(CanExecute = nameof(LocateCanExecute))]
    private void Locate()
    {
        ProcessHelper.StartShellExecutable("explorer.exe", "/select," + SelectedItem!.Path, true);
    }

    public bool LocateCanExecute => SelectedItem is not null;

    [RelayCommand]
    private void OpenEventsLogViewer()
    {
        ProcessHelper.StartShellExecutable("eventvwr.msc", showMessageBox: true);
    }

    [RelayCommand]
    private void Refresh()
    {
        StartHandlingSecurityLogEvents();
    }


    private bool TcpFilterPredicate(object entryAsObject) => ((LoggedConnection)entryAsObject).Protocol == "TCP";
    private bool FilterTextPredicate(object entryAsObject)
    {
        var le = (LoggedConnection)entryAsObject;

        // Note: do not use Remote Host, because this will trigger dns resolution over all entries
        return (le.TargetIP is not null && le.TargetIP.StartsWith(TextFilter, StringComparison.Ordinal))
            || (le.ServiceName is not null && le.ServiceName.Contains(TextFilter, StringComparison.OrdinalIgnoreCase))
            || (le.FileName is not null && le.FileName.Contains(TextFilter, StringComparison.OrdinalIgnoreCase));
    }

    internal void ResetTcpFilter()
    {
        if (DataView is null)
        {
            return;
        }

        DataView.Filter -= TcpFilterPredicate;
        if (IsTCPOnlyEnabled)
        {
            DataView.Filter += TcpFilterPredicate;
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
                DataView!.Filter -= FilterTextPredicate;
                DataView.Filter += FilterTextPredicate;
            }
            else
            {
                DataView!.Filter -= FilterTextPredicate;
            }
            _isResetTextFilterPending = false;
        }
    }


}
