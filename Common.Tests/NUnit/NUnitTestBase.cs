using System;
using System.Diagnostics;
using System.Linq;
using NUnit.Framework;

namespace Wokhan.WindowsFirewallNotifier.Console.Tests.NUnit
{

    /// <summary>
    /// Integration category to be used in CI/CD pipeline. 
    /// Note: The Category is shown in Test Explorer column Traits.
    /// 
    /// usage:
    ///     nunit3-console mytest.dll --where "cat == IntegrationTestCategory"
    ///     
    /// </summary>
    /// <seealso cref="https://github.com/nunit/docs/wiki/Test-Selection-Language"/>
    /// <example>nunit3-console mytest.dll --where "cat == IntegrationTestCategory"</example>
    //[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
    class IntegrationTestCategory : CategoryAttribute { }

    /// <summary>
    /// Manual test category to be used for manual tests during debugging.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
    class ManualTestCategory : CategoryAttribute { }

    /// <summary>
    /// Fixme test category.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
    class FixmeCategory : CategoryAttribute { }


    /// <summary>
    /// NUnit abstract test class which tests should extend from.
    /// </summary>
    public abstract class NUnitTestBase
    {

        protected enum TestCategory
        {
            Integration, Manual
        }

        [OneTimeSetUp]
        public void OneTimeSet()
        {
            // one-time setup here
        }

        [SetUp]
        public virtual void SetUp()
        {
            // test method setup
        }

        [TearDown]
        public virtual void TearDown()
        {
            TestContext.CurrentContext.Test.Arguments.Select(e => e.ToString()).ToList().ForEach(e => WriteTestProgress(e));
            TestContext.PropertyBagAdapter properties = TestContext.CurrentContext.Test.Properties;
            properties.Keys.ToList().ForEach(k => WriteTestProgress(k + "=" + properties.Get(k).ToString()));
            WriteTestProgress("Test outcome: " + TestContext.CurrentContext.Result.Outcome.ToString());
        }

        /// <summary>
        /// Write to the Tests result ouput available at the end of the test run.
        /// </summary>
        /// <param name="msg"></param>
        protected static void WriteTestOutput(string msg)
        {
            TestContext.WriteLine(InsertTestInfo(msg));
        }

        /// <summary>
        /// Write immediate progress to the Tests output window.
        /// </summary>
        /// <param name="msg"></param>
        protected static void WriteTestProgress(string msg)
        {
            TestContext.Progress.WriteLine(InsertTestInfo(msg));
        }

        /// <summary>
        /// Write to the Debug output window (only when running in debug mode).
        /// </summary>
        /// <param name="msg"></param>
        protected static void WriteDebugOutput(string msg)
        {
            Debug.WriteLine(InsertTestInfo(msg));
        }

        /// <summary>
        /// Write to the Console output.
        /// </summary>
        /// <param name="msg"></param>
        protected static void WriteConsoleOutput(string msg)
        {
            System.Console.WriteLine(InsertTestInfo(msg));
        }

        private static string InsertTestInfo(string msg)
        {
            string cname = TestContext.CurrentContext.Test.ClassName.Split(".").LastOrDefault();
            string mname = TestContext.CurrentContext.Test.MethodName;
            string testInfo = string.IsNullOrEmpty(mname) ? cname : string.Concat(cname, ".", mname);

            return $"{DateTime.Now:HH:mm:ss,fff} [{testInfo}] {msg}";
        }
    }
}
