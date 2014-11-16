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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(InstallForm));
            System.Windows.Forms.Label lblInstDesc;
            this.chkToTray = new System.Windows.Forms.CheckBox();
            this.label1 = new System.Windows.Forms.Label();
            this.lblNoBlockRule = new System.Windows.Forms.Label();
            this.chkNoBlockRule = new System.Windows.Forms.CheckBox();
            this.chkOEnableServiceDetection = new System.Windows.Forms.CheckBox();
            this.chkAnimate = new System.Windows.Forms.CheckBox();
            this.rbDisable = new System.Windows.Forms.RadioButton();
            this.ddlEnableFor = new System.Windows.Forms.ComboBox();
            this.rbEnable = new System.Windows.Forms.RadioButton();
            this.btnOK = new System.Windows.Forms.Button();
            pnlInsStep1 = new System.Windows.Forms.Panel();
            lblInstDesc = new System.Windows.Forms.Label();
            pnlInsStep1.SuspendLayout();
            this.SuspendLayout();
            // 
            // pnlInsStep1
            // 
            resources.ApplyResources(pnlInsStep1, "pnlInsStep1");
            pnlInsStep1.BackColor = System.Drawing.SystemColors.Window;
            pnlInsStep1.Controls.Add(this.chkToTray);
            pnlInsStep1.Controls.Add(this.label1);
            pnlInsStep1.Controls.Add(this.lblNoBlockRule);
            pnlInsStep1.Controls.Add(this.chkNoBlockRule);
            pnlInsStep1.Controls.Add(this.chkOEnableServiceDetection);
            pnlInsStep1.Controls.Add(this.chkAnimate);
            pnlInsStep1.Controls.Add(this.rbDisable);
            pnlInsStep1.Controls.Add(this.ddlEnableFor);
            pnlInsStep1.Controls.Add(this.rbEnable);
            pnlInsStep1.Controls.Add(lblInstDesc);
            pnlInsStep1.Name = "pnlInsStep1";
            // 
            // chkToTray
            // 
            resources.ApplyResources(this.chkToTray, "chkToTray");
            this.chkToTray.FlatAppearance.MouseOverBackColor = System.Drawing.Color.PowderBlue;
            this.chkToTray.Name = "chkToTray";
            this.chkToTray.UseVisualStyleBackColor = true;
            // 
            // label1
            // 
            resources.ApplyResources(this.label1, "label1");
            this.label1.Name = "label1";
            // 
            // lblNoBlockRule
            // 
            resources.ApplyResources(this.lblNoBlockRule, "lblNoBlockRule");
            this.lblNoBlockRule.Name = "lblNoBlockRule";
            // 
            // chkNoBlockRule
            // 
            resources.ApplyResources(this.chkNoBlockRule, "chkNoBlockRule");
            this.chkNoBlockRule.FlatAppearance.MouseOverBackColor = System.Drawing.Color.PowderBlue;
            this.chkNoBlockRule.Name = "chkNoBlockRule";
            this.chkNoBlockRule.UseVisualStyleBackColor = true;
            // 
            // chkOEnableServiceDetection
            // 
            resources.ApplyResources(this.chkOEnableServiceDetection, "chkOEnableServiceDetection");
            this.chkOEnableServiceDetection.FlatAppearance.MouseOverBackColor = System.Drawing.Color.PowderBlue;
            this.chkOEnableServiceDetection.Name = "chkOEnableServiceDetection";
            this.chkOEnableServiceDetection.UseVisualStyleBackColor = true;
            // 
            // chkAnimate
            // 
            resources.ApplyResources(this.chkAnimate, "chkAnimate");
            this.chkAnimate.FlatAppearance.MouseOverBackColor = System.Drawing.Color.PowderBlue;
            this.chkAnimate.Name = "chkAnimate";
            this.chkAnimate.UseVisualStyleBackColor = true;
            // 
            // rbDisable
            // 
            resources.ApplyResources(this.rbDisable, "rbDisable");
            this.rbDisable.Name = "rbDisable";
            this.rbDisable.TabStop = true;
            this.rbDisable.UseVisualStyleBackColor = true;
            // 
            // ddlEnableFor
            // 
            resources.ApplyResources(this.ddlEnableFor, "ddlEnableFor");
            this.ddlEnableFor.BackColor = System.Drawing.Color.PowderBlue;
            this.ddlEnableFor.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.ddlEnableFor.Items.AddRange(new object[] {
            resources.GetString("ddlEnableFor.Items"),
            resources.GetString("ddlEnableFor.Items1")});
            this.ddlEnableFor.Name = "ddlEnableFor";
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
            this.btnOK.BackColor = System.Drawing.SystemColors.ButtonFace;
            this.btnOK.FlatAppearance.BorderColor = System.Drawing.Color.DarkBlue;
            this.btnOK.FlatAppearance.MouseOverBackColor = System.Drawing.Color.SkyBlue;
            this.btnOK.Name = "btnOK";
            this.btnOK.TabStop = false;
            this.btnOK.Tag = "";
            this.btnOK.UseVisualStyleBackColor = false;
            this.btnOK.Click += new System.EventHandler(this.btnOK_Click);
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
            pnlInsStep1.PerformLayout();
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
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.CheckBox chkToTray;

    }
}