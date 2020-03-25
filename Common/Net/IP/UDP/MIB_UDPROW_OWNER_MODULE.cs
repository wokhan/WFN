using System;
using System.Net;
using System.Runtime.InteropServices;

using Wokhan.WindowsFirewallNotifier.Common.Net.IP;
using Wokhan.WindowsFirewallNotifier.Common.Net.IP.UDP;

namespace Wokhan.WindowsFirewallNotifier.Common.Helpers.IPHelpers
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
        public string LocalAddress => IPHelper.GetAddressAsString(_localAddr);
        public int LocalPort => IPHelper.GetRealPort(_localPort);
        public Owner? OwnerModule => UDPHelper.GetOwningModuleUDP(this);
        public string Protocol => "UDP";
        public DateTime? CreationTime => _creationTime == 0 ? (DateTime?)null : DateTime.FromFileTime(_creationTime);
        public string RemoteAddress => string.Empty;
        public int RemotePort => -1;
        public bool IsLoopback => IPAddress.IsLoopback(IPAddress.Parse(RemoteAddress));
    }
}