using Microsoft.Win32;

using System;
using System.Linq;

namespace Wokhan.WindowsFirewallNotifier.Common.Core.Resources;

public static partial class ResourcesLoader
{
    public static string GetMSResourceString(string src)
    {
        if (String.IsNullOrEmpty(src))
        {
            return String.Empty;
        }

        if (src?.StartsWith("@") ?? false)
        {
            unsafe
            {
                char* sb = stackalloc char[1024]; //FIXME: Hardcoded maximum string size!
                if (0 == NativeMethods.SHLoadIndirectString(Environment.ExpandEnvironmentVariables(src), sb, (uint)1024, IntPtr.Zero))
                {
                    src = new String(sb);
                }
            }

        }

        return src ?? String.Empty;
    }
}