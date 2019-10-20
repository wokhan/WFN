using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;


namespace Harrwiss.Common.Network.Helper
{
    [TestClass]
    public class DnsResolverTest
    {
        [TestMethod]
        public void TestDnsResolverResolveIpAddresses()
        {
            // Hostname -> IP lookup: https://whatismyipaddress.com/hostname-ip
            List<string> ipList = new List<string>
            {
                "52.200.121.83",  // origin on ec2-52-200-121-83.compute-1.amazonaws.com
                "8.8.8.8", // dns.google
                "52.200.121.83", // ec2-52-200-121-83.compute-1.amazonaws.com
                "2001:4860:4860::8888", // dns.google
                Dns.GetHostAddresses("www.google.ch").FirstOrDefault().ToString()
            };

            Console.WriteLine("Resolve first 3 entries:");
            Task<bool> t = DnsResolver.ResolveIpAddresses(ipList, maxEntriesToResolve: 4);
            t.Wait();
            LogDictEntries();
            Assert.AreEqual("dns.google", DnsResolver.CachedIPHostEntryDict[IPAddress.Parse("8.8.8.8")].HostEntry.HostName);
            Assert.IsTrue(DnsResolver.CachedIPHostEntryDict.Values.Count == 4);

            ipList = new List<string>
            {
                "2001:4860:4860::8888", // dns.google
                "172.217.5.195", // lax28s10-in-f195.1e100.net (www.google.ch)
                "23.211.5.15", // a23-211-5-15.deploy.static.akamaitechnologies.com
                "1.78.64.10", // sp1-78-64-10.msa.spmode.ne.jp
            };
            Console.WriteLine("Resolve next 3 entries:");
            t = DnsResolver.ResolveIpAddresses(ipList, maxEntriesToResolve: 3);
            t.Wait();
            LogDictEntries();
            Assert.IsTrue(DnsResolver.CachedIPHostEntryDict.Values.Count == 7);
            Assert.AreEqual("dns.google", DnsResolver.CachedIPHostEntryDict[IPAddress.Parse("2001:4860:4860::8888")].HostEntry.HostName);

        }

        public void TestDnsResolverResolveIpAddresses_unresolved()
        {
            List<string> ipList = new List<string>
            {
                // unresolvable ips
                "1.9.1.9", // cdns01.tm.net.my
            };
            Console.WriteLine("Unresolvabe IPs:");
            Task<bool> t = DnsResolver.ResolveIpAddresses(ipList);
            t.Wait();
            LogDictEntries();

            Assert.IsTrue(DnsResolver.CachedIPHostEntryDict.ContainsKey(IPAddress.Parse("1.9.1.9")));
            Assert.IsFalse(DnsResolver.CachedIPHostEntryDict[IPAddress.Parse("1.9.1.9")].IsResolved);
        }

        private static void LogDictEntries()
        {
            foreach (var entry in DnsResolver.CachedIPHostEntryDict)
            {
                Console.WriteLine($"{ entry.Key }: isResolved={entry.Value.IsResolved} hostName={entry.Value.HostEntry.HostName}, tooltipText={entry.Value.ToolTipText}\n");
            }
        }
    }
}
