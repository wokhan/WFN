
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

using Wokhan.ComponentModel.Extensions;
using Wokhan.UI.Extensions;
using Wokhan.WindowsFirewallNotifier.Common.Config;
using Wokhan.WindowsFirewallNotifier.Common.Logging;
using Wokhan.WindowsFirewallNotifier.Common.Net.WFP;
using Wokhan.WindowsFirewallNotifier.Common.Processes;
using Wokhan.WindowsFirewallNotifier.Common.Security;
using Wokhan.WindowsFirewallNotifier.Common.UI.ViewModels;

namespace Wokhan.WindowsFirewallNotifier.Console.UI.Pages
{
    /// <summary>
    /// Interaction logic for EventLog.xaml
    /// </summary>
    public sealed partial class EventsLog : Page, INotifyPropertyChanged, IDisposable
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private EventLogAsyncReader<LogEntryViewModel> eventsReader;
        public ICollectionView dataView;
        private readonly EventsLogFilters eventsLogFilters;

        private uint newEntriesCount;
        public uint NewEntriesCount
        {
            get => newEntriesCount;
            set => this.SetValue(ref newEntriesCount, value, NotifyPropertyChanged);
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
                    eventsLogFilters.ResetTcpFilter();
                }
            }
        }

        public string FilterText
        {
            get => eventsLogFilters.FilterText;
            set
            {
                eventsLogFilters.FilterText = value;
                eventsLogFilters.ResetTextfilter();
            }
        }

        public EventsLog()
        {
            VirtualizedQueryableExtensions.Init(Dispatcher);

            Loaded += (s, e) => StartHandlingSecurityLogEvents();
            Unloaded += (s, e) => StopHandlingSecurityLogEvents();

            eventsLogFilters = new EventsLogFilters(this);

            InitializeComponent();

            if (!Settings.Default.EnableDnsResolver)
            {
                RemoteHostCol.Visibility = Visibility.Hidden;
            }
        }

        private void StartHandlingSecurityLogEvents(bool reset = false)
        {
            try
            {
                if (reset || eventsReader is null)
                {
                    NewEntriesCount = 0;
                    eventsReader?.Dispose();
                    eventsReader = new EventLogAsyncReader<LogEntryViewModel>(EventLogAsyncReader.EVENTLOG_SECURITY, LogEntryViewModel.CreateFromEventLogEntry)
                    {
                        FilterPredicate = EventLogAsyncReader.IsFirewallEvent
                    };
                    eventsReader.EntryWritten += EntryWritten;
                    dataView = CollectionViewSource.GetDefaultView(eventsReader.Entries);
                    gridLog.ItemsSource = dataView;
                }

                //eventsLogFilters.ResetTcpFilter();
            }
            catch (Exception exc)
            {
                LogHelper.Error("Unable to connect to the event log", exc);
                throw;
            }
        }

        private void StopHandlingSecurityLogEvents()
        {
            eventsReader?.Dispose();
            eventsReader = null;
        }

        private void EntryWritten(object sender, EntryWrittenEventArgs e)
        {
            NewEntriesCount++;
        }

        private void btnLocate_Click(object sender, RoutedEventArgs e)
        {
            var selectedLog = (LogEntryViewModel)gridLog.SelectedItem;
            if (selectedLog is null)
            {
                //@
                return;
            }
            ProcessHelper.StartShellExecutable("explorer.exe", "/select," + selectedLog.Path, true);
        }

        private void btnEventLogVwr_Click(object sender, RoutedEventArgs e)
        {
            ProcessHelper.StartShellExecutable("eventvwr.msc", null, true);
        }


        public void Dispose()
        {
            eventsReader?.Dispose();
        }

        private void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            StartHandlingSecurityLogEvents(true);
        }
    }
}
