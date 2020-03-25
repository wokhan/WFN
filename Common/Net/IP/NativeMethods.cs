using System;
using System.Runtime.InteropServices;

namespace Wokhan.WindowsFirewallNotifier.Common.Net.IP
{
    public partial class IPHelper
    {
        protected static class NativeMethods
        {
            [DllImport("kernel32.dll", EntryPoint = "RtlZeroMemory", SetLastError = false)]
            internal static extern void ZeroMemory(IntPtr dest, uint size);
        }
    }
}
