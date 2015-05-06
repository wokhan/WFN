using System;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.ServiceProcess;
using System.Text.RegularExpressions;
using Wokhan.WindowsFirewallNotifier.Common;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using NetFwTypeLib;
using System.Windows.Media;
using System.Collections.Generic;
using System.Windows.Interop;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.IO;


namespace Wokhan.WindowsFirewallNotifier.Common.Helpers
{
    public class ProcessHelper
    {
        private static Dictionary<string, ImageSource> procIconLst = new Dictionary<string, ImageSource>();

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

        private static string[] filteredsvcs = new string[] { };/*// offline
                                                              "appinfo", "gpsvc", "shellhwdetection", "themes", "winmgmt", "sens",
                                                              //already ok
                                                              "dnscache", "nlasvc", "eventsystem", "ssdpsrv", "fdrespub", "ikeext", "EventLog", "dhcp",
                                                              // detected
                                                              "lanmanserver", "browser", "schedule" };
        */
        // private static string[] prioSvcs = new string[] { "wuauserv", "BITS", "aelookupsvc", "CryptSvc", "dnscache", "LanmanWorkstation", "TapiSrv" };

        //private static string servicesParser = @"^\s*(?<protocol>[^\s]+)(?<localip>(?:\[[^\]]*\])|(?:[^(:|\r)]))*:{0}\s+(?<ip>[^:]+):(?<port>(?:\d*|\*))\s+[^\d]*{1}\r\n\s+(?<service>[^\r]+)\r\n\s*\[[^\]]*\]";
        public static void GetService(int pid, string threadid, string protocol, string port, string localport, out string[] svc, out string[] svcdsc, out bool unsure)
        {
            GetService(pid, threadid, (NetFwTypeLib.NET_FW_IP_PROTOCOL_)Enum.Parse(typeof(NetFwTypeLib.NET_FW_IP_PROTOCOL_), protocol), port, localport, out svc, out svcdsc, out unsure);
        }

        public static void GetService(int pid, string threadid, NetFwTypeLib.NET_FW_IP_PROTOCOL_ protocol, string port, string localport, out string[] svc, out string[] svcdsc, out bool unsure)
        {
            string[] svcs = GetAllServices(pid);

            svc = new string[0];
            svcdsc = new string[0];
            unsure = false;

            if (svcs == null)
            {
                return;
            }

            LogHelper.Debug("GetService found the following services: " + String.Join(",", svcs));

            var ret = BaseHelper.GetOwner(protocol, int.Parse(localport));
            if (ret != null && !String.IsNullOrEmpty(ret.ModuleName))
            {
                // Returns the owner only if it's indeed a service (hence contained in the previously retrieved list)
                //if (svcs.Contains(ret.ModuleName))
                svc = new[] { ret.ModuleName };
                svcdsc = new[] { getServiceDesc(ret.ModuleName) };

                return;
            }

            // If it fails, tries to retrieve the module name from the calling thread id
            // /!\ Unfortunately, retrieving the proper thread ID requires another log to be enabled and parsed.
            // I don't want to get things too complicated since not that many users actually bother about services.
            /*var p = Process.GetProcessById(pid);
            int threadidint;
            if (int.TryParse(threadid, out threadidint))
            {
                var thread = p.Threads.Cast<ProcessThread>().SingleOrDefault(t => t.Id == threadidint);
                if (thread == null)
                {
                    LogHelper.Debug("The thread " + threadidint + " has not been found for PID " + pid);
                }
                else
                {
                    var thaddr = thread.StartAddress.ToInt64();
                    var module = p.Modules.Cast<ProcessModule>().FirstOrDefault(m => thaddr >= (m.BaseAddress.ToInt64() + m.ModuleMemorySize));
                    if (module == null)
                    {
                        LogHelper.Debug("The thread has been found, but no module matches.");
                    }
                    else
                    {
                        LogHelper.Debug("The thread has been found for module " + module.ModuleName);

                        svc = module.ModuleName;
                        svcdsc = getServiceDesc(svc);

                        if (String.IsNullOrEmpty(svcdsc))
                        {
                            LogHelper.Debug("But no service description matches...");
                            
                            svc = null;
                            svcdsc = null;
                        }

                        return;
                    }
                }
            }
            else
            {
                LogHelper.Error("Unable to parse the Thread ID / value = " + threadid, null);
            }*/


            LogHelper.Error("Unable to retrieve the service name, falling back to previous method.", null);

            // And if it still fails, fall backs to the most ugly way ever I am not able to get rid of :-P
            // Retrieves corresponding existing rules
            int profile = FirewallHelper.GetCurrentProfile();
            var cRules = FirewallHelper.GetRules()
                                       .Where(r => r.Enabled &&
                                                   (((r.Profiles & profile) != 0) || ((r.Profiles & (int)NET_FW_PROFILE_TYPE2_.NET_FW_PROFILE2_ALL) != 0)) &&
                                                   r.Action == NetFwTypeLib.NET_FW_ACTION_.NET_FW_ACTION_ALLOW &&
                                                   !String.IsNullOrEmpty(r.serviceName) && svcs.Contains(r.serviceName, StringComparer.CurrentCultureIgnoreCase) &&
                                                   (r.Protocol == (int)NetFwTypeLib.NET_FW_IP_PROTOCOL_.NET_FW_IP_PROTOCOL_ANY || r.Protocol == (int)protocol) &&
                                                   (String.IsNullOrEmpty(r.RemotePorts) || r.RemotePorts == "*" || r.RemotePorts.Split(',').Contains(port)) &&
                                                   (String.IsNullOrEmpty(r.LocalPorts) || r.LocalPorts == "*" || r.LocalPorts.Split(',').Contains(localport)))
                                       .Select(r => r.serviceName)
                                       .Distinct()
                                       .ToList();

            // Trying to guess the corresponding service if not found with the previous method and if not already filtered
            svcs = svcs//.Except(filteredsvcs, StringComparer.CurrentCultureIgnoreCase)
                       .Except(cRules, StringComparer.CurrentCultureIgnoreCase)
                       .ToArray();

            LogHelper.Debug("Excluding " + String.Join(",", cRules) + " // Remains " + String.Join(",", svcs));

            if (svcs.Length > 0)
            {
                unsure = true;
                svc = svcs;
                svcdsc = svcs.Select(s => getServiceDesc(s)).ToArray();
            }

            return;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static ImageSource GetIcon(string path, bool defaultIfNotFound = false)
        {
            Icon ic;
            if (File.Exists(path))
            {
                ic = (path != "System" ? Icon.ExtractAssociatedIcon(path) : SystemIcons.WinLogo);
            }
            else
            {
                ic = SystemIcons.Application;
            }
            return Imaging.CreateBitmapSourceFromHIcon(ic.Handle, new Int32Rect(0, 0, ic.Width, ic.Height), BitmapSizeOptions.FromEmptyOptions());
        }


        public static async Task<ImageSource> GetIconAsync(string path, bool defaultIfNotFound = false)
        {
            return await Task<ImageSource>.Run(() => GetIcon(path, defaultIfNotFound));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static ImageSource GetCachedIcon(string path)
        {
            ImageSource icon;
            if (!procIconLst.ContainsKey(path))
            {
                icon = GetIcon(path, true);//.ToBitmap().GetThumbnailImage(18, 18, null, IntPtr.Zero);
                procIconLst.Add(path, icon);
            }
            else
            {
                icon = procIconLst[path];
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

        /// <summary>
        ///  Turns command line parameters into a dictionary to ease values retrieval
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public static Dictionary<string, string> ParseParameters(IList<string> args)
        {
            Dictionary<string, string> ret = null;
            try
            {
                ret = new Dictionary<string, string>(args.Count / 2);
                for (int i = 0; i < args.Count(); i += 2)
                {
                    ret.Add(args[i].TrimStart('-'), args[i + 1]);
                }
            }
            catch (Exception e)
            {
                LogHelper.Error("Unable to parse the parameters: argv = " + String.Join(" ", args), e);
            }

            return ret;
        }

    }
}
