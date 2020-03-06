using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Wokhan.WindowsFirewallNotifier.Common.Helpers
{
    public class CommonHelper
    {
        [DllImport("Wtsapi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool WTSQueryUserToken(uint SessionId, ref IntPtr phToken);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern uint WTSGetActiveConsoleSessionId();

        public static void OverrideSettingsFile(string fileName)
        {
            //AppDomain.CurrentDomain.SetupInformation.ConfigurationFile = fileName;
        }

        [DllImport("shlwapi.dll", BestFitMapping = false, CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = false, ThrowOnUnmappableChar = true)]
        private static extern int SHLoadIndirectString(string pszSource, StringBuilder pszOutBuf, uint cchOutBuf, IntPtr ppvReserved);

        public static string GetMSResourceString(string src)
        {
            if (src != null && src.StartsWith("@"))
            {
                StringBuilder sb = new StringBuilder(1024); //FIXME: Hardcoded maximum string size!
                if (0 == SHLoadIndirectString(Environment.ExpandEnvironmentVariables(src), sb, (uint)sb.Capacity, IntPtr.Zero))
                {
                    src = sb.ToString();
                }

            }
            return src;

        }

        public static string FormatBytes(double size, string suffix = null)
        {
            if (size >= 1024.0 * 1024.0 * 1024.0 * 1024.0)
            {
                return String.Format("{0:##.##}TiB{1}", size / (1024.0 * 1024.0 * 1024.0 * 1024.0), suffix);
            }
            else if (size >= 1024.0 * 1024.0 * 1024.0)
            {
                return String.Format("{0:##.##}GiB{1}", size / (1024.0 * 1024.0 * 1024.0), suffix);
            }
            else if (size >= 1024.0 * 1024.0)
            {
                return String.Format("{0:##.##}MiB{1}", size / (1024.0 * 1024.0), suffix);
            }
            else if (size >= 1024.0)
            {
                return String.Format("{0:##.##}KiB{1}", size / 1024.0, suffix);
            }

            return String.Format("{0:#0.##}B{1}", size, suffix);
        }
    }
}