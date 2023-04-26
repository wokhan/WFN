/* This code has based on http://stackoverflow.com/questions/1220213/detect-if-running-as-administrator-with-or-without-elevated-privileges
   Assumed author: Scott Chamberlain

   With additions from https://code.msdn.microsoft.com/windowsdesktop/CSUACSelfElevation-644673d3/
*/
using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Principal;

using Windows.Win32;
using Windows.Win32.Security;

namespace Wokhan.WindowsFirewallNotifier.Common.Helpers;

public static partial class UAC
{
    internal const string UAC_REGISTRY_KEY = "Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\System";
    internal const string UAC_REGISTRY_VALUE = "EnableLUA";

    internal const uint STANDARD_RIGHTS_READ = 0x00020000;
    internal const uint TOKEN_QUERY = 0x0008;

    public static bool CheckUAC()
    {
        using RegistryKey uacKey = Registry.LocalMachine.OpenSubKey(UAC_REGISTRY_KEY, false);
        
        return uacKey.GetValue(UAC_REGISTRY_VALUE).Equals(1);
    }

    public unsafe static bool CheckProcessElevated()
    {
        if (CheckUAC())
        {
            if (!NativeMethods.OpenProcessToken(Process.GetCurrentProcess().SafeHandle, TOKEN_ACCESS_MASK.TOKEN_QUERY, out SafeFileHandle tokenHandle))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Could not get process token.");
            }

            try
            {
                uint returnedSize = sizeof(TOKEN_ELEVATION_TYPE);
                var elevationType = new TOKEN_ELEVATION_TYPE();
                try
                {
                    if (NativeMethods.GetTokenInformation(tokenHandle, TOKEN_INFORMATION_CLASS.TokenElevationType, &elevationType, returnedSize, out returnedSize))
                    {
                        switch (elevationType)
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
                    //if (elevationTypePtr != IntPtr.Zero)
                    //{
                    //    Marshal.FreeHGlobal(elevationTypePtr);
                    //}
                }
            }
            finally
            {
                tokenHandle?.Close();
            }
        }
        else
        {
            WindowsPrincipal principal = new WindowsPrincipal(WindowsIdentity.GetCurrent());
            return principal.IsInRole(WindowsBuiltInRole.Administrator) || principal.IsInRole(0x200); //Domain Administrator
        }
    }
}
