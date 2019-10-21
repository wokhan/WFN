using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Wokhan.WindowsFirewallNotifier.Common;
using Wokhan.WindowsFirewallNotifier.Common.Helpers;

namespace TestProject.Common
{
    [TestClass]
    public class CustomSettingsProviderTest
    {
        [TestMethod]
        ///
        /// Tests whether global settings are properly initialized from this test project (path names to config files e.g. WFN.config)
        /// If not, check the Build > Output path
        /// 
        public void testInit()
        {
            Assert.IsNotNull(Settings.Default);

            // is the path to the global config file WFN.config correct?
            Assert.IsTrue(File.Exists(CustomSettingsProvider.SharedConfigurationPath));

            // can we read the first run parameter?
            Assert.IsTrue(Settings.Default.FirstRun);
        }
    }
}
