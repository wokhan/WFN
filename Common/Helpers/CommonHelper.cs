using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
//using Windows.ApplicationModel.Resources.Core;

namespace Wokhan.WindowsFirewallNotifier.Common.Helpers
{
    public class CommonHelper
    {
        [DllImport("Wtsapi32.dll", SetLastError = true)]
        public static extern bool WTSQueryUserToken(uint SessionId, ref IntPtr phToken);

        [DllImport("kernel32.dll", SetLastError = true, EntryPoint = "WTSGetActiveConsoleSessionId")]
        public static extern uint WTSGetActiveConsoleSessionId();

        [DllImport("kernel32.dll")]
        static extern uint QueryDosDevice(string lpDeviceName, StringBuilder lpTargetPath, int ucchMax);

        private static Dictionary<string, string> deviceNameMap = null;

        public static void OverrideSettingsFile(string fileName)
        {
            AppDomain.CurrentDomain.SetupInformation.ConfigurationFile = fileName;
        }

        /// <summary>
        /// 
        /// </summary>
        private static void initDriveMapping()
        {
            try
            {
                string[] drives = Directory.GetLogicalDrives();
                deviceNameMap = new Dictionary<string, string>(drives.Length);
                StringBuilder sb = new StringBuilder(260);
                string trimmedDrive;

                foreach (string drive in drives)
                {
                    trimmedDrive = drive.TrimEnd('\\');
                    QueryDosDevice(trimmedDrive, sb, sb.Capacity);
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

        [DllImport("shlwapi.dll", BestFitMapping = false, CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = false, ThrowOnUnmappableChar = true)]
        public static extern int SHLoadIndirectString(string pszSource, StringBuilder pszOutBuf, int cchOutBuf, IntPtr ppvReserved);

        public static string GetMSResourceString(string src)
        {
            if (src != null && src.StartsWith("@"))
            {
                StringBuilder sb = new StringBuilder(1024);
                if (0 == SHLoadIndirectString(Environment.ExpandEnvironmentVariables(src), sb, sb.Capacity, IntPtr.Zero))
                {
                    src = sb.ToString();
                }

            }
            return src;

        }


        public static string FormatBytes(double size, string suffix = null)
        {
            if (size >= 1073741824)
            {
                return String.Format("{0:##.##}GB{1}", size / 1073741824.0, suffix);
            }
            else if (size >= 1048576)
            {
                return String.Format("{0:##.##}MB{1}", size / 1048576.0, suffix);
            }
            else if (size >= 1024)
            {
                return String.Format("{0:##.##}KB{1}", size / 1024.0, suffix);
            }

            return String.Format("{0:#0.##}B{1}", size, suffix);
        }
    }
}