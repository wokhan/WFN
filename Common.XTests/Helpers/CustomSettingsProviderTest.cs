using System;
using System.IO;
using Xunit;

namespace Wokhan.WindowsFirewallNotifier.Common.Helpers
{
    public class CustomSettingsProviderTest
    {
        ///
        /// Tests whether global settings are properly initialized from this test project (path names to config files e.g. WFN.config)
        /// If not, check the Probject properties > Build > Output path
        /// 
        [Fact]
        public void TestInit()
        {
            Assert.NotNull(Settings.Default);

            // is the path to the global config file WFN.config correct?
            Assert.True(File.Exists(CustomSettingsProvider.SharedConfigurationPath));

            // Reset to default
            Settings.Default.Reset();
            Settings.Default.FirstRun = true;
            Settings.Default.Save();

            // Reload user settings (should be same as default now)
            Settings.Default.Reload();

            // can we read the first run parameter?
            Assert.True(Settings.Default.FirstRun);
        }
    }
}
