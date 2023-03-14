using System;
using System.Collections.Concurrent;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using Wokhan.WindowsFirewallNotifier.Common.Logging;

/// <summary>
/// DnsResolver resolves IP addesses to IPHostEntry records asynchronously and caches them in a dictionary.
/// Author (initial version): harrwiss / Nov 2019
/// Rewritten by wokhan
/// </summary>
namespace Wokhan.WindowsFirewallNotifier.Common.Net.DNS;

public class ResolvedIPInformation
{
    
    private SemaphoreSlim semaphore = new(1);
    private IPAddress ip;
    private bool isResolving = false;

    public IPHostEntry? HostEntry { get; private set; }

    /// <summary>
    /// Gets the resolved status of an ip address - a resolved entry can have <see cref="HasErrors"/>
    /// </summary>
    public bool IsResolved { get; private set; } = false;

    /// <summary>
    /// Returns true if an ip address could not be resolved to a host name.
    /// </summary>
    public bool HasErrors { get; private set; } = false;

    /// <summary>
    /// Text displayed on the ui - may also return an error message.
    /// </summary>
    public string DisplayText { get; private set; } = "...";

    public static readonly ConcurrentDictionary<IPAddress, ResolvedIPInformation> CachedIPHostEntryDict = new();

    public static async Task<string?> ResolveIpAddressAsync(string ip)
    {
        if (String.IsNullOrEmpty(ip))
        {
            return String.Empty;
        }

        if (!IPAddress.TryParse(ip, out var ipa))
        {
            return "N/A";
        }

        var entry = CachedIPHostEntryDict.GetOrAdd(ipa, sourceIp => new ResolvedIPInformation(sourceIp));

        // Ensure that it has been resolved
        await entry.WaitForResolution();

        return entry.DisplayText;
    }

    public ResolvedIPInformation(IPAddress ip)
    {
        this.ip = ip;
    }

    private async Task WaitForResolution()
    {
        if (isResolving)
        {
            await semaphore.WaitAsync();
        }
        
        if (IsResolved || HasErrors)
        {
            return;
        }

        isResolving = true;
        
        try
        {
            HostEntry = await Dns.GetHostEntryAsync(ip);

            IsResolved = true;
            DisplayText = HostEntry.HostName;
        }
        catch (Exception e)
        {
            LogHelper.Debug($"Unable to resolve {ip}, message was {e.Message}");

            HostEntry = new IPHostEntry
            {
                HostName = "unknown",
                AddressList = ip != IPAddress.None ? new[] { ip } : Array.Empty<IPAddress>()
            };
            IsResolved = false;
            HasErrors = true;
            DisplayText = "N/A";
        }

        // Releases all waiting entry resolutions
        while (semaphore.Release() > 1);

        isResolving = false;
    }
}
