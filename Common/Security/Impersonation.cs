using System;
using System.Runtime.InteropServices;
using System.ComponentModel;

namespace Wokhan.WindowsFirewallNotifier.Common.Helpers
{
    public static partial class Impersonation
    {
        public static void LaunchProcessAsUser(string app, string args, IntPtr token)
        {
            IntPtr primaryToken = IntPtr.Zero;

            var securityAttrs = new NativeMethods.SECURITY_ATTRIBUTES();
            //ZeroMemory(sa, (uint)Marshal.SizeOf(sa));
            securityAttrs.nLength = (uint)Marshal.SizeOf(securityAttrs);
            securityAttrs.bInheritHandle = false;
            securityAttrs.lpSecurityDescriptor = IntPtr.Zero;

            var startupInfo = new NativeMethods.STARTUPINFO();
            //ZeroMemory(si, (uint)Marshal.SizeOf(si));
            startupInfo.cb = (uint)Marshal.SizeOf(startupInfo);

            bool retdup = NativeMethods.DuplicateTokenEx(token, NativeMethods.TOKEN_ASSIGN_PRIMARY | NativeMethods.TOKEN_DUPLICATE | NativeMethods.TOKEN_QUERY, ref securityAttrs,
                                      (int)NativeMethods.SECURITY_IMPERSONATION_LEVEL.SecurityIdentification, (int)NativeMethods.TOKEN_TYPE.TokenPrimary,
                                      ref primaryToken);
            if (!retdup)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Unable to duplicate the current user's token.");
            }

            try
            {
                IntPtr UserEnvironment;
                bool retenviron = NativeMethods.CreateEnvironmentBlock(out UserEnvironment, token, false);
                if (!retenviron)
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error(), "Unable to create user environment.");
                }

                try
                {
                    string cmd = $"\"{app}\" {args}";
                    bool retimper = NativeMethods.CreateProcessAsUser(primaryToken, null, cmd, ref securityAttrs, ref securityAttrs, false, NativeMethods.CREATE_UNICODE_ENVIRONMENT, UserEnvironment, null, ref startupInfo, out var processInfo);
                    if (!retimper)
                    {
                        throw new Win32Exception(Marshal.GetLastWin32Error(), "Unable to impersonate. Command was: " + cmd);
                    }
                    NativeMethods.CloseHandle(processInfo.hThread);
                    NativeMethods.CloseHandle(processInfo.hProcess);
                }
                finally
                {
                    NativeMethods.DestroyEnvironmentBlock(UserEnvironment);
                }
            }
            finally
            {
                NativeMethods.CloseHandle(primaryToken);
            }
        }
    }
}

