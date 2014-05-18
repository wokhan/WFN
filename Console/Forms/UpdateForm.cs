using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Text;
using System.Windows.Forms;
using WindowsFirewallNotifierConsole.Properties;

namespace WindowsFirewallNotifierConsole
{
    public partial class UpdateForm : Form
    {
        public UpdateForm()
        {
            InitializeComponent();
        }

        private void UpdateForm_Load(object sender, EventArgs e)
        {
            try
            {
                WebClient wc = new WebClient();
                txtUpdate.Text = wc.DownloadString("http://wokhan.online.fr/progs/" + Resources.FILENAME_UPDATE);
            }
            catch
            {
                txtUpdate.Text = "Unable to retrieve the release notes. Please check your connection.";
            }
            txtUpdate.Select(0, 0);
        
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            Process.Start("http://wfn.codeplex.com/releases");
        }
    }
}
