using System;
using System.Net;
using System.Runtime.InteropServices;

namespace Wokhan.WindowsFirewallNotifier.Common.Net.IP
{
    [StructLayout(LayoutKind.Sequential)]
    public struct MIB_TCPROW_OWNER_MODULE : IConnectionOwnerInfo
    {
        internal ConnectionStatus _state;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        internal byte[] _localAddr;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        internal byte[] _localPort;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        internal byte[] _remoteAddr;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        internal byte[] _remotePort;

        internal uint _owningPid;
        internal long _creationTime;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        internal ulong[] _owningModuleInfo;

        public byte[] RemoteAddrBytes => _remoteAddr;
        public ConnectionStatus State => _state;
        public string RemoteAddress => IPHelper.GetAddressAsString(_remoteAddr);
        public string LocalAddress => IPHelper.GetAddressAsString(_localAddr);
        public int RemotePort => IPHelper.GetRealPort(_remotePort);
        public int LocalPort => IPHelper.GetRealPort(_localPort);
        public Owner? OwnerModule => TCPHelper.GetOwningModuleTCP(this);
        public string Protocol => "TCP";
        public uint OwningPid => _owningPid;
        public DateTime? CreationTime => _creationTime == 0 ? (DateTime?)null : DateTime.FromFileTime(_creationTime);
        public bool IsLoopback => IPAddress.IsLoopback(IPAddress.Parse(RemoteAddress));

        public TCPHelper.MIB_TCPROW ToTCPRow()
        {
            return new TCPHelper.MIB_TCPROW() { dwLocalAddr = _localAddr, dwRemoteAddr = _remoteAddr, dwLocalPort = _localPort, dwRemotePort = _remotePort, State = _state };
        }
    }
}
