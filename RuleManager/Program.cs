using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Wokhan.WindowsFirewallNotifier.Common;
using Wokhan.WindowsFirewallNotifier.Common.Helpers;

namespace Wokhan.WindowsFirewallNotifier.RuleManager
{
    class Program
    {
        private static Dictionary<string, string> tmpnames;

        [STAThread]
        static void Main(string[] args)
        {
            LogHelper.Debug("Starting RuleManager: " + Environment.CommandLine);
            try
            {
                if (args.Count() == 0)
                {
                    MessageBox.Show(Resources.MSG_RULEMANAGER_ARGUMENTS_ERR, Resources.MSG_DLG_ERR_TITLE, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Environment.Exit(1);
                }
                if (args.Count() != 1)
                {
                    throw new ArgumentException("Wrong number of arguments!");
                }
                string[] param = Encoding.Unicode.GetString(Convert.FromBase64String(args[0])).Split(new string[] { "#$#" }, StringSplitOptions.None);

                if (param.Count() != 9)
                {
                    throw new ArgumentException("Invalid argument!");
                }

                string rname = param[0];
                string path = param[1];
                string sv = param[2];
                string[] services = (sv != null ? sv.Split(',') : new string[] { });
                int protocol = int.Parse(param[3]);
                string target = param[4];
                string targetPort = param[5];
                string localPort = param[6];
                bool useCurrentProfile = bool.Parse(param[7]);
                string action = param[8];
                bool keepOpen = false;
                bool ret = false;

                switch (action)
                {
                    case "A":
                        ret = services.All(s => FirewallHelper.AddAllowRule(rname + (s != null ? "[" + s + "]" : ""), path, s, protocol, target, targetPort, localPort, useCurrentProfile));
                        break;

                    case "B":
                        ret = services.All(s => FirewallHelper.AddBlockRule(rname + (s != null ? "[" + s + "]" : ""), path, s, protocol, target, targetPort, localPort, useCurrentProfile));
                        break;

                    case "T":
                        tmpnames = services.ToDictionary(s => s, s => "[WFN Temp Rule] " + Guid.NewGuid().ToString());
                        ret = services.All(s => FirewallHelper.AddTempRule(tmpnames[s], path, s, protocol, target, targetPort, localPort, useCurrentProfile));
                        keepOpen = true;
                        break;
                }

                if (!ret)
                {
                    throw new Exception("Unable to create the rule");
                }
                else if (keepOpen)
                {
                    NotifyIcon ni = new NotifyIcon();
                    ni.Click += new EventHandler(ni_Click);
                    ni.BalloonTipIcon = ToolTipIcon.Info;
                    ni.BalloonTipTitle = "Temporary rule"; //FIXME: Move to resources!
                    ni.BalloonTipText = "A temporary rule has been created." + Environment.NewLine + "Path: " + param[8] + Environment.NewLine + "Click on the shield icon to remove this rule."; //FIXME: Move to resources!
                    ni.Icon = new Icon(SystemIcons.Shield, new Size(16, 16));
                    ni.Visible = true;
                    ni.ShowBalloonTip(2000);

                    Application.Run();
                }
            }
            catch (Exception e)
            {
                LogHelper.Error("WFNRuleManager failure", e);
                Environment.Exit(1);
            }

            Environment.Exit(0);
        }

        private static void ni_Click(object sender, EventArgs e)
        {

            if (!tmpnames.All(kv => FirewallHelper.RemoveRule(kv.Value)))
            {
                MessageBox.Show(Resources.MSG_RULE_RM_FAILED, Resources.MSG_DLG_ERR_TITLE, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            
            Environment.Exit(0);
        }

    }
}
