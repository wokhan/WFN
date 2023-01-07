using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;

using Wokhan.WindowsFirewallNotifier.Common.Logging;

using static Wokhan.WindowsFirewallNotifier.Common.Net.IP.TCP.TCP6.TCP6Helper;

namespace Wokhan.WindowsFirewallNotifier.Common.Net.IP.TCP;

public partial class TCPHelper : IPHelper
{

    internal delegate uint SetPerTcpConnectionEStatsDelegate<T>(T row, TCP_ESTATS_TYPE eStatsType, IntPtr rw, uint rwVersion, uint rwSize, uint offset);

    internal delegate uint GetPerTcpConnectionEStateDelegate<T>(T row, TCP_ESTATS_TYPE eStatsType, nint rw, uint rwVersion, uint rwSize, nint ros, uint rosVersion, uint rosSize, nint rod, uint rodVersion, uint rodSize);

    internal delegate uint GetOwnerModuleFromTcpEntryDelegate<T>(T pTcpEntry, TCPIP_OWNER_MODULE_INFO_CLASS Class, IntPtr Buffer, ref uint pdwSize);

    private static IEnumerable<IConnectionOwnerInfo> GetAllTCPConnections<TTable, TRow>(AF_INET aF_INET) where TRow : IConnectionOwnerInfo
    {
        IntPtr buffTable = IntPtr.Zero;

        try
        {
            uint buffSize = 0;
            _ = TCPHelper.NativeMethods.GetExtendedTcpTable(IntPtr.Zero, ref buffSize, false, aF_INET, TCP_TABLE_CLASS.TCP_TABLE_OWNER_MODULE_ALL, 0);

            buffTable = Marshal.AllocHGlobal((int)buffSize);

            uint ret = TCPHelper.NativeMethods.GetExtendedTcpTable(buffTable, ref buffSize, false, aF_INET, TCP_TABLE_CLASS.TCP_TABLE_OWNER_MODULE_ALL, 0);
            if (ret == 0)
            {
                var tab = Marshal.PtrToStructure<BaseTcpTableOwnerModule>(buffTable);
                IntPtr rowPtr = (IntPtr)((long)buffTable + (long)Marshal.OffsetOf<TTable>(nameof(MIB_TCPTABLE_OWNER_MODULE.FirstEntry)));

                TRow current;
                for (uint i = 0; i < tab.NumEntries; i++)
                {
                    current = Marshal.PtrToStructure<TRow>(rowPtr);
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


    internal static void EnsureStatsAreEnabledInternal<T>(SetPerTcpConnectionEStatsDelegate<T> SetPerTcpConnectionEStats, T row)
    {
        var rwS = Marshal.SizeOf<TCP_ESTATS_BANDWIDTH_RW_v0>();
        IntPtr rw = Marshal.AllocHGlobal(rwS);
        Marshal.StructureToPtr(new TCP_ESTATS_BANDWIDTH_RW_v0() { EnableCollectionInbound = TCP_BOOLEAN_OPTIONAL.TcpBoolOptEnabled, EnableCollectionOutbound = TCP_BOOLEAN_OPTIONAL.TcpBoolOptEnabled }, rw, true);

        try
        {
            var r = SetPerTcpConnectionEStats(row, TCP_ESTATS_TYPE.TcpConnectionEstatsBandwidth, rw, 0, (uint)rwS, 0);
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

    internal static TCP_ESTATS_BANDWIDTH_ROD_v0 GetTCPBandwidthInternal<T>(GetPerTcpConnectionEStateDelegate<T> getPerTcpConnectionEState, T row)
    {
        IntPtr rw = IntPtr.Zero;
        IntPtr rod = IntPtr.Zero;

        var rwS = Marshal.SizeOf<TCP_ESTATS_BANDWIDTH_RW_v0>();
        rw = Marshal.AllocHGlobal(rwS);

        var rodS = Marshal.SizeOf<TCP_ESTATS_BANDWIDTH_ROD_v0>();
        rod = Marshal.AllocHGlobal(rodS);

        try
        {
            var r = getPerTcpConnectionEState(row, TCP_ESTATS_TYPE.TcpConnectionEstatsBandwidth, rw, 0, (uint)rwS, IntPtr.Zero, 0, 0, rod, 0, (uint)rodS);
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

    //private static Dictionary<MIB_TCPROW_OWNER_MODULE, Owner> ownerCache = new Dictionary<MIB_TCPROW_OWNER_MODULE, Owner>();


    internal static Owner? GetOwningModuleTCPInternal<TRow>(GetOwnerModuleFromTcpEntryDelegate<TRow> getOwnerModuleFromTcpEntry, TRow row) where TRow : IConnectionOwnerInfo
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
            var retn = getOwnerModuleFromTcpEntry(row, TCPIP_OWNER_MODULE_INFO_CLASS.TCPIP_OWNER_MODULE_INFO_BASIC, IntPtr.Zero, ref buffSize);
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
            IPHelper.NativeMethods.RtlZeroMemory(buffer, buffSize);

            var resp = getOwnerModuleFromTcpEntry(row, TCPIP_OWNER_MODULE_INFO_CLASS.TCPIP_OWNER_MODULE_INFO_BASIC, buffer, ref buffSize);
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


    public static IEnumerable<IConnectionOwnerInfo> GetAllTCPConnections() => GetAllTCPConnections<MIB_TCPTABLE_OWNER_MODULE, MIB_TCPROW_OWNER_MODULE>(AF_INET.IP4);
    
    public static IEnumerable<IConnectionOwnerInfo> GetAllTCP6Connections() => GetAllTCPConnections<MIB_TCP6TABLE_OWNER_MODULE, MIB_TCP6ROW_OWNER_MODULE>(AF_INET.IP6);

}
