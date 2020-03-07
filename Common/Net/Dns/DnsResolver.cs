using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
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
        public static readonly Dictionary<IPAddress, CachedIPHostEntry> CachedIPHostEntryDict = new Dictionary<IPAddress, CachedIPHostEntry>();

        public static async Task ResolveIpAddresses(IEnumerable<string> ipAddressList, int maxEntriesToResolve = 1000)
        {
            if (ipAddressList == null)
            {
                return;
            }

            var ipList = ipAddressList.AsParallel()
                                     .Select(s => IPAddress.TryParse(s, out IPAddress parsedIP) ? parsedIP : LogHelper.WarnAndReturn<IPAddress>($"Cannot parse IP {s}", null))
                                     .ToList();

            await ResolveIpAddresses(ipList, maxEntriesToResolve).ConfigureAwait(false);
        }
        /// <summary>
        /// Resolves given ip addresses to IPHostEntry and stores them in CachedIPHostEntryDict.
        /// </summary>
        /// <param name="ipAddressList">IP address list to resolve</param>
        /// <param name="maxEntriesToResolve">Max entries to resolve for this task</param>
        /// <returns></returns>
        public static async Task ResolveIpAddresses(IEnumerable<IPAddress> ipAddressList, int maxEntriesToResolve = 1000)
        {
            await Task.Run(() =>
            {
                ipAddressList.Take(maxEntriesToResolve)
                             .AsParallel()
                             .ForAll(ip => ResolveIP(ip));
            }).ConfigureAwait(false);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "<Pending>")]
        public static async Task<CachedIPHostEntry> ResolveIpAddress(string ip)
        {
            return await Task.Run(() =>
            {
                if (!IsIPValid(ip))
                {
                    return CachedIPHostEntry.EMTPY;
                }
                IPAddress ipa = null;
                try
                {
                    ipa = IPAddress.Parse(ip);
                    ResolveIP(ipa);
                    return CachedIPHostEntryDict[ipa];
                } catch (Exception e)
                {
                    return CachedIPHostEntry.CreateErrorEntry(ipa, e);
                }
            }).ConfigureAwait(false);
        }

        internal static bool IsIPValid(string ip) => !string.IsNullOrWhiteSpace(ip) && ip != "0.0.0.0" && ip != "::" && ip != "::0";

        public static string getResolvedHostName(string ipAddress)
        {
            if (!IsIPValid(ipAddress))
            {
                return "invalid ip";
            }
            CachedIPHostEntryDict.TryGetValue(IPAddress.Parse(ipAddress), out CachedIPHostEntry ipHost);
            if (ipHost == null)
            {
                return "unkown host";
            }
            return ipHost.DisplayText;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "<Pending>")]
        private static void ResolveIP(IPAddress ip)
        {
            try
            {
                if (!CachedIPHostEntryDict.ContainsKey(ip))
                {
                    PutEntry(ip, CachedIPHostEntry.EMTPY);  // reserve slot
                    // http://www.dotnetframework.org/default.aspx/4@0/4@0/DEVDIV_TFS/Dev10/Releases/RTMRel/ndp/fx/src/Net/System/Net/DNS@cs/1305376/DNS@cs
                    IPHostEntry resolvedEntry = System.Net.Dns.GetHostEntry(address: ip);
                    var entry = new CachedIPHostEntry
                    {
                        HostEntry = resolvedEntry,
                        IsResolved = true,
                        DisplayText = resolvedEntry.HostName
                    };
                    PutEntry(ip, entry);
                }
            }
            catch (Exception e)
            {
                PutEntry(ip, CachedIPHostEntry.CreateErrorEntry(ip, e));
            }
        }

        private static void PutEntry(IPAddress ip, CachedIPHostEntry entry)
        {
            lock (CachedIPHostEntryDict)
            {
                if (CachedIPHostEntryDict.ContainsKey(ip))
                {
                    CachedIPHostEntryDict[ip] = entry;
                    LogHelper.Debug($"End resolve IPHostEntry for {ip}: IsResolved={entry.IsResolved}, HasErrors={entry.HasErrors}, ToolTipText={entry.DisplayText}");
                }
                else
                {
                    CachedIPHostEntryDict.Add(ip, entry);
                    LogHelper.Debug($"Start resolve IPHostEntry for {ip}: IsResolved={entry.IsResolved}, HasErrors={entry.HasErrors}, ToolTipText={entry.DisplayText}");
                }
            }
        }
    }
}
