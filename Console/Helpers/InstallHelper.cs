using System;
using System.IO;
using System.Security.Principal;
using System.Text;
using Wokhan.WindowsFirewallNotifier.Common.Properties;
using System.Reflection;
using System.ServiceProcess;
using System.Linq;
using System.Diagnostics.CodeAnalysis;
using Wokhan.WindowsFirewallNotifier.Common.Config;
using Wokhan.WindowsFirewallNotifier.Common.Net.WFP;
using Wokhan.WindowsFirewallNotifier.Common.Net.WFP.Rules;
using Wokhan.WindowsFirewallNotifier.Common.Logging;
using Wokhan.WindowsFirewallNotifier.Common.Processes;

namespace Wokhan.WindowsFirewallNotifier.Console.Helpers
{
    public static class InstallHelper
    {

        internal const string NOTIFIER_TASK_NAME = "WindowsFirewallNotifierTask";

        /// <summary>
        /// Check install after save.
        /// </summary>
        public static bool InstallCheck(Func<Func<bool>, string, string, bool> checkResult)
        {
            LogHelper.Debug(nameof(InstallCheck));
            if (checkResult is null)
            {
                throw new ArgumentNullException(nameof(checkResult));
            }

            if (!checkResult(() => SetAuditPolConnection(enableSuccess: Settings.Default.AuditPolEnableSuccessEvent, enableFailure: true)
                , "Security log audit policy enabled."
                , Resources.MSG_INST_ENABLE_LOG_ERR)) return false;

            if (Settings.Default.StartNotifierAfterLogin)
            {
                if (!checkResult(CreateNotifierTask, $"{Resources.MSG_INST_OK} Notifier has been enabled and will start on user's login.", Resources.MSG_INST_TASK_ERR)) return false;
            }
            else
            {
                if (!checkResult(RemoveNotifierTask, $"{Resources.MSG_UNINST_OK} Notifier has been disabled. You will not get any notification!", Resources.MSG_UNINST_TASK_ERR)) return false;
            }

            Settings.Default.Save();

            return true;
        }


        /// <summary>
        /// Install and setup.
        /// </summary>
        /// <param name="checkResult"></param>
        /// <returns></returns>
        public static bool Install([param: NotNull] Func<Func<bool>, string, string, bool> checkResult)
        {
            if (checkResult is null)
            {
                throw new ArgumentNullException(nameof(checkResult));
            }

            var success = checkResult(RemoveNotifierTask, "[1/5] Removed Notifier task (or it didn't exist) so it can be recreated.", "[2/5] Unable to remove Notifier task. Stopping now.")  // will be re-created below (to ensure it's up to date and not created twice)
                       && (!Settings.Default.StartNotifierAfterLogin || !checkResult(CreateNotifierTask, "[2/5] Created Notifier task so it will start after next windows login", "[2/5] Unable to create Notifier task. Stopping now."))
                       && checkResult(DisableLegacyPolicy, "[3/5] Enabled SCENoApplyLegacyAuditPolicy in Registry.", "[3/5] Unable to override SCENoApplyLegacyAuditPolicy in Registry. Stopping now.")
                       && checkResult(EnableEventLogging, "[4/5] Enabled event logging.", "[4/5] Unable to enable event logging. Stopping now. Notifier is enabled anyway but will not be able to notify you!")
                       //&& checkResult(FirewallHelper.EnableWindowsFirewall, "Enabled Windows Firewall", Resources.MSG_INST_ENABLE_FW_ERR)
                       && checkResult(CreateDefaultRules, "[5/5] Created default firewall rules for common Windows services", "[5/5] Unable to create default firewall rules for common Windows services. You will get notified for those ones as this is not critical.");
            
            if (success)
            {
                Settings.Default.IsInstalled = true;
                Settings.Default.Save();
            }

            return success;
        }

        private static bool EnableEventLogging()
        {
            return SetAuditPolConnection(enableSuccess: Settings.Default.AuditPolEnableSuccessEvent, enableFailure: true);
        }

        private static bool DisableLegacyPolicy()
        {
            return ProcessHelper.GetProcessFeedback(Environment.SystemDirectory + "\\reg.exe", @"ADD HKLM\SYSTEM\CurrentControlSet\Control\Lsa /v SCENoApplyLegacyAuditPolicy /t REG_DWORD /d 1 /f");
        }

        public static bool Uninstall([param: NotNull] Func<Func<bool>, string, string, bool> checkResult)
        {
            return checkResult(RemoveNotifierTask, "Disabled automatic startup", "Unable to disable automatic startup");
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
                        CustomRule newRule = new CustomRule(rname, Environment.SystemDirectory + "\\wwahost.exe", null, null, (string)null, Protocol.ANY, null, null, null, FirewallHelper.GetGlobalProfile(), CustomRule.CustomRuleAction.Allow);
                        ret = ret && FirewallHelper.AddRule(newRule.GetPreparedRule(false));
                    }
                }

                sc.ServiceName = "wuauserv";
                rname = String.Format(Resources.RULE_NAME_FORMAT, sc.DisplayName + " (auto)");
                if (rules.All(r => r.Name != rname + " [R:80,443]"))
                {
                    CustomRule newRule = new CustomRule(rname, Environment.SystemDirectory + "\\svchost.exe", null, null, "wuauserv", Protocol.TCP, null, "80,443", null, FirewallHelper.GetGlobalProfile(), CustomRule.CustomRuleAction.Allow);
                    ret = ret && FirewallHelper.AddRule(newRule.GetPreparedRule(false));
                }

                sc.ServiceName = "bits";
                rname = String.Format(Resources.RULE_NAME_FORMAT, sc.DisplayName + "(auto)");
                if (rules.All(r => r.Name != rname + " [R:80,443]"))
                {
                    CustomRule newRule = new CustomRule(rname, Environment.SystemDirectory + "\\svchost.exe", null, null, "bits", Protocol.TCP, null, "80,443", null, FirewallHelper.GetGlobalProfile(), CustomRule.CustomRuleAction.Allow);
                    ret = ret && FirewallHelper.AddRule(newRule.GetPreparedRule(false));
                }

                sc.ServiceName = "cryptsvc";
                rname = String.Format(Resources.RULE_NAME_FORMAT, sc.DisplayName + "(auto)");
                if (rules.All(r => r.Name != rname + " [R:80]"))
                {
                    CustomRule newRule = new CustomRule(rname, Environment.SystemDirectory + "\\svchost.exe", null, null, "cryptsvc", Protocol.TCP, null, "80", null, FirewallHelper.GetGlobalProfile(), CustomRule.CustomRuleAction.Allow);
                    ret = ret && FirewallHelper.AddRule(newRule.GetPreparedRule(false));
                }

                //sc.ServiceName = "aelookupsvc";
                //rname = String.Format(Resources.RULE_NAME_FORMAT, sc.DisplayName + "(auto)");
                //if (rules.All(r => r.Name != rname + " [R:80]"))
                //{
                //    CustomRule newRule = new CustomRule(rname, Environment.SystemDirectory + "\\svchost.exe", null, null,"aelookupsvc", (int)NET_FW_IP_PROTOCOL_.NET_FW_IP_PROTOCOL_TCP, null, "80", null, FirewallHelper.GetGlobalProfile(), "A");
                //    ret = ret && FirewallHelper.AddRule(newRule.GetPreparedRule(false));
                //}
            }

            return ret;
        }


        /// <summary>
        /// Create the notifier task.
        /// </summary>
        /// <returns></returns>
        public static bool CreateNotifierTask()
        {
            LogHelper.Debug("CreateNotifierTask");
            string tmpXML = Path.GetTempFileName();
            string newtask;
            using (var taskStr = new StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream("Wokhan.WindowsFirewallNotifier.Console.Resources.TaskTemplate.xml")))
            {
                string principle = "<UserId><![CDATA[" + WindowsIdentity.GetCurrent().Name + "]]></UserId>";
                string command = "\"" + Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Notifier.exe") + "\"";
                string arguments = Settings.Default.StartNotifierMinimized ? "-minimized " + Settings.Default.StartNotifierMinimized : ""; // TODO: To be implemented
                string dateTime = DateTime.Now.ToString("s");
                newtask = String.Format(taskStr.ReadToEnd(), principle, command, arguments, dateTime);
            }

            File.WriteAllText(tmpXML, newtask, Encoding.Unicode);

            bool ret = ProcessHelper.GetProcessFeedback(Environment.SystemDirectory + "\\schtasks.exe", $"/IT /Create /TN {NOTIFIER_TASK_NAME} /XML \"" + tmpXML + "\"");

            File.Delete(tmpXML);

            ret = RunNotifierTask();

            return ret;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private static bool RemoveNotifierTask()
        {
            LogHelper.Debug("RemoveNotifierTask");
            if (IsNotifierTaskInstalled())
            {
                return ProcessHelper.GetProcessFeedback(Environment.SystemDirectory + "\\schtasks.exe", $"/Delete /TN {NOTIFIER_TASK_NAME} /F");
            }
            else
            {
                return true;
            }
        }

        /// <summary>
        /// Ends the notifier task (if installed and running)
        /// </summary>
        /// <returns></returns>
        public static bool EndNotifierTask()
        {
            LogHelper.Debug("EndNotifierTask");
            return ProcessHelper.GetProcessFeedback(Environment.SystemDirectory + "\\schtasks.exe", $"/End /TN {NOTIFIER_TASK_NAME}");
        }

        /// <summary>
        /// Runs the notifier task immediately (if installed)
        /// </summary>
        /// <returns></returns>
        public static bool RunNotifierTask()
        {
            LogHelper.Debug("RunNotifierTask");
            return ProcessHelper.GetProcessFeedback(Environment.SystemDirectory + "\\schtasks.exe", $"/Run /I /TN {NOTIFIER_TASK_NAME}");
        }


        /// <summary>
        /// Gets whether the notifier task is installed.
        /// </summary>
        /// <returns></returns>
        public static bool IsNotifierTaskInstalled()
        {
            bool isInstalled = ProcessHelper.GetProcessFeedback(Environment.SystemDirectory + "\\schtasks.exe", $"/Query /TN {NOTIFIER_TASK_NAME}");
            LogHelper.Debug($"IsNotifierTaskInstalled {isInstalled}");
            return isInstalled;
        }

        /// <summary>
        /// Gets the fllag whether the app was installed from settings.
        /// </summary>
        /// <returns></returns>
        public static bool IsInstalled()
        {
            LogHelper.Debug($"IsInstalled: {Settings.Default.IsInstalled}");
            return Settings.Default.IsInstalled;
        }

    }
}
