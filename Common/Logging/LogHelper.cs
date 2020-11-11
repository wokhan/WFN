using System;
using System.IO;
using System.Reflection;
using log4net;
using log4net.Config;
using Wokhan.WindowsFirewallNotifier.Common.Helpers;

#if DEBUG
using System.Runtime.CompilerServices;
#else
using Wokhan.WindowsFirewallNotifier.Common.Config;
#endif

namespace Wokhan.WindowsFirewallNotifier.Common.Logging
{
    public static class LogHelper
    {
        private readonly static ILog LOGGER = LogManager.GetLogger(typeof(LogHelper));

        private const string LOG4NET_CONFIG_FILE = "WFN.config.log4net";

        private static readonly bool IsAdmin = UAC.CheckProcessElevated();
        public static readonly string CurrentLogsPath = AppDomain.CurrentDomain.BaseDirectory ?? string.Empty;

        enum LogLevel
        {
            DEBUG, WARNING, INFO, ERROR
        }

        static LogHelper()
        {
            var assembly = Assembly.GetCallingAssembly().GetName();
            var appVersion = assembly.Version?.ToString() ?? string.Empty;

            // log4net - look for a configuration file in the installation dir
            var logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());
            XmlConfigurator.ConfigureAndWatch(logRepository, new FileInfo(LOG4NET_CONFIG_FILE));

            // better to have this info always in the log
            WriteLog(LogLevel.INFO, string.Format("OS: {0} ({1} bit) / .Net CLR: {2} / Path: {3} / Version: {4} ({5} bit)", Environment.OSVersion, Environment.Is64BitOperatingSystem ? 64 : 32, Environment.Version, AppDomain.CurrentDomain.BaseDirectory, appVersion, Environment.Is64BitProcess ? 64 : 32));
            WriteLog(LogLevel.INFO, $"Process elevated: {IsAdmin}");
        }

#if DEBUG

        public static bool IsDebugEnabled()
        {
            return true;
        }
        public static void Debug(string msg,
            [CallerMemberName] string? memberName = null,
            [CallerFilePath] string? filePath = null,
            [CallerLineNumber] int lineNumber = -1)
#else
        public static bool IsDebugEnabled()
        {
            return false;
        }
        public static void Debug(string msg)
#endif
        {

#if DEBUG
            WriteLog(LogLevel.DEBUG, msg, memberName, filePath, lineNumber);
#else
            try
            {
                if (Settings.Default?.EnableVerboseLogging ?? false)
                {
                    WriteLog(LogLevel.DEBUG, msg);
                }
            }
            catch (Exception ex)
            {
                // may throw if settings not yet inited
                LOGGER.Warn(ex.Message);
                LOGGER.Debug(msg);
            }
#endif
        }

#if DEBUG
        public static void Info(string msg,
            [CallerMemberName] string? memberName = null,
            [CallerFilePath] string? filePath = null,
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
            [CallerMemberName] string? memberName = null,
            [CallerFilePath] string? filePath = null,
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
            [CallerMemberName] string? memberName = null,
            [CallerFilePath] string? filePath = null,
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

        private static void WriteLog(LogLevel type, string msg, string? memberName, string? filePath, int lineNumber)
        {
            switch (type)
            {
                case LogLevel.DEBUG:
                    LOGGER.Debug($"{msg} [{memberName}() in {Path.GetFileName(filePath)}, line {lineNumber}]");
                    break;

                case LogLevel.WARNING:
                    LOGGER.Warn($"{msg} [{memberName}() in {Path.GetFileName(filePath)}, line {lineNumber}]");
                    break;

                case LogLevel.ERROR:
                    LOGGER.Error($"{msg} [{memberName}()\n in {Path.GetFileName(filePath)}, line {lineNumber}]");
                    break;

                default:
                    LOGGER.Info(msg);
                    break;
            }
        }

        private static void WriteLog(LogLevel type, string msg)
        {
            switch (type)
            {
                case LogLevel.DEBUG:
                    LOGGER.Debug($"{msg}");
                    break;

                case LogLevel.WARNING:
                    LOGGER.Warn($"{msg}");
                    break;

                case LogLevel.ERROR:
                    LOGGER.Error($"{msg}");
                    break;

                default:
                    LOGGER.Info(msg);
                    break;
            }
        }

    }
}