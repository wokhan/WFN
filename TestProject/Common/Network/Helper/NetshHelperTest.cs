using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Wokhan.WindowsFirewallNotifier.Common.Helpers;
using Harrwiss.Common.Network.Helper;

namespace TestProject.CommonTests
{
    /// 
    /// Test NetShHelper to retrieve filterId information. Note that filterId is generated at runtime and therefore may change 
    /// after a reboot etc.
    /// 
    [TestClass]
    public class NetshHelperTest
    {
        [TestInitialize]
        public void Init()
        {
            Assert.IsTrue(UacHelper.CheckProcessElevated(), "Only admin can run this test - restart as admin");
        }

        [TestMethod]
        public void TestFindMatchingFilterInfo()
        {
            // Default Outbound (at the end of the filters xml)
            int filterId = FindRuntimeFilterIdByFilterKey(@"{e2b0969d-ced5-4d33-a3c2-41b578a727f5}");
            FilterResult result = NetshHelper.FindMatchingFilterInfo(filterId); 
            Assert.IsNotNull(result);
            Assert.IsFalse(result.HasErrors);
            Console.WriteLine($"name ={result.Name}, description={result.Description}");
            Assert.AreEqual("Default Outbound", result.Name, true);
            Assert.IsTrue(result.Description != null);
            Assert.AreEqual(result.FoundIn, FiltersContextEnum.FILTERS);
        }
        [TestMethod]
        public void TestFindMatchingFilterInfo2Boot1()
        {
            // Boot time filter v4
            int filterId = FindRuntimeFilterIdByFilterKey(@"{935b7f48-0ede-44dd-9bc2-e00bb635cda3}");
            FilterResult result = NetshHelper.FindMatchingFilterInfo(filterId); 
            Assert.IsNotNull(result);
            Assert.IsFalse(result.HasErrors);
            Console.WriteLine($"name={result.Name}, description={result.Description}");
            Assert.AreEqual("Boot Time Filter", result.Name, true);
            Assert.IsTrue(result.Description != null);
        }
        [TestMethod]
        public void TestFindMatchingFilterInfo2Boot2()
        {
            // Boot time filter v6
            int filterId = FindRuntimeFilterIdByFilterKey(@"{941dad9d-7b1a-4354-997b-00cf1aa9b35c}");
            FilterResult result = NetshHelper.FindMatchingFilterInfo(filterId);
            Assert.IsNotNull(result);
            Assert.IsFalse(result.HasErrors);
            Console.WriteLine($"name={result.Name}, description={result.Description}");
            Assert.AreEqual("Boot Time Filter", result.Name, true);
            Assert.IsTrue(result.Description != null);
        }

        public void TestFindMatchingFilterInfo2Boot3()
        {
            // Boot time filter FWPM_LAYER_INBOUND_ICMP_ERROR_V4 (wfpstate only filter)
            int filterId = FindRuntimeFilterIdByFilterKey(@"{074f7f68-ee10-428a-89d1-ba78f6c327ca}");
            FilterResult result = NetshHelper.FindMatchingFilterInfo(filterId);
            Assert.IsNotNull(result);
            Assert.IsFalse(result.HasErrors);
            Console.WriteLine($"name={result.Name}, description={result.Description}");
            Assert.AreEqual("Boot Time Filter", result.Name, true);
            Assert.IsTrue(result.Description != null);
            Assert.AreEqual(result.FoundIn, FiltersContextEnum.WFPSTATE);
        }

        [TestMethod]
        public void TestFindMatchingFilterInfo3()
        {
            // Boot time filter
            int filterId = FindRuntimeFilterIdByFilterKey(@"{935b7f48-0ede-44dd-9bc2-e00bb635cda3}");
            FilterResult result = NetshHelper.FindMatchingFilterInfo(filterId); // Boot time filter (id's may vary?)
            Assert.IsFalse(result.HasErrors);
            Assert.IsNotNull(result);
            Console.WriteLine($"name={result.Name}, description={result.Description}");
            Assert.AreEqual("Boot Time Filter", result.Name, true);
            Assert.IsTrue(result.Description != null);
        }

        

    [TestMethod]
    public void TestFindMatchingFilterInfo4()
    {
        // Port scanning prevention filter (wfpstate filter)
        int filterId = FindRuntimeFilterIdByFilterKey(@"{a3dfb1bd-bea6-4b91-b103-e64a545e8e78}");
        FilterResult result = NetshHelper.FindMatchingFilterInfo(filterId); // Boot time filter (id's may vary?)
        Assert.IsNotNull(result);
        Assert.IsFalse(result.HasErrors);
        Console.WriteLine($"name={result.Name}, description={result.Description}");
        Assert.AreEqual("Port Scanning Prevention Filter", result.Name, true);
        Assert.IsTrue(result.Description != null);
    }
    [TestMethod]
        public void TestGetMatchingFilterInfo_notfound()
        {
            FilterResult result = NetshHelper.FindMatchingFilterInfo(1); 
            Assert.IsNotNull(result);
            Assert.IsTrue(result.HasErrors);
        }

        private int FindRuntimeFilterIdByFilterKey(string filterKey)
        {
            FilterResult result = NetshHelper.FindMatchingFilterByKey(@filterKey, false);
            Assert.IsNotNull(result);
            Assert.IsFalse(result.HasErrors);
            Assert.IsTrue(result.FilterId > 0);
            Assert.AreNotEqual(result.FoundIn, FiltersContextEnum.NONE);

            return result.FilterId;
        }
    }
}
