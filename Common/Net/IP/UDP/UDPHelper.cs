using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;

using Wokhan.WindowsFirewallNotifier.Common.Net.IP.UDP.UDP6;

using static Wokhan.WindowsFirewallNotifier.Common.Net.IP.IPHelper;

namespace Wokhan.WindowsFirewallNotifier.Common.Net.IP.UDP;

public class UDPHelper 
{
    internal static IEnumerable<IConnectionOwnerInfo> GetAllUDPConnections<TTable, TRow>(AF_INET aF_INET) where TRow : IConnectionOwnerInfo
    {
        IntPtr buffTable = IntPtr.Zero;

        try
        {
            var buffSize = 0;
            _ = NativeMethods.GetExtendedUdpTable(IntPtr.Zero, ref buffSize, false, aF_INET, UDP_TABLE_CLASS.UDP_TABLE_OWNER_MODULE, 0);

            buffTable = Marshal.AllocHGlobal(buffSize);

            var ret = NativeMethods.GetExtendedUdpTable(buffTable, ref buffSize, false, aF_INET, UDP_TABLE_CLASS.UDP_TABLE_OWNER_MODULE, 0);
            if (ret == 0)
            {
                var tab = Marshal.PtrToStructure<BaseTcpTableOwnerModule>(buffTable);
                var rowPtr = (IntPtr)((long)buffTable + (long)Marshal.OffsetOf<TTable>(nameof(UDP4.MIB_UDPTABLE_OWNER_MODULE.FirstEntry)));

                TRow current;
                for (var i = 0; i < tab.NumEntries; i++)
                {
                    current = Marshal.PtrToStructure<TRow>(rowPtr);
                    rowPtr = (IntPtr)((long)rowPtr + Marshal.SizeOf(current));

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

    public static IEnumerable<IConnectionOwnerInfo> GetAllUDPConnections() => GetAllUDPConnections<UDP4.MIB_UDPTABLE_OWNER_MODULE, UDP4.MIB_UDPROW_OWNER_MODULE>(AF_INET.IP4);
    public static IEnumerable<IConnectionOwnerInfo> GetAllUDP6Connections() => GetAllUDPConnections<MIB_UDP6TABLE_OWNER_MODULE, MIB_UDP6ROW_OWNER_MODULE>(AF_INET.IP6);

}
