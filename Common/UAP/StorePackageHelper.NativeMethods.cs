using System;
using System.Runtime.InteropServices;

namespace Wokhan.WindowsFirewallNotifier.Common.UAP;

public static partial class StorePackageHelper
{
    protected static partial class NativeMethods
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

        //Note: Only exists on Windows 8 and higher
        /*[DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        internal static extern uint GetPackageFullName(IntPtr hProcess, ref uint packageFullNameLength, StringBuilder packageFullName);*/

        //Note: Only exists on Windows 8 and higher
        [LibraryImport("kernel32.dll")]
        internal static unsafe partial uint GetPackageFamilyName(IntPtr hProcess, ref uint packageFamilyNameLength, char* packageFamilyName);

        //Note: Only exists on Windows 8 and higher
        [LibraryImport("userenv.dll", StringMarshalling = StringMarshalling.Utf16)]
        internal static partial uint DeriveAppContainerSidFromAppContainerName(string pszAppContainerName, out IntPtr ppsidAppContainerSid);

        internal const uint ERROR_SUCCESS = 0;
        internal const uint APPMODEL_ERROR_NO_PACKAGE = 15700;
        internal const uint S_OK = 0x00000000;

        [LibraryImport("kernel32.dll", SetLastError = true)]
        internal static partial IntPtr OpenProcess(ProcessAccessFlags dwDesiredAccess, [MarshalAs(UnmanagedType.Bool)] bool bInheritHandle, uint dwProcessId);

        internal const int TOKEN_QUERY = 0X00000008;

        [LibraryImport("advapi32", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool ConvertSidToStringSidW(IntPtr pSID, [MarshalAs(UnmanagedType.LPTStr)] out string pStringSid);

        [LibraryImport("advapi32")]
        internal static partial IntPtr FreeSid(IntPtr pSid);

        [LibraryImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool CloseHandle(IntPtr hObject);
    }

}