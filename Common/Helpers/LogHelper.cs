using System;
using System.IO;
using System.Reflection;

namespace Wokhan.WindowsFirewallNotifier.Common.Helpers
{
    public class LogHelper
    {
        private static string runPath = AppDomain.CurrentDomain.BaseDirectory;
        private static string logPath = Path.Combine(runPath, "errors.log");
        private static string appVersion = Assembly.GetCallingAssembly().GetName().Version.ToString();


        static LogHelper()
        {
            if (Settings.Default.FirstRun)
            {
                writeLog("INIT", String.Format("OS: {0} ({1}bit) / .Net CLR: {2} / Path: {3} / Version: {4}", Environment.OSVersion, IntPtr.Size * 8, Environment.Version, runPath, appVersion));
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

        private static void writeLog(string type, string msg)
        {
            System.Diagnostics.Debug.WriteLine(msg);
            return;

            StreamWriter sw = null;
            try
            {
                sw = new StreamWriter(logPath, true);
                sw.WriteLine("{0:yyyy/MM/dd HH:mm:ss} - {1} - [{2}] - {3}", DateTime.Now, Environment.UserName, type, msg);
            }
            catch { }
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
