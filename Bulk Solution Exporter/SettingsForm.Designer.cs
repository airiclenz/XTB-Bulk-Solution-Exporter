namespace Com.AiricLenz.XTB.Plugin
{
	partial class SettingsForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SettingsForm));
            this.button_ok = new System.Windows.Forms.Button();
            this.groupBox_generalSettings = new System.Windows.Forms.GroupBox();
            this.label_retryDelay = new System.Windows.Forms.Label();
            this.label_retryCount = new System.Windows.Forms.Label();
            this.textBox_retryDelay = new System.Windows.Forms.TextBox();
            this.textBox_retryCount = new System.Windows.Forms.TextBox();
            this.textBox_connectionTimeout = new System.Windows.Forms.TextBox();
            this.label_connectiontimeout = new System.Windows.Forms.Label();
            this.flipSwitch_saveVersionJson = new Com.AiricLenz.XTB.Components.FlipSwitch();
            this.groupBox_solutionListBoxSettings = new System.Windows.Forms.GroupBox();
            this.flipSwitch_showLogicalNames = new Com.AiricLenz.XTB.Components.FlipSwitch();
            this.flipSwitch_showFriendlyNames = new Com.AiricLenz.XTB.Components.FlipSwitch();
            this.flipSwitch_showTooltips = new Com.AiricLenz.XTB.Components.FlipSwitch();
            this.flipSwitch_continueOnError = new Com.AiricLenz.XTB.Components.FlipSwitch();
            this.groupBox_generalSettings.SuspendLayout();
            this.groupBox_solutionListBoxSettings.SuspendLayout();
            this.SuspendLayout();
            // 
            // button_ok
            // 
            this.button_ok.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.button_ok.Location = new System.Drawing.Point(497, 326);
            this.button_ok.Name = "button_ok";
            this.button_ok.Size = new System.Drawing.Size(75, 23);
            this.button_ok.TabIndex = 0;
            this.button_ok.Text = "OK";
            this.button_ok.UseVisualStyleBackColor = true;
            this.button_ok.Click += new System.EventHandler(this.button_ok_Click);
            // 
            // groupBox_generalSettings
            // 
            this.groupBox_generalSettings.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox_generalSettings.Controls.Add(this.flipSwitch_continueOnError);
            this.groupBox_generalSettings.Controls.Add(this.label_retryDelay);
            this.groupBox_generalSettings.Controls.Add(this.label_retryCount);
            this.groupBox_generalSettings.Controls.Add(this.textBox_retryDelay);
            this.groupBox_generalSettings.Controls.Add(this.textBox_retryCount);
            this.groupBox_generalSettings.Controls.Add(this.textBox_connectionTimeout);
            this.groupBox_generalSettings.Controls.Add(this.label_connectiontimeout);
            this.groupBox_generalSettings.Controls.Add(this.flipSwitch_saveVersionJson);
            this.groupBox_generalSettings.Location = new System.Drawing.Point(12, 12);
            this.groupBox_generalSettings.Name = "groupBox_generalSettings";
            this.groupBox_generalSettings.Size = new System.Drawing.Size(560, 182);
            this.groupBox_generalSettings.TabIndex = 1;
            this.groupBox_generalSettings.TabStop = false;
            this.groupBox_generalSettings.Text = "General Settings";
            // 
            // label_retryDelay
            // 
            this.label_retryDelay.AutoSize = true;
            this.label_retryDelay.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label_retryDelay.Location = new System.Drawing.Point(49, 146);
            this.label_retryDelay.Name = "label_retryDelay";
            this.label_retryDelay.Size = new System.Drawing.Size(265, 15);
            this.label_retryDelay.TabIndex = 6;
            this.label_retryDelay.Text = "Delay in seconds before a retry (Apply Upgrade)";
            // 
            // label_retryCount
            // 
            this.label_retryCount.AutoSize = true;
            this.label_retryCount.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label_retryCount.Location = new System.Drawing.Point(49, 117);
            this.label_retryCount.Name = "label_retryCount";
            this.label_retryCount.Size = new System.Drawing.Size(279, 15);
            this.label_retryCount.TabIndex = 5;
            this.label_retryCount.Text = "Retry count after an error occured (Apply Upgrade)";
            // 
            // textBox_retryDelay
            // 
            this.textBox_retryDelay.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.textBox_retryDelay.Font = new System.Drawing.Font("Consolas", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBox_retryDelay.Location = new System.Drawing.Point(7, 142);
            this.textBox_retryDelay.Name = "textBox_retryDelay";
            this.textBox_retryDelay.Size = new System.Drawing.Size(35, 23);
            this.textBox_retryDelay.TabIndex = 4;
            this.textBox_retryDelay.Text = "10";
            this.textBox_retryDelay.WordWrap = false;
            this.textBox_retryDelay.TextChanged += new System.EventHandler(this.textBox_retryDelay_TextChanged);
            // 
            // textBox_retryCount
            // 
            this.textBox_retryCount.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.textBox_retryCount.Font = new System.Drawing.Font("Consolas", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBox_retryCount.Location = new System.Drawing.Point(7, 113);
            this.textBox_retryCount.Name = "textBox_retryCount";
            this.textBox_retryCount.Size = new System.Drawing.Size(35, 23);
            this.textBox_retryCount.TabIndex = 3;
            this.textBox_retryCount.Text = "5";
            this.textBox_retryCount.WordWrap = false;
            this.textBox_retryCount.TextChanged += new System.EventHandler(this.textBox_retryCount_TextChanged);
            // 
            // textBox_connectionTimeout
            // 
            this.textBox_connectionTimeout.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.textBox_connectionTimeout.Font = new System.Drawing.Font("Consolas", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBox_connectionTimeout.Location = new System.Drawing.Point(7, 84);
            this.textBox_connectionTimeout.Name = "textBox_connectionTimeout";
            this.textBox_connectionTimeout.Size = new System.Drawing.Size(35, 23);
            this.textBox_connectionTimeout.TabIndex = 2;
            this.textBox_connectionTimeout.Text = "10";
            this.textBox_connectionTimeout.WordWrap = false;
            this.textBox_connectionTimeout.TextChanged += new System.EventHandler(this.textBox_connectionTimeout_TextChanged);
            // 
            // label_connectiontimeout
            // 
            this.label_connectiontimeout.AutoSize = true;
            this.label_connectiontimeout.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label_connectiontimeout.Location = new System.Drawing.Point(49, 87);
            this.label_connectiontimeout.Name = "label_connectiontimeout";
            this.label_connectiontimeout.Size = new System.Drawing.Size(213, 15);
            this.label_connectiontimeout.TabIndex = 1;
            this.label_connectiontimeout.Text = "Default connection timeout in minutes";
            // 
            // flipSwitch_saveVersionJson
            // 
            this.flipSwitch_saveVersionJson.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.flipSwitch_saveVersionJson.ColorOff = System.Drawing.Color.FromArgb(((int)(((byte)(150)))), ((int)(((byte)(150)))), ((int)(((byte)(150)))));
            this.flipSwitch_saveVersionJson.ColorOn = System.Drawing.Color.MediumSlateBlue;
            this.flipSwitch_saveVersionJson.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.flipSwitch_saveVersionJson.IsLocked = false;
            this.flipSwitch_saveVersionJson.IsOn = false;
            this.flipSwitch_saveVersionJson.Location = new System.Drawing.Point(6, 29);
            this.flipSwitch_saveVersionJson.MarginText = 11;
            this.flipSwitch_saveVersionJson.Name = "flipSwitch_saveVersionJson";
            this.flipSwitch_saveVersionJson.Size = new System.Drawing.Size(459, 20);
            this.flipSwitch_saveVersionJson.SwitchHeight = 18;
            this.flipSwitch_saveVersionJson.SwitchWidth = 32;
            this.flipSwitch_saveVersionJson.TabIndex = 0;
            this.flipSwitch_saveVersionJson.TextOnLeftSide = false;
            this.flipSwitch_saveVersionJson.Title = "Save a JSON-file alongside the exported solutions with version information.";
            this.flipSwitch_saveVersionJson.Toggled += new System.EventHandler(this.flipSwitch_saveVersionJson_Toggled);
            // 
            // groupBox_solutionListBoxSettings
            // 
            this.groupBox_solutionListBoxSettings.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox_solutionListBoxSettings.Controls.Add(this.flipSwitch_showLogicalNames);
            this.groupBox_solutionListBoxSettings.Controls.Add(this.flipSwitch_showFriendlyNames);
            this.groupBox_solutionListBoxSettings.Controls.Add(this.flipSwitch_showTooltips);
            this.groupBox_solutionListBoxSettings.Location = new System.Drawing.Point(18, 200);
            this.groupBox_solutionListBoxSettings.Name = "groupBox_solutionListBoxSettings";
            this.groupBox_solutionListBoxSettings.Size = new System.Drawing.Size(560, 120);
            this.groupBox_solutionListBoxSettings.TabIndex = 2;
            this.groupBox_solutionListBoxSettings.TabStop = false;
            this.groupBox_solutionListBoxSettings.Text = "Solution List Settings";
            // 
            // flipSwitch_showLogicalNames
            // 
            this.flipSwitch_showLogicalNames.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.flipSwitch_showLogicalNames.ColorOff = System.Drawing.Color.FromArgb(((int)(((byte)(150)))), ((int)(((byte)(150)))), ((int)(((byte)(150)))));
            this.flipSwitch_showLogicalNames.ColorOn = System.Drawing.Color.MediumSlateBlue;
            this.flipSwitch_showLogicalNames.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.flipSwitch_showLogicalNames.IsLocked = false;
            this.flipSwitch_showLogicalNames.IsOn = false;
            this.flipSwitch_showLogicalNames.Location = new System.Drawing.Point(6, 80);
            this.flipSwitch_showLogicalNames.MarginText = 11;
            this.flipSwitch_showLogicalNames.Name = "flipSwitch_showLogicalNames";
            this.flipSwitch_showLogicalNames.Size = new System.Drawing.Size(212, 20);
            this.flipSwitch_showLogicalNames.SwitchHeight = 18;
            this.flipSwitch_showLogicalNames.SwitchWidth = 32;
            this.flipSwitch_showLogicalNames.TabIndex = 3;
            this.flipSwitch_showLogicalNames.TextOnLeftSide = false;
            this.flipSwitch_showLogicalNames.Title = "Show Logical Solution Names";
            this.flipSwitch_showLogicalNames.Toggled += new System.EventHandler(this.flipSwitch_showLogicalNames_Toggled);
            // 
            // flipSwitch_showFriendlyNames
            // 
            this.flipSwitch_showFriendlyNames.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.flipSwitch_showFriendlyNames.ColorOff = System.Drawing.Color.FromArgb(((int)(((byte)(150)))), ((int)(((byte)(150)))), ((int)(((byte)(150)))));
            this.flipSwitch_showFriendlyNames.ColorOn = System.Drawing.Color.MediumSlateBlue;
            this.flipSwitch_showFriendlyNames.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.flipSwitch_showFriendlyNames.IsLocked = false;
            this.flipSwitch_showFriendlyNames.IsOn = false;
            this.flipSwitch_showFriendlyNames.Location = new System.Drawing.Point(6, 54);
            this.flipSwitch_showFriendlyNames.MarginText = 11;
            this.flipSwitch_showFriendlyNames.Name = "flipSwitch_showFriendlyNames";
            this.flipSwitch_showFriendlyNames.Size = new System.Drawing.Size(216, 20);
            this.flipSwitch_showFriendlyNames.SwitchHeight = 18;
            this.flipSwitch_showFriendlyNames.SwitchWidth = 32;
            this.flipSwitch_showFriendlyNames.TabIndex = 2;
            this.flipSwitch_showFriendlyNames.TextOnLeftSide = false;
            this.flipSwitch_showFriendlyNames.Title = "Show Friendly Solution Names";
            this.flipSwitch_showFriendlyNames.Toggled += new System.EventHandler(this.flipSwitch_showFriendlyNames_Toggled);
            // 
            // flipSwitch_showTooltips
            // 
            this.flipSwitch_showTooltips.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.flipSwitch_showTooltips.ColorOff = System.Drawing.Color.FromArgb(((int)(((byte)(150)))), ((int)(((byte)(150)))), ((int)(((byte)(150)))));
            this.flipSwitch_showTooltips.ColorOn = System.Drawing.Color.MediumSlateBlue;
            this.flipSwitch_showTooltips.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.flipSwitch_showTooltips.IsLocked = false;
            this.flipSwitch_showTooltips.IsOn = false;
            this.flipSwitch_showTooltips.Location = new System.Drawing.Point(6, 28);
            this.flipSwitch_showTooltips.MarginText = 11;
            this.flipSwitch_showTooltips.Name = "flipSwitch_showTooltips";
            this.flipSwitch_showTooltips.Size = new System.Drawing.Size(125, 20);
            this.flipSwitch_showTooltips.SwitchHeight = 18;
            this.flipSwitch_showTooltips.SwitchWidth = 32;
            this.flipSwitch_showTooltips.TabIndex = 1;
            this.flipSwitch_showTooltips.TextOnLeftSide = false;
            this.flipSwitch_showTooltips.Title = "Show Tooltips";
            this.flipSwitch_showTooltips.Toggled += new System.EventHandler(this.flipSwitch_showTooltips_Toggled);
            // 
            // flipSwitch_continueOnError
            // 
            this.flipSwitch_continueOnError.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.flipSwitch_continueOnError.ColorOff = System.Drawing.Color.FromArgb(((int)(((byte)(150)))), ((int)(((byte)(150)))), ((int)(((byte)(150)))));
            this.flipSwitch_continueOnError.ColorOn = System.Drawing.Color.MediumSlateBlue;
            this.flipSwitch_continueOnError.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.flipSwitch_continueOnError.IsLocked = false;
            this.flipSwitch_continueOnError.IsOn = false;
            this.flipSwitch_continueOnError.Location = new System.Drawing.Point(6, 55);
            this.flipSwitch_continueOnError.MarginText = 11;
            this.flipSwitch_continueOnError.Name = "flipSwitch_continueOnError";
            this.flipSwitch_continueOnError.Size = new System.Drawing.Size(149, 20);
            this.flipSwitch_continueOnError.SwitchHeight = 18;
            this.flipSwitch_continueOnError.SwitchWidth = 32;
            this.flipSwitch_continueOnError.TabIndex = 7;
            this.flipSwitch_continueOnError.TextOnLeftSide = false;
            this.flipSwitch_continueOnError.Title = "Continue on Error";
            this.flipSwitch_continueOnError.Toggled += new System.EventHandler(this.flipSwitch_continueOnError_Toggled);
            // 
            // SettingsForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(584, 361);
            this.Controls.Add(this.groupBox_solutionListBoxSettings);
            this.Controls.Add(this.groupBox_generalSettings);
            this.Controls.Add(this.button_ok);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(600, 400);
            this.Name = "SettingsForm";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Settings";
            this.TopMost = true;
            this.groupBox_generalSettings.ResumeLayout(false);
            this.groupBox_generalSettings.PerformLayout();
            this.groupBox_solutionListBoxSettings.ResumeLayout(false);
            this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.Button button_ok;
		private System.Windows.Forms.GroupBox groupBox_generalSettings;
		private Components.FlipSwitch flipSwitch_saveVersionJson;
		private System.Windows.Forms.GroupBox groupBox_solutionListBoxSettings;
		private Components.FlipSwitch flipSwitch_showFriendlyNames;
		private Components.FlipSwitch flipSwitch_showTooltips;
		private Components.FlipSwitch flipSwitch_showLogicalNames;
		private System.Windows.Forms.TextBox textBox_connectionTimeout;
		private System.Windows.Forms.Label label_connectiontimeout;
		private System.Windows.Forms.Label label_retryDelay;
		private System.Windows.Forms.Label label_retryCount;
		private System.Windows.Forms.TextBox textBox_retryDelay;
		private System.Windows.Forms.TextBox textBox_retryCount;
		private Components.FlipSwitch flipSwitch_continueOnError;
	}
}