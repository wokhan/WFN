using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

using Wokhan.ComponentModel.Extensions;
using Wokhan.WindowsFirewallNotifier.Common.IO.Files;
using Wokhan.WindowsFirewallNotifier.Common.Logging;
using Wokhan.WindowsFirewallNotifier.Common.Net.WFP;
using Wokhan.WindowsFirewallNotifier.Common.Processes;
using Wokhan.WindowsFirewallNotifier.Common.Security;

using WFP = Wokhan.WindowsFirewallNotifier.Common.Net.WFP;

namespace Wokhan.WindowsFirewallNotifier.Common.UI.ViewModels;

public class LoggedConnection : ConnectionBaseInfo
{
    public int Index { get; protected set; }
    public int Id { get; protected set; }
    public string? FilterId { get; protected set; }

    private FilterResult? _matchingFilter;
    public FilterResult? MatchingFilter => this.GetOrSetValueAsync(() => Task.Run(() => NetshHelper.FindMatchingFilterInfo(int.Parse(FilterId))), ref _matchingFilter, OnPropertyChanged);
    public string? Reason { get; protected set; }
    public string? Message { get; protected set; }
    public bool IsAllowed { get; private set; }


    public static bool TryCreateFromEventLogEntry<T>(EventLogEntry entry, int index, out T? view) where T : LoggedConnection, new()
    {
        if (entry is null)
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
            path = (path == "-" ? "System" : PathResolver.ResolvePath(path));

            var fileName = System.IO.Path.GetFileName(path);

            // try to get the servicename from pid (works only if service is running)
            //TODO: set this as optional according to the EnableServiceResolution settings (have to check impacts first)
            var serviceName = ServiceNameResolver.GetServiceInfo(pid, fileName);

            var le = new T()
            {
                Index = index,
                Id = entry.Index,
                Pid = pid,
                CreationTime = entry.TimeGenerated,
                Path = path,
                FileName = fileName,
                ServiceName = serviceName?.Name,
                ServiceDisplayName = serviceName?.DisplayName,
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

            le.IsAllowed = !le.Reason.StartsWith("Block");

            le.SetProductInfo();

            view = le;

            return true;
        }
        catch (Exception ex)
        {
            LogHelper.Error("Cannot parse eventlog entry: eventID=" + entry.InstanceId.ToString(), ex);

            view = null;
            return false;
        }
    }

    private static string? GetReplacementString(EventLogEntry entry, int i)
    {
        return entry.ReplacementStrings.DefaultIfEmpty(string.Empty).ElementAtOrDefault(i);
    }

    public static LoggedConnection? CreateFromEventLogEntry(EventLogEntry entry, int index)
    {
        return TryCreateFromEventLogEntry(entry, index, out LoggedConnection? ret) ? ret : null;
    }
}
