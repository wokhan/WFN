using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace WindowsFirewallNotifier.Forms
{
    public partial class ServicesForm : Form
    {
        public ServicesForm(WindowsFirewallNotifier.MainForm.CurrentConn conn)
        {
            InitializeComponent();
            lstServices.Items.AddRange(conn.PossibleServices.Select((s, i) => new ListViewItem(s, conn.PossibleServicesDesc[i]) { Checked = true }).ToArray());
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        public string[] SelectedServices
        {
            get
            {
                return lstServices.CheckedItems.Cast<ListViewItem>().Select(li => li.Text).ToArray();
            }
        }

        public bool CreateAppRule
        {
            get
            {
                return rbAppRule.Checked;
            }
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            this.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.Close();
        }

    }
}
