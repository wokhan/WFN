using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Wokhan.WindowsFirewallNotifier.Common.Helpers
{
    public static partial class ProcessHelper
    {
        protected static class NativeMethods
        {
            [Flags]
            internal enum ProcessAccessFlags : uint
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

            internal enum TOKEN_INFORMATION_CLASS
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
            internal struct TOKEN_USER
            {
                public SID_AND_ATTRIBUTES User;
            }

            [StructLayout(LayoutKind.Sequential)]
            internal struct SID_AND_ATTRIBUTES
            {
                public IntPtr Sid;
                public int Attributes;
            }

            [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
            internal static extern IntPtr OpenSCManager(string? machineName, string? databaseName, uint dwAccess);

            [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
            internal static extern uint EnumServicesStatusEx(IntPtr hSCManager,
                   int infoLevel, uint dwServiceType,
                   uint dwServiceState, IntPtr lpServices, uint cbBufSize,
                   out uint pcbBytesNeeded, out uint lpServicesReturned,
                   ref uint lpResumeHandle, string? pszGroupName);

            [DllImport("advapi32.dll", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool CloseServiceHandle(IntPtr hSCObject);

            [Flags]
            internal enum ACCESS_MASK : uint { DELETE = 0x00010000, READ_CONTROL = 0x00020000, WRITE_DAC = 0x00040000, WRITE_OWNER = 0x00080000, SYNCHRONIZE = 0x00100000, STANDARD_RIGHTS_REQUIRED = 0x000f0000, STANDARD_RIGHTS_READ = 0x00020000, STANDARD_RIGHTS_WRITE = 0x00020000, STANDARD_RIGHTS_EXECUTE = 0x00020000, STANDARD_RIGHTS_ALL = 0x001f0000, SPECIFIC_RIGHTS_ALL = 0x0000ffff, ACCESS_SYSTEM_SECURITY = 0x01000000, MAXIMUM_ALLOWED = 0x02000000, GENERIC_READ = 0x80000000, GENERIC_WRITE = 0x40000000, GENERIC_EXECUTE = 0x20000000, GENERIC_ALL = 0x10000000, DESKTOP_READOBJECTS = 0x00000001, DESKTOP_CREATEWINDOW = 0x00000002, DESKTOP_CREATEMENU = 0x00000004, DESKTOP_HOOKCONTROL = 0x00000008, DESKTOP_JOURNALRECORD = 0x00000010, DESKTOP_JOURNALPLAYBACK = 0x00000020, DESKTOP_ENUMERATE = 0x00000040, DESKTOP_WRITEOBJECTS = 0x00000080, DESKTOP_SWITCHDESKTOP = 0x00000100, WINSTA_ENUMDESKTOPS = 0x00000001, WINSTA_READATTRIBUTES = 0x00000002, WINSTA_ACCESSCLIPBOARD = 0x00000004, WINSTA_CREATEDESKTOP = 0x00000008, WINSTA_WRITEATTRIBUTES = 0x00000010, WINSTA_ACCESSGLOBALATOMS = 0x00000020, WINSTA_EXITWINDOWS = 0x00000040, WINSTA_ENUMERATE = 0x00000100, WINSTA_READSCREEN = 0x00000200, WINSTA_ALL_ACCESS = 0x0000037f }

            [Flags]
            internal enum SERVICE_STATE : int { SERVICE_ACTIVE = 0x00000001, SERVICE_INACTIVE = 0x00000002, SERVICE_STATE_ALL = SERVICE_ACTIVE | SERVICE_INACTIVE }

            [Flags]
            internal enum SERVICE_TYPES : int { SERVICE_KERNEL_DRIVER = 0x00000001, SERVICE_FILE_SYSTEM_DRIVER = 0x00000002, SERVICE_ADAPTER = 0x00000004, SERVICE_RECOGNIZER_DRIVER = 0x00000008, SERVICE_DRIVER = SERVICE_KERNEL_DRIVER | SERVICE_FILE_SYSTEM_DRIVER | SERVICE_RECOGNIZER_DRIVER, SERVICE_WIN32_OWN_PROCESS = 0x00000010, SERVICE_WIN32_SHARE_PROCESS = 0x00000020, SERVICE_WIN32 = SERVICE_WIN32_OWN_PROCESS | SERVICE_WIN32_SHARE_PROCESS, }

            [StructLayout(LayoutKind.Sequential, Pack = 1)]
            internal struct SERVICE_STATUS { public static readonly int SizeOf = Marshal.SizeOf(typeof(SERVICE_STATUS)); public SERVICE_TYPES dwServiceType; public SERVICE_STATE dwCurrentState; public uint dwControlsAccepted; public uint dwWin32ExitCode; public uint dwServiceSpecificExitCode; public uint dwCheckPoint; public uint dwWaitHint; }

            [StructLayout(LayoutKind.Sequential, Pack = 4)]
            internal struct ENUM_SERVICE_STATUS_PROCESS
            {
                public static readonly int SizeOf = Marshal.SizeOf(typeof(ENUM_SERVICE_STATUS_PROCESS));

                [MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPTStr)]
                public string lpServiceName;

                [MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPTStr)]
                public string lpDisplayName;

                public SERVICE_STATUS_PROCESS ServiceStatus;
            }

            [StructLayout(LayoutKind.Sequential, Pack = 1)]
            internal struct SERVICE_STATUS_PROCESS
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
            internal enum SCM_ACCESS : uint
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
            internal enum SC_ENUM_TYPE : uint
            {
                SC_ENUM_PROCESS_INFO = 0
            }

            internal const uint ERROR_MORE_DATA = 234;

            //Note: Only exists on Windows 8 and higher
            /*[DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
            internal static extern uint GetPackageFullName(IntPtr hProcess, ref uint packageFullNameLength, StringBuilder packageFullName);*/

            //Note: Only exists on Windows 8 and higher
            [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
            internal static extern uint GetPackageFamilyName(IntPtr hProcess, ref uint packageFamilyNameLength, StringBuilder packageFamilyName);

            //Note: Only exists on Windows 8 and higher
            [DllImport("userenv.dll", CharSet = CharSet.Unicode)]
            internal static extern uint DeriveAppContainerSidFromAppContainerName(string pszAppContainerName, out IntPtr ppsidAppContainerSid);

            internal const uint ERROR_SUCCESS = 0;
            internal const uint APPMODEL_ERROR_NO_PACKAGE = 15700;
            internal const uint S_OK = 0x00000000;

            [DllImport("kernel32.dll", SetLastError = true)]
            internal static extern IntPtr OpenProcess(ProcessAccessFlags dwDesiredAccess, [MarshalAs(UnmanagedType.Bool)] bool bInheritHandle, uint dwProcessId);

            internal const int TOKEN_QUERY = 0X00000008;

            [DllImport("kernel32.dll", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool OpenProcessToken(IntPtr ProcessHandle, uint DesiredAccess, out IntPtr TokenHandle);

            [DllImport("advapi32", SetLastError = true, CharSet = CharSet.Auto)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool GetTokenInformation(IntPtr hToken, TOKEN_INFORMATION_CLASS TokenInformationClass, IntPtr TokenInformation, uint dwTokenInfoLength, ref uint dwReturnLength);

            [DllImport("advapi32", SetLastError = true, CharSet = CharSet.Auto)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool ConvertSidToStringSid(IntPtr pSID, [MarshalAs(UnmanagedType.LPTStr)] out string pStringSid);

            [DllImport("advapi32", CharSet = CharSet.Auto)]
            internal static extern IntPtr FreeSid(IntPtr pSid);

            [DllImport("kernel32.dll", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool CloseHandle(IntPtr hObject);

            [DllImport("user32.dll")]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool ShowWindow(IntPtr hWnd, ShowWindowEnum flags);

            [DllImport("user32.dll")]
            internal static extern int SetForegroundWindow(IntPtr hwnd);

            internal enum ShowWindowEnum
            {
                Hide = 0,
                ShowNormal = 1, ShowMinimized = 2, ShowMaximized = 3,
                Maximize = 3, ShowNormalNoActivate = 4, Show = 5,
                Minimize = 6, ShowMinNoActivate = 7, ShowNoActivate = 8,
                Restore = 9, ShowDefault = 10, ForceMinimized = 11
            };
        }

    }
}