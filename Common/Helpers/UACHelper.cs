/* This code has based on http://stackoverflow.com/questions/1220213/detect-if-running-as-administrator-with-or-without-elevated-privileges
   Assumed author: Scott Chamberlain

   With additions from https://code.msdn.microsoft.com/windowsdesktop/CSUACSelfElevation-644673d3/
*/
using Microsoft.Win32;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Claims;
using System.Security.Principal;

namespace Wokhan.WindowsFirewallNotifier.Common.Helpers
{
    public static class UacHelper
    {
        private const string uacRegistryKey = "Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\System";
        private const string uacRegistryValue = "EnableLUA";

        private static uint STANDARD_RIGHTS_READ = 0x00020000;
        private static uint TOKEN_QUERY = 0x0008;
        private static uint TOKEN_READ = (STANDARD_RIGHTS_READ | TOKEN_QUERY);

        [DllImport("advapi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool OpenProcessToken(IntPtr ProcessHandle, uint DesiredAccess, out IntPtr TokenHandle);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CloseHandle(IntPtr hObject);

        [DllImport("advapi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetTokenInformation(IntPtr TokenHandle, TOKEN_INFORMATION_CLASS TokenInformationClass, IntPtr TokenInformation, uint TokenInformationLength, out uint ReturnLength);

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
            MaxTokenInfoClass
        }

        private enum TOKEN_ELEVATION_TYPE
        {
            TokenElevationTypeDefault = 1,
            TokenElevationTypeFull,
            TokenElevationTypeLimited
        }

        public static bool CheckUAC()
        {
            using (RegistryKey uacKey = Registry.LocalMachine.OpenSubKey(uacRegistryKey, false))
            {
                return uacKey.GetValue(uacRegistryValue).Equals(1);
            }
        }

        public static bool CheckProcessElevated()
        {
            if (CheckUAC())
            {
                IntPtr tokenHandle = IntPtr.Zero;
                if (!OpenProcessToken(Process.GetCurrentProcess().Handle, TOKEN_QUERY, out tokenHandle))
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error(), "Could not get process token.");
                }

                try
                {
                    uint returnedSize = sizeof(TOKEN_ELEVATION_TYPE);

                    IntPtr elevationTypePtr = Marshal.AllocHGlobal((int)returnedSize);
                    try
                    {
                        if (GetTokenInformation(tokenHandle, TOKEN_INFORMATION_CLASS.TokenElevationType, elevationTypePtr, returnedSize, out returnedSize))
                        {
                            TOKEN_ELEVATION_TYPE elevationResult = (TOKEN_ELEVATION_TYPE)Marshal.ReadInt32(elevationTypePtr);
                            switch (elevationResult)
                            {
                                case TOKEN_ELEVATION_TYPE.TokenElevationTypeDefault:
                                    //Token is not split; if user is admin, we're admin.
                                    WindowsPrincipal principal = new WindowsPrincipal(WindowsIdentity.GetCurrent());
                                    return principal.IsInRole(WindowsBuiltInRole.Administrator) || principal.IsInRole(0x200); //Domain Administrator
                                case TOKEN_ELEVATION_TYPE.TokenElevationTypeFull:
                                    //Token is split, but we're admin.
                                    return true;
                                case TOKEN_ELEVATION_TYPE.TokenElevationTypeLimited:
                                    //Token is split, and we're limited.
                                    return false;
                                default:
                                    throw new Exception("Unknown elevation type!");
                            }
                        }
                        else
                        {
                            throw new ApplicationException("Unable to determine the current elevation.");
                        }
                    }
                    finally
                    {
                        if (elevationTypePtr != IntPtr.Zero)
                        {
                            Marshal.FreeHGlobal(elevationTypePtr);
                        }
                    }
                }
                finally
                {
                    if (tokenHandle != IntPtr.Zero)
                    {
                        CloseHandle(tokenHandle);
                    }
                }
            }
            else
            {
                WindowsPrincipal principal = new WindowsPrincipal(WindowsIdentity.GetCurrent());
                return principal.IsInRole(WindowsBuiltInRole.Administrator) || principal.IsInRole(0x200); //Domain Administrator
            }
        }
    }
}
