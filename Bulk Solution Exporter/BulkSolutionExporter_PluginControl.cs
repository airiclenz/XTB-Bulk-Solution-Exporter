using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.ServiceModel;
using System.Windows.Forms;
using Com.AiricLenz.Extentions;
using Com.AiricLenz.XTB.Components;
using Com.AiricLenz.XTB.Plugin.Helpers;
using Com.AiricLenz.XTB.Plugin.Schema;
using McTools.Xrm.Connection;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Tooling.Connector;
using Newtonsoft.Json;
using XrmToolBox.Extensibility;


// ============================================================================
// ============================================================================
// ============================================================================
namespace Com.AiricLenz.XTB.Plugin
{

	// ============================================================================
	// ============================================================================
	// ============================================================================
	public partial class BulkSolutionExporter_PluginControl : MultipleConnectionsPluginControlBase
	{

		#region Variables and Properties

		private bool isDebugMode = false;

		private Settings _settings;
		private bool _codeUpdate = false;
		private bool _codeUpdateSplitter = false;
		private Guid _currentConnectionGuid;
		private SolutionConfiguration _currentSolutionConfig;

		private List<string> _sessionFiles = new List<string>();
		private List<Solution> _solutionsOrigin = new List<Solution>();
		private List<Solution> _solutionsTarget = new List<Solution>();
		private ConnectionManager _connectionManager = null;
		private GitHelper _gitHelper = null;
		private List<string> _gitBranches = new List<string>();
		private Logger _logger = null;

		private WorkAsyncInfo _executionWorker = null;
		private Timer _progressTimer;
		private DateTime _progressStartTime;
		private string _progressBaseMessage;
		private Size _workerPanelSize = new Size(400, 200);


		private Solution _currentSolution;
		private object origin;
		private object target;

		private const string ColorError = "<color=#770000>";
		private const string ColorSolution = "<color=#000077>";
		private const string ColorFile = "<color=#777700>";
		private const string ColorIndent = "<color=#DDDDDD>";
		private const string ColorGreen = "<color=#227700>";
		private const string ColorConnection = "<color=#337799>";
		private const string ColorTeeth = "<color=#CCCCCE>";

		private const string ColorEndTag = "</color>";

		private const string dateTimeFormat = "yyyy-MM-dd HH:mm:ss";


		// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		public List<ConnectionDetail> TargetConnections
		{
			get
			{
				return AdditionalConnectionDetails.ToList();
			}
		}

		// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		private bool CodeUpdate
		{
			get
			{
				return _codeUpdate;
			}

			set
			{
				_codeUpdate = value;
				//LogDebug($"CodeUpdate = {_codeUpdate}");
			}
		}

		// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		private bool CodeUpdateSplitter
		{
			get
			{
				return _codeUpdateSplitter;
			}

			set
			{
				_codeUpdateSplitter = value;
				//LogDebug($"CodeUpdateSplitter = {_codeUpdateSplitter}");
			}
		}


		#endregion

		// ##################################################
		// ##################################################

		#region Plugin Events


		// ============================================================================
		public BulkSolutionExporter_PluginControl()
		{
			InitializeComponent();

			richTextBox_log.Text = string.Empty;

			_logger = new Logger(richTextBox_log);
			_logger.Indent = ColorIndent + "|" + ColorEndTag + "   ";

			ParentChanged += MyPlugin_ParentChanged;

			UpdateColumns();
		}



		// ============================================================================
		private void OnPluginControl_Load(
			object sender,
			EventArgs e)
		{

			// Loads or creates the settings for the plugin
			if (!SettingsManager.Instance.TryLoad(GetType(), out _settings))
			{
				_settings = new Settings();
				CodeUpdate = false;

				SaveSettings();

				LogWarning("Settings not found => a new settings file has been created!");
			}
			else
			{
				LogInfo("Settings found and loaded");

				CodeUpdate = true;
				CodeUpdateSplitter = true;

				flipSwitch_exportManaged.IsOn = _settings.ExportManaged;
				flipSwitch_exportUnmanaged.IsOn = _settings.ExportUnmanaged;

				flipSwitch_importManaged.IsOn = _settings.ImportManaged;
				flipSwitch_importManaged.Enabled = TargetConnections?.Count != 0;
				flipSwitch_importUnmanaged.IsOn = _settings.ImportUnmanaged && _settings.ImportManaged != true;
				flipSwitch_importUnmanaged.Enabled = TargetConnections?.Count != 0;
				flipSwitch_enableAutomation.IsOn = _settings.EnableAutomation;
				flipSwitch_upgrade.IsOn = _settings.Upgrade;
				flipSwitch_overwrite.IsOn = _settings.OverwriteCustomizations;
				UpdateImportOptionsVisibility();

				flipSwitch_publishSource.IsOn = _settings.PublishAllPreExport;
				flipSwitch_publishTarget.IsOn = _settings.PublishAllPostImport;
				flipSwitch_updateVersion.IsOn = _settings.UpdateVersion;
				textBox_versionFormat.Text = _settings.VersionFormat;
				UpdateVersionOptionsVisibility();

				flipSwitch_gitCommit.IsOn = _settings.GitCommit;
				flipSwitch_gitCommit.Enabled =
					flipSwitch_exportManaged.IsOn || flipSwitch_exportUnmanaged.IsOn;

				flipSwitch_pushCommit.IsOn = _settings.PushCommit && _settings.GitCommit != false;
				flipSwitch_pushCommit.Enabled = flipSwitch_gitCommit.IsOn;
				textBox_commitMessage.Text = _settings.CommitMessage;
				UpdateGitOptionsVisibility();

				listBoxSolutions.ShowTooltips = _settings.ShowToolTips;

				UpdateSolutionSettingsScreen();
				SetExportButtonState();

				CodeUpdate = false;
			}

			button_loadSolutions.Enabled = Service != null;
			button_manageConnections.Visible = TargetConnections?.Count > 0;

			LoadAllSolutions();
			InitializeGitHelper();
			UpdateAllConnectionTimeouts();
		}


		// ============================================================================
		private void MyPlugin_ParentChanged(object sender, EventArgs e)
		{
			if (this.Parent != null)
			{
				if (this.IsHandleCreated)
				{
					this.BeginInvoke((MethodInvoker) delegate
					{
						CodeUpdate = true;
						splitContainer1.SplitterDistance = _settings.SplitContainerPosition;
						splitContainer1.Update();  // Forces immediate UI update
						splitContainer1.Refresh(); // Ensures repaint
						Application.DoEvents();    // Processes pending UI messages
						CodeUpdate = false;
						CodeUpdateSplitter = false;
						LogDebug($"After ParentChanged: {splitContainer1.SplitterDistance}");
					});
				}
				else
				{
					this.HandleCreated += MyPlugin_HandleCreated;
				}
			}
		}

		// ============================================================================
		private void MyPlugin_HandleCreated(object sender, EventArgs e)
		{
			this.HandleCreated -= MyPlugin_HandleCreated; // Unsubscribe to avoid multiple calls
			this.BeginInvoke((MethodInvoker) delegate
			{
				CodeUpdate = true;
				splitContainer1.SplitterDistance = _settings.SplitContainerPosition;
				splitContainer1.Update();  // Forces immediate UI update
				splitContainer1.Refresh(); // Ensures repaint
				Application.DoEvents();    // Processes pending UI messages
				CodeUpdate = false;
				CodeUpdateSplitter = false;

				LogDebug($"After HandleCreated: {splitContainer1.SplitterDistance}");
			});
		}

		// ============================================================================
		/// <summary>
		/// This occurs when the plugin is closing.
		/// </summary>
		/// <param name="info"></param>
		public override void ClosingPlugin(PluginCloseInfo info)
		{
			base.ClosingPlugin(info);
			SaveSettings();
		}

		// ============================================================================
		/// <summary>
		/// This event occurs when the plugin is closed
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void MyPluginControl_OnCloseTool(
			object sender,
			EventArgs e)
		{
			// Before leaving, save the settings
			SaveSettings(cleanUpNonExistingSolutions: true);
		}


		// ============================================================================
		/// <summary>
		/// This event occurs when the connection has been updated in XrmToolBox
		/// </summary>
		public override void UpdateConnection(
			IOrganizationService newService,
			ConnectionDetail detail,
			string actionName,
			object parameter)
		{
			if (actionName != "AdditionalOrganization")
			{
				_currentConnectionGuid = detail.ConnectionId.Value;

				if (_settings != null &&
					detail != null)
				{
					_settings.LastUsedOrganizationWebappUrl = detail.WebApplicationUrl;
					LogInfo("Connection has changed to: {0}", detail.WebApplicationUrl);

					// Set the timeout for the connection
					if (detail != null)
					{
						UpdateConnectionTimout(
							ref detail,
							_settings.ConnectionTimeoutInMinutes);
					}
				}

				LoadAllSolutions();

				button_loadSolutions.Enabled = Service != null;
			}

			base.UpdateConnection(newService, detail, actionName, parameter);
		}


		// ============================================================================
		protected override void ConnectionDetailsUpdated(
			NotifyCollectionChangedEventArgs e)
		{
			//var detail = (ConnectionDetail) e.NewItems[0];

			flipSwitch_importManaged.Enabled = TargetConnections?.Count > 0;
			flipSwitch_importUnmanaged.Enabled = TargetConnections?.Count > 0;
			button_manageConnections.Visible = TargetConnections?.Count > 0;

			LoadAllSolutions();

			_connectionManager?.UpdateConnections();

			UpdateAllConnectionTimeouts();
		}


		// ============================================================================
		private void UpdateAllConnectionTimeouts()
		{
			// Update the source connection
			if (ConnectionDetail != null)
			{
				var connectionDetail = ConnectionDetail;

				UpdateConnectionTimout(
					ref connectionDetail,
					_settings.ConnectionTimeoutInMinutes);
			}


			// Update the target connections
			for (int i = 0; i < TargetConnections.Count; i++)
			{
				var targetConnection = TargetConnections[i];

				UpdateConnectionTimout(
					ref targetConnection,
					_settings.ConnectionTimeoutInMinutes);
			}
		}


		// ============================================================================
		private void button_loadSolutions_Click(object sender, EventArgs e)
		{
			LoadAllSolutions();
			InitializeGitHelper();
		}

		// ============================================================================
		private void flipSwitch_publish_Toggled(object sender, EventArgs e)
		{
			if (CodeUpdate)
			{
				return;
			}

			_settings.PublishAllPreExport = flipSwitch_publishSource.IsOn;

			SaveSettings();
			SetExportButtonState();
		}


		// ============================================================================
		private void flipSwitch_updateVersion_Toggled(object sender, EventArgs e)
		{
			if (CodeUpdate)
			{
				return;
			}

			_settings.UpdateVersion = flipSwitch_updateVersion.IsOn;

			UpdateVersionOptionsVisibility();
			SetExportButtonState();
			SaveSettings();
		}


		// ============================================================================
		private void flipSwitch_exportManaged_Toggled(object sender, EventArgs e)
		{
			if (CodeUpdate)
			{
				return;
			}

			_settings.ExportManaged = flipSwitch_exportManaged.IsOn;

			CodeUpdate = true;

			var gitEnabeld =
				flipSwitch_exportManaged.IsOn ||
				flipSwitch_exportUnmanaged.IsOn;

			flipSwitch_gitCommit.Enabled = gitEnabeld;
			flipSwitch_pushCommit.Enabled = gitEnabeld;

			CodeUpdate = false;

			SetExportButtonState();
			SaveSettings();
		}

		// ============================================================================
		private void flipSwitch_exportUnmanaged_Toggled(object sender, EventArgs e)
		{
			if (CodeUpdate)
			{
				return;
			}

			_settings.ExportUnmanaged = flipSwitch_exportManaged.IsOn;

			CodeUpdate = true;

			var gitEnabeld =
				flipSwitch_exportManaged.IsOn ||
				flipSwitch_exportUnmanaged.IsOn;

			flipSwitch_gitCommit.Enabled = gitEnabeld;
			flipSwitch_pushCommit.Enabled = gitEnabeld;

			CodeUpdate = false;

			SetExportButtonState();
			SaveSettings();
		}


		// ============================================================================
		private void flipSwitch_gitCommit_Toggled(object sender, EventArgs e)
		{
			if (CodeUpdate)
			{
				return;
			}

			_settings.GitCommit = flipSwitch_gitCommit.IsOn;
			flipSwitch_pushCommit.Enabled = flipSwitch_gitCommit.IsOn;

			UpdateGitOptionsVisibility();
			SetExportButtonState();
			SaveSettings();
		}

		// ============================================================================
		private void flipSwitch_pushCommit_Toggled(object sender, EventArgs e)
		{
			if (CodeUpdate)
			{
				return;
			}

			_settings.PushCommit = flipSwitch_pushCommit.IsOn;
			SetExportButtonState();
			SaveSettings();

		}


		// ============================================================================
		private void flipSwitch_importManaged_Toggled(object sender, EventArgs e)
		{
			if (CodeUpdate)
			{
				return;
			}

			_settings.ImportManaged =
				flipSwitch_importManaged.IsOn &&
				TargetConnections?.Count > 0;

			if (flipSwitch_importManaged.IsOn &&
				flipSwitch_importUnmanaged.IsOn &&
				!CodeUpdate)
			{
				CodeUpdate = true;

				flipSwitch_importUnmanaged.IsOn = false;
				_settings.ImportUnmanaged = false;

				CodeUpdate = false;
			}

			UpdateImportOptionsVisibility();
			SetExportButtonState();
			SaveSettings();
		}

		// ============================================================================
		private void flipSwitch_importUnmanaged_Toggled(object sender, EventArgs e)
		{
			if (CodeUpdate)
			{
				return;
			}

			_settings.ImportUnmanaged =
				flipSwitch_importUnmanaged.IsOn &&
				TargetConnections?.Count > 0;

			if (flipSwitch_importManaged.IsOn &&
				flipSwitch_importUnmanaged.IsOn &&
				!CodeUpdate)
			{
				CodeUpdate = true;

				flipSwitch_importManaged.IsOn = false;
				_settings.ImportManaged = false;

				CodeUpdate = false;
			}

			UpdateImportOptionsVisibility();
			SetExportButtonState();
			SaveSettings();
		}


		// ============================================================================
		private void flipSwitch_upgrade_Toggled(object sender, EventArgs e)
		{
			if (CodeUpdate)
			{
				return;
			}

			_settings.Upgrade =
				flipSwitch_upgrade.IsOn;

			SaveSettings();
		}


		// ============================================================================
		private void textBox_commitMessage_TextChanged(object sender, EventArgs e)
		{
			if (CodeUpdate)
			{
				return;
			}

			_settings.CommitMessage = textBox_commitMessage.Text;
			SetExportButtonState();
			SaveSettings();
		}


		// ============================================================================
		private void textBox_commitMessage_Leave(object sender, EventArgs e)
		{
			if (CodeUpdate)
			{
				return;

			}
			_settings.CommitMessage = textBox_commitMessage.Text;
			SaveSettings();
		}


		// ============================================================================
		private void textBox_versionFormat_TextChanged(object sender, EventArgs e)
		{
			if (CodeUpdate)
			{
				return;

			}
			_settings.VersionFormat = textBox_versionFormat.Text;
			SetExportButtonState();
			SaveSettings();
		}

		// ============================================================================
		private void textBox_versionFormat_Leave(object sender, EventArgs e)
		{
			if (CodeUpdate)
			{
				return;
			}

			_settings.VersionFormat = textBox_versionFormat.Text;
			SaveSettings();
		}


		// ============================================================================
		private void flipSwitch_enableAutomation_Toggled(object sender, EventArgs e)
		{
			if (CodeUpdate)
			{
				return;
			}

			_settings.EnableAutomation = flipSwitch_enableAutomation.IsOn;
			SaveSettings();
		}

		// ============================================================================
		private void flipSwitch_overwrite_Toggled(object sender, EventArgs e)
		{
			if (CodeUpdate)
			{
				return;

			}
			_settings.OverwriteCustomizations = flipSwitch_overwrite.IsOn;
			SaveSettings();

		}

		// ============================================================================
		private void flipSwitch_publishTarget_Toggled(object sender, EventArgs e)
		{
			if (CodeUpdate)
			{
				return;

			}
			_settings.PublishAllPostImport = flipSwitch_publishTarget.IsOn;
			SaveSettings();
		}


		// ============================================================================
		private void listSolutions_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (CodeUpdate)
			{
				return;
			}

			UpdateSolutionSettingsScreen();

			var rowIsSelected = listBoxSolutions.SelectedIndex != -1;

			button_browseManaged.Enabled = rowIsSelected;
			button_browseUnmananged.Enabled = rowIsSelected;

			SetExportButtonState();

		}



		// ============================================================================
		private void listBoxSolutions_ItemOrderChanged(object sender, EventArgs e)
		{
			if (CodeUpdate)
			{
				return;
			}

			UpdateItemSortingIndexesFromListBox();
			SaveSettings();
		}


		// ============================================================================
		private void textBox_managed_TextChanged(object sender, EventArgs e)
		{
			if (listBoxSolutions.SelectedIndex == -1 ||
				CodeUpdate)
			{
				return;
			}

			_currentSolutionConfig.FileNameManaged = textBox_managed.Text;
			_settings.UpdateSolutionConfiguration(_currentSolutionConfig);

			UpdateSolutionStatusImages(listBoxSolutions.SelectedIndex);
			UpdateTargetVersionStatusImages();
			SaveSettings();
		}

		// ============================================================================
		private void textBox_unmanaged_TextChanged(object sender, EventArgs e)
		{
			if (listBoxSolutions.SelectedIndex == -1 ||
				CodeUpdate)
			{
				return;
			}

			_currentSolutionConfig.FileNameUnmanaged = textBox_unmanaged.Text;
			_settings.UpdateSolutionConfiguration(_currentSolutionConfig);

			UpdateSolutionStatusImages(listBoxSolutions.SelectedIndex);
			UpdateTargetVersionStatusImages();
			SaveSettings();
		}


		// ============================================================================
		private void button_browseManaged_Click(object sender, EventArgs e)
		{
			var selectedSolution =
				listBoxSolutions.SelectedItem as Solution;

			if (selectedSolution == null)
			{
				return;
			}

			saveFileDialog1.Title = "Save Managed file as...";
			saveFileDialog1.Filter = "Solution files (*.zip)|*.zip|All files (*.*)|*.*";
			saveFileDialog1.FilterIndex = 0;

			if (!string.IsNullOrWhiteSpace(textBox_managed.Text))
			{
				saveFileDialog1.FileName = Path.GetFileName(textBox_managed.Text);
			}
			else
			{
				saveFileDialog1.FileName = GetDefaultFileName(selectedSolution, true);
			}

			if (saveFileDialog1.ShowDialog() == DialogResult.OK)
			{
				textBox_managed.Text = saveFileDialog1.FileName;
			}

			UpdateSolutionStatusImages(listBoxSolutions.SelectedIndex);
			UpdateTargetVersionStatusImages();
			UpdateFileWarningStatus();

			listBoxSolutions.Refresh();

			// No saving needed because the text field update triggers saving already
		}

		// ============================================================================
		private void button_browseUnmananged_Click(object sender, EventArgs e)
		{
			var selectedSolution =
			   listBoxSolutions.SelectedItem as Solution;

			if (selectedSolution == null)
			{
				return;
			}

			saveFileDialog1.Title = "Save Unmanaged file as...";
			saveFileDialog1.Filter = "Solution files (*.zip)|*.zip|All files (*.*)|*.*";
			saveFileDialog1.FilterIndex = 0;

			if (!string.IsNullOrWhiteSpace(textBox_unmanaged.Text))
			{
				saveFileDialog1.FileName = Path.GetFileName(textBox_unmanaged.Text);
			}
			else
			{
				saveFileDialog1.FileName = GetDefaultFileName(selectedSolution, false);
			}

			if (saveFileDialog1.ShowDialog() == DialogResult.OK)
			{
				textBox_unmanaged.Text = saveFileDialog1.FileName;
			}


			UpdateSolutionStatusImages(listBoxSolutions.SelectedIndex);
			UpdateTargetVersionStatusImages();
			UpdateFileWarningStatus();

			listBoxSolutions.Refresh();

			// No saving needed because the text field update triggers saving already
		}

		// ============================================================================
		private void button_Export_Click(object sender, EventArgs e)
		{
			listBoxSolutions.DeselectAll();
			UpdateSolutionSettingsScreen();

			if (!CheckIfAllSolutionFilesAreDefined())
			{
				return;
			}

			SetUiEnabledState(false);

			ExecuteOperations();
		}


		// ============================================================================
		private List<string> GetActionsList()
		{
			var actions = new List<string>();

			if (flipSwitch_updateVersion.IsOn)
			{
				actions.Add("Updating Version of");
			}

			if (flipSwitch_exportManaged.IsOn ||
				flipSwitch_exportUnmanaged.IsOn)
			{
				actions.Add("Exporting");
			}

			if (flipSwitch_importManaged.IsOn ||
				flipSwitch_importUnmanaged.IsOn)
			{
				actions.Add("Importing");
			}

			return actions;
		}


		// ============================================================================
		private void SetUiEnabledState(
			bool state)
		{
			listBoxSolutions.Enabled = state;

			flipSwitch_publishSource.IsLocked = !state;
			flipSwitch_updateVersion.IsLocked = !state;
			flipSwitch_exportManaged.IsLocked = !state;
			flipSwitch_exportUnmanaged.IsLocked = !state;

			flipSwitch_importManaged.IsLocked = !state;
			flipSwitch_importUnmanaged.IsLocked = !state;
			flipSwitch_enableAutomation.IsLocked = !state;
			flipSwitch_overwrite.IsLocked = !state;
			flipSwitch_upgrade.IsLocked = !state;

			flipSwitch_publishTarget.IsLocked = !state;
			flipSwitch_gitCommit.IsLocked = !state;
			flipSwitch_pushCommit.IsLocked = !state;

			button_browseManaged.Enabled = state;
			button_browseUnmananged.Enabled = state;

			textBox_versionFormat.Enabled = state;
			textBox_commitMessage.Enabled = state;
			textBox_managed.Enabled = state;
			textBox_unmanaged.Enabled = state;

			comboBox_gitBranches.Enabled = state;

			button_loadSolutions.Enabled = state;
			button_Export.Enabled = state;
			button_addAdditionalConnection.Enabled = state;
			button_manageConnections.Enabled = state;
			button_Settings.Enabled = state;
		}


		// ============================================================================
		private void listSolutions_ItemCheck(
			object sender,
			ItemEventArgs e)
		{
			if (CodeUpdate)
			{
				return;
			}

			button_fileWizzard.Enabled = listBoxSolutions.CheckedItems.Count > 0;

			SetExportButtonState();
			SaveSettings();
		}

		// ============================================================================
		private void button_addAdditionalConnection_Click(object sender, EventArgs e)
		{
			AddAdditionalOrganization();
		}

		// ============================================================================
		private void button_checkAll_Click(object sender, EventArgs e)
		{
			listBoxSolutions.CheckAllItems();
			SaveSettings();
		}

		// ============================================================================
		private void button_invertCheck_Click(object sender, EventArgs e)
		{
			listBoxSolutions.InvertCheckOfAllItems();
			SaveSettings();
		}

		// ============================================================================
		private void button_uncheckAll_Click(object sender, EventArgs e)
		{
			listBoxSolutions.UnCheckAllItems();
			SaveSettings();
		}


		// ============================================================================
		private void button_Settings_Click(
			object sender,
			EventArgs e)
		{
			SettingsForm settingsForm = new SettingsForm();
			settingsForm.Settings = _settings;
			settingsForm.ShowDialog();
			_settings = settingsForm.Settings;

			SaveSettings();
			UpdateColumns();
			UpdateSolutionList();
			UpdateAllConnectionTimeouts();
		}


		// ============================================================================
		private void button_exportCsv_Click(object sender, EventArgs e)
		{
			saveFileDialog1.Title = "Save solution information...";
			saveFileDialog1.FileName = "Solution Information.csv";
			saveFileDialog1.Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*";
			saveFileDialog1.FilterIndex = 0;

			if (saveFileDialog1.ShowDialog() == DialogResult.OK)
			{
				ExportSolutionCsv(saveFileDialog1.FileName);
			}
		}


		// ============================================================================
		private void button_manageConnections_Click(object sender, EventArgs e)
		{
			_connectionManager = new ConnectionManager(this);
			var dialogResult = _connectionManager.ShowDialog();
			_connectionManager = null;
		}


		// ============================================================================
		private void listBoxSolutions_SortingColumnChanged(object sender, EventArgs e)
		{
			if (_settings == null ||
				CodeUpdate)
			{
				return;
			}

			_settings.SortingColumnIndex = listBoxSolutions.SortingColumnIndex;
			_settings.SortingColumnOrder = listBoxSolutions.SortingColumnOrder;

			SaveSettings();
		}


		// ============================================================================
		private void comboBox_gitBranches_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (CodeUpdate)
			{
				return;
			}

			if (!_gitHelper.SwitchToBranch(comboBox_gitBranches.SelectedItem.ToString(), out string errorMessage))
			{
				UpdateGitBranches();

				LogError(errorMessage);
			}
		}

		// ============================================================================
		private void splitContainer1_SplitterMoved(object sender, SplitterEventArgs e)
		{
			if (_settings == null ||
				CodeUpdate ||
				CodeUpdateSplitter)
			{
				LogDebug($"OnSplitterMoved not executing - settingsNull: {_settings == null}, codeUpdate: {CodeUpdate}, codeUpdateSplitter {CodeUpdateSplitter}");
				return;
			}

			LogDebug("OnSplitterMoved: " + splitContainer1.SplitterDistance);
			_settings.SplitContainerPosition = splitContainer1.SplitterDistance;
			SaveSettings();
		}

		// ============================================================================
		private void button_fileWizzard_Click(object sender, EventArgs e)
		{
			if (folderBrowserDialog1.ShowDialog() != DialogResult.OK)
			{
				return;
			}

			foreach (var selectedSolution in listBoxSolutions.CheckedItems)
			{
				var solution = selectedSolution.ItemObject as Solution;

				if (solution == null)
				{
					continue;
				}

				var solutionConfig =
					_settings.GetSolutionConfiguration(
						solution.SolutionIdentifier);

				if (solutionConfig == null)
				{
					continue;
				}

				solutionConfig.FileNameManaged =
					Path.Combine(
						folderBrowserDialog1.SelectedPath,
						GetDefaultFileName(solution, true));

				solutionConfig.FileNameUnmanaged =
					Path.Combine(
						folderBrowserDialog1.SelectedPath,
						GetDefaultFileName(solution, false));


				_settings.UpdateSolutionConfiguration(solutionConfig);
			}

			UpdateSolutionList();
			UpdateSolutionSettingsScreen();

			// Show a pop-up to inform the user that the process has finished
			MessageBox.Show(
				"The file locations have been updated.",
				"Process Complete",
				MessageBoxButtons.OK,
				MessageBoxIcon.Information);
		}



		// ============================================================================
		private void ExecuteOperations()
		{
			richTextBox_log.Text = string.Empty;
			_sessionFiles.Clear();

			var message = string.Join(" / ", GetActionsList()) + " Solutions...";

			_executionWorker = new WorkAsyncInfo()
			{
				Message = message,
				MessageWidth = _workerPanelSize.Width,
				MessageHeight = _workerPanelSize.Height,
				IsCancelable = true,
				Work = (worker, args) =>
				{
					if (flipSwitch_publishSource.IsOn)
					{
						worker.ReportProgress(0, $"Publishing all...{Environment.NewLine}[{ConnectionDetail.ConnectionName}]");
						PublishAll(ConnectionDetail);
					}

					UpdateCheckedVersionNumbers(worker);
					ExportCheckedSolutions(worker);
					HandleGit(worker);

					foreach (var targetConnection in TargetConnections)
					{

						Log(ColorTeeth + ":::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::" + ColorEndTag);
						Log($"##### Handling Target *" + ColorConnection + targetConnection.ConnectionName + ColorEndTag + "*:");
						Log(ColorTeeth + ":::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::" + ColorEndTag);

						Log();

						ImportCheckedSolutions(
							targetConnection,
							worker);

						if (flipSwitch_publishTarget.IsOn)
						{
							worker.ReportProgress(0, $"Publishing all [{targetConnection.ConnectionName}]...");
							PublishAll(targetConnection);
						}
					}


					args.Result = null;
				},

				PostWorkCallBack = (args) =>
				{
					if (args.Error != null)
					{
						MessageBox.Show(
							args.Error.ToString(),
							"Error",
							MessageBoxButtons.OK,
							MessageBoxIcon.Error);
					}

					LoadAllSolutions();

					Log(ColorGreen + "##### Done." + ColorEndTag);

					SetUiEnabledState(true);
				},

				ProgressChanged = (args) =>
				{
					var workerMessage = args.UserState.ToString();
					SetWorkingMessage(workerMessage, _workerPanelSize.Width, _workerPanelSize.Height);
					StartProgressTimer(workerMessage);
				},
			};

			WorkAsync(_executionWorker);
		}


		// ============================================================================
		/// <summary>
		/// Set a new working message. 
		/// Sending an empty message will stop the progress timer.
		/// </summary>
		/// <param name="message"></param>
		private new void SetWorkingMessage(
			string message = null,
			int width = 340,
			int height = 150)
		{
			if (string.IsNullOrEmpty(message))
			{
				StopProgressTimer();
			}

			base.SetWorkingMessage(message, width, height);
		}


		// ============================================================================
		// Call this method to start the timer and set the base message
		private void StartProgressTimer(
			string message)
		{
			StopProgressTimer();

			_progressBaseMessage = message;
			_progressStartTime = DateTime.Now;

			if (_progressTimer == null)
			{
				_progressTimer = new Timer();
				_progressTimer.Interval = 1000; // 1 second
				_progressTimer.Tick += ProgressTimer_Tick;
			}

			_progressTimer.Start();
		}

		// ============================================================================
		// Call this method to stop the timer
		private void StopProgressTimer()
		{
			if (_progressTimer != null)
			{
				_progressTimer.Stop();
			}
		}


		// ============================================================================
		// Timer tick event handler
		private void ProgressTimer_Tick(object sender, EventArgs e)
		{
			var elapsed = DateTime.Now - _progressStartTime;
			var elapsedStr = GetDurationString(_progressStartTime, true);

			SetWorkingMessage(
				$"{_progressBaseMessage}{Environment.NewLine}{elapsedStr}",
				_workerPanelSize.Width,
				_workerPanelSize.Height);
		}


		// ============================================================================
		public void RemoveConnection(
			ConnectionDetail connection)
		{
			RemoveAdditionalOrganization(connection);

			_connectionManager?.UpdateConnections();
		}


		// ============================================================================
		public void AddConnection()
		{
			AddAdditionalOrganization();

			_connectionManager?.UpdateConnections();
		}


		// ============================================================================
		private void UpdateSolutionSettingsScreen()
		{
			if (listBoxSolutions.SelectedIndex == -1)
			{
				CodeUpdate = true;

				_currentSolution = null;
				_currentSolutionConfig = null;

				button_browseManaged.Enabled = false;
				button_browseUnmananged.Enabled = false;

				textBox_managed.Text = string.Empty;
				textBox_unmanaged.Text = string.Empty;

				label_title.Text = "No Solution selected (click a solution in the list)";
				CodeUpdate = false;

				return;
			}

			_currentSolution =
				(listBoxSolutions.SelectedItem as Solution);

			_currentSolutionConfig =
				_settings.GetSolutionConfiguration(
					_currentSolution.SolutionIdentifier);

			if (_currentSolutionConfig == null)
			{
				_settings.AddSolutionConfiguration(
					_currentSolution.SolutionIdentifier);
			}

			CodeUpdate = true;
			textBox_managed.Text = _currentSolutionConfig.FileNameManaged;
			textBox_unmanaged.Text = _currentSolutionConfig.FileNameUnmanaged;
			CodeUpdate = false;

			button_browseManaged.Enabled = true;
			button_browseUnmananged.Enabled = true;

			label_title.Text = _currentSolution.FriendlyName;

			UpdateFileWarningStatus();
		}


		// ============================================================================
		private void UpdateFileWarningStatus()
		{
			if (_currentSolution == null ||
				_currentSolutionConfig == null ||
				_settings.SaveVersionJson == false)
			{
				pictureBox_warningManaged.Visible = false;
				pictureBox_warningUnmanaged.Visible = false;

				return;
			}

			var isManagedVersionOutdated = false;
			var isUnmanagedVersionOutdated = false;


			if (_currentSolutionConfig.FileNameManaged.HasValue())
			{
				var managedSolutionFileExists =
					File.Exists(_currentSolutionConfig.FileNameManaged);

				var loadedManaged =
					ExportedSolutionVersions.TryLoadExportedSolution(
						_currentSolutionConfig.FileNameManaged,
						_currentSolution,
						out var exportedSolutionManaged);

				if (exportedSolutionManaged != null)
				{
					isManagedVersionOutdated =
						exportedSolutionManaged.Version != _currentSolution.Version.ToString() ||
						managedSolutionFileExists == false;
				}
				else
				{
					isManagedVersionOutdated = !managedSolutionFileExists;
				}
			}

			if (_currentSolutionConfig.FileNameUnmanaged.HasValue())
			{
				var unmanagedSolutionFileExists =
					File.Exists(_currentSolutionConfig.FileNameUnmanaged);

				var loadedUnmanaged =
					ExportedSolutionVersions.TryLoadExportedSolution(
						_currentSolutionConfig.FileNameManaged,
						_currentSolution,
						out var exportedSolutionUnmanaged);

				if (exportedSolutionUnmanaged != null)
				{
					isUnmanagedVersionOutdated =
						exportedSolutionUnmanaged.Version != _currentSolution.Version.ToString() ||
						unmanagedSolutionFileExists == false;
				}
				else
				{
					isUnmanagedVersionOutdated = !unmanagedSolutionFileExists;
				}
			}

			pictureBox_warningManaged.Visible = isManagedVersionOutdated;
			pictureBox_warningUnmanaged.Visible = isUnmanagedVersionOutdated;
		}


		// ============================================================================
		private void PublishAll(
			ConnectionDetail connection)
		{
			Log("##### Publishing All now (" + ColorConnection + connection.ConnectionName + ColorEndTag + ")");
			_logger.IncreaseIndent();

			var startTime = DateTime.Now;

			Log("Start Time: " + startTime.ToString(dateTimeFormat));

			PublishAllXmlRequest publishRequest =
				new PublishAllXmlRequest();

			try
			{
				connection.ServiceClient.Execute(publishRequest);
			}
			catch (Exception ex)
			{
				LogError(ex.Message);
				_logger.DecreaseIndent();
				return;
			}

			var duration = GetDurationString(startTime);

			Log("Publish All was completed.");
			Log($"Duration: {duration}");

			_logger.DecreaseIndent();
			Log();
		}


		// ============================================================================
		private void UpdateCheckedVersionNumbers(
			BackgroundWorker worker)
		{
			if (!flipSwitch_updateVersion.IsOn ||
				listBoxSolutions.CheckedItems.Count == 0)
			{
				return;
			}

			worker.ReportProgress(0, "Updating Version Numbers...");

			Log("##### Updating Version Numbers:");
			_logger.IncreaseIndent();


			for (int i = 0; i < listBoxSolutions.CheckedItems.Count; i++)
			{
				var listItem = listBoxSolutions.CheckedItems[i];
				var solution = listItem.ItemObject as Solution;

				var solutionConfig =
					_settings.GetSolutionConfiguration(
						solution.SolutionIdentifier);

				Log("Updating version number for solution " + ColorSolution + "'" + solution.FriendlyName + "'" + ColorEndTag);

				UpdateVersionNumberInSource(
					solution);

				if (i < listBoxSolutions.CheckedItems.Count - 1)
				{
					Log();
				}
			}

			_logger.DecreaseIndent();
			Log();
		}



		// ============================================================================
		private void ExportCheckedSolutions(
			BackgroundWorker worker)
		{
			if (flipSwitch_exportManaged.IsOff &&
				flipSwitch_exportUnmanaged.IsOff)
			{
				return;
			}

			Log("##### Exporting:");
			_logger.IncreaseIndent();

			for (int i = 0; i < listBoxSolutions.CheckedItems.Count; i++)
			{
				var listItem = listBoxSolutions.CheckedItems[i];
				var solution = listItem.ItemObject as Solution;

				var solutionConfig =
					_settings.GetSolutionConfiguration(
						solution.SolutionIdentifier);

				Log("**Exporting solution " + ColorSolution + "'" + solution.FriendlyName + "'" + ColorEndTag + ":**");

				var exportResult =
					ExportSolution(
						solution,
						solutionConfig,
						worker);

				if (!exportResult)
				{
					Log();
					LogError("**Export aborted due to an error!**");
					break;
				}

				if (i < listBoxSolutions.CheckedItems.Count - 1)
				{
					Log();
				}
			}

			_logger.DecreaseIndent();
			Log();
		}


		// ============================================================================
		private void SaveVersionJson(
			string exportedFilePath,
			Solution solution,
			bool isManaged)
		{
			var exportParth =
				Path.GetDirectoryName(
					exportedFilePath);

			var jsonFilePath =
				Path.Combine(
					exportParth,
					BulkSolutionExporter_Plugin.JsonSettingsFileName);

			var exportedSolutions = new ExportedSolutionVersions();

			if (File.Exists(jsonFilePath))
			{
				string json = File.ReadAllText(jsonFilePath);
				exportedSolutions = JsonConvert.DeserializeObject<ExportedSolutionVersions>(json);
			}

			var found = false;
			var fileName = Path.GetFileName(exportedFilePath);

			if (isManaged)
			{
				for (int i = 0; i < exportedSolutions.Managed.Count; i++)
				{
					if (exportedSolutions.Managed[i].LogicalSolutionName == solution.UniqueName &&
						exportedSolutions.Managed[i].FileName == fileName)
					{
						exportedSolutions.Managed[i].Version = solution.Version.ToString();
						found = true;
					}
				}
			}
			else
			{
				for (int i = 0; i < exportedSolutions.Unmanaged.Count; i++)
				{
					if (exportedSolutions.Unmanaged[i].LogicalSolutionName == solution.UniqueName &&
						exportedSolutions.Unmanaged[i].FileName == fileName)
					{
						exportedSolutions.Unmanaged[i].Version = solution.Version.ToString();
						found = true;
					}
				}
			}

			if (!found)
			{
				var newExportSolution = new ExportedSolution
				{
					SolutionId = solution.SolutionId.ToString().ToLower(),
					FileName = fileName,
					Version = solution.Version.ToString(),
					LogicalSolutionName = solution.UniqueName

				};

				if (isManaged)
				{
					exportedSolutions.Managed.Add(newExportSolution);
				}
				else
				{
					exportedSolutions.Unmanaged.Add(newExportSolution);
				}
			}

			var settings = new JsonSerializerSettings();
			settings.Formatting = Formatting.Indented;
			string outputJson = JsonConvert.SerializeObject(exportedSolutions, settings);
			File.WriteAllText(jsonFilePath, outputJson);
		}


		// ============================================================================
		private void ImportCheckedSolutions(
			ConnectionDetail targetService,
			BackgroundWorker worker)
		{
			var targetServiceClient = targetService?.ServiceClient;

			if (targetServiceClient == null ||
				(
					flipSwitch_importManaged.IsOff &&
					flipSwitch_importUnmanaged.IsOff
				))
			{
				return;
			}

			bool importManagd = flipSwitch_importManaged.IsOn;

			Log("##### Importing:");
			_logger.IncreaseIndent();

			for (int i = 0; i < listBoxSolutions.CheckedItems.Count; i++)
			{
				var listItem = listBoxSolutions.CheckedItems[i];
				var solution = listItem.ItemObject as Solution;

				var solutionConfig =
					_settings.GetSolutionConfiguration(
						solution.SolutionIdentifier);

				var startTime = DateTime.Now;

				Log("**Importing solution " + ColorSolution + "'" + solution.FriendlyName + "'" + ColorEndTag + ":**");

				if (ImportSolution(targetService, solution, worker, importManagd))
				{
					_logger.IncreaseIndent();
					var duration = GetDurationString(startTime);
					Log("The import was successful.");
					Log("Duration: " + duration);

					if (i < listBoxSolutions.CheckedItems.Count - 1)
					{
						Log();
					}

					_logger.DecreaseIndent();
				}
			}

			_logger.DecreaseIndent();
			Log();
		}



		// ============================================================================
		private bool ExportSolution(
			Solution solution,
			SolutionConfiguration solutionConfiguration,
			BackgroundWorker worker)
		{
			_logger.IncreaseIndent();

			if (flipSwitch_exportManaged.IsOn)
			{
				worker.ReportProgress(
					0,
					$"Exporting as managed...{Environment.NewLine}'{solution.FriendlyName}'");

				ExportToFile(
					solution,
					solutionConfiguration,
					true);
			}


			if (flipSwitch_exportManaged.IsOn &&
				flipSwitch_exportUnmanaged.IsOn)
			{
				Log();
			}

			if (flipSwitch_exportUnmanaged.IsOn)
			{
				worker.ReportProgress(
					0,
					$"Exporting as unmanged...{Environment.NewLine}'{solution.FriendlyName}'");

				ExportToFile(
					solution,
					solutionConfiguration,
					false);
			}

			_logger.DecreaseIndent();

			return true;
		}


		// ============================================================================
		private void ExportToFile(
			Solution solution,
			SolutionConfiguration solutionConfiguration,
			bool isManaged)
		{
			Log("Exporting as " + (isManaged ? "" : "un") + "managed solution now...");
			_logger.IncreaseIndent();


			if (isManaged && string.IsNullOrWhiteSpace(solutionConfiguration.FileNameManaged) ||
				!isManaged && string.IsNullOrWhiteSpace(solutionConfiguration.FileNameUnmanaged))
			{
				LogError("No file name for the " + (isManaged ? "" : "un") + "managed solution was given!");
				_logger.DecreaseIndent();
				return;
			}

			var startTime = DateTime.Now;

			Log("Start Time: " + startTime.ToString(dateTimeFormat));

			var exportSolutionRequest = new ExportSolutionRequest
			{
				SolutionName = solution.UniqueName,
				Managed = isManaged
			};

			var exportSolutionResponse =
				(ExportSolutionResponse) Service.Execute(
					exportSolutionRequest);

			var filePath =
				isManaged ? solutionConfiguration.FileNameManaged : solutionConfiguration.FileNameUnmanaged;

			string directoryPath = Path.GetDirectoryName(filePath);

			// Check if the directory exists
			if (!Directory.Exists(directoryPath))
			{
				// Create the directory
				Directory.CreateDirectory(directoryPath);
			}

			// Save the exported solution to a file
			File.WriteAllBytes(
				filePath,
				exportSolutionResponse.ExportSolutionFile);

			_sessionFiles.Add(filePath);

			// update the settings
			var configToBeUpdated =
				_settings.GetSolutionConfiguration(
					solutionConfiguration.SolutionIndentifier);

			var duration = GetDurationString(startTime);
			Log("Exported to file: " + ColorFile + "" + filePath + "" + ColorEndTag);
			Log("Duration: " + duration);

			if (_settings.SaveVersionJson)
			{
				SaveVersionJson(
					filePath,
					solution,
					isManaged);
			}

			_logger.DecreaseIndent();


		}



		// ============================================================================
		private bool UpdateVersionNumberInSource(
			Solution solution)
		{
			_logger.IncreaseIndent();

			var oldVersionString = solution.Version.ToString();

			var versionUpdateResult =
				solution.Version.IncreaseVersionNumber(
					textBox_versionFormat.Text);

			if (!versionUpdateResult)
			{
				LogError("The version format is invalid!");
				_logger.DecreaseIndent();
				return false;
			}

			var solutionToBeUpdated =
				new Entity(
					"solution",
					solution.SolutionId);

			solutionToBeUpdated.Attributes.Add(
				"version",
				solution.Version.ToString());

			Service.Update(solutionToBeUpdated);

			Log("Updated the version number: **" + oldVersionString + "** → **" + solution.Version + "**");

			// update the list as well:
			for (int i = 0; i < listBoxSolutions.Items.Count; i++)
			{
				var listItem = listBoxSolutions.Items[i];
				var solutionItem = listItem.ItemObject as Solution;

				if (solutionItem.SolutionIdentifier == solution.SolutionIdentifier)
				{
					listBoxSolutions.Items[i].ItemObject = solution;
					listBoxSolutions.Invalidate();
					break;
				}
			}

			_logger.DecreaseIndent();
			return true;
		}


		// ============================================================================
		private void InitializeGitHelper()
		{
			string filePath = string.Empty;

			foreach (var configString in _settings.SolutionConfigurations)
			{
				var config = SolutionConfiguration.GetConfigFromJson(configString);

				if (config.FileNameManaged.HasValue())
				{
					filePath = config.FileNameManaged;
					break;
				}

				if (config.FileNameUnmanaged.HasValue())
				{
					filePath = config.FileNameUnmanaged;
					break;
				}
			}

			if (filePath.IsEmpty())
			{
				return;
			}

			var gitRootPath =
				FileAndFolderHelper.FindRepositoryRootPath(
					filePath,
					out string message);

			LogDebug();
			LogDebug(message);
			LogDebug();

			if (gitRootPath.IsEmpty())
			{
				LogDebug("No Git-Root path determined within: " + filePath);
				return;
			}

			LogDebug("No Git-Root path is: " + gitRootPath);

			_gitHelper = new GitHelper(gitRootPath);

			UpdateGitBranches();
		}


		// ============================================================================
		private void UpdateGitBranches()
		{
			if (_gitHelper == null)
			{
				return;
			}

			_gitBranches = _gitHelper.GetAllBranches();

			LogDebug("Retrieved Git Branches: " + string.Join(", ", _gitBranches));

			CodeUpdate = true;
			comboBox_gitBranches.Items.Clear();
			comboBox_gitBranches.Items.AddRange(_gitBranches.ToArray());
			comboBox_gitBranches.SelectedIndex = _gitBranches.IndexOf(_gitHelper.GetActiveBranch());
			CodeUpdate = false;
		}


		// ============================================================================
		private void HandleGit(
			BackgroundWorker worker)
		{
			if (flipSwitch_gitCommit.IsOff ||
				_gitHelper == null)
			{
				return;
			}

			worker.ReportProgress(
				0,
				"Committing files to Git...");

			Log("##### Commiting the new files to Git:");
			_logger.IncreaseIndent();

			if (_sessionFiles.Count == 0)
			{
				Log("No files need to be committed.");
				_logger.DecreaseIndent();
				Log();
				return;
			}


			string errorMessage;

			foreach (var file in _sessionFiles)
			{
				var fileShortened = file.Replace(_gitHelper.GitRootDirectory, "");

				if (fileShortened.StartsWith("\\"))
				{
					// remove the first backslash
					fileShortened = fileShortened.Substring(1);
				}

				if (!_gitHelper.ExecuteCommand("add \"" + fileShortened + "\"", out _, out errorMessage))
				{
					LogError("Error staging: *" + errorMessage + "*");
				}
			}

			var message = textBox_commitMessage.Text;


			if (!_gitHelper.ExecuteCommand("commit -m \"" + message + "\"", out _, out errorMessage))
			{
				LogError("Error commiting: *" + errorMessage + "*");
			}
			else
			{
				Log(_sessionFiles.Count + " File(s) was/weres commited: " + message);
			}


			if (flipSwitch_pushCommit.IsOff)
			{
				_logger.DecreaseIndent();
				Log();
				return;
			}

			if (!_gitHelper.ExecuteCommand("push", out _, out errorMessage))
			{
				LogError("Error pushing: *" + errorMessage + "*");
			}
			else
			{
				Log("The commit has been pushed to the remote origin.");
			}

			_logger.DecreaseIndent();
			Log();
		}


		// ============================================================================
		private bool ImportSolution(
			ConnectionDetail targetService,
			Solution solution,
			BackgroundWorker worker,
			bool isManaged = true)
		{
			_logger.IncreaseIndent();

			var targetServiceClient = targetService?.ServiceClient;

			var config =
				_settings.GetSolutionConfiguration(
					solution.SolutionIdentifier);

			if (config == null)
			{
				_logger.DecreaseIndent();
				return false;
			}

			string solutionPath;

			if (isManaged)
			{
				solutionPath = config.FileNameManaged;
			}
			else
			{
				solutionPath = config.FileNameUnmanaged;
			}

			if (!File.Exists(solutionPath))
			{
				LogError("The file '" + solutionPath + "' does not exist.");
				_logger.DecreaseIndent();
				return false;
			}

			var solutionBytes = File.ReadAllBytes(solutionPath);
			var isHolding = isManaged && flipSwitch_upgrade.IsOn;

			try
			{

				// Create import request
				var importRequest = new ImportSolutionRequest()
				{
					CustomizationFile = solutionBytes,
					PublishWorkflows = flipSwitch_enableAutomation.IsOn,
					OverwriteUnmanagedCustomizations = flipSwitch_overwrite.IsOn,
					ConvertToManaged = isManaged,
					HoldingSolution = isHolding,
				};

				if (isHolding)
				{
					Log("Installing the solution upgrade...");
					Log("Start Time: " + DateTime.Now.ToString(dateTimeFormat));

					worker.ReportProgress(
						0,
						$"Installing upgrade: '{solution.FriendlyName}'{Environment.NewLine}[{targetService.ConnectionName}]...");
				}
				else
				{
					Log("Installing the solution update...");
					Log("Start Time: " + DateTime.Now.ToString(dateTimeFormat));

					worker.ReportProgress(
						0,
						$"Installing update: '{solution.FriendlyName}'{Environment.NewLine}[{targetService.ConnectionName}]...");
				}

				targetServiceClient.Execute(importRequest);

				if (isHolding)
				{
					Log();
					Log("Applying the solution upgrade...");
					Log("Start Time: " + DateTime.Now.ToString(dateTimeFormat));

					worker.ReportProgress(
						0,
						$"Applying upgrade: '{solution.FriendlyName}'{Environment.NewLine}[{targetService.ConnectionName}]...");

					var applyUpgradeRequest = new DeleteAndPromoteRequest
					{
						UniqueName = solution.UniqueName,
					};

					targetServiceClient.Execute(applyUpgradeRequest);
				}
			}

			catch (FaultException ex)
			{
				var errorMessage =
					Formatter.FormatErrorStringWithXml(
						ex.Message,
						_logger.Indent);

				LogError(errorMessage);
				Log();
				_logger.DecreaseIndent();
				return false;
			}
			catch (Exception ex)
			{
				var errorMessage =
					Formatter.FormatErrorStringWithXml(
						ex.Message,
						_logger.Indent);

				LogError(errorMessage);
				Log();
				_logger.DecreaseIndent();
				return false;
			}

			_logger.DecreaseIndent();
			return true;
		}


		// ============================================================================
		private void UpdateItemSortingIndexesFromListBox()
		{
			for (int i = 0; i < listBoxSolutions.Items.Count; i++)
			{
				(listBoxSolutions.Items[i].ItemObject as Solution).SortingIndex = i;

				var config =
					_settings.GetSolutionConfiguration(
						(listBoxSolutions.Items[i].ItemObject as Solution).SolutionIdentifier);

				config.SortingIndex = i;

				_settings.UpdateSolutionConfiguration(
					config);
			}
		}


		// ============================================================================
		private void LoadAllSolutions()
		{
			WorkAsync(new WorkAsyncInfo
			{
				Message = "**Getting All Solutions**",
				IsCancelable = false,
				Work = (worker, args) =>
				{
					// ORIGIN SOLUTIONS
					var queryOrigin = new QueryExpression("solution");

					queryOrigin.Criteria.AddCondition(
						"ismanaged",
						Microsoft.Xrm.Sdk.Query.ConditionOperator.Equal,
						false);

					queryOrigin.Criteria.AddCondition(
						"isvisible",
						Microsoft.Xrm.Sdk.Query.ConditionOperator.Equal,
						true);

					queryOrigin.ColumnSet.AddColumns(
						"friendlyname",
						"solutiontype",
						"ismanaged",
						"solutionid",
						"uniquename",
						"description",
						"version");

					// TARGET SOLUTIONS
					var queryTarget = new QueryExpression("solution");

					queryTarget.Criteria.AddCondition(
						"isvisible",
						Microsoft.Xrm.Sdk.Query.ConditionOperator.Equal,
						true);

					queryTarget.ColumnSet.AddColumns(
						"friendlyname",
						"solutiontype",
						"ismanaged",
						"solutionid",
						"uniquename",
						"description",
						"version");

					LoadSolutionsResult result =
						new LoadSolutionsResult();

					if (Service != null)
					{
						result.OriginsSolutions =
							Service.RetrieveMultiple(
								queryOrigin);
					}

					if (TargetConnections?.Count == 1)
					{
						result.TargetSolutions =
							TargetConnections[0].GetCrmServiceClient().RetrieveMultiple(
								queryTarget);
					}

					args.Result = result;
				},
				PostWorkCallBack = (args) =>
				{
					if (args.Error != null)
					{
						MessageBox.Show(
							args.Error.ToString(),
							"Error",
							MessageBoxButtons.OK,
							MessageBoxIcon.Error);

						return;
					}

					var result = args.Result as LoadSolutionsResult;

					if (result == null)
					{
						return;
					}

					if (result.OriginsSolutions != null)
					{
						_solutionsOrigin.Clear();

						// sort alphabetically after loading
						var sortedSolutionsEntities =
							result.OriginsSolutions.Entities.OrderBy(
								item => item.GetAttributeValue<string>("friendlyname")).ToList();

						// create our custom solutions records
						foreach (var entity in sortedSolutionsEntities)
						{
							var newSolution =
								Solution.ConvertFrom(
									entity,
									_currentConnectionGuid);

							var config =
								_settings.GetSolutionConfiguration(
									newSolution.SolutionIdentifier,
									true);

							newSolution.SortingIndex = config.SortingIndex;

							_solutionsOrigin.Add(
								newSolution);
						}
					}


					if (result.TargetSolutions != null)
					{
						_solutionsTarget.Clear();

						// sort alphabetically after loading
						var sortedSolutionsEntities =
							result.TargetSolutions.Entities.OrderBy(
								item => item.GetAttributeValue<string>("friendlyname")).ToList();

						// create our custom solutions records
						foreach (var entity in sortedSolutionsEntities)
						{
							var newSolution =
								Solution.ConvertFrom(
									entity,
									_currentConnectionGuid);

							var config =
								_settings.GetSolutionConfiguration(
									newSolution.SolutionIdentifier,
									true);

							newSolution.SortingIndex = config.SortingIndex;

							_solutionsTarget.Add(
								newSolution);
						}
					}

					SaveSettings(cleanUpNonExistingSolutions: true);

					UpdateColumns();
					UpdateSolutionList();
					UpdateImportOptionsVisibility();
					SetExportButtonState();
				}
			});

		}


		// ============================================================================
		private void SetExportButtonState()
		{
			var versionIsInvalid =
				!Com.AiricLenz.XTB.Plugin.Schema.Version.ValidateFormat(
					textBox_versionFormat.Text);

			var exportEnabled =
				(listBoxSolutions.CheckedItems.Count > 0) &&
				!(flipSwitch_gitCommit.IsOn && (textBox_commitMessage.Text.Length < 5)) &&
				!(flipSwitch_updateVersion.IsOn && versionIsInvalid) &&
				(
					flipSwitch_exportManaged.IsOn ||
					flipSwitch_exportUnmanaged.IsOn ||
					flipSwitch_updateVersion.IsOn ||
					flipSwitch_importManaged.IsOn ||
					flipSwitch_importUnmanaged.IsOn
				);

			button_Export.Enabled = exportEnabled;


			// Version Format
			if (flipSwitch_updateVersion.IsOn &&
				versionIsInvalid)
			{
				label_versionFormat.ForeColor = Color.Red;
				label_versionFormat.Font = new Font(label_commitMessage.Font, FontStyle.Bold);
			}
			else
			{
				label_versionFormat.ForeColor = SystemColors.ControlText;
				label_versionFormat.Font = new Font(label_commitMessage.Font, FontStyle.Regular);
			}

			// Commit Message Text
			if (flipSwitch_gitCommit.IsOn &&
				textBox_commitMessage.Text.Length < 5)
			{
				label_commitMessage.ForeColor = Color.Red;
				label_commitMessage.Font = new Font(label_commitMessage.Font, FontStyle.Bold);
			}
			else
			{
				label_commitMessage.ForeColor = SystemColors.ControlText;
				label_commitMessage.Font = new Font(label_commitMessage.Font, FontStyle.Regular);
			}

		}


		// ============================================================================
		private void UpdateSolutionList()
		{
			listBoxSolutions.Items.Clear();

			_solutionsOrigin.Sort();

			for (int i = 0; i < _solutionsOrigin.Count; i++)
			{
				var solution = _solutionsOrigin[i];

				listBoxSolutions.Items.Add(
					new SortableCheckItem(solution, i));
			}

			if (_settings == null)
			{
				return;
			}

			// select the item in the list that were selected before
			// (information from the saved settings)
			foreach (var configString in _settings.SolutionConfigurations)
			{
				var config =
					SolutionConfiguration.GetConfigFromJson(
						configString);

				for (int i = 0; i < listBoxSolutions.Items.Count; i++)
				{
					var solution = listBoxSolutions.Items[i].ItemObject as Solution;

					if (solution.SolutionIdentifier == config.SolutionIndentifier)
					{
						listBoxSolutions.SetItemChecked(i, config.Checked);
						UpdateSolutionStatusImages(i);
					}
				}
			}

			UpdateTargetVersionStatusImages();
			listBoxSolutions.Refresh();
			SetExportButtonState();

		}


		// ============================================================================
		private void SaveSettings(
			[CallerMemberName] string caller = "",
			bool cleanUpNonExistingSolutions = false)
		{
			if (CodeUpdate)
			{
				return;
			}

			// remove solution-configuration from the config
			// that have no solution in this environment anymore
			if (cleanUpNonExistingSolutions)
			{
				RemoveNonExistantSolutions();
			}

			// Update the Solution Configs from the solutions...
			foreach (var listBoxItem in listBoxSolutions.Items)
			{
				var solution = listBoxItem.ItemObject as Solution;
				var config = _settings.GetSolutionConfiguration(solution.SolutionIdentifier, true);

				config.Checked = listBoxItem.IsChecked;
				config.SortingIndex = listBoxItem.SortingIndex;

				_settings.UpdateSolutionConfiguration(config);
			}

			// Save
			SettingsManager.Instance.Save(GetType(), _settings);

			LogDebug($"Settings have been saved ({caller}): ({_settings.SplitContainerPosition})");
		}


		// ============================================================================
		private void RemoveNonExistantSolutions()
		{
			var configsForCurrentConnection =
					_settings.GetAllSolutionConfigurationdForConnection(
						_currentConnectionGuid);

			foreach (var solution in _solutionsOrigin)
			{
				var found = false;

				foreach (var config in configsForCurrentConnection)
				{
					if (solution.SolutionIdentifier == config.SolutionIndentifier)
					{
						found = true;
						break;
					}
				}

				if (!found)
				{
					_settings.RemoveSolutionConfiguration(
						solution.SolutionIdentifier);
				}
			}
		}

		// ============================================================================
		private bool CheckIfAllSolutionFilesAreDefined()
		{

			if (!flipSwitch_exportManaged.IsOn &&
				!flipSwitch_exportUnmanaged.IsOn)
			{
				return true;
			}

			var problemsFound = false;
			var message = string.Empty;

			foreach (var item in listBoxSolutions.CheckedItems)
			{
				var solution =
					(item.ItemObject as Solution);

				var solutionConfig =
					_settings.GetSolutionConfiguration(
						solution.SolutionIdentifier);


				if (solutionConfig.FileNameManaged.IsEmpty() &&
					flipSwitch_exportManaged.IsOn)
				{
					if (!message.IsEmpty())
					{
						message += "," + Environment.NewLine;
					}
					message += "\"" + solution.FriendlyName + "\" (Managed)";

					problemsFound = true;
				}

				if (solutionConfig.FileNameUnmanaged.IsEmpty() &&
					flipSwitch_exportUnmanaged.IsOn)
				{
					if (!message.IsEmpty())
					{
						message += "," + Environment.NewLine;
					}
					message += "\"" + solution.FriendlyName + "\" (Unmanaged)";

					problemsFound = true;
				}
			}

			if (!problemsFound)
			{
				return true;
			}

			DialogResult result =
				MessageBox.Show(
					"These files were not defined:" +
					Environment.NewLine + Environment.NewLine +
					message + Environment.NewLine + Environment.NewLine +
					"Do you want to continue anyway?",
					"Problems were found",
					MessageBoxButtons.OKCancel,
					MessageBoxIcon.Warning);

			return result == DialogResult.OK;

		}


		// ============================================================================
		/// <summary>
		/// A simplified parser that tries to handle a few basic tokens:
		/// - **bold** text
		/// - *italic* text
		/// - `code` (colored)
		/// - # Heading (rest of line in bold, bigger font)
		/// - <color=#000066>colored</color> text
		/// - Regular text
		/// </summary>
		/// <param name="message"></param>
		private void Log(string message = "")
		{
			if (richTextBox_log.InvokeRequired)
			{
				richTextBox_log.Invoke(new Action(() => _logger.Log(message)));
				richTextBox_log.Invoke(new Action(() => richTextBox_log.SelectionStart = richTextBox_log.Text.Length));
				richTextBox_log.Invoke(new Action(() => richTextBox_log.ScrollToCaret()));
			}
			else
			{
				_logger.Log(message);
				richTextBox_log.SelectionStart = richTextBox_log.Text.Length;
				richTextBox_log.ScrollToCaret();
			}
		}

		// ============================================================================
		private void LogDebug(string message = "")
		{
			// only allow debug messages when compiled as debug
#if DEBUG

			if (!isDebugMode)
			{
				return;
			}

			Log(message);
#endif
		}


		// ============================================================================
		private void LogError(string message)
		{
			message = ColorError + message + ColorEndTag;
			Log(message);
		}

		// ============================================================================
		static string GetDurationString(
			DateTime startTime,
			bool noMilliseconds = false)
		{
			var duration = DateTime.Now - startTime;

			// Extract components from the TimeSpan
			int hours = duration.Hours;
			int minutes = duration.Minutes;
			double seconds = duration.TotalSeconds % 60;

			// Build the formatted string dynamically
			string result = "";

			if (hours > 0)
			{
				result += $"{hours}h ";
			}

			if (minutes > 0 ||
				hours > 0)
			{
				if (hours > 0)
				{
					result += $"{minutes:D2}min ";
				}
				else
				{
					result += $"{minutes}min ";
				}

			}

			if (seconds > 0 ||
				hours > 0 ||
				minutes > 0)
			{
				if (minutes > 0 ||
					hours > 0 ||
					noMilliseconds)
				{
					if (minutes > 0)
					{
						result += $"{Math.Round(seconds):00}sec";
					}
					else
					{
						result += $"{Math.Round(seconds)}sec";
					}
				}
				else
				{
					if (minutes > 0)
					{
						result += $"{seconds:00.0}sec";
					}
					else
					{
						result += $"{seconds:F1}sec";
					}

					result = result.Replace(",", ".");
				}
			}

			return result.Trim(); // Remove trailing whitespace
		}



		// ============================================================================
		private void UpdateSolutionStatusImages(
			int index)
		{
			if (index == -1)
			{
				return;
			}

			var solution = listBoxSolutions.Items[index].ItemObject as Solution;
			var config = _settings.GetSolutionConfiguration(solution.SolutionIdentifier);

			var hasFileManaged = !string.IsNullOrWhiteSpace(config.FileNameManaged);
			var hasFileUnmanaged = !string.IsNullOrWhiteSpace(config.FileNameUnmanaged);

			var managedSolutionFileExists = false;
			var unmanagedSolutionFileExists = false;

			var managedUpToDate = true;
			var unmanagedUpToDate = true;

			if (hasFileManaged)
			{
				managedSolutionFileExists =
					File.Exists(config.FileNameManaged);
			}

			if (hasFileUnmanaged)
			{
				unmanagedSolutionFileExists =
					File.Exists(config.FileNameUnmanaged);
			}


			if (_settings.SaveVersionJson)
			{
				if (hasFileManaged)
				{
					var loadedManaged =
						ExportedSolutionVersions.TryLoadExportedSolution(
							config.FileNameManaged,
							solution,
							out var exportedSolutionManaged);

					if (loadedManaged)
					{
						managedUpToDate =
							exportedSolutionManaged.Version == solution.Version.ToString() &&
							managedSolutionFileExists;
					}
					else
					{
						managedUpToDate = managedSolutionFileExists;
					}
				}



				if (hasFileUnmanaged)
				{
					var loadedUnmanaged =
						ExportedSolutionVersions.TryLoadExportedSolution(
							config.FileNameManaged,
							solution,
							out var exportedSolutionUnmanaged);

					if (loadedUnmanaged)
					{
						unmanagedUpToDate =
							exportedSolutionUnmanaged.Version == solution.Version.ToString() &&
							unmanagedSolutionFileExists;
					}
					else
					{
						unmanagedUpToDate = unmanagedSolutionFileExists;
					}
				}
			}

			var isVersionOutdated =
				!managedUpToDate ||
				!unmanagedUpToDate;


			if (hasFileManaged)
			{
				if (hasFileUnmanaged)
				{
					(listBoxSolutions.Items[index].ItemObject as Solution).FileStatusImage =
						Properties.Resources.FileStatus_MU;
				}
				else
				{
					(listBoxSolutions.Items[index].ItemObject as Solution).FileStatusImage =
							Properties.Resources.FileStatus_MX;
				}
			}
			else
			{
				if (hasFileUnmanaged)
				{
					(listBoxSolutions.Items[index].ItemObject as Solution).FileStatusImage =
						Properties.Resources.FileStatus_XU;
				}
				else
				{
					(listBoxSolutions.Items[index].ItemObject as Solution).FileStatusImage =
						Properties.Resources.FileStatus_XX;
				}
			}


			if (isVersionOutdated)
			{
				(listBoxSolutions.Items[index].ItemObject as Solution).FileVersionState =
					Properties.Resources.Warning;
			}
			else
			{
				(listBoxSolutions.Items[index].ItemObject as Solution).FileVersionState =
					null;
			}
		}


		// ============================================================================
		private void UpdateTargetVersionStatusImages()
		{
			if (_solutionsTarget == null ||
				_solutionsOrigin == null)
			{
				return;
			}

			for (int i = 0; i < listBoxSolutions.Items.Count; i++)
			{
				var found = false;
				var originSolution = listBoxSolutions.Items[i].ItemObject as Solution;
				Com.AiricLenz.XTB.Plugin.Schema.Version originVersion = originSolution.Version;
				Com.AiricLenz.XTB.Plugin.Schema.Version targetVersion = null;

				foreach (var targetSolution in _solutionsTarget)
				{
					if (targetSolution.UniqueName != originSolution.UniqueName)
					{
						continue;
					}

					targetVersion = targetSolution.Version;
					found = true;
					break;
				}

				if (found == false)
				{
					(listBoxSolutions.Items[i].ItemObject as Solution).TargetVersionState =
						Properties.Resources.missing;
					continue;
				}

				if (targetVersion.ToString() == originVersion.ToString())
				{
					(listBoxSolutions.Items[i].ItemObject as Solution).TargetVersionState =
						Properties.Resources.match;
					continue;
				}
				else
				{
					(listBoxSolutions.Items[i].ItemObject as Solution).TargetVersionState =
						Properties.Resources.missmatch;
					continue;
				}
			}

		}

		// ============================================================================
		private void UpdateColumns()
		{
			if (_settings == null)
			{
				return;
			}

			var colFriendlyName =
				new ColumnDefinition
				{
					Header = "Solution Name",
					PropertyName = "FriendlyName",
					TooltipText = "The human readable friendly-name of the solution.",
					Width = (_settings.ShowLogicalSolutionNames ? "55%" : "100%"),
					Enabled = _settings.ShowFriendlySolutionNames,
					IsSortable = true,
					//IsSortingColumn = _settings.ShowFriendlySolutionNames,
				};

			var colLogicalName =
				new ColumnDefinition
				{
					Header = "Logical Name",
					PropertyName = "UniqueName",
					TooltipText = "The logical-name of the solution.",
					Width = (_settings.ShowFriendlySolutionNames ? "45%" : "100%"),
					Enabled = _settings.ShowLogicalSolutionNames,
					IsSortable = true,
					//IsSortingColumn = !_settings.ShowFriendlySolutionNames,
				};

			var colVersion =
				new ColumnDefinition
				{
					Header = "Version",
					PropertyName = "Version",
					TooltipText =
						"The current version number of the solution\n" +
						"in the origin environment.",
					Width = "130px",
					Enabled = true,
					IsSortable = true,
				};

			var colTargetVersionState =
				new ColumnDefinition
				{
					Header = "",
					PropertyName = "TargetVersionState",
					TooltipText =
						"Indicates if a file was not yet installed in the target environment,\n" +
						"if it is outdated (version number missmatch)\n" +
						"or if the version numbers match up between origin- and target environment.",
					Width = "50px",
					Enabled = TargetConnections?.Count == 1,
					IsSortable = false,
				};

			var colFileStatusImage =
				new ColumnDefinition
				{
					Header = "Files",
					PropertyName = "FileStatusImage",
					TooltipText =
						"Indicates if the managed (M) or\n" +
						"unmanaged (U) file has/have been defined.",
					Width = "45px",
					Enabled = true,
					IsSortable = false,
				};

			var colFileVersionState =
				new ColumnDefinition
				{
					Header = "",
					PropertyName = "FileVersionState",
					TooltipText =
						"This indicates if the version in the origin environent\n" +
						"is different from the one that was downloaded last.",
					Width = "40px",
					Enabled = true,
					IsSortable = false,
				};

			CodeUpdate = true;

			listBoxSolutions.Columns =
				new List<ColumnDefinition>
				{
					colFriendlyName,		// 0
					colLogicalName,			// 1
					colVersion,				// 2
					colTargetVersionState,	// 3
					colFileStatusImage,		// 4
					colFileVersionState     // 5
				};

			listBoxSolutions.SortingColumnIndex = _settings.SortingColumnIndex;
			listBoxSolutions.SortingColumnOrder = _settings.SortingColumnOrder;

			CodeUpdate = false;

			//listBoxSolutions.Refresh();
		}

		// ============================================================================
		private void UpdateGitOptionsVisibility()
		{
			InitializeGitHelper();

			textBox_commitMessage.Enabled = flipSwitch_gitCommit.IsOn;
			textBox_commitMessage.BackColor = (
				flipSwitch_gitCommit.IsOn ?
				SystemColors.ControlLightLight :
				SystemColors.Control);

			label_commitMessage.Enabled = flipSwitch_gitCommit.IsOn;
			label_commitMessage.ForeColor = (
				flipSwitch_gitCommit.IsOn ?
				SystemColors.ControlText :
				SystemColors.ControlDarkDark);

			comboBox_gitBranches.Enabled = flipSwitch_gitCommit.IsOn;
		}

		// ============================================================================
		private void UpdateVersionOptionsVisibility()
		{
			textBox_versionFormat.Enabled = flipSwitch_updateVersion.IsOn;
			textBox_versionFormat.BackColor = (
				flipSwitch_updateVersion.IsOn ?
				SystemColors.ControlLightLight :
				SystemColors.Control);

			label_versionFormat.Enabled = flipSwitch_updateVersion.IsOn;
			label_versionFormat.ForeColor = (
				flipSwitch_updateVersion.IsOn ?
				SystemColors.ControlText :
				SystemColors.ControlDarkDark);
		}

		// ============================================================================
		private void UpdateImportOptionsVisibility()
		{
			var optionEnabled =
				flipSwitch_importManaged.IsOn ||
				flipSwitch_importUnmanaged.IsOn;

			flipSwitch_upgrade.Enabled = optionEnabled;
			flipSwitch_enableAutomation.Enabled = optionEnabled;
			flipSwitch_overwrite.Enabled = optionEnabled;
			flipSwitch_publishTarget.Enabled = optionEnabled;
		}


		// ============================================================================
		private void ExportSolutionCsv(
			string targetFileName)
		{

			var separator = ",";
			var exportSelectedOnly = listBoxSolutions.CheckedItems.Count > 0;

			var resultCsv =
				"Unique Name, Solution Guid, Version" + Environment.NewLine;

			foreach (var listItem in listBoxSolutions.Items)
			{
				var newLine = string.Empty;
				var solution = listItem.ItemObject as Solution;

				if (exportSelectedOnly)
				{
					if (listItem.IsChecked)
					{
						newLine =
							solution.UniqueName + separator +
							solution.SolutionId + separator +
							solution.Version.ToString() + Environment.NewLine;

						resultCsv += newLine;
					}
				}
				else
				{
					newLine =
						solution.UniqueName + separator +
						solution.SolutionId + separator +
						solution.Version.ToString() + Environment.NewLine;

					resultCsv += newLine;
				}
			}

			File.WriteAllText(targetFileName, resultCsv);
		}


		// ============================================================================
		private void UpdateConnectionTimout(
			ref ConnectionDetail detail,
			int timoutInMinutes)
		{
			// update the global timout setting
			detail.Timeout = TimeSpan.FromMinutes(timoutInMinutes);

			// also update the timeout on the service client
			if (detail.ServiceClient != null)
			{
				var serviceClient = detail.ServiceClient;

				UpdateConnectionTimout(
					ref serviceClient,
					timoutInMinutes);
			}
		}


		// ============================================================================
		private void UpdateConnectionTimout(
			ref CrmServiceClient serviceClient,
			int timoutInMinutes)
		{
			if (serviceClient != null)
			{
				// For CrmServiceClient, need to access the underlying client configuration
				if (serviceClient.OrganizationWebProxyClient != null)
				{
					// Set timeout on the WebProxy client
					serviceClient.OrganizationWebProxyClient.InnerChannel.OperationTimeout = TimeSpan.FromMinutes(timoutInMinutes);
					serviceClient.OrganizationWebProxyClient.Endpoint.Binding.SendTimeout = TimeSpan.FromMinutes(timoutInMinutes);
					serviceClient.OrganizationWebProxyClient.Endpoint.Binding.ReceiveTimeout = TimeSpan.FromMinutes(timoutInMinutes);
				}

				// If OrganizationServiceProxy is available (for older auth types)
				if (serviceClient.OrganizationServiceProxy != null)
				{
					serviceClient.OrganizationServiceProxy.Timeout = TimeSpan.FromMinutes(timoutInMinutes);
				}
			}
		}


		// ============================================================================
		private string GetDefaultFileName(
			Solution solution,
			bool isManaged = true)
		{
			var solutionName = solution.UniqueName;
			solutionName = solutionName.Replace("#", "");

			while (solutionName.Contains("  "))
			{
				solutionName = solutionName.Replace("  ", " ");
			}

			solutionName = solutionName.Trim();

			if (isManaged)
			{
				return
					"Managed." + solutionName + ".zip";
			}
			else
			{
				return
					"Unmanaged." + solutionName + ".zip";
			}
		}




		#endregion





	}

	// ============================================================================
	// ============================================================================
	// ============================================================================
	class LoadSolutionsResult
	{
		public EntityCollection OriginsSolutions = null;
		public EntityCollection TargetSolutions = null;
	}


}
