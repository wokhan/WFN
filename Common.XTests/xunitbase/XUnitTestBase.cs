using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Xunit.Abstractions;

namespace Wokhan.WindowsFirewallNotifier.Console.Tests.xunitbase
{
    //
    // Abstract base class for xunit tests.
    //
    public abstract class XunitTestBase
    {

        // used for redirecting console output
        class TestTraceListener : TraceListener
        {
            private ITestOutputHelper _output;
            public TestTraceListener(ITestOutputHelper output) { _output = output; }
            public override void Write(string message) { _output.WriteLine(message); }
            public override void WriteLine(string message) { _output.WriteLine(message); }
        }

        private ITestOutputHelper _testLogOutput;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="output">Passed in from xunit test</param>
        /// <param name="captureOutput">Should Console, Debug output be included in the test output</param>
        public XunitTestBase(ITestOutputHelper output, bool captureInTestOutput)
        {
            _testLogOutput = output;
            if (captureInTestOutput)
            {
                Trace.Listeners.Add(new TestTraceListener(output));
            }
        }

        /// <summary>
        /// Log to test output.
        /// </summary>
        /// <param name="msg"></param>
        protected void Log(string msg)
        {
            _testLogOutput.WriteLine(msg);
        }

        /// <summary>
        /// Log to test output.
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="args"></param>
        protected void Log(string msg, params object[] args)
        {
            _testLogOutput.WriteLine(msg, args);
        }

    }
}
