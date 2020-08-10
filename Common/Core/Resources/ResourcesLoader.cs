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
    }
}