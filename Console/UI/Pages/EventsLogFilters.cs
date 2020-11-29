using System;
using System.Threading.Tasks;

using Wokhan.WindowsFirewallNotifier.Common.UI.ViewModels;

namespace Wokhan.WindowsFirewallNotifier.Console.UI.Pages
{
    /// <summary>
    /// Handles filtering of security event logs
    /// </summary>
    internal class EventsLogFilters
    {
        private readonly EventsLog _eventsLog;

        private static string _filterText;

        internal string FilterText
        {
            get => _filterText;
            set
            {
                _filterText = value;
                ResetTextfilter();
            }
        }

        internal EventsLogFilters(EventsLog eventsLog)
        {
            this._eventsLog = eventsLog;
        }

        private readonly Predicate<object> TcpFilterPredicate = (o) => ((LogEntryViewModel)o).Protocol == "TCP";

        internal void ResetTcpFilter()
        {
            if (_eventsLog.dataView is null) { return; }

            _eventsLog.dataView.Filter -= TcpFilterPredicate;
            if (_eventsLog.IsTCPOnlyEnabled)
            {
                _eventsLog.dataView.Filter += TcpFilterPredicate;
            }
        }

        private Predicate<object> FilterTextPredicate = (o) =>
        {
            LogEntryViewModel le = o as LogEntryViewModel;
            // Note: do not use Remote Host, because this will trigger dns resolution over all entries
            return (le.TargetIP is null ? false : le.TargetIP.StartsWith(_filterText, StringComparison.Ordinal))
            || (le.ServiceName is null ? false : le.ServiceName.Contains(_filterText, StringComparison.OrdinalIgnoreCase))
            || (le.FileName is null ? false : le.FileName.Contains(_filterText, StringComparison.OrdinalIgnoreCase));
        };


        private bool _isResetTextFilterPending;
        internal async void ResetTextfilter()
        {
            if (!_isResetTextFilterPending)
            {
                _isResetTextFilterPending = true;
                await Task.Delay(500).ConfigureAwait(true);
                if (!string.IsNullOrWhiteSpace(_filterText))
                {
                    _eventsLog.dataView.Filter -= FilterTextPredicate;
                    _eventsLog.dataView.Filter += FilterTextPredicate;
                }
                else
                {
                    _eventsLog.dataView.Filter -= FilterTextPredicate;
                }
                _isResetTextFilterPending = false;
            }
        }
    }
}
