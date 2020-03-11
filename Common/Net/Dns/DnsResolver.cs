using System;
using System.Collections.Generic;
using System.Collections.Specialized;
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
        public static async void ResolveIpAddress(string ip, Action<CachedIPHostEntry> updateRemoteHostNameCallback)
        {
            await Task.Run(() =>
            {
                if (!IsIPValid(ip))
                {
                    updateRemoteHostNameCallback(CachedIPHostEntry.EMTPY);
                    return;
                }
                IPAddress ipa = null;
                try
                {
                    ipa = IPAddress.Parse(ip);

                    try
                    {
                        if (CachedIPHostEntryDict.TryGetValue(ipa, out var entry))
                        {
                            if (entry.IsResolved)
                            {
                                updateRemoteHostNameCallback(entry);
                                return;
                            }

                            NotifyCollectionChangedEventHandler add = (sender, args) =>
                            {
                                if (args.Action == NotifyCollectionChangedAction.Replace)
                                {
                                    var entry = (KeyValuePair<IPAddress, CachedIPHostEntry>)args.NewItems[0];
                                    if (entry.Key == ipa)
                                        updateRemoteHostNameCallback(entry.Value);
                                }
                            };
                            CachedIPHostEntryDict.CollectionChanged += add;
                            CachedIPHostEntryDict.CollectionChanged += (s, e) => CachedIPHostEntryDict.CollectionChanged -= add;
                        }
                        else
                        {
                            PutEntry(ipa, CachedIPHostEntry.EMTPY);  // reserve slot
                            updateRemoteHostNameCallback(CachedIPHostEntry.EMTPY);

                            // http://www.dotnetframework.org/default.aspx/4@0/4@0/DEVDIV_TFS/Dev10/Releases/RTMRel/ndp/fx/src/Net/System/Net/DNS@cs/1305376/DNS@cs
                            IPHostEntry resolvedEntry = System.Net.Dns.GetHostEntry(ipa);
                            PutEntry(ipa, CachedIPHostEntry.WrapHostEntry(resolvedEntry));
                        }
                    }
                    catch (Exception e)
                    {
                        PutEntry(ipa, CachedIPHostEntry.CreateErrorEntry(ipa, e));
                    }

                    updateRemoteHostNameCallback(CachedIPHostEntryDict[ipa]);
                }
                catch (Exception e)
                {
                    updateRemoteHostNameCallback(CachedIPHostEntry.CreateErrorEntry(ipa, e));
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
