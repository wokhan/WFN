using System.Runtime.InteropServices;

using Windows.Win32.NetworkManagement.IpHelper;

namespace Wokhan.WindowsFirewallNotifier.Common.Net.IP.NativeOverrides;

internal static class MarshalHelper
{
    internal static uint rwS = (uint)Marshal.SizeOf<TCP_ESTATS_BANDWIDTH_RW_v0>();
    internal static uint rodS = (uint)Marshal.SizeOf<TCP_ESTATS_BANDWIDTH_ROD_v0>();
}
