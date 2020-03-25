using System.Runtime.InteropServices;

namespace Wokhan.WindowsFirewallNotifier.Common.Net.IP
{
    public partial class TCP6Helper
    {
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct MIB_TCP6ROW
        {
            internal ConnectionStatus State;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
            internal byte[] _localAddress;
            internal uint LocalScopeId;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            internal byte[] _localPort;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
            internal byte[] _remoteAddress;
            internal uint RemoteScopeId;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            internal byte[] _remotePort;
        }
    }
}