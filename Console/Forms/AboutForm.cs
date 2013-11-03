using System;
using System.Diagnostics;
using System.Windows.Forms;
using WindowsFirewallNotifierConsole.Properties;

namespace WindowsFirewallNotifierConsole
{
    public partial class AboutForm : Form
    {
        public AboutForm()
        {
            InitializeComponent();
            lblVersion.Text = String.Format(lblVersion.Text, Application.ProductVersion);
            pbIcon.Image = Resources.ICON_SHIELD.ToBitmap();
        }


        private void btnOK_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void btnDonate_Click(object sender, EventArgs e)
        {
            Process.Start("https://www.paypal.com/cgi-bin/webscr?cmd=_donations&business=wokhan%40online%2efr&lc=US&item_name=Khan%20%28Windows%20Firewall%20Notifier%29&item_number=WOK%2dWFN&currency_code=EUR&bn=PP%2dDonationsBF%3abtn_donateCC_LG%2egif%3aNonHosted");
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start("http://wokhan.online.fr");
        }
    }
}
