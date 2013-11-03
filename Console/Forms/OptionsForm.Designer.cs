using WindowsFirewallNotifier.Properties;

namespace WindowsFirewallNotifierConsole
{
    partial class OptionsForm
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
            this.components = new System.ComponentModel.Container();
            System.Windows.Forms.Button btnOptions;
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(OptionsForm));
            System.Windows.Forms.Button btnDonate;
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
            this.btnUpdate = new System.Windows.Forms.Button();
            this.btnTConnections = new System.Windows.Forms.Button();
            this.btnTRules = new System.Windows.Forms.Button();
            this.btnTLog = new System.Windows.Forms.Button();
            this.lblRules = new System.Windows.Forms.Label();
            this.lblExceptions = new System.Windows.Forms.Label();
            this.lblLog = new System.Windows.Forms.Label();
            this.tabPanel = new System.Windows.Forms.TabControl();
            this.tabConnections = new System.Windows.Forms.TabPage();
            this.lstConnections = new System.Windows.Forms.ListView();
            this.connOwner = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.connProtocol = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.connLocalAddr = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.connLocalPort = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.connRemoteAddr = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.connRemotePort = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.connState = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.connCreaTime = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.imgLstConn = new System.Windows.Forms.ImageList(this.components);
            this.toolStripConnections = new System.Windows.Forms.ToolStrip();
            this.btnConnTrack = new System.Windows.Forms.ToolStripButton();
            this.btnConnStop = new System.Windows.Forms.ToolStripButton();
            this.stripConnUpdSpeed = new System.Windows.Forms.ToolStripDropDownButton();
            this.slow5SecondsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.normal2sToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.fast1sToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator3 = new System.Windows.Forms.ToolStripSeparator();
            this.btnConnFindR = new System.Windows.Forms.ToolStripButton();
            this.lblConnections = new System.Windows.Forms.Label();
            this.tabRules = new System.Windows.Forms.TabPage();
            this.gridRules = new System.Windows.Forms.DataGridView();
            this.colRName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colRIcon = new System.Windows.Forms.DataGridViewImageColumn();
            this.colRPath = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colRSvc = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colRDirection = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colRProfile = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colRAction = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colRLocalport = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colRTarget = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colRPort = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colREnabled = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            this.toolStripRules = new System.Windows.Forms.ToolStrip();
            this.btnRDelete = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.btnRLocate = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator6 = new System.Windows.Forms.ToolStripSeparator();
            this.btnOpenConsole = new System.Windows.Forms.ToolStripButton();
            this.btnRRefresh = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator8 = new System.Windows.Forms.ToolStripSeparator();
            this.btnRShow = new System.Windows.Forms.ToolStripDropDownButton();
            this.showAllToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.activeRulesOnlyToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.wFNRulesOnlyToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.wSHRulesOnlyToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator7 = new System.Windows.Forms.ToolStripSeparator();
            this.txtFilter = new System.Windows.Forms.ToolStripTextBox();
            this.toolStripLabel1 = new System.Windows.Forms.ToolStripLabel();
            this.tabExceptions = new System.Windows.Forms.TabPage();
            this.gridExceptions = new System.Windows.Forms.DataGridView();
            this.colExcIcon = new System.Windows.Forms.DataGridViewImageColumn();
            this.colExcPath = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colExcLocalPort = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colExcTarget = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colExcTargetPort = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.toolStripExceptions = new System.Windows.Forms.ToolStrip();
            this.btnERemove = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.btnELocate = new System.Windows.Forms.ToolStripButton();
            this.tabLog = new System.Windows.Forms.TabPage();
            this.gridLog = new System.Windows.Forms.DataGridView();
            this.colDate = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colIcon = new System.Windows.Forms.DataGridViewImageColumn();
            this.colPath = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colTarget = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colProtocol = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colPort = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.toolStripLog = new System.Windows.Forms.ToolStrip();
            this.btnLLocate = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator4 = new System.Windows.Forms.ToolStripSeparator();
            this.btnLEvents = new System.Windows.Forms.ToolStripButton();
            this.timerTrk = new System.Windows.Forms.Timer(this.components);
            this.pnlOverTabTop = new System.Windows.Forms.Panel();
            this.btnTExceptions = new System.Windows.Forms.Button();
            btnOptions = new System.Windows.Forms.Button();
            btnDonate = new System.Windows.Forms.Button();
            this.tabPanel.SuspendLayout();
            this.tabConnections.SuspendLayout();
            this.toolStripConnections.SuspendLayout();
            this.tabRules.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.gridRules)).BeginInit();
            this.toolStripRules.SuspendLayout();
            this.tabExceptions.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.gridExceptions)).BeginInit();
            this.toolStripExceptions.SuspendLayout();
            this.tabLog.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.gridLog)).BeginInit();
            this.toolStripLog.SuspendLayout();
            this.pnlOverTabTop.SuspendLayout();
            this.SuspendLayout();
            // 
            // btnOptions
            // 
            resources.ApplyResources(btnOptions, "btnOptions");
            btnOptions.BackColor = System.Drawing.Color.White;
            btnOptions.FlatAppearance.BorderColor = System.Drawing.Color.DarkOrange;
            btnOptions.FlatAppearance.MouseOverBackColor = System.Drawing.Color.Moccasin;
            btnOptions.Name = "btnOptions";
            btnOptions.Tag = "";
            btnOptions.UseVisualStyleBackColor = false;
            btnOptions.Click += new System.EventHandler(this.btnOptions_Click);
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
            // btnUpdate
            // 
            resources.ApplyResources(this.btnUpdate, "btnUpdate");
            this.btnUpdate.BackColor = System.Drawing.Color.White;
            this.btnUpdate.FlatAppearance.BorderColor = System.Drawing.Color.DeepSkyBlue;
            this.btnUpdate.FlatAppearance.MouseOverBackColor = System.Drawing.Color.SkyBlue;
            this.btnUpdate.Name = "btnUpdate";
            this.btnUpdate.Tag = "";
            this.btnUpdate.UseVisualStyleBackColor = false;
            this.btnUpdate.Click += new System.EventHandler(this.btnUpdate_Click);
            // 
            // btnTConnections
            // 
            resources.ApplyResources(this.btnTConnections, "btnTConnections");
            this.btnTConnections.BackColor = System.Drawing.Color.White;
            this.btnTConnections.FlatAppearance.BorderColor = System.Drawing.Color.LimeGreen;
            this.btnTConnections.FlatAppearance.MouseOverBackColor = System.Drawing.Color.SkyBlue;
            this.btnTConnections.Name = "btnTConnections";
            this.btnTConnections.Tag = "";
            this.btnTConnections.UseVisualStyleBackColor = false;
            this.btnTConnections.Click += new System.EventHandler(this.btnTConnections_Click);
            // 
            // btnTRules
            // 
            resources.ApplyResources(this.btnTRules, "btnTRules");
            this.btnTRules.BackColor = System.Drawing.Color.White;
            this.btnTRules.FlatAppearance.BorderColor = System.Drawing.Color.LimeGreen;
            this.btnTRules.FlatAppearance.MouseOverBackColor = System.Drawing.Color.SkyBlue;
            this.btnTRules.Name = "btnTRules";
            this.btnTRules.Tag = "";
            this.btnTRules.UseVisualStyleBackColor = false;
            this.btnTRules.Click += new System.EventHandler(this.btnTRules_Click);
            // 
            // btnTLog
            // 
            resources.ApplyResources(this.btnTLog, "btnTLog");
            this.btnTLog.BackColor = System.Drawing.Color.White;
            this.btnTLog.FlatAppearance.BorderColor = System.Drawing.Color.LimeGreen;
            this.btnTLog.FlatAppearance.MouseOverBackColor = System.Drawing.Color.SkyBlue;
            this.btnTLog.Name = "btnTLog";
            this.btnTLog.Tag = "";
            this.btnTLog.UseVisualStyleBackColor = false;
            this.btnTLog.Click += new System.EventHandler(this.btnTLog_Click);
            // 
            // lblRules
            // 
            resources.ApplyResources(this.lblRules, "lblRules");
            this.lblRules.BackColor = System.Drawing.SystemColors.Info;
            this.lblRules.Name = "lblRules";
            // 
            // lblExceptions
            // 
            resources.ApplyResources(this.lblExceptions, "lblExceptions");
            this.lblExceptions.BackColor = System.Drawing.SystemColors.Info;
            this.lblExceptions.Name = "lblExceptions";
            // 
            // lblLog
            // 
            resources.ApplyResources(this.lblLog, "lblLog");
            this.lblLog.BackColor = System.Drawing.SystemColors.Info;
            this.lblLog.Name = "lblLog";
            // 
            // tabPanel
            // 
            resources.ApplyResources(this.tabPanel, "tabPanel");
            this.tabPanel.Controls.Add(this.tabConnections);
            this.tabPanel.Controls.Add(this.tabRules);
            this.tabPanel.Controls.Add(this.tabExceptions);
            this.tabPanel.Controls.Add(this.tabLog);
            this.tabPanel.DrawMode = System.Windows.Forms.TabDrawMode.OwnerDrawFixed;
            this.tabPanel.Name = "tabPanel";
            this.tabPanel.SelectedIndex = 0;
            this.tabPanel.SizeMode = System.Windows.Forms.TabSizeMode.Fixed;
            // 
            // tabConnections
            // 
            resources.ApplyResources(this.tabConnections, "tabConnections");
            this.tabConnections.Controls.Add(this.lstConnections);
            this.tabConnections.Controls.Add(this.toolStripConnections);
            this.tabConnections.Controls.Add(this.lblConnections);
            this.tabConnections.Name = "tabConnections";
            this.tabConnections.UseVisualStyleBackColor = true;
            // 
            // lstConnections
            // 
            resources.ApplyResources(this.lstConnections, "lstConnections");
            this.lstConnections.Activation = System.Windows.Forms.ItemActivation.OneClick;
            this.lstConnections.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.lstConnections.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.connOwner,
            this.connProtocol,
            this.connLocalAddr,
            this.connLocalPort,
            this.connRemoteAddr,
            this.connRemotePort,
            this.connState,
            this.connCreaTime});
            this.lstConnections.FullRowSelect = true;
            this.lstConnections.GridLines = true;
            this.lstConnections.MultiSelect = false;
            this.lstConnections.Name = "lstConnections";
            this.lstConnections.SmallImageList = this.imgLstConn;
            this.lstConnections.UseCompatibleStateImageBehavior = false;
            this.lstConnections.View = System.Windows.Forms.View.Details;
            // 
            // connOwner
            // 
            resources.ApplyResources(this.connOwner, "connOwner");
            // 
            // connProtocol
            // 
            resources.ApplyResources(this.connProtocol, "connProtocol");
            // 
            // connLocalAddr
            // 
            resources.ApplyResources(this.connLocalAddr, "connLocalAddr");
            // 
            // connLocalPort
            // 
            resources.ApplyResources(this.connLocalPort, "connLocalPort");
            // 
            // connRemoteAddr
            // 
            resources.ApplyResources(this.connRemoteAddr, "connRemoteAddr");
            // 
            // connRemotePort
            // 
            resources.ApplyResources(this.connRemotePort, "connRemotePort");
            // 
            // connState
            // 
            resources.ApplyResources(this.connState, "connState");
            // 
            // connCreaTime
            // 
            resources.ApplyResources(this.connCreaTime, "connCreaTime");
            // 
            // imgLstConn
            // 
            this.imgLstConn.ColorDepth = System.Windows.Forms.ColorDepth.Depth32Bit;
            resources.ApplyResources(this.imgLstConn, "imgLstConn");
            this.imgLstConn.TransparentColor = System.Drawing.Color.Transparent;
            // 
            // toolStripConnections
            // 
            resources.ApplyResources(this.toolStripConnections, "toolStripConnections");
            this.toolStripConnections.BackColor = System.Drawing.Color.White;
            this.toolStripConnections.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.btnConnTrack,
            this.btnConnStop,
            this.stripConnUpdSpeed,
            this.toolStripSeparator3,
            this.btnConnFindR});
            this.toolStripConnections.Name = "toolStripConnections";
            // 
            // btnConnTrack
            // 
            resources.ApplyResources(this.btnConnTrack, "btnConnTrack");
            this.btnConnTrack.Name = "btnConnTrack";
            this.btnConnTrack.Click += new System.EventHandler(this.btnConnTrack_Click);
            // 
            // btnConnStop
            // 
            resources.ApplyResources(this.btnConnStop, "btnConnStop");
            this.btnConnStop.Name = "btnConnStop";
            this.btnConnStop.Click += new System.EventHandler(this.btnConnStop_Click);
            // 
            // stripConnUpdSpeed
            // 
            resources.ApplyResources(this.stripConnUpdSpeed, "stripConnUpdSpeed");
            this.stripConnUpdSpeed.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.stripConnUpdSpeed.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.slow5SecondsToolStripMenuItem,
            this.normal2sToolStripMenuItem,
            this.fast1sToolStripMenuItem});
            this.stripConnUpdSpeed.Name = "stripConnUpdSpeed";
            // 
            // slow5SecondsToolStripMenuItem
            // 
            resources.ApplyResources(this.slow5SecondsToolStripMenuItem, "slow5SecondsToolStripMenuItem");
            this.slow5SecondsToolStripMenuItem.CheckOnClick = true;
            this.slow5SecondsToolStripMenuItem.Name = "slow5SecondsToolStripMenuItem";
            this.slow5SecondsToolStripMenuItem.Click += new System.EventHandler(this.slow5SecondsToolStripMenuItem_Click);
            // 
            // normal2sToolStripMenuItem
            // 
            resources.ApplyResources(this.normal2sToolStripMenuItem, "normal2sToolStripMenuItem");
            this.normal2sToolStripMenuItem.Checked = true;
            this.normal2sToolStripMenuItem.CheckOnClick = true;
            this.normal2sToolStripMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
            this.normal2sToolStripMenuItem.Name = "normal2sToolStripMenuItem";
            this.normal2sToolStripMenuItem.Click += new System.EventHandler(this.normal2sToolStripMenuItem_Click);
            // 
            // fast1sToolStripMenuItem
            // 
            resources.ApplyResources(this.fast1sToolStripMenuItem, "fast1sToolStripMenuItem");
            this.fast1sToolStripMenuItem.CheckOnClick = true;
            this.fast1sToolStripMenuItem.Name = "fast1sToolStripMenuItem";
            this.fast1sToolStripMenuItem.Click += new System.EventHandler(this.fast1sToolStripMenuItem_Click);
            // 
            // toolStripSeparator3
            // 
            resources.ApplyResources(this.toolStripSeparator3, "toolStripSeparator3");
            this.toolStripSeparator3.Name = "toolStripSeparator3";
            // 
            // btnConnFindR
            // 
            resources.ApplyResources(this.btnConnFindR, "btnConnFindR");
            this.btnConnFindR.Name = "btnConnFindR";
            this.btnConnFindR.Click += new System.EventHandler(this.btnConnFindR_Click);
            // 
            // lblConnections
            // 
            resources.ApplyResources(this.lblConnections, "lblConnections");
            this.lblConnections.BackColor = System.Drawing.SystemColors.Info;
            this.lblConnections.Name = "lblConnections";
            // 
            // tabRules
            // 
            resources.ApplyResources(this.tabRules, "tabRules");
            this.tabRules.BackColor = System.Drawing.Color.White;
            this.tabRules.Controls.Add(this.gridRules);
            this.tabRules.Controls.Add(this.toolStripRules);
            this.tabRules.Controls.Add(this.lblRules);
            this.tabRules.Name = "tabRules";
            // 
            // gridRules
            // 
            resources.ApplyResources(this.gridRules, "gridRules");
            this.gridRules.AllowUserToAddRows = false;
            this.gridRules.AllowUserToDeleteRows = false;
            this.gridRules.AllowUserToResizeRows = false;
            this.gridRules.BackgroundColor = System.Drawing.Color.White;
            this.gridRules.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.gridRules.ColumnHeadersBorderStyle = System.Windows.Forms.DataGridViewHeaderBorderStyle.None;
            this.gridRules.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            this.gridRules.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.colRName,
            this.colRIcon,
            this.colRPath,
            this.colRSvc,
            this.colRDirection,
            this.colRProfile,
            this.colRAction,
            this.colRLocalport,
            this.colRTarget,
            this.colRPort,
            this.colREnabled});
            this.gridRules.MultiSelect = false;
            this.gridRules.Name = "gridRules";
            this.gridRules.ReadOnly = true;
            this.gridRules.RowHeadersVisible = false;
            this.gridRules.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.gridRules.ShowEditingIcon = false;
            // 
            // colRName
            // 
            this.colRName.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.colRName.DataPropertyName = "Name";
            this.colRName.FillWeight = 31.47455F;
            resources.ApplyResources(this.colRName, "colRName");
            this.colRName.Name = "colRName";
            this.colRName.ReadOnly = true;
            // 
            // colRIcon
            // 
            this.colRIcon.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None;
            this.colRIcon.DataPropertyName = "Icon";
            resources.ApplyResources(this.colRIcon, "colRIcon");
            this.colRIcon.Name = "colRIcon";
            this.colRIcon.ReadOnly = true;
            // 
            // colRPath
            // 
            this.colRPath.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.colRPath.DataPropertyName = "ApplicationName";
            this.colRPath.FillWeight = 31.47455F;
            resources.ApplyResources(this.colRPath, "colRPath");
            this.colRPath.Name = "colRPath";
            this.colRPath.ReadOnly = true;
            // 
            // colRSvc
            // 
            this.colRSvc.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.DisplayedCells;
            this.colRSvc.DataPropertyName = "serviceName";
            resources.ApplyResources(this.colRSvc, "colRSvc");
            this.colRSvc.Name = "colRSvc";
            this.colRSvc.ReadOnly = true;
            // 
            // colRDirection
            // 
            this.colRDirection.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None;
            this.colRDirection.DataPropertyName = "DirectionStr";
            this.colRDirection.FillWeight = 169.4915F;
            resources.ApplyResources(this.colRDirection, "colRDirection");
            this.colRDirection.Name = "colRDirection";
            this.colRDirection.ReadOnly = true;
            // 
            // colRProfile
            // 
            this.colRProfile.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
            this.colRProfile.DataPropertyName = "ProfilesStr";
            resources.ApplyResources(this.colRProfile, "colRProfile");
            this.colRProfile.Name = "colRProfile";
            this.colRProfile.ReadOnly = true;
            // 
            // colRAction
            // 
            this.colRAction.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
            this.colRAction.DataPropertyName = "ActionStr";
            this.colRAction.FillWeight = 182.7051F;
            resources.ApplyResources(this.colRAction, "colRAction");
            this.colRAction.Name = "colRAction";
            this.colRAction.ReadOnly = true;
            // 
            // colRLocalport
            // 
            this.colRLocalport.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None;
            this.colRLocalport.DataPropertyName = "LocalPorts";
            resources.ApplyResources(this.colRLocalport, "colRLocalport");
            this.colRLocalport.Name = "colRLocalport";
            this.colRLocalport.ReadOnly = true;
            // 
            // colRTarget
            // 
            this.colRTarget.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None;
            this.colRTarget.DataPropertyName = "RemoteAddresses";
            resources.ApplyResources(this.colRTarget, "colRTarget");
            this.colRTarget.Name = "colRTarget";
            this.colRTarget.ReadOnly = true;
            // 
            // colRPort
            // 
            this.colRPort.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None;
            this.colRPort.DataPropertyName = "RemotePorts";
            resources.ApplyResources(this.colRPort, "colRPort");
            this.colRPort.Name = "colRPort";
            this.colRPort.ReadOnly = true;
            // 
            // colREnabled
            // 
            this.colREnabled.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
            this.colREnabled.DataPropertyName = "Enabled";
            this.colREnabled.FillWeight = 61.95779F;
            resources.ApplyResources(this.colREnabled, "colREnabled");
            this.colREnabled.Name = "colREnabled";
            this.colREnabled.ReadOnly = true;
            // 
            // toolStripRules
            // 
            resources.ApplyResources(this.toolStripRules, "toolStripRules");
            this.toolStripRules.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.btnRDelete,
            this.toolStripSeparator2,
            this.btnRLocate,
            this.toolStripSeparator6,
            this.btnOpenConsole,
            this.btnRRefresh,
            this.toolStripSeparator8,
            this.btnRShow,
            this.toolStripSeparator7,
            this.txtFilter,
            this.toolStripLabel1});
            this.toolStripRules.Name = "toolStripRules";
            // 
            // btnRDelete
            // 
            resources.ApplyResources(this.btnRDelete, "btnRDelete");
            this.btnRDelete.Name = "btnRDelete";
            this.btnRDelete.Click += new System.EventHandler(this.btnRDelete_Click);
            // 
            // toolStripSeparator2
            // 
            resources.ApplyResources(this.toolStripSeparator2, "toolStripSeparator2");
            this.toolStripSeparator2.Name = "toolStripSeparator2";
            // 
            // btnRLocate
            // 
            resources.ApplyResources(this.btnRLocate, "btnRLocate");
            this.btnRLocate.Name = "btnRLocate";
            this.btnRLocate.Click += new System.EventHandler(this.btnRLocate_Click);
            // 
            // toolStripSeparator6
            // 
            resources.ApplyResources(this.toolStripSeparator6, "toolStripSeparator6");
            this.toolStripSeparator6.Name = "toolStripSeparator6";
            // 
            // btnOpenConsole
            // 
            resources.ApplyResources(this.btnOpenConsole, "btnOpenConsole");
            this.btnOpenConsole.Name = "btnOpenConsole";
            this.btnOpenConsole.Click += new System.EventHandler(this.btnOpenConsole_Click);
            // 
            // btnRRefresh
            // 
            resources.ApplyResources(this.btnRRefresh, "btnRRefresh");
            this.btnRRefresh.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.btnRRefresh.Name = "btnRRefresh";
            this.btnRRefresh.Click += new System.EventHandler(this.btnRRefresh_Click);
            // 
            // toolStripSeparator8
            // 
            resources.ApplyResources(this.toolStripSeparator8, "toolStripSeparator8");
            this.toolStripSeparator8.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.toolStripSeparator8.Name = "toolStripSeparator8";
            // 
            // btnRShow
            // 
            resources.ApplyResources(this.btnRShow, "btnRShow");
            this.btnRShow.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.btnRShow.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.showAllToolStripMenuItem,
            this.activeRulesOnlyToolStripMenuItem,
            this.wFNRulesOnlyToolStripMenuItem,
            this.wSHRulesOnlyToolStripMenuItem});
            this.btnRShow.Name = "btnRShow";
            // 
            // showAllToolStripMenuItem
            // 
            resources.ApplyResources(this.showAllToolStripMenuItem, "showAllToolStripMenuItem");
            this.showAllToolStripMenuItem.Checked = true;
            this.showAllToolStripMenuItem.CheckOnClick = true;
            this.showAllToolStripMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
            this.showAllToolStripMenuItem.Name = "showAllToolStripMenuItem";
            this.showAllToolStripMenuItem.Click += new System.EventHandler(this.showAllToolStripMenuItem_Click);
            // 
            // activeRulesOnlyToolStripMenuItem
            // 
            resources.ApplyResources(this.activeRulesOnlyToolStripMenuItem, "activeRulesOnlyToolStripMenuItem");
            this.activeRulesOnlyToolStripMenuItem.CheckOnClick = true;
            this.activeRulesOnlyToolStripMenuItem.Name = "activeRulesOnlyToolStripMenuItem";
            this.activeRulesOnlyToolStripMenuItem.Click += new System.EventHandler(this.activeRulesOnlyToolStripMenuItem_Click);
            // 
            // wFNRulesOnlyToolStripMenuItem
            // 
            resources.ApplyResources(this.wFNRulesOnlyToolStripMenuItem, "wFNRulesOnlyToolStripMenuItem");
            this.wFNRulesOnlyToolStripMenuItem.CheckOnClick = true;
            this.wFNRulesOnlyToolStripMenuItem.Name = "wFNRulesOnlyToolStripMenuItem";
            this.wFNRulesOnlyToolStripMenuItem.Click += new System.EventHandler(this.wFNRulesOnlyToolStripMenuItem_Click);
            // 
            // wSHRulesOnlyToolStripMenuItem
            // 
            resources.ApplyResources(this.wSHRulesOnlyToolStripMenuItem, "wSHRulesOnlyToolStripMenuItem");
            this.wSHRulesOnlyToolStripMenuItem.CheckOnClick = true;
            this.wSHRulesOnlyToolStripMenuItem.Name = "wSHRulesOnlyToolStripMenuItem";
            this.wSHRulesOnlyToolStripMenuItem.Click += new System.EventHandler(this.wSHRulesOnlyToolStripMenuItem_Click);
            // 
            // toolStripSeparator7
            // 
            resources.ApplyResources(this.toolStripSeparator7, "toolStripSeparator7");
            this.toolStripSeparator7.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.toolStripSeparator7.Name = "toolStripSeparator7";
            // 
            // txtFilter
            // 
            resources.ApplyResources(this.txtFilter, "txtFilter");
            this.txtFilter.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.txtFilter.Name = "txtFilter";
            // 
            // toolStripLabel1
            // 
            resources.ApplyResources(this.toolStripLabel1, "toolStripLabel1");
            this.toolStripLabel1.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.toolStripLabel1.Name = "toolStripLabel1";
            // 
            // tabExceptions
            // 
            resources.ApplyResources(this.tabExceptions, "tabExceptions");
            this.tabExceptions.BackColor = System.Drawing.Color.White;
            this.tabExceptions.Controls.Add(this.gridExceptions);
            this.tabExceptions.Controls.Add(this.toolStripExceptions);
            this.tabExceptions.Controls.Add(this.lblExceptions);
            this.tabExceptions.Name = "tabExceptions";
            // 
            // gridExceptions
            // 
            resources.ApplyResources(this.gridExceptions, "gridExceptions");
            this.gridExceptions.AllowUserToResizeRows = false;
            this.gridExceptions.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.gridExceptions.BackgroundColor = System.Drawing.Color.White;
            this.gridExceptions.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.gridExceptions.ColumnHeadersBorderStyle = System.Windows.Forms.DataGridViewHeaderBorderStyle.None;
            this.gridExceptions.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            this.gridExceptions.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.colExcIcon,
            this.colExcPath,
            this.colExcLocalPort,
            this.colExcTarget,
            this.colExcTargetPort});
            this.gridExceptions.Name = "gridExceptions";
            this.gridExceptions.RowHeadersVisible = false;
            this.gridExceptions.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.gridExceptions.ShowCellErrors = false;
            this.gridExceptions.ShowRowErrors = false;
            // 
            // colExcIcon
            // 
            this.colExcIcon.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None;
            this.colExcIcon.DataPropertyName = "Icon";
            dataGridViewCellStyle1.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            this.colExcIcon.DefaultCellStyle = dataGridViewCellStyle1;
            this.colExcIcon.FillWeight = 20.30457F;
            resources.ApplyResources(this.colExcIcon, "colExcIcon");
            this.colExcIcon.Name = "colExcIcon";
            this.colExcIcon.ReadOnly = true;
            this.colExcIcon.Resizable = System.Windows.Forms.DataGridViewTriState.False;
            // 
            // colExcPath
            // 
            this.colExcPath.DataPropertyName = "Path";
            dataGridViewCellStyle2.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            this.colExcPath.DefaultCellStyle = dataGridViewCellStyle2;
            this.colExcPath.FillWeight = 179.6954F;
            resources.ApplyResources(this.colExcPath, "colExcPath");
            this.colExcPath.Name = "colExcPath";
            // 
            // colExcLocalPort
            // 
            this.colExcLocalPort.DataPropertyName = "LocalPort";
            resources.ApplyResources(this.colExcLocalPort, "colExcLocalPort");
            this.colExcLocalPort.Name = "colExcLocalPort";
            // 
            // colExcTarget
            // 
            this.colExcTarget.DataPropertyName = "Target";
            resources.ApplyResources(this.colExcTarget, "colExcTarget");
            this.colExcTarget.Name = "colExcTarget";
            // 
            // colExcTargetPort
            // 
            this.colExcTargetPort.DataPropertyName = "RemotePort";
            resources.ApplyResources(this.colExcTargetPort, "colExcTargetPort");
            this.colExcTargetPort.Name = "colExcTargetPort";
            // 
            // toolStripExceptions
            // 
            resources.ApplyResources(this.toolStripExceptions, "toolStripExceptions");
            this.toolStripExceptions.BackColor = System.Drawing.Color.White;
            this.toolStripExceptions.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.btnERemove,
            this.toolStripSeparator1,
            this.btnELocate});
            this.toolStripExceptions.Name = "toolStripExceptions";
            // 
            // btnERemove
            // 
            resources.ApplyResources(this.btnERemove, "btnERemove");
            this.btnERemove.Name = "btnERemove";
            this.btnERemove.Click += new System.EventHandler(this.btnERemove_Click);
            // 
            // toolStripSeparator1
            // 
            resources.ApplyResources(this.toolStripSeparator1, "toolStripSeparator1");
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            // 
            // btnELocate
            // 
            resources.ApplyResources(this.btnELocate, "btnELocate");
            this.btnELocate.Name = "btnELocate";
            this.btnELocate.Click += new System.EventHandler(this.btnELocate_Click);
            // 
            // tabLog
            // 
            resources.ApplyResources(this.tabLog, "tabLog");
            this.tabLog.BackColor = System.Drawing.Color.White;
            this.tabLog.Controls.Add(this.gridLog);
            this.tabLog.Controls.Add(this.toolStripLog);
            this.tabLog.Controls.Add(this.lblLog);
            this.tabLog.Name = "tabLog";
            // 
            // gridLog
            // 
            resources.ApplyResources(this.gridLog, "gridLog");
            this.gridLog.AllowUserToAddRows = false;
            this.gridLog.AllowUserToDeleteRows = false;
            this.gridLog.AllowUserToResizeRows = false;
            this.gridLog.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.gridLog.BackgroundColor = System.Drawing.Color.White;
            this.gridLog.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.gridLog.ColumnHeadersBorderStyle = System.Windows.Forms.DataGridViewHeaderBorderStyle.None;
            this.gridLog.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            this.gridLog.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.colDate,
            this.colIcon,
            this.colPath,
            this.colTarget,
            this.colProtocol,
            this.colPort});
            this.gridLog.MultiSelect = false;
            this.gridLog.Name = "gridLog";
            this.gridLog.ReadOnly = true;
            this.gridLog.RowHeadersVisible = false;
            this.gridLog.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.gridLog.ShowEditingIcon = false;
            // 
            // colDate
            // 
            this.colDate.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
            this.colDate.DataPropertyName = "Date";
            resources.ApplyResources(this.colDate, "colDate");
            this.colDate.Name = "colDate";
            this.colDate.ReadOnly = true;
            // 
            // colIcon
            // 
            this.colIcon.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None;
            this.colIcon.DataPropertyName = "Icon";
            this.colIcon.FillWeight = 142.8571F;
            resources.ApplyResources(this.colIcon, "colIcon");
            this.colIcon.Name = "colIcon";
            this.colIcon.ReadOnly = true;
            this.colIcon.Resizable = System.Windows.Forms.DataGridViewTriState.False;
            // 
            // colPath
            // 
            this.colPath.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.colPath.DataPropertyName = "Path";
            this.colPath.FillWeight = 57.14285F;
            resources.ApplyResources(this.colPath, "colPath");
            this.colPath.Name = "colPath";
            this.colPath.ReadOnly = true;
            // 
            // colTarget
            // 
            this.colTarget.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
            this.colTarget.DataPropertyName = "Target";
            resources.ApplyResources(this.colTarget, "colTarget");
            this.colTarget.Name = "colTarget";
            this.colTarget.ReadOnly = true;
            // 
            // colProtocol
            // 
            this.colProtocol.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None;
            this.colProtocol.DataPropertyName = "Protocol";
            resources.ApplyResources(this.colProtocol, "colProtocol");
            this.colProtocol.Name = "colProtocol";
            this.colProtocol.ReadOnly = true;
            // 
            // colPort
            // 
            this.colPort.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
            this.colPort.DataPropertyName = "Port";
            resources.ApplyResources(this.colPort, "colPort");
            this.colPort.Name = "colPort";
            this.colPort.ReadOnly = true;
            // 
            // toolStripLog
            // 
            resources.ApplyResources(this.toolStripLog, "toolStripLog");
            this.toolStripLog.BackColor = System.Drawing.Color.White;
            this.toolStripLog.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.btnLLocate,
            this.toolStripSeparator4,
            this.btnLEvents});
            this.toolStripLog.Name = "toolStripLog";
            // 
            // btnLLocate
            // 
            resources.ApplyResources(this.btnLLocate, "btnLLocate");
            this.btnLLocate.Name = "btnLLocate";
            this.btnLLocate.Click += new System.EventHandler(this.btnLLocate_Click);
            // 
            // toolStripSeparator4
            // 
            resources.ApplyResources(this.toolStripSeparator4, "toolStripSeparator4");
            this.toolStripSeparator4.Name = "toolStripSeparator4";
            // 
            // btnLEvents
            // 
            resources.ApplyResources(this.btnLEvents, "btnLEvents");
            this.btnLEvents.Name = "btnLEvents";
            this.btnLEvents.Click += new System.EventHandler(this.btnLEvents_Click);
            // 
            // timerTrk
            // 
            this.timerTrk.Enabled = true;
            this.timerTrk.Interval = 1000;
            this.timerTrk.Tick += new System.EventHandler(this.timerTrk_Tick);
            // 
            // pnlOverTabTop
            // 
            resources.ApplyResources(this.pnlOverTabTop, "pnlOverTabTop");
            this.pnlOverTabTop.BackColor = System.Drawing.Color.WhiteSmoke;
            this.pnlOverTabTop.Controls.Add(this.btnUpdate);
            this.pnlOverTabTop.Controls.Add(this.btnTExceptions);
            this.pnlOverTabTop.Controls.Add(this.btnTLog);
            this.pnlOverTabTop.Controls.Add(this.btnTRules);
            this.pnlOverTabTop.Controls.Add(this.btnTConnections);
            this.pnlOverTabTop.Controls.Add(btnOptions);
            this.pnlOverTabTop.Controls.Add(btnDonate);
            this.pnlOverTabTop.Name = "pnlOverTabTop";
            // 
            // btnTExceptions
            // 
            resources.ApplyResources(this.btnTExceptions, "btnTExceptions");
            this.btnTExceptions.BackColor = System.Drawing.Color.White;
            this.btnTExceptions.FlatAppearance.BorderColor = System.Drawing.Color.LimeGreen;
            this.btnTExceptions.FlatAppearance.MouseOverBackColor = System.Drawing.Color.SkyBlue;
            this.btnTExceptions.Name = "btnTExceptions";
            this.btnTExceptions.Tag = "";
            this.btnTExceptions.UseVisualStyleBackColor = false;
            this.btnTExceptions.Click += new System.EventHandler(this.btnTExceptions_Click);
            // 
            // OptionsForm
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.pnlOverTabTop);
            this.Controls.Add(this.tabPanel);
            this.DoubleBuffered = true;
            this.Name = "OptionsForm";
            this.Load += new System.EventHandler(this.OptionsForm_Load);
            this.tabPanel.ResumeLayout(false);
            this.tabConnections.ResumeLayout(false);
            this.tabConnections.PerformLayout();
            this.toolStripConnections.ResumeLayout(false);
            this.toolStripConnections.PerformLayout();
            this.tabRules.ResumeLayout(false);
            this.tabRules.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.gridRules)).EndInit();
            this.toolStripRules.ResumeLayout(false);
            this.toolStripRules.PerformLayout();
            this.tabExceptions.ResumeLayout(false);
            this.tabExceptions.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.gridExceptions)).EndInit();
            this.toolStripExceptions.ResumeLayout(false);
            this.toolStripExceptions.PerformLayout();
            this.tabLog.ResumeLayout(false);
            this.tabLog.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.gridLog)).EndInit();
            this.toolStripLog.ResumeLayout(false);
            this.toolStripLog.PerformLayout();
            this.pnlOverTabTop.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TabControl tabPanel;
        private System.Windows.Forms.TabPage tabExceptions;
        private System.Windows.Forms.DataGridView gridExceptions;
        private System.Windows.Forms.TabPage tabLog;
        private System.Windows.Forms.DataGridView gridLog;
        private System.Windows.Forms.TabPage tabRules;
        private System.Windows.Forms.DataGridView gridRules;
        private System.Windows.Forms.ToolStrip toolStripRules;
        private System.Windows.Forms.ToolStrip toolStripExceptions;
        private System.Windows.Forms.ToolStrip toolStripLog;
        private System.Windows.Forms.ToolStripButton btnLLocate;
        private System.Windows.Forms.ToolStripButton btnERemove;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripButton btnELocate;
        private System.Windows.Forms.ToolStripButton btnRDelete;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
        private System.Windows.Forms.ToolStripButton btnRLocate;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator6;
        private System.Windows.Forms.ToolStripButton btnOpenConsole;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator4;
        private System.Windows.Forms.ToolStripButton btnLEvents;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator7;
        private System.Windows.Forms.ToolStripTextBox txtFilter;
        private System.Windows.Forms.ToolStripLabel toolStripLabel1;
        private System.Windows.Forms.ToolStripDropDownButton btnRShow;
        private System.Windows.Forms.ToolStripMenuItem showAllToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem activeRulesOnlyToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem wFNRulesOnlyToolStripMenuItem;
        private System.Windows.Forms.ToolStripButton btnRRefresh;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator8;
        private System.Windows.Forms.Label lblRules;
        private System.Windows.Forms.Label lblExceptions;
        private System.Windows.Forms.Label lblLog;
        private System.Windows.Forms.TabPage tabConnections;
        private System.Windows.Forms.ListView lstConnections;
        private System.Windows.Forms.ColumnHeader connOwner;
        private System.Windows.Forms.ColumnHeader connProtocol;
        private System.Windows.Forms.ColumnHeader connLocalAddr;
        private System.Windows.Forms.ColumnHeader connLocalPort;
        private System.Windows.Forms.ColumnHeader connRemoteAddr;
        private System.Windows.Forms.ColumnHeader connRemotePort;
        private System.Windows.Forms.Timer timerTrk;
        private System.Windows.Forms.ColumnHeader connCreaTime;
        private System.Windows.Forms.ToolStrip toolStripConnections;
        private System.Windows.Forms.ColumnHeader connState;
        private System.Windows.Forms.ToolStripDropDownButton stripConnUpdSpeed;
        private System.Windows.Forms.ToolStripMenuItem slow5SecondsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem normal2sToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem fast1sToolStripMenuItem;
        private System.Windows.Forms.Label lblConnections;
        private System.Windows.Forms.ToolStripButton btnConnStop;
        private System.Windows.Forms.ToolStripButton btnConnTrack;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator3;
        private System.Windows.Forms.ToolStripButton btnConnFindR;
        private System.Windows.Forms.ImageList imgLstConn;
        private System.Windows.Forms.ToolStripMenuItem wSHRulesOnlyToolStripMenuItem;
        private System.Windows.Forms.DataGridViewTextBoxColumn colRName;
        private System.Windows.Forms.DataGridViewImageColumn colRIcon;
        private System.Windows.Forms.DataGridViewTextBoxColumn colRPath;
        private System.Windows.Forms.DataGridViewTextBoxColumn colRSvc;
        private System.Windows.Forms.DataGridViewTextBoxColumn colRDirection;
        private System.Windows.Forms.DataGridViewTextBoxColumn colRProfile;
        private System.Windows.Forms.DataGridViewTextBoxColumn colRAction;
        private System.Windows.Forms.DataGridViewTextBoxColumn colRLocalport;
        private System.Windows.Forms.DataGridViewTextBoxColumn colRTarget;
        private System.Windows.Forms.DataGridViewTextBoxColumn colRPort;
        private System.Windows.Forms.DataGridViewCheckBoxColumn colREnabled;
        private System.Windows.Forms.DataGridViewTextBoxColumn colDate;
        private System.Windows.Forms.DataGridViewImageColumn colIcon;
        private System.Windows.Forms.DataGridViewTextBoxColumn colPath;
        private System.Windows.Forms.DataGridViewTextBoxColumn colTarget;
        private System.Windows.Forms.DataGridViewTextBoxColumn colProtocol;
        private System.Windows.Forms.DataGridViewTextBoxColumn colPort;
        private System.Windows.Forms.DataGridViewImageColumn colExcIcon;
        private System.Windows.Forms.DataGridViewTextBoxColumn colExcPath;
        private System.Windows.Forms.DataGridViewTextBoxColumn colExcLocalPort;
        private System.Windows.Forms.DataGridViewTextBoxColumn colExcTarget;
        private System.Windows.Forms.DataGridViewTextBoxColumn colExcTargetPort;
        private System.Windows.Forms.Panel pnlOverTabTop;
        private System.Windows.Forms.Button btnTConnections;
        private System.Windows.Forms.Button btnTRules;
        private System.Windows.Forms.Button btnTLog;
        private System.Windows.Forms.Button btnTExceptions;
        private System.Windows.Forms.Button btnUpdate;


    }
}