using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using Wokhan.WindowsFirewallNotifier.Common.Helpers;

namespace Wokhan.WindowsFirewallNotifier.Common.Net.IP
{
    public partial class TCP6Helper : TCPHelper
    {

        [StructLayout(LayoutKind.Sequential)]
        private struct MIB_TCP6TABLE_OWNER_MODULE
        {
            public uint NumEntries;
            public MIB_TCP6ROW_OWNER_MODULE FirstEntry;
        }

        /// <summary>
        /// 
        /// </summary>
        public static IEnumerable<IConnectionOwnerInfo> GetAllTCP6Connections()
        {
            IntPtr buffTable = IntPtr.Zero;

            try
            {
                uint buffSize = 0;
                _ = TCPHelper.NativeMethods.GetExtendedTcpTable(IntPtr.Zero, ref buffSize, false, AF_INET.IP6, TCP_TABLE_CLASS.TCP_TABLE_OWNER_MODULE_ALL, 0);

                buffTable = Marshal.AllocHGlobal((int)buffSize);

                uint ret = TCPHelper.NativeMethods.GetExtendedTcpTable(buffTable, ref buffSize, false, AF_INET.IP6, TCP_TABLE_CLASS.TCP_TABLE_OWNER_MODULE_ALL, 0);
                if (ret == 0)
                {
                    var tab = Marshal.PtrToStructure<MIB_TCP6TABLE_OWNER_MODULE>(buffTable);
                    var rowPtr = (long)buffTable + (long)Marshal.OffsetOf<MIB_TCP6TABLE_OWNER_MODULE>(nameof(MIB_TCP6TABLE_OWNER_MODULE.FirstEntry));

                    MIB_TCP6ROW_OWNER_MODULE current;
                    for (uint i = 0; i < tab.NumEntries; i++)
                    {
                        current = Marshal.PtrToStructure<MIB_TCP6ROW_OWNER_MODULE>((IntPtr)rowPtr);
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
        internal static Owner? GetOwningModuleTCP6(MIB_TCP6ROW_OWNER_MODULE row)
        {
            Owner? ret = null;
            //if (ownerCache.TryGetValue(row, out ret))
            //{
            //    return ret;
            //}

            IntPtr buffer = IntPtr.Zero;
            try
            {
                uint buffSize = 0;
                var retn = NativeMethods.GetOwnerModuleFromTcp6Entry(ref row, TCPIP_OWNER_MODULE_INFO_CLASS.TCPIP_OWNER_MODULE_INFO_BASIC, IntPtr.Zero, ref buffSize);
                if (retn != NO_ERROR && retn != ERROR_INSUFFICIENT_BUFFER)
                {
                    //Cannot get owning module for this connection
                    LogHelper.Info("Unable to get the connection owner.");
                    return ret;
                }
                if (buffSize == 0)
                {
                    //No buffer? Probably means we can't retrieve any information about this connection; skip it
                    LogHelper.Info("Unable to get the connection owner.");
                    return ret;
                }
                buffer = Marshal.AllocHGlobal((int)buffSize);

                //GetOwnerModuleFromTcp6Entry needs the fields of TCPIP_OWNER_MODULE_INFO_BASIC to be NULL
                IPHelper.NativeMethods.ZeroMemory(buffer, buffSize);

                var resp = NativeMethods.GetOwnerModuleFromTcp6Entry(ref row, TCPIP_OWNER_MODULE_INFO_CLASS.TCPIP_OWNER_MODULE_INFO_BASIC, buffer, ref buffSize);
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

        public static void EnsureStatsAreEnabled(MIB_TCP6ROW row)
        {

            var rwS = Marshal.SizeOf<TCP_ESTATS_BANDWIDTH_RW_v0>();
            IntPtr rw = Marshal.AllocHGlobal(rwS);
            Marshal.StructureToPtr(new TCP_ESTATS_BANDWIDTH_RW_v0() { EnableCollectionInbound = TCP_BOOLEAN_OPTIONAL.TcpBoolOptEnabled, EnableCollectionOutbound = TCP_BOOLEAN_OPTIONAL.TcpBoolOptEnabled }, rw, true);

            try
            {
                var r = NativeMethods.SetPerTcp6ConnectionEStats(ref row, TCP_ESTATS_TYPE.TcpConnectionEstatsBandwidth, rw, 0, (uint)rwS, 0);
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

                var r = NativeMethods.GetPerTcp6ConnectionEStats(ref row, TCP_ESTATS_TYPE.TcpConnectionEstatsBandwidth, rw, 0, (uint)rwS, IntPtr.Zero, 0, 0, rod, 0, (uint)rodS);
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

                var resp = NativeMethods.GetPerTcp6ConnectionEStats(ref row, TCP_ESTATS_TYPE.TcpConnectionEstatsData, rw, 0, (uint)rwS, IntPtr.Zero, 0, 0, rod, 0, (uint)rodS);
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
    }
}