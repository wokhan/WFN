using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Wokhan.WindowsFirewallNotifier.Common.Core.Resources;

public static partial class ResourcesLoader
{
    protected static partial class NativeMethods
    {
        /*[DllImport("Wtsapi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool WTSQueryUserToken(uint SessionId, ref IntPtr phToken);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern uint WTSGetActiveConsoleSessionId();
        */
        [LibraryImport("shlwapi.dll", SetLastError = false, StringMarshalling = StringMarshalling.Utf16)] //, ThrowOnUnmappableChar = true)]
        internal static unsafe partial int SHLoadIndirectString(string pszSource, char* pszOutBuf, uint cchOutBuf, IntPtr ppvReserved);
    }
}
