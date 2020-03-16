using System;
using System.IO;
using NUnit.Framework;
using Wokhan.WindowsFirewallNotifier.Console.Tests.NUnit;

namespace Wokhan.WindowsFirewallNotifier.Common.Helpers
{
    // 
    // Tests LogHelper functionality and configuration.
    //
    public class LogHelperTest : NUnitTestBase
    {
        private const string TEST_OUTPUT = "[TEST_OUTPUT]";

        [SetUp]
        public override void SetUp()
        {
            Assert.NotNull(Settings.Default);
            // test if the path to the global log4net config file exists
            Assert.True(File.Exists("WFN.config.log4net"));
        }

        // See testoutput for result (test method doesn't write to output window)
        [Test, IntegrationTestCategory]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "<Pending>")]
        public void TestLogHelper()
        {
            // test log4net ouput capturing (see Debug output window)

            LogHelper.Info($"{TEST_OUTPUT} Loghelper.Info(msg)");

            LogHelper.Warning($"{TEST_OUTPUT} Loghelper.Warning(msg)");

            LogHelper.Error($"{TEST_OUTPUT} Loghelper.Error(msg, exception)", new Exception("[Testexception message]"));

            LogHelper.Debug($"{TEST_OUTPUT} Loghelper.Debug(msg)");

        }
    }
}
