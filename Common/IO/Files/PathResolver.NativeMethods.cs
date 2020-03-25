using System.Runtime.InteropServices;
using System.Text;

namespace Wokhan.WindowsFirewallNotifier.Common.IO.Files
{
    public static partial class PathResolver
    {
        protected static class NativeMethods
        {
            [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
            internal static extern uint QueryDosDevice(string lpDeviceName, StringBuilder lpTargetPath, uint ucchMax);
        }
    }
}
