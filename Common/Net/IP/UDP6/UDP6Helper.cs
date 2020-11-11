using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;

using Wokhan.WindowsFirewallNotifier.Common.Logging;
using Wokhan.WindowsFirewallNotifier.Common.Net.IP.UDP;

namespace Wokhan.WindowsFirewallNotifier.Common.Net.IP.UDP6
{
    public partial class UDP6Helper : UDPHelper
    {

        [StructLayout(LayoutKind.Sequential)]
        private struct MIB_UDP6TABLE_OWNER_MODULE
        {
            public uint NumEntries;
            public MIB_UDP6ROW_OWNER_MODULE FirstEntry;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<IConnectionOwnerInfo> GetAllUDP6Connections()
        {
            IntPtr buffTable = IntPtr.Zero;

            try
            {
                var buffSize = 0;
                _ = UDPHelper.NativeMethods.GetExtendedUdpTable(IntPtr.Zero, ref buffSize, true, AF_INET.IP6, UDP_TABLE_CLASS.UDP_TABLE_OWNER_MODULE, 0);

                buffTable = Marshal.AllocHGlobal(buffSize);

                uint ret = UDPHelper.NativeMethods.GetExtendedUdpTable(buffTable, ref buffSize, true, AF_INET.IP6, UDP_TABLE_CLASS.UDP_TABLE_OWNER_MODULE, 0);
                if (ret == 0)
                {
                    MIB_UDP6TABLE_OWNER_MODULE tab = Marshal.PtrToStructure<MIB_UDP6TABLE_OWNER_MODULE>(buffTable);
                    var rowPtr = (IntPtr)((long)buffTable + (long)Marshal.OffsetOf<MIB_UDP6TABLE_OWNER_MODULE>(nameof(MIB_UDP6TABLE_OWNER_MODULE.FirstEntry)));

                    MIB_UDP6ROW_OWNER_MODULE current;
                    for (var i = 0; i < tab.NumEntries; i++)
                    {
                        current = Marshal.PtrToStructure<MIB_UDP6ROW_OWNER_MODULE>(rowPtr);
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

        private static Dictionary<MIB_UDP6ROW_OWNER_MODULE, Owner> ownerCache = new Dictionary<MIB_UDP6ROW_OWNER_MODULE, Owner>();
        internal static Owner? GetOwningModuleUDP6(MIB_UDP6ROW_OWNER_MODULE row)
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
                var retn = NativeMethods.GetOwnerModuleFromUdp6Entry(ref row, TCPIP_OWNER_MODULE_INFO_CLASS.TCPIP_OWNER_MODULE_INFO_BASIC, IntPtr.Zero, ref buffSize);
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

                //GetOwnerModuleFromUdp6Entry might want the fields of TCPIP_OWNER_MODULE_INFO_BASIC to be NULL
                IPHelper.NativeMethods.ZeroMemory(buffer, buffSize);

                var resp = NativeMethods.GetOwnerModuleFromUdp6Entry(ref row, TCPIP_OWNER_MODULE_INFO_CLASS.TCPIP_OWNER_MODULE_INFO_BASIC, buffer, ref buffSize);
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
