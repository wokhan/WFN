using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Wokhan.WindowsFirewallNotifier.Common.Core.Resources
{
    public static partial class ResourcesLoader
    {
        protected static class NativeMethods
        {
            /*[DllImport("Wtsapi32.dll", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool WTSQueryUserToken(uint SessionId, ref IntPtr phToken);

            [DllImport("kernel32.dll", SetLastError = true)]
            public static extern uint WTSGetActiveConsoleSessionId();
            */
            [DllImport("shlwapi.dll", BestFitMapping = false, CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = false, ThrowOnUnmappableChar = true)]
            internal static extern int SHLoadIndirectString(string pszSource, StringBuilder pszOutBuf, uint cchOutBuf, IntPtr ppvReserved);
        }
    }
}
