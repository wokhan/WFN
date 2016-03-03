using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows;

namespace Wokhan.WindowsFirewallNotifier.Common.Helpers
{
    public class LogHelper
    {
        public static string CurrentLogsPath
        {
            get;
            private set;
        }

        private static string appVersion;
        private static string assemblyName;

        private static string logFilePath;

        private static bool isAdmin = UacHelper.CheckProcessElevated();

        static LogHelper()
        {
            var assembly = Assembly.GetCallingAssembly().GetName();
            appVersion = assembly.Version.ToString();
            assemblyName = assembly.Name;

            CurrentLogsPath = AppDomain.CurrentDomain.BaseDirectory;
            logFilePath = Path.Combine(CurrentLogsPath, assemblyName + ".log");

            locker.WaitOne();
            try
            {
                using (var fs = new FileStream(logFilePath, FileMode.Append, FileAccess.Write, FileShare.Write))
                {
                    if (!fs.CanWrite)
                    {
                        CurrentLogsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Wokhan Solutions", "WFN");
                        logFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Wokhan Solutions", "WFN", assemblyName + ".log");
                    }
                }
            }
            finally
            {
                locker.Set();
            }

            if (Settings.Default.FirstRun)
            {
                writeLog("INIT", String.Format("OS: {0} ({1} bit) / .Net CLR: {2} / Path: {3} / Version: {4} ({5} bit)", Environment.OSVersion, Environment.Is64BitOperatingSystem ? 64 : 32, Environment.Version, AppDomain.CurrentDomain.BaseDirectory, appVersion, Environment.Is64BitProcess ? 64 : 32));
            }
        }

        public static void Debug(string msg,
            [CallerMemberName] string memberName = null,
            [CallerFilePath] string filePath = null,
            [CallerLineNumber] int lineNumber = -1)
        {
            if (Settings.Default.EnableVerboseLogging)
            {
                writeLog("DEBUG", msg, memberName, filePath, lineNumber);
            }
        }

        public static void Info(string msg,
            [CallerMemberName] string memberName = null,
            [CallerFilePath] string filePath = null,
            [CallerLineNumber] int lineNumber = -1)
        {
            if (Settings.Default.EnableVerboseLogging)
            {
                writeLog("INFO", msg, memberName, filePath, lineNumber);
            }
        }

        public static void Warning(string msg,
            [CallerMemberName] string memberName = null,
            [CallerFilePath] string filePath = null,
            [CallerLineNumber] int lineNumber = -1)
        {
            writeLog("WARNING", msg, memberName, filePath, lineNumber);
        }

        public static void Error(string msg, Exception e,
            [CallerMemberName] string memberName = null,
            [CallerFilePath] string filePath = null,
            [CallerLineNumber] int lineNumber = -1)
        {
            writeLog("ERROR", msg + Environment.NewLine + (e != null ? e.Message + Environment.NewLine + e.StackTrace : ""), memberName, filePath, lineNumber);
        }

        private static readonly EventWaitHandle locker = new EventWaitHandle(true, EventResetMode.AutoReset, "WFN_Log_Sync_Lock");
        private static void writeLog(string type, string msg,
            string memberName = null,
            string filePath = null,
            int lineNumber = -1)
        {
            System.Diagnostics.Debug.WriteLine(msg);

            locker.WaitOne();
            try
            {
                using (var sw = new StreamWriter(logFilePath, true))
                {
                    var codeLocation = string.Empty;
                    if (!string.IsNullOrWhiteSpace(memberName)
                        || !string.IsNullOrWhiteSpace(filePath))
                    {
                        codeLocation = string.Format(" [{0}() in {1}, line {2}]",
                            memberName,
                            filePath,
                            lineNumber);
                    }

                    sw.WriteLine("{0:yyyy/MM/dd HH:mm:ss} - {1} [{2}] - [{3}]{5} {4}",
                        DateTime.Now,
                        Environment.UserName,
                        isAdmin,
                        type,
                        msg,
                        codeLocation);
                }
            }
            finally
            {
                locker.Set();
            }
        }
    }
}