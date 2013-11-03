namespace WindowsFirewallNotifier
{
    partial class MainForm
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
            System.Windows.Forms.Panel pnlMain;
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            System.Windows.Forms.CheckBox chkPath;
            System.Windows.Forms.Label lblTitle;
            this.chkCurrentProfile = new System.Windows.Forms.CheckBox();
            this.chkTemp = new System.Windows.Forms.CheckBox();
            this.btnAlwaysBlock = new System.Windows.Forms.Button();
            this.btnAlwaysAllow = new System.Windows.Forms.Button();
            this.pnlHeader = new System.Windows.Forms.Panel();
            this.btnOptions = new System.Windows.Forms.Button();
            this.lblConn = new System.Windows.Forms.Label();
            this.btnClose = new System.Windows.Forms.Button();
            this.btnMin = new System.Windows.Forms.Button();
            this.chkTRule = new System.Windows.Forms.CheckBox();
            this.btnNext = new System.Windows.Forms.Button();
            this.btnPrev = new System.Windows.Forms.Button();
            this.chkPortRule = new System.Windows.Forms.CheckBox();
            this.chkLPortRule = new System.Windows.Forms.CheckBox();
            this.chkServiceRule = new System.Windows.Forms.CheckBox();
            this.lblPath = new System.Windows.Forms.Label();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.lblApp = new System.Windows.Forms.Label();
            pnlMain = new System.Windows.Forms.Panel();
            chkPath = new System.Windows.Forms.CheckBox();
            lblTitle = new System.Windows.Forms.Label();
            pnlMain.SuspendLayout();
            this.pnlHeader.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.SuspendLayout();
            // 
            // pnlMain
            // 
            resources.ApplyResources(pnlMain, "pnlMain");
            pnlMain.BackColor = System.Drawing.SystemColors.Info;
            pnlMain.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            pnlMain.Controls.Add(this.chkCurrentProfile);
            pnlMain.Controls.Add(this.chkTemp);
            pnlMain.Controls.Add(this.btnAlwaysBlock);
            pnlMain.Controls.Add(this.btnAlwaysAllow);
            pnlMain.Controls.Add(this.pnlHeader);
            pnlMain.Name = "pnlMain";
            // 
            // chkCurrentProfile
            // 
            resources.ApplyResources(this.chkCurrentProfile, "chkCurrentProfile");
            this.chkCurrentProfile.Name = "chkCurrentProfile";
            this.chkCurrentProfile.TabStop = false;
            this.chkCurrentProfile.UseVisualStyleBackColor = true;
            // 
            // chkTemp
            // 
            resources.ApplyResources(this.chkTemp, "chkTemp");
            this.chkTemp.Name = "chkTemp";
            this.chkTemp.TabStop = false;
            this.chkTemp.UseVisualStyleBackColor = true;
            // 
            // btnAlwaysBlock
            // 
            resources.ApplyResources(this.btnAlwaysBlock, "btnAlwaysBlock");
            this.btnAlwaysBlock.BackColor = System.Drawing.Color.White;
            this.btnAlwaysBlock.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnAlwaysBlock.FlatAppearance.BorderColor = System.Drawing.Color.Red;
            this.btnAlwaysBlock.FlatAppearance.MouseOverBackColor = System.Drawing.Color.SkyBlue;
            this.btnAlwaysBlock.Name = "btnAlwaysBlock";
            this.btnAlwaysBlock.TabStop = false;
            this.btnAlwaysBlock.Tag = "";
            this.btnAlwaysBlock.UseVisualStyleBackColor = false;
            this.btnAlwaysBlock.Click += new System.EventHandler(this.btnIgnore_Click);
            // 
            // btnAlwaysAllow
            // 
            resources.ApplyResources(this.btnAlwaysAllow, "btnAlwaysAllow");
            this.btnAlwaysAllow.BackColor = System.Drawing.Color.White;
            this.btnAlwaysAllow.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnAlwaysAllow.FlatAppearance.BorderColor = System.Drawing.Color.DarkBlue;
            this.btnAlwaysAllow.FlatAppearance.MouseOverBackColor = System.Drawing.Color.SkyBlue;
            this.btnAlwaysAllow.Name = "btnAlwaysAllow";
            this.btnAlwaysAllow.TabStop = false;
            this.btnAlwaysAllow.Tag = "";
            this.btnAlwaysAllow.UseVisualStyleBackColor = false;
            this.btnAlwaysAllow.Click += new System.EventHandler(this.btnAllow_Click);
            // 
            // pnlHeader
            // 
            resources.ApplyResources(this.pnlHeader, "pnlHeader");
            this.pnlHeader.BackColor = System.Drawing.Color.White;
            this.pnlHeader.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pnlHeader.Controls.Add(this.btnOptions);
            this.pnlHeader.Controls.Add(chkPath);
            this.pnlHeader.Controls.Add(this.lblConn);
            this.pnlHeader.Controls.Add(this.btnClose);
            this.pnlHeader.Controls.Add(this.btnMin);
            this.pnlHeader.Controls.Add(this.chkTRule);
            this.pnlHeader.Controls.Add(this.btnNext);
            this.pnlHeader.Controls.Add(this.btnPrev);
            this.pnlHeader.Controls.Add(this.chkPortRule);
            this.pnlHeader.Controls.Add(this.chkLPortRule);
            this.pnlHeader.Controls.Add(this.chkServiceRule);
            this.pnlHeader.Controls.Add(lblTitle);
            this.pnlHeader.Controls.Add(this.lblPath);
            this.pnlHeader.Controls.Add(this.pictureBox1);
            this.pnlHeader.Controls.Add(this.lblApp);
            this.pnlHeader.Name = "pnlHeader";
            // 
            // btnOptions
            // 
            resources.ApplyResources(this.btnOptions, "btnOptions");
            this.btnOptions.BackColor = System.Drawing.Color.White;
            this.btnOptions.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnOptions.FlatAppearance.BorderColor = System.Drawing.Color.Gray;
            this.btnOptions.FlatAppearance.MouseOverBackColor = System.Drawing.Color.DarkTurquoise;
            this.btnOptions.Name = "btnOptions";
            this.btnOptions.TabStop = false;
            this.btnOptions.UseVisualStyleBackColor = false;
            this.btnOptions.Click += new System.EventHandler(this.btnOptions_Click);
            // 
            // chkPath
            // 
            resources.ApplyResources(chkPath, "chkPath");
            chkPath.Checked = true;
            chkPath.CheckState = System.Windows.Forms.CheckState.Checked;
            chkPath.Name = "chkPath";
            chkPath.TabStop = false;
            chkPath.UseVisualStyleBackColor = true;
            // 
            // lblConn
            // 
            resources.ApplyResources(this.lblConn, "lblConn");
            this.lblConn.Name = "lblConn";
            // 
            // btnClose
            // 
            resources.ApplyResources(this.btnClose, "btnClose");
            this.btnClose.BackColor = System.Drawing.Color.White;
            this.btnClose.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnClose.FlatAppearance.MouseOverBackColor = System.Drawing.Color.DarkTurquoise;
            this.btnClose.ForeColor = System.Drawing.Color.Red;
            this.btnClose.Name = "btnClose";
            this.btnClose.TabStop = false;
            this.btnClose.UseVisualStyleBackColor = false;
            this.btnClose.Click += new System.EventHandler(this.button2_Click);
            // 
            // btnMin
            // 
            resources.ApplyResources(this.btnMin, "btnMin");
            this.btnMin.BackColor = System.Drawing.Color.White;
            this.btnMin.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnMin.FlatAppearance.MouseOverBackColor = System.Drawing.Color.DarkTurquoise;
            this.btnMin.ForeColor = System.Drawing.Color.SteelBlue;
            this.btnMin.Name = "btnMin";
            this.btnMin.TabStop = false;
            this.btnMin.UseVisualStyleBackColor = false;
            this.btnMin.Click += new System.EventHandler(this.btnMin_Click);
            // 
            // chkTRule
            // 
            resources.ApplyResources(this.chkTRule, "chkTRule");
            this.chkTRule.AutoEllipsis = true;
            this.chkTRule.Name = "chkTRule";
            this.chkTRule.TabStop = false;
            this.chkTRule.UseVisualStyleBackColor = true;
            // 
            // btnNext
            // 
            resources.ApplyResources(this.btnNext, "btnNext");
            this.btnNext.BackColor = System.Drawing.Color.White;
            this.btnNext.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnNext.FlatAppearance.MouseOverBackColor = System.Drawing.Color.DarkTurquoise;
            this.btnNext.ForeColor = System.Drawing.Color.DarkOrange;
            this.btnNext.Name = "btnNext";
            this.btnNext.TabStop = false;
            this.btnNext.UseVisualStyleBackColor = false;
            this.btnNext.Click += new System.EventHandler(this.btnNext_Click);
            // 
            // btnPrev
            // 
            resources.ApplyResources(this.btnPrev, "btnPrev");
            this.btnPrev.BackColor = System.Drawing.Color.White;
            this.btnPrev.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnPrev.FlatAppearance.MouseOverBackColor = System.Drawing.Color.DarkTurquoise;
            this.btnPrev.ForeColor = System.Drawing.Color.DarkOrange;
            this.btnPrev.Name = "btnPrev";
            this.btnPrev.TabStop = false;
            this.btnPrev.UseVisualStyleBackColor = false;
            this.btnPrev.Click += new System.EventHandler(this.btnPrev_Click);
            // 
            // chkPortRule
            // 
            resources.ApplyResources(this.chkPortRule, "chkPortRule");
            this.chkPortRule.Name = "chkPortRule";
            this.chkPortRule.TabStop = false;
            this.chkPortRule.UseVisualStyleBackColor = true;
            // 
            // chkLPortRule
            // 
            resources.ApplyResources(this.chkLPortRule, "chkLPortRule");
            this.chkLPortRule.Name = "chkLPortRule";
            this.chkLPortRule.TabStop = false;
            this.chkLPortRule.UseVisualStyleBackColor = true;
            // 
            // chkServiceRule
            // 
            resources.ApplyResources(this.chkServiceRule, "chkServiceRule");
            this.chkServiceRule.AutoEllipsis = true;
            this.chkServiceRule.Name = "chkServiceRule";
            this.chkServiceRule.TabStop = false;
            this.chkServiceRule.UseVisualStyleBackColor = true;
            // 
            // lblTitle
            // 
            resources.ApplyResources(lblTitle, "lblTitle");
            lblTitle.BackColor = System.Drawing.SystemColors.Info;
            lblTitle.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            lblTitle.ForeColor = System.Drawing.SystemColors.ActiveCaptionText;
            lblTitle.Name = "lblTitle";
            // 
            // lblPath
            // 
            resources.ApplyResources(this.lblPath, "lblPath");
            this.lblPath.AutoEllipsis = true;
            this.lblPath.Cursor = System.Windows.Forms.Cursors.Hand;
            this.lblPath.Name = "lblPath";
            // 
            // pictureBox1
            // 
            resources.ApplyResources(this.pictureBox1, "pictureBox1");
            this.pictureBox1.BackColor = System.Drawing.Color.White;
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.TabStop = false;
            // 
            // lblApp
            // 
            resources.ApplyResources(this.lblApp, "lblApp");
            this.lblApp.AutoEllipsis = true;
            this.lblApp.BackColor = System.Drawing.Color.Transparent;
            this.lblApp.Name = "lblApp";
            // 
            // MainForm
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(pnlMain);
            this.DoubleBuffered = true;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.MaximizeBox = false;
            this.Name = "MainForm";
            this.Opacity = 0.9D;
            this.TopMost = true;
            pnlMain.ResumeLayout(false);
            pnlMain.PerformLayout();
            this.pnlHeader.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.CheckBox chkPortRule;
        private System.Windows.Forms.CheckBox chkTemp;
        private System.Windows.Forms.Button btnAlwaysBlock;
        private System.Windows.Forms.Button btnAlwaysAllow;
        private System.Windows.Forms.Button btnClose;
        private System.Windows.Forms.Label lblPath;
        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.Label lblApp;
        private System.Windows.Forms.Panel pnlHeader;
        private System.Windows.Forms.CheckBox chkServiceRule;
        private System.Windows.Forms.Button btnMin;
        private System.Windows.Forms.CheckBox chkLPortRule;
        private System.Windows.Forms.Button btnNext;
        private System.Windows.Forms.Button btnPrev;
        private System.Windows.Forms.CheckBox chkTRule;
        private System.Windows.Forms.Label lblConn;
        private System.Windows.Forms.Button btnOptions;
        private System.Windows.Forms.CheckBox chkCurrentProfile;

    }
}