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
            FilterResult result = NetshHelper.getMatchingFilterInfo(1208377); // "Block Inbound Default Rule"
            Assert.IsNotNull(result);
            Console.WriteLine($"name={result.Name}, description={result.Description}");
            Assert.AreEqual("Block Inbound Default Rule", result.Name);
            Assert.IsFalse(string.IsNullOrEmpty(result.Description));
        }
        public void TestGetMatchingFilterInfo_notfound()
        {
            Assert.IsTrue(UacHelper.CheckProcessElevated(), "Only admin can run this test - restart as admin");
            FilterResult result = NetshHelper.getMatchingFilterInfo(65785); // allows localhost in/out connections but does not exist in filters 
            Assert.IsNull(result);
        }
    }
}
