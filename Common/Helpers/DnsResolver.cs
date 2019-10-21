using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Wokhan.WindowsFirewallNotifier.Common.Helpers;

namespace Harrwiss.Common.Network.Helper
{
    public class CachedIPHostEntry
    {
        public readonly static CachedIPHostEntry EMTPY = new CachedIPHostEntry();

        public IPHostEntry HostEntry { get; set; } = new IPHostEntry()
        {
            HostName = "unknown",
            AddressList = new IPAddress[] { }
        };
        public bool IsResolved { get; set; } = false;
        public string ToolTipText { get; set; } = "...";
    }

    /// <summary>
    /// Resolves IP addesses to IPHostEntry records asynchronously and caches them in a dictionary.
    /// </summary>
    public static class DnsResolver
    {
        /// <summary>
        /// Dictionary of resolved IP addresses.
        /// </summary>
        public static Dictionary<IPAddress, CachedIPHostEntry> CachedIPHostEntryDict = new Dictionary<IPAddress, CachedIPHostEntry>();

        public static async Task<bool> ResolveIpAddresses(List<String> ipAddressList, int maxEntriesToResolve = 100)
        {
            if (ipAddressList == null)
            {
                return true;
            }
            List<IPAddress> ipList = new List<IPAddress>();
            ipAddressList.ForEach(s =>
            {
                if (IPAddress.TryParse(s, out IPAddress parsedIP))
                {
                    ipList.Add(parsedIP);
                }
                else
                {
                    LogHelper.Warning($"Cannot parse IP {s}");
                }
            });
            return await ResolveIpAddresses(ipList, maxEntriesToResolve).ConfigureAwait(false);
        }
        /// <summary>
        /// Resolves given ip addresses to IPHostEntry and stores them in CachedIPHostEntryDict.
        /// </summary>
        /// <param name="ipAddressList">IP address list to resolve</param>
        /// <param name="maxEntriesToResolve">Max entries to resolve for this task</param>
        /// <returns></returns>
        public static async Task<bool> ResolveIpAddresses(List<IPAddress> ipAddressList, int maxEntriesToResolve = 100)
        {
            return await Task.Run(() =>
            {
                int entryCnt = 0;
                ipAddressList.ForEach(ip => {
                    if (entryCnt++ <= maxEntriesToResolve)
                    {
                        ResolveIP(ip);
                    }
                });
                return true;
            }).ConfigureAwait(false);
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
                    IPHostEntry resolvedEntry = Dns.GetHostEntry(address: ip);
                    CachedIPHostEntry entry = new CachedIPHostEntry
                    {
                        HostEntry = resolvedEntry,
                        IsResolved = true,
                        ToolTipText = resolvedEntry.HostName
                    };
                    PutEntry(ip, entry);
                }
            }
            catch (Exception e)
            {
                CachedIPHostEntry entry = new CachedIPHostEntry
                {
                    HostEntry = new IPHostEntry
                    {
                        HostName = "unknown",
                        AddressList = new IPAddress[] { ip }
                    },
                    IsResolved = false,
                    ToolTipText = e.Message
                };
                PutEntry(ip, entry);
            }
        }

        private static void PutEntry(IPAddress ip, CachedIPHostEntry entry)
        {
            lock (CachedIPHostEntryDict)
            {
                if (CachedIPHostEntryDict.ContainsKey(ip))
                {
                    CachedIPHostEntryDict[ip] = entry;
                    LogHelper.Debug($"End resolve IPHostEntry for {ip}: IsResolved={entry.IsResolved}, ToolTipText={entry.ToolTipText}");
                }
                else
                {
                    CachedIPHostEntryDict.Add(ip, entry);
                    LogHelper.Debug($"Start resolve IPHostEntry for {ip}: IsResolved={entry.IsResolved}, ToolTipText={entry.ToolTipText}");
                }
            }
        }
    }

    public static class IPAddressExtensions
    {
        /// <summary>
        /// Converts a string representing a host name or address to its <see cref="IPAddress"/> representation, 
        /// optionally opting to return a IpV6 address (defaults to IpV4)
        /// </summary>
        /// <param name="hostNameOrAddress">Host name or address to convert into an <see cref="IPAddress"/></param>
        /// <param name="favorIpV6">When <code>true</code> will return an IpV6 address whenever available, otherwise 
        /// returns an IpV4 address instead.</param>
        /// <returns>The <see cref="IPAddress"/> represented by <paramref name="hostNameOrAddress"/> in either IpV4 or
        /// IpV6 (when available) format depending on <paramref name="favorIpV6"/></returns>
        public static IPAddress IPAddress(this string hostNameOrAddress, bool favorIpV6 = false)
        // see How to write Extension Methods: https://www.tutorialsteacher.com/csharp/csharp-extension-method
        {
            var favoredFamily = favorIpV6 ? AddressFamily.InterNetworkV6 : AddressFamily.InterNetwork;
            var addrs = Dns.GetHostAddresses(hostNameOrAddress);
            return addrs.FirstOrDefault(addr => addr.AddressFamily == favoredFamily)
                   ??
                   addrs.FirstOrDefault();

        }

    }
}
