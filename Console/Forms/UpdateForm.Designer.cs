namespace WindowsFirewallNotifierConsole
{
    partial class UpdateForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.Windows.Forms.Panel pnlInsStep1;
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(UpdateForm));
            System.Windows.Forms.Label lblInstDesc;
            this.txtUpdate = new System.Windows.Forms.TextBox();
            this.btnOK = new System.Windows.Forms.Button();
            pnlInsStep1 = new System.Windows.Forms.Panel();
            lblInstDesc = new System.Windows.Forms.Label();
            pnlInsStep1.SuspendLayout();
            this.SuspendLayout();
            // 
            // pnlInsStep1
            // 
            pnlInsStep1.BackColor = System.Drawing.Color.White;
            pnlInsStep1.Controls.Add(this.txtUpdate);
            pnlInsStep1.Controls.Add(lblInstDesc);
            resources.ApplyResources(pnlInsStep1, "pnlInsStep1");
            pnlInsStep1.Name = "pnlInsStep1";
            // 
            // txtUpdate
            // 
            resources.ApplyResources(this.txtUpdate, "txtUpdate");
            this.txtUpdate.Name = "txtUpdate";
            this.txtUpdate.ReadOnly = true;
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
            // UpdateForm
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(pnlInsStep1);
            this.Controls.Add(this.btnOK);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Name = "UpdateForm";
            this.Load += new System.EventHandler(this.UpdateForm_Load);
            pnlInsStep1.ResumeLayout(false);
            pnlInsStep1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TextBox txtUpdate;
        private System.Windows.Forms.Button btnOK;
    }
}