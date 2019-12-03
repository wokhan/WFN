using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Wokhan.WindowsFirewallNotifier.Common.Helpers;

/// <summary>
/// DnsResolver resolves IP addesses to IPHostEntry records asynchronously and caches them in a dictionary.
/// Author: harrwiss / Nov 2019
/// </summary>
namespace Wokhan.WindowsFirewallNotifier.Common.Helpers
{
    /// <summary>
    /// An ip host entry for the dictionary.
    /// </summary>
    public class CachedIPHostEntry
    {
        public readonly static CachedIPHostEntry EMTPY = new CachedIPHostEntry();

        internal static Func<IPAddress, Exception, CachedIPHostEntry> ERROR_ENTRY = (ip, e) =>
        {
            CachedIPHostEntry entry = new CachedIPHostEntry
            {
                HostEntry = new IPHostEntry
                {
                    HostName = "unknown",
                    AddressList = ip != null ? new IPAddress[] { ip } : new IPAddress[] { }
                },
                IsResolved = false,
                HasErrors = true,
                DisplayText = e.Message
            };
            return entry;
        };

        public IPHostEntry HostEntry { get; set; } = new IPHostEntry()
        {
            HostName = "unknown",
            AddressList = new IPAddress[] { }
        };

        /// <summary>
        /// Gets the resolved status of an ip address - a resolved entry can have <see cref="HasErrors"/>
        /// </summary>
        public bool IsResolved { get; set; } = false;

        /// <summary>
        /// Returns true if an ip address could not be resolved to a host name.
        /// </summary>
        public bool HasErrors { get; set; } = false;
        
        /// <summary>
        /// Text displayed on the ui - may also return an error message.
        /// </summary>
        public string DisplayText { get; set; } = "...";
    }

    /// <summary>
    /// Resolves IP addesses to IPHostEntry records asynchronously and caches them in a dictionary.
    /// </summary>
    public static class DnsResolver
    {
        /// <summary>
        /// Dictionary of resolved IP addresses.
        /// </summary>
        public static readonly Dictionary<IPAddress, CachedIPHostEntry> CachedIPHostEntryDict = new Dictionary<IPAddress, CachedIPHostEntry>();

        public static async Task<bool> ResolveIpAddresses(List<String> ipAddressList, int maxEntriesToResolve = 1000)
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
        public static async Task<bool> ResolveIpAddresses(List<IPAddress> ipAddressList, int maxEntriesToResolve = 1000)
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
                    return CachedIPHostEntry.ERROR_ENTRY(ipa, e);
                }
            }).ConfigureAwait(false);
        }

        internal static bool IsIPValid(string ip)
        {
            if (string.IsNullOrWhiteSpace(ip) || ip == "0.0.0.0" || ip == "::" || ip == "::0")
            {
                return false;
            }
            return true;
        }

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
                    IPHostEntry resolvedEntry = Dns.GetHostEntry(address: ip);
                    CachedIPHostEntry entry = new CachedIPHostEntry
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
                PutEntry(ip, CachedIPHostEntry.ERROR_ENTRY(ip, e));
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
