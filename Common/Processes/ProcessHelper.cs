using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Text;
using System.Management;
using System.Text.RegularExpressions;
using System.Windows;
using Wokhan.WindowsFirewallNotifier.Common.Net.WFP;
using Wokhan.WindowsFirewallNotifier.Common.Net.IP;
using System.IO;
using Wokhan.WindowsFirewallNotifier.Common.Logging;

namespace Wokhan.WindowsFirewallNotifier.Common.Processes
{
    public static partial class ProcessHelper
    {
        public static void ElevateCurrentProcess()
        {
            ProcessStartInfo proc = new ProcessStartInfo
            {
                UseShellExecute = true,
                WorkingDirectory = Environment.CurrentDirectory,
                FileName = Path.Combine(Assembly.GetCallingAssembly().Location),
                Verb = "runas"
            };

            Process.Start(proc);
        }
        public static string[] GetProcessOwnerWMI(int owningPid, ref Dictionary<int, string[]> previousCache)
        {
            if (previousCache is null)
            {
                using var searcher = new ManagementObjectSearcher("SELECT ProcessId, Name, ExecutablePath, CommandLine FROM Win32_Process");
                using var results = searcher.Get();
                // Looks like the first cast to uint is required for this to work (pretty weird if you ask me).
                previousCache = results.Cast<ManagementObject>().ToDictionary(r => (int)(uint)r["ProcessId"], r => new[] { (string)r["Name"], (string)r["ExecutablePath"], (string)r["CommandLine"] });
            }

            if (!previousCache.ContainsKey(owningPid))
            {
                using var searcher = new ManagementObjectSearcher($"SELECT ProcessId, Name, ExecutablePath, CommandLine FROM Win32_Process WHERE ProcessId = {owningPid}");
                using var r = searcher.Get().Cast<ManagementObject>().FirstOrDefault();
                previousCache.Add(owningPid, new[] { (string)r["Name"], (string)r["ExecutablePath"], (string)r["CommandLine"] });
            }

            return previousCache[owningPid];
        }

        public static IEnumerable<string>? GetAllServices(uint pid)
        {
            IntPtr hServiceManager = NativeMethods.OpenSCManager(null, null, (uint)(NativeMethods.SCM_ACCESS.SC_MANAGER_CONNECT | NativeMethods.SCM_ACCESS.SC_MANAGER_ENUMERATE_SERVICE));
            if (hServiceManager == IntPtr.Zero)
            {
                LogHelper.Warning("Unable to open SCManager.");
                return Array.Empty<string>();
            }
            try
            {
                uint dwBufSize = 0;
                uint dwBufNeed = 0;
                uint ServicesReturned = 0;
                uint ResumeHandle = 0;

                var resp = NativeMethods.EnumServicesStatusEx(hServiceManager, (int)NativeMethods.SC_ENUM_TYPE.SC_ENUM_PROCESS_INFO, (int)NativeMethods.SERVICE_TYPES.SERVICE_WIN32, (int)NativeMethods.SERVICE_STATE.SERVICE_ACTIVE, IntPtr.Zero, dwBufSize, out dwBufNeed, out ServicesReturned, ref ResumeHandle, null);
                if (resp != 0)
                {
                    LogHelper.Warning("Unexpected result from call to EnumServicesStatusEx.");
                    return Array.Empty<string>();
                }

                if (Marshal.GetLastWin32Error() != NativeMethods.ERROR_MORE_DATA)
                {
                    LogHelper.Warning("Unable to retrieve data from SCManager.");
                    return Array.Empty<string>();
                }

                List<string> result = new List<string>();

                bool IsThereMore = true;
                while (IsThereMore)
                {
                    IsThereMore = false;
                    dwBufSize = dwBufNeed;
                    dwBufNeed = 0;
                    IntPtr buffer = Marshal.AllocHGlobal((int)dwBufSize);
                    try
                    {
                        resp = NativeMethods.EnumServicesStatusEx(hServiceManager, (int)NativeMethods.SC_ENUM_TYPE.SC_ENUM_PROCESS_INFO, (int)NativeMethods.SERVICE_TYPES.SERVICE_WIN32, (int)NativeMethods.SERVICE_STATE.SERVICE_ACTIVE, buffer, dwBufSize, out dwBufNeed, out ServicesReturned, ref ResumeHandle, null);
                        if (resp == 0)
                        {
                            uint resp2 = (uint)Marshal.GetLastWin32Error();
                            if (resp2 == NativeMethods.ERROR_MORE_DATA)
                            {
                                IsThereMore = true;
                            }
                            else
                            {
                                LogHelper.Error("Unable to retrieve data from SCManager.", new Win32Exception((int)resp2));
                                return null;
                            }
                        }
                        for (uint i = 0; i < ServicesReturned; i++)
                        {
                            IntPtr buffer2;
                            if (Environment.Is64BitProcess)
                            {
                                //8 byte packing on 64 bit OSes.
                                buffer2 = IntPtr.Add(buffer, (int)i * (NativeMethods.ENUM_SERVICE_STATUS_PROCESS.SizeOf + 4));
                            }
                            else
                            {
                                buffer2 = IntPtr.Add(buffer, (int)i * NativeMethods.ENUM_SERVICE_STATUS_PROCESS.SizeOf);
                            }
                            NativeMethods.ENUM_SERVICE_STATUS_PROCESS service = Marshal.PtrToStructure<NativeMethods.ENUM_SERVICE_STATUS_PROCESS>(buffer2);
                            if (pid == service.ServiceStatus.dwProcessId)
                            {
                                //We have found one of the services we're looking for!
                                result.Add(service.lpServiceName);
                            }
                        }
                    }
                    finally
                    {
                        Marshal.FreeHGlobal(buffer);
                    }
                }

                return result;
            }
            finally
            {
                NativeMethods.CloseServiceHandle(hServiceManager);
            }
        }

        // TODO: Commenting out as unused (replaced by better methods by fellow contributors :-) ). To be deleted if everything is working as expected.
        /*public static void GetService(uint pid, int threadid, string path, int protocol, int localport, string target, int remoteport, out string[] svc, out string[] svcdsc, out bool unsure)
        {
            // Try to lookup details about connection to localport.
            //@wokhan: how is this supposed to work since connection is blocked by firewall??
            LogHelper.Info("Trying to retrieve service name through connection information.");
            var ret = IPHelper.GetOwner(pid, localport);
            if (ret != null && !String.IsNullOrEmpty(ret.ModuleName))
            {
                // Returns the owner only if it's indeed a service.
                string ServiceDesc = GetServiceDesc(ret.ModuleName);

                if (String.IsNullOrEmpty(ServiceDesc))
                {
                    LogHelper.Debug("But no service description matches...");
                    svc = Array.Empty<string>();
                    svcdsc = Array.Empty<string>();
                    unsure = false;
                }
                else
                {
                    svc = new[] { ret.ModuleName };
                    svcdsc = new[] { GetServiceDesc(ret.ModuleName) };
                    unsure = false;
                    LogHelper.Debug("Identified service as: " + String.Join(",", svcdsc));
                }
                return;
            }

            // Try to retrieve the module name from the calling thread id.
            LogHelper.Info("Trying to retrieve service name through thread information.");
            if (threadid != 0)
            {
                Process? p;
                try
                {
                    p = Process.GetProcessById((int)pid);
                }
                catch (ArgumentException)
                {
                    p = null;
                }
                if (p != null)
                {
                    var thread = p.Threads.Cast<ProcessThread>().SingleOrDefault(t => t.Id == threadid);
                    if (thread is null)
                    {
                        LogHelper.Debug("The thread " + threadid + " has not been found for PID " + pid);
                    }
                    else
                    {
                        var thaddr = thread.StartAddress.ToInt64();
                        var module = p.Modules.Cast<ProcessModule>().FirstOrDefault(m => thaddr >= (m.BaseAddress.ToInt64() + m.ModuleMemorySize));
                        if (module is null)
                        {
                            LogHelper.Debug("The thread has been found, but no module matches.");
                        }
                        else
                        {
                            LogHelper.Debug("The thread has been found for module " + module.ModuleName);

                            string ServiceDesc = GetServiceDesc(module.ModuleName);

                            if (String.IsNullOrEmpty(ServiceDesc))
                            {
                                LogHelper.Debug("But no service description matches...");
                                svc = Array.Empty<string>();
                                svcdsc = Array.Empty<string>();
                                unsure = false;
                            }
                            else
                            {
                                svc = new[] { module.ModuleName };
                                svcdsc = new[] { ServiceDesc };
                                unsure = false;
                                LogHelper.Debug("Identified service as: " + String.Join(",", svcdsc));
                            }
                            return;
                        }
                    }
                }
            }

            LogHelper.Info("Trying to retrieve service name through process information.");
            var svcs = GetAllServices(pid);
            //int protocol = (int)Enum.Parse(typeof(NET_FW_IP_PROTOCOL_), protocolStr);

            if (!svcs.Any())
            {
                LogHelper.Debug("No services running in process " + pid.ToString() + " found!");
                svc = Array.Empty<string>();
                svcdsc = Array.Empty<string>();
                unsure = false;
                return;
            }

            //Only one service? Then we've probably found our guy!
            if (svcs.Count() == 1)
            {
                svc = svcs.ToArray();
                svcdsc = svcs.Select(s => GetServiceDesc(s)).ToArray();
                unsure = true;
                LogHelper.Debug("Identified service as: " + String.Join(",", svcdsc));
                return;
            }

            svc = Array.Empty<string>();

            // And if it still fails, fall backs to the most ugly way ever I am not able to get rid of :-P
            // Retrieves corresponding existing rules
            LogHelper.Info("Trying to retrieve service name through rule information.");
            var cRules = FirewallHelper.GetMatchingRules(path, GetAppPkgId(pid), protocol, target, remoteport.ToString(), localport.ToString(), svc, GetLocalUserOwner(pid), false, false)
                                       .Select(r => r.ServiceName)
                                       .Distinct()
                                       .Cast<string>()
                                       .ToList();

            // Trying to guess the corresponding service if not found with the previous method and if not already filtered
            svcs = svcs.Except(cRules, StringComparer.CurrentCultureIgnoreCase)
                       .ToArray();

            LogHelper.Debug("Excluding " + String.Join(",", cRules) + " // Remains " + String.Join(",", svcs));

            if (svcs.Any())
            {
                svc = svcs.ToArray();
                svcdsc = svcs.Select(s => GetServiceDesc(s)).ToArray();
                unsure = true;
                LogHelper.Debug("Identified service as: " + String.Join(",", svcdsc) + " (unsure)");
            }
            else
            {
                svcdsc = Array.Empty<string>();
                unsure = false;
                LogHelper.Debug("No service found!" + String.Join(",", svcdsc));
            }

            return;
        }*/

        /// <summary>
        /// Retrieve information about all services by pid
        /// </summary>
        /// <returns></returns>
        public static Dictionary<uint, ServiceInfoResult> GetAllServicesByPidWMI()
        {
            // use WMI "Win32_Service" query to get service names by pid
            // https://docs.microsoft.com/en-us/windows/win32/cimwin32prov/win32-service
            Dictionary<uint, ServiceInfoResult> dict = new Dictionary<uint, ServiceInfoResult>();
            using (var searcher = new ManagementObjectSearcher("SELECT ProcessId, Name, DisplayName, PathName FROM Win32_Service WHERE ProcessId != 0"))
            {
                using var results = searcher.Get();
                foreach (var r in results)
                {
                    //Console.WriteLine($"{r["processId"]} {r["Name"]}");
                    var pid = (uint)r["ProcessId"];
                    if (pid > 0 && !dict.ContainsKey(pid))
                    {
                        dict.Add(pid, new ServiceInfoResult(pid, (string)r["Name"], (string)r["DisplayName"], (string)r["PathName"]));
                    }
                }
            }
            return dict;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="service"></param>
        /// <returns></returns>
        private static string GetServiceDesc(string service)
        {
            string ret;
            try
            {
                using (var sc = new ServiceController(service))
                {
                    ret = sc.DisplayName;
                }

                return ret;
            }
            //There's an undocumented feature/bug where instead of ArgumentException, an InvalidOperationException is thrown.
            catch (Exception e) when (e is ArgumentException || e is InvalidOperationException)
            {
                LogHelper.Debug("Couldn't get description for service: " + service);
                return String.Empty;
            }
        }

        public static string GetAppPkgId(uint pid)
        {
            if (Environment.OSVersion.Version <= new Version(6, 2))
            {
                //Not Windows 8 or higher, there are no Apps
                return String.Empty;
            }

            IntPtr hProcess = NativeMethods.OpenProcess(NativeMethods.ProcessAccessFlags.QueryLimitedInformation, false, (uint)pid);
            if (hProcess == IntPtr.Zero)
            {
                LogHelper.Warning("Unable to retrieve process package id: process cannot be found!");
                return String.Empty;
            }
            try
            {
                //Based on: https://github.com/jimschubert/clr-profiler/blob/master/src/CLRProfiler45Source/WindowsStoreAppHelper/WindowsStoreAppHelper.cs
                uint packageFamilyNameLength = 0;
                StringBuilder packageFamilyNameBld = new StringBuilder();

                uint ret = NativeMethods.GetPackageFamilyName(hProcess, ref packageFamilyNameLength, packageFamilyNameBld);
                if ((ret == NativeMethods.APPMODEL_ERROR_NO_PACKAGE) || (packageFamilyNameLength == 0))
                {
                    // Not a WindowsStoreApp process
                    return String.Empty;
                }

                // Call again, now that we know the size
                packageFamilyNameBld = new StringBuilder((int)packageFamilyNameLength);
                ret = NativeMethods.GetPackageFamilyName(hProcess, ref packageFamilyNameLength, packageFamilyNameBld);
                if (ret != NativeMethods.ERROR_SUCCESS)
                {
                    LogHelper.Warning("Unable to retrieve process package id: failed to retrieve family package name!");
                    return String.Empty;
                }

                IntPtr pSID;
                ret = NativeMethods.DeriveAppContainerSidFromAppContainerName(packageFamilyNameBld.ToString(), out pSID);
                if (ret != NativeMethods.S_OK)
                {
                    LogHelper.Warning("Unable to retrieve process package id: failed to retrieve package SID!");
                    return String.Empty;
                }
                try
                {
                    string SID;
                    if (NativeMethods.ConvertSidToStringSid(pSID, out SID) == false)
                    {
                        LogHelper.Warning("Unable to retrieve process package id: SID cannot be converted!");
                        return String.Empty;
                    }

                    return SID;
                }
                finally
                {
                    NativeMethods.FreeSid(pSID);
                }
            }
            finally
            {
                NativeMethods.CloseHandle(hProcess);
            }
        }

        public static string GetLocalUserOwner(uint pid)
        {
            //Based on: https://bytes.com/topic/c-sharp/answers/225065-how-call-win32-native-api-gettokeninformation-using-c
            IntPtr hProcess = NativeMethods.OpenProcess(NativeMethods.ProcessAccessFlags.QueryInformation, false, (uint)pid);
            if (hProcess == IntPtr.Zero)
            {
                LogHelper.Warning($"Unable to retrieve process local user owner: process pid={pid} cannot be found!");
                return String.Empty;
            }
            try
            {
                IntPtr hToken;
                if (!NativeMethods.OpenProcessToken(hProcess, NativeMethods.TOKEN_QUERY, out hToken))
                {
                    LogHelper.Warning("Unable to retrieve process local user owner: process pid={pid} cannot be opened!");
                    return String.Empty;
                }
                try
                {
                    uint dwBufSize = 0;

                    if (NativeMethods.GetTokenInformation(hToken, NativeMethods.TOKEN_INFORMATION_CLASS.TokenUser, IntPtr.Zero, 0, ref dwBufSize))
                    {
                        LogHelper.Warning("Unexpected result from call to GetTokenInformation.");
                        return String.Empty;
                    }

                    IntPtr hTokenInformation = Marshal.AllocHGlobal((int)dwBufSize);
                    try
                    {
                        if (!NativeMethods.GetTokenInformation(hToken, NativeMethods.TOKEN_INFORMATION_CLASS.TokenUser, hTokenInformation, dwBufSize, ref dwBufSize))
                        {
                            LogHelper.Warning("Unable to retrieve process local user owner: token cannot be opened!");
                            return String.Empty;
                        }

                        string SID;
                        if (!NativeMethods.ConvertSidToStringSid(Marshal.PtrToStructure<NativeMethods.TOKEN_USER>(hTokenInformation).User.Sid, out SID))
                        {
                            LogHelper.Warning("Unable to retrieve process local user owner: SID cannot be converted!");
                            return String.Empty;
                        }

                        return SID;
                    }
                    finally
                    {
                        Marshal.FreeHGlobal(hTokenInformation);
                    }
                }
                finally
                {
                    NativeMethods.CloseHandle(hToken);
                }
            }
            finally
            {
                NativeMethods.CloseHandle(hProcess);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="p"></param>
        /// <param name="p_2"></param>
        /// <returns></returns>
        public static bool GetProcessFeedback(string cmd, string args, bool runas = false, bool dontwait = false)
        {
            try
            {
                ProcessStartInfo psiTaskTest = new ProcessStartInfo(cmd, args) { CreateNoWindow = true };

                if (runas)
                {
                    psiTaskTest.Verb = "runas";
                }
                else
                {
                    psiTaskTest.UseShellExecute = false;
                }

                Process procTaskTest = Process.Start(psiTaskTest);
                if (dontwait)
                {
                    procTaskTest.WaitForExit(100);
                    if (procTaskTest.HasExited)
                    {
                        return procTaskTest.ExitCode == 0;
                    }
                    else
                    {
                        return true;
                    }
                }
                else
                {
                    procTaskTest.WaitForExit();
                }

                return procTaskTest.ExitCode == 0;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        ///  Turns command line parameters into a dictionary to ease values retrieval
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public static Dictionary<string, string> ParseParameters(IList<string> args)
        {
            Dictionary<string, string>? ret = null;
            String key = String.Empty;
            try
            {
                ret = new Dictionary<string, string>(args.Count / 2);
                for (int i = args.Count % 2; i < args.Count; i += 2)
                {
                    key = args[i].TrimStart('-');
                    ret.Add(key, args[i + 1]);
                }
            }
            catch (Exception e)
            {
                LogHelper.Error("Unable to parse the parameters: key = " + key + " argv = " + String.Join(" ", args), e);
            }

            return ret ?? new Dictionary<string, string>();
        }

        /// <summary>
        /// Get the command-line of a running process id.<br>Use parseCommandLine to parse it into list of arguments</br>
        /// </summary>
        /// <param name="processId"></param>
        /// <returns>command-line or null</returns>
        public static string? GetCommandLineFromProcessWMI(uint processId)
        {
            try
            {
                using ManagementObjectSearcher clSearcher = new ManagementObjectSearcher($"SELECT CommandLine FROM Win32_Process WHERE ProcessId = {processId}");
                return String.Join(String.Empty, clSearcher.Get().Cast<ManagementObject>().Select(mObj => (string)mObj["CommandLine"]));
            }
            catch (Exception e)
            {
                LogHelper.Error($"Unable to get command-line from processId: {processId} - is process running?", e);
            }
            return null;
        }
        /// <summary>
        /// Parses a complete command-line with arguments provided as a string (support commands spaces and quotes).
        /// <para>Special keys in dictionary
        /// <br>key=@command  contains the command itself</br>
        /// <br>key=@arg[x] for args wihtout argname</br>
        /// </para>
        /// </summary>
        /// 
        /// <param name="cmdLine">command-line to parse e.g. "\"c:\\program files\\svchost.exe\" -k -s svcName -t \"some text\""</param>
        /// <returns>Dictionary with key-value pairs.<para>
        /// key=@command contains the command itself</para>
        /// <para>key=@arg[x] for args without key</para>
        /// <para>[-argname|/aname]</para>
        /// </returns>
        public static Dictionary<string, string?> ParseCommandLineArgs(string cmdLine)
        {
            // https://stackoverflow.com/questions/298830/split-string-containing-command-line-parameters-into-string-in-c-sharp
            // Fiddle link (regex): https://dotnetfiddle.net/PU7kXD

            string regEx = @"\G(""((""""|[^""])+)""|(\S+)) *";
            MatchCollection matches = Regex.Matches(cmdLine, regEx);
            List<string> args = matches.Cast<Match>().Select(m => Regex.Replace(m.Groups[2].Success ? m.Groups[2].Value : m.Groups[4].Value, @"""""", @"""")).ToList();
            return ParseCommandLineArgsToDict(args);
        }

        /// <summary>
        /// Creates a dictionary from a command-line arguments list.
        /// <para>Special keys in dictionary
        /// <br>key=@command  contains the command itself from the first element in the list</br>
        /// <br>key=@arg[x] for args wihtout argname</br>
        /// </para>
        /// </summary>
        /// 
        public static Dictionary<string, string?> ParseCommandLineArgsToDict(List<String> args)
        {
            // Fiddle link to test it: https://dotnetfiddle.net/PU7kXD
            Dictionary<string, string?> dict = new Dictionary<string, string?>(args.Count);
            for (int i = 0; i < args.Count; i++)
            {
                string key;
                string? val;
                if (args[i].StartsWith("-") || args[i].StartsWith("/"))
                {
                    key = args[i];
                    if ((i + 1) < args.Count && !args[i + 1].StartsWith("-") && !args[i + 1].StartsWith("/"))
                    {
                        val = args[i + 1];
                        i++;
                    }
                    else
                    {
                        val = null;
                    }
                }
                else
                {
                    // key=@command@ or argX 
                    key = (i == 0) ? "@command" : "@arg" + i;
                    val = args[i];
                }
                dict.Add(key, val);
            }

            return dict;
        }

        /// <summary>
        /// Finds the process by name and sets the main window to the foreground.
        /// Note: Process name is the cli executable excluding ".exe" e.g. "WFN" instead of "WFN.exe". 
        /// </summary>
        /// <param name="processName">Known process from enum</param>
        public static void StartOrRestoreToForeground(ProcessNames processName)
        {
            // TODO: check NullPointerRef
            Process bProcess = Process.GetProcessesByName(processName.ProcessName).FirstOrDefault();
            // check if the process is running
            if (bProcess != null)
            {
                // check if the window is hidden / minimized
                if (bProcess.MainWindowHandle == IntPtr.Zero)
                {
                    // the window is hidden so try to restore it before setting focus.
                    NativeMethods.ShowWindow(bProcess.Handle, NativeMethods.ShowWindowEnum.Restore);
                }

                // set user the focus to the window
                _ = NativeMethods.SetForegroundWindow(bProcess.MainWindowHandle);
            }
            else
            {
                _ = Process.Start(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, processName.FileName));
            }
        }

        /**
         * Starts a default shell executable (netcore31) with arguments and optional message box.
         * 
         */
        public static void StartShellExecutable(string executable, string args, bool showMessageBox)
        {
            try
            {
                LogHelper.Debug($"Starting shell executable: {executable}, args: {args}");
                Process.Start(new ProcessStartInfo(executable, args) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                LogHelper.Error($"{ex.Message}: {executable} {args}", ex);
                if (showMessageBox)
                {
                    MessageBox.Show($"Cannot start shell program: {executable}, Message: {ex.Message}");
                }
            }
        }
    }

}

