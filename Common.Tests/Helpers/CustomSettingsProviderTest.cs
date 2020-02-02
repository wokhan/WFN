using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Wokhan.WindowsFirewallNotifier.Common.Helpers
{
    [TestClass]
    public class CustomSettingsProviderTest
    {
        [TestMethod]
        ///
        /// Tests whether global settings are properly initialized from this test project (path names to config files e.g. WFN.config)
        /// If not, check the Probject properties > Build > Output path
        /// 
        public void TestInit()
        {
            Assert.IsNotNull(Settings.Default);

            // is the path to the global config file WFN.config correct?
            Assert.IsTrue(File.Exists(CustomSettingsProvider.SharedConfigurationPath));

            // Reset to default
            Settings.Default.Reset();
            Settings.Default.FirstRun = true;
            Settings.Default.Save();

            // Reload user settings (should be same as default now)
            Settings.Default.Reload();

            // can we read the first run parameter?
            Assert.IsTrue(Settings.Default.FirstRun);
        }
    }
}
