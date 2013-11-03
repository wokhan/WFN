namespace WindowsFirewallNotifierConsole
{
    partial class InstallForm
    {
        /// <summary>
        /// Variable nécessaire au concepteur.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Nettoyage des ressources utilisées.
        /// </summary>
        /// <param name="disposing">true si les ressources managées doivent être supprimées ; sinon, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Code généré par le Concepteur Windows Form

        /// <summary>
        /// Méthode requise pour la prise en charge du concepteur - ne modifiez pas
        /// le contenu de cette méthode avec l'éditeur de code.
        /// </summary>
        private void InitializeComponent()
        {
            System.Windows.Forms.Panel pnlInsStep1;
            System.Windows.Forms.Label lblInstDesc;
            WindowsFirewallNotifier.Settings settings = new WindowsFirewallNotifier.Settings();
            //settings.ConsoleSize = new System.Drawing.Size(770, 507);
            //settings.ConsoleState = System.Windows.Forms.FormWindowState.Normal;
            //settings.EnableFor = 0;
            //settings.EnableForAllAccounts = false;
            //settings.EnableServiceDetection = true;
            //settings.FirstRun = true;
            //settings.SettingsKey = "";
            //settings.UseAnimation = true;
            //settings.UseBlockRules = false;
            
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(InstallForm));
            this.rbDisable = new System.Windows.Forms.RadioButton();
            this.rbEnable = new System.Windows.Forms.RadioButton();
            this.btnOK = new System.Windows.Forms.Button();
            this.chkNoBlockRule = new System.Windows.Forms.CheckBox();
            this.chkOEnableServiceDetection = new System.Windows.Forms.CheckBox();
            this.chkAnimate = new System.Windows.Forms.CheckBox();
            this.ddlEnableFor = new System.Windows.Forms.ComboBox();
            this.lblNoBlockRule = new System.Windows.Forms.Label();
            pnlInsStep1 = new System.Windows.Forms.Panel();
            lblInstDesc = new System.Windows.Forms.Label();
            pnlInsStep1.SuspendLayout();
            this.SuspendLayout();
            // 
            // pnlInsStep1
            // 
            pnlInsStep1.BackColor = System.Drawing.Color.White;
            pnlInsStep1.Controls.Add(this.lblNoBlockRule);
            pnlInsStep1.Controls.Add(this.chkNoBlockRule);
            pnlInsStep1.Controls.Add(this.chkOEnableServiceDetection);
            pnlInsStep1.Controls.Add(this.chkAnimate);
            pnlInsStep1.Controls.Add(this.rbDisable);
            pnlInsStep1.Controls.Add(this.ddlEnableFor);
            pnlInsStep1.Controls.Add(this.rbEnable);
            pnlInsStep1.Controls.Add(lblInstDesc);
            resources.ApplyResources(pnlInsStep1, "pnlInsStep1");
            pnlInsStep1.Name = "pnlInsStep1";
            // 
            // rbDisable
            // 
            resources.ApplyResources(this.rbDisable, "rbDisable");
            this.rbDisable.Name = "rbDisable";
            this.rbDisable.TabStop = true;
            this.rbDisable.UseVisualStyleBackColor = true;
            // 
            // rbEnable
            // 
            resources.ApplyResources(this.rbEnable, "rbEnable");
            this.rbEnable.Name = "rbEnable";
            this.rbEnable.TabStop = true;
            this.rbEnable.UseVisualStyleBackColor = true;
            // 
            // lblInstDesc
            // 
            resources.ApplyResources(lblInstDesc, "lblInstDesc");
            lblInstDesc.Name = "lblInstDesc";
            // 
            // btnOK
            // 
            resources.ApplyResources(this.btnOK, "btnOK");
            this.btnOK.BackColor = System.Drawing.Color.White;
            this.btnOK.FlatAppearance.BorderColor = System.Drawing.Color.DarkBlue;
            this.btnOK.FlatAppearance.MouseOverBackColor = System.Drawing.Color.SkyBlue;
            this.btnOK.Name = "btnOK";
            this.btnOK.TabStop = false;
            this.btnOK.Tag = "";
            this.btnOK.UseVisualStyleBackColor = false;
            this.btnOK.Click += new System.EventHandler(this.btnOK_Click);
            // 
            // chkNoBlockRule
            // 
            this.chkNoBlockRule.Checked = settings.UseBlockRules;
            this.chkNoBlockRule.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkNoBlockRule.DataBindings.Add(new System.Windows.Forms.Binding("Checked", settings, "UseBlockRules", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.chkNoBlockRule.FlatAppearance.MouseOverBackColor = System.Drawing.Color.PowderBlue;
            resources.ApplyResources(this.chkNoBlockRule, "chkNoBlockRule");
            this.chkNoBlockRule.Name = "chkNoBlockRule";
            this.chkNoBlockRule.UseVisualStyleBackColor = true;
            // 
            // chkOEnableServiceDetection
            // 
            this.chkOEnableServiceDetection.Checked = settings.EnableServiceDetection;
            this.chkOEnableServiceDetection.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkOEnableServiceDetection.DataBindings.Add(new System.Windows.Forms.Binding("Checked", settings, "EnableServiceDetection", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.chkOEnableServiceDetection.FlatAppearance.MouseOverBackColor = System.Drawing.Color.PowderBlue;
            resources.ApplyResources(this.chkOEnableServiceDetection, "chkOEnableServiceDetection");
            this.chkOEnableServiceDetection.Name = "chkOEnableServiceDetection";
            this.chkOEnableServiceDetection.UseVisualStyleBackColor = true;
            // 
            // chkAnimate
            // 
            this.chkAnimate.Checked = settings.UseAnimation;
            this.chkAnimate.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkAnimate.DataBindings.Add(new System.Windows.Forms.Binding("Checked", settings, "UseAnimation", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.chkAnimate.FlatAppearance.MouseOverBackColor = System.Drawing.Color.PowderBlue;
            resources.ApplyResources(this.chkAnimate, "chkAnimate");
            this.chkAnimate.Name = "chkAnimate";
            this.chkAnimate.UseVisualStyleBackColor = true;
            // 
            // ddlEnableFor
            // 
            this.ddlEnableFor.BackColor = System.Drawing.Color.PowderBlue;
            this.ddlEnableFor.DataBindings.Add(new System.Windows.Forms.Binding("SelectedIndex", settings, "EnableFor", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.ddlEnableFor.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            resources.ApplyResources(this.ddlEnableFor, "ddlEnableFor");
            this.ddlEnableFor.Items.AddRange(new object[] {
            resources.GetString("ddlEnableFor.Items"),
            resources.GetString("ddlEnableFor.Items1")});
            this.ddlEnableFor.Name = "ddlEnableFor";
            // 
            // lblNoBlockRule
            // 
            resources.ApplyResources(this.lblNoBlockRule, "lblNoBlockRule");
            this.lblNoBlockRule.Name = "lblNoBlockRule";
            // 
            // InstallForm
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.btnOK);
            this.Controls.Add(pnlInsStep1);
            this.DoubleBuffered = true;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Name = "InstallForm";
            pnlInsStep1.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button btnOK;
        private System.Windows.Forms.RadioButton rbDisable;
        private System.Windows.Forms.ComboBox ddlEnableFor;
        private System.Windows.Forms.RadioButton rbEnable;
        private System.Windows.Forms.CheckBox chkAnimate;
        private System.Windows.Forms.CheckBox chkOEnableServiceDetection;
        private System.Windows.Forms.CheckBox chkNoBlockRule;
        private System.Windows.Forms.Label lblNoBlockRule;

    }
}