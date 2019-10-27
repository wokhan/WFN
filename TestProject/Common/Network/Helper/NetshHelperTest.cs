using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Wokhan.WindowsFirewallNotifier.Common.Helpers;

namespace TestProject.Common.Network.Helper
{
    [TestClass]
    public class NetshHelperTest
    {
        [TestInitialize]
        public void Init()
        {
            Assert.IsTrue(UacHelper.CheckProcessElevated(), "Only admin can run this test - restart as admin");
        }

        [TestMethod]
        public void TestGetMatchingFilterInfo()
        {
            //FilterResult result = NetshHelper.getMatchingFilterInfo(1260694); 
            FilterResult result = NetshHelper.getMatchingFilterInfo(104337); // Default Outbound (at the end of the xml)
            Assert.IsNotNull(result);
            Console.WriteLine($"name={result.Name}, description={result.Description}");
            //Assert.AreEqual("Boot Time Filter", result.Name, true);
            Assert.IsTrue(result.Description != null);
        }

        [TestMethod]
        public void TestGetMatchingFilterInfo_notfound()
        {
            Assert.IsTrue(UacHelper.CheckProcessElevated(), "Only admin can run this test - restart as admin");
            FilterResult result = NetshHelper.getMatchingFilterInfo(65785); // allows localhost in/out connections but does not exist in netsh wfp filters 
            Assert.IsNull(result);
        }
    }
}
