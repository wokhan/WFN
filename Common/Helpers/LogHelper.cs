using System;
using System.IO;
using System.Reflection;
using System.Windows;

namespace Wokhan.WindowsFirewallNotifier.Common.Helpers
{
    public class LogHelper
    {
        private static string appVersion;
        private static string assemblyName;

        private static string logFilePath;

        private static bool isAdmin = UacHelper.CheckProcessElevated();

        static LogHelper()
        {
            var assembly = Assembly.GetCallingAssembly().GetName();
            appVersion = assembly.Version.ToString();
            assemblyName = assembly.Name;

            logFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, assemblyName + ".log");

            using (var fs = new FileStream(logFilePath, FileMode.Append))
            {
                if (!fs.CanWrite)
                {
                    logFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Wokhan Solutions", "WFN", assemblyName + ".log");
                }
            }

            if (Settings.Default.FirstRun)
            {
                writeLog("INIT", String.Format("OS: {0} ({1}bit) / .Net CLR: {2} / Path: {3} / Version: {4}", Environment.OSVersion, IntPtr.Size * 8, Environment.Version, AppDomain.CurrentDomain.BaseDirectory, appVersion));
            }
        }

        public static void Debug(string msg)
        {
            if (Settings.Default.DebugMode)
            {
                writeLog("DEBUG", msg);
            }
        }

        public static void Info(string msg)
        {
            writeLog("INFO", msg);
        }

        public static void Error(string msg, Exception e)
        {
            writeLog("ERROR", msg + "\r\n" + (e != null ? e.Message + "\r\n" + e.StackTrace : ""));
        }

        public static readonly object locker = new object();
        private static void writeLog(string type, string msg)
        {
            System.Diagnostics.Debug.WriteLine(msg);

            if (Settings.Default.EnableVerboseLogging)
            {
                lock (locker)
                {
                    StreamWriter sw = null;
                    try
                    {
                        sw = new StreamWriter(logFilePath, true);
                        sw.WriteLine("{0:yyyy/MM/dd HH:mm:ss} - {1} [{2}] - [{3}] - {4}", DateTime.Now, Environment.UserName, isAdmin, type, msg);
                    }
                    finally
                    {
                        if (sw != null)
                        {
                            sw.Close();
                        }
                    }
                }
            }
        }
    }
}
