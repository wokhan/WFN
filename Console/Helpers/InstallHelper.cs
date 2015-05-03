using System;
using System.IO;
using System.Security.Principal;
using WindowsFirewallNotifierConsole.Properties;
using WindowsFirewallNotifier;
using System.Text;
using System.Windows;

namespace WindowsFirewallNotifierConsole.Helpers
{
    public class InstallHelper
    {
        /// <summary>
        /// 
        /// </summary>
        public static bool RemoveProgram()
        {
            if (MessageBox.Show(Resources.MSG_DISABLE_WFN, Resources.MSG_DLG_TITLE, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                if (MessageBox.Show(Resources.MSG_UNINST_UNBLOCK, Resources.MSG_UNINST_TITLE_1,
                    MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes
                    && !FirewallHelper.RestoreWindowsFirewall())
                {
                    MessageBox.Show(Resources.MSG_UNINST_UNBLOCK_ERR, Resources.MSG_DLG_ERR_TITLE, MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }

                if (MessageBox.Show(Resources.MSG_UNINST_DISABLE_LOG, Resources.MSG_UNINST_TITLE_2, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes
                    && !ProcessHelper.getProcessFeedback(Environment.SystemDirectory + "\\auditpol.exe", "/set /subcategory:{0CCE9226-69AE-11D9-BED3-505054503030} /failure:disable"))
                {
                    MessageBox.Show(Resources.MSG_UNINST_DISABLE_LOG_ERR, Resources.MSG_DLG_ERR_TITLE, MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }

                if (!RemoveTask())
                {
                    MessageBox.Show(Resources.MSG_UNINST_TASK_ERR, Resources.MSG_DLG_ERR_TITLE, MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }

                MessageBox.Show(Resources.MSG_UNINST_OK, Resources.MSG_DLG_TITLE, MessageBoxButton.OK, MessageBoxImage.Information);

                return true;
            }
            else
            {
                return false;
            }
        }


        /// <summary>
        /// 
        /// </summary>
        public static bool EnableProgram()
        {
            return EnableProgram(false);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pass"></param>
        /// <returns></returns>
        public static bool EnableProgram(bool allUsers)
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
                    if (FirewallHelper.CreateDefaultRules())
                    {
                        if (createTask(allUsers))
                        {
                            MessageBox.Show(Resources.MSG_INST_OK, Resources.MSG_DLG_TITLE, MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        else
                        {
                            MessageBox.Show(Resources.MSG_INST_TASK_ERR, Resources.MSG_DLG_ERR_TITLE, MessageBoxButton.OK, MessageBoxImage.Error);
                            return false;
                        }
                    }
                    else
                    {
                        MessageBox.Show("Unable to create the default rules, please consider reactivating Windows Firewall Notifier.", Resources.MSG_DLG_ERR_TITLE, MessageBoxButton.OK, MessageBoxImage.Error);
                        return false;
                    }
                }
                else
                {
                    MessageBox.Show(Resources.MSG_INST_ENABLE_FW_ERR, Resources.MSG_DLG_ERR_TITLE, MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }
            }
            else
            {
                MessageBox.Show(Resources.MSG_INST_ENABLE_LOG_ERR, Resources.MSG_DLG_ERR_TITLE, MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            return true;
        }



        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private static bool createTask(bool allUsers)
        {
            string tmpXML = Path.GetTempFileName();
            File.WriteAllText(tmpXML, 
                              String.Format(Resources.WindowsFirewallNotifier, allUsers ? "<UserId>NT AUTHORITY\\SYSTEM</UserId>"//"<GroupId>S-1-5-32-545</GroupId>" 
                                                                                        : "<UserId><![CDATA[" + WindowsIdentity.GetCurrent().Name + "]]></UserId>", 
                                                                               "",//Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), "Notifier.exe"), 
                                                                               DateTime.Now.ToString("s")), 
                              Encoding.Unicode);
            //getProcessFeedback(Environment.SystemDirectory + "\\schtasks.exe", "/Create /TN WindowsFirewallNotifierTask /IT /SC ONEVENT /MO \"*[System[(Level=4 or Level=0) and (EventID=5157)]]\" /EC Security /TR \"" + Application.ExecutablePath + " run\" /F /RL HIGHEST");

            //bool ret = ProcessHelper.getProcessFeedback(Environment.SystemDirectory + "\\schtasks.exe", (pass != null ? "/RU " + WindowsIdentity.GetCurrent().Name + " /RP " + pass : "") + " /Create /TN WindowsFirewallNotifierTask /XML \"" + tmpXML + "\"");
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
