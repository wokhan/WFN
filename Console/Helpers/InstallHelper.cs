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
using Wokhan.WindowsFirewallNotifier.Common;
using static Wokhan.WindowsFirewallNotifier.Common.Helpers.FirewallHelper.CustomRule;

namespace Wokhan.WindowsFirewallNotifier.Console.Helpers
{
    public class InstallHelper
    {
        /// <summary>
        /// 
        /// </summary>
        public static bool UninstallCheck(bool disableAuditPolicy, bool removeNotifierTask, Action<bool, string> callback)
        {
            /*if (reallowOutgoing && !FirewallHelper.RestoreWindowsFirewall())
            {
                failureCallback(Resources.MSG_UNINST_UNBLOCK_ERR, Resources.MSG_DLG_ERR_TITLE);
                return false;
            }*/

            if (disableAuditPolicy
                && !SetAuditPolConnection(enableSuccess: false, enableFailure: false))
            {
                callback(false, Resources.MSG_UNINST_DISABLE_LOG_ERR);
                return false;
            }

            if (removeNotifierTask && !RemoveTask())
            {
                callback(false, Resources.MSG_UNINST_TASK_ERR);
                return false;
            }

            if (removeNotifierTask) callback(true, Resources.MSG_UNINST_OK);

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
                RemoveTask();  // will be re-created below
            }

            if (ProcessHelper.getProcessFeedback(Environment.SystemDirectory + "\\reg.exe", @"ADD HKLM\SYSTEM\CurrentControlSet\Control\Lsa /v SCENoApplyLegacyAuditPolicy /t REG_DWORD /d 1 /f")
                && SetAuditPolConnection(enableSuccess: Settings.Default.AuditPolEnableSuccessEvent, enableFailure: true)
                )
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

        public static bool SetAuditPolConnection(bool enableSuccess, bool enableFailure)
        {
            string successOption = enableSuccess ? "/success:enable" : "/success:disable";
            string failureOption = enableFailure ? "/failure:enable" : "/failure:disable";
            return ProcessHelper.getProcessFeedback(Environment.SystemDirectory + "\\auditpol.exe", "/set /subcategory:{0CCE9226-69AE-11D9-BED3-505054503030} " + successOption + " " + failureOption);
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
                        FirewallHelper.CustomRule newRule = new FirewallHelper.CustomRule(rname, Environment.SystemDirectory + "\\wwahost.exe", null, null, (string)null, (int)FirewallHelper.Protocols.ANY, null, null, null, FirewallHelper.GetGlobalProfile(), CustomRuleAction.A);
                        ret = ret && newRule.Apply(false);
                    }
                }

                sc.ServiceName = "wuauserv";
                rname = String.Format(Resources.RULE_NAME_FORMAT, sc.DisplayName + " (auto)");
                if (rules.All(r => r.Name != rname + " [R:80,443]"))
                {
                    FirewallHelper.CustomRule newRule = new FirewallHelper.CustomRule(rname, Environment.SystemDirectory + "\\svchost.exe", null, null, "wuauserv", (int)FirewallHelper.Protocols.TCP, null, "80,443", null, FirewallHelper.GetGlobalProfile(), CustomRuleAction.A);
                    ret = ret && newRule.Apply(false);
                }

                sc.ServiceName = "bits";
                rname = String.Format(Resources.RULE_NAME_FORMAT, sc.DisplayName + "(auto)");
                if (rules.All(r => r.Name != rname + " [R:80,443]"))
                {
                    FirewallHelper.CustomRule newRule = new FirewallHelper.CustomRule(rname, Environment.SystemDirectory + "\\svchost.exe", null, null, "bits", (int)FirewallHelper.Protocols.TCP, null, "80,443", null, FirewallHelper.GetGlobalProfile(), CustomRuleAction.A);
                    ret = ret && newRule.Apply(false);
                }

                sc.ServiceName = "cryptsvc";
                rname = String.Format(Resources.RULE_NAME_FORMAT, sc.DisplayName + "(auto)");
                if (rules.All(r => r.Name != rname + " [R:80]"))
                {
                    FirewallHelper.CustomRule newRule = new FirewallHelper.CustomRule(rname, Environment.SystemDirectory + "\\svchost.exe", null, null, "cryptsvc", (int)FirewallHelper.Protocols.TCP, null, "80", null, FirewallHelper.GetGlobalProfile(), CustomRuleAction.A);
                    ret = ret && newRule.Apply(false);
                }

                //sc.ServiceName = "aelookupsvc";
                //rname = String.Format(Resources.RULE_NAME_FORMAT, sc.DisplayName + "(auto)");
                //if (rules.All(r => r.Name != rname + " [R:80]"))
                //{
                //    FirewallHelper.CustomRule newRule = new FirewallHelper.CustomRule(rname, Environment.SystemDirectory + "\\svchost.exe", null, null,"aelookupsvc", (int)NET_FW_IP_PROTOCOL_.NET_FW_IP_PROTOCOL_TCP, null, "80", null, FirewallHelper.GetGlobalProfile(), "A");
                //    ret = ret && newRule.Apply(false);
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
                // TODO: Unclear why SYSTEM was required in case of all users - however task scheduler does not properly start notifier with this
                //newtask = String.Format(taskStr.ReadToEnd(),
                //                        allUsers ? "<UserId>NT AUTHORITY\\SYSTEM</UserId>"//"<GroupId>S-1-5-32-545</GroupId>" 
                //                                 : "<UserId><![CDATA[" + WindowsIdentity.GetCurrent().Name + "]]></UserId>",
                //                        "\"" + Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Notifier.exe") + "\"",
                //                        DateTime.Now.ToString("s"));

                string principle = "<UserId><![CDATA[" + WindowsIdentity.GetCurrent().Name + "]]></UserId>";
                string command = "\"" + Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Notifier.exe") + "\"";
                string arguments = "-minimized"; // TODO: To be implemented
                string dateTime = DateTime.Now.ToString("s");
                newtask = String.Format(taskStr.ReadToEnd(),
                                        principle,
                                        command,
                                        arguments,
                                        dateTime);
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
