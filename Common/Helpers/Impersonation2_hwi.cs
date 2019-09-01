using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Security.Principal;
using System.ComponentModel;
using System.Security;
using log4net;
using LogManager = log4net.LogManager;

namespace Wokhan.WindowsFirewallNotifier.Common.Helpers
{
    class Impersonation2_hwi
    {

        #region DLL Imports
        internal const int SE_PRIVILEGE_ENABLED = 0x00000002;
        internal const int TOKEN_QUERY = 0x00000008;
        internal const int TOKEN_ADJUST_PRIVILEGES = 0x00000020;
        internal const int TOKEN_ASSIGN_PRIMARY = 0x0001;
        internal const int TOKEN_DUPLICATE = 0x0002;
        internal const int TOKEN_IMPERSONATE = 0X00000004;
        internal const int TOKEN_ADJUST_DEFAULT = 0x0080;
        internal const int TOKEN_ADJUST_SESSIONID = 0x0100;
        internal const int MAXIMUM_ALLOWED = 0x2000000;
        internal const int CREATE_UNICODE_ENVIRONMENT = 0x00000400;
        internal const int NORMAL_PRIORITY_CLASS = 0x20;
        internal const int CREATE_NEW_CONSOLE = 0x00000010;

        internal const string SE_SHUTDOWN_NAME = "SeShutdownPrivilege";
        internal const string SE_TCB_NAME = "SeTcbPrivilege";
        internal const string SE_RESTORE_NAME = "SeRestorePrivilege";

        private static WindowsImpersonationContext impersonatedUser;
        public static IntPtr hToken = IntPtr.Zero;
        public static IntPtr dupeTokenHandle = IntPtr.Zero;
        const string SE_INCREASE_QUOTA_NAME = "SeIncreaseQuotaPrivilege";

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal struct TokPriv1Luid
        {
            public int Count;
            public long Luid;
            public int Attr;
        }

        struct PROCESS_INFORMATION
        {
            public IntPtr hProcess;
            public IntPtr hThread;
            public uint dwProcessId;
            public uint dwThreadId;
        }

        struct STARTUPINFO
        {
            public uint cb;
            public string lpReserved;
            public string lpDesktop;
            public string lpTitle;
            public uint dwX;
            public uint dwY;
            public uint dwXSize;
            public uint dwYSize;
            public uint dwXCountChars;
            public uint dwYCountChars;
            public uint dwFillAttribute;
            public uint dwFlags;
            public short wShowWindow;
            public short cbReserved2;
            public IntPtr lpReserved2;
            public IntPtr hStdInput;
            public IntPtr hStdOutput;
            public IntPtr hStdError;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct SECURITY_ATTRIBUTES
        {
            public int nLength;
            public IntPtr lpSecurityDescriptor;
            public int bInheritHandle;
        }
        public enum ShowCommands : int
        {
            SW_HIDE = 0,
            SW_SHOWNORMAL = 1,
            SW_NORMAL = 1,
            SW_SHOWMINIMIZED = 2,
            SW_SHOWMAXIMIZED = 3,
            SW_MAXIMIZE = 3,
            SW_SHOWNOACTIVATE = 4,
            SW_SHOW = 5,
            SW_MINIMIZE = 6,
            SW_SHOWMINNOACTIVE = 7,
            SW_SHOWNA = 8,
            SW_RESTORE = 9,
            SW_SHOWDEFAULT = 10,
            SW_FORCEMINIMIZE = 11,
            SW_MAX = 11
        }

        [DllImport("shell32.dll")]
        static extern IntPtr ShellExecute(
            IntPtr hwnd,
            string lpOperation,
            string lpFile,
            string lpParameters,
            string lpDirectory,
            ShowCommands nShowCmd);

        [DllImport("advapi32.dll", SetLastError = true)]
        static extern int ImpersonateLoggedOnUser(IntPtr hToken);

        [DllImport("advapi32.dll", SetLastError = true)]
        internal static extern bool LookupPrivilegeValue(string host, string name, ref long pluid);

        [DllImport("advapi32.dll", ExactSpelling = true, SetLastError = true)]
        internal static extern bool AdjustTokenPrivileges(IntPtr htok, bool disall, ref TokPriv1Luid newst, int len, IntPtr prev, IntPtr relen);

        [DllImport("kernel32", SetLastError = true), SuppressUnmanagedCodeSecurityAttribute]
        static extern bool CloseHandle(IntPtr handle);

        [DllImport("advapi32.dll", ExactSpelling = true, SetLastError = true)]
        internal static extern bool OpenProcessToken(IntPtr h, int acc, ref IntPtr phtok);

        [DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public extern static bool DuplicateToken(IntPtr ExistingTokenHandle, int SECURITY_IMPERSONATION_LEVEL, ref IntPtr DuplicateTokenHandle);

        [DllImport("advapi32.dll", EntryPoint = "DuplicateTokenEx")]
        static extern bool DuplicateTokenEx(IntPtr hExistingToken, Int32 dwDesiredAccess,
                            ref SECURITY_ATTRIBUTES lpThreadAttributes,
                            Int32 ImpersonationLevel, Int32 dwTokenType,
                            ref IntPtr phNewToken);

        [DllImport("userenv.dll", SetLastError = true)]
        static extern bool CreateEnvironmentBlock(out IntPtr lpEnvironment, IntPtr hToken, bool bInherit);

        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        static extern bool CreateProcessAsUser(
            IntPtr hToken,
            string lpApplicationName,
            string lpCommandLine,
            ref SECURITY_ATTRIBUTES lpProcessAttributes,
            ref SECURITY_ATTRIBUTES lpThreadAttributes,
            bool bInheritHandles,
            uint dwCreationFlags,
            IntPtr lpEnvironment,
            string lpCurrentDirectory,
            ref STARTUPINFO lpStartupInfo,
            out PROCESS_INFORMATION lpProcessInformation);
        #endregion


        private static readonly ILog log = LogManager.GetLogger(typeof(Impersonation));

        private static void WriteToLog(string message)
        {
            log.Debug(message);
        }

        /// <summary>
        /// Duplicates the token information derived 
        /// from the logged in user's credentials. This 
        /// is required to run the application on the 
        /// logged in users desktop.
        /// </summary>
        /// <returns>Returns true if the application was successfully started in the user's desktop.</returns>
        public static bool ExecuteAppAsLoggedOnUser(string AppName, string CmdLineArgs)
        {
            WriteToLog("In ExecuteAppAsLoggedOnUser for all users.");
            IntPtr LoggedInUserToken = IntPtr.Zero;
            IntPtr DuplicateToken = IntPtr.Zero;
            IntPtr ShellProcessToken = IntPtr.Zero;

            if (!OpenProcessToken(Process.GetCurrentProcess().Handle, TOKEN_ADJUST_PRIVILEGES, ref LoggedInUserToken))
            {
                WriteToLog("OpenProcessToken failed: " + Marshal.GetLastWin32Error());
                return false;
            }
            else
            {
                //Below part for increasing the UAC previleges to the token.
                TokPriv1Luid tp = new TokPriv1Luid();
                tp.Count = 1;
                tp.Luid = 0;
                if (!LookupPrivilegeValue(null, SE_INCREASE_QUOTA_NAME, ref tp.Luid))
                {
                    WriteToLog("LookupPrivilegeValue failed: " + Marshal.GetLastWin32Error());
                    return false;
                }

                tp.Attr = SE_PRIVILEGE_ENABLED;
                if (!AdjustTokenPrivileges(LoggedInUserToken, false, ref tp, 0, IntPtr.Zero, IntPtr.Zero))
                {
                    WriteToLog("OpenProcessToken failed: " + Marshal.GetLastWin32Error());
                    return false;
                }
                CloseHandle(LoggedInUserToken);
            }

            List<Process> explorerProcessList = new List<Process>();
            string trayProcessName = AppName.Substring(AppName.LastIndexOf(@"\") + 1, AppName.Length - AppName.LastIndexOf(@"\") - 5);
            foreach (Process explorerProcess in Process.GetProcessesByName("explorer"))
            {
                bool IsProcessRunningForUser = false;
                foreach (Process PHTrayProcess in Process.GetProcessesByName(trayProcessName))
                {
                    if (explorerProcess.SessionId == PHTrayProcess.SessionId)
                    {
                        if (log.IsDebugEnabled) log.Debug(trayProcessName + " is already running for user SessionId " + explorerProcess.SessionId);
                        IsProcessRunningForUser = true;
                        break;
                    }
                }

                if (((Environment.OSVersion.Version.Major > 5 && explorerProcess.SessionId > 0)
                    || Environment.OSVersion.Version.Major == 5)
                    && !IsProcessRunningForUser)
                {
                    if (log.IsDebugEnabled) log.Debug(trayProcessName + " is not running for user SessionId " + explorerProcess.SessionId);
                    explorerProcessList.Add(explorerProcess);
                }
            }

            if (null != explorerProcessList && explorerProcessList.Count > 0)
            {
                foreach (Process explorerProcess in explorerProcessList)
                {
                    Process ShellProcess = explorerProcess;
                    ShellProcess.StartInfo.LoadUserProfile = true;

                    try
                    {
                        int tokenRights = MAXIMUM_ALLOWED; //TOKEN_ADJUST_PRIVILEGES | TOKEN_QUERY | TOKEN_ASSIGN_PRIMARY | TOKEN_DUPLICATE | TOKEN_ADJUST_DEFAULT | TOKEN_ADJUST_SESSIONID;
                        if (!OpenProcessToken(ShellProcess.Handle, tokenRights, ref ShellProcessToken))
                        {
                            WriteToLog("Unable to OpenProcessToken " + Marshal.GetLastWin32Error());
                            return false;
                        }

                        SECURITY_ATTRIBUTES sa = new SECURITY_ATTRIBUTES();
                        sa.nLength = Marshal.SizeOf(sa);

                        if (!DuplicateTokenEx(ShellProcessToken, tokenRights, ref sa, 2, 1, ref DuplicateToken))
                        {
                            WriteToLog("Unable to duplicate token " + Marshal.GetLastWin32Error());
                            return false;
                        }

                        WriteToLog("Duplicated the token " + WindowsIdentity.GetCurrent().Name);

                        SECURITY_ATTRIBUTES processAttributes = new SECURITY_ATTRIBUTES();
                        SECURITY_ATTRIBUTES threadAttributes = new SECURITY_ATTRIBUTES();
                        PROCESS_INFORMATION pi;
                        STARTUPINFO si = new STARTUPINFO();
                        si.cb = (uint)Marshal.SizeOf(si);

                        IntPtr UserEnvironment = IntPtr.Zero;
                        uint dwCreationFlags = NORMAL_PRIORITY_CLASS | CREATE_NEW_CONSOLE;
                        if (!CreateEnvironmentBlock(out UserEnvironment, ShellProcessToken, true))
                        {
                            WriteToLog("Unable to create user's enviroment block " + Marshal.GetLastWin32Error());
                        }
                        else
                        {
                            dwCreationFlags |= CREATE_UNICODE_ENVIRONMENT;
                        }
                        //string userName = getUserName(UserEnvironment);

                        //WriteToLog("UserName:::" + userName);

                        if (!CreateProcessAsUser(DuplicateToken, AppName, (CmdLineArgs == null) ? string.Empty : CmdLineArgs, ref processAttributes, ref threadAttributes, true, dwCreationFlags, UserEnvironment, AppName.Substring(0, AppName.LastIndexOf('\\')), ref si, out pi))
                        {
                            WriteToLog("Unable to create process " + Marshal.GetLastWin32Error());
                            if (Marshal.GetLastWin32Error() == 740)
                            {
                                WriteToLog("Please check the installation as some elevated permissions is required to execute the binaries");
                            }
                            return false;
                        }
                        Process trayApp = Process.GetProcessById(Convert.ToInt32(pi.dwProcessId));
                        trayApp.StartInfo.LoadUserProfile = true;
                    }
                    finally
                    {
                        if (ShellProcessToken != null) CloseHandle(ShellProcessToken);
                        if (DuplicateToken != null) CloseHandle(DuplicateToken);
                    }
                }
            }
            else
            {
                WriteToLog("No user has been identified to have logged into the system.");
                return false;
            }
            WriteToLog("Finished ExecuteAppAsLoggedOnUser for all users.");
            return true;
        }

        /// <summary>
        /// Impersonate the user credentials. This would be required by 
        /// the service applications to impersonate the logged in user
        /// credentials to launch certain applications or applying the
        /// power scheme.
        /// </summary>
        /// <returns>Returns true if the impersonation is successful.</returns>
        public static bool ImpersonateUser()
        {
            // For simplicity I'm using the PID of System here
            //if (log.IsDebugEnabled) log.Debug("GetaProcess for Explorer"); 
            Process Pname = GetaProcess("explorer");
            //This can be null if no user has not logged into the system.
            if (Pname == null) return false;

            int pid = Pname.Id;
            Process proc = Process.GetProcessById(pid);
            if (OpenProcessToken(proc.Handle, TOKEN_QUERY | TOKEN_IMPERSONATE | TOKEN_DUPLICATE, ref hToken)) // != 0)
            {
                WindowsIdentity newId = new WindowsIdentity(hToken);
                //log.Debug(newId.Owner);
                try
                {
                    const int SecurityImpersonation = 2;
                    dupeTokenHandle = DupeToken(hToken,
                    SecurityImpersonation);
                    if (IntPtr.Zero == dupeTokenHandle)
                    {
                        string s = String.Format("Dup failed {0}, privilege not held",
                        Marshal.GetLastWin32Error());
                        throw new Exception(s);
                    }

                    impersonatedUser = newId.Impersonate();
                    return true;
                }
                finally
                {
                    CloseHandle(hToken);
                }
            }
            else
            {
                string s = String.Format("OpenProcess Failed {0}, privilege not held", Marshal.GetLastWin32Error());
                throw new Exception(s);
            }
        }

        /// <summary>
        /// Duplicate the token for user impersonation.
        /// </summary>
        /// <param name="token">Token to duplicate for impersonation</param>
        /// <param name="Level">Impersonation security level, currently hardcored to 2</param>
        /// <returns>Returns duplicated token</returns>
        public static IntPtr DupeToken(IntPtr token, int Level)
        {
            IntPtr dupeTokenHandle = IntPtr.Zero;
            bool retVal = DuplicateToken(token, Level, ref dupeTokenHandle);
            return dupeTokenHandle;
        }

        /// <summary>
        /// Get the process running locally on the machine.
        /// If the specified process does not exists, it 
        /// returns back the current process.
        /// </summary>
        /// <param name="processname">Process name to get</param>
        /// <returns>Returns back the process</returns>
        public static Process GetaProcess(string processname)
        {
            Process[] aProc = Process.GetProcessesByName(processname);
            if (aProc.Length > 0) return aProc[0];
            else
            {
                //if (log.IsDebugEnabled) log.Debug("Explorer is not running");
                Process currentProcess = Process.GetCurrentProcess();
                return currentProcess;
            }
        }

        /// <summary>
        /// Roleback the impersonation if applied previously.
        /// </summary>
        public static void UndoImpersonate()
        {
            impersonatedUser.Undo();
            if (hToken != IntPtr.Zero) CloseHandle(hToken);
            if (dupeTokenHandle != IntPtr.Zero) CloseHandle(dupeTokenHandle);
            return;
        }
    }
}
