using System;
using System.IO;
using System.Reflection;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading;
using System.Windows;
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

        private const int RetryDelay = 200; //ms
        private const uint Retries = 5;

        private static string appVersion;
        private static string assemblyName;
        private static string logFilePath;
        private static readonly Mutex logFileMutex = null;
        private static readonly String logFileMutexName = @"WindowsFirewallNotifier_Common_LogFile_Mutex";

        private static bool isAdmin = UacHelper.CheckProcessElevated();

        static LogHelper()
        {
            var assembly = Assembly.GetCallingAssembly().GetName();
            appVersion = assembly.Version.ToString();
            assemblyName = assembly.Name;

            CurrentLogsPath = AppDomain.CurrentDomain.BaseDirectory;
            logFilePath = Path.Combine(CurrentLogsPath, assemblyName + ".log");

            MutexSecurity logFileMutexSecurity = new MutexSecurity();
            MutexAccessRule rule = new MutexAccessRule(new SecurityIdentifier(WellKnownSidType.WorldSid, null), MutexRights.Synchronize | MutexRights.Modify, AccessControlType.Allow);
            logFileMutexSecurity.AddAccessRule(rule);
            rule = new MutexAccessRule(new SecurityIdentifier(WellKnownSidType.WorldSid, null), MutexRights.ChangePermissions | MutexRights.TakeOwnership, AccessControlType.Deny);
            logFileMutexSecurity.AddAccessRule(rule);
            bool createdNew; //Not used
            logFileMutex = new Mutex(false, logFileMutexName, out createdNew, logFileMutexSecurity);

            bool LoggingFailed = false;

            try
            {
                logFileMutex.WaitOne();
            }
            catch (AbandonedMutexException /*ex*/)
            {
                //Mutex was abandoned; previous instance probably crashed while holding it.
                //Console.WriteLine("Exception on return from WaitOne." + "\r\n\tMessage: {0}", ex.Message);
            }
            try
            {
                //Every once in a while, some external program holds on to our logfile (probably anti-virus suites). So we have a retry-structure here.
                uint RetryCount = 0;
                while (true)
                {
                    try
                    {
                        using (var fs = new FileStream(logFilePath, FileMode.Append, FileAccess.Write, FileShare.Read))
                        {
                            if (!fs.CanWrite)
                            {
                                CurrentLogsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Wokhan Solutions", "WFN");
                                logFilePath = Path.Combine(CurrentLogsPath, assemblyName + ".log");
                            }
                        }
                        break;
                    }
                    catch (IOException)
                    {
                        if (RetryCount == Retries)
                        {
                            LoggingFailed = true; //Let's release the Mutex before showing the messagebox.
                            //throw would create an endless loop, so let's just ignore all of this mess...
                            break;
                        }
                        RetryCount++;
                        Thread.Sleep(RetryDelay);
                    }
                }
            }
            finally
            {
                logFileMutex.ReleaseMutex();
            }

            if (LoggingFailed)
            {
                if (!WindowsIdentity.GetCurrent().IsSystem) //Don't try to display a messagebox when we're SYSTEM, as this is not allowed.
                {
                    MessageBox.Show(Common.Resources.MSG_LOG_FAILED, Common.Resources.MSG_DLG_ERR_TITLE, MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }

            if (Settings.Default.FirstRun)
            {
                writeLog("INIT", String.Format("OS: {0} ({1} bit) / .Net CLR: {2} / Path: {3} / Version: {4} ({5} bit)", Environment.OSVersion, Environment.Is64BitOperatingSystem ? 64 : 32, Environment.Version, AppDomain.CurrentDomain.BaseDirectory, appVersion, Environment.Is64BitProcess ? 64 : 32));
            }
        }

        ~LogHelper()
        {
            if (logFileMutex != null)
            {
                logFileMutex.Dispose();
            }
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
#if DEBUG
            writeLog("ERROR", msg + Environment.NewLine + (e != null ? e.GetType().ToString() + ": " + e.Message + Environment.NewLine + e.StackTrace : ""), memberName, filePath, lineNumber);
#else
            writeLog("ERROR", msg + Environment.NewLine + (e != null ? e.GetType().ToString() + ": " + e.Message + Environment.NewLine + e.StackTrace : ""));
#endif
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

            bool LoggingFailed = false;

            if (logFileMutex == null)
            {
                //Probably crashed in the initialization of LogHelper!
                LoggingFailed = true;
            }
            else
            {
                try
                {
                    logFileMutex.WaitOne();
                }
                catch (AbandonedMutexException /*ex*/)
                {
                    //Mutex was abandoned; previous instance probably crashed while holding it.
                    //Console.WriteLine("Exception on return from WaitOne." + "\r\n\tMessage: {0}", ex.Message);
                }
                try
                {
                    //Every once in a while, some external program holds on to our logfile (probably anti-virus suites). So we have a retry-structure here.
                    uint RetryCount = 0;
                    while (true)
                    {
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
                            break;
                        }
                        catch (IOException)
                        {
                            if (RetryCount == Retries)
                            {
                                LoggingFailed = true; //Let's release the Mutex before showing the messagebox.
                                //throw would create an endless loop, so let's just ignore all of this mess...
                                break;
                            }
                            RetryCount++;
                            Thread.Sleep(RetryDelay);
                        }
                    }
                }
                finally
                {
                    logFileMutex.ReleaseMutex();
                }
            }

            if (LoggingFailed)
            {
                if (!WindowsIdentity.GetCurrent().IsSystem) //Don't try to display a messagebox when we're SYSTEM, as this is not allowed.
                {
                    MessageBox.Show(Common.Resources.MSG_LOG_FAILED, Common.Resources.MSG_DLG_ERR_TITLE, MessageBoxButton.OK, MessageBoxImage.Error);
#if DEBUG
                    MessageBox.Show(msg, Common.Resources.MSG_DLG_ERR_TITLE, MessageBoxButton.OK, MessageBoxImage.Error);
#endif
                }
            }
        }
    }
}