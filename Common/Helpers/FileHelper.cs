using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace Wokhan.WindowsFirewallNotifier.Common.Helpers
{
    public class FileHelper
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern uint QueryDosDevice(string lpDeviceName, StringBuilder lpTargetPath, uint ucchMax);

        private const int MAX_PATH = 260;

        private static Dictionary<string, string> deviceNameMap = null;

        /// <summary>
        /// 
        /// </summary>
        private static void initDriveMapping()
        {
            try
            {
                string[] drives = Directory.GetLogicalDrives();
                deviceNameMap = new Dictionary<string, string>(drives.Length);
                StringBuilder sb = new StringBuilder(MAX_PATH + 1);
                string trimmedDrive;

                foreach (string drive in drives)
                {
                    trimmedDrive = drive.TrimEnd('\\');
                    if (QueryDosDevice(trimmedDrive, sb, (uint)sb.Capacity) == 0)
                    {
                        throw new Win32Exception(Marshal.GetLastWin32Error(), "Call to QueryDosDevice failed!");
                    }
                    deviceNameMap.Add(sb.ToString().ToLower() + "\\", trimmedDrive);
                }
            }
            catch (Exception e)
            {
                LogHelper.Error("Unable to initialized drive mappings", e);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        public static string GetFriendlyPath(string p)
        {
            if (deviceNameMap == null)
            {
                initDriveMapping();
            }

            KeyValuePair<string, string> item = deviceNameMap.FirstOrDefault(d => p.StartsWith(d.Key, StringComparison.InvariantCultureIgnoreCase));
            return (item.Key == null ? p : item.Value + p.Substring(item.Key.Length - 1));
        }

        //FIXME: Clear the cache if there's some change with the drives!
    }
}
