using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using Wokhan.WindowsFirewallNotifier.Common.Helpers;

namespace Wokhan.WindowsFirewallNotifier.Common.Net.IP
{
    public partial class TCPHelper : IPHelper
    {
        protected enum TCP_TABLE_CLASS
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
        /*
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
        }*/
        /*
        [StructLayout(LayoutKind.Sequential)]
        protected struct TCP_ESTATS_DATA_RW_v0
        {
            public bool EnableCollection;
        }*/

        [StructLayout(LayoutKind.Sequential)]
        protected struct TCP_ESTATS_BANDWIDTH_RW_v0
        {
            public TCP_BOOLEAN_OPTIONAL EnableCollectionOutbound;
            public TCP_BOOLEAN_OPTIONAL EnableCollectionInbound;
        }
        /*
        [StructLayout(LayoutKind.Sequential)]
        protected struct TCP_ESTATS_DATA_ROD_v0
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
        }*/

        protected enum TCP_ESTATS_TYPE
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

        protected enum TCP_BOOLEAN_OPTIONAL
        {
            TcpBoolOptDisabled = 0,
            TcpBoolOptEnabled = 1,
            TcpBoolOptUnchanged = -1
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
        public static IEnumerable<IConnectionOwnerInfo> GetAllTCPConnections()
        {
            IntPtr buffTable = IntPtr.Zero;

            try
            {
                uint buffSize = 0;
                _ = NativeMethods.GetExtendedTcpTable(IntPtr.Zero, ref buffSize, false, AF_INET.IP4, TCP_TABLE_CLASS.TCP_TABLE_OWNER_MODULE_ALL, 0);

                buffTable = Marshal.AllocHGlobal((int)buffSize);

                uint ret = NativeMethods.GetExtendedTcpTable(buffTable, ref buffSize, false, AF_INET.IP4, TCP_TABLE_CLASS.TCP_TABLE_OWNER_MODULE_ALL, 0);
                if (ret == 0)
                {
                    MIB_TCPTABLE_OWNER_MODULE tab = Marshal.PtrToStructure<MIB_TCPTABLE_OWNER_MODULE>(buffTable);
                    IntPtr rowPtr = (IntPtr)((long)buffTable + (long)Marshal.OffsetOf<MIB_TCPTABLE_OWNER_MODULE>(nameof(MIB_TCPTABLE_OWNER_MODULE.FirstEntry)));

                    MIB_TCPROW_OWNER_MODULE current;
                    for (uint i = 0; i < tab.NumEntries; i++)
                    {
                        current = Marshal.PtrToStructure<MIB_TCPROW_OWNER_MODULE>(rowPtr);
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

            var rwS = Marshal.SizeOf<TCP_ESTATS_BANDWIDTH_RW_v0>();
            IntPtr rw = Marshal.AllocHGlobal(rwS);
            Marshal.StructureToPtr(new TCP_ESTATS_BANDWIDTH_RW_v0() { EnableCollectionInbound = TCP_BOOLEAN_OPTIONAL.TcpBoolOptEnabled, EnableCollectionOutbound = TCP_BOOLEAN_OPTIONAL.TcpBoolOptEnabled }, rw, true);

            try
            {
                var r = NativeMethods.SetPerTcpConnectionEStats(ref row, TCP_ESTATS_TYPE.TcpConnectionEstatsBandwidth, rw, 0, (uint)rwS, 0);
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
                var rwS = Marshal.SizeOf<TCP_ESTATS_BANDWIDTH_RW_v0>();
                rw = Marshal.AllocHGlobal(rwS);

                var rodS = Marshal.SizeOf<TCP_ESTATS_BANDWIDTH_ROD_v0>();
                rod = Marshal.AllocHGlobal(rodS);

                var r = NativeMethods.GetPerTcpConnectionEStats(ref row, TCP_ESTATS_TYPE.TcpConnectionEstatsBandwidth, rw, 0, (uint)rwS, IntPtr.Zero, 0, 0, rod, 0, (uint)rodS);
                if (r != 0)
                {
                    throw new Win32Exception((int)r);
                }

                var parsedRW = Marshal.PtrToStructure<TCP_ESTATS_BANDWIDTH_RW_v0>(rw);
                if (parsedRW.EnableCollectionInbound != TCP_BOOLEAN_OPTIONAL.TcpBoolOptEnabled || parsedRW.EnableCollectionOutbound != TCP_BOOLEAN_OPTIONAL.TcpBoolOptEnabled)
                {
                    throw new Exception("Monitoring is disabled for this connection.");
                }

                return Marshal.PtrToStructure<TCP_ESTATS_BANDWIDTH_ROD_v0>(rod);
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
        /*
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

                var resp = NativeMethods.GetPerTcpConnectionEStats(ref row, TCP_ESTATS_TYPE.TcpConnectionEstatsData, rw, 0, (uint)rwS, IntPtr.Zero, 0, 0, rod, 0, (uint)rodS);
                if (resp != NO_ERROR)
                {
                    LogHelper.Error("Unable to get the connection statistics.", new Win32Exception((int)resp));
                }

                var parsedRW = Marshal.PtrToStructure<TCP_ESTATS_DATA_RW_v0>(rw);
                var parsedROD = Marshal.PtrToStructure<TCP_ESTATS_DATA_ROD_v0>(rod);

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
        }*/

        private static Dictionary<MIB_TCPROW_OWNER_MODULE, Owner> ownerCache = new Dictionary<MIB_TCPROW_OWNER_MODULE, Owner>();
        internal static Owner? GetOwningModuleTCP(MIB_TCPROW_OWNER_MODULE row)
        {
            Owner? ret = null;
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
                var retn = NativeMethods.GetOwnerModuleFromTcpEntry(ref row, TCPIP_OWNER_MODULE_INFO_CLASS.TCPIP_OWNER_MODULE_INFO_BASIC, IntPtr.Zero, ref buffSize);
                if (retn != NO_ERROR && retn != ERROR_INSUFFICIENT_BUFFER)
                {
                    //Cannot get owning module for this connection
                    LogHelper.Info("Unable to get the connection owner: ownerPid=" + row.OwningPid + " remoteAdr=" + row.RemoteAddress + ":" + row.RemotePort);
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
                IPHelper.NativeMethods.ZeroMemory(buffer, buffSize);

                var resp = NativeMethods.GetOwnerModuleFromTcpEntry(ref row, TCPIP_OWNER_MODULE_INFO_CLASS.TCPIP_OWNER_MODULE_INFO_BASIC, buffer, ref buffSize);
                if (resp == 0)
                {
                    ret = new Owner(Marshal.PtrToStructure<TCPIP_OWNER_MODULE_BASIC_INFO>(buffer));
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
