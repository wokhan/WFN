using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

using Wokhan.WindowsFirewallNotifier.Common.Logging;

namespace Wokhan.WindowsFirewallNotifier.Common.Net.DNS;

public class ResolvedIPInformation : IDisposable
{
    internal static readonly ConcurrentDictionary<string, ResolvedIPInformation> CachedIPHostEntryDict = new();

    private readonly ManualResetEventSlim _eventSlim = new(true);

    private readonly string ip;

    internal bool handled;

    internal string resolvedHost = "N/A";

    public static async Task<string?> ResolveIpAddressAsync(string ip)
    {
        if (String.IsNullOrEmpty(ip))
        {
            return String.Empty;
        }

        // If using GetHostEntryAsync in WaitOrResolveHost, SocketExceptions are never catched back and a deadlock occurs...
        // So we've to initiate the async task ourselves.
        return await Task.Run(() =>
        {
            var entry = CachedIPHostEntryDict.GetOrAdd(ip, sourceIp => new ResolvedIPInformation(sourceIp));

            if (entry.handled)
            {
                return entry.resolvedHost;
            }

            // Ensure that it has been resolved
            return entry.WaitOrResolveHost();
        });
    }

    public ResolvedIPInformation(string ip)
    {
        this.ip = ip;
    }

    private string WaitOrResolveHost()
    {
        try
        {
            _eventSlim.Wait();

            if (!handled)
            {
                _eventSlim.Reset();

                resolvedHost = Dns.GetHostEntry(ip)?.HostName ?? "N/A";
            }
        }
        catch (SocketException se)
        {
            resolvedHost = se.Message;
        }
        catch (Exception e)
        {
            LogHelper.Debug($"Unable to resolve {ip}, message was {e.Message}");
        }
        finally
        {

            handled = true;

            // Releases all pending entry resolutions in other threads for this entry
            // TODO: _eventSlim might need to be disposed once the hostname resolution is done and after no thread is waiting... Could induce an unjustified memory use if noe?
            _eventSlim.Set();
        }

        return resolvedHost;
    }

    public void Dispose()
    {
        _eventSlim.Dispose();
    }
}
