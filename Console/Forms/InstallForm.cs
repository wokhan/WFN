using System;
using System.Windows.Forms;
using WindowsFirewallNotifier;
using WindowsFirewallNotifierConsole.Helpers;

namespace WindowsFirewallNotifierConsole
{
    public partial class InstallForm : Form
    {
        private bool stateChanged = false;

        public InstallForm()
        {
            InitializeComponent();

            rbEnable.CheckedChanged += new EventHandler(rbEnable_CheckedChanged);
            rbEnable.Checked = InstallHelper.IsInstalled();

            chkAnimate.Checked = Settings.Default.UseAnimation;
            chkDetails.Checked = Settings.Default.AlwaysShowDetails;
            chkNoBlockRule.Checked = Settings.Default.UseBlockRules;
            chkOEnableServiceDetection.Checked = Settings.Default.EnableServiceDetection;
            chkToTray.Checked = Settings.Default.MinimizeToTray;

            ddlEnableFor.SelectedIndex = Settings.Default.EnableFor;

        }

        private void rbEnable_CheckedChanged(object sender, EventArgs e)
        {
            ddlEnableFor.Enabled = rbEnable.Checked;
            chkAnimate.Enabled = rbEnable.Checked;
            chkNoBlockRule.Enabled = rbEnable.Checked;
            chkOEnableServiceDetection.Enabled = rbEnable.Checked;

            stateChanged = true;
        }


        private void btnOK_Click(object sender, EventArgs e)
        {
            if (stateChanged)
            {
                if (rbEnable.Checked)
                {
                    InstallHelper.EnableProgram(ddlEnableFor.SelectedIndex == 1);
                }
                else if (InstallHelper.IsInstalled())
                {
                    InstallHelper.RemoveProgram();
                }
            }

            Settings.Default.UseAnimation = chkAnimate.Checked;
            Settings.Default.AlwaysShowDetails = chkDetails.Checked;
            Settings.Default.UseBlockRules = chkNoBlockRule.Checked;
            Settings.Default.EnableServiceDetection = chkOEnableServiceDetection.Checked;
            Settings.Default.MinimizeToTray = chkToTray.Checked;

            Settings.Default.EnableFor = ddlEnableFor.SelectedIndex;



            Settings.Default.Save();

            this.Close();
        }
    }
}
