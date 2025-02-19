using Com.AiricLenz.XTB.Components;


namespace Com.AiricLenz.XTB.Plugin
{
    partial class BulkSolutionExporter_PluginControl
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

        #region Code généré par le Concepteur de composants

        /// <summary> 
        /// Méthode requise pour la prise en charge du concepteur - ne modifiez pas 
        /// le contenu de cette méthode avec l'éditeur de code.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(BulkSolutionExporter_PluginControl));
            this.label_versionFormat = new System.Windows.Forms.Label();
            this.textBox_versionFormat = new System.Windows.Forms.TextBox();
            this.saveFileDialog1 = new System.Windows.Forms.SaveFileDialog();
            this.fileSystemWatcher1 = new System.IO.FileSystemWatcher();
            this.tssSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.toolStripMenu = new System.Windows.Forms.ToolStrip();
            this.button_loadSolutions = new System.Windows.Forms.ToolStripButton();
            this.button_addAdditionalConnection = new System.Windows.Forms.ToolStripButton();
            this.button_manageConnections = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.button_Export = new System.Windows.Forms.ToolStripButton();
            this.button_Settings = new System.Windows.Forms.ToolStripButton();
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            this.flipSwitch_overwrite = new Com.AiricLenz.XTB.Components.FlipSwitch();
            this.flipSwitch_enableAutomation = new Com.AiricLenz.XTB.Components.FlipSwitch();
            this.flipSwitch_exportUnmanaged = new Com.AiricLenz.XTB.Components.FlipSwitch();
            this.flipSwitch_exportManaged = new Com.AiricLenz.XTB.Components.FlipSwitch();
            this.pictureBox_arrow = new System.Windows.Forms.PictureBox();
            this.pictureBox_warningUnmanaged = new System.Windows.Forms.PictureBox();
            this.pictureBox_warningManaged = new System.Windows.Forms.PictureBox();
            this.textBox_commitMessage = new System.Windows.Forms.TextBox();
            this.label_commitMessage = new System.Windows.Forms.Label();
            this.flipSwitch_publishTarget = new Com.AiricLenz.XTB.Components.FlipSwitch();
            this.flipSwitch_pushCommit = new Com.AiricLenz.XTB.Components.FlipSwitch();
            this.flipSwitch_importUnmanaged = new Com.AiricLenz.XTB.Components.FlipSwitch();
            this.flipSwitch_importManaged = new Com.AiricLenz.XTB.Components.FlipSwitch();
            this.flipSwitch_gitCommit = new Com.AiricLenz.XTB.Components.FlipSwitch();
            this.flipSwitch_publishSource = new Com.AiricLenz.XTB.Components.FlipSwitch();
            this.flipSwitch_updateVersion = new Com.AiricLenz.XTB.Components.FlipSwitch();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.panelSeparator = new System.Windows.Forms.Panel();
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.button_uncheckAll = new System.Windows.Forms.ToolStripButton();
            this.button_checkAll = new System.Windows.Forms.ToolStripButton();
            this.button_invertCheck = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator3 = new System.Windows.Forms.ToolStripSeparator();
            this.button_exportCsv = new System.Windows.Forms.ToolStripButton();
            this.listBoxSolutions = new Com.AiricLenz.XTB.Components.SortableCheckList();
            this.panel1 = new System.Windows.Forms.Panel();
            this.richTextBox_log = new System.Windows.Forms.RichTextBox();
            this.label_title = new System.Windows.Forms.Label();
            this.label_Log = new System.Windows.Forms.Label();
            this.button_browseUnmananged = new System.Windows.Forms.Button();
            this.label_unmanaged = new System.Windows.Forms.Label();
            this.button_browseManaged = new System.Windows.Forms.Button();
            this.label_Managed = new System.Windows.Forms.Label();
            this.textBox_unmanaged = new System.Windows.Forms.TextBox();
            this.textBox_managed = new System.Windows.Forms.TextBox();
            ((System.ComponentModel.ISupportInitialize)(this.fileSystemWatcher1)).BeginInit();
            this.toolStripMenu.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox_arrow)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox_warningUnmanaged)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox_warningManaged)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.toolStrip1.SuspendLayout();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // label_versionFormat
            // 
            this.label_versionFormat.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label_versionFormat.Location = new System.Drawing.Point(750, 57);
            this.label_versionFormat.Name = "label_versionFormat";
            this.label_versionFormat.Size = new System.Drawing.Size(150, 16);
            this.label_versionFormat.TabIndex = 9;
            this.label_versionFormat.Text = "Version Format";
            this.label_versionFormat.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // textBox_versionFormat
            // 
            this.textBox_versionFormat.BackColor = System.Drawing.SystemColors.ControlLightLight;
            this.textBox_versionFormat.Font = new System.Drawing.Font("Consolas", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBox_versionFormat.Location = new System.Drawing.Point(906, 53);
            this.textBox_versionFormat.Name = "textBox_versionFormat";
            this.textBox_versionFormat.Size = new System.Drawing.Size(150, 23);
            this.textBox_versionFormat.TabIndex = 8;
            this.textBox_versionFormat.Text = "YYYY.MM.DD.+";
            this.toolTip1.SetToolTip(this.textBox_versionFormat, "Allowed Tokens:\r\n\r\n#    Number with no changed applied\r\n+    Increment number by " +
        "1 \r\nYYYY    Current year\r\nMM    Current month\r\nDD    Current day\r\n");
            this.textBox_versionFormat.TextChanged += new System.EventHandler(this.textBox_versionFormat_TextChanged);
            this.textBox_versionFormat.Leave += new System.EventHandler(this.textBox_versionFormat_Leave);
            // 
            // saveFileDialog1
            // 
            this.saveFileDialog1.Filter = "Solution files (*.zip)|*.zip|All files (*.*)|*.*\"";
            this.saveFileDialog1.FilterIndex = 0;
            // 
            // fileSystemWatcher1
            // 
            this.fileSystemWatcher1.EnableRaisingEvents = true;
            this.fileSystemWatcher1.SynchronizingObject = this;
            // 
            // tssSeparator1
            // 
            this.tssSeparator1.Name = "tssSeparator1";
            this.tssSeparator1.Size = new System.Drawing.Size(6, 42);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(6, 42);
            // 
            // toolStripMenu
            // 
            this.toolStripMenu.AutoSize = false;
            this.toolStripMenu.ImageScalingSize = new System.Drawing.Size(32, 32);
            this.toolStripMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.button_loadSolutions,
            this.tssSeparator1,
            this.button_addAdditionalConnection,
            this.button_manageConnections,
            this.toolStripSeparator2,
            this.button_Export,
            this.toolStripSeparator1,
            this.button_Settings});
            this.toolStripMenu.Location = new System.Drawing.Point(0, 0);
            this.toolStripMenu.Name = "toolStripMenu";
            this.toolStripMenu.Padding = new System.Windows.Forms.Padding(0, 0, 2, 0);
            this.toolStripMenu.Size = new System.Drawing.Size(1500, 42);
            this.toolStripMenu.TabIndex = 4;
            this.toolStripMenu.Text = "toolStrip1";
            // 
            // button_loadSolutions
            // 
            this.button_loadSolutions.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.button_loadSolutions.Image = ((System.Drawing.Image)(resources.GetObject("button_loadSolutions.Image")));
            this.button_loadSolutions.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.button_loadSolutions.Margin = new System.Windows.Forms.Padding(0, 1, 10, 2);
            this.button_loadSolutions.Name = "button_loadSolutions";
            this.button_loadSolutions.Size = new System.Drawing.Size(138, 39);
            this.button_loadSolutions.Text = "  Load Solutions";
            this.button_loadSolutions.ToolTipText = "Load all unmanaged solutions from the environment...";
            this.button_loadSolutions.Click += new System.EventHandler(this.button_loadSolutions_Click);
            // 
            // button_addAdditionalConnection
            // 
            this.button_addAdditionalConnection.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.button_addAdditionalConnection.Image = ((System.Drawing.Image)(resources.GetObject("button_addAdditionalConnection.Image")));
            this.button_addAdditionalConnection.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.button_addAdditionalConnection.Margin = new System.Windows.Forms.Padding(10, 1, 10, 2);
            this.button_addAdditionalConnection.Name = "button_addAdditionalConnection";
            this.button_addAdditionalConnection.Size = new System.Drawing.Size(182, 39);
            this.button_addAdditionalConnection.Text = " Add Target Connection";
            this.button_addAdditionalConnection.ToolTipText = "  Connect to Target";
            this.button_addAdditionalConnection.Click += new System.EventHandler(this.button_addAdditionalConnection_Click);
            // 
            // button_manageConnections
            // 
            this.button_manageConnections.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.button_manageConnections.Image = global::Com.AiricLenz.XTB.Plugin.Properties.Resources.connections_32px;
            this.button_manageConnections.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.button_manageConnections.Margin = new System.Windows.Forms.Padding(10, 1, 10, 2);
            this.button_manageConnections.Name = "button_manageConnections";
            this.button_manageConnections.Size = new System.Drawing.Size(171, 39);
            this.button_manageConnections.Text = " Manage Connections";
            this.button_manageConnections.ToolTipText = "Manage all connected target environments";
            this.button_manageConnections.Click += new System.EventHandler(this.button_manageConnections_Click);
            // 
            // toolStripSeparator2
            // 
            this.toolStripSeparator2.Name = "toolStripSeparator2";
            this.toolStripSeparator2.Size = new System.Drawing.Size(6, 42);
            // 
            // button_Export
            // 
            this.button_Export.Enabled = false;
            this.button_Export.Font = new System.Drawing.Font("Segoe UI", 10.125F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.button_Export.Image = ((System.Drawing.Image)(resources.GetObject("button_Export.Image")));
            this.button_Export.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.button_Export.Margin = new System.Windows.Forms.Padding(10, 1, 10, 2);
            this.button_Export.Name = "button_Export";
            this.button_Export.Size = new System.Drawing.Size(99, 39);
            this.button_Export.Text = " Execute ";
            this.button_Export.ToolTipText = "Export the Solutions";
            this.button_Export.Click += new System.EventHandler(this.button_Export_Click);
            // 
            // button_Settings
            // 
            this.button_Settings.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.button_Settings.Font = new System.Drawing.Font("Segoe UI", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.button_Settings.Image = ((System.Drawing.Image)(resources.GetObject("button_Settings.Image")));
            this.button_Settings.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.button_Settings.Margin = new System.Windows.Forms.Padding(10, 1, 10, 2);
            this.button_Settings.Name = "button_Settings";
            this.button_Settings.Size = new System.Drawing.Size(102, 39);
            this.button_Settings.Text = " Settings";
            this.button_Settings.Click += new System.EventHandler(this.button_Settings_Click);
            // 
            // flipSwitch_overwrite
            // 
            this.flipSwitch_overwrite.ColorOff = System.Drawing.Color.FromArgb(((int)(((byte)(180)))), ((int)(((byte)(180)))), ((int)(((byte)(180)))));
            this.flipSwitch_overwrite.ColorOn = System.Drawing.Color.MediumSlateBlue;
            this.flipSwitch_overwrite.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.flipSwitch_overwrite.IsLocked = false;
            this.flipSwitch_overwrite.IsOn = false;
            this.flipSwitch_overwrite.Location = new System.Drawing.Point(300, 130);
            this.flipSwitch_overwrite.Margin = new System.Windows.Forms.Padding(4);
            this.flipSwitch_overwrite.MarginText = 2;
            this.flipSwitch_overwrite.Name = "flipSwitch_overwrite";
            this.flipSwitch_overwrite.Size = new System.Drawing.Size(193, 20);
            this.flipSwitch_overwrite.SwitchHeight = 18;
            this.flipSwitch_overwrite.SwitchWidth = 30;
            this.flipSwitch_overwrite.TabIndex = 24;
            this.flipSwitch_overwrite.TextOnLeftSide = false;
            this.flipSwitch_overwrite.Title = "Overwrite Customizations";
            this.toolTip1.SetToolTip(this.flipSwitch_overwrite, "Overwrite local customizations with the solution component(s) if existing in the " +
        "target environment.");
            this.flipSwitch_overwrite.Toggled += new System.EventHandler(this.flipSwitch_overwrite_Toggled);
            // 
            // flipSwitch_enableAutomation
            // 
            this.flipSwitch_enableAutomation.ColorOff = System.Drawing.Color.FromArgb(((int)(((byte)(180)))), ((int)(((byte)(180)))), ((int)(((byte)(180)))));
            this.flipSwitch_enableAutomation.ColorOn = System.Drawing.Color.MediumSlateBlue;
            this.flipSwitch_enableAutomation.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.flipSwitch_enableAutomation.IsLocked = false;
            this.flipSwitch_enableAutomation.IsOn = true;
            this.flipSwitch_enableAutomation.Location = new System.Drawing.Point(300, 105);
            this.flipSwitch_enableAutomation.Margin = new System.Windows.Forms.Padding(4);
            this.flipSwitch_enableAutomation.MarginText = 2;
            this.flipSwitch_enableAutomation.Name = "flipSwitch_enableAutomation";
            this.flipSwitch_enableAutomation.Size = new System.Drawing.Size(154, 20);
            this.flipSwitch_enableAutomation.SwitchHeight = 18;
            this.flipSwitch_enableAutomation.SwitchWidth = 30;
            this.flipSwitch_enableAutomation.TabIndex = 23;
            this.flipSwitch_enableAutomation.TextOnLeftSide = false;
            this.flipSwitch_enableAutomation.Title = "Enable Automation";
            this.toolTip1.SetToolTip(this.flipSwitch_enableAutomation, "Enable Workflows and Plugin-Steps after import of the solution.");
            this.flipSwitch_enableAutomation.Toggled += new System.EventHandler(this.flipSwitch_enableAutomation_Toggled);
            // 
            // flipSwitch_exportUnmanaged
            // 
            this.flipSwitch_exportUnmanaged.ColorOff = System.Drawing.Color.FromArgb(((int)(((byte)(180)))), ((int)(((byte)(180)))), ((int)(((byte)(180)))));
            this.flipSwitch_exportUnmanaged.ColorOn = System.Drawing.Color.MediumSlateBlue;
            this.flipSwitch_exportUnmanaged.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.flipSwitch_exportUnmanaged.IsLocked = false;
            this.flipSwitch_exportUnmanaged.IsOn = false;
            this.flipSwitch_exportUnmanaged.Location = new System.Drawing.Point(15, 130);
            this.flipSwitch_exportUnmanaged.Margin = new System.Windows.Forms.Padding(4);
            this.flipSwitch_exportUnmanaged.MarginText = 7;
            this.flipSwitch_exportUnmanaged.Name = "flipSwitch_exportUnmanaged";
            this.flipSwitch_exportUnmanaged.Size = new System.Drawing.Size(161, 20);
            this.flipSwitch_exportUnmanaged.SwitchHeight = 18;
            this.flipSwitch_exportUnmanaged.SwitchWidth = 30;
            this.flipSwitch_exportUnmanaged.TabIndex = 11;
            this.flipSwitch_exportUnmanaged.TextOnLeftSide = false;
            this.flipSwitch_exportUnmanaged.Title = "Export Unmanaged";
            this.toolTip1.SetToolTip(this.flipSwitch_exportUnmanaged, "Enter a file location for the unmanaged solutions that you want to export");
            this.flipSwitch_exportUnmanaged.Toggled += new System.EventHandler(this.flipSwitch_exportUnmanaged_Toggled);
            // 
            // flipSwitch_exportManaged
            // 
            this.flipSwitch_exportManaged.ColorOff = System.Drawing.Color.FromArgb(((int)(((byte)(180)))), ((int)(((byte)(180)))), ((int)(((byte)(180)))));
            this.flipSwitch_exportManaged.ColorOn = System.Drawing.Color.MediumSlateBlue;
            this.flipSwitch_exportManaged.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.flipSwitch_exportManaged.IsLocked = false;
            this.flipSwitch_exportManaged.IsOn = false;
            this.flipSwitch_exportManaged.Location = new System.Drawing.Point(15, 105);
            this.flipSwitch_exportManaged.Margin = new System.Windows.Forms.Padding(4);
            this.flipSwitch_exportManaged.MarginText = 7;
            this.flipSwitch_exportManaged.Name = "flipSwitch_exportManaged";
            this.flipSwitch_exportManaged.Size = new System.Drawing.Size(144, 20);
            this.flipSwitch_exportManaged.SwitchHeight = 18;
            this.flipSwitch_exportManaged.SwitchWidth = 30;
            this.flipSwitch_exportManaged.TabIndex = 10;
            this.flipSwitch_exportManaged.TextOnLeftSide = false;
            this.flipSwitch_exportManaged.Title = "Export Managed";
            this.toolTip1.SetToolTip(this.flipSwitch_exportManaged, "Enter a file location for the managed solutions that you want to export");
            this.flipSwitch_exportManaged.Toggled += new System.EventHandler(this.flipSwitch_exportManaged_Toggled);
            // 
            // pictureBox_arrow
            // 
            this.pictureBox_arrow.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("pictureBox_arrow.BackgroundImage")));
            this.pictureBox_arrow.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.pictureBox_arrow.ErrorImage = null;
            this.pictureBox_arrow.InitialImage = ((System.Drawing.Image)(resources.GetObject("pictureBox_arrow.InitialImage")));
            this.pictureBox_arrow.Location = new System.Drawing.Point(210, 77);
            this.pictureBox_arrow.Name = "pictureBox_arrow";
            this.pictureBox_arrow.Size = new System.Drawing.Size(48, 48);
            this.pictureBox_arrow.TabIndex = 16;
            this.pictureBox_arrow.TabStop = false;
            this.toolTip1.SetToolTip(this.pictureBox_arrow, "Allowed Tokens:\r\n\r\n#    Number with no changed applied\r\n+    Increment number by " +
        "1 \r\nYYYY    Current year\r\nMM    Current month\r\nDD    Current day");
            // 
            // pictureBox_warningUnmanaged
            // 
            this.pictureBox_warningUnmanaged.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.pictureBox_warningUnmanaged.BackColor = System.Drawing.SystemColors.ControlLightLight;
            this.pictureBox_warningUnmanaged.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("pictureBox_warningUnmanaged.BackgroundImage")));
            this.pictureBox_warningUnmanaged.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Center;
            this.pictureBox_warningUnmanaged.ErrorImage = null;
            this.pictureBox_warningUnmanaged.InitialImage = null;
            this.pictureBox_warningUnmanaged.Location = new System.Drawing.Point(780, 103);
            this.pictureBox_warningUnmanaged.Name = "pictureBox_warningUnmanaged";
            this.pictureBox_warningUnmanaged.Size = new System.Drawing.Size(30, 20);
            this.pictureBox_warningUnmanaged.TabIndex = 26;
            this.pictureBox_warningUnmanaged.TabStop = false;
            this.toolTip1.SetToolTip(this.pictureBox_warningUnmanaged, "The file was not exported yet or is out-dated!");
            this.pictureBox_warningUnmanaged.Visible = false;
            // 
            // pictureBox_warningManaged
            // 
            this.pictureBox_warningManaged.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.pictureBox_warningManaged.BackColor = System.Drawing.SystemColors.ControlLightLight;
            this.pictureBox_warningManaged.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("pictureBox_warningManaged.BackgroundImage")));
            this.pictureBox_warningManaged.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Center;
            this.pictureBox_warningManaged.ErrorImage = null;
            this.pictureBox_warningManaged.InitialImage = null;
            this.pictureBox_warningManaged.Location = new System.Drawing.Point(780, 55);
            this.pictureBox_warningManaged.Name = "pictureBox_warningManaged";
            this.pictureBox_warningManaged.Size = new System.Drawing.Size(30, 20);
            this.pictureBox_warningManaged.TabIndex = 9;
            this.pictureBox_warningManaged.TabStop = false;
            this.toolTip1.SetToolTip(this.pictureBox_warningManaged, "The file was not exported yet or is out-dated!");
            this.pictureBox_warningManaged.Visible = false;
            // 
            // textBox_commitMessage
            // 
            this.textBox_commitMessage.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.textBox_commitMessage.BackColor = System.Drawing.SystemColors.ControlLightLight;
            this.textBox_commitMessage.Font = new System.Drawing.Font("Consolas", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBox_commitMessage.Location = new System.Drawing.Point(906, 104);
            this.textBox_commitMessage.Multiline = true;
            this.textBox_commitMessage.Name = "textBox_commitMessage";
            this.textBox_commitMessage.Size = new System.Drawing.Size(584, 47);
            this.textBox_commitMessage.TabIndex = 20;
            this.textBox_commitMessage.Text = "XTB Commit";
            this.textBox_commitMessage.TextChanged += new System.EventHandler(this.textBox_commitMessage_TextChanged);
            this.textBox_commitMessage.Leave += new System.EventHandler(this.textBox_commitMessage_Leave);
            // 
            // label_commitMessage
            // 
            this.label_commitMessage.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label_commitMessage.Location = new System.Drawing.Point(750, 107);
            this.label_commitMessage.Name = "label_commitMessage";
            this.label_commitMessage.Size = new System.Drawing.Size(150, 16);
            this.label_commitMessage.TabIndex = 22;
            this.label_commitMessage.Text = "Commit Message";
            this.label_commitMessage.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // flipSwitch_publishTarget
            // 
            this.flipSwitch_publishTarget.ColorOff = System.Drawing.Color.FromArgb(((int)(((byte)(180)))), ((int)(((byte)(180)))), ((int)(((byte)(180)))));
            this.flipSwitch_publishTarget.ColorOn = System.Drawing.Color.MediumSlateBlue;
            this.flipSwitch_publishTarget.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.flipSwitch_publishTarget.IsLocked = false;
            this.flipSwitch_publishTarget.IsOn = false;
            this.flipSwitch_publishTarget.Location = new System.Drawing.Point(550, 55);
            this.flipSwitch_publishTarget.Margin = new System.Windows.Forms.Padding(4);
            this.flipSwitch_publishTarget.MarginText = 7;
            this.flipSwitch_publishTarget.Name = "flipSwitch_publishTarget";
            this.flipSwitch_publishTarget.Size = new System.Drawing.Size(160, 20);
            this.flipSwitch_publishTarget.SwitchHeight = 18;
            this.flipSwitch_publishTarget.SwitchWidth = 30;
            this.flipSwitch_publishTarget.TabIndex = 25;
            this.flipSwitch_publishTarget.TextOnLeftSide = false;
            this.flipSwitch_publishTarget.Title = "Publish All (Target)";
            this.flipSwitch_publishTarget.Toggled += new System.EventHandler(this.flipSwitch_publishTarget_Toggled);
            // 
            // flipSwitch_pushCommit
            // 
            this.flipSwitch_pushCommit.ColorOff = System.Drawing.Color.FromArgb(((int)(((byte)(180)))), ((int)(((byte)(180)))), ((int)(((byte)(180)))));
            this.flipSwitch_pushCommit.ColorOn = System.Drawing.Color.MediumSlateBlue;
            this.flipSwitch_pushCommit.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.flipSwitch_pushCommit.IsLocked = false;
            this.flipSwitch_pushCommit.IsOn = false;
            this.flipSwitch_pushCommit.Location = new System.Drawing.Point(550, 130);
            this.flipSwitch_pushCommit.Margin = new System.Windows.Forms.Padding(4);
            this.flipSwitch_pushCommit.MarginText = 7;
            this.flipSwitch_pushCommit.Name = "flipSwitch_pushCommit";
            this.flipSwitch_pushCommit.Size = new System.Drawing.Size(126, 20);
            this.flipSwitch_pushCommit.SwitchHeight = 18;
            this.flipSwitch_pushCommit.SwitchWidth = 30;
            this.flipSwitch_pushCommit.TabIndex = 19;
            this.flipSwitch_pushCommit.TextOnLeftSide = false;
            this.flipSwitch_pushCommit.Title = "Push Commit";
            this.flipSwitch_pushCommit.Toggled += new System.EventHandler(this.flipSwitch_pushCommit_Toggled);
            // 
            // flipSwitch_importUnmanaged
            // 
            this.flipSwitch_importUnmanaged.ColorOff = System.Drawing.Color.FromArgb(((int)(((byte)(180)))), ((int)(((byte)(180)))), ((int)(((byte)(180)))));
            this.flipSwitch_importUnmanaged.ColorOn = System.Drawing.Color.MediumSlateBlue;
            this.flipSwitch_importUnmanaged.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.flipSwitch_importUnmanaged.IsLocked = false;
            this.flipSwitch_importUnmanaged.IsOn = false;
            this.flipSwitch_importUnmanaged.Location = new System.Drawing.Point(300, 80);
            this.flipSwitch_importUnmanaged.Margin = new System.Windows.Forms.Padding(4);
            this.flipSwitch_importUnmanaged.MarginText = 7;
            this.flipSwitch_importUnmanaged.Name = "flipSwitch_importUnmanaged";
            this.flipSwitch_importUnmanaged.Size = new System.Drawing.Size(160, 20);
            this.flipSwitch_importUnmanaged.SwitchHeight = 18;
            this.flipSwitch_importUnmanaged.SwitchWidth = 30;
            this.flipSwitch_importUnmanaged.TabIndex = 18;
            this.flipSwitch_importUnmanaged.TextOnLeftSide = false;
            this.flipSwitch_importUnmanaged.Title = "Import Unmanaged";
            this.flipSwitch_importUnmanaged.Toggled += new System.EventHandler(this.flipSwitch_importUnmanaged_Toggled);
            // 
            // flipSwitch_importManaged
            // 
            this.flipSwitch_importManaged.ColorOff = System.Drawing.Color.FromArgb(((int)(((byte)(180)))), ((int)(((byte)(180)))), ((int)(((byte)(180)))));
            this.flipSwitch_importManaged.ColorOn = System.Drawing.Color.MediumSlateBlue;
            this.flipSwitch_importManaged.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.flipSwitch_importManaged.IsLocked = false;
            this.flipSwitch_importManaged.IsOn = false;
            this.flipSwitch_importManaged.Location = new System.Drawing.Point(300, 55);
            this.flipSwitch_importManaged.Margin = new System.Windows.Forms.Padding(4);
            this.flipSwitch_importManaged.MarginText = 7;
            this.flipSwitch_importManaged.Name = "flipSwitch_importManaged";
            this.flipSwitch_importManaged.Size = new System.Drawing.Size(143, 20);
            this.flipSwitch_importManaged.SwitchHeight = 18;
            this.flipSwitch_importManaged.SwitchWidth = 30;
            this.flipSwitch_importManaged.TabIndex = 17;
            this.flipSwitch_importManaged.TextOnLeftSide = false;
            this.flipSwitch_importManaged.Title = "Import Managed";
            this.flipSwitch_importManaged.Toggled += new System.EventHandler(this.flipSwitch_importManaged_Toggled);
            // 
            // flipSwitch_gitCommit
            // 
            this.flipSwitch_gitCommit.ColorOff = System.Drawing.Color.FromArgb(((int)(((byte)(180)))), ((int)(((byte)(180)))), ((int)(((byte)(180)))));
            this.flipSwitch_gitCommit.ColorOn = System.Drawing.Color.MediumSlateBlue;
            this.flipSwitch_gitCommit.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.flipSwitch_gitCommit.IsLocked = false;
            this.flipSwitch_gitCommit.IsOn = false;
            this.flipSwitch_gitCommit.Location = new System.Drawing.Point(550, 105);
            this.flipSwitch_gitCommit.Margin = new System.Windows.Forms.Padding(4);
            this.flipSwitch_gitCommit.MarginText = 7;
            this.flipSwitch_gitCommit.Name = "flipSwitch_gitCommit";
            this.flipSwitch_gitCommit.Size = new System.Drawing.Size(157, 20);
            this.flipSwitch_gitCommit.SwitchHeight = 18;
            this.flipSwitch_gitCommit.SwitchWidth = 30;
            this.flipSwitch_gitCommit.TabIndex = 15;
            this.flipSwitch_gitCommit.TextOnLeftSide = false;
            this.flipSwitch_gitCommit.Title = "Create Git Commit";
            this.flipSwitch_gitCommit.Toggled += new System.EventHandler(this.flipSwitch_gitCommit_Toggled);
            // 
            // flipSwitch_publishSource
            // 
            this.flipSwitch_publishSource.ColorOff = System.Drawing.Color.FromArgb(((int)(((byte)(180)))), ((int)(((byte)(180)))), ((int)(((byte)(180)))));
            this.flipSwitch_publishSource.ColorOn = System.Drawing.Color.MediumSlateBlue;
            this.flipSwitch_publishSource.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.flipSwitch_publishSource.IsLocked = false;
            this.flipSwitch_publishSource.IsOn = false;
            this.flipSwitch_publishSource.Location = new System.Drawing.Point(15, 55);
            this.flipSwitch_publishSource.Margin = new System.Windows.Forms.Padding(4);
            this.flipSwitch_publishSource.MarginText = 7;
            this.flipSwitch_publishSource.Name = "flipSwitch_publishSource";
            this.flipSwitch_publishSource.Size = new System.Drawing.Size(163, 20);
            this.flipSwitch_publishSource.SwitchHeight = 18;
            this.flipSwitch_publishSource.SwitchWidth = 30;
            this.flipSwitch_publishSource.TabIndex = 14;
            this.flipSwitch_publishSource.TextOnLeftSide = false;
            this.flipSwitch_publishSource.Title = "Publish All (Source)";
            this.flipSwitch_publishSource.Toggled += new System.EventHandler(this.flipSwitch_publish_Toggled);
            // 
            // flipSwitch_updateVersion
            // 
            this.flipSwitch_updateVersion.ColorOff = System.Drawing.Color.FromArgb(((int)(((byte)(180)))), ((int)(((byte)(180)))), ((int)(((byte)(180)))));
            this.flipSwitch_updateVersion.ColorOn = System.Drawing.Color.MediumSlateBlue;
            this.flipSwitch_updateVersion.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.flipSwitch_updateVersion.IsLocked = false;
            this.flipSwitch_updateVersion.IsOn = false;
            this.flipSwitch_updateVersion.Location = new System.Drawing.Point(15, 80);
            this.flipSwitch_updateVersion.Margin = new System.Windows.Forms.Padding(4);
            this.flipSwitch_updateVersion.MarginText = 7;
            this.flipSwitch_updateVersion.Name = "flipSwitch_updateVersion";
            this.flipSwitch_updateVersion.Size = new System.Drawing.Size(154, 20);
            this.flipSwitch_updateVersion.SwitchHeight = 18;
            this.flipSwitch_updateVersion.SwitchWidth = 30;
            this.flipSwitch_updateVersion.TabIndex = 12;
            this.flipSwitch_updateVersion.TextOnLeftSide = false;
            this.flipSwitch_updateVersion.Title = "Update Version(s)";
            this.flipSwitch_updateVersion.Toggled += new System.EventHandler(this.flipSwitch_updateVersion_Toggled);
            // 
            // splitContainer1
            // 
            this.splitContainer1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.splitContainer1.Location = new System.Drawing.Point(3, 177);
            this.splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.panelSeparator);
            this.splitContainer1.Panel1.Controls.Add(this.toolStrip1);
            this.splitContainer1.Panel1.Controls.Add(this.listBoxSolutions);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.panel1);
            this.splitContainer1.Panel2.Controls.Add(this.pictureBox_warningUnmanaged);
            this.splitContainer1.Panel2.Controls.Add(this.pictureBox_warningManaged);
            this.splitContainer1.Panel2.Controls.Add(this.label_title);
            this.splitContainer1.Panel2.Controls.Add(this.label_Log);
            this.splitContainer1.Panel2.Controls.Add(this.button_browseUnmananged);
            this.splitContainer1.Panel2.Controls.Add(this.label_unmanaged);
            this.splitContainer1.Panel2.Controls.Add(this.button_browseManaged);
            this.splitContainer1.Panel2.Controls.Add(this.label_Managed);
            this.splitContainer1.Panel2.Controls.Add(this.textBox_unmanaged);
            this.splitContainer1.Panel2.Controls.Add(this.textBox_managed);
            this.splitContainer1.Size = new System.Drawing.Size(1494, 620);
            this.splitContainer1.SplitterDistance = 671;
            this.splitContainer1.TabIndex = 6;
            // 
            // panelSeparator
            // 
            this.panelSeparator.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.panelSeparator.BackColor = System.Drawing.SystemColors.ControlDark;
            this.panelSeparator.Location = new System.Drawing.Point(0, 28);
            this.panelSeparator.Name = "panelSeparator";
            this.panelSeparator.Size = new System.Drawing.Size(668, 2);
            this.panelSeparator.TabIndex = 9;
            // 
            // toolStrip1
            // 
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.button_uncheckAll,
            this.button_checkAll,
            this.button_invertCheck,
            this.toolStripSeparator3,
            this.button_exportCsv});
            this.toolStrip1.Location = new System.Drawing.Point(0, 0);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(671, 25);
            this.toolStrip1.TabIndex = 8;
            this.toolStrip1.Text = "toolStrip1";
            // 
            // button_uncheckAll
            // 
            this.button_uncheckAll.Image = ((System.Drawing.Image)(resources.GetObject("button_uncheckAll.Image")));
            this.button_uncheckAll.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.button_uncheckAll.Name = "button_uncheckAll";
            this.button_uncheckAll.Size = new System.Drawing.Size(97, 22);
            this.button_uncheckAll.Text = "Un-Check All";
            this.button_uncheckAll.Click += new System.EventHandler(this.button_uncheckAll_Click);
            // 
            // button_checkAll
            // 
            this.button_checkAll.Image = ((System.Drawing.Image)(resources.GetObject("button_checkAll.Image")));
            this.button_checkAll.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.button_checkAll.Name = "button_checkAll";
            this.button_checkAll.Size = new System.Drawing.Size(77, 22);
            this.button_checkAll.Text = "Check All";
            this.button_checkAll.Click += new System.EventHandler(this.button_checkAll_Click);
            // 
            // button_invertCheck
            // 
            this.button_invertCheck.Image = ((System.Drawing.Image)(resources.GetObject("button_invertCheck.Image")));
            this.button_invertCheck.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.button_invertCheck.Name = "button_invertCheck";
            this.button_invertCheck.Size = new System.Drawing.Size(93, 22);
            this.button_invertCheck.Text = "Invert Check";
            this.button_invertCheck.Click += new System.EventHandler(this.button_invertCheck_Click);
            // 
            // toolStripSeparator3
            // 
            this.toolStripSeparator3.Margin = new System.Windows.Forms.Padding(10, 0, 10, 0);
            this.toolStripSeparator3.Name = "toolStripSeparator3";
            this.toolStripSeparator3.Size = new System.Drawing.Size(6, 25);
            // 
            // button_exportCsv
            // 
            this.button_exportCsv.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.button_exportCsv.Image = global::Com.AiricLenz.XTB.Plugin.Properties.Resources.export_16px;
            this.button_exportCsv.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.button_exportCsv.Name = "button_exportCsv";
            this.button_exportCsv.Size = new System.Drawing.Size(23, 22);
            this.button_exportCsv.Text = "Export to CSV";
            this.button_exportCsv.Click += new System.EventHandler(this.button_exportCsv_Click);
            // 
            // listBoxSolutions
            // 
            this.listBoxSolutions.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.listBoxSolutions.BackColor = System.Drawing.SystemColors.Window;
            this.listBoxSolutions.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("listBoxSolutions.BackgroundImage")));
            this.listBoxSolutions.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Center;
            this.listBoxSolutions.BorderColor = System.Drawing.SystemColors.ControlDark;
            this.listBoxSolutions.BorderThickness = 1F;
            this.listBoxSolutions.CheckBoxMargin = 4;
            this.listBoxSolutions.CheckBoxRadius = 18;
            this.listBoxSolutions.CheckBoxSize = 18;
            this.listBoxSolutions.ColorChecked = System.Drawing.Color.MediumSlateBlue;
            this.listBoxSolutions.ColorUnchecked = System.Drawing.Color.FromArgb(((int)(((byte)(200)))), ((int)(((byte)(200)))), ((int)(((byte)(200)))));
            this.listBoxSolutions.ColumnsJson = "[]";
            this.listBoxSolutions.DragBurgerLineThickness = 1.5F;
            this.listBoxSolutions.DragBurgerSize = 11;
            this.listBoxSolutions.Font = new System.Drawing.Font("Consolas", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.listBoxSolutions.IsCheckable = true;
            this.listBoxSolutions.IsSortable = true;
            this.listBoxSolutions.ItemHeigth = 24;
            this.listBoxSolutions.Location = new System.Drawing.Point(3, 35);
            this.listBoxSolutions.Name = "listBoxSolutions";
            this.listBoxSolutions.ShowScrollBar = true;
            this.listBoxSolutions.ShowTooltips = true;
            this.listBoxSolutions.Size = new System.Drawing.Size(665, 580);
            this.listBoxSolutions.TabIndex = 7;
            this.listBoxSolutions.Text = "sortableCheckList1";
            this.listBoxSolutions.SelectedIndexChanged += new System.EventHandler(this.listSolutions_SelectedIndexChanged);
            this.listBoxSolutions.ItemChecked += new System.EventHandler(this.listSolutions_ItemCheck);
            this.listBoxSolutions.ItemOrderChanged += new System.EventHandler(this.listBoxSolutions_ItemOrderChanged);
            this.listBoxSolutions.SortingColumnChanged += new System.EventHandler(this.listBoxSolutions_SortingColumnChanged);
            // 
            // panel1
            // 
            this.panel1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.panel1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panel1.Controls.Add(this.richTextBox_log);
            this.panel1.Location = new System.Drawing.Point(15, 156);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(795, 459);
            this.panel1.TabIndex = 28;
            // 
            // richTextBox_log
            // 
            this.richTextBox_log.BackColor = System.Drawing.SystemColors.Window;
            this.richTextBox_log.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.richTextBox_log.Dock = System.Windows.Forms.DockStyle.Fill;
            this.richTextBox_log.Font = new System.Drawing.Font("Consolas", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.richTextBox_log.Location = new System.Drawing.Point(0, 0);
            this.richTextBox_log.Name = "richTextBox_log";
            this.richTextBox_log.ReadOnly = true;
            this.richTextBox_log.Size = new System.Drawing.Size(793, 457);
            this.richTextBox_log.TabIndex = 27;
            this.richTextBox_log.Text = "";
            // 
            // label_title
            // 
            this.label_title.AutoSize = true;
            this.label_title.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label_title.Location = new System.Drawing.Point(10, 3);
            this.label_title.Name = "label_title";
            this.label_title.Size = new System.Drawing.Size(369, 18);
            this.label_title.TabIndex = 8;
            this.label_title.Text = "No Solution selected (click a solution in the list)";
            // 
            // label_Log
            // 
            this.label_Log.AutoSize = true;
            this.label_Log.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label_Log.Location = new System.Drawing.Point(12, 137);
            this.label_Log.Name = "label_Log";
            this.label_Log.Size = new System.Drawing.Size(30, 16);
            this.label_Log.TabIndex = 7;
            this.label_Log.Text = "Log";
            // 
            // button_browseUnmananged
            // 
            this.button_browseUnmananged.Enabled = false;
            this.button_browseUnmananged.Location = new System.Drawing.Point(12, 100);
            this.button_browseUnmananged.Name = "button_browseUnmananged";
            this.button_browseUnmananged.Size = new System.Drawing.Size(84, 25);
            this.button_browseUnmananged.TabIndex = 5;
            this.button_browseUnmananged.Text = "Browse";
            this.button_browseUnmananged.UseVisualStyleBackColor = true;
            this.button_browseUnmananged.Click += new System.EventHandler(this.button_browseUnmananged_Click);
            // 
            // label_unmanaged
            // 
            this.label_unmanaged.AutoSize = true;
            this.label_unmanaged.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label_unmanaged.Location = new System.Drawing.Point(12, 83);
            this.label_unmanaged.Name = "label_unmanaged";
            this.label_unmanaged.Size = new System.Drawing.Size(212, 16);
            this.label_unmanaged.TabIndex = 4;
            this.label_unmanaged.Text = "File Location Unmanaged Solution";
            // 
            // button_browseManaged
            // 
            this.button_browseManaged.Enabled = false;
            this.button_browseManaged.Location = new System.Drawing.Point(12, 53);
            this.button_browseManaged.Name = "button_browseManaged";
            this.button_browseManaged.Size = new System.Drawing.Size(84, 25);
            this.button_browseManaged.TabIndex = 3;
            this.button_browseManaged.Text = "Browse";
            this.button_browseManaged.UseVisualStyleBackColor = true;
            this.button_browseManaged.Click += new System.EventHandler(this.button_browseManaged_Click);
            // 
            // label_Managed
            // 
            this.label_Managed.AutoSize = true;
            this.label_Managed.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label_Managed.Location = new System.Drawing.Point(12, 35);
            this.label_Managed.Name = "label_Managed";
            this.label_Managed.Size = new System.Drawing.Size(195, 16);
            this.label_Managed.TabIndex = 2;
            this.label_Managed.Text = "File Location Managed Solution";
            // 
            // textBox_unmanaged
            // 
            this.textBox_unmanaged.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.textBox_unmanaged.BackColor = System.Drawing.SystemColors.ControlLightLight;
            this.textBox_unmanaged.Font = new System.Drawing.Font("Consolas", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBox_unmanaged.Location = new System.Drawing.Point(107, 101);
            this.textBox_unmanaged.Name = "textBox_unmanaged";
            this.textBox_unmanaged.Size = new System.Drawing.Size(705, 23);
            this.textBox_unmanaged.TabIndex = 1;
            this.textBox_unmanaged.TextChanged += new System.EventHandler(this.textBox_unmanaged_TextChanged);
            // 
            // textBox_managed
            // 
            this.textBox_managed.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.textBox_managed.BackColor = System.Drawing.SystemColors.ControlLightLight;
            this.textBox_managed.Font = new System.Drawing.Font("Consolas", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBox_managed.Location = new System.Drawing.Point(107, 53);
            this.textBox_managed.Name = "textBox_managed";
            this.textBox_managed.Size = new System.Drawing.Size(705, 23);
            this.textBox_managed.TabIndex = 0;
            this.textBox_managed.TextChanged += new System.EventHandler(this.textBox_managed_TextChanged);
            // 
            // BulkSolutionExporter_PluginControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.flipSwitch_publishTarget);
            this.Controls.Add(this.flipSwitch_overwrite);
            this.Controls.Add(this.flipSwitch_enableAutomation);
            this.Controls.Add(this.label_commitMessage);
            this.Controls.Add(this.textBox_commitMessage);
            this.Controls.Add(this.flipSwitch_pushCommit);
            this.Controls.Add(this.flipSwitch_importUnmanaged);
            this.Controls.Add(this.flipSwitch_importManaged);
            this.Controls.Add(this.pictureBox_arrow);
            this.Controls.Add(this.flipSwitch_gitCommit);
            this.Controls.Add(this.flipSwitch_publishSource);
            this.Controls.Add(this.label_versionFormat);
            this.Controls.Add(this.flipSwitch_updateVersion);
            this.Controls.Add(this.textBox_versionFormat);
            this.Controls.Add(this.flipSwitch_exportUnmanaged);
            this.Controls.Add(this.flipSwitch_exportManaged);
            this.Controls.Add(this.splitContainer1);
            this.Controls.Add(this.toolStripMenu);
            this.Name = "BulkSolutionExporter_PluginControl";
            this.Size = new System.Drawing.Size(1500, 800);
            this.Load += new System.EventHandler(this.OnPluginControl_Load);
            ((System.ComponentModel.ISupportInitialize)(this.fileSystemWatcher1)).EndInit();
            this.toolStripMenu.ResumeLayout(false);
            this.toolStripMenu.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox_arrow)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox_warningUnmanaged)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox_warningManaged)).EndInit();
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel1.PerformLayout();
            this.splitContainer1.Panel2.ResumeLayout(false);
            this.splitContainer1.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            this.panel1.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.TextBox textBox_unmanaged;
        private System.Windows.Forms.TextBox textBox_managed;
        private System.Windows.Forms.Button button_browseUnmananged;
        private System.Windows.Forms.Label label_unmanaged;
        private System.Windows.Forms.Button button_browseManaged;
        private System.Windows.Forms.Label label_Managed;
        private System.Windows.Forms.Label label_Log;
        private System.Windows.Forms.SaveFileDialog saveFileDialog1;
        private System.IO.FileSystemWatcher fileSystemWatcher1;
        private FlipSwitch flipSwitch_exportManaged;
        private FlipSwitch flipSwitch_exportUnmanaged;
        private FlipSwitch flipSwitch_updateVersion;
        private System.Windows.Forms.Label label_versionFormat;
        private System.Windows.Forms.TextBox textBox_versionFormat;
        private System.Windows.Forms.ToolStrip toolStripMenu;
        private System.Windows.Forms.ToolStripButton button_loadSolutions;
        private System.Windows.Forms.ToolStripSeparator tssSeparator1;
        private System.Windows.Forms.ToolStripButton button_Export;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolTip toolTip1;
        private System.Windows.Forms.Label label_title;
		private FlipSwitch flipSwitch_gitCommit;
		private FlipSwitch flipSwitch_publishSource;
		private System.Windows.Forms.PictureBox pictureBox_arrow;
		private FlipSwitch flipSwitch_importManaged;
		private FlipSwitch flipSwitch_importUnmanaged;
		private FlipSwitch flipSwitch_pushCommit;
		private System.Windows.Forms.ToolStripButton button_addAdditionalConnection;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
		private System.Windows.Forms.Label label_commitMessage;
		private System.Windows.Forms.TextBox textBox_commitMessage;
		private FlipSwitch flipSwitch_enableAutomation;
		private FlipSwitch flipSwitch_overwrite;
		private SortableCheckList listBoxSolutions;
		private System.Windows.Forms.ToolStrip toolStrip1;
		private System.Windows.Forms.ToolStripButton button_checkAll;
		private System.Windows.Forms.ToolStripButton button_uncheckAll;
		private System.Windows.Forms.Panel panelSeparator;
		private FlipSwitch flipSwitch_publishTarget;
		private System.Windows.Forms.ToolStripButton button_invertCheck;
		private System.Windows.Forms.PictureBox pictureBox_warningManaged;
		private System.Windows.Forms.PictureBox pictureBox_warningUnmanaged;
		private System.Windows.Forms.ToolStripButton button_Settings;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator3;
		private System.Windows.Forms.ToolStripButton button_exportCsv;
		private System.Windows.Forms.ToolStripButton button_manageConnections;
		private System.Windows.Forms.Panel panel1;
		private System.Windows.Forms.RichTextBox richTextBox_log;
	}
}
