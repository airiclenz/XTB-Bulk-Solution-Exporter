using Microsoft.Xrm.Sdk.Query;
using System.Collections.Generic;

namespace Com.AiricLenz.XTB.Plugin
{
	partial class ConnectionManager
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ConnectionManager));
            this.button_ok = new System.Windows.Forms.Button();
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.button_addConnection = new System.Windows.Forms.ToolStripButton();
            this.button_removeConnection = new System.Windows.Forms.ToolStripButton();
            this.listBoxConnections = new Com.AiricLenz.XTB.Components.SortableCheckList();
            this.toolStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // button_ok
            // 
            this.button_ok.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.button_ok.Location = new System.Drawing.Point(397, 326);
            this.button_ok.Name = "button_ok";
            this.button_ok.Size = new System.Drawing.Size(75, 23);
            this.button_ok.TabIndex = 1;
            this.button_ok.Text = "OK";
            this.button_ok.UseVisualStyleBackColor = true;
            this.button_ok.Click += new System.EventHandler(this.button_ok_Click);
            // 
            // toolStrip1
            // 
            this.toolStrip1.AutoSize = false;
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.button_addConnection,
            this.button_removeConnection});
            this.toolStrip1.Location = new System.Drawing.Point(0, 0);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(484, 35);
            this.toolStrip1.TabIndex = 2;
            this.toolStrip1.Text = "toolStrip1";
            // 
            // button_addConnection
            // 
            this.button_addConnection.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.button_addConnection.Image = global::Com.AiricLenz.XTB.Plugin.Properties.Resources.add_32px;
            this.button_addConnection.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.button_addConnection.Margin = new System.Windows.Forms.Padding(0, 1, 10, 2);
            this.button_addConnection.Name = "button_addConnection";
            this.button_addConnection.Size = new System.Drawing.Size(166, 32);
            this.button_addConnection.Text = " Add Target Connection";
            this.button_addConnection.ToolTipText = "Add new Target Connection";
            this.button_addConnection.Click += new System.EventHandler(this.button_addConnection_Click);
            // 
            // button_removeConnection
            // 
            this.button_removeConnection.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.button_removeConnection.Image = global::Com.AiricLenz.XTB.Plugin.Properties.Resources.delete_32px;
            this.button_removeConnection.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.button_removeConnection.Margin = new System.Windows.Forms.Padding(10, 1, 10, 2);
            this.button_removeConnection.Name = "button_removeConnection";
            this.button_removeConnection.Size = new System.Drawing.Size(205, 32);
            this.button_removeConnection.Text = " Disconnect Target Connection";
            this.button_removeConnection.Click += new System.EventHandler(this.button_removeConnection_Click);
            // 
            // listBoxConnections
            // 
            this.listBoxConnections.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.listBoxConnections.BackColor = System.Drawing.SystemColors.Window;
            this.listBoxConnections.BorderColor = System.Drawing.SystemColors.ControlDark;
            this.listBoxConnections.BorderThickness = 1F;
            this.listBoxConnections.CheckBoxMargin = 4;
            this.listBoxConnections.CheckBoxRadius = 18;
            this.listBoxConnections.CheckBoxSize = 18;
            this.listBoxConnections.ColorChecked = System.Drawing.Color.MediumSlateBlue;
            this.listBoxConnections.ColorUnchecked = System.Drawing.Color.FromArgb(((int)(((byte)(200)))), ((int)(((byte)(200)))), ((int)(((byte)(200)))));
            this.listBoxConnections.ColumnsJson = "[{\"Enabled\":true,\"Width\":\"100%\",\"TooltipText\":\"\",\"Header\":\"Target Connection Name" +
    "\",\"PropertyName\":\"ConnectionName\",\"IsFixedWidth\":false,\"IsSortable\":true,\"SortOr" +
    "der\":1,\"IsSortingColumn\":true}]";
            this.listBoxConnections.DragBurgerLineThickness = 1.5F;
            this.listBoxConnections.DragBurgerSize = 11;
            this.listBoxConnections.Font = new System.Drawing.Font("Consolas", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.listBoxConnections.IsCheckable = false;
            this.listBoxConnections.IsSortable = false;
            this.listBoxConnections.ItemHeigth = 24;
            this.listBoxConnections.Location = new System.Drawing.Point(6, 38);
            this.listBoxConnections.Name = "listBoxConnections";
            this.listBoxConnections.ShowScrollBar = true;
            this.listBoxConnections.ShowTooltips = true;
            this.listBoxConnections.Size = new System.Drawing.Size(466, 274);
            this.listBoxConnections.TabIndex = 0;
            this.listBoxConnections.SelectedIndexChanged += new System.EventHandler(this.listBoxConnections_SelectedIndexChanged);
            // 
            // ConnectionManager
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(484, 361);
            this.Controls.Add(this.toolStrip1);
            this.Controls.Add(this.button_ok);
            this.Controls.Add(this.listBoxConnections);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Margin = new System.Windows.Forms.Padding(2);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(500, 400);
            this.Name = "ConnectionManager";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Connection Manager";
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            this.ResumeLayout(false);

		}


		#endregion

		private Components.SortableCheckList listBoxConnections;
		private System.Windows.Forms.Button button_ok;
		private System.Windows.Forms.ToolStrip toolStrip1;
		private System.Windows.Forms.ToolStripButton button_removeConnection;
		private System.Windows.Forms.ToolStripButton button_addConnection;

	}
}