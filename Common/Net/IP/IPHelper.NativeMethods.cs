using System;
using System.Runtime.InteropServices;

namespace Wokhan.WindowsFirewallNotifier.Common.Net.IP;

public partial class IPHelper
{
    protected static partial class NativeMethods
    {
        [LibraryImport("kernel32.dll", SetLastError = false)]
        internal static partial void RtlZeroMemory(IntPtr dest, uint size);
    }
}
