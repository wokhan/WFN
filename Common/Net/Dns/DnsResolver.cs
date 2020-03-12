using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Wokhan.Collections;
using Wokhan.WindowsFirewallNotifier.Common.Helpers;

/// <summary>
/// DnsResolver resolves IP addesses to IPHostEntry records asynchronously and caches them in a dictionary.
/// Author: harrwiss / Nov 2019
/// </summary>
namespace Wokhan.WindowsFirewallNotifier.Common.Net.Dns
{

    /// <summary>
    /// Resolves IP addesses to IPHostEntry records asynchronously and caches them in a dictionary.
    /// </summary>
    public static class DnsResolver
    {
        /// <summary>
        /// Dictionary of resolved IP addresses.
        /// </summary>
        public static readonly ObservableDictionary<IPAddress, CachedIPHostEntry> CachedIPHostEntryDict = new ObservableDictionary<IPAddress, CachedIPHostEntry>();

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "<Pending>")]
        public static async Task<bool?> ResolveIpAddress(string ip, Action<CachedIPHostEntry> callback = null)
        {
            return await Task.Run(() =>
            {
                if (!IsIPValid(ip))
                {
                    callback?.Invoke(CachedIPHostEntry.EMTPY);
                    return false;
                }

                IPAddress ipa = null;
                try
                {
                    ipa = IPAddress.Parse(ip);
                    if (CachedIPHostEntryDict.TryGetValue(ipa, out var entry))
                    {
                        if (entry.IsResolved || entry.HasErrors)
                        {
                            callback?.Invoke(entry);
                            return true;
                        }

                        NotifyCollectionChangedEventHandler add = (sender, args) =>
                        {
                            if (args.Action == NotifyCollectionChangedAction.Replace)
                            {
                                var entry = (KeyValuePair<IPAddress, CachedIPHostEntry>)args.NewItems[0];
                                if (entry.Key == ipa)
                                    callback?.Invoke(entry.Value);
                            }
                        };
                        CachedIPHostEntryDict.CollectionChanged += add;
                        CachedIPHostEntryDict.CollectionChanged += (s, e) => CachedIPHostEntryDict.CollectionChanged -= add;
                        return (bool?)null;
                    }
                    else
                    {
                        PutEntry(ipa, CachedIPHostEntry.RESOLVING);  // reserve slot
                        callback?.Invoke(CachedIPHostEntry.RESOLVING);

                        // http://www.dotnetframework.org/default.aspx/4@0/4@0/DEVDIV_TFS/Dev10/Releases/RTMRel/ndp/fx/src/Net/System/Net/DNS@cs/1305376/DNS@cs
                        IPHostEntry resolvedEntry = System.Net.Dns.GetHostEntry(ipa);
                        PutEntry(ipa, CachedIPHostEntry.WrapHostEntry(resolvedEntry));
                        callback?.Invoke(CachedIPHostEntryDict[ipa]);
                        return true;
                    }
                }
                catch (Exception e)
                {
                    LogHelper.Debug($"Unable to resolve {ip}, message was {e.Message}");
                    
                    var ret = CachedIPHostEntry.CreateErrorEntry(ipa, e);
                    PutEntry(ipa, ret);
                    callback?.Invoke(ret);

                    return false;
                }
            }).ConfigureAwait(false);
        }

        internal static bool IsIPValid(string ip) => !string.IsNullOrWhiteSpace(ip) && ip != "0.0.0.0" && ip != "::" && ip != "::0";

        private static void PutEntry(IPAddress ip, CachedIPHostEntry entry)
        {
            lock (CachedIPHostEntryDict)
            {
                if (CachedIPHostEntryDict.TryAdd(ip, entry))
                {
                    LogHelper.Debug($"Start resolve IPHostEntry for {ip}: IsResolved={entry.IsResolved}, HasErrors={entry.HasErrors}, ToolTipText={entry.DisplayText}");
                }
                else
                {
                    CachedIPHostEntryDict[ip] = entry;
                    LogHelper.Debug($"End resolve IPHostEntry for {ip}: IsResolved={entry.IsResolved}, HasErrors={entry.HasErrors}, ToolTipText={entry.DisplayText}");
                }
            }
        }
    }
}
