using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;

namespace Wokhan.WindowsFirewallNotifier.Common.Helpers.IPHelpers
{
    public class TCP6Helper : TCPHelper
    {
        [DllImport("iphlpapi.dll", SetLastError = true)]
        public static extern uint GetOwnerModuleFromTcp6Entry(ref MIB_TCP6ROW_OWNER_MODULE pTcpEntry, TCPIP_OWNER_MODULE_INFO_CLASS Class, IntPtr Buffer, ref int pdwSize);


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

            public uint OwningPid { get { return _owningPid; } }
            public string LocalAddress { get { return GetRealAddress(_localAddress); } }
            public int LocalPort { get { return GetRealPort(_localPort); } }

            public string RemoteAddress { get { return GetRealAddress(_remoteAddress); } }
            public int RemotePort { get { return GetRealPort(_remotePort); } }

            public Owner OwnerModule { get { return GetOwningModule(this); } }

            public MIB_TCP_STATE State { get { return (MIB_TCP_STATE)_state; } }
            public DateTime CreationTime { get { return _creationTime == 0 ? DateTime.MinValue : DateTime.FromFileTime(_creationTime); } }
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
        public static MIB_TCP6ROW_OWNER_MODULE[] GetAllTCP6Connections()
        {
            IntPtr buffTable = IntPtr.Zero;

            try
            {
                int buffSize = 0;
                GetExtendedTcpTable(IntPtr.Zero, ref buffSize, true, AF_INET.IP6, TCP_TABLE_CLASS.TCP_TABLE_OWNER_MODULE_ALL, 0);

                buffTable = Marshal.AllocHGlobal(buffSize);
                MIB_TCP6ROW_OWNER_MODULE[] tTable;

                uint ret = GetExtendedTcpTable(buffTable, ref buffSize, true, AF_INET.IP6, TCP_TABLE_CLASS.TCP_TABLE_OWNER_MODULE_ALL, 0);
                if (ret == 0)
                {
                    MIB_TCP6TABLE_OWNER_MODULE tab = (MIB_TCP6TABLE_OWNER_MODULE)Marshal.PtrToStructure(buffTable, typeof(MIB_TCP6TABLE_OWNER_MODULE));
                    IntPtr rowPtr = (IntPtr)((long)buffTable + (long)Marshal.OffsetOf(typeof(MIB_TCP6TABLE_OWNER_MODULE), "FirstEntry"));

                    tTable = new MIB_TCP6ROW_OWNER_MODULE[tab.NumEntries];
                    for (int i = 0; i < tab.NumEntries; i++)
                    {
                        tTable[i] = (MIB_TCP6ROW_OWNER_MODULE)Marshal.PtrToStructure(rowPtr, typeof(MIB_TCP6ROW_OWNER_MODULE));
                        rowPtr = (IntPtr)((long)rowPtr + (long)Marshal.SizeOf(tTable[i]));
                    }

                    return tTable;
                }
                else
                {
                    throw new Exception("Unable to retrieve all connections rows (err:" + ret + ")");
                }
            }
            catch (Exception e)
            {
                return null;
            }
            finally
            {
                if (buffTable != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(buffTable);
                }
            }
        }

        internal static uint GetOwningModuleTCP(MIB_TCP6ROW_OWNER_MODULE row, ref IntPtr buffer)
        {
            int buffSize = 0;

            GetOwnerModuleFromTcp6Entry(ref row, TCPIP_OWNER_MODULE_INFO_CLASS.TCPIP_OWNER_MODULE_INFO_BASIC, IntPtr.Zero, ref buffSize);
            buffer = Marshal.AllocHGlobal(buffSize);
            return GetOwnerModuleFromTcp6Entry(ref row, TCPIP_OWNER_MODULE_INFO_CLASS.TCPIP_OWNER_MODULE_INFO_BASIC, buffer, ref buffSize);
        }


        internal static new Owner GetOwningModule(I_OWNER_MODULE row)
        {
            IntPtr buffer = IntPtr.Zero;
            try
            {
                uint resp = IPHelpers.TCP6Helper.GetOwningModuleTCP((IPHelpers.TCP6Helper.MIB_TCP6ROW_OWNER_MODULE)row, ref buffer);

                if (resp == 0)
                {
                    return new Owner((TCPIP_OWNER_MODULE_BASIC_INFO)Marshal.PtrToStructure(buffer, typeof(TCPIP_OWNER_MODULE_BASIC_INFO)));
                }
                else
                {
                    if (resp != 1168) // Ignore closed connections 
                    {
                        LogHelper.Error("Unable to get the connection owner.", new Exception("GetOwningModule returned " + resp));
                    }
                    return null;
                }
            }
            catch (Exception e)
            {
                LogHelper.Error("Unable to get the connection owner.", e);
                return null;
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


        public static TCP_ESTATS_BANDWIDTH_ROD_v0 GetTCPBandwidth(MIB_TCP6ROW_OWNER_MODULE conn)
        {
            IntPtr rw = IntPtr.Zero;
            IntPtr rod = IntPtr.Zero;

            try
            {
                var row = new MIB_TCP6ROW() { _localAddress = conn._localAddress, _remoteAddress = conn._remoteAddress, _localPort = conn._localPort, _remotePort = conn._remotePort, State = conn._state };

                EnsureStatsAreEnabled(row);

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
                var parsedROD = (TCP_ESTATS_BANDWIDTH_ROD_v0)Marshal.PtrToStructure(rod, typeof(TCP_ESTATS_BANDWIDTH_ROD_v0));

                return parsedROD;
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
    }
}