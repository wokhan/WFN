using System;
using System.IO;
using System.Security.Principal;
using System.Text;
using System.Windows;
using Wokhan.WindowsFirewallNotifier.Common.Helpers;
using Wokhan.WindowsFirewallNotifier.Common.Properties;
using System.Reflection;
using System.ServiceProcess;
using System.Linq;

namespace Wokhan.WindowsFirewallNotifier.Console.Helpers
{
    public class InstallHelper
    {
        /// <summary>
        /// 
        /// </summary>
        public static bool RemoveProgram(bool disableLogging, Action<bool, string> callback)
        {
            /*if (reallowOutgoing && !FirewallHelper.RestoreWindowsFirewall())
            {
                failureCallback(Resources.MSG_UNINST_UNBLOCK_ERR, Resources.MSG_DLG_ERR_TITLE);
                return false;
            }*/

            if (disableLogging
                && !ProcessHelper.getProcessFeedback(Environment.SystemDirectory + "\\auditpol.exe", "/set /subcategory:{0CCE9226-69AE-11D9-BED3-505054503030} /failure:disable"))
            {
                callback(false, Resources.MSG_UNINST_DISABLE_LOG_ERR);
                return false;
            }

            if (!RemoveTask())
            {
                callback(false, Resources.MSG_UNINST_TASK_ERR);
                return false;
            }

            callback(true, Resources.MSG_UNINST_OK);

            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pass"></param>
        /// <returns></returns>
        public static bool EnableProgram(bool allUsers, Action<bool, string> callback)
        {
            if (IsInstalled())
            {
                RemoveTask();
            }

            if (ProcessHelper.getProcessFeedback(Environment.SystemDirectory + "\\reg.exe", @"ADD HKLM\SYSTEM\CurrentControlSet\Control\Lsa /v SCENoApplyLegacyAuditPolicy /t REG_DWORD /d 1 /f")
                && ProcessHelper.getProcessFeedback(Environment.SystemDirectory + "\\auditpol.exe", "/set /subcategory:{0CCE9226-69AE-11D9-BED3-505054503030} /failure:enable /success:disable"))
            {
                if (FirewallHelper.EnableWindowsFirewall())
                {
                    if (CreateDefaultRules())
                    {
                        if (createTask(allUsers))
                        {
                            callback(true, Resources.MSG_INST_OK);
                        }
                        else
                        {
                            callback(false, Resources.MSG_INST_TASK_ERR);
                            return false;
                        }
                    }
                    else
                    {
                        callback(false, "Unable to create the default rules, please consider reactivating WFN.");
                        return false;
                    }
                }
                else
                {
                    callback(false, Resources.MSG_INST_ENABLE_FW_ERR);
                    return false;
                }
            }
            else
            {
                callback(false, Resources.MSG_INST_ENABLE_LOG_ERR);
                return false;
            }

            return true;
        }

        private static bool CreateDefaultRules()
        {
            bool ret = true;
            var rules = FirewallHelper.GetRules();
            using (ServiceController sc = new ServiceController())
            {
                string rname;

                // Windows 8 or higher
                if (Environment.OSVersion.Version >= new System.Version(6, 2))
                {
                    rname = String.Format(Resources.RULE_NAME_FORMAT, "Windows Applications (auto)");
                    if (rules.All(r => r.Name != rname))
                    {
                        FirewallHelper.CustomRule newRule = new FirewallHelper.CustomRule(rname, Environment.SystemDirectory + "\\wwahost.exe", null, null, (string)null, (int)FirewallHelper.Protocols.ANY, null, null, null, 0, "A");
                        ret = ret && newRule.Apply(false, false);
                    }
                }

                sc.ServiceName = "wuauserv";
                rname = String.Format(Resources.RULE_NAME_FORMAT, sc.DisplayName + " (auto)");
                if (rules.All(r => r.Name != rname + " [R:80,443]"))
                {
                    FirewallHelper.CustomRule newRule = new FirewallHelper.CustomRule(rname, Environment.SystemDirectory + "\\svchost.exe", null, null, "wuauserv", (int)FirewallHelper.Protocols.TCP, null, "80,443", null, 0, "A");
                    ret = ret && newRule.Apply(false, false);
                }

                sc.ServiceName = "bits";
                rname = String.Format(Resources.RULE_NAME_FORMAT, sc.DisplayName + "(auto)");
                if (rules.All(r => r.Name != rname + " [R:80,443]"))
                {
                    FirewallHelper.CustomRule newRule = new FirewallHelper.CustomRule(rname, Environment.SystemDirectory + "\\svchost.exe", null, null, "bits", (int)FirewallHelper.Protocols.TCP, null, "80,443", null, 0, "A");
                    ret = ret && newRule.Apply(false, false);
                }

                sc.ServiceName = "cryptsvc";
                rname = String.Format(Resources.RULE_NAME_FORMAT, sc.DisplayName + "(auto)");
                if (rules.All(r => r.Name != rname + " [R:80]"))
                {
                    FirewallHelper.CustomRule newRule = new FirewallHelper.CustomRule(rname, Environment.SystemDirectory + "\\svchost.exe", null, null, "cryptsvc", (int)FirewallHelper.Protocols.TCP, null, "80", null, 0, "A");
                    ret = ret && newRule.Apply(false, false);
                }

                //sc.ServiceName = "aelookupsvc";
                //rname = String.Format(Resources.RULE_NAME_FORMAT, sc.DisplayName + "(auto)");
                //if (rules.All(r => r.Name != rname + " [R:80]"))
                //{
                //    FirewallHelper.CustomRule newRule = new FirewallHelper.CustomRule(rname, Environment.SystemDirectory + "\\svchost.exe", null, null,"aelookupsvc", (int)NET_FW_IP_PROTOCOL_.NET_FW_IP_PROTOCOL_TCP, null, "80", null, 0, "A");
                //    ret = ret && newRule.Apply(false, false);
                //}
            }

            return ret;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private static bool createTask(bool allUsers)
        {
            string tmpXML = Path.GetTempFileName();
            string newtask;
            using (var taskStr = new StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream("Wokhan.WindowsFirewallNotifier.Console.Resources.TaskTemplate.xml")))
            {
                 newtask = String.Format(taskStr.ReadToEnd(),
                                         allUsers ? "<UserId>NT AUTHORITY\\SYSTEM</UserId>"//"<GroupId>S-1-5-32-545</GroupId>" 
                                                  : "<UserId><![CDATA[" + WindowsIdentity.GetCurrent().Name + "]]></UserId>",
                                         "\"" + Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Notifier.exe") + "\"",
                                         DateTime.Now.ToString("s"));
            }

            File.WriteAllText(tmpXML, newtask, Encoding.Unicode);

            bool ret = ProcessHelper.getProcessFeedback(Environment.SystemDirectory + "\\schtasks.exe", "/IT /Create /TN WindowsFirewallNotifierTask /XML \"" + tmpXML + "\"");

            File.Delete(tmpXML);

            return ret;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private static bool RemoveTask()
        {
            return ProcessHelper.getProcessFeedback(Environment.SystemDirectory + "\\schtasks.exe", "/Delete /TN WindowsFirewallNotifierTask /F");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public static bool IsInstalled()
        {
            return ProcessHelper.getProcessFeedback(Environment.SystemDirectory + "\\schtasks.exe", "/Query /TN WindowsFirewallNotifierTask");
        }

    }
}
