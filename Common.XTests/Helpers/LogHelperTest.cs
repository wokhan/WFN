using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Wokhan.WindowsFirewallNotifier.Console.Tests.xunitbase;
using Xunit;
using Xunit.Abstractions;

namespace Wokhan.WindowsFirewallNotifier.Common.Helpers
{
    // Manual tests for loghelper functionality
    public class LogHelperTest : XunitTestBase
    {
        public LogHelperTest(ITestOutputHelper output) : base(output, captureInTestOutput: true) {}

        // See testoutput for result (test method doesn't write to output window)
        [Fact]
        public void TestLogHelperX()
        {
            Assert.NotNull(Settings.Default);
            // test if the path to the global log4net config file exists
            Assert.True(File.Exists("WFN.config.log4net"));

            Log("XunitTestBase.Log: output");

            // TODO: not being captured
            System.Console.WriteLine("System.console: output");

            // Debug is captured
            Debug.WriteLine("Debug: output");

            // log4net is captured
            LogHelper.Info("Loghelper: info log message");

            LogHelper.Warning("Loghelper: warning log message");

            LogHelper.Error("Loghelper:error log message", new Exception("[Testexception message]"));

            LogHelper.Debug("Loghelper: debug log message");

        }
    }
}
