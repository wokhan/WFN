using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Wokhan.WindowsFirewallNotifier.Common.Helpers
{
    // Manual tests for loghelper functionality
    [TestClass]
    public class LogHelperTest
    {
        [TestMethod]

        public void TestInit()
        {
            Assert.IsNotNull(Settings.Default);

            // is the path to the global config file WFN.config correct?
            Assert.IsTrue(File.Exists("WFN.config.log4net"));

            LogHelper.Info("info log message");

            LogHelper.Debug("debug log message");

        }
    }
}
