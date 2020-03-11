using System.Linq;
using System.Net;
using System.Net.Sockets;

/// <summary>
/// DnsResolver resolves IP addesses to IPHostEntry records asynchronously and caches them in a dictionary.
/// Author: harrwiss / Nov 2019
/// </summary>
namespace Wokhan.WindowsFirewallNotifier.Common.Net.Dns
{
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
            var addrs = System.Net.Dns.GetHostAddresses(hostNameOrAddress);
            return addrs.FirstOrDefault(addr => addr.AddressFamily == favoredFamily)
                   ??
                   addrs.FirstOrDefault();

        }

    }
}
