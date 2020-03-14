using System;
using System.IO;
using NUnit.Framework;
using Wokhan.WindowsFirewallNotifier.Console.Tests.NUnit;

namespace Wokhan.WindowsFirewallNotifier.Common.Helpers
{
    [TestFixture]
    public class CustomSettingsProviderTest : NUnitTestBase
    {
        [SetUp]
        public override void SetUp()
        {
            base.SetUp();

            // test if the path to the global config file exists
            Assert.True(File.Exists("WFN.config"));
            // loads the settings
            Assert.NotNull(Settings.Default);
        }
        ///
        /// Tests whether global settings are properly initialized from this test project (path names to config files e.g. WFN.config)
        /// If not, check the Probject properties > Build > Output path
        /// 
        [Test, IntegrationTestCategory]
        public void TestInit()
        {
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
