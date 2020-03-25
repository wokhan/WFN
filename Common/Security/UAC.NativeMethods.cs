/* This code has based on http://stackoverflow.com/questions/1220213/detect-if-running-as-administrator-with-or-without-elevated-privileges
   Assumed author: Scott Chamberlain

   With additions from https://code.msdn.microsoft.com/windowsdesktop/CSUACSelfElevation-644673d3/
*/
using System;
using System.Runtime.InteropServices;

namespace Wokhan.WindowsFirewallNotifier.Common.Helpers
{
    public static partial class UAC
    {
        protected static class NativeMethods
        {
            internal const string UAC_REGISTRY_KEY = "Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\System";
            internal const string UAC_REGISTRY_VALUE = "EnableLUA";
            
            internal const uint STANDARD_RIGHTS_READ = 0x00020000;
            internal const uint TOKEN_QUERY = 0x0008;
            //internal const uint TOKEN_READ = (STANDARD_RIGHTS_READ | TOKEN_QUERY);

            [DllImport("advapi32.dll", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool OpenProcessToken(IntPtr ProcessHandle, uint DesiredAccess, out IntPtr TokenHandle);

            [DllImport("kernel32.dll", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool CloseHandle(IntPtr hObject);

            [DllImport("advapi32.dll", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool GetTokenInformation(IntPtr TokenHandle, TOKEN_INFORMATION_CLASS TokenInformationClass, IntPtr TokenInformation, uint TokenInformationLength, out uint ReturnLength);

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
                MaxTokenInfoClass
            }

            internal enum TOKEN_ELEVATION_TYPE
            {
                TokenElevationTypeDefault = 1,
                TokenElevationTypeFull,
                TokenElevationTypeLimited
            }
        }
    }
}