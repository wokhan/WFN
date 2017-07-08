using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Wokhan.WindowsFirewallNotifier.Common.Helpers;

namespace Wokhan.WindowsFirewallNotifier.Common.Extensions
{
    public static class ProcessExtensions
    {
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool QueryFullProcessImageName(IntPtr hprocess, uint dwFlags, StringBuilder lpExeName, ref uint size);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr OpenProcess(ProcessHelper.ProcessAccessFlags dwDesiredAccess, [MarshalAs(UnmanagedType.Bool)] bool bInheritHandle, uint dwProcessId);

        public static string GetModulePath(this Process src)
        {
            try
            {
                return src.MainModule.FileName;
            }
            catch
            {
                /*var buffer = new StringBuilder(1024);
                IntPtr hprocess = OpenProcess(ProcessHelper.ProcessAccessFlags.QueryLimitedInformation, false, src.Id);
                if (hprocess != IntPtr.Zero)
                {
                    try
                    {
                        uint size = buffer.Capacity;
                        if (QueryFullProcessImageName(hprocess, 0, buffer, ref size))
                        {
                            return buffer.ToString();
                        }
                    }
                    finally
                    {
                        Marshal.FreeHGlobal(hprocess);
                    }
                }*/

                return "Protected";
            }
        }
    }
}
