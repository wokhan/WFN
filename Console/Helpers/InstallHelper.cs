using System;
using System.IO;
using System.Security.Principal;
using System.Text;
using Wokhan.WindowsFirewallNotifier.Common.Helpers;
using Wokhan.WindowsFirewallNotifier.Common.Properties;
using System.Reflection;
using System.ServiceProcess;
using System.Linq;
using static Wokhan.WindowsFirewallNotifier.Common.Net.WFP.Rules.CustomRule;
using Wokhan.WindowsFirewallNotifier.Common.Net.WFP;
using Wokhan.WindowsFirewallNotifier.Common.Net.WFP.Rules;
using Wokhan.WindowsFirewallNotifier.Common.Config;

namespace Wokhan.WindowsFirewallNotifier.Console.Helpers
{
    public static class InstallHelper
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

            if (ProcessHelper.GetProcessFeedback(Environment.SystemDirectory + "\\reg.exe", @"ADD HKLM\SYSTEM\CurrentControlSet\Control\Lsa /v SCENoApplyLegacyAuditPolicy /t REG_DWORD /d 1 /f")
                && SetAuditPolConnection(enableSuccess: Settings.Default.AuditPolEnableSuccessEvent, enableFailure: true)
                )
            {
                if (FirewallHelper.EnableWindowsFirewall())
                {
                    if (CreateDefaultRules())
                    {
                        if (CreateTask(allUsers))
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
            return ProcessHelper.GetProcessFeedback(Environment.SystemDirectory + "\\auditpol.exe", "/set /subcategory:{0CCE9226-69AE-11D9-BED3-505054503030} " + successOption + " " + failureOption);
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
                        CustomRule newRule = new CustomRule(rname, Environment.SystemDirectory + "\\wwahost.exe", null, null, (string)null, Protocol.ANY, null, null, null, FirewallHelper.GetGlobalProfile(), CustomRuleAction.A);
                        ret = ret && FirewallHelper.AddRule(newRule.GetPreparedRule(false));
                    }
                }

                sc.ServiceName = "wuauserv";
                rname = String.Format(Resources.RULE_NAME_FORMAT, sc.DisplayName + " (auto)");
                if (rules.All(r => r.Name != rname + " [R:80,443]"))
                {
                    CustomRule newRule = new CustomRule(rname, Environment.SystemDirectory + "\\svchost.exe", null, null, "wuauserv", Protocol.TCP, null, "80,443", null, FirewallHelper.GetGlobalProfile(), CustomRuleAction.A);
                    ret = ret && FirewallHelper.AddRule(newRule.GetPreparedRule(false));
                }

                sc.ServiceName = "bits";
                rname = String.Format(Resources.RULE_NAME_FORMAT, sc.DisplayName + "(auto)");
                if (rules.All(r => r.Name != rname + " [R:80,443]"))
                {
                    CustomRule newRule = new CustomRule(rname, Environment.SystemDirectory + "\\svchost.exe", null, null, "bits", Protocol.TCP, null, "80,443", null, FirewallHelper.GetGlobalProfile(), CustomRuleAction.A);
                    ret = ret && FirewallHelper.AddRule(newRule.GetPreparedRule(false));
                }

                sc.ServiceName = "cryptsvc";
                rname = String.Format(Resources.RULE_NAME_FORMAT, sc.DisplayName + "(auto)");
                if (rules.All(r => r.Name != rname + " [R:80]"))
                {
                    CustomRule newRule = new CustomRule(rname, Environment.SystemDirectory + "\\svchost.exe", null, null, "cryptsvc", Protocol.TCP, null, "80", null, FirewallHelper.GetGlobalProfile(), CustomRuleAction.A);
                    ret = ret && FirewallHelper.AddRule(newRule.GetPreparedRule(false));
                }

                //sc.ServiceName = "aelookupsvc";
                //rname = String.Format(Resources.RULE_NAME_FORMAT, sc.DisplayName + "(auto)");
                //if (rules.All(r => r.Name != rname + " [R:80]"))
                //{
                //    CustomRule newRule = new CustomRule(rname, Environment.SystemDirectory + "\\svchost.exe", null, null,"aelookupsvc", (int)NET_FW_IP_PROTOCOL_.NET_FW_IP_PROTOCOL_TCP, null, "80", null, FirewallHelper.GetGlobalProfile(), "A");
                //    ret = ret && newRule.Apply(false);
                //}
            }

            return ret;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private static bool CreateTask(bool allUsers)
        {
            string tmpXML = Path.GetTempFileName();
            string newtask;
            using (var taskStr = new StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream("Wokhan.WindowsFirewallNotifier.Console.Resources.TaskTemplate.xml")))
            {
                // TODO: Unclear why SYSTEM was required in case of all users - however task scheduler does not properly start notifier with this
                
                // TODO: !!!! (@wokhan): I used SYSTEM because it has to be able to trigger the notification for whatever user is actually connected. Adding only the current user won't work for others and can be really annoying (connection would be block, the notification will be triggered for another user (maybe connected but not on the active session)).

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

            bool ret = ProcessHelper.GetProcessFeedback(Environment.SystemDirectory + "\\schtasks.exe", "/IT /Create /TN WindowsFirewallNotifierTask /XML \"" + tmpXML + "\"");

            File.Delete(tmpXML);

            return ret;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private static bool RemoveTask()
        {
            return ProcessHelper.GetProcessFeedback(Environment.SystemDirectory + "\\schtasks.exe", "/Delete /TN WindowsFirewallNotifierTask /F");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public static bool IsInstalled()
        {
            return ProcessHelper.GetProcessFeedback(Environment.SystemDirectory + "\\schtasks.exe", "/Query /TN WindowsFirewallNotifierTask");
        }

    }
}
