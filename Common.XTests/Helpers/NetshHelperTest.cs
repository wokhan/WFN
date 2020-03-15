using System;
using Wokhan.WindowsFirewallNotifier.Console.Tests.xunitbase;
using Xunit;
using Xunit.Abstractions;

namespace Wokhan.WindowsFirewallNotifier.Common.Helpers
{
    /// 
    /// Test NetShHelper to retrieve filterId information. Note that filterId and even filterKey is generated at runtime and therefore may change 
    /// after a reboot etc.
    /// 
    public class NetshHelperTest : XunitTestBase
    {
        public NetshHelperTest(ITestOutputHelper output) : base(output, captureInTestOutput: true)
        {
            Assert.True(UacHelper.CheckProcessElevated(), "Only admin can run this test - restart as admin");
        }

        //[Fact]
        public void TestFindMatchingFilterInfo()
        {
            // Default Outbound (at the end of the filters xml)
            int filterId = FindRuntimeFilterIdByFilterKey(@"{e2b0969d-ced5-4d33-a3c2-41b578a727f5}");
            FilterResult result = NetshHelper.FindMatchingFilterInfo(filterId);
            Assert.NotNull(result);
            Assert.False(result.HasErrors);
            Log($"name ={result.Name}, description={result.Description}");
            Assert.Equal("Default Outbound", result.Name, true);
            Assert.True(result.Description != null);
            Assert.Equal(FiltersContextEnum.FILTERS, result.FoundIn);
        }

        //TODO: Commented out by @wokhan since test result depends on OS language and will fail on non-english ones
        /*
        [Fact]
        public void TestFindMatchingFilterInfo2Boot1()
        {
            // Boot time filter v4
            int filterId = FindRuntimeFilterIdByFilterKey(@"{935b7f48-0ede-44dd-9bc2-e00bb635cda3}");
            FilterResult result = NetshHelper.FindMatchingFilterInfo(filterId); 
            Assert.NotNull(result);
            Assert.False(result.HasErrors);
            Log($"name={result.Name}, description={result.Description}");
            Assert.Equal("Boot Time Filter", result.Name, true);
            Assert.True(result.Description != null);
        }*/

        //TODO: Commented out by @wokhan since test result depends on OS language and will fail on non-english ones
        /*
        [Fact]
        public void TestFindMatchingFilterInfo2Boot2()
        {
            // Boot time filter v6
            int filterId = FindRuntimeFilterIdByFilterKey(@"{941dad9d-7b1a-4354-997b-00cf1aa9b35c}");
            FilterResult result = NetshHelper.FindMatchingFilterInfo(filterId);
            Assert.NotNull(result);
            Assert.False(result.HasErrors);
            Log($"name={result.Name}, description={result.Description}");
            Assert.Equal("Boot Time Filter", result.Name, true);
            Assert.True(result.Description != null);
        }
        */

        public void TestFindMatchingFilterInfo2Boot3()
        {
            // Boot time filter FWPM_LAYER_INBOUND_ICMP_ERROR_V4 (wfpstate only filter)
            int filterId = FindRuntimeFilterIdByFilterKey(@"{074f7f68-ee10-428a-89d1-ba78f6c327ca}");
            FilterResult result = NetshHelper.FindMatchingFilterInfo(filterId);
            Assert.NotNull(result);
            Assert.False(result.HasErrors);
            Log($"name={result.Name}, description={result.Description}");
            Assert.Equal("Boot Time Filter", result.Name, true);
            Assert.True(result.Description != null);
            Assert.Equal(result.FoundIn, FiltersContextEnum.WFPSTATE);
        }

        //TODO: Commented out by @wokhan since test result depends on OS language and will fail on non-english ones
        /*
        [Fact]
        public void TestFindMatchingFilterInfo3()
        {
            // Boot time filter
            int filterId = FindRuntimeFilterIdByFilterKey(@"{935b7f48-0ede-44dd-9bc2-e00bb635cda3}");
            FilterResult result = NetshHelper.FindMatchingFilterInfo(filterId); 
            Assert.False(result.HasErrors);
            Assert.NotNull(result);
            Log($"name={result.Name}, description={result.Description}");
            Assert.Equal("Boot Time Filter", result.Name, true);
            Assert.True(result.Description != null);
        }
        */

        //[Fact] filterKey can change
        public void TestFindMatchingFilterInfo4()
        {
            // Port scanning prevention filter (wfpstate filter)
            int filterId = FindRuntimeFilterIdByFilterKey(@"{a3dfb1bd-bea6-4b91-b103-e64a545e8e78}");
            FilterResult result = NetshHelper.FindMatchingFilterInfo(filterId);
            Assert.NotNull(result);
            Assert.False(result.HasErrors);
            Log($"name={result.Name}, description={result.Description}");
            Assert.Equal("Port Scanning Prevention Filter", result.Name, true);
            Assert.True(result.Description != null);
        }
        [Fact]
        public void TestGetMatchingFilterInfo_notfound()
        {
            FilterResult result = NetshHelper.FindMatchingFilterInfo(0);
            Assert.NotNull(result);
            Assert.True(result.HasErrors);
        }

        private int FindRuntimeFilterIdByFilterKey(string filterKey)
        {
            FilterResult result = NetshHelper.FindMatchingFilterByKey(@filterKey, false);
            Assert.NotNull(result);
            Assert.False(result.HasErrors);
            Assert.True(result.FilterId > 0);
            Assert.NotEqual(FiltersContextEnum.NONE, result.FoundIn);

            return result.FilterId;
        }
    }
}
