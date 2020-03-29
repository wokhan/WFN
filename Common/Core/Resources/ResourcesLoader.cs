using System;
using System.Text;

namespace Wokhan.WindowsFirewallNotifier.Common.Core.Resources
{
    public static partial class ResourcesLoader
    {
        public static string GetMSResourceString(string src)
        {
            if (src?.StartsWith("@") ?? false)
            {
                var sb = new StringBuilder(1024); //FIXME: Hardcoded maximum string size!
                if (0 == NativeMethods.SHLoadIndirectString(Environment.ExpandEnvironmentVariables(src), sb, (uint)sb.Capacity, IntPtr.Zero))
                {
                    src = sb.ToString();
                }

            }
            return src ?? String.Empty;

        }

        public static string FormatBytes(double size, string? suffix = null)
        {
            if (size >= 1024.0 * 1024.0 * 1024.0 * 1024.0)
            {
                return string.Format("{0:##.##}TiB{1}", size / (1024.0 * 1024.0 * 1024.0 * 1024.0), suffix);
            }
            else if (size >= 1024.0 * 1024.0 * 1024.0)
            {
                return string.Format("{0:##.##}GiB{1}", size / (1024.0 * 1024.0 * 1024.0), suffix);
            }
            else if (size >= 1024.0 * 1024.0)
            {
                return string.Format("{0:##.##}MiB{1}", size / (1024.0 * 1024.0), suffix);
            }
            else if (size >= 1024.0)
            {
                return string.Format("{0:##.##}KiB{1}", size / 1024.0, suffix);
            }

            return string.Format("{0:#0.##}B{1}", size, suffix);
        }
    }
}