using System;
using System.Windows.Media;
using Wokhan.WindowsFirewallNotifier.Common.IO.Files;
using System.Diagnostics;
using Wokhan.WindowsFirewallNotifier.Common.Processes;
using Wokhan.WindowsFirewallNotifier.Common.Security;
using WFP = Wokhan.WindowsFirewallNotifier.Common.Net.WFP;
using Wokhan.WindowsFirewallNotifier.Common.Logging;
using System.Linq;
using Wokhan.WindowsFirewallNotifier.Common.Net.WFP;
using Wokhan.WindowsFirewallNotifier.Common.Core;

namespace Wokhan.WindowsFirewallNotifier.Common.UI.ViewModels
{
    public class LogEntryViewModel : ConnectionBaseInfo
    {
        public int Index { get; protected set; }
        public int Id { get; protected set; }
        public string? FilterId { get; protected set; }

        private FilterResult? _matchingFilter;
        public FilterResult? MatchingFilter => this.GetOrSetAsyncValue(() => NetshHelper.FindMatchingFilterInfo(int.Parse(FilterId)), NotifyPropertyChanged, nameof(_matchingFilter));
        public string? Reason { get; protected set; }
        public string? Message { get; protected set; }

        //TODO: should be in the XAML as conditionally triggered style
        public SolidColorBrush? ReasonColor { get; protected set; }

        public SolidColorBrush? DirectionColor { get; protected set; }

        public static bool TryCreateFromEventLogEntry<T>(EventLogEntry entry, int index, out T? view) where T : LogEntryViewModel, new()
        {
            if (entry == null)
            {
                view = null;
                return false;
            }

            try
            {
                //LogHelper.Debug($"Create EntryViewModel entry...");
                var pid = uint.Parse(GetReplacementString(entry, 0));
                var direction = GetReplacementString(entry, 2) == @"%%14593" ? "Out" : "In";
                var protocol = int.Parse(GetReplacementString(entry, 7));

                var path = GetReplacementString(entry, 1);
                if (path == "-")
                {
                    path = "System";
                }
                else
                {
                    path = PathResolver.ResolvePath(path);
                }
                var fileName = System.IO.Path.GetFileName(path);

                // try to get the servicename from pid (works only if service is running)
                var serviceName = ServiceNameResolver.GetServicName(pid);

                var le = new T()
                {
                    Index = index,
                    Id = entry.Index,
                    Pid = pid,
                    CreationTime = entry.TimeGenerated,
                    Path = (path == "-" ? "System" : path),
                    FileName = fileName,
                    ServiceName = serviceName,
                    SourceIP = GetReplacementString(entry, 3),
                    SourcePort = GetReplacementString(entry, 4),
                    TargetIP = GetReplacementString(entry, 5),
                    TargetPort = GetReplacementString(entry, 6),
                    RawProtocol = protocol,
                    Protocol = WFP.Protocol.GetProtocolAsString(protocol),
                    Direction = direction,
                    FilterId = GetReplacementString(entry, 8),
                    Reason = EventLogAsyncReader.GetEventInstanceIdAsString(entry.InstanceId),
                    Message = entry.Message
                };

                le.ReasonColor = le.Reason.StartsWith("Block") ? Brushes.OrangeRed : Brushes.Blue;
                le.DirectionColor = le.Direction.StartsWith("In") ? Brushes.OrangeRed : Brushes.Black;

                view = le;

                return true;
            }
            catch (Exception ex)
            {
                LogHelper.Error("Cannot parse eventlog entry: eventID=" + entry.InstanceId.ToString(), ex);
            }

            view = null;

            return false;
        }

        private static string GetReplacementString(EventLogEntry entry, int i)
        {
            return entry.ReplacementStrings.DefaultIfEmpty(string.Empty).ElementAtOrDefault(i);
        }

        public static LogEntryViewModel? CreateFromEventLogEntry(EventLogEntry entry, int index)
        {
            return TryCreateFromEventLogEntry(entry, index, out LogEntryViewModel? ret) ? ret : null;
        }
    }

}
