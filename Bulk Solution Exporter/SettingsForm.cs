﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using McTools.Xrm.Connection.WinForms;
using XrmToolBox.Extensibility;


// ============================================================================
// ============================================================================
// ============================================================================
namespace Com.AiricLenz.XTB.Plugin
{

	// ============================================================================
	// ============================================================================
	// ============================================================================
	public partial class SettingsForm : Form
	{

		private Settings _settings;

		// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		public Settings Settings
		{
			get
			{
				return _settings;
			}
			set
			{
				_settings = value;
				UpdateForm();
			}
		}

		// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		private bool IsCodeUpddate
		{
			get; set;
		}


		// ============================================================================
		public SettingsForm()
		{
			InitializeComponent();
		}


		#region Event Functions

		// ============================================================================
		private void flipSwitch_saveVersionJson_Toggled(object sender, EventArgs e)
		{
			Settings.SaveVersionJson = flipSwitch_saveVersionJson.IsOn;
		}


		// ============================================================================
		private void flipSwitch_showTooltips_Toggled(object sender, EventArgs e)
		{
			Settings.ShowToolTips = flipSwitch_showTooltips.IsOn;
		}


		// ============================================================================
		private void flipSwitch_showFriendlyNames_Toggled(object sender, EventArgs e)
		{
			Settings.ShowFriendlySolutionNames = flipSwitch_showFriendlyNames.IsOn;

			if (flipSwitch_showFriendlyNames.IsOn)
			{
				return;
			}

			Settings.ShowLogicalSolutionNames = true;
			flipSwitch_showLogicalNames.IsOn = true;

		}
		// ============================================================================
		private void flipSwitch_showLogicalNames_Toggled(object sender, EventArgs e)
		{
			Settings.ShowLogicalSolutionNames = flipSwitch_showLogicalNames.IsOn;

			if (flipSwitch_showLogicalNames.IsOn)
			{
				return;
			}

			Settings.ShowFriendlySolutionNames = true;
			flipSwitch_showFriendlyNames.IsOn = true;
		}

		// ============================================================================
		private void textBox_connectionTimeout_TextChanged(object sender, EventArgs e)
		{
			if (IsCodeUpddate)
			{
				return;
			}

			string numericOnlyString =
				System.Text.RegularExpressions.Regex.Replace(
					textBox_connectionTimeout.Text,
					"[^0-9]", "");

			if (numericOnlyString.Length > 3)
			{
				numericOnlyString = numericOnlyString.Substring(0, 3);
			}

			if (!string.IsNullOrEmpty(numericOnlyString))
			{
				numericOnlyString = int.Parse(numericOnlyString).ToString();
			}
			else
			{
				numericOnlyString = "0";
			}


			int caretPosition = textBox_connectionTimeout.SelectionStart;

			if (textBox_connectionTimeout.Text != numericOnlyString)
			{
				IsCodeUpddate = true;
				textBox_connectionTimeout.Text = numericOnlyString;
				IsCodeUpddate = false;

				// Adjust caret position to account for removed characters
				textBox_connectionTimeout.SelectionStart =
					caretPosition - (textBox_connectionTimeout.Text.Length - numericOnlyString.Length);

				textBox_connectionTimeout.SelectionLength = 0;
			}

			if (int.TryParse(textBox_connectionTimeout.Text, out int value))
			{
				Settings.ConnectionTimeoutInMinutes = value;
			}
			else
			{
				Settings.ConnectionTimeoutInMinutes = 120;
			}
		}


		// ============================================================================
		private void button_ok_Click(object sender, EventArgs e)
		{
			Close();
		}


		#endregion



		// ============================================================================
		private void UpdateForm()
		{
			flipSwitch_saveVersionJson.IsOn = Settings.SaveVersionJson;
			textBox_connectionTimeout.Text = Settings.ConnectionTimeoutInMinutes.ToString();

			flipSwitch_showTooltips.IsOn = Settings.ShowToolTips;
			flipSwitch_showFriendlyNames.IsOn = Settings.ShowFriendlySolutionNames;
			flipSwitch_showLogicalNames.IsOn = Settings.ShowLogicalSolutionNames;
		}



	}
}

