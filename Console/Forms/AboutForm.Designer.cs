namespace WindowsFirewallNotifierConsole
{
    partial class AboutForm
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
            System.Windows.Forms.LinkLabel lnkWebsite;
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(AboutForm));
            System.Windows.Forms.Button btnDonate;
            System.Windows.Forms.Label lblAbout;
            this.lblVersion = new System.Windows.Forms.Label();
            this.pbIcon = new System.Windows.Forms.PictureBox();
            this.btnOK = new System.Windows.Forms.Button();
            pnlInsStep1 = new System.Windows.Forms.Panel();
            lnkWebsite = new System.Windows.Forms.LinkLabel();
            btnDonate = new System.Windows.Forms.Button();
            lblAbout = new System.Windows.Forms.Label();
            pnlInsStep1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pbIcon)).BeginInit();
            this.SuspendLayout();
            // 
            // pnlInsStep1
            // 
            pnlInsStep1.BackColor = System.Drawing.Color.White;
            pnlInsStep1.Controls.Add(lnkWebsite);
            pnlInsStep1.Controls.Add(this.lblVersion);
            pnlInsStep1.Controls.Add(this.pbIcon);
            pnlInsStep1.Controls.Add(btnDonate);
            pnlInsStep1.Controls.Add(lblAbout);
            resources.ApplyResources(pnlInsStep1, "pnlInsStep1");
            pnlInsStep1.Name = "pnlInsStep1";
            // 
            // lnkWebsite
            // 
            resources.ApplyResources(lnkWebsite, "lnkWebsite");
            lnkWebsite.LinkBehavior = System.Windows.Forms.LinkBehavior.NeverUnderline;
            lnkWebsite.Name = "lnkWebsite";
            lnkWebsite.TabStop = true;
            lnkWebsite.UseCompatibleTextRendering = true;
            lnkWebsite.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.linkLabel1_LinkClicked);
            // 
            // lblVersion
            // 
            resources.ApplyResources(this.lblVersion, "lblVersion");
            this.lblVersion.Name = "lblVersion";
            // 
            // pbIcon
            // 
            resources.ApplyResources(this.pbIcon, "pbIcon");
            this.pbIcon.Name = "pbIcon";
            this.pbIcon.TabStop = false;
            // 
            // btnDonate
            // 
            resources.ApplyResources(btnDonate, "btnDonate");
            btnDonate.BackColor = System.Drawing.Color.White;
            btnDonate.FlatAppearance.BorderColor = System.Drawing.Color.DeepSkyBlue;
            btnDonate.FlatAppearance.MouseOverBackColor = System.Drawing.Color.SkyBlue;
            btnDonate.Name = "btnDonate";
            btnDonate.Tag = "";
            btnDonate.UseVisualStyleBackColor = false;
            btnDonate.Click += new System.EventHandler(this.btnDonate_Click);
            // 
            // lblAbout
            // 
            resources.ApplyResources(lblAbout, "lblAbout");
            lblAbout.Name = "lblAbout";
            // 
            // btnOK
            // 
            resources.ApplyResources(this.btnOK, "btnOK");
            this.btnOK.BackColor = System.Drawing.Color.White;
            this.btnOK.FlatAppearance.BorderColor = System.Drawing.Color.DarkBlue;
            this.btnOK.FlatAppearance.MouseOverBackColor = System.Drawing.Color.SkyBlue;
            this.btnOK.Name = "btnOK";
            this.btnOK.Tag = "";
            this.btnOK.UseVisualStyleBackColor = false;
            this.btnOK.Click += new System.EventHandler(this.btnOK_Click);
            // 
            // AboutForm
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.btnOK);
            this.Controls.Add(pnlInsStep1);
            this.DoubleBuffered = true;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Name = "AboutForm";
            pnlInsStep1.ResumeLayout(false);
            pnlInsStep1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pbIcon)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button btnOK;
        private System.Windows.Forms.PictureBox pbIcon;
        private System.Windows.Forms.Label lblVersion;

    }
}