using System;
using System.Net;
using System.Runtime.InteropServices;

namespace Wokhan.WindowsFirewallNotifier.Common.Net.IP
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct MIB_TCP6ROW_OWNER_MODULE : IConnectionOwnerInfo
    {
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
        internal ConnectionStatus _state;
        internal uint _owningPid;
        internal long _creationTime;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public ulong[] OwningModuleInfo;


        public byte[] RemoteAddrBytes => _remoteAddress;
        public uint OwningPid => _owningPid;
        public string LocalAddress => IPHelper.GetRealAddress(_localAddress);
        public int LocalPort => IPHelper.GetRealPort(_localPort);
        public string RemoteAddress => IPHelper.GetRealAddress(_remoteAddress);
        public int RemotePort => IPHelper.GetRealPort(_remotePort);
        public Owner? OwnerModule => TCP6Helper.GetOwningModuleTCP6(this);
        public string Protocol => "TCP";
        public ConnectionStatus State => _state;
        public DateTime? CreationTime => _creationTime == 0 ? (DateTime?)null : DateTime.FromFileTime(_creationTime);
        public bool IsLoopback => IPAddress.IsLoopback(IPAddress.Parse(RemoteAddress));

        public TCP6Helper.MIB_TCP6ROW ToTCPRow()
        {
            return new TCP6Helper.MIB_TCP6ROW() { _localAddress = _localAddress, _remoteAddress = _remoteAddress, _localPort = _localPort, _remotePort = _remotePort, State = _state };
        }
    }

}