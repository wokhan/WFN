using System;
using System.Diagnostics.Contracts;
using System.Net;

/// <summary>
/// DnsResolver resolves IP addesses to IPHostEntry records asynchronously and caches them in a dictionary.
/// Author: harrwiss / Nov 2019
/// </summary>
namespace Wokhan.WindowsFirewallNotifier.Common.Net.Dns
{
    /// <summary>
    /// An ip host entry for the dictionary.
    /// </summary>
    public class CachedIPHostEntry
    {
        public static readonly CachedIPHostEntry EMTPY = new CachedIPHostEntry();
        public static readonly CachedIPHostEntry RESOLVING = new CachedIPHostEntry() { DisplayText = "..." };

        internal static CachedIPHostEntry CreateErrorEntry(IPAddress ip, Exception e)
        {
            return new CachedIPHostEntry
            {
                HostEntry = new IPHostEntry
                {
                    HostName = "unknown",
                    AddressList = ip != null ? new IPAddress[] { ip } : Array.Empty<IPAddress>()
                },
                IsResolved = false,
                HasErrors = true,
                DisplayText = e.Message
            };
        }

        public static CachedIPHostEntry WrapHostEntry(IPHostEntry resolvedEntry)
        {
            Contract.Requires(!(resolvedEntry is null));

            return new CachedIPHostEntry
            {
                HostEntry = resolvedEntry,
                IsResolved = true,
                DisplayText = resolvedEntry.HostName
            };
        }

        public IPHostEntry HostEntry { get; set; } = new IPHostEntry()
        {
            HostName = "unknown",
            AddressList = Array.Empty<IPAddress>()
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
        public string DisplayText { get; set; } = "?";
    }
}
