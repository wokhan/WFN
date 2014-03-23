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
            System.Windows.Forms.Label lblTitle;
            System.Windows.Forms.CheckBox chkPath;
            this.btnPrev = new System.Windows.Forms.Button();
            this.btnNext = new System.Windows.Forms.Button();
            this.btnAdvanced = new System.Windows.Forms.Button();
            this.btnOptions = new System.Windows.Forms.Button();
            this.btnMin = new System.Windows.Forms.Button();
            this.pnlInfos = new System.Windows.Forms.Panel();
            this.lblConn = new System.Windows.Forms.Label();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.lblApp = new System.Windows.Forms.Label();
            this.btnClose = new System.Windows.Forms.Button();
            this.btnAlwaysBlock = new System.Windows.Forms.Button();
            this.btnAlwaysAllow = new System.Windows.Forms.Button();
            this.pnlHeader = new System.Windows.Forms.Panel();
            this.chkCurrentProfile = new System.Windows.Forms.CheckBox();
            this.chkTemp = new System.Windows.Forms.CheckBox();
            this.chkTRule = new System.Windows.Forms.CheckBox();
            this.chkPortRule = new System.Windows.Forms.CheckBox();
            this.chkLPortRule = new System.Windows.Forms.CheckBox();
            this.chkServiceRule = new System.Windows.Forms.CheckBox();
            this.lblPath = new System.Windows.Forms.Label();
            pnlMain = new System.Windows.Forms.Panel();
            lblTitle = new System.Windows.Forms.Label();
            chkPath = new System.Windows.Forms.CheckBox();
            pnlMain.SuspendLayout();
            this.pnlInfos.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.pnlHeader.SuspendLayout();
            this.SuspendLayout();
            // 
            // pnlMain
            // 
            pnlMain.BackColor = System.Drawing.SystemColors.Window;
            pnlMain.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            pnlMain.Controls.Add(this.btnPrev);
            pnlMain.Controls.Add(this.btnNext);
            pnlMain.Controls.Add(this.btnAdvanced);
            pnlMain.Controls.Add(this.btnOptions);
            pnlMain.Controls.Add(this.btnMin);
            pnlMain.Controls.Add(lblTitle);
            pnlMain.Controls.Add(this.pnlInfos);
            pnlMain.Controls.Add(this.btnClose);
            pnlMain.Controls.Add(this.btnAlwaysBlock);
            pnlMain.Controls.Add(this.btnAlwaysAllow);
            pnlMain.Controls.Add(this.pnlHeader);
            resources.ApplyResources(pnlMain, "pnlMain");
            pnlMain.Name = "pnlMain";
            // 
            // btnPrev
            // 
            this.btnPrev.BackColor = System.Drawing.SystemColors.ButtonFace;
            this.btnPrev.Cursor = System.Windows.Forms.Cursors.Hand;
            resources.ApplyResources(this.btnPrev, "btnPrev");
            this.btnPrev.FlatAppearance.BorderSize = 0;
            this.btnPrev.FlatAppearance.MouseOverBackColor = System.Drawing.SystemColors.ActiveBorder;
            this.btnPrev.ForeColor = System.Drawing.Color.DarkOrange;
            this.btnPrev.Name = "btnPrev";
            this.btnPrev.TabStop = false;
            this.btnPrev.UseVisualStyleBackColor = false;
            this.btnPrev.Click += new System.EventHandler(this.btnPrev_Click);
            // 
            // btnNext
            // 
            this.btnNext.BackColor = System.Drawing.SystemColors.ButtonFace;
            this.btnNext.Cursor = System.Windows.Forms.Cursors.Hand;
            resources.ApplyResources(this.btnNext, "btnNext");
            this.btnNext.FlatAppearance.BorderSize = 0;
            this.btnNext.FlatAppearance.MouseOverBackColor = System.Drawing.Color.DarkTurquoise;
            this.btnNext.ForeColor = System.Drawing.Color.DarkOrange;
            this.btnNext.Name = "btnNext";
            this.btnNext.TabStop = false;
            this.btnNext.UseVisualStyleBackColor = false;
            this.btnNext.Click += new System.EventHandler(this.btnNext_Click);
            // 
            // btnAdvanced
            // 
            this.btnAdvanced.BackColor = System.Drawing.SystemColors.ButtonFace;
            this.btnAdvanced.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnAdvanced.FlatAppearance.BorderSize = 0;
            this.btnAdvanced.FlatAppearance.MouseOverBackColor = System.Drawing.Color.SkyBlue;
            resources.ApplyResources(this.btnAdvanced, "btnAdvanced");
            this.btnAdvanced.Name = "btnAdvanced";
            this.btnAdvanced.TabStop = false;
            this.btnAdvanced.UseVisualStyleBackColor = false;
            this.btnAdvanced.Click += new System.EventHandler(this.btnAdvanced_Click);
            // 
            // btnOptions
            // 
            this.btnOptions.BackColor = System.Drawing.SystemColors.ButtonFace;
            this.btnOptions.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnOptions.FlatAppearance.BorderColor = System.Drawing.Color.Gray;
            this.btnOptions.FlatAppearance.MouseOverBackColor = System.Drawing.Color.SkyBlue;
            resources.ApplyResources(this.btnOptions, "btnOptions");
            this.btnOptions.Name = "btnOptions";
            this.btnOptions.TabStop = false;
            this.btnOptions.UseVisualStyleBackColor = false;
            this.btnOptions.Click += new System.EventHandler(this.btnOptions_Click);
            // 
            // btnMin
            // 
            this.btnMin.BackColor = System.Drawing.SystemColors.ButtonFace;
            this.btnMin.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnMin.FlatAppearance.BorderColor = System.Drawing.Color.Gray;
            this.btnMin.FlatAppearance.MouseOverBackColor = System.Drawing.Color.SkyBlue;
            resources.ApplyResources(this.btnMin, "btnMin");
            this.btnMin.Name = "btnMin";
            this.btnMin.TabStop = false;
            this.btnMin.UseVisualStyleBackColor = false;
            this.btnMin.Click += new System.EventHandler(this.btnMin_Click);
            // 
            // lblTitle
            // 
            resources.ApplyResources(lblTitle, "lblTitle");
            lblTitle.BackColor = System.Drawing.SystemColors.ActiveCaption;
            lblTitle.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            lblTitle.ForeColor = System.Drawing.SystemColors.ActiveCaptionText;
            lblTitle.Name = "lblTitle";
            // 
            // pnlInfos
            // 
            this.pnlInfos.Controls.Add(this.lblConn);
            this.pnlInfos.Controls.Add(this.pictureBox1);
            this.pnlInfos.Controls.Add(this.lblApp);
            resources.ApplyResources(this.pnlInfos, "pnlInfos");
            this.pnlInfos.Name = "pnlInfos";
            // 
            // lblConn
            // 
            resources.ApplyResources(this.lblConn, "lblConn");
            this.lblConn.Name = "lblConn";
            // 
            // pictureBox1
            // 
            resources.ApplyResources(this.pictureBox1, "pictureBox1");
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.TabStop = false;
            // 
            // lblApp
            // 
            this.lblApp.AutoEllipsis = true;
            resources.ApplyResources(this.lblApp, "lblApp");
            this.lblApp.Name = "lblApp";
            // 
            // btnClose
            // 
            this.btnClose.BackColor = System.Drawing.SystemColors.ButtonFace;
            this.btnClose.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnClose.FlatAppearance.MouseOverBackColor = System.Drawing.Color.SkyBlue;
            resources.ApplyResources(this.btnClose, "btnClose");
            this.btnClose.Name = "btnClose";
            this.btnClose.TabStop = false;
            this.btnClose.UseVisualStyleBackColor = false;
            this.btnClose.Click += new System.EventHandler(this.button2_Click);
            // 
            // btnAlwaysBlock
            // 
            this.btnAlwaysBlock.BackColor = System.Drawing.SystemColors.ButtonFace;
            this.btnAlwaysBlock.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnAlwaysBlock.FlatAppearance.BorderColor = System.Drawing.Color.Red;
            this.btnAlwaysBlock.FlatAppearance.MouseOverBackColor = System.Drawing.Color.SkyBlue;
            resources.ApplyResources(this.btnAlwaysBlock, "btnAlwaysBlock");
            this.btnAlwaysBlock.Name = "btnAlwaysBlock";
            this.btnAlwaysBlock.TabStop = false;
            this.btnAlwaysBlock.Tag = "";
            this.btnAlwaysBlock.UseVisualStyleBackColor = false;
            this.btnAlwaysBlock.Click += new System.EventHandler(this.btnIgnore_Click);
            // 
            // btnAlwaysAllow
            // 
            this.btnAlwaysAllow.BackColor = System.Drawing.SystemColors.ButtonFace;
            this.btnAlwaysAllow.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnAlwaysAllow.FlatAppearance.BorderColor = System.Drawing.Color.DarkBlue;
            this.btnAlwaysAllow.FlatAppearance.MouseOverBackColor = System.Drawing.Color.SkyBlue;
            resources.ApplyResources(this.btnAlwaysAllow, "btnAlwaysAllow");
            this.btnAlwaysAllow.Name = "btnAlwaysAllow";
            this.btnAlwaysAllow.TabStop = false;
            this.btnAlwaysAllow.Tag = "";
            this.btnAlwaysAllow.UseVisualStyleBackColor = false;
            this.btnAlwaysAllow.Click += new System.EventHandler(this.btnAllow_Click);
            // 
            // pnlHeader
            // 
            resources.ApplyResources(this.pnlHeader, "pnlHeader");
            this.pnlHeader.BackColor = System.Drawing.SystemColors.Info;
            this.pnlHeader.Controls.Add(this.chkCurrentProfile);
            this.pnlHeader.Controls.Add(this.chkTemp);
            this.pnlHeader.Controls.Add(chkPath);
            this.pnlHeader.Controls.Add(this.chkTRule);
            this.pnlHeader.Controls.Add(this.chkPortRule);
            this.pnlHeader.Controls.Add(this.chkLPortRule);
            this.pnlHeader.Controls.Add(this.chkServiceRule);
            this.pnlHeader.Controls.Add(this.lblPath);
            this.pnlHeader.Name = "pnlHeader";
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
            // chkPath
            // 
            chkPath.Checked = true;
            chkPath.CheckState = System.Windows.Forms.CheckState.Checked;
            resources.ApplyResources(chkPath, "chkPath");
            chkPath.Name = "chkPath";
            chkPath.TabStop = false;
            chkPath.UseVisualStyleBackColor = true;
            // 
            // chkTRule
            // 
            this.chkTRule.AutoEllipsis = true;
            resources.ApplyResources(this.chkTRule, "chkTRule");
            this.chkTRule.Name = "chkTRule";
            this.chkTRule.TabStop = false;
            this.chkTRule.UseVisualStyleBackColor = true;
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
            this.chkServiceRule.AutoEllipsis = true;
            resources.ApplyResources(this.chkServiceRule, "chkServiceRule");
            this.chkServiceRule.Name = "chkServiceRule";
            this.chkServiceRule.TabStop = false;
            this.chkServiceRule.UseVisualStyleBackColor = true;
            // 
            // lblPath
            // 
            this.lblPath.AutoEllipsis = true;
            this.lblPath.Cursor = System.Windows.Forms.Cursors.Hand;
            resources.ApplyResources(this.lblPath, "lblPath");
            this.lblPath.Name = "lblPath";
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
            this.pnlInfos.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.pnlHeader.ResumeLayout(false);
            this.pnlHeader.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.CheckBox chkPortRule;
        private System.Windows.Forms.Button btnAlwaysBlock;
        private System.Windows.Forms.Button btnAlwaysAllow;
        private System.Windows.Forms.Button btnClose;
        private System.Windows.Forms.Label lblPath;
        private System.Windows.Forms.Panel pnlHeader;
        private System.Windows.Forms.CheckBox chkServiceRule;
        private System.Windows.Forms.CheckBox chkLPortRule;
        private System.Windows.Forms.Button btnNext;
        private System.Windows.Forms.Button btnPrev;
        private System.Windows.Forms.CheckBox chkTRule;
        private System.Windows.Forms.Button btnAdvanced;
        private System.Windows.Forms.Button btnOptions;
        private System.Windows.Forms.Button btnMin;
        private System.Windows.Forms.Panel pnlInfos;
        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.Label lblApp;
        private System.Windows.Forms.CheckBox chkCurrentProfile;
        private System.Windows.Forms.CheckBox chkTemp;
        private System.Windows.Forms.Label lblConn;

    }
}