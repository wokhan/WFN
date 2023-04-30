using System;

using Windows.Win32.NetworkManagement.IpHelper;

namespace Wokhan.WindowsFirewallNotifier.Common.Net.IP;

public interface IConnectionOwnerInfo
{
    internal uint SetPerTcpConnectionEStats(ref TCP_ESTATS_BANDWIDTH_RW_v0 rw, MIB_TCP6ROW? tcp6Row)
    {
        throw new NotImplementedException();
    }

    internal TCP_ESTATS_BANDWIDTH_ROD_v0? GetPerTcpConnectionEState(MIB_TCP6ROW? tcp6Row)
    {
        throw new NotImplementedException();
    }

    internal uint GetOwnerModule(IntPtr buffer, ref uint buffSize);
}