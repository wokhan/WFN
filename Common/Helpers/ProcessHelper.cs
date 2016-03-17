using System;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.ServiceProcess;
using System.Windows.Media;
using System.Collections.Generic;
using System.Windows.Interop;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Threading.Tasks;
using System.IO;
using System.Reflection;
using System.Management;

namespace Wokhan.WindowsFirewallNotifier.Common.Helpers
{
    public class ProcessHelper
    {
        [Flags]
        public enum ProcessAccessFlags : uint
        {
            All = 0x001F0FFF,
            Terminate = 0x00000001,
            CreateThread = 0x00000002,
            VirtualMemoryOperation = 0x00000008,
            VirtualMemoryRead = 0x00000010,
            VirtualMemoryWrite = 0x00000020,
            DuplicateHandle = 0x00000040,
            CreateProcess = 0x000000080,
            SetQuota = 0x00000100,
            SetInformation = 0x00000200,
            QueryInformation = 0x00000400,
            QueryLimitedInformation = 0x00001000,
            Synchronize = 0x00100000
        }

        public static void ElevateCurrentProcess()
        {
            ProcessStartInfo proc = new ProcessStartInfo();
            proc.UseShellExecute = true;
            proc.WorkingDirectory = Environment.CurrentDirectory;
            proc.FileName = Path.Combine(Assembly.GetCallingAssembly().Location);
            proc.Verb = "runas";

            Process.Start(proc);
        }

        public static string[] GetProcessOwnerWMI(int owningPid, ref Dictionary<int, string[]> previousCache)
        {
            if (previousCache == null)
            {
                using (var searcher = new ManagementObjectSearcher("SELECT ProcessId, Name, ExecutablePath FROM Win32_Process"))
                {
                    using (var results = searcher.Get())
                    {
                        previousCache = results.Cast<ManagementObject>()
                                               .ToDictionary(r => (int)(uint)r["ProcessId"], 
                                                             r => new[] { (string)r["Name"], (string)r["ExecutablePath"] });
                    }
                }
            }

            return previousCache[owningPid];
        }

        private static Dictionary<string, ImageSource> procIconLst = new Dictionary<string, ImageSource>();

        public static string[] GetAllServices(String exeName)
        {
            return GetTasklistResponse("Imagename eq " + exeName);
        }

        public static string[] GetAllServices(int pid)
        {
            return GetTasklistResponse("pid eq " + pid);
        }
        private static string[] GetTasklistResponse(String filterString)
        {
            string resp = ProcessHelper.getProcessResponse("tasklist", "/svc /fi \""+filterString+"\" /nh /fo csv");
            if (resp != null)
            {
                string[] resplit = resp.Split(new char[] { ',' }, 3);
                if (resplit.Length == 3)
                {
                    String svc = resplit[2].Substring(1, resplit[2].Length - 4);

                    //add here more language for tasklist "N/A" output
                    string[] notAvailableStrings = new string[] { "N/A", "Nicht zutreffend" }; //FIXME: This is a terrible way to do this

                    if (notAvailableStrings.Contains(svc))
                    {
                        return null;
                    }
                    else
                    {
                        return svc.Split(',');
                    }
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
        public static void GetService(int pid, string threadid, string path, string protocolStr, string localport, string target, string remoteport, out string[] svc, out string[] svcdsc, out bool unsure)
        {
            string[] svcs = GetAllServices(pid);
            //int protocol = (int)Enum.Parse(typeof(NET_FW_IP_PROTOCOL_), protocolStr);
            svc = new string[0];
            svcdsc = new string[0];
            unsure = false;

            if (svcs == null)
            {
                return;
            }

            LogHelper.Debug("GetService found the following services: " + String.Join(",", svcs));

            //tries to lookup details about connection to localport.
            //@wokhan: how is this supposed to work since connection is blocked by firewall??
            var ret = IPHelper.GetOwner(pid, int.Parse(localport));
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


            LogHelper.Warning("Unable to retrieve the service name, falling back to previous method.");

            // And if it still fails, fall backs to the most ugly way ever I am not able to get rid of :-P
            // Retrieves corresponding existing rules
            int profile = FirewallHelper.GetCurrentProfile();
            var cRules = FirewallHelper.GetMatchingRules(path, protocolStr, target, remoteport, localport, svc, true)
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
            Icon ic = null;
            switch (path)
            {
                case "System":
                    ic = SystemIcons.WinLogo;
                    break;

                case "?error":
                    ic = SystemIcons.Error;
                    break;

                default:
                    if (File.Exists(path))
                    {
                        ic = Icon.ExtractAssociatedIcon(path) ?? (defaultIfNotFound ? SystemIcons.Application : null);
                    }
                    else
                    {
                        ic = SystemIcons.Warning;
                    }
                    break;
            }

            if (ic != null)
            {
                return Imaging.CreateBitmapSourceFromHIcon(ic.Handle, new Int32Rect(0, 0, ic.Width, ic.Height), BitmapSizeOptions.FromEmptyOptions());
            }
            else
            {
                return null;
            }
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
        public static ImageSource GetCachedIcon(string path, bool defaultIfNotFound = false)
        {
            ImageSource icon;
            if (!procIconLst.ContainsKey(path))
            {
                icon = GetIcon(path, defaultIfNotFound);//.ToBitmap().GetThumbnailImage(18, 18, null, IntPtr.Zero);
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
            catch (ArgumentException)
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
            String key = "";
            try
            {
                ret = new Dictionary<string, string>(args.Count / 2);
                for (int i = args.Count % 2; i < args.Count(); i += 2)
                {
                    key = args[i].TrimStart('-');
                    ret.Add(key, args[i + 1]);
                }
            }
            catch (Exception e)
            {
                LogHelper.Error("Unable to parse the parameters: key = "+ key + " argv = " + String.Join(" ", args), e);
            }

            return ret;
        }
    }
}
