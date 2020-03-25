/* This code has based on http://stackoverflow.com/questions/1220213/detect-if-running-as-administrator-with-or-without-elevated-privileges
   Assumed author: Scott Chamberlain

   With additions from https://code.msdn.microsoft.com/windowsdesktop/CSUACSelfElevation-644673d3/
*/
using Microsoft.Win32;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Principal;

namespace Wokhan.WindowsFirewallNotifier.Common.Helpers
{
    public static partial class UAC
    {
        public static bool CheckUAC()
        {
            using RegistryKey uacKey = Registry.LocalMachine.OpenSubKey(NativeMethods.UAC_REGISTRY_KEY, false);
            
            return uacKey.GetValue(NativeMethods.UAC_REGISTRY_VALUE).Equals(1);
        }

        public static bool CheckProcessElevated()
        {
            if (CheckUAC())
            {
                IntPtr tokenHandle = IntPtr.Zero;
                if (!NativeMethods.OpenProcessToken(Process.GetCurrentProcess().Handle, NativeMethods.TOKEN_QUERY, out tokenHandle))
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error(), "Could not get process token.");
                }

                try
                {
                    uint returnedSize = sizeof(NativeMethods.TOKEN_ELEVATION_TYPE);

                    IntPtr elevationTypePtr = Marshal.AllocHGlobal((int)returnedSize);
                    try
                    {
                        if (NativeMethods.GetTokenInformation(tokenHandle, NativeMethods.TOKEN_INFORMATION_CLASS.TokenElevationType, elevationTypePtr, returnedSize, out returnedSize))
                        {
                            NativeMethods.TOKEN_ELEVATION_TYPE elevationResult = (NativeMethods.TOKEN_ELEVATION_TYPE)Marshal.ReadInt32(elevationTypePtr);
                            switch (elevationResult)
                            {
                                case NativeMethods.TOKEN_ELEVATION_TYPE.TokenElevationTypeDefault:
                                    //Token is not split; if user is admin, we're admin.
                                    WindowsPrincipal principal = new WindowsPrincipal(WindowsIdentity.GetCurrent());
                                    return principal.IsInRole(WindowsBuiltInRole.Administrator) || principal.IsInRole(0x200); //Domain Administrator

                                case NativeMethods.TOKEN_ELEVATION_TYPE.TokenElevationTypeFull:
                                    //Token is split, but we're admin.
                                    return true;

                                case NativeMethods.TOKEN_ELEVATION_TYPE.TokenElevationTypeLimited:
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
                        NativeMethods.CloseHandle(tokenHandle);
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
