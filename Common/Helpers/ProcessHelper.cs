using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.IO;
using System.Reflection;
using System.Management;

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

        public static void ElevateCurrentProcess()
        {
            ProcessStartInfo proc = new ProcessStartInfo();
            proc.UseShellExecute = true;
            proc.WorkingDirectory = Environment.CurrentDirectory;
            proc.FileName = Path.Combine(Assembly.GetCallingAssembly().Location);
            proc.Verb = "runas";

            Process.Start(proc);
        }

        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern IntPtr OpenSCManager(string machineName, string databaseName, uint dwAccess);

        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Auto)]
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

            public int dwServiceType;
            public int dwCurrentState;
            public int dwControlsAccepted;
            public int dwWin32ExitCode;
            public int dwServiceSpecificExitCode;
            public int dwCheckPoint;
            public int dwWaitHint;
            public int dwProcessId;
            public int dwServiceFlags;
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

        public static string[] GetProcessOwnerWMI(int owningPid, ref Dictionary<int, string[]> previousCache)
        {
            if (previousCache == null)
            {
                using (var searcher = new ManagementObjectSearcher("SELECT ProcessId, Name, ExecutablePath FROM Win32_Process"))
                {
                    using (var results = searcher.Get())
                    {
                        previousCache = results.Cast<ManagementObject>()
                                               .ToDictionary(r => (int)(uint)r["ProcessId"], 
                                                             r => new[] { (string)r["Name"], (string)r["ExecutablePath"] });
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

            uint dwBufSize = 0;
            uint dwBufNeed = 0;
            uint ServicesReturned = 0;
            uint ResumeHandle = 0;

            var resp = EnumServicesStatusEx(hServiceManager, (int)SC_ENUM_TYPE.SC_ENUM_PROCESS_INFO, (int)SERVICE_TYPES.SERVICE_WIN32, (int)SERVICE_STATE.SERVICE_ACTIVE, IntPtr.Zero, dwBufSize, out dwBufNeed, out ServicesReturned, ref ResumeHandle, null);
            if (resp != 0)
            {
                LogHelper.Warning("Unexpected result from call to EnumServicesStatusEx.");
                CloseServiceHandle(hServiceManager);
                return null;
            }

            if (Marshal.GetLastWin32Error() != ERROR_MORE_DATA)
            {
                LogHelper.Warning("Unable to retrieve data from SCManager.");
                CloseServiceHandle(hServiceManager);
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
                        Marshal.FreeHGlobal(buffer);
                        CloseServiceHandle(hServiceManager);
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

                Marshal.FreeHGlobal(buffer);
            }

            CloseServiceHandle(hServiceManager);

            return result.ToArray();
        }

        public static void GetService(int pid, int threadid, string path, string protocolStr, string localport, string target, string remoteport, out string[] svc, out string[] svcdsc, out bool unsure)
        {
            // Try to lookup details about connection to localport.
            //@wokhan: how is this supposed to work since connection is blocked by firewall??
            LogHelper.Info("Trying to retrieve service name through connection information.");
            var ret = IPHelper.GetOwner(pid, int.Parse(localport));
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

            //Only one service? Then we've found our guy!
            if (svcs.Length == 1)
            {
                svc = svcs;
                svcdsc = svcs.Select(s => getServiceDesc(s)).ToArray();
                unsure = false;
                LogHelper.Debug("Identified service as: " + String.Join(",", svcdsc));
                return;
            }

            svc = new string[0];

            // And if it still fails, fall backs to the most ugly way ever I am not able to get rid of :-P
            // Retrieves corresponding existing rules
            LogHelper.Info("Trying to retrieve service name through rule information.");
            int profile = FirewallHelper.GetCurrentProfile();
            var cRules = FirewallHelper.GetMatchingRules(path, protocolStr, target, remoteport, localport, svc, true)
                                       .Select(r => r.serviceName)
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
        /// 
        /// </summary>
        /// <param name="cmd"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public static string getProcessResponse(string cmd, string args)
        {
            ProcessStartInfo psiTaskTest = new ProcessStartInfo(cmd, args);
            psiTaskTest.CreateNoWindow = true;
            psiTaskTest.UseShellExecute = false;
            psiTaskTest.RedirectStandardOutput = true;

            Process procTaskTest = Process.Start(psiTaskTest);

            string ret = procTaskTest.StandardOutput.ReadToEnd();
            procTaskTest.Close();
            //procTaskTest.WaitForExit();

            //Application.DoEvents();

            return ret;
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
                LogHelper.Error("Unable to parse the parameters: key = "+ key + " argv = " + String.Join(" ", args), e);
            }

            return ret;
        }
    }
}
