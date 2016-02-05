using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;

namespace Wokhan.WindowsFirewallNotifier.Common.Helpers.IPHelpers
{
    public class TCP6Helper : TCPHelper
    {
        [DllImport("iphlpapi.dll", SetLastError = true)]
        public static extern uint GetOwnerModuleFromTcp6Entry(ref MIB_TCP6ROW_OWNER_MODULE pTcpEntry, TCPIP_OWNER_MODULE_INFO_CLASS Class, IntPtr Buffer, ref uint pdwSize);

        [DllImport("iphlpapi.dll", SetLastError = true)]
        public static extern uint GetPerTcp6ConnectionEStats(ref MIB_TCP6ROW Row, TCP_ESTATS_TYPE EstatsType, IntPtr Rw, uint RwVersion, uint RwSize, IntPtr Ros, uint RosVersion, uint RosSize, IntPtr Rod, uint RodVersion, uint RodSize);

        [DllImport("iphlpapi.dll", SetLastError = true)]
        public static extern uint SetPerTcp6ConnectionEStats(ref MIB_TCP6ROW Row, TCP_ESTATS_TYPE EstatsType, IntPtr Rw, uint RwVersion, uint RwSize, uint Offset);

        [DllImport("ntdll.dll", SetLastError = true)]
        public static extern void RtlIpv6AddressToString(byte[] Addr, out StringBuilder res);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct MIB_TCP6ROW
        {
            internal MIB_TCP_STATE State;
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

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct MIB_TCP6ROW_OWNER_MODULE : I_OWNER_MODULE
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
            internal MIB_TCP_STATE _state;
            internal uint _owningPid;
            internal long _creationTime;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
            public ulong[] OwningModuleInfo;


            public byte[] RemoteAddrBytes { get { return _remoteAddress; } }
            public uint OwningPid { get { return _owningPid; } }
            public string LocalAddress { get { return GetRealAddress(_localAddress); } }
            public int LocalPort { get { return GetRealPort(_localPort); } }
            public string RemoteAddress { get { return GetRealAddress(_remoteAddress); } }
            public int RemotePort { get { return GetRealPort(_remotePort); } }
            public Owner OwnerModule { get { return GetOwningModuleTCP6(this); } }
            public string Protocol { get { return "TCP"; } }
            public MIB_TCP_STATE State { get { return _state; } }
            public DateTime? CreationTime { get { return _creationTime == 0 ? (DateTime?)null : DateTime.FromFileTime(_creationTime); } }
            public bool IsLoopback { get { return IPAddress.IsLoopback(IPAddress.Parse(RemoteAddress)); } }

            public MIB_TCP6ROW ToTCPRow()
            {
                return new MIB_TCP6ROW() { _localAddress = _localAddress, _remoteAddress = _remoteAddress, _localPort = _localPort, _remotePort = _remotePort, State = _state };
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MIB_TCP6TABLE_OWNER_MODULE
        {
            public uint NumEntries;
            public MIB_TCP6ROW_OWNER_MODULE FirstEntry;
        }

        /// <summary>
        /// 
        /// </summary>
        public static IEnumerable<I_OWNER_MODULE> GetAllTCP6Connections()
        {
            IntPtr buffTable = IntPtr.Zero;

            try
            {
                uint buffSize = 0;
                GetExtendedTcpTable(IntPtr.Zero, ref buffSize, false, AF_INET.IP6, TCP_TABLE_CLASS.TCP_TABLE_OWNER_MODULE_ALL, 0);

                buffTable = Marshal.AllocHGlobal((int)buffSize);

                uint ret = GetExtendedTcpTable(buffTable, ref buffSize, false, AF_INET.IP6, TCP_TABLE_CLASS.TCP_TABLE_OWNER_MODULE_ALL, 0);
                if (ret == 0)
                {
                    MIB_TCP6TABLE_OWNER_MODULE tab = (MIB_TCP6TABLE_OWNER_MODULE)Marshal.PtrToStructure(buffTable, typeof(MIB_TCP6TABLE_OWNER_MODULE));
                    long rowPtr = ((long)buffTable + (long)Marshal.OffsetOf(typeof(MIB_TCP6TABLE_OWNER_MODULE), "FirstEntry"));

                    MIB_TCP6ROW_OWNER_MODULE current;
                    for (uint i = 0; i < tab.NumEntries; i++)
                    {
                        current = (MIB_TCP6ROW_OWNER_MODULE)Marshal.PtrToStructure((IntPtr)rowPtr, typeof(MIB_TCP6ROW_OWNER_MODULE));
                        rowPtr += Marshal.SizeOf(current);

                        yield return current;
                    }
                }
                else
                {
                    throw new Win32Exception((int)ret);
                }
            }
            finally
            {
                if (buffTable != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(buffTable);
                }
            }
        }

        private static Dictionary<MIB_TCP6ROW_OWNER_MODULE, Owner> ownerCache = new Dictionary<MIB_TCP6ROW_OWNER_MODULE, Owner>();
        internal static Owner GetOwningModuleTCP6(MIB_TCP6ROW_OWNER_MODULE row)
        {
            Owner ret = null;
            //if (ownerCache.TryGetValue(row, out ret))
            //{
            //    return ret;
            //}

            IntPtr buffer = IntPtr.Zero;
            try
            {
                uint buffSize = 0;
                GetOwnerModuleFromTcp6Entry(ref row, TCPIP_OWNER_MODULE_INFO_CLASS.TCPIP_OWNER_MODULE_INFO_BASIC, IntPtr.Zero, ref buffSize);
                buffer = Marshal.AllocHGlobal((int)buffSize);

                //GetOwnerModuleFromTcp6Entry needs the fields of TCPIP_OWNER_MODULE_INFO_BASIC to be NULL
                ZeroMemory(buffer, buffSize);

                var resp = GetOwnerModuleFromTcp6Entry(ref row, TCPIP_OWNER_MODULE_INFO_CLASS.TCPIP_OWNER_MODULE_INFO_BASIC, buffer, ref buffSize);
                if (resp == 0)
                {
                    ret = new Owner((TCPIP_OWNER_MODULE_BASIC_INFO)Marshal.PtrToStructure(buffer, typeof(TCPIP_OWNER_MODULE_BASIC_INFO)));
                }
                else if (resp != 1168) // Ignore closed connections
                {
                    LogHelper.Error("Unable to get the connection owner.", new Win32Exception((int)resp));
                }

                //ownerCache.Add(row, ret);

                return ret;
            }
            finally
            {
                if (buffer != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(buffer);
                }
            }
        }

        public static void EnsureStatsAreEnabled(MIB_TCP6ROW row)
        {

            var rwS = Marshal.SizeOf(typeof(TCP_ESTATS_BANDWIDTH_RW_v0));
            IntPtr rw = Marshal.AllocHGlobal(rwS);
            Marshal.StructureToPtr(new TCP_ESTATS_BANDWIDTH_RW_v0() { EnableCollectionInbound = TCP_BOOLEAN_OPTIONAL.TcpBoolOptEnabled, EnableCollectionOutbound = TCP_BOOLEAN_OPTIONAL.TcpBoolOptEnabled }, rw, true);

            try
            {
                var r = SetPerTcp6ConnectionEStats(ref row, TCP_ESTATS_TYPE.TcpConnectionEstatsBandwidth, rw, (uint)0, (uint)rwS, (uint)0);
                if (r != 0)
                {
                    throw new Win32Exception((int)r);
                }
            }
            finally
            {
                Marshal.FreeHGlobal(rw);
            }
        }


        public static TCP_ESTATS_BANDWIDTH_ROD_v0 GetTCPBandwidth(MIB_TCP6ROW row)
        {
            IntPtr rw = IntPtr.Zero;
            IntPtr rod = IntPtr.Zero;

            try
            {
                var rwS = Marshal.SizeOf(typeof(TCP_ESTATS_BANDWIDTH_RW_v0));
                rw = Marshal.AllocHGlobal(rwS);

                var rodS = Marshal.SizeOf(typeof(TCP_ESTATS_BANDWIDTH_ROD_v0));
                rod = Marshal.AllocHGlobal(rodS);

                var r = GetPerTcp6ConnectionEStats(ref row, TCP_ESTATS_TYPE.TcpConnectionEstatsBandwidth, rw, (uint)0, (uint)rwS, IntPtr.Zero, (uint)0, (uint)0, rod, (uint)0, (uint)rodS);
                if (r != 0)
                {
                    throw new Win32Exception((int)r);
                }

                var parsedRW = (TCP_ESTATS_BANDWIDTH_RW_v0)Marshal.PtrToStructure(rw, typeof(TCP_ESTATS_BANDWIDTH_RW_v0));
                if (parsedRW.EnableCollectionInbound != TCP_BOOLEAN_OPTIONAL.TcpBoolOptEnabled || parsedRW.EnableCollectionOutbound != TCP_BOOLEAN_OPTIONAL.TcpBoolOptEnabled)
                {
                    throw new Exception("Monitoring is disabled for this connection.");
                }

                return (TCP_ESTATS_BANDWIDTH_ROD_v0)Marshal.PtrToStructure(rod, typeof(TCP_ESTATS_BANDWIDTH_ROD_v0));
            }
            catch (Win32Exception we)
            {
                if (we.NativeErrorCode == 1168)
                {
                    return new TCP_ESTATS_BANDWIDTH_ROD_v0() { InboundBandwidth = 0, OutboundBandwidth = 0 };
                }
                else
                {
                    throw we;
                }
            }
            catch (Exception e)
            {
                throw e;
            }
            finally
            {
                if (rw != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(rw);
                }

                if (rod != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(rod);
                }
            }
        }


        public static TCP_ESTATS_DATA_ROD_v0 GetTCPStatistics(MIB_TCP6ROW_OWNER_MODULE conn)
        {
            IntPtr rw = IntPtr.Zero;
            IntPtr rod = IntPtr.Zero;

            try
            {
                var row = new MIB_TCP6ROW() { _localAddress = conn._localAddress, _remoteAddress = conn._remoteAddress, _localPort = conn._localPort, _remotePort = conn._remotePort, State = conn._state, LocalScopeId = conn.LocalScopeId, RemoteScopeId = conn.RemoteScopeId };

                var rwS = Marshal.SizeOf(typeof(TCP_ESTATS_DATA_RW_v0));
                rw = Marshal.AllocHGlobal(rwS);

                var rodS = Marshal.SizeOf(typeof(TCP_ESTATS_DATA_ROD_v0));
                rod = Marshal.AllocHGlobal(rodS);

                GetPerTcp6ConnectionEStats(ref row, TCP_ESTATS_TYPE.TcpConnectionEstatsData, rw, (uint)0, (uint)rwS, IntPtr.Zero, (uint)0, (uint)0, rod, (uint)0, (uint)rodS);

                var parsedRW = (TCP_ESTATS_DATA_RW_v0)Marshal.PtrToStructure(rw, typeof(TCP_ESTATS_DATA_RW_v0));
                var parsedROD = (TCP_ESTATS_DATA_ROD_v0)Marshal.PtrToStructure(rod, typeof(TCP_ESTATS_DATA_ROD_v0));

                return parsedROD;
            }
            finally
            {
                if (rw != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(rw);
                }

                if (rod != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(rod);
                }
            }
        }
    }
}