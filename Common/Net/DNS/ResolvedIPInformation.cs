using System;
using System.Collections.Concurrent;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using Wokhan.WindowsFirewallNotifier.Common.Logging;

namespace Wokhan.WindowsFirewallNotifier.Common.Net.DNS;

public class ResolvedIPInformation
{
    internal static readonly ConcurrentDictionary<string, ResolvedIPInformation> CachedIPHostEntryDict = new();

    private readonly ManualResetEventSlim _eventSlim = new ManualResetEventSlim(true);

    private readonly string ip;

    internal bool handled;

    internal string resolvedHost = "N/A";

    public static async Task<string?> ResolveIpAddressAsync(string ip)
    {
        if (String.IsNullOrEmpty(ip))
        {
            return String.Empty;
        }

        var entry = CachedIPHostEntryDict.GetOrAdd(ip, sourceIp => new ResolvedIPInformation(sourceIp));

        // Ensure that it has been resolved
        return await entry.WaitOrResolveHostAsync();
    }

    public ResolvedIPInformation(string ip)
    {
        this.ip = ip;
    }

    private async Task<string> WaitOrResolveHostAsync()
    {
        _eventSlim.Wait();

        if (!handled)
        {
            _eventSlim.Reset();

            try
            {
                resolvedHost = (await Dns.GetHostEntryAsync(ip)).HostName;
            }
            catch (Exception e)
            {
                LogHelper.Debug($"Unable to resolve {ip}, message was {e.Message}");
            }

            handled = true;

            // Releases all pending entry resolutions in other threads for this entry
            _eventSlim.Set();
        }

        return resolvedHost;
    }
}
