using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using Wokhan.WindowsFirewallNotifier.Common.Helpers;
using Wokhan.WindowsFirewallNotifier.Common.Helpers.IPHelpers;

namespace Wokhan.WindowsFirewallNotifier.Common.Net.IP.UDP
{
    public partial class UDPHelper : IPHelper
    {
        protected enum UDP_TABLE_CLASS
        {
            UDP_TABLE_BASIC,
            UDP_TABLE_OWNER_PID,
            UDP_TABLE_OWNER_MODULE
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MIB_UDPTABLE_OWNER_MODULE
        {
            public uint NumEntries;
            public MIB_UDPROW_OWNER_MODULE FirstEntry;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<IConnectionOwnerInfo> GetAllUDPConnections()
        {
            IntPtr buffTable = IntPtr.Zero;

            try
            {
                var buffSize = 0;
                _ = NativeMethods.GetExtendedUdpTable(IntPtr.Zero, ref buffSize, false, AF_INET.IP4, UDP_TABLE_CLASS.UDP_TABLE_OWNER_MODULE, 0);

                buffTable = Marshal.AllocHGlobal(buffSize);

                var ret = NativeMethods.GetExtendedUdpTable(buffTable, ref buffSize, false, AF_INET.IP4, UDP_TABLE_CLASS.UDP_TABLE_OWNER_MODULE, 0);
                if (ret == 0)
                {
                    var tab = Marshal.PtrToStructure<MIB_UDPTABLE_OWNER_MODULE>(buffTable);
                    var rowPtr = (IntPtr)((long)buffTable + (long)Marshal.OffsetOf<MIB_UDPTABLE_OWNER_MODULE>(nameof(MIB_UDPTABLE_OWNER_MODULE.FirstEntry)));

                    MIB_UDPROW_OWNER_MODULE current;
                    for (var i = 0; i < tab.NumEntries; i++)
                    {
                        current = Marshal.PtrToStructure<MIB_UDPROW_OWNER_MODULE>(rowPtr);
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

        private static Dictionary<MIB_UDPROW_OWNER_MODULE, Owner> ownerCache = new Dictionary<MIB_UDPROW_OWNER_MODULE, Owner>();
        internal static Owner? GetOwningModuleUDP(MIB_UDPROW_OWNER_MODULE row)
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
                var retn = NativeMethods.GetOwnerModuleFromUdpEntry(ref row, TCPIP_OWNER_MODULE_INFO_CLASS.TCPIP_OWNER_MODULE_INFO_BASIC, IntPtr.Zero, ref buffSize);
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

                //GetOwnerModuleFromUdpEntry might want the fields of TCPIP_OWNER_MODULE_INFO_BASIC to be NULL
                IPHelper.NativeMethods.ZeroMemory(buffer, buffSize);

                var resp = UDPHelper.NativeMethods.GetOwnerModuleFromUdpEntry(ref row, TCPIP_OWNER_MODULE_INFO_CLASS.TCPIP_OWNER_MODULE_INFO_BASIC, buffer, ref buffSize);
                if (resp == NO_ERROR)
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
