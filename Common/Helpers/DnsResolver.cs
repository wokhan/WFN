using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Harrwiss.Common.Network.Helper
{
    /// <summary>
    /// Resolves IP addesses to IPHostEntry records asynchronously and caches them in a dictionary.
    /// </summary>
    public class DnsResolver
    {
        private readonly static object syncLock = new object();

        public class CachedIPHostEntry
        {
            public IPHostEntry HostEntry;
            public bool IsResolved = false;
            public string TextHint;
        }
        /// <summary>
        /// Dictionary of resolved IP addresses.
        /// </summary>
        public static Dictionary<IPAddress, CachedIPHostEntry> CachedIPHostEntryDict = new Dictionary<IPAddress, CachedIPHostEntry>();

        public static async Task<bool> ResolveIpAddresses(List<String> ipAddressList, int maxEntriesToResolve = 100)
        {
            List<IPAddress> ipList = new List<IPAddress>();
            ipAddressList.ForEach(s =>
            {
                ipList.Add(IPAddress.Parse(s));
            });
            return await ResolveIpAddresses(ipList, maxEntriesToResolve);
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
            });
        }

        private static void ResolveIP(IPAddress ip)
        {
            lock (syncLock)
            {
                try
                {
                    if (!CachedIPHostEntryDict.ContainsKey(ip))
                    {
                        IPHostEntry resolvedEntry = Dns.GetHostEntry(address: ip);
                        CachedIPHostEntry entry = new CachedIPHostEntry
                        {
                            HostEntry = resolvedEntry,
                            IsResolved = true,
                            TextHint = resolvedEntry.HostName
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
                        TextHint = e.Message
                    };
                    PutEntry(ip, entry);
                }
            }
        }

        private static void PutEntry(IPAddress ip, CachedIPHostEntry entry)
        {
            lock (syncLock)
            {
                if (CachedIPHostEntryDict.ContainsKey(ip))
                {
                    CachedIPHostEntryDict.Remove(ip);
                    CachedIPHostEntryDict.Add(ip, entry);
                }
                else
                {
                    CachedIPHostEntryDict.Add(ip, entry);
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
