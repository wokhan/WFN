using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Security.Principal;
using System.ComponentModel;

namespace Wokhan.WindowsFirewallNotifier.Common.Helpers
{
    public class Impersonation
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
        private static extern bool CreateEnvironmentBlock(
            out IntPtr lpEnvironment,
            IntPtr hToken,
            [MarshalAs(UnmanagedType.Bool)] bool bInherit);

        [DllImport("userenv.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool DestroyEnvironmentBlock(
            IntPtr lpEnvironment);

        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CreateProcessAsUser(
            IntPtr hToken,
            string lpApplicationName,
            string lpCommandLine,
            ref SECURITY_ATTRIBUTES lpProcessAttributes,
            ref SECURITY_ATTRIBUTES lpThreadAttributes,
            [MarshalAs(UnmanagedType.Bool)] bool bInheritHandles,
            uint dwCreationFlags,
            IntPtr lpEnvironment,
            string lpCurrentDirectory,
            ref STARTUPINFO lpStartupInfo,
            out PROCESS_INFORMATION lpProcessInformation);

        [StructLayout(LayoutKind.Sequential)] public struct STARTUPINFO
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
        private static extern bool DuplicateTokenEx(
            IntPtr hExistingToken,
            uint dwDesiredAccess,
            ref SECURITY_ATTRIBUTES lpThreadAttributes,
            Int32 ImpersonationLevel,
            Int32 dwTokenType,
            ref IntPtr phNewToken);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CloseHandle(IntPtr hObject);

        //[DllImport("kernel32.dll", EntryPoint = "RtlZeroMemory", SetLastError = false)]
        //public static extern void ZeroMemory(IntPtr dest, uint size);

        private const uint TOKEN_QUERY = 0x0008;
        private const uint TOKEN_DUPLICATE = 0x0002;
        private const uint TOKEN_ASSIGN_PRIMARY = 0x0001;

        private const uint CREATE_UNICODE_ENVIRONMENT = 0x00000400;

        public static void LaunchProcessAsUser(string app, string args, IntPtr token)
        {
            IntPtr primaryToken = IntPtr.Zero;

            SECURITY_ATTRIBUTES sa = new SECURITY_ATTRIBUTES();
            //ZeroMemory(sa, (uint)Marshal.SizeOf(sa));
            sa.nLength = (uint)Marshal.SizeOf(sa);
            sa.bInheritHandle = false;
            sa.lpSecurityDescriptor = IntPtr.Zero;

            PROCESS_INFORMATION pi = new PROCESS_INFORMATION();
            STARTUPINFO si = new STARTUPINFO();
            //ZeroMemory(si, (uint)Marshal.SizeOf(si));
            si.cb = (uint)Marshal.SizeOf(si);

            bool retdup = DuplicateTokenEx(token, TOKEN_ASSIGN_PRIMARY | TOKEN_DUPLICATE | TOKEN_QUERY, ref sa,
                                      (int)SECURITY_IMPERSONATION_LEVEL.SecurityIdentification, (int)TOKEN_TYPE.TokenPrimary,
                                      ref primaryToken);
            if (!retdup)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Unable to duplicate the current user's token.");
            }

            try
            {
                IntPtr UserEnvironment;
                bool retenviron = CreateEnvironmentBlock(out UserEnvironment, token, false);
                if (!retenviron)
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error(), "Unable to create user environment.");
                }

                try
                {
                    string cmd = "\"" + app + "\" " + args;
                    bool retimper = CreateProcessAsUser(primaryToken, null, cmd, ref sa, ref sa, false, CREATE_UNICODE_ENVIRONMENT, UserEnvironment, null, ref si, out pi);
                    if (!retimper)
                    {
                        throw new Win32Exception(Marshal.GetLastWin32Error(), "Unable to impersonate. Command was: " + cmd);
                    }
                    CloseHandle(pi.hThread);
                    CloseHandle(pi.hProcess);
                }
                finally
                {
                    DestroyEnvironmentBlock(UserEnvironment);
                }
            }
            finally
            {
                CloseHandle(primaryToken);
            }
        }
    }
}

