using System;
using System.IO;
using System.Reflection;
using System.Threading;
#if DEBUG
using System.Runtime.CompilerServices;
#endif


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
        private static readonly Mutex logFileMutex = new Mutex(false, "WindowsFirewallNotifier_Common_LogFile_Mutex");

        private static bool isAdmin = UacHelper.CheckProcessElevated();

        static LogHelper()
        {
            var assembly = Assembly.GetCallingAssembly().GetName();
            appVersion = assembly.Version.ToString();
            assemblyName = assembly.Name;

            CurrentLogsPath = AppDomain.CurrentDomain.BaseDirectory;
            logFilePath = Path.Combine(CurrentLogsPath, assemblyName + ".log");

            logFileMutex.WaitOne();
            try
            {
                using (var fs = new FileStream(logFilePath, FileMode.Append, FileAccess.Write, FileShare.Read))
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
                logFileMutex.ReleaseMutex();
            }

            if (Settings.Default.FirstRun)
            {
                writeLog("INIT", String.Format("OS: {0} ({1} bit) / .Net CLR: {2} / Path: {3} / Version: {4} ({5} bit)", Environment.OSVersion, Environment.Is64BitOperatingSystem ? 64 : 32, Environment.Version, AppDomain.CurrentDomain.BaseDirectory, appVersion, Environment.Is64BitProcess ? 64 : 32));
            }
        }

        ~LogHelper()
        {
            logFileMutex.Dispose();
        }

#if DEBUG
        public static void Debug(string msg,
            [CallerMemberName] string memberName = null,
            [CallerFilePath] string filePath = null,
            [CallerLineNumber] int lineNumber = -1)
#else
        public static void Debug(string msg)
#endif
        {
            if (Settings.Default.EnableVerboseLogging)
            {
#if DEBUG
                writeLog("DEBUG", msg, memberName, filePath, lineNumber);
#else
                writeLog("DEBUG", msg);
#endif
            }
        }

#if DEBUG
        public static void Info(string msg,
            [CallerMemberName] string memberName = null,
            [CallerFilePath] string filePath = null,
            [CallerLineNumber] int lineNumber = -1)
#else
        public static void Info(string msg)
#endif
        {
            if (Settings.Default.EnableVerboseLogging)
            {
#if DEBUG
                writeLog("INFO", msg, memberName, filePath, lineNumber);
#else
                writeLog("INFO", msg);
#endif
            }
        }

#if DEBUG
        public static void Warning(string msg,
            [CallerMemberName] string memberName = null,
            [CallerFilePath] string filePath = null,
            [CallerLineNumber] int lineNumber = -1)
#else
        public static void Warning(string msg)
#endif
        {
#if DEBUG
            writeLog("WARNING", msg, memberName, filePath, lineNumber);
#else
            writeLog("WARNING", msg);
#endif
        }

#if DEBUG
        public static void Error(string msg, Exception e,
            [CallerMemberName] string memberName = null,
            [CallerFilePath] string filePath = null,
            [CallerLineNumber] int lineNumber = -1)
#else
        public static void Error(string msg, Exception e)
#endif
        {
            writeLog("ERROR", msg + Environment.NewLine + (e != null ? e.Message + Environment.NewLine + e.StackTrace : "")
#if DEBUG
, memberName, filePath, lineNumber
#endif
                );
        }

#if DEBUG
        private static void writeLog(string type, string msg,
            string memberName = null,
            string filePath = null,
            int lineNumber = -1)
#else
        private static void writeLog(string type, string msg)
#endif
        {
            System.Diagnostics.Debug.WriteLine(msg);

            logFileMutex.WaitOne();
            try
            {
                using (var sw = new StreamWriter(logFilePath, true))
                {
#if DEBUG
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
#else
                    sw.WriteLine("{0:yyyy/MM/dd HH:mm:ss} - {1} [{2}] - [{3}] {4}",
                        DateTime.Now,
                        Environment.UserName,
                        isAdmin,
                        type,
                        msg);
#endif
                }
            }
            finally
            {
                logFileMutex.ReleaseMutex();
            }
        }
    }
}