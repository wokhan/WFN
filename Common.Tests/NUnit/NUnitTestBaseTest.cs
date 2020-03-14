using System;
using System.IO;
using NUnit.Framework;
using Wokhan.WindowsFirewallNotifier.Console.Tests.NUnit;

namespace Common.Tests.NUnit
{
    [ManualTestCategory]
    public class NunitTestBaseTest :  NUnitTestBase
    {
        private const string TEST_OUTPUT = "[TEST_OUTPUT]";

        [Test]
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
    }
}
