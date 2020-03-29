using System;
using System.Net;
using System.Runtime.InteropServices;

namespace Wokhan.WindowsFirewallNotifier.Common.Net.IP.UDP6
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct MIB_UDP6ROW_OWNER_MODULE : IConnectionOwnerInfo
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        internal byte[] _localAddress;
        internal uint LocalScopeId;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        internal byte[] _localPort;
        internal uint _owningPid;
        //[FieldOffset(16)]
        internal long _creationTime;
        //[FieldOffset(24)]
        //public int SpecificPortBind;
        //[FieldOffset(24)]
        internal int Flags;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)] //FieldOffset(32), 
        internal ulong[] OwningModuleInfo;

        public byte[] RemoteAddrBytes => Array.Empty<byte>();
        public ConnectionStatus State => ConnectionStatus.NOT_APPLICABLE;
        public uint OwningPid => _owningPid;
        public string LocalAddress => IPHelper.GetRealAddress(_localAddress);
        public int LocalPort => IPHelper.GetRealPort(_localPort);
        public Owner? OwnerModule => UDP6Helper.GetOwningModuleUDP6(this);
        public string Protocol => "UDP";
        public string RemoteAddress => string.Empty;
        public int RemotePort => -1;
        public DateTime? CreationTime => _creationTime == 0 ? (DateTime?)null : DateTime.FromFileTime(_creationTime);
        public bool IsLoopback => IPAddress.IsLoopback(IPAddress.Parse(RemoteAddress));
    }
}
