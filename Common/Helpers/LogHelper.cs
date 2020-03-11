using System;
using System.IO;
using System.Reflection;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading;
using System.Windows;
using log4net;
using log4net.Config;

#if DEBUG
using System.Runtime.CompilerServices;
#endif
using Wokhan.WindowsFirewallNotifier.Common.Properties;

namespace Wokhan.WindowsFirewallNotifier.Common.Helpers
{
    public static class LogHelper
    {
        private readonly static ILog LOGGER = LogManager.GetLogger(typeof(LogHelper));

        private const string LOG4NET_CONFIG_FILE = "WFN.config.log4net";

        private static readonly bool IsAdmin = UacHelper.CheckProcessElevated();
        public static readonly string CurrentLogsPath;

        enum LogLevel
        {
            DEBUG, WARNING, INFO, ERROR
        }

        static LogHelper()
        {
            var assembly = Assembly.GetCallingAssembly().GetName();
            string appVersion = assembly.Version.ToString();
            string assemblyName = assembly.Name;
            CurrentLogsPath = AppDomain.CurrentDomain.BaseDirectory;

            // log4net - look for a configuration file in the installation dir
            var logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());
            XmlConfigurator.ConfigureAndWatch(logRepository, new FileInfo(LOG4NET_CONFIG_FILE));

            // better to have this info always in the log
            WriteLog(LogLevel.INFO, String.Format("OS: {0} ({1} bit) / .Net CLR: {2} / Path: {3} / Version: {4} ({5} bit)", Environment.OSVersion, Environment.Is64BitOperatingSystem ? 64 : 32, Environment.Version, AppDomain.CurrentDomain.BaseDirectory, appVersion, Environment.Is64BitProcess ? 64 : 32));
            WriteLog(LogLevel.INFO, $"Process elevated: {IsAdmin}");
            if (Settings.Default?.FirstRun ?? true)
            {

                // maybe not required anymore since notifier is not triggered by eventlog anymore

                if (Settings.Default != null)
                {
                    Settings.Default.FirstRun = false;
                    Settings.Default.Save();
                }
            }
        }

#if DEBUG

        public static bool isDebugEnabled()
        {
            return true;
        }
        public static void Debug(string msg,
            [CallerMemberName] string memberName = null,
            [CallerFilePath] string filePath = null,
            [CallerLineNumber] int lineNumber = -1)
#else
        public static bool isDebugEnabled()
        {
            return false;
        }
        public static void Debug(string msg)
#endif
        {
            if (Settings.Default?.EnableVerboseLogging ?? false)
            {
#if DEBUG
                WriteLog(LogLevel.DEBUG, msg, memberName, filePath, lineNumber);
#else
                WriteLog(LogLevel.DEBUG, msg);
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
#if DEBUG
            WriteLog(LogLevel.INFO, msg, memberName, filePath, lineNumber);
#else
                WriteLog(LogLevel.INFO, msg);
#endif
        }

        internal static T WarnAndReturn<T>(string msg, T value)
        {
            Warning(msg);
            return value;
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
            WriteLog(LogLevel.WARNING, msg, memberName, filePath, lineNumber);
#else
            WriteLog(LogLevel.WARNING, msg);
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
#if DEBUG
            WriteLog(LogLevel.ERROR, msg + Environment.NewLine + (e != null ? e.GetType().ToString() + ": " + e.Message + Environment.NewLine + e.StackTrace : ""), memberName, filePath, lineNumber);
#else
            WriteLog(LogLevel.ERROR, msg + Environment.NewLine + (e != null ? e.GetType().ToString() + ": " + e.Message + Environment.NewLine + e.StackTrace : ""));
#endif
        }

        private static void WriteLog(LogLevel type, string msg, string memberName, string filePath, int lineNumber)
        {
            if (LogLevel.DEBUG.Equals(type))
            {
                LOGGER.Debug($"{msg} [{memberName}() in {Path.GetFileName(filePath)}, line {lineNumber}]");
            }
            else if (LogLevel.WARNING.Equals(type))
            {
                LOGGER.Warn($"{msg} [{memberName}() in {Path.GetFileName(filePath)}, line {lineNumber}]");
            }
            else if (LogLevel.ERROR.Equals(type))
            {
                LOGGER.Error($"{msg} [{memberName}()\n in {Path.GetFileName(filePath)}, line {lineNumber}]");
            }
            else
            {
                LOGGER.Info(msg);
            }
        }

        private static void WriteLog(LogLevel type, string msg)
        {
            // Console.WriteLine($"{DateTime.Now:yyyy/MM/dd HH:mm:ss} - {DateTime.Now} [{Environment.UserName}] - [{type}] {msg}");
            if (LogLevel.DEBUG.Equals(type))
            {
                LOGGER.Debug($"{msg}");
            }
            else if (LogLevel.WARNING.Equals(type))
            {
                LOGGER.Warn($"{msg}");
            }
            else if (LogLevel.ERROR.Equals(type))
            {
                LOGGER.Error($"{msg}");
            }
            else
            {
                LOGGER.Info(msg);
            }
        }

    }
}