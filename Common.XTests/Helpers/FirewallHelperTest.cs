using System;
using System.Collections.Generic;
using Wokhan.WindowsFirewallNotifier.Common.Helpers;
using static Wokhan.WindowsFirewallNotifier.Common.Helpers.FirewallHelper;
using System.Linq;
using NetFwTypeLib;
using Xunit;

namespace Common.Tests.Helpers
{
    public class FirewallHelperTest
    {
        [Fact]
        public void TestGetMatchingRulesForEvent()
        {
            string exePath = @"C:\Windows\System32\svchost.exe";
            IEnumerable<FirewallHelper.Rule> results = FirewallHelper.GetMatchingRulesForEvent(pid: 0, path: exePath, target: "*", targetPort: "*", blockOnly: false);
            Assert.NotNull(results);
            Assert.True(results.ToList().Count >= 1, "Has no results or number of results does not match");
            foreach (FirewallHelper.Rule rule in results) {
                Console.WriteLine($"{rule.Name}, {rule.RemoteAddresses}");
            }
        }

        [Fact]
        public void TestRuleMatchesEvent()
        {
            IEnumerable<Rule> ret = GetRules(AlsoGetInactive: false);
            string exePath = @"C:\Windows\System32\svchost.exe";
            const int PROF_ALL = (int)NET_FW_PROFILE_TYPE2_.NET_FW_PROFILE2_ALL;
            Console.WriteLine($"{exePath}");
            int cntMatch = 0;
            foreach (Rule rule in ret)
            {
                bool matches = FirewallHelper.RuleMatchesEvent(rule, currentProfile: PROF_ALL, appPkgId: null, svcName: "*", path: exePath, target: "*", remoteport: "*");
                if (matches)
                {
                    string ruleFriendlyPath = String.IsNullOrWhiteSpace(rule.ApplicationName) ? rule.ApplicationName : FileHelper.GetFriendlyPath(rule.ApplicationName);
                    Assert.True(String.IsNullOrWhiteSpace(ruleFriendlyPath) || exePath.Equals(ruleFriendlyPath, StringComparison.OrdinalIgnoreCase));
                    Console.WriteLine($"match found={matches}, rule={rule.Name}");
                    cntMatch++;
                }
            }
            Assert.True(cntMatch > 0);

        }
    }
}
