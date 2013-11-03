using System;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.ServiceProcess;
using System.Text.RegularExpressions;
using WindowsFirewallNotifier.Properties;
using System.Net.NetworkInformation;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;


namespace WindowsFirewallNotifier
{
    public class ProcessHelper
    {
        private static ImageList procIconLst = new ImageList() { ImageSize = new Size(18, 18) };

        public static string[] GetAllServices(int pid)
        {
            string resp = ProcessHelper.getProcessResponse("tasklist", "/svc /fi \"pid eq " + pid + "\" /nh /fo csv");
            if (resp != null)
            {
                string[] resplit = resp.Split(new char[] { ',' }, 3);
                if (resplit.Length == 3)
                {
                    return (resplit[2] == "\"N/A\"\r\n" ? null : resplit[2].Substring(1, resplit[2].Length - 4).Split(','));
                }
            }

            return null;
        }

        private static string[] filteredsvcs = new string[] { // offline
                                                              "appinfo", "gpsvc", "shellhwdetection", "themes", "winmgmt", "sens",
                                                              //already ok
                                                              "dnscache", "nlasvc", "eventsystem", "ssdpsrv", "fdrespub", "ikeext", "EventLog", "dhcp",
                                                              // detected
                                                              "lanmanserver", "browser", "schedule" };

        // private static string[] prioSvcs = new string[] { "wuauserv", "BITS", "aelookupsvc", "CryptSvc", "dnscache", "LanmanWorkstation", "TapiSrv" };

        //private static string servicesParser = @"^\s*(?<protocol>[^\s]+)(?<localip>(?:\[[^\]]*\])|(?:[^(:|\r)]))*:{0}\s+(?<ip>[^:]+):(?<port>(?:\d*|\*))\s+[^\d]*{1}\r\n\s+(?<service>[^\r]+)\r\n\s*\[[^\]]*\]";
        public static void GetService(int pid, NetFwTypeLib.NET_FW_IP_PROTOCOL_ protocol, string port, string localport, out string svc, out string svcdsc)
        {
            string[] svcs = GetAllServices(pid);

            svc = null;
            svcdsc = null;

            if (svcs == null)
            {
                return;
            }

            var ret = IpHlpApiHelper.GetOwner(protocol, int.Parse(localport));
            if (ret != null && !String.IsNullOrEmpty(ret.ModuleName))
            {
                // Returns the owner only if it's indeed a service (hence contained in the previously retrieved list)
                //if (svcs.Contains(ret.ModuleName))
                svc = ret.ModuleName;
                svcdsc = getServiceDesc(svc);

                if (String.IsNullOrEmpty(svcdsc))
                {
                    svc = null;
                    svcdsc = null;
                }

                return;
            }

            // Retrieves corresponding existing rules
            var cRules = FirewallHelper.GetRules()
                                       .Where(r => r.Enabled &&
                                                   r.Action == NetFwTypeLib.NET_FW_ACTION_.NET_FW_ACTION_ALLOW &&
                                                   !String.IsNullOrEmpty(r.serviceName) && svcs.Contains(r.serviceName, StringComparer.CurrentCultureIgnoreCase) &&
                                                   (r.Protocol == (int)NetFwTypeLib.NET_FW_IP_PROTOCOL_.NET_FW_IP_PROTOCOL_ANY || r.Protocol == (int)protocol) &&
                                                   (String.IsNullOrEmpty(r.RemotePorts) || r.RemotePorts == "*" || r.RemotePorts.Split(',').Contains(port)) &&
                                                   (String.IsNullOrEmpty(r.LocalPorts) || r.LocalPorts == "*" || r.LocalPorts.Split(',').Contains(localport)))
                                       .Select(r => r.serviceName);

            // Trying to guess the corresponding service if not found with the previous method and if not already filtered
            svcs = svcs.Except(filteredsvcs, StringComparer.CurrentCultureIgnoreCase)
                       .Except(cRules, StringComparer.CurrentCultureIgnoreCase)
                       .ToArray();

            if (svcs.Length == 1)
            {
                svc = svcs[0];
                svcdsc = getServiceDesc(svc);
            }
            else if (svcs.Length > 1)
            {
                svc = "* " + String.Join(", ", svcs);
                svcdsc = String.Empty;
            }

            return;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static Icon GetIcon(string path)
        {
            Icon icon = Resources.ICON_SHIELD;

            try
            {
                if (path != "System")
                {
                    icon = Icon.ExtractAssociatedIcon(path);
                }
            }
            catch { }

            return icon;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static Image GetIcon(string path, bool cache)
        {
            Image icon;
            if (!procIconLst.Images.ContainsKey(path))
            {
                icon = GetIcon(path).ToBitmap().GetThumbnailImage(18, 18, null, IntPtr.Zero);
                procIconLst.Images.Add(path, icon);
            }
            else
            {
                icon = procIconLst.Images[path];
            }

            return icon;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="service"></param>
        /// <returns></returns>
        private static string getServiceDesc(string service)
        {
            string ret;
            try
            {
                using (ServiceController sc = new ServiceController(service))
                {
                    ret = sc.DisplayName;
                }

                return ret;
            }
            catch
            {
                return String.Empty;
            }
        }

        public static bool getProcessFeedback(string cmd, string args)
        {
            return getProcessFeedback(cmd, args, false, false);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="p"></param>
        /// <param name="p_2"></param>
        /// <returns></returns>
        public static bool getProcessFeedback(string cmd, string args, bool runas, bool dontwait)
        {
            try
            {
                ProcessStartInfo psiTaskTest = new ProcessStartInfo(cmd, args);
                psiTaskTest.CreateNoWindow = true;
                if (runas)
                {
                    psiTaskTest.Verb = "runas";
                }
                else
                {
                    psiTaskTest.UseShellExecute = false;
                }

                Process procTaskTest = Process.Start(psiTaskTest);
                if (dontwait)
                {
                    procTaskTest.WaitForExit(100);
                    if (procTaskTest.HasExited)
                    {
                        return procTaskTest.ExitCode == 0;
                    }
                    else
                    {
                        return true;
                    }
                }
                else
                {
                    procTaskTest.WaitForExit();
                }

                return (procTaskTest.ExitCode == 0);
            }
            catch
            {
                return false;
            }
        }

        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="cmd"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public static string getProcessResponse(string cmd, string args)
        {
            ProcessStartInfo psiTaskTest = new ProcessStartInfo(cmd, args);
            psiTaskTest.CreateNoWindow = true;
            psiTaskTest.UseShellExecute = false;
            psiTaskTest.RedirectStandardOutput = true;

            Process procTaskTest = Process.Start(psiTaskTest);

            string ret = procTaskTest.StandardOutput.ReadToEnd();
            procTaskTest.Close();
            //procTaskTest.WaitForExit();

            //Application.DoEvents();

            return ret;
        }
    }
}
