using AlphaChiTech.Virtualization;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Wokhan.WindowsFirewallNotifier.Common.Logging;
using System.Diagnostics;

namespace Wokhan.WindowsFirewallNotifier.Common.Security
{
    public static class EventLogAsyncReader
    {
        public static readonly string EVENTLOG_SECURITY = "security";


        public static bool IsFirewallEventSimple(long instanceId)
        {
            // https://docs.microsoft.com/en-us/windows/security/threat-protection/auditing/audit-filtering-platform-connection
            return instanceId == 5156
                || instanceId == 5157 // block connection
                || instanceId == 5152;// drop packet
        }

        public static bool IsFirewallEvent(long instanceId)
        {
            // https://docs.microsoft.com/en-us/windows/security/threat-protection/auditing/audit-filtering-platform-connection
            //5031: The Windows Firewall Service blocked an application from accepting incoming connections on the network.
            //5150: The Windows Filtering Platform blocked a packet.
            //5151: A more restrictive Windows Filtering Platform filter has blocked a packet.
            //5154: The Windows Filtering Platform has permitted an application or service to listen on a port for incoming connections.
            //5155: The Windows Filtering Platform has blocked an application or service from listening on a port for incoming connections.
            //5156: The Windows Filtering Platform has permitted a connection.
            //5157: The Windows Filtering Platform has blocked a connection.
            //5158: The Windows Filtering Platform has permitted a bind to a local port.
            //5159: The Windows Filtering Platform has blocked a bind to a local port.
            return instanceId == 5157
                   || instanceId == 5152
                   // Cannot parse this event 
                   // || instanceId == 5031
                   || instanceId == 5150
                   || instanceId == 5151
                   || instanceId == 5154
                   || instanceId == 5155
                   || instanceId == 5156;
        }

        public static string GetEventInstanceIdAsString(long instanceId)
        {
            // https://docs.microsoft.com/en-us/windows/security/threat-protection/auditing/audit-filtering-platform-connection
            switch (instanceId)
            {
                case 5157:
                    return "Block: connection";
                case 5152:
                    return "Block: packet (WFP)";
                case 5031:
                    return "Block: app connection"; //  Firewall blocked an application from accepting incoming connections on the network.
                case 5150:
                    return "Block: packet";
                case 5151:
                    return "Block: packet (other FW)";
                case 5154:
                    return "Allow: listen";
                case 5155:
                    return "Block: listen";
                case 5156:
                    return "Allow: connection";
                default:
                    return $"[UNKNOWN] eventId: {instanceId}";
            }
        }

        public static bool IsFirewallEventAllowed(long instanceId)
        {
            return instanceId == 5156;
        }
    }

    public class EventLogAsyncReader<T> : IPagedSourceProviderAsync<T>, IDisposable where T : class, new()
    {
        public Func<long, bool>? FilterPredicate { get; set; }

        public event EntryWrittenEventHandler EntryWritten;

        private EventLog eventLog;

        public EventLogAsyncReader(string eventLogName, Func<EventLogEntry, T?> projection)
        {
            _projection = projection;
         
            eventLog = new EventLog(eventLogName);
            eventLog.BeginInit();
            eventLog.EntryWritten += DefaultEntryWrittenHandler;
            if (EntryWritten != null)
            {
                eventLog.EntryWritten += EntryWritten;
            }
            eventLog.EnableRaisingEvents = true;
            eventLog.EndInit();

            Entries = new VirtualizingObservableCollection<T>(this);
        }


        private Func<EventLogEntry, T?> _projection;
        public Func<int, T>? PlaceHolderCreator { get; set; }
        private Dictionary<int, int> filteredPagesMap = new Dictionary<int, int>();

        public int Count => eventLog.Entries.Count;

        public int CurrentPageOffset { get; private set; }

        public void OnReset(int count)
        {
            newEntriesOffset = 0;
            filteredPagesMap.Clear();
        }

        public int IndexOf(T item)
        {
            return 0;
        }

        public async Task<PagedSourceItemsPacket<T>> GetItemsAtAsync(int pageoffset, int count, bool usePlaceholder)
        {
            return await Task.Run(() => GetItemsAt(pageoffset, count, usePlaceholder)).ConfigureAwait(false);
        }

        T placeHolder = new T();
        private int newEntriesOffset;
        
        public VirtualizingObservableCollection<T> Entries { get; }

        public T GetPlaceHolder(int index, int page, int offset)
        {
            return PlaceHolderCreator?.Invoke(index) ?? placeHolder;
        }

        public async Task<int> GetCountAsync()
        {
            return await Task.FromResult(Count).ConfigureAwait(false);
        }

        public async Task<int> IndexOfAsync(T item) => 0;

        public PagedSourceItemsPacket<T> GetItemsAt(int pageoffset, int count, bool usePlaceholder)
        {
            pageoffset = filteredPagesMap.GetValueOrDefault(pageoffset, pageoffset);

            CurrentPageOffset = pageoffset;

            var ret = new PagedSourceItemsPacket<T>();

            ret.LoadedAt = DateTime.Now;
            if (usePlaceholder)
            {
                ret.Items = Enumerable.Range(0, count).Select(_ => placeHolder);
            }
            else
            {
                ret.Items = GetAsEnumerable(pageoffset, count);
            }

            return ret;
        }

        private IEnumerable<T> GetAsEnumerable(int pageoffset, int count)
        {
            int cpt = 0;
            for (var i = pageoffset + 1; i < eventLog.Entries.Count && cpt < count; i++)
            {
                T? ret = null;
                try
                {
                    var entry = eventLog.Entries[^(i + newEntriesOffset)];
                    if (EventLogAsyncReader.IsFirewallEvent(entry.InstanceId))
                    {
                        cpt++;
                        if (cpt == count - 1)
                        {
                            filteredPagesMap.Add(pageoffset + count, i);
                        }

                        ret = _projection(entry);
                    }
                    else
                    {
                        continue;
                    }
                }
                catch// (IndexOutOfRangeException)
                {
                    // Ignore
                    continue;
                }

                if (ret is object)
                    yield return ret;
            }
        }

        private void DefaultEntryWrittenHandler(object sender, EntryWrittenEventArgs e)
        {
            newEntriesOffset++;

            if (!FilterPredicate?.Invoke(e.Entry.InstanceId) ?? false)
            {
                return;
            }
        }

        public void Dispose()
        {
            LogHelper.Debug($"AsyncTaskRunner: Disposing resources...");
            if (eventLog != null)
            {
                eventLog.Dispose();
            }
        }


        public void StopRaisingEvents()
        {
            eventLog.EnableRaisingEvents = false;
        }

        public void StartRaisingEvents()
        {
            eventLog.EnableRaisingEvents = true;
        }
    }
}
