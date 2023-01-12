using System.Runtime.InteropServices;
using System.Text;

namespace Wokhan.WindowsFirewallNotifier.Common.IO.Files;

public static partial class PathResolver
{
    protected static partial class NativeMethods
    {
        [LibraryImport("kernel32.dll", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
        internal static unsafe partial uint QueryDosDeviceW(string lpDeviceName, char* lpTargetPath, uint ucchMax);
    }
}
