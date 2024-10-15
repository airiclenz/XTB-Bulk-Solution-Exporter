﻿using Com.AiricLenz.XTB.Components;


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
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.listSolutions = new System.Windows.Forms.CheckedListBox();
            this.label_title = new System.Windows.Forms.Label();
            this.label_Log = new System.Windows.Forms.Label();
            this.textBox_log = new System.Windows.Forms.TextBox();
            this.button_browseUnmananged = new System.Windows.Forms.Button();
            this.label_unmanaged = new System.Windows.Forms.Label();
            this.button_browseManaged = new System.Windows.Forms.Button();
            this.label_Managed = new System.Windows.Forms.Label();
            this.textBox_unmanaged = new System.Windows.Forms.TextBox();
            this.textBox_managed = new System.Windows.Forms.TextBox();
            this.label_versionFormat = new System.Windows.Forms.Label();
            this.textBox_versionFormat = new System.Windows.Forms.TextBox();
            this.saveFileDialog1 = new System.Windows.Forms.SaveFileDialog();
            this.fileSystemWatcher1 = new System.IO.FileSystemWatcher();
            this.button_loadSolutions = new System.Windows.Forms.ToolStripButton();
            this.tssSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.button_Export = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.toolStripMenu = new System.Windows.Forms.ToolStrip();
            this.button_addAdditionalConnection = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            this.pictureBox_arrow = new System.Windows.Forms.PictureBox();
            this.toolTip2 = new System.Windows.Forms.ToolTip(this.components);
            this.textBox_commitMessage = new System.Windows.Forms.TextBox();
            this.label_commitMessage = new System.Windows.Forms.Label();
            this.flipSwitch_overwrite = new Com.AiricLenz.XTB.Components.FlipSwitch();
            this.flipSwitch_enableAutomation = new Com.AiricLenz.XTB.Components.FlipSwitch();
            this.flipSwitch_pushCommit = new Com.AiricLenz.XTB.Components.FlipSwitch();
            this.flipSwitch_importUnmanaged = new Com.AiricLenz.XTB.Components.FlipSwitch();
            this.flipSwitch_importManaged = new Com.AiricLenz.XTB.Components.FlipSwitch();
            this.flipSwitch_gitCommit = new Com.AiricLenz.XTB.Components.FlipSwitch();
            this.flipSwitch_publishSource = new Com.AiricLenz.XTB.Components.FlipSwitch();
            this.flipSwitch_updateVersion = new Com.AiricLenz.XTB.Components.FlipSwitch();
            this.flipSwitch_exportUnmanaged = new Com.AiricLenz.XTB.Components.FlipSwitch();
            this.flipSwitch_exportManaged = new Com.AiricLenz.XTB.Components.FlipSwitch();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.fileSystemWatcher1)).BeginInit();
            this.toolStripMenu.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox_arrow)).BeginInit();
            this.SuspendLayout();
            // 
            // splitContainer1
            // 
            this.splitContainer1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.splitContainer1.Location = new System.Drawing.Point(3, 172);
            this.splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.listSolutions);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.label_title);
            this.splitContainer1.Panel2.Controls.Add(this.label_Log);
            this.splitContainer1.Panel2.Controls.Add(this.textBox_log);
            this.splitContainer1.Panel2.Controls.Add(this.button_browseUnmananged);
            this.splitContainer1.Panel2.Controls.Add(this.label_unmanaged);
            this.splitContainer1.Panel2.Controls.Add(this.button_browseManaged);
            this.splitContainer1.Panel2.Controls.Add(this.label_Managed);
            this.splitContainer1.Panel2.Controls.Add(this.textBox_unmanaged);
            this.splitContainer1.Panel2.Controls.Add(this.textBox_managed);
            this.splitContainer1.Size = new System.Drawing.Size(994, 625);
            this.splitContainer1.SplitterDistance = 328;
            this.splitContainer1.TabIndex = 6;
            // 
            // listSolutions
            // 
            this.listSolutions.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.listSolutions.Font = new System.Drawing.Font("Consolas", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.listSolutions.FormattingEnabled = true;
            this.listSolutions.Location = new System.Drawing.Point(3, 3);
            this.listSolutions.Name = "listSolutions";
            this.listSolutions.Size = new System.Drawing.Size(322, 616);
            this.listSolutions.Sorted = true;
            this.listSolutions.TabIndex = 6;
            this.listSolutions.UseCompatibleTextRendering = true;
            this.listSolutions.ItemCheck += new System.Windows.Forms.ItemCheckEventHandler(this.listSolutions_ItemCheck);
            this.listSolutions.SelectedIndexChanged += new System.EventHandler(this.listSolutions_SelectedIndexChanged);
            // 
            // label_title
            // 
            this.label_title.AutoSize = true;
            this.label_title.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label_title.Location = new System.Drawing.Point(15, 3);
            this.label_title.Name = "label_title";
            this.label_title.Size = new System.Drawing.Size(167, 18);
            this.label_title.TabIndex = 8;
            this.label_title.Text = "No Solution Selected";
            // 
            // label_Log
            // 
            this.label_Log.AutoSize = true;
            this.label_Log.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label_Log.Location = new System.Drawing.Point(17, 137);
            this.label_Log.Name = "label_Log";
            this.label_Log.Size = new System.Drawing.Size(30, 16);
            this.label_Log.TabIndex = 7;
            this.label_Log.Text = "Log";
            // 
            // textBox_log
            // 
            this.textBox_log.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.textBox_log.BackColor = System.Drawing.SystemColors.ControlLightLight;
            this.textBox_log.Cursor = System.Windows.Forms.Cursors.Default;
            this.textBox_log.Font = new System.Drawing.Font("Consolas", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBox_log.Location = new System.Drawing.Point(17, 156);
            this.textBox_log.Multiline = true;
            this.textBox_log.Name = "textBox_log";
            this.textBox_log.ReadOnly = true;
            this.textBox_log.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.textBox_log.Size = new System.Drawing.Size(638, 464);
            this.textBox_log.TabIndex = 6;
            this.textBox_log.Text = "This is the log...";
            // 
            // button_browseUnmananged
            // 
            this.button_browseUnmananged.Enabled = false;
            this.button_browseUnmananged.Location = new System.Drawing.Point(18, 100);
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
            this.label_unmanaged.Location = new System.Drawing.Point(17, 83);
            this.label_unmanaged.Name = "label_unmanaged";
            this.label_unmanaged.Size = new System.Drawing.Size(212, 16);
            this.label_unmanaged.TabIndex = 4;
            this.label_unmanaged.Text = "File Location Unmanaged Solution";
            // 
            // button_browseManaged
            // 
            this.button_browseManaged.Enabled = false;
            this.button_browseManaged.Location = new System.Drawing.Point(17, 53);
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
            this.label_Managed.Location = new System.Drawing.Point(17, 35);
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
            this.textBox_unmanaged.Size = new System.Drawing.Size(548, 23);
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
            this.textBox_managed.Size = new System.Drawing.Size(548, 23);
            this.textBox_managed.TabIndex = 0;
            this.textBox_managed.TextChanged += new System.EventHandler(this.textBox_managed_TextChanged);
            // 
            // label_versionFormat
            // 
            this.label_versionFormat.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label_versionFormat.Location = new System.Drawing.Point(490, 79);
            this.label_versionFormat.Name = "label_versionFormat";
            this.label_versionFormat.Size = new System.Drawing.Size(200, 16);
            this.label_versionFormat.TabIndex = 9;
            this.label_versionFormat.Text = "Version Format";
            this.label_versionFormat.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // textBox_versionFormat
            // 
            this.textBox_versionFormat.BackColor = System.Drawing.SystemColors.ControlLightLight;
            this.textBox_versionFormat.Font = new System.Drawing.Font("Consolas", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBox_versionFormat.Location = new System.Drawing.Point(696, 77);
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
            this.saveFileDialog1.Filter = "Solution files (*.zip|*.zip|All files (*.*)|*.*\"";
            this.saveFileDialog1.FilterIndex = 0;
            // 
            // fileSystemWatcher1
            // 
            this.fileSystemWatcher1.EnableRaisingEvents = true;
            this.fileSystemWatcher1.SynchronizingObject = this;
            // 
            // button_loadSolutions
            // 
            this.button_loadSolutions.Font = new System.Drawing.Font("Segoe UI", 10.125F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.button_loadSolutions.Image = ((System.Drawing.Image)(resources.GetObject("button_loadSolutions.Image")));
            this.button_loadSolutions.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.button_loadSolutions.Margin = new System.Windows.Forms.Padding(0, 1, 10, 2);
            this.button_loadSolutions.Name = "button_loadSolutions";
            this.button_loadSolutions.Size = new System.Drawing.Size(143, 39);
            this.button_loadSolutions.Text = "  Load Solutions";
            this.button_loadSolutions.ToolTipText = "Load all unmanaged solutions from the environment...";
            this.button_loadSolutions.Click += new System.EventHandler(this.button_loadSolutions_Click);
            // 
            // tssSeparator1
            // 
            this.tssSeparator1.Name = "tssSeparator1";
            this.tssSeparator1.Size = new System.Drawing.Size(6, 42);
            // 
            // button_Export
            // 
            this.button_Export.Enabled = false;
            this.button_Export.Font = new System.Drawing.Font("Segoe UI", 10.125F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.button_Export.Image = ((System.Drawing.Image)(resources.GetObject("button_Export.Image")));
            this.button_Export.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.button_Export.Margin = new System.Windows.Forms.Padding(10, 1, 0, 2);
            this.button_Export.Name = "button_Export";
            this.button_Export.Size = new System.Drawing.Size(99, 39);
            this.button_Export.Text = " Execute ";
            this.button_Export.ToolTipText = "Export the Solutions";
            this.button_Export.Click += new System.EventHandler(this.button_Export_Click);
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
            this.toolStripSeparator2,
            this.button_Export,
            this.toolStripSeparator1});
            this.toolStripMenu.Location = new System.Drawing.Point(0, 0);
            this.toolStripMenu.Name = "toolStripMenu";
            this.toolStripMenu.Padding = new System.Windows.Forms.Padding(0, 0, 2, 0);
            this.toolStripMenu.Size = new System.Drawing.Size(1000, 42);
            this.toolStripMenu.TabIndex = 4;
            this.toolStripMenu.Text = "toolStrip1";
            // 
            // button_addAdditionalConnection
            // 
            this.button_addAdditionalConnection.Font = new System.Drawing.Font("Segoe UI", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.button_addAdditionalConnection.Image = ((System.Drawing.Image)(resources.GetObject("button_addAdditionalConnection.Image")));
            this.button_addAdditionalConnection.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.button_addAdditionalConnection.Margin = new System.Windows.Forms.Padding(10, 1, 10, 2);
            this.button_addAdditionalConnection.Name = "button_addAdditionalConnection";
            this.button_addAdditionalConnection.Size = new System.Drawing.Size(170, 39);
            this.button_addAdditionalConnection.Text = "  Connect to Target";
            this.button_addAdditionalConnection.ToolTipText = "  Connect to Target";
            this.button_addAdditionalConnection.Click += new System.EventHandler(this.button_addAdditionalConnection_Click);
            // 
            // toolStripSeparator2
            // 
            this.toolStripSeparator2.Name = "toolStripSeparator2";
            this.toolStripSeparator2.Size = new System.Drawing.Size(6, 42);
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
            // textBox_commitMessage
            // 
            this.textBox_commitMessage.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.textBox_commitMessage.BackColor = System.Drawing.SystemColors.ControlLightLight;
            this.textBox_commitMessage.Font = new System.Drawing.Font("Consolas", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBox_commitMessage.Location = new System.Drawing.Point(696, 52);
            this.textBox_commitMessage.Name = "textBox_commitMessage";
            this.textBox_commitMessage.Size = new System.Drawing.Size(294, 23);
            this.textBox_commitMessage.TabIndex = 20;
            this.textBox_commitMessage.Text = "XTB Commit";
            this.textBox_commitMessage.TextChanged += new System.EventHandler(this.textBox_commitMessage_TextChanged);
            this.textBox_commitMessage.Leave += new System.EventHandler(this.textBox_commitMessage_Leave);
            // 
            // label_commitMessage
            // 
            this.label_commitMessage.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label_commitMessage.Location = new System.Drawing.Point(490, 56);
            this.label_commitMessage.Name = "label_commitMessage";
            this.label_commitMessage.Size = new System.Drawing.Size(200, 16);
            this.label_commitMessage.TabIndex = 22;
            this.label_commitMessage.Text = "Commit Message";
            this.label_commitMessage.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // flipSwitch_overwrite
            // 
            this.flipSwitch_overwrite.AutoSize = true;
            this.flipSwitch_overwrite.ColorOff = System.Drawing.Color.FromArgb(((int)(((byte)(150)))), ((int)(((byte)(150)))), ((int)(((byte)(150)))));
            this.flipSwitch_overwrite.ColorOn = System.Drawing.Color.MediumSlateBlue;
            this.flipSwitch_overwrite.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.flipSwitch_overwrite.IsOn = false;
            this.flipSwitch_overwrite.Location = new System.Drawing.Point(535, 130);
            this.flipSwitch_overwrite.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.flipSwitch_overwrite.MarginText = 2;
            this.flipSwitch_overwrite.Name = "flipSwitch_overwrite";
            this.flipSwitch_overwrite.Size = new System.Drawing.Size(193, 20);
            this.flipSwitch_overwrite.SwitchHeight = 18;
            this.flipSwitch_overwrite.SwitchWidth = 30;
            this.flipSwitch_overwrite.TabIndex = 24;
            this.flipSwitch_overwrite.TextOnLeftSide = true;
            this.flipSwitch_overwrite.Title = "Overwrite Customizations";
            this.toolTip1.SetToolTip(this.flipSwitch_overwrite, "Overwrite local customizations with the solution component(s) if existing in the " +
        "target environment.");
            this.flipSwitch_overwrite.Toggled += new System.EventHandler(this.flipSwitch_overwrite_Toggled);
            // 
            // flipSwitch_enableAutomation
            // 
            this.flipSwitch_enableAutomation.AutoSize = true;
            this.flipSwitch_enableAutomation.ColorOff = System.Drawing.Color.FromArgb(((int)(((byte)(150)))), ((int)(((byte)(150)))), ((int)(((byte)(150)))));
            this.flipSwitch_enableAutomation.ColorOn = System.Drawing.Color.MediumSlateBlue;
            this.flipSwitch_enableAutomation.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.flipSwitch_enableAutomation.IsOn = true;
            this.flipSwitch_enableAutomation.Location = new System.Drawing.Point(574, 105);
            this.flipSwitch_enableAutomation.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.flipSwitch_enableAutomation.MarginText = 2;
            this.flipSwitch_enableAutomation.Name = "flipSwitch_enableAutomation";
            this.flipSwitch_enableAutomation.Size = new System.Drawing.Size(154, 20);
            this.flipSwitch_enableAutomation.SwitchHeight = 18;
            this.flipSwitch_enableAutomation.SwitchWidth = 30;
            this.flipSwitch_enableAutomation.TabIndex = 23;
            this.flipSwitch_enableAutomation.TextOnLeftSide = true;
            this.flipSwitch_enableAutomation.Title = "Enable Automation";
            this.toolTip1.SetToolTip(this.flipSwitch_enableAutomation, "Enable Workflows and Plugin-Steps after import of the solution.");
            this.flipSwitch_enableAutomation.Toggled += new System.EventHandler(this.flipSwitch_enableAutomation_Toggled);
            // 
            // flipSwitch_pushCommit
            // 
            this.flipSwitch_pushCommit.AutoSize = true;
            this.flipSwitch_pushCommit.ColorOff = System.Drawing.Color.FromArgb(((int)(((byte)(150)))), ((int)(((byte)(150)))), ((int)(((byte)(150)))));
            this.flipSwitch_pushCommit.ColorOn = System.Drawing.Color.MediumSlateBlue;
            this.flipSwitch_pushCommit.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.flipSwitch_pushCommit.IsOn = false;
            this.flipSwitch_pushCommit.Location = new System.Drawing.Point(300, 80);
            this.flipSwitch_pushCommit.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
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
            this.flipSwitch_importUnmanaged.AutoSize = true;
            this.flipSwitch_importUnmanaged.ColorOff = System.Drawing.Color.FromArgb(((int)(((byte)(150)))), ((int)(((byte)(150)))), ((int)(((byte)(150)))));
            this.flipSwitch_importUnmanaged.ColorOn = System.Drawing.Color.MediumSlateBlue;
            this.flipSwitch_importUnmanaged.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.flipSwitch_importUnmanaged.IsOn = false;
            this.flipSwitch_importUnmanaged.Location = new System.Drawing.Point(300, 130);
            this.flipSwitch_importUnmanaged.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
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
            this.flipSwitch_importManaged.AutoSize = true;
            this.flipSwitch_importManaged.ColorOff = System.Drawing.Color.FromArgb(((int)(((byte)(150)))), ((int)(((byte)(150)))), ((int)(((byte)(150)))));
            this.flipSwitch_importManaged.ColorOn = System.Drawing.Color.MediumSlateBlue;
            this.flipSwitch_importManaged.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.flipSwitch_importManaged.IsOn = false;
            this.flipSwitch_importManaged.Location = new System.Drawing.Point(300, 105);
            this.flipSwitch_importManaged.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
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
            this.flipSwitch_gitCommit.AutoSize = true;
            this.flipSwitch_gitCommit.ColorOff = System.Drawing.Color.FromArgb(((int)(((byte)(150)))), ((int)(((byte)(150)))), ((int)(((byte)(150)))));
            this.flipSwitch_gitCommit.ColorOn = System.Drawing.Color.MediumSlateBlue;
            this.flipSwitch_gitCommit.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.flipSwitch_gitCommit.IsOn = false;
            this.flipSwitch_gitCommit.Location = new System.Drawing.Point(300, 55);
            this.flipSwitch_gitCommit.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
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
            this.flipSwitch_publishSource.AutoSize = true;
            this.flipSwitch_publishSource.ColorOff = System.Drawing.Color.FromArgb(((int)(((byte)(150)))), ((int)(((byte)(150)))), ((int)(((byte)(150)))));
            this.flipSwitch_publishSource.ColorOn = System.Drawing.Color.MediumSlateBlue;
            this.flipSwitch_publishSource.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.flipSwitch_publishSource.IsOn = false;
            this.flipSwitch_publishSource.Location = new System.Drawing.Point(15, 55);
            this.flipSwitch_publishSource.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
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
            this.flipSwitch_updateVersion.AutoSize = true;
            this.flipSwitch_updateVersion.ColorOff = System.Drawing.Color.FromArgb(((int)(((byte)(150)))), ((int)(((byte)(150)))), ((int)(((byte)(150)))));
            this.flipSwitch_updateVersion.ColorOn = System.Drawing.Color.MediumSlateBlue;
            this.flipSwitch_updateVersion.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.flipSwitch_updateVersion.IsOn = false;
            this.flipSwitch_updateVersion.Location = new System.Drawing.Point(15, 80);
            this.flipSwitch_updateVersion.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
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
            // flipSwitch_exportUnmanaged
            // 
            this.flipSwitch_exportUnmanaged.AutoSize = true;
            this.flipSwitch_exportUnmanaged.ColorOff = System.Drawing.Color.FromArgb(((int)(((byte)(150)))), ((int)(((byte)(150)))), ((int)(((byte)(150)))));
            this.flipSwitch_exportUnmanaged.ColorOn = System.Drawing.Color.MediumSlateBlue;
            this.flipSwitch_exportUnmanaged.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.flipSwitch_exportUnmanaged.IsOn = false;
            this.flipSwitch_exportUnmanaged.Location = new System.Drawing.Point(15, 130);
            this.flipSwitch_exportUnmanaged.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.flipSwitch_exportUnmanaged.MarginText = 7;
            this.flipSwitch_exportUnmanaged.Name = "flipSwitch_exportUnmanaged";
            this.flipSwitch_exportUnmanaged.Size = new System.Drawing.Size(161, 20);
            this.flipSwitch_exportUnmanaged.SwitchHeight = 18;
            this.flipSwitch_exportUnmanaged.SwitchWidth = 30;
            this.flipSwitch_exportUnmanaged.TabIndex = 11;
            this.flipSwitch_exportUnmanaged.TextOnLeftSide = false;
            this.flipSwitch_exportUnmanaged.Title = "Export Unmanaged";
            this.flipSwitch_exportUnmanaged.Toggled += new System.EventHandler(this.flipSwitch_exportUnmanaged_Toggled);
            // 
            // flipSwitch_exportManaged
            // 
            this.flipSwitch_exportManaged.AutoSize = true;
            this.flipSwitch_exportManaged.ColorOff = System.Drawing.Color.FromArgb(((int)(((byte)(150)))), ((int)(((byte)(150)))), ((int)(((byte)(150)))));
            this.flipSwitch_exportManaged.ColorOn = System.Drawing.Color.MediumSlateBlue;
            this.flipSwitch_exportManaged.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.flipSwitch_exportManaged.IsOn = false;
            this.flipSwitch_exportManaged.Location = new System.Drawing.Point(15, 105);
            this.flipSwitch_exportManaged.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.flipSwitch_exportManaged.MarginText = 7;
            this.flipSwitch_exportManaged.Name = "flipSwitch_exportManaged";
            this.flipSwitch_exportManaged.Size = new System.Drawing.Size(144, 20);
            this.flipSwitch_exportManaged.SwitchHeight = 18;
            this.flipSwitch_exportManaged.SwitchWidth = 30;
            this.flipSwitch_exportManaged.TabIndex = 10;
            this.flipSwitch_exportManaged.TextOnLeftSide = false;
            this.flipSwitch_exportManaged.Title = "Export Managed";
            this.flipSwitch_exportManaged.Toggled += new System.EventHandler(this.flipSwitch_exportManaged_Toggled);
            // 
            // BulkSolutionExporter_PluginControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
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
            this.Size = new System.Drawing.Size(1000, 800);
            this.Load += new System.EventHandler(this.OnPluginControl_Load);
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            this.splitContainer1.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.fileSystemWatcher1)).EndInit();
            this.toolStripMenu.ResumeLayout(false);
            this.toolStripMenu.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox_arrow)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.CheckedListBox listSolutions;
        private System.Windows.Forms.TextBox textBox_unmanaged;
        private System.Windows.Forms.TextBox textBox_managed;
        private System.Windows.Forms.TextBox textBox_log;
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
        private System.Windows.Forms.ToolTip toolTip2;
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
	}
}
