using System;
using System.Runtime.InteropServices;

namespace Wokhan.WindowsFirewallNotifier.Common.Helpers
{
    public static partial class Impersonation
    {
        protected static class NativeMethods
        {
            [StructLayout(LayoutKind.Sequential)]
            internal struct PROCESS_INFORMATION
            {
                public IntPtr hProcess;
                public IntPtr hThread;
                public uint dwProcessId;
                public uint dwThreadId;
            }

            [StructLayout(LayoutKind.Sequential)]
            internal struct SECURITY_ATTRIBUTES
            {
                public uint nLength;
                public IntPtr lpSecurityDescriptor;
                public bool bInheritHandle;
            }

            internal enum SECURITY_IMPERSONATION_LEVEL
            {
                SecurityAnonymous,
                SecurityIdentification,
                SecurityImpersonation,
                SecurityDelegation
            }

            internal enum TOKEN_TYPE
            {
                TokenPrimary = 1,
                TokenImpersonation
            }

            [DllImport("userenv.dll", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool CreateEnvironmentBlock(
                out IntPtr lpEnvironment,
                IntPtr hToken,
                [MarshalAs(UnmanagedType.Bool)] bool bInherit);

            [DllImport("userenv.dll", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool DestroyEnvironmentBlock(
                IntPtr lpEnvironment);

            [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool CreateProcessAsUser(
                IntPtr hToken,
                string? lpApplicationName,
                string lpCommandLine,
                ref SECURITY_ATTRIBUTES lpProcessAttributes,
                ref SECURITY_ATTRIBUTES lpThreadAttributes,
                [MarshalAs(UnmanagedType.Bool)] bool bInheritHandles,
                uint dwCreationFlags,
                IntPtr lpEnvironment,
                string? lpCurrentDirectory,
                ref STARTUPINFO lpStartupInfo,
                out PROCESS_INFORMATION lpProcessInformation);

            [StructLayout(LayoutKind.Sequential)]
            internal struct STARTUPINFO
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

            [DllImport("advapi32.dll", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool DuplicateTokenEx(
                IntPtr hExistingToken,
                uint dwDesiredAccess,
                ref SECURITY_ATTRIBUTES lpThreadAttributes,
                Int32 ImpersonationLevel,
                Int32 dwTokenType,
                ref IntPtr phNewToken);

            [DllImport("kernel32.dll", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool CloseHandle(IntPtr hObject);

            //[DllImport("kernel32.dll", EntryPoint = "RtlZeroMemory", SetLastError = false)]
            //public static extern void ZeroMemory(IntPtr dest, uint size);

            internal const uint TOKEN_QUERY = 0x0008;
            internal const uint TOKEN_DUPLICATE = 0x0002;
            internal const uint TOKEN_ASSIGN_PRIMARY = 0x0001;

            internal const uint CREATE_UNICODE_ENVIRONMENT = 0x00000400;
        }

    }
}

