using System;
using System.Runtime.InteropServices;

namespace Wokhan.WindowsFirewallNotifier.Common.Net.IP
{
    public class Owner
    {
        public static Owner System { get; } = new Owner("System", "System");

        public string ModuleName { get; private set; }
        public string ModulePath { get; private set; }

        internal Owner(IPHelper.TCPIP_OWNER_MODULE_BASIC_INFO inf)
        {
            ModuleName = Marshal.PtrToStringAuto(inf.p1) ?? String.Empty;
            ModulePath = Marshal.PtrToStringAuto(inf.p2) ?? String.Empty;
        }

        public Owner(string moduleName, string modulePath)
        {
            ModuleName = moduleName;
            ModulePath = modulePath;
        }
    }
}
