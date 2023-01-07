using System;
using System.Net;
using System.Runtime.InteropServices;

using Wokhan.WindowsFirewallNotifier.Common.Net.IP.TCP;

namespace Wokhan.WindowsFirewallNotifier.Common.Net.IP.UDP;

public partial class UDPHelper
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct MIB_UDPROW_OWNER_MODULE : IConnectionOwnerInfo
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]//, FieldOffset(0)]
        internal byte[] _localAddr;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]//, FieldOffset(4)]
        internal byte[] _localPort;
        //[FieldOffset(8)]
        internal uint _owningPid;
        //[FieldOffset(16)]
        internal long _creationTime;
        //[FieldOffset(24)]
        //public int SpecificPortBind;
        //[FieldOffset(24)]
        internal int _flags;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)] //FieldOffset(32), 
        internal ulong[] _owningModuleInfo;

        public byte[] RemoteAddrBytes => Array.Empty<byte>();
        public ConnectionStatus State => ConnectionStatus.NOT_APPLICABLE;
        public uint OwningPid => _owningPid;
        public string LocalAddress => GetAddressAsString(_localAddr);
        public int LocalPort => GetRealPort(_localPort);
        public Owner? OwnerModule => GetOwningModuleUDP(this);
        public string Protocol => "UDP";
        public DateTime? CreationTime => _creationTime == 0 ? null : DateTime.FromFileTime(_creationTime);
        public string RemoteAddress => string.Empty;
        public int RemotePort => -1;
        public bool IsLoopback => IPAddress.IsLoopback(IPAddress.Parse(RemoteAddress));

        public ITcpRow ToTcpRow()
        {
            throw new NotImplementedException("UDP connections owner details cannot be mapped to TCP connections rows.");
        }
    }
}