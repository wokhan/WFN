using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net;
using System.Runtime.InteropServices;

namespace Wokhan.WindowsFirewallNotifier.Common.Helpers.IPHelpers
{
    public class TCPHelper : IPHelper
    {
        [DllImport("kernel32.dll", EntryPoint = "RtlZeroMemory", SetLastError = false)]
        public static extern void ZeroMemory(IntPtr dest, uint size);

        [DllImport("iphlpapi.dll", SetLastError = true)]
        public static extern uint GetOwnerModuleFromTcpEntry(ref MIB_TCPROW_OWNER_MODULE pTcpEntry, TCPIP_OWNER_MODULE_INFO_CLASS Class, IntPtr Buffer, ref uint pdwSize);

        [DllImport("iphlpapi.dll", SetLastError = true)]
        public static extern uint GetExtendedTcpTable(IntPtr pTcpTable, ref uint dwOutBufLen, [MarshalAs(UnmanagedType.Bool)] bool sort, AF_INET ipVersion, TCP_TABLE_CLASS tblClass, uint reserved);

        [DllImport("iphlpapi.dll", SetLastError = true)]
        public static extern uint GetPerTcpConnectionEStats(ref MIB_TCPROW Row, TCP_ESTATS_TYPE EstatsType, IntPtr Rw, uint RwVersion, uint RwSize, IntPtr Ros, uint RosVersion, uint RosSize, IntPtr Rod, uint RodVersion, uint RodSize);

        [DllImport("iphlpapi.dll", SetLastError = true)]
        public static extern uint SetPerTcpConnectionEStats(ref MIB_TCPROW Row, TCP_ESTATS_TYPE EstatsType, IntPtr Rw, uint RwVersion, uint RwSize, uint Offset);

        protected const uint NO_ERROR = 0;
        protected const uint ERROR_INSUFFICIENT_BUFFER = 122;
        protected const uint ERROR_NOT_FOUND = 1168;

        public enum TCP_TABLE_CLASS
        {
            TCP_TABLE_BASIC_LISTENER,
            TCP_TABLE_BASIC_CONNECTIONS,
            TCP_TABLE_BASIC_ALL,
            TCP_TABLE_OWNER_PID_LISTENER,
            TCP_TABLE_OWNER_PID_CONNECTIONS,
            TCP_TABLE_OWNER_PID_ALL,
            TCP_TABLE_OWNER_MODULE_LISTENER,
            TCP_TABLE_OWNER_MODULE_CONNECTIONS,
            TCP_TABLE_OWNER_MODULE_ALL
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MIB_TCPROW
        {
            public uint dwState;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public byte[] dwLocalAddr;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public byte[] dwLocalPort;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public byte[] dwRemoteAddr;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public byte[] dwRemotePort;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MIB_TCPSTATS
        {
            public int dwRtoAlgorithm;
            public int dwRtoMin;
            public int dwRtoMax;
            public int dwMaxConn;
            public int dwActiveOpens;
            public int dwPassiveOpens;
            public int dwAttemptFails;
            public int dwEstabResets;
            public int dwCurrEstab;
            public int dwInSegs;
            public int dwOutSegs;
            public int dwRetransSegs;
            public int dwInErrs;
            public int dwOutRsts;
            public int dwNumConns;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct TCP_ESTATS_DATA_RW_v0
        {
            public bool EnableCollection;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct TCP_ESTATS_BANDWIDTH_RW_v0
        {
            public TCP_BOOLEAN_OPTIONAL EnableCollectionOutbound;
            public TCP_BOOLEAN_OPTIONAL EnableCollectionInbound;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct TCP_ESTATS_DATA_ROD_v0
        {
            public uint DataBytesOut;
            public uint DataSegsOut;
            public uint DataBytesIn;
            public uint DataSegsIn;
            public uint SegsOut;
            public uint SegsIn;
            public uint SoftErrors;
            public uint SoftErrorReason;
            public uint SndUna;
            public uint SndNxt;
            public uint SndMax;
            public uint ThruBytesAcked;
            public uint RcvNxt;
            public uint ThruBytesReceived;
        }

        public enum TCP_ESTATS_TYPE
        {
            TcpConnectionEstatsSynOpts,
            TcpConnectionEstatsData,
            TcpConnectionEstatsSndCong,
            TcpConnectionEstatsPath,
            TcpConnectionEstatsSendBuff,
            TcpConnectionEstatsRec,
            TcpConnectionEstatsObsRec,
            TcpConnectionEstatsBandwidth,
            TcpConnectionEstatsFineRtt,
            TcpConnectionEstatsMaximum
        }

        public enum TCP_BOOLEAN_OPTIONAL
        {
            TcpBoolOptDisabled = 0,
            TcpBoolOptEnabled = 1,
            TcpBoolOptUnchanged = -1
        }

        public struct TCP_ESTATS_BANDWIDTH_ROD_v0
        {
            public ulong OutboundBandwidth;
            public ulong InboundBandwidth;
            public ulong OutboundInstability;
            public ulong InboundInstability;
            public bool OutboundBandwidthPeaked;
            public bool InboundBandwidthPeaked;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MIB_TCPROW_OWNER_MODULE : I_OWNER_MODULE
        {
            internal uint _state;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            internal byte[] _localAddr;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            internal byte[] _localPort;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public byte[] _remoteAddr;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            internal byte[] _remotePort;
            internal uint _owningPid;
            internal long _creationTime;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
            internal ulong[] _owningModuleInfo;

            public byte[] RemoteAddrBytes { get { return _remoteAddr; } }
            public MIB_TCP_STATE State { get { return (MIB_TCP_STATE)_state; } }
            public string RemoteAddress { get { return GetAddressAsString(_remoteAddr); } }
            public string LocalAddress { get { return GetAddressAsString(_localAddr); } }
            public int RemotePort { get { return GetRealPort(_remotePort); } }
            public int LocalPort { get { return GetRealPort(_localPort); } }
            public Owner OwnerModule { get { return GetOwningModuleTCP(this); } }
            public string Protocol { get { return "TCP"; } }
            public uint OwningPid { get { return _owningPid; } }
            public DateTime? CreationTime { get { return _creationTime == 0 ? (DateTime?)null : DateTime.FromFileTime(_creationTime); } }
            public bool IsLoopback { get { return IPAddress.IsLoopback(IPAddress.Parse(RemoteAddress)); } }

            public MIB_TCPROW ToTCPRow()
            {
                return new MIB_TCPROW() { dwLocalAddr = _localAddr, dwRemoteAddr = _remoteAddr, dwLocalPort = _localPort, dwRemotePort = _remotePort, dwState = _state };
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MIB_TCPTABLE_OWNER_MODULE
        {
            public uint NumEntries;
            public MIB_TCPROW_OWNER_MODULE FirstEntry;
        }


        /// <summary>
        /// 
        /// </summary>
        public static IEnumerable<I_OWNER_MODULE> GetAllTCPConnections()
        {
            IntPtr buffTable = IntPtr.Zero;

            try
            {
                uint buffSize = 0;
                GetExtendedTcpTable(IntPtr.Zero, ref buffSize, false, AF_INET.IP4, TCP_TABLE_CLASS.TCP_TABLE_OWNER_MODULE_ALL, 0);

                buffTable = Marshal.AllocHGlobal((int)buffSize);

                uint ret = GetExtendedTcpTable(buffTable, ref buffSize, false, AF_INET.IP4, TCP_TABLE_CLASS.TCP_TABLE_OWNER_MODULE_ALL, 0);
                if (ret == 0)
                {
                    MIB_TCPTABLE_OWNER_MODULE tab = (MIB_TCPTABLE_OWNER_MODULE)Marshal.PtrToStructure(buffTable, typeof(MIB_TCPTABLE_OWNER_MODULE));
                    IntPtr rowPtr = (IntPtr)((long)buffTable + (long)Marshal.OffsetOf(typeof(MIB_TCPTABLE_OWNER_MODULE), "FirstEntry"));

                    MIB_TCPROW_OWNER_MODULE current;
                    for (uint i = 0; i < tab.NumEntries; i++)
                    {
                        current = (MIB_TCPROW_OWNER_MODULE)Marshal.PtrToStructure(rowPtr, typeof(MIB_TCPROW_OWNER_MODULE));
                        rowPtr = (IntPtr)((long)rowPtr + (long)Marshal.SizeOf(current));

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

        public static void EnsureStatsAreEnabled(MIB_TCPROW row)
        {

            var rwS = Marshal.SizeOf(typeof(TCP_ESTATS_BANDWIDTH_RW_v0));
            IntPtr rw = Marshal.AllocHGlobal(rwS);
            Marshal.StructureToPtr(new TCP_ESTATS_BANDWIDTH_RW_v0() { EnableCollectionInbound = TCP_BOOLEAN_OPTIONAL.TcpBoolOptEnabled, EnableCollectionOutbound = TCP_BOOLEAN_OPTIONAL.TcpBoolOptEnabled }, rw, true);

            try
            {
                var r = SetPerTcpConnectionEStats(ref row, TCP_ESTATS_TYPE.TcpConnectionEstatsBandwidth, rw, 0, (uint)rwS, 0);
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

        public static TCP_ESTATS_BANDWIDTH_ROD_v0 GetTCPBandwidth(MIB_TCPROW row)
        {
            IntPtr rw = IntPtr.Zero;
            IntPtr rod = IntPtr.Zero;

            try
            {
                var rwS = Marshal.SizeOf(typeof(TCP_ESTATS_BANDWIDTH_RW_v0));
                rw = Marshal.AllocHGlobal(rwS);

                var rodS = Marshal.SizeOf(typeof(TCP_ESTATS_BANDWIDTH_ROD_v0));
                rod = Marshal.AllocHGlobal(rodS);

                var r = GetPerTcpConnectionEStats(ref row, TCP_ESTATS_TYPE.TcpConnectionEstatsBandwidth, rw, 0, (uint)rwS, IntPtr.Zero, 0, 0, rod, 0, (uint)rodS);
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
                if (we.NativeErrorCode == ERROR_NOT_FOUND)
                {
                    return new TCP_ESTATS_BANDWIDTH_ROD_v0() { InboundBandwidth = 0, OutboundBandwidth = 0 };
                }
                else
                {
                    throw;
                }
            }
            catch (Exception)
            {
                throw;
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

        public static TCP_ESTATS_DATA_ROD_v0 GetTCPStatistics(MIB_TCPROW_OWNER_MODULE conn)
        {
            IntPtr rw = IntPtr.Zero;
            IntPtr rod = IntPtr.Zero;

            try
            {
                var row = new MIB_TCPROW() { dwLocalAddr = conn._localAddr, dwRemoteAddr = conn._remoteAddr, dwLocalPort = conn._localPort, dwRemotePort = conn._remotePort, dwState = conn._state };

                EnsureStatsAreEnabled(row);

                var rwS = Marshal.SizeOf(typeof(TCP_ESTATS_DATA_RW_v0));
                rw = Marshal.AllocHGlobal(rwS);

                var rodS = Marshal.SizeOf(typeof(TCP_ESTATS_DATA_ROD_v0));
                rod = Marshal.AllocHGlobal(rodS);

                var resp = GetPerTcpConnectionEStats(ref row, TCP_ESTATS_TYPE.TcpConnectionEstatsData, rw, 0, (uint)rwS, IntPtr.Zero, 0, 0, rod, 0, (uint)rodS);
                if (resp != NO_ERROR)
                {
                    LogHelper.Error("Unable to get the connection statistics.", new Win32Exception((int)resp));
                }

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

        private static Dictionary<MIB_TCPROW_OWNER_MODULE, Owner> ownerCache = new Dictionary<MIB_TCPROW_OWNER_MODULE, Owner>();
        internal static Owner GetOwningModuleTCP(MIB_TCPROW_OWNER_MODULE row)
        {
            Owner ret = null;
            /*if (ownerCache.TryGetValue(row, out ret))
            {
                return ret;
            }*/

            if (row.OwningPid == 0)
            {
                return Owner.System;
            }

            IntPtr buffer = IntPtr.Zero;
            try
            {
                uint buffSize = 0;
                var retn = GetOwnerModuleFromTcpEntry(ref row, TCPIP_OWNER_MODULE_INFO_CLASS.TCPIP_OWNER_MODULE_INFO_BASIC, IntPtr.Zero, ref buffSize);
                if ((retn != NO_ERROR) && (retn != ERROR_INSUFFICIENT_BUFFER))
                {
                    //Cannot get owning module for this connection
                    LogHelper.Info("Unable to get the connection owner: ownerPid=" + row.OwningPid +" remoteAdr=" + row.RemoteAddress + ":" + row.RemotePort);
                    return ret;
                }
                if (buffSize == 0)
                {
                    //No buffer? Probably means we can't retrieve any information about this connection; skip it
                    LogHelper.Info("Unable to get the connection owner (no buffer).");
                    return ret;
                }
                buffer = Marshal.AllocHGlobal((int)buffSize);

                //GetOwnerModuleFromTcpEntry needs the fields of TCPIP_OWNER_MODULE_INFO_BASIC to be NULL
                ZeroMemory(buffer, buffSize);

                var resp = GetOwnerModuleFromTcpEntry(ref row, TCPIP_OWNER_MODULE_INFO_CLASS.TCPIP_OWNER_MODULE_INFO_BASIC, buffer, ref buffSize);
                if (resp == 0)
                {
                    ret = new Owner((TCPIP_OWNER_MODULE_BASIC_INFO)Marshal.PtrToStructure(buffer, typeof(TCPIP_OWNER_MODULE_BASIC_INFO)));
                }
                else if (resp != ERROR_NOT_FOUND) // Ignore closed connections
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
    }
}
