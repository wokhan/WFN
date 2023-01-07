namespace Wokhan.WindowsFirewallNotifier.Common.Net.IP.TCP;

public partial class TCPHelper
{
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

    internal protected enum TCP_ESTATS_TYPE
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

}
