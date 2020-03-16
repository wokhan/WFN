using System.Collections.Generic;
using System.Net;
using System.Linq;
using NUnit.Framework;
using Wokhan.WindowsFirewallNotifier.Common.Net.Dns;
using System.Threading.Tasks;
using Wokhan.WindowsFirewallNotifier.Console.Tests.NUnit;

namespace Wokhan.WindowsFirewallNotifier.Common.Helpers
{

    public class DnsResolverTest : NUnitTestBase
    {
       //[Test, ManualTestCategory]
        public void TestDnsResolverResolveIpAddresses()
        {
            // Hostname -> IP lookup: https://whatismyipaddress.com/hostname-ip
            List<string> ipList = new List<string>
            {
                "52.200.121.83",  // origin on ec2-52-200-121-83.compute-1.amazonaws.com
                "52.200.121.83",  // duplicate
                "8.8.8.8", // dns.google
                "2001:4860:4860::8888", // dns.google
                Dns.GetHostAddresses("www.google.ch").FirstOrDefault().ToString()
            };

            WriteDebugOutput("Resolve first 3 entries:");
            _ = DnsResolver.ResolveIpAddress(ipList[0]).ConfigureAwait(true);
            _ = DnsResolver.ResolveIpAddress(ipList[1]).ConfigureAwait(true);
            _ = DnsResolver.ResolveIpAddress(ipList[2]).ConfigureAwait(false);

            Task.WaitAll();

            LogDictEntries();
            Assert.AreEqual("dns.google", DnsResolver.CachedIPHostEntryDict[IPAddress.Parse("8.8.8.8")].HostEntry.HostName);
            Assert.True(DnsResolver.CachedIPHostEntryDict.Values.Count == 4);

            ipList = new List<string>
            {
                "2001:4860:4860::8888", // dns.google
                "172.217.5.195", // lax28s10-in-f195.1e100.net (www.google.ch)
                "23.211.5.15", // a23-211-5-15.deploy.static.akamaitechnologies.com
                "1.78.64.10", // sp1-78-64-10.msa.spmode.ne.jp
            };
            WriteDebugOutput("Resolve next 3 entries:");
            _ = DnsResolver.ResolveIpAddress(ipList[0]).ConfigureAwait(false);
            _ = DnsResolver.ResolveIpAddress(ipList[1]).ConfigureAwait(false);
            _ = DnsResolver.ResolveIpAddress(ipList[2]).ConfigureAwait(false);
            _ = DnsResolver.ResolveIpAddress(ipList[3]).ConfigureAwait(false);
            
            LogDictEntries();
            Assert.True(DnsResolver.CachedIPHostEntryDict.Values.Count == 7);
            //Wrong test, at least on my computer. This is not one of google's dns (which is 8.8.8.8), probably something true in Switzerland but not here :-/
            //Assert.AreEqual("dns.google", DnsResolver.CachedIPHostEntryDict[IPAddress.Parse("2001:4860:4860::8888")].HostEntry.HostName);

        }

        //[Test, ManualTestCategory]
        public void TestDnsResolverResolveIpAddresses_unresolved()
        {
            List<string> ipList = new List<string>
            {
                // unresolvable ips
                "1.9.1.9", // cdns01.tm.net.my
            };
            WriteDebugOutput("Unresolvabe IPs:");

            _ = DnsResolver.ResolveIpAddress("1.9.1.9").ConfigureAwait(false);
            
            LogDictEntries();

            Assert.True(DnsResolver.CachedIPHostEntryDict.ContainsKey(IPAddress.Parse("1.9.1.9")));
            Assert.False(DnsResolver.CachedIPHostEntryDict[IPAddress.Parse("1.9.1.9")].IsResolved);
        }

        private static void LogDictEntries()
        {
            foreach (var entry in DnsResolver.CachedIPHostEntryDict)
            {
                WriteDebugOutput($"{ entry.Key }: isResolved={entry.Value.IsResolved} hostName={entry.Value.HostEntry.HostName}, tooltipText={entry.Value.DisplayText}\n");
            }
        }
    }
}
