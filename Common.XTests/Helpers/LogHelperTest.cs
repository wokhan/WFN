using System;
using System.IO;
using NUnit.Framework;
using Common.Tests.NUnit;

namespace Wokhan.WindowsFirewallNotifier.Common.Helpers
{
    // Manual tests for loghelper functionality
    public class LogHelperTest : NUnitTestBase
    {
        private const string TEST_OUTPUT = "[TEST_OUTPUT]";

        [SetUp, IntegrationTestCategory]
        public override void SetUp()
        {
            Assert.NotNull(Settings.Default);
            // test if the path to the global log4net config file exists
            Assert.True(File.Exists("WFN.config.log4net"));
        }

        [Test, ManualTestCategory]
        public void TestWriters()
        {
            // See test result output
            WriteTestOutput($"{TEST_OUTPUT} NUnitTestBase.WriteTestOutput(msg)");

            // See Tests output window
            WriteTestProgress($"{TEST_OUTPUT} NUnitTestBase.WriteTestProgress(msg)");

            // See test result output
            WriteConsoleOutput($"{TEST_OUTPUT} NUnitTestBase.WriteConsoleOutput(msg)");

            // See Debug outtput window
            WriteDebugOutput($"{TEST_OUTPUT} NUnitTestBase.WriteDebugOutput(msg)");
        }

        // See testoutput for result (test method doesn't write to output window)
        [Test, ManualTestCategory]
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
