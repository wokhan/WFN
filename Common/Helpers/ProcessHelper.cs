using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.IO;
using System.Reflection;
using System.Text;
using System.Management;
using System.Text.RegularExpressions;
using System.Windows;

namespace Wokhan.WindowsFirewallNotifier.Common.Helpers
{
    public class ProcessHelper
    {
        [Flags]
        public enum ProcessAccessFlags : uint
        {
            All = 0x001F0FFF,
            Terminate = 0x00000001,
            CreateThread = 0x00000002,
            VirtualMemoryOperation = 0x00000008,
            VirtualMemoryRead = 0x00000010,
            VirtualMemoryWrite = 0x00000020,
            DuplicateHandle = 0x00000040,
            CreateProcess = 0x000000080,
            SetQuota = 0x00000100,
            SetInformation = 0x00000200,
            QueryInformation = 0x00000400,
            QueryLimitedInformation = 0x00001000,
            Synchronize = 0x00100000
        }

        private enum TOKEN_INFORMATION_CLASS
        {
            TokenUser = 1,
            TokenGroups,
            TokenPrivileges,
            TokenOwner,
            TokenPrimaryGroup,
            TokenDefaultDacl,
            TokenSource,
            TokenType,
            TokenImpersonationLevel,
            TokenStatistics,
            TokenRestrictedSids,
            TokenSessionId,
            TokenGroupsAndPrivileges,
            TokenSessionReference,
            TokenSandBoxInert,
            TokenAuditPolicy,
            TokenOrigin,
            TokenElevationType,
            TokenLinkedToken,
            TokenElevation,
            TokenHasRestrictions,
            TokenAccessInformation,
            TokenVirtualizationAllowed,
            TokenVirtualizationEnabled,
            TokenIntegrityLevel,
            TokenUIAccess,
            TokenMandatoryPolicy,
            TokenLogonSid,
            TokenIsAppContainer,
            TokenCapabilities,
            TokenAppContainerSid,
            TokenAppContainerNumber,
            TokenUserClaimAttributes,
            TokenDeviceClaimAttributes,
            TokenRestrictedUserClaimAttributes,
            TokenRestrictedDeviceClaimAttributes,
            TokenDeviceGroups,
            TokenRestrictedDeviceGroups,
            TokenSecurityAttributes,
            TokenIsRestricted,
            MaxTokenInfoClass
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct TOKEN_USER
        {
            public SID_AND_ATTRIBUTES User;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct SID_AND_ATTRIBUTES
        {
            public IntPtr Sid;
            public int Attributes;
        }

        public static void ElevateCurrentProcess()
        {
            ProcessStartInfo proc = new ProcessStartInfo();
            proc.UseShellExecute = true;
            proc.WorkingDirectory = Environment.CurrentDirectory;
            proc.FileName = Path.Combine(Assembly.GetCallingAssembly().Location);
            proc.Verb = "runas";

            Process.Start(proc);
        }

        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern IntPtr OpenSCManager(string machineName, string databaseName, uint dwAccess);

        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern uint EnumServicesStatusEx(IntPtr hSCManager,
               int infoLevel, uint dwServiceType,
               uint dwServiceState, IntPtr lpServices, uint cbBufSize,
               out uint pcbBytesNeeded, out uint lpServicesReturned,
               ref uint lpResumeHandle, string pszGroupName);

        [DllImport("advapi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CloseServiceHandle(IntPtr hSCObject);

        [Flags]
        private enum ACCESS_MASK : uint { DELETE = 0x00010000, READ_CONTROL = 0x00020000, WRITE_DAC = 0x00040000, WRITE_OWNER = 0x00080000, SYNCHRONIZE = 0x00100000, STANDARD_RIGHTS_REQUIRED = 0x000f0000, STANDARD_RIGHTS_READ = 0x00020000, STANDARD_RIGHTS_WRITE = 0x00020000, STANDARD_RIGHTS_EXECUTE = 0x00020000, STANDARD_RIGHTS_ALL = 0x001f0000, SPECIFIC_RIGHTS_ALL = 0x0000ffff, ACCESS_SYSTEM_SECURITY = 0x01000000, MAXIMUM_ALLOWED = 0x02000000, GENERIC_READ = 0x80000000, GENERIC_WRITE = 0x40000000, GENERIC_EXECUTE = 0x20000000, GENERIC_ALL = 0x10000000, DESKTOP_READOBJECTS = 0x00000001, DESKTOP_CREATEWINDOW = 0x00000002, DESKTOP_CREATEMENU = 0x00000004, DESKTOP_HOOKCONTROL = 0x00000008, DESKTOP_JOURNALRECORD = 0x00000010, DESKTOP_JOURNALPLAYBACK = 0x00000020, DESKTOP_ENUMERATE = 0x00000040, DESKTOP_WRITEOBJECTS = 0x00000080, DESKTOP_SWITCHDESKTOP = 0x00000100, WINSTA_ENUMDESKTOPS = 0x00000001, WINSTA_READATTRIBUTES = 0x00000002, WINSTA_ACCESSCLIPBOARD = 0x00000004, WINSTA_CREATEDESKTOP = 0x00000008, WINSTA_WRITEATTRIBUTES = 0x00000010, WINSTA_ACCESSGLOBALATOMS = 0x00000020, WINSTA_EXITWINDOWS = 0x00000040, WINSTA_ENUMERATE = 0x00000100, WINSTA_READSCREEN = 0x00000200, WINSTA_ALL_ACCESS = 0x0000037f }

        [Flags]
        private enum SERVICE_STATE : int { SERVICE_ACTIVE = 0x00000001, SERVICE_INACTIVE = 0x00000002, SERVICE_STATE_ALL = SERVICE_ACTIVE | SERVICE_INACTIVE }

        [Flags]
        private enum SERVICE_TYPES : int { SERVICE_KERNEL_DRIVER = 0x00000001, SERVICE_FILE_SYSTEM_DRIVER = 0x00000002, SERVICE_ADAPTER = 0x00000004, SERVICE_RECOGNIZER_DRIVER = 0x00000008, SERVICE_DRIVER = SERVICE_KERNEL_DRIVER | SERVICE_FILE_SYSTEM_DRIVER | SERVICE_RECOGNIZER_DRIVER, SERVICE_WIN32_OWN_PROCESS = 0x00000010, SERVICE_WIN32_SHARE_PROCESS = 0x00000020, SERVICE_WIN32 = SERVICE_WIN32_OWN_PROCESS | SERVICE_WIN32_SHARE_PROCESS, }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct SERVICE_STATUS { public static readonly int SizeOf = Marshal.SizeOf(typeof(SERVICE_STATUS)); public SERVICE_TYPES dwServiceType; public SERVICE_STATE dwCurrentState; public uint dwControlsAccepted; public uint dwWin32ExitCode; public uint dwServiceSpecificExitCode; public uint dwCheckPoint; public uint dwWaitHint; }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        private struct ENUM_SERVICE_STATUS_PROCESS
        {
            public static readonly int SizeOf = Marshal.SizeOf(typeof(ENUM_SERVICE_STATUS_PROCESS));

            [MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPTStr)]
            public string lpServiceName;

            [MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPTStr)]
            public string lpDisplayName;

            public SERVICE_STATUS_PROCESS ServiceStatus;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct SERVICE_STATUS_PROCESS
        {
            public static readonly int SizeOf = Marshal.SizeOf(typeof(SERVICE_STATUS_PROCESS));

            public uint dwServiceType;
            public uint dwCurrentState;
            public uint dwControlsAccepted;
            public uint dwWin32ExitCode;
            public uint dwServiceSpecificExitCode;
            public uint dwCheckPoint;
            public uint dwWaitHint;
            public uint dwProcessId;
            public uint dwServiceFlags;
        }

        [Flags]
        private enum SCM_ACCESS : uint
        {
            SC_MANAGER_CONNECT = 0x00001,
            SC_MANAGER_CREATE_SERVICE = 0x00002,
            SC_MANAGER_ENUMERATE_SERVICE = 0x00004,
            SC_MANAGER_LOCK = 0x00008,
            SC_MANAGER_QUERY_LOCK_STATUS = 0x00010,
            SC_MANAGER_MODIFY_BOOT_CONFIG = 0x00020,
            SC_MANAGER_ALL_ACCESS = ACCESS_MASK.STANDARD_RIGHTS_REQUIRED | SC_MANAGER_CONNECT | SC_MANAGER_CREATE_SERVICE | SC_MANAGER_ENUMERATE_SERVICE | SC_MANAGER_LOCK | SC_MANAGER_QUERY_LOCK_STATUS | SC_MANAGER_MODIFY_BOOT_CONFIG, GENERIC_READ = ACCESS_MASK.STANDARD_RIGHTS_READ | SC_MANAGER_ENUMERATE_SERVICE | SC_MANAGER_QUERY_LOCK_STATUS, GENERIC_WRITE = ACCESS_MASK.STANDARD_RIGHTS_WRITE | SC_MANAGER_CREATE_SERVICE | SC_MANAGER_MODIFY_BOOT_CONFIG, GENERIC_EXECUTE = ACCESS_MASK.STANDARD_RIGHTS_EXECUTE | SC_MANAGER_CONNECT | SC_MANAGER_LOCK, GENERIC_ALL = SC_MANAGER_ALL_ACCESS,
        }

        [Flags]
        private enum SC_ENUM_TYPE : uint
        {
            SC_ENUM_PROCESS_INFO = 0
        }

        private const uint ERROR_MORE_DATA = 234;

        //Note: Only exists on Windows 8 and higher
        /*[DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        private static extern uint GetPackageFullName(IntPtr hProcess, ref uint packageFullNameLength, StringBuilder packageFullName);*/

        //Note: Only exists on Windows 8 and higher
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        private static extern uint GetPackageFamilyName(IntPtr hProcess, ref uint packageFamilyNameLength, StringBuilder packageFamilyName);

        //Note: Only exists on Windows 8 and higher
        [DllImport("userenv.dll", CharSet = CharSet.Unicode)]
        private static extern uint DeriveAppContainerSidFromAppContainerName(string pszAppContainerName, out IntPtr ppsidAppContainerSid);

        private const uint ERROR_SUCCESS = 0;
        private const uint APPMODEL_ERROR_NO_PACKAGE = 15700;
        private const uint S_OK = 0x00000000;

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr OpenProcess(ProcessHelper.ProcessAccessFlags dwDesiredAccess, [MarshalAs(UnmanagedType.Bool)] bool bInheritHandle, uint dwProcessId);

        private const int TOKEN_QUERY = 0X00000008;

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool OpenProcessToken(IntPtr ProcessHandle, uint DesiredAccess, out IntPtr TokenHandle);

        [DllImport("advapi32", SetLastError = true, CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetTokenInformation(IntPtr hToken, TOKEN_INFORMATION_CLASS TokenInformationClass, IntPtr TokenInformation, uint dwTokenInfoLength, ref uint dwReturnLength);

        [DllImport("advapi32", SetLastError = true, CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool ConvertSidToStringSid(IntPtr pSID, [MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPTStr)] out string pStringSid);

        [DllImport("advapi32", CharSet = CharSet.Auto)]
        private static extern IntPtr FreeSid(IntPtr pSid);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CloseHandle(IntPtr hObject);

        public static string[] GetProcessOwnerWMI(int owningPid, ref Dictionary<int, string[]> previousCache)
        {
            if (previousCache == null)
            {
                using (var searcher = new ManagementObjectSearcher("SELECT ProcessId, Name, ExecutablePath, CommandLine FROM Win32_Process"))
                {
                    using (var results = searcher.Get())
                    {
                        previousCache = results.Cast<ManagementObject>()
                                               .ToDictionary(r => (int)(uint)r["ProcessId"],
                                                             r => new[] { (string)r["Name"], (string)r["ExecutablePath"], (string)r["CommandLine"] });
                    }
                }
            }

            return previousCache[owningPid];
        }

        public static string[] GetAllServices(int pid)
        {
            IntPtr hServiceManager = OpenSCManager(null, null, (uint)(SCM_ACCESS.SC_MANAGER_CONNECT | SCM_ACCESS.SC_MANAGER_ENUMERATE_SERVICE));
            if (hServiceManager == IntPtr.Zero)
            {
                LogHelper.Warning("Unable to open SCManager.");
                return null;
            }
            try
            {
                uint dwBufSize = 0;
                uint dwBufNeed = 0;
                uint ServicesReturned = 0;
                uint ResumeHandle = 0;

                var resp = EnumServicesStatusEx(hServiceManager, (int)SC_ENUM_TYPE.SC_ENUM_PROCESS_INFO, (int)SERVICE_TYPES.SERVICE_WIN32, (int)SERVICE_STATE.SERVICE_ACTIVE, IntPtr.Zero, dwBufSize, out dwBufNeed, out ServicesReturned, ref ResumeHandle, null);
                if (resp != 0)
                {
                    LogHelper.Warning("Unexpected result from call to EnumServicesStatusEx.");
                    return null;
                }

                if (Marshal.GetLastWin32Error() != ERROR_MORE_DATA)
                {
                    LogHelper.Warning("Unable to retrieve data from SCManager.");
                    return null;
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
                        resp = EnumServicesStatusEx(hServiceManager, (int)SC_ENUM_TYPE.SC_ENUM_PROCESS_INFO, (int)SERVICE_TYPES.SERVICE_WIN32, (int)SERVICE_STATE.SERVICE_ACTIVE, buffer, dwBufSize, out dwBufNeed, out ServicesReturned, ref ResumeHandle, null);
                        if (resp == 0)
                        {
                            uint resp2 = (uint)Marshal.GetLastWin32Error();
                            if (resp2 == ERROR_MORE_DATA)
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
                                buffer2 = IntPtr.Add(buffer, (int)i * (ENUM_SERVICE_STATUS_PROCESS.SizeOf + 4));
                            }
                            else
                            {
                                buffer2 = IntPtr.Add(buffer, (int)i * ENUM_SERVICE_STATUS_PROCESS.SizeOf);
                            }
                            ENUM_SERVICE_STATUS_PROCESS service = (ENUM_SERVICE_STATUS_PROCESS)Marshal.PtrToStructure(buffer2, typeof(ENUM_SERVICE_STATUS_PROCESS));
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

                return result.ToArray();
            }
            finally
            {
                CloseServiceHandle(hServiceManager);
            }
        }

        public static void GetService(int pid, int threadid, string path, int protocol, int localport, string target, int remoteport, out string[] svc, out string[] svcdsc, out bool unsure)
        {
            // Try to lookup details about connection to localport.
            //@wokhan: how is this supposed to work since connection is blocked by firewall??
            LogHelper.Info("Trying to retrieve service name through connection information.");
            var ret = IPHelper.GetOwner(pid, localport);
            if (ret != null && !String.IsNullOrEmpty(ret.ModuleName))
            {
                // Returns the owner only if it's indeed a service.
                string ServiceDesc = getServiceDesc(ret.ModuleName);

                if (String.IsNullOrEmpty(ServiceDesc))
                {
                    LogHelper.Debug("But no service description matches...");
                    svc = new string[0];
                    svcdsc = new string[0];
                    unsure = false;
                }
                else
                {
                    svc = new[] { ret.ModuleName };
                    svcdsc = new[] { getServiceDesc(ret.ModuleName) };
                    unsure = false;
                    LogHelper.Debug("Identified service as: " + String.Join(",", svcdsc));
                }
                return;
            }

            // Try to retrieve the module name from the calling thread id.
            LogHelper.Info("Trying to retrieve service name through thread information.");
            if (threadid != 0)
            {
                Process p;
                try
                {
                    p = Process.GetProcessById(pid);
                }
                catch (ArgumentException)
                {
                    p = null;
                }
                if (p != null)
                {
                    var thread = p.Threads.Cast<ProcessThread>().SingleOrDefault(t => t.Id == threadid);
                    if (thread == null)
                    {
                        LogHelper.Debug("The thread " + threadid + " has not been found for PID " + pid);
                    }
                    else
                    {
                        var thaddr = thread.StartAddress.ToInt64();
                        var module = p.Modules.Cast<ProcessModule>().FirstOrDefault(m => thaddr >= (m.BaseAddress.ToInt64() + m.ModuleMemorySize));
                        if (module == null)
                        {
                            LogHelper.Debug("The thread has been found, but no module matches.");
                        }
                        else
                        {
                            LogHelper.Debug("The thread has been found for module " + module.ModuleName);

                            string ServiceDesc = getServiceDesc(module.ModuleName);

                            if (String.IsNullOrEmpty(ServiceDesc))
                            {
                                LogHelper.Debug("But no service description matches...");
                                svc = new string[0];
                                svcdsc = new string[0];
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
            string[] svcs = GetAllServices(pid);
            //int protocol = (int)Enum.Parse(typeof(NET_FW_IP_PROTOCOL_), protocolStr);

            if (svcs == null)
            {
                LogHelper.Debug("No services running in process " + pid.ToString() + " found!");
                svc = new string[0];
                svcdsc = new string[0];
                unsure = false;
                return;
            }

            //Only one service? Then we've probably found our guy!
            if (svcs.Length == 1)
            {
                svc = svcs;
                svcdsc = svcs.Select(s => getServiceDesc(s)).ToArray();
                unsure = true;
                LogHelper.Debug("Identified service as: " + String.Join(",", svcdsc));
                return;
            }

            svc = new string[0];

            // And if it still fails, fall backs to the most ugly way ever I am not able to get rid of :-P
            // Retrieves corresponding existing rules
            LogHelper.Info("Trying to retrieve service name through rule information.");
            int profile = FirewallHelper.GetCurrentProfile();
            var cRules = FirewallHelper.GetMatchingRules(path, getAppPkgId(pid), protocol, target, remoteport.ToString(), localport.ToString(), svc, getLocalUserOwner(pid), false, false)
                                       .Select(r => r.ServiceName)
                                       .Distinct()
                                       .ToList();

            // Trying to guess the corresponding service if not found with the previous method and if not already filtered
            svcs = svcs.Except(cRules, StringComparer.CurrentCultureIgnoreCase)
                       .ToArray();

            LogHelper.Debug("Excluding " + String.Join(",", cRules) + " // Remains " + String.Join(",", svcs));

            if (svcs.Length > 0)
            {
                svc = svcs;
                svcdsc = svcs.Select(s => getServiceDesc(s)).ToArray();
                unsure = true;
                LogHelper.Debug("Identified service as: " + String.Join(",", svcdsc) + " (unsure)");
            }
            else
            {
                svcdsc = new string[0];
                unsure = false;
                LogHelper.Debug("No service found!" + String.Join(",", svcdsc));
            }

            return;
        }

        public class ServiceInfoResult
        {
            public int ProcessId { get; set; }
            public string Name { get; set; }
            public string DisplayName { get; set; }
            public string PathName { get; set; }
        }
        /// <summary>
        /// Retrieve information about all services by pid
        /// </summary>
        /// <returns></returns>
        public static Dictionary<int, ServiceInfoResult> GetAllServicesByPidWMI()
        {
            // use WMI "Win32_Service" query to get service names by pid
            // https://docs.microsoft.com/en-us/windows/win32/cimwin32prov/win32-service
            Dictionary<int, ServiceInfoResult> dict = new Dictionary<int, ServiceInfoResult>();
            using (var searcher = new ManagementObjectSearcher("SELECT ProcessId, Name, DisplayName, PathName FROM Win32_Service WHERE ProcessId != 0"))
            {
                using (var results = searcher.Get())
                {
                    foreach (var r in results)
                    {
                        //Console.WriteLine($"{r["processId"]} {r["Name"]}");
                        int pid = (int)(uint)r["ProcessId"];
                        if (pid > 0 && !dict.ContainsKey(pid))
                        {
                            ServiceInfoResult si = new ServiceInfoResult()
                            {
                                ProcessId = pid,
                                Name = (string)r["Name"],
                                DisplayName = (string)r["DisplayName"],
                                PathName = (string)r["PathName"]
                            };
                            dict.Add(pid, si);
                        }
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
        private static string getServiceDesc(string service)
        {
            string ret;
            try
            {
                using (ServiceController sc = new ServiceController(service))
                {
                    ret = sc.DisplayName;
                }

                return ret;
            }
            catch (ArgumentException)
            {
                LogHelper.Debug("Couldn't get description for service: " + service);
                return String.Empty;
            }
            //There's an undocumented feature/bug where instead of ArgumentException, an InvalidOperationException is thrown.
            catch (InvalidOperationException) //FIXME: Add undocumented System. ?
            {
                LogHelper.Debug("Couldn't get description for service: " + service);
                return String.Empty;
            }
        }

        public static string getAppPkgId(int pid)
        {
            if (Environment.OSVersion.Version <= new System.Version(6, 2))
            {
                //Not Windows 8 or higher, there are no Apps
                return String.Empty;
            }

            IntPtr hProcess = OpenProcess(ProcessHelper.ProcessAccessFlags.QueryLimitedInformation, false, (uint)pid);
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

                uint ret = GetPackageFamilyName(hProcess, ref packageFamilyNameLength, packageFamilyNameBld);
                if ((ret == APPMODEL_ERROR_NO_PACKAGE) || (packageFamilyNameLength == 0))
                {
                    // Not a WindowsStoreApp process
                    return String.Empty;
                }

                // Call again, now that we know the size
                packageFamilyNameBld = new StringBuilder((int)packageFamilyNameLength);
                ret = GetPackageFamilyName(hProcess, ref packageFamilyNameLength, packageFamilyNameBld);
                if (ret != ERROR_SUCCESS)
                {
                    LogHelper.Warning("Unable to retrieve process package id: failed to retrieve family package name!");
                    return String.Empty;
                }

                IntPtr pSID;
                ret = DeriveAppContainerSidFromAppContainerName(packageFamilyNameBld.ToString(), out pSID);
                if (ret != S_OK)
                {
                    LogHelper.Warning("Unable to retrieve process package id: failed to retrieve package SID!");
                    return String.Empty;
                }
                try
                {
                    string SID;
                    if (ConvertSidToStringSid(pSID, out SID) == false)
                    {
                        LogHelper.Warning("Unable to retrieve process package id: SID cannot be converted!");
                        return String.Empty;
                    }

                    return SID;
                }
                finally
                {
                    FreeSid(pSID);
                }
            }
            finally
            {
                CloseHandle(hProcess);
            }
        }

        public static string getLocalUserOwner(int pid)
        {
            //Based on: https://bytes.com/topic/c-sharp/answers/225065-how-call-win32-native-api-gettokeninformation-using-c
            IntPtr hProcess = OpenProcess(ProcessHelper.ProcessAccessFlags.QueryInformation, false, (uint)pid);
            if (hProcess == IntPtr.Zero)
            {
                LogHelper.Warning($"Unable to retrieve process local user owner: process pid={pid} cannot be found!");
                return String.Empty;
            }
            try
            {
                IntPtr hToken;
                if (OpenProcessToken(hProcess, TOKEN_QUERY, out hToken) == false)
                {
                    LogHelper.Warning("Unable to retrieve process local user owner: process pid={pid} cannot be opened!");
                    return String.Empty;
                }
                try
                {
                    uint dwBufSize = 0;

                    if (GetTokenInformation(hToken, TOKEN_INFORMATION_CLASS.TokenUser, IntPtr.Zero, 0, ref dwBufSize) != false)
                    {
                        LogHelper.Warning("Unexpected result from call to GetTokenInformation.");
                        return String.Empty;
                    }

                    IntPtr hTokenInformation = Marshal.AllocHGlobal((int)dwBufSize);
                    try
                    {
                        if (GetTokenInformation(hToken, TOKEN_INFORMATION_CLASS.TokenUser, hTokenInformation, dwBufSize, ref dwBufSize) == false)
                        {
                            LogHelper.Warning("Unable to retrieve process local user owner: token cannot be opened!");
                            return String.Empty;
                        }

                        string SID;
                        if (ConvertSidToStringSid(((TOKEN_USER)Marshal.PtrToStructure(hTokenInformation, typeof(TOKEN_USER))).User.Sid, out SID) == false)
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
                    CloseHandle(hToken);
                }
            }
            finally
            {
                CloseHandle(hProcess);
            }
        }

        public static bool getProcessFeedback(string cmd, string args)
        {
            return getProcessFeedback(cmd, args, false, false);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="p"></param>
        /// <param name="p_2"></param>
        /// <returns></returns>
        public static bool getProcessFeedback(string cmd, string args, bool runas, bool dontwait)
        {
            try
            {
                ProcessStartInfo psiTaskTest = new ProcessStartInfo(cmd, args);
                psiTaskTest.CreateNoWindow = true;
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

                return (procTaskTest.ExitCode == 0);
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
            Dictionary<string, string> ret = null;
            String key = "";
            try
            {
                ret = new Dictionary<string, string>(args.Count / 2);
                for (int i = args.Count % 2; i < args.Count(); i += 2)
                {
                    key = args[i].TrimStart('-');
                    ret.Add(key, args[i + 1]);
                }
            }
            catch (Exception e)
            {
                LogHelper.Error("Unable to parse the parameters: key = " + key + " argv = " + String.Join(" ", args), e);
            }

            return ret;
        }

        /// <summary>
        /// Get the command-line of a running process id.<br>Use parseCommandLine to parse it into list of arguments</br>
        /// </summary>
        /// <param name="processId"></param>
        /// <returns>command-line or null</returns>
        public static String getCommandLineFromProcessWMI(int processId)
        {
            try
            {
                using (ManagementObjectSearcher clSearcher = new ManagementObjectSearcher(
                    "SELECT CommandLine FROM Win32_Process WHERE ProcessId = " + processId))
                {
                    String cLine = "";
                    foreach (ManagementObject mObj in clSearcher.Get())
                    {
                        cLine += (String)mObj["CommandLine"];
                    }
                    return cLine;
                }
            }
            catch (Exception e)
            {
                LogHelper.Error("Unable to get command-line from processId: " + processId + " - is process running?", e);
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
        public static Dictionary<string, string> ParseCommandLineArgs(string cmdLine)
        {
            // https://stackoverflow.com/questions/298830/split-string-containing-command-line-parameters-into-string-in-c-sharp
            // Fiddle link (regex): https://dotnetfiddle.net/PU7kXD

            string regEx = @"\G(""((""""|[^""])+)""|(\S+)) *";
            MatchCollection matches = Regex.Matches(cmdLine, regEx);
            List<String> args = matches.Cast<Match>().Select(m => Regex.Replace(m.Groups[2].Success ? m.Groups[2].Value : m.Groups[4].Value, @"""""", @"""")).ToList();
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
        public static Dictionary<string, string> ParseCommandLineArgsToDict(List<String> args)
        {
            // Fiddle link to test it: https://dotnetfiddle.net/PU7kXD
            Dictionary<string, string> dict = new Dictionary<string, string>(args.Count);
            for (int i = 0; i < args.Count(); i++)
            {
                string key, val;
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

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
        private static extern bool ShowWindow(IntPtr hWnd, ShowWindowEnum flags);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern int SetForegroundWindow(IntPtr hwnd);

        private enum ShowWindowEnum
        {
            Hide = 0,
            ShowNormal = 1, ShowMinimized = 2, ShowMaximized = 3,
            Maximize = 3, ShowNormalNoActivate = 4, Show = 5,
            Minimize = 6, ShowMinNoActivate = 7, ShowNoActivate = 8,
            Restore = 9, ShowDefault = 10, ForceMinimized = 11
        };

        /**
         * Finds the process by name and sets the main window to the foreground.
         */
        public static void RestoreProcessWindowState(string processName)
        {
            // get the process
            Process bProcess = Process.GetProcessesByName(processName).FirstOrDefault();

            // check if the process is running
            if (bProcess != null)
            {
                // check if the window is hidden / minimized
                if (bProcess.MainWindowHandle == IntPtr.Zero)
                {
                    // the window is hidden so try to restore it before setting focus.
                    ShowWindow(bProcess.Handle, ShowWindowEnum.Restore);
                }

                // set user the focus to the window
                SetForegroundWindow(bProcess.MainWindowHandle);
            }
            else
            {
                // the process is not running, so start it
                Process.Start(processName);
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

