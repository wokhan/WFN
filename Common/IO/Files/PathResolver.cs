using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

using Wokhan.WindowsFirewallNotifier.Common.Logging;

namespace Wokhan.WindowsFirewallNotifier.Common.IO.Files
{
    public static partial class PathResolver
    {
        private const int MAX_PATH = 260;

        private static Dictionary<string, string>? deviceNameMap = null;

        /// <summary>
        /// 
        /// </summary>
        private static void InitDriveMapping()
        {
            try
            {
                var drives = Directory.GetLogicalDrives();
                deviceNameMap = new Dictionary<string, string>(drives.Length);
                var sb = new StringBuilder(MAX_PATH + 1);
                string trimmedDrive;

                foreach (var drive in drives)
                {
                    trimmedDrive = drive.TrimEnd('\\');
                    if (NativeMethods.QueryDosDevice(trimmedDrive, sb, (uint)sb.Capacity) == 0)
                    {
                        throw new Win32Exception(Marshal.GetLastWin32Error(), "Call to QueryDosDevice failed!");
                    }
                    deviceNameMap.Add(sb.ToString().ToLower() + "\\", trimmedDrive); //FIXME: Switch to ToUpper?
                }
            }
            catch (Exception e)
            {
                LogHelper.Error("Unable to initialized drive mappings", e);
            }
        }

        /// <summary>
        /// Get a normalized fully qaulified path in the form "C:\Program Files\....".<br>
        /// - resolves device paths e.g: \device\harddiskvolume1\...
        /// - expands environment variables contained in the path such es %WinDir%, etc.
        /// </br>
        /// 
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        public static string ResolvePath(string p)
        {
            if (string.IsNullOrEmpty(p))
            {
                return string.Empty;
            }

            if (deviceNameMap is null)
            {
                InitDriveMapping();
            }

            var item = deviceNameMap.FirstOrDefault(d => p.StartsWith(d.Key, StringComparison.InvariantCultureIgnoreCase));
            return item.Key is null ? Environment.ExpandEnvironmentVariables(p) : item.Value + p.Substring(item.Key.Length - 1);
        }

        //FIXME: Clear the cache if there's some change with the drives!
        //WM_DEVICECHANGE
        //DBT_CONFIGCHANGED
        //DBT_DEVICEARRIVAL
        //DBT_DEVICEREMOVECOMPLETE
        //DBT_DEVICEREMOVEPENDING
        //DBT_DEVNODES_CHANGED
        //http://stackoverflow.com/questions/16245706/check-for-device-change-add-remove-events
    }
}
