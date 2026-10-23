using System;
using System.Activities.Presentation.Debug;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.ServiceModel;
using System.Windows.Forms;
using Com.AiricLenz.BulkSolutionExporter.Core;
using Com.AiricLenz.BulkSolutionExporter.Core.Extensions;
using Com.AiricLenz.BulkSolutionExporter.Core.IO;
using Com.AiricLenz.BulkSolutionExporter.Core.Run;
using Com.AiricLenz.BulkSolutionExporter.Core.Schema;
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
using Version = Com.AiricLenz.BulkSolutionExporter.Core.Version;


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
		private RunLog _runLog = null;

		private WorkAsyncInfo _executionWorker = null;
		private volatile BackgroundWorker _executionBackgroundWorker = null;
		private volatile bool _isAbortRequested = false;
		private bool _isExecuting = false;
		private Image _executeButtonImage;
		private string _executeButtonToolTip;
		private readonly Image _abortButtonImage = Properties.Resources.delete_32px;
		private Timer _progressTimer;
		private Timer _saveDebounceTimer;
		private DateTime _progressStartTime;
		private string _progressBaseMessage;
		private Size _workerPanelSize = new Size(400, 200);


		private Solution _currentSolution;
		private const string ColorError = RunLog.ColorError;
		private const string ColorSolution = RunLog.ColorSolution;
		private const string ColorFile = RunLog.ColorFile;
		private const string ColorGreen = RunLog.ColorGreen;
		private const string ColorOrange = RunLog.ColorOrange;
		private const string ColorConnection = RunLog.ColorConnection;
		private const string ColorTeeth = RunLog.ColorTeeth;

		private const string ColorEndTag = RunLog.ColorEndTag;

		private const string dateTimeFormat = "yyyy-MM-dd HH:mm:ss";

		// Looks of the toolbar's Execute button; see SetExecuteButtonMode.
		private enum ExecuteButtonMode
		{
			Execute,
			Abort,
			Aborting
		}


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

			// Kept so the Execute look is restored without reloading it from the resx.
			_executeButtonImage = button_Export.Image;
			_executeButtonToolTip = button_Export.ToolTipText;

			richTextBox_log.Text = string.Empty;

			_saveDebounceTimer = new Timer();
			_saveDebounceTimer.Interval = 500;
			_saveDebounceTimer.Tick += (s, e) => { _saveDebounceTimer.Stop(); PersistSettings(); };

			_logger = new Logger(richTextBox_log);

#if DEBUG
			bool isDebugEnabled = isDebugMode;
#else
			bool isDebugEnabled = false;
#endif
			_runLog = new RunLog(AppendLogLine, isDebugEnabled);

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

				var allowGit =
					_settings.ExportManaged ||
					_settings.ExportUnmanaged;

				flipSwitch_gitCommit.IsOn = _settings.GitCommit && allowGit;
				flipSwitch_gitCommit.Enabled = allowGit;

				flipSwitch_pushCommit.IsOn = _settings.PushCommit;
				flipSwitch_pushCommit.Enabled = _settings.GitCommit && allowGit;
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
			SaveSettings(immediate: true);
			_saveDebounceTimer?.Stop();
			_saveDebounceTimer?.Dispose();
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
			SaveSettings(cleanUpNonExistingSolutions: true, immediate: true);
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
			flipSwitch_pushCommit.Enabled = gitEnabeld && flipSwitch_gitCommit.IsOn;

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

			_settings.ExportUnmanaged = flipSwitch_exportUnmanaged.IsOn;

			CodeUpdate = true;

			var gitEnabeld =
				flipSwitch_exportManaged.IsOn ||
				flipSwitch_exportUnmanaged.IsOn;

			flipSwitch_gitCommit.Enabled = gitEnabeld;
			flipSwitch_pushCommit.Enabled = gitEnabeld && flipSwitch_gitCommit.IsOn;

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
			if (_isExecuting)
			{
				var answer =
					MessageBox.Show(
						"Abort after the current step finishes?",
						"Abort Execution",
						MessageBoxButtons.YesNo,
						MessageBoxIcon.Question);

				// The run may have ended while the dialog was open.
				if (answer != DialogResult.Yes ||
					!_isExecuting)
				{
					return;
				}

				// Cancel the execution worker directly so the working panel stays open
				// until the run ends; CancelWorker() would remove it. CancelWorker() is
				// only the fallback when the worker has not started yet.
				if (_executionBackgroundWorker != null)
				{
					_executionBackgroundWorker.CancelAsync();
				}
				else
				{
					CancelWorker();
				}

				SetExecuteButtonMode(ExecuteButtonMode.Aborting);

				// The progress timer keeps showing this message until the run ends.
				_progressBaseMessage =
					$"Aborting...{Environment.NewLine}Waiting for the current step to finish.";

				SetWorkingMessage(
					_progressBaseMessage,
					_workerPanelSize.Width,
					_workerPanelSize.Height);

				return;
			}

			listBoxSolutions.DeselectAll();
			UpdateSolutionSettingsScreen();

			if (!CheckIfAllSolutionFilesAreDefined())
			{
				return;
			}

			// Flush the current check state before the run starts
			SaveSettings(immediate: true);

			SetUiEnabledState(false);

			_isExecuting = true;
			SetExecuteButtonMode(ExecuteButtonMode.Abort);

			ExecuteOperations();
		}


		// ============================================================================
		/// <summary>
		/// Sets the text, image, tooltip and enabled state of the Execute button.
		/// The Execute look takes its enabled state from SetExportButtonState(),
		/// so _isExecuting must be false before switching back to it.
		/// </summary>
		private void SetExecuteButtonMode(
			ExecuteButtonMode mode)
		{
			switch (mode)
			{
				case ExecuteButtonMode.Abort:
					button_Export.Text = " Abort ";
					button_Export.Image = _abortButtonImage;
					button_Export.ToolTipText = "Abort the execution";
					button_Export.Enabled = true;
					break;

				case ExecuteButtonMode.Aborting:
					button_Export.Text = " Aborting... ";
					button_Export.Image = _abortButtonImage;
					button_Export.ToolTipText = "Abort the execution";
					button_Export.Enabled = false;
					break;

				default:
					button_Export.Text = " Execute ";
					button_Export.Image = _executeButtonImage;
					button_Export.ToolTipText = _executeButtonToolTip;
					SetExportButtonState();
					break;
			}
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

			// The wizard rewrites solution configurations; a run reads them from
			// its snapshot, so they must not change while it is active.
			button_fileWizzard.Enabled =
				state &&
				listBoxSolutions.CheckedItems.Count > 0;

			textBox_versionFormat.Enabled = state;
			textBox_commitMessage.Enabled = state;
			textBox_managed.Enabled = state;
			textBox_unmanaged.Enabled = state;

			comboBox_gitBranches.Enabled = state;

			button_loadSolutions.Enabled = state;

			// While a run is active the button is the Abort button, owned by
			// SetExecuteButtonMode.
			if (!_isExecuting)
			{
				button_Export.Enabled = state;
			}

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

			var selectedFolder = folderBrowserDialog1.SelectedPath;
			var folderPrefix =
				Path.GetFullPath(selectedFolder).TrimEnd('\\', '/') +
				Path.DirectorySeparatorChar;

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

				var fileNameManaged =
					Path.Combine(
						selectedFolder,
						GetDefaultFileName(solution, true));

				var fileNameUnmanaged =
					Path.Combine(
						selectedFolder,
						GetDefaultFileName(solution, false));

				// Never store a path that resolves outside the selected folder
				if (!Path.GetFullPath(fileNameManaged).StartsWith(folderPrefix, StringComparison.OrdinalIgnoreCase) ||
					!Path.GetFullPath(fileNameUnmanaged).StartsWith(folderPrefix, StringComparison.OrdinalIgnoreCase))
				{
					LogError(
						"The default file name for the solution '" + solution.UniqueName +
						"' resolves outside the selected folder. The file locations were not changed.");
					continue;
				}

				solutionConfig.FileNameManaged = fileNameManaged;
				solutionConfig.FileNameUnmanaged = fileNameUnmanaged;

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

		#endregion

		// ##################################################
		// ##################################################

		#region Custom Logic


		// ============================================================================
		private void ExecuteOperations()
		{
			richTextBox_log.Text = string.Empty;
			_runLog.ResetIndent();
			_sessionFiles.Clear();
			_isAbortRequested = false;

			// Taken on the UI thread: the list is rebuilt during the run, so the
			// worker phases must not read listBoxSolutions.CheckedItems live.
			var checkedSolutions =
				listBoxSolutions.CheckedItems
					.Select(item => item.ItemObject as Solution)
					.ToList();

			// Taken on the UI thread too: Settings is not thread-safe, so the
			// worker phases read this snapshot and never touch _settings.
			var settingsSnapshot =
				new ExecutionSettingsSnapshot(
					_settings,
					checkedSolutions);

			var message = string.Join(" / ", GetActionsList()) + " Solutions...";

			_executionWorker = new WorkAsyncInfo()
			{
				Message = message,
				MessageWidth = _workerPanelSize.Width,
				MessageHeight = _workerPanelSize.Height,
				IsCancelable = true,
				Work = (worker, args) =>
				{
					// Kept for the abort path of button_Export_Click.
					_executionBackgroundWorker = worker;

					try
					{
						if (flipSwitch_publishSource.IsOn &&
							!IsAbortRequested(worker))
						{
							ReportExtendedProgress(
								worker,
								$"Publishing all...{Environment.NewLine}[{ConnectionDetail.ConnectionName}]",
								resetTimer: true);

							PublishAll(ConnectionDetail);
						}

						if (!IsAbortRequested(worker))
						{
							UpdateCheckedVersionNumbers(
								checkedSolutions,
								worker);
						}

						if (!IsAbortRequested(worker))
						{
							ExportCheckedSolutions(
								checkedSolutions,
								settingsSnapshot,
								worker);
						}

						if (!IsAbortRequested(worker))
						{
							HandleGit(worker);
						}

						foreach (var targetConnection in TargetConnections)
						{
							if (IsAbortRequested(worker))
							{
								break;
							}

							Log(ColorTeeth + ":::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::" + ColorEndTag);
							Log($"##### Handling Target *" + ColorConnection + targetConnection.ConnectionName + ColorEndTag + "*:");
							Log(ColorTeeth + ":::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::" + ColorEndTag);

							Log();

							ImportCheckedSolutions(
								targetConnection,
								checkedSolutions,
								settingsSnapshot,
								worker);

							if (flipSwitch_publishTarget.IsOn &&
								!IsAbortRequested(worker))
							{
								ReportExtendedProgress(
									worker,
									$"Publishing all...{Environment.NewLine}[{targetConnection.ConnectionName}]",
									resetTimer: true);

								PublishAll(targetConnection);
							}
						}

						if (IsAbortRequested(worker))
						{
							// Marks the run as cancelled; args.Result must then not be read.
							args.Cancel = true;
							return;
						}

						args.Result = null;
					}
					catch
					{
						// Records an abort requested while the failing step was running, so
						// PostWorkCallBack still reports the run as aborted.
						IsAbortRequested(worker);
						throw;
					}
				},

				PostWorkCallBack = (args) =>
				{
					StopProgressTimer();

					_isExecuting = false;
					_executionBackgroundWorker = null;
					SetExecuteButtonMode(ExecuteButtonMode.Execute);

					// The flag also covers a step that threw after the abort was
					// requested, where args.Cancelled is false and args.Error is set.
					var isAborted = RetryRunner.IsRunAborted(
						args.Cancelled,
						_isAbortRequested);

					if (args.Error != null)
					{
						MessageBox.Show(
							args.Error.ToString(),
							"Error",
							MessageBoxButtons.OK,
							MessageBoxIcon.Error);
					}

					LoadAllSolutions();

					if (isAborted)
					{
						Log(ColorOrange + "##### Aborted by user." + ColorEndTag);
					}
					else
					{
						Log(ColorGreen + "##### Done." + ColorEndTag);
					}

					SetUiEnabledState(true);

					if (!isAborted &&
						args.Error == null &&
						_settings.AutoDisableExportButtons)
					{
						flipSwitch_updateVersion.IsOn = false;
						flipSwitch_exportManaged.IsOn = false;
						flipSwitch_exportUnmanaged.IsOn = false;
					}
				},

				ProgressChanged = (args) =>
				{
					var extendedArgs = args.UserState as ExtendedProgressChangedEventArgs;

					// While aborting, the step still running must not replace the
					// "Aborting..." message on the working panel.
					var isAborting =
						_executionBackgroundWorker?.CancellationPending == true;

					if (extendedArgs != null &&
						!isAborting)
					{
						// Use the extended information
						var workerMessage = extendedArgs.Message;
						var additionalData = extendedArgs.AdditionalMessage;
						var resetTimer = extendedArgs.ResetTimer;

						_progressBaseMessage =
							workerMessage + (
								additionalData.HasValue() ? Environment.NewLine + additionalData : string.Empty
							);

						SetWorkingMessage(
							_progressBaseMessage,
							_workerPanelSize.Width,
							_workerPanelSize.Height);

						if (resetTimer)
						{
							StartProgressTimer();
						}
					}
				},
			};

			WorkAsync(_executionWorker);
		}


		// ============================================================================
		/// <summary>
		/// Applies a change to the solution configuration in _settings and saves
		/// the settings. Callable from the execution worker: the change runs on
		/// the UI thread (BeginInvoke, so the worker does not block on it), the
		/// only thread that touches _settings.
		/// </summary>
		/// <param name="solutionIdentifier">The solution whose configuration changes.</param>
		/// <param name="applyChange">Applies the change to the configuration in _settings.</param>
		private void WriteBackSolutionConfiguration(
			string solutionIdentifier,
			Action<SolutionConfiguration> applyChange)
		{
			BeginInvoke((MethodInvoker) (() =>
			{
				var config =
					_settings.GetSolutionConfiguration(
						solutionIdentifier,
						true);

				if (config == null)
				{
					return;
				}

				applyChange(config);

				_settings.UpdateSolutionConfiguration(config);
				SaveSettings();
			}));
		}


		// ============================================================================
		/// <summary>
		/// Returns whether the user requested an abort of the execution worker.
		/// The first time the request is observed, the abort is remembered for
		/// the post-work callback and logged once.
		/// </summary>
		/// <param name="worker">The execution worker; null never reports an abort.</param>
		private bool IsAbortRequested(
			BackgroundWorker worker)
		{
			if (worker?.CancellationPending != true)
			{
				return false;
			}

			if (!_isAbortRequested)
			{
				_isAbortRequested = true;
				Log(ColorOrange + "Abort requested — skipping remaining steps." + ColorEndTag);
			}

			return true;
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
			base.SetWorkingMessage(message, width, height);
		}



		// ============================================================================
		// Call this method to start the timer and set the base message
		private void StartProgressTimer()
		{
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
				_currentSolutionConfig =
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
						_currentSolution.UniqueName,
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
						_currentSolutionConfig.FileNameUnmanaged,
						_currentSolution.UniqueName,
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
			_runLog.IncreaseIndent();

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
				_runLog.DecreaseIndent();
				return;
			}

			var duration = GetDurationString(startTime);

			Log("Publish All was completed.");
			Log($"Duration: {duration}");

			_runLog.DecreaseIndent();
			Log();
		}


		// ============================================================================
		private void UpdateCheckedVersionNumbers(
			List<Solution> solutions,
			BackgroundWorker worker)
		{
			if (!flipSwitch_updateVersion.IsOn ||
				solutions.Count == 0)
			{
				return;
			}

			ReportExtendedProgress(
				worker,
				"Updating Version Numbers...",
				resetTimer: true);

			Log("##### Updating Version Numbers:");
			_runLog.IncreaseIndent();


			for (int i = 0; i < solutions.Count; i++)
			{
				if (IsAbortRequested(worker))
				{
					break;
				}

				var solution = solutions[i];

				Log("Updating version number for solution " + ColorSolution + "'" + solution.FriendlyName + "'" + ColorEndTag);

				UpdateVersionNumberInSource(solution);
				RefreshSolutionInListBox(solution);

				if (i < solutions.Count - 1)
				{
					Log();
				}
			}

			_runLog.DecreaseIndent();
			Log();
		}


		// ============================================================================
		private void ExportCheckedSolutions(
			List<Solution> solutions,
			ExecutionSettingsSnapshot settingsSnapshot,
			BackgroundWorker worker)
		{
			if (flipSwitch_exportManaged.IsOff &&
				flipSwitch_exportUnmanaged.IsOff)
			{
				return;
			}

			Log("##### Exporting:");
			_runLog.IncreaseIndent();

			for (int i = 0; i < solutions.Count; i++)
			{
				if (IsAbortRequested(worker))
				{
					break;
				}

				var solution = solutions[i];

				var solutionConfig =
					settingsSnapshot.GetSolutionConfiguration(
						solution.SolutionIdentifier);

				Log("**Exporting solution " + ColorSolution + "'" + solution.FriendlyName + "'" + ColorEndTag + ":**");

				var exportResult =
					ExportSolution(
						solution,
						solutionConfig,
						settingsSnapshot,
						worker);

				if (!exportResult)
				{
					Log();
					LogError("**Export aborted due to an error!**");
					break;
				}

				if (i < solutions.Count - 1)
				{
					Log();
				}

				RefreshSolutionInListBox(solution);
			}

			_runLog.DecreaseIndent();
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
					ExportedSolutionVersions.FileName);

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
					if (exportedSolutions.Managed[i].Matches(solution.UniqueName, fileName))
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
					if (exportedSolutions.Unmanaged[i].Matches(solution.UniqueName, fileName))
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
			List<Solution> solutions,
			ExecutionSettingsSnapshot settingsSnapshot,
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
			_runLog.IncreaseIndent();

			for (int i = 0; i < solutions.Count; i++)
			{
				if (IsAbortRequested(worker))
				{
					break;
				}

				var solution = solutions[i];

				var solutionConfig =
					settingsSnapshot.GetSolutionConfiguration(
						solution.SolutionIdentifier);

				var startTime = DateTime.Now;

				Log("**Importing solution " + ColorSolution + "'" + solution.FriendlyName + "'" + ColorEndTag + ":**");


				var importResult =
					ImportSolution(
						targetService,
						solution,
						settingsSnapshot,
						worker,
						importManagd);

				if (importResult)
				{
					_runLog.IncreaseIndent();
					var duration = GetDurationString(startTime);
					var durationSeconds = (int) (DateTime.Now - startTime).TotalSeconds;

					Log("The import was successful.");
					Log("Duration: " + duration);

					if (i < solutions.Count - 1)
					{
						Log();
					}

					_runLog.DecreaseIndent();

					if (importManagd)
					{
						solutionConfig.LastImportDurationManagedInSeconds = durationSeconds;

						WriteBackSolutionConfiguration(
							solution.SolutionIdentifier,
							config => config.LastImportDurationManagedInSeconds = durationSeconds);
					}
					else
					{
						solutionConfig.LastImportDurationUnmanagedInSeconds = durationSeconds;

						WriteBackSolutionConfiguration(
							solution.SolutionIdentifier,
							config => config.LastImportDurationUnmanagedInSeconds = durationSeconds);
					}
				}

				// An import ended by the abort is not reported as an import error.
				if (!importResult &&
					IsAbortRequested(worker))
				{
					break;
				}

				if (!importResult &&
					!settingsSnapshot.ContinueOnError)
				{
					LogError("The import was not successful and Continue-On-Error is disabled.");
					break;
				}

				RefreshSolutionInListBox(solution);
			}

			_runLog.DecreaseIndent();
			Log();
		}



		// ============================================================================
		private bool ExportSolution(
			Solution solution,
			SolutionConfiguration solutionConfiguration,
			ExecutionSettingsSnapshot settingsSnapshot,
			BackgroundWorker worker)
		{
			_runLog.IncreaseIndent();

			if (flipSwitch_exportManaged.IsOn)
			{
				var lastDurationString =
					solutionConfiguration.LastExportDurationManagedInSeconds != 0 ?
					$" (Last duration: {GetDurationString(solutionConfiguration.LastExportDurationManagedInSeconds, noMilliseconds: true)})" :
					null;

				ReportExtendedProgress(
					worker,
					$"Exporting as managed...{Environment.NewLine}'{solution.FriendlyName}'",
					additionalMessage: lastDurationString,
					resetTimer: true);

				var durationInSeconds =
					ExportToFile(
						solution,
						solutionConfiguration,
						settingsSnapshot,
						true);

				if (durationInSeconds > 0)
				{
					solutionConfiguration.LastExportDurationManagedInSeconds = durationInSeconds;

					WriteBackSolutionConfiguration(
						solutionConfiguration.SolutionIndentifier,
						config => config.LastExportDurationManagedInSeconds = durationInSeconds);
				}
			}

			// Abort between the managed and the unmanaged export: the managed
			// export completed, so this is not an export error.
			if (IsAbortRequested(worker))
			{
				_runLog.DecreaseIndent();
				return true;
			}

			if (flipSwitch_exportManaged.IsOn &&
				flipSwitch_exportUnmanaged.IsOn)
			{
				Log();
			}

			if (flipSwitch_exportUnmanaged.IsOn)
			{
				var lastDurationString =
					solutionConfiguration.LastExportDurationUnamangedInSeconds != 0 ?
					$" (Last duration: {GetDurationString(solutionConfiguration.LastExportDurationUnamangedInSeconds, noMilliseconds: true)})" :
					null;

				ReportExtendedProgress(
					worker,
					$"Exporting as unmanaged...{Environment.NewLine}'{solution.FriendlyName}'",
					additionalMessage: lastDurationString,
					resetTimer: true);

				var durationInSeconds =
					ExportToFile(
						solution,
						solutionConfiguration,
						settingsSnapshot,
						false);

				if (durationInSeconds > 0)
				{
					solutionConfiguration.LastExportDurationUnamangedInSeconds = durationInSeconds;

					WriteBackSolutionConfiguration(
						solutionConfiguration.SolutionIndentifier,
						config => config.LastExportDurationUnamangedInSeconds = durationInSeconds);
				}
			}

			_runLog.DecreaseIndent();

			return true;
		}


		// ============================================================================
		private int ExportToFile(
			Solution solution,
			SolutionConfiguration solutionConfiguration,
			ExecutionSettingsSnapshot settingsSnapshot,
			bool isManaged)
		{
			Log("Exporting as " + (isManaged ? "" : "un") + "managed solution now...");
			_runLog.IncreaseIndent();


			if (isManaged && string.IsNullOrWhiteSpace(solutionConfiguration.FileNameManaged) ||
				!isManaged && string.IsNullOrWhiteSpace(solutionConfiguration.FileNameUnmanaged))
			{
				LogError("No file name for the " + (isManaged ? "" : "un") + "managed solution was given!");
				_runLog.DecreaseIndent();
				return -1;
			}

			var filePath =
				isManaged ? solutionConfiguration.FileNameManaged : solutionConfiguration.FileNameUnmanaged;

			if (!FileAndFolderHelper.IsValidSolutionFilePath(filePath))
			{
				LogError("The file path '" + filePath + "' is not a valid absolute file path. The file is skipped.");
				_runLog.DecreaseIndent();
				return -1;
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

			var duration = GetDurationString(startTime);
			var durationSeconds = (int) (DateTime.Now - startTime).TotalSeconds;

			Log("Duration: " + duration);

			if (settingsSnapshot.SaveVersionJson)
			{
				SaveVersionJson(
					filePath,
					solution,
					isManaged);
			}

			_runLog.DecreaseIndent();
			return durationSeconds;

		}


		// ============================================================================
		private bool UpdateVersionNumberInSource(
			Solution solution)
		{
			_runLog.IncreaseIndent();

			var oldVersionString = solution.Version.ToString();

			var versionUpdateResult =
				solution.Version.IncreaseVersionNumber(
					textBox_versionFormat.Text);

			if (!versionUpdateResult)
			{
				LogError("The version format is invalid!");
				_runLog.DecreaseIndent();
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

			_runLog.DecreaseIndent();
			return true;
		}


		// ============================================================================
		private void RefreshSolutionInListBox(
			Solution solution)
		{
			// The list box is UI-thread-only. Invoke, not BeginInvoke: the
			// refresh reads the export folder's version JSON, which the worker's
			// next SaveVersionJson writes, so the two must not overlap.
			if (InvokeRequired)
			{
				Invoke(new MethodInvoker(() => RefreshSolutionInListBox(solution)));
				return;
			}

			// The list items wrap the same Solution objects as _solutionsOrigin,
			// so rebuilding the list from it picks up the changed solution.
			var isListed =
				_solutionsOrigin.Any(
					s => s.SolutionIdentifier == solution.SolutionIdentifier);

			if (isListed)
			{
				UpdateSolutionList();
			}
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

			ReportExtendedProgress(
				worker,
				"Committing files to Git...",
				resetTimer: true);

			Log("##### Commiting the new files to Git:");
			_runLog.IncreaseIndent();

			if (_sessionFiles.Count == 0)
			{
				Log("No files need to be committed.");
				_runLog.DecreaseIndent();
				Log();
				return;
			}


			string errorMessage;

			foreach (var file in _sessionFiles)
			{
				if (!_gitHelper.AddFile(file, out errorMessage))
				{
					LogError("Error staging: *" + errorMessage + "*");
				}
			}

			var message = textBox_commitMessage.Text;


			if (!_gitHelper.Commit(message, out errorMessage))
			{
				LogError("Error commiting: *" + errorMessage + "*");
			}
			else
			{
				Log(_sessionFiles.Count + " File(s) was/weres commited: " + message);
			}


			if (flipSwitch_pushCommit.IsOff)
			{
				_runLog.DecreaseIndent();
				Log();
				return;
			}

			if (!_gitHelper.Push(out errorMessage))
			{
				LogError("Error pushing: *" + errorMessage + "*");
			}
			else
			{
				Log("The commit has been pushed to the remote origin.");
			}

			_runLog.DecreaseIndent();
			Log();
		}


		// ============================================================================
		private bool ImportSolution(
			ConnectionDetail targetService,
			Solution solution,
			ExecutionSettingsSnapshot settingsSnapshot,
			BackgroundWorker worker,
			bool isManaged = true)
		{
			_runLog.IncreaseIndent();

			var targetServiceClient = targetService?.ServiceClient;
			int maxRetries = settingsSnapshot.RetryCount > 0 ? settingsSnapshot.RetryCount : 3;
			int retryDelay = settingsSnapshot.RetryDelayInSeconds > 0 ? settingsSnapshot.RetryDelayInSeconds : 5;
			bool continueOnError = settingsSnapshot.ContinueOnError;

			var config =
				settingsSnapshot.GetSolutionConfiguration(
					solution.SolutionIdentifier);

			if (config == null)
			{
				_runLog.DecreaseIndent();
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

			if (!FileAndFolderHelper.IsValidSolutionFilePath(solutionPath))
			{
				LogError("The file path '" + solutionPath + "' is not a valid absolute file path. The file is skipped.");
				_runLog.DecreaseIndent();
				return false;
			}

			if (!File.Exists(solutionPath))
			{
				LogError("The file '" + solutionPath + "' does not exist.");
				_runLog.DecreaseIndent();
				return false;
			}

			var solutionBytes = File.ReadAllBytes(solutionPath);

			bool IsInstalledAlready =
				DoesSolutionExistInTarget(
					targetService,
					solution.UniqueName);

			var isHolding =
				isManaged &&
				IsInstalledAlready &&
				flipSwitch_upgrade.IsOn;


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

				var lastImportDuration =
					isManaged ?
					config.LastImportDurationManagedInSeconds :
					config.LastImportDurationUnmanagedInSeconds;

				var lastImportDurationString =
					lastImportDuration > 0 ?
					$" (Last Import: {GetDurationString(lastImportDuration, noMilliseconds: true)})" :
					null;

				if (isHolding)
				{
					Log("Installing the solution upgrade...");
					Log("Start Time: " + DateTime.Now.ToString(dateTimeFormat) + lastImportDurationString);

					ReportExtendedProgress(
						worker,
						$"Installing upgrade: '{solution.FriendlyName}'{Environment.NewLine}[{targetService.ConnectionName}]...",
						lastImportDurationString,
						resetTimer: true);
				}
				else
				{
					Log("Installing the solution update...");
					Log("Start Time: " + DateTime.Now.ToString(dateTimeFormat));

					ReportExtendedProgress(
						worker,
						$"Installing update: '{solution.FriendlyName}'{Environment.NewLine}[{targetService.ConnectionName}]...",
						lastImportDurationString,
						resetTimer: true);
				}

				var isInstallCompleted =
					ExecuteWithRetries(
						() => targetServiceClient.Execute(importRequest),
						worker,
						$"Retry {{0}}/{{1}}: Installing solution: '{solution.FriendlyName}'{Environment.NewLine}[{targetService.ConnectionName}]...",
						maxRetries,
						retryDelay,
						$"installing solution '{solution.FriendlyName}'",
						continueOnError);

				if (!isInstallCompleted)
				{
					_runLog.DecreaseIndent();
					return false;
				}


				if (isHolding)
				{
					Log();
					Log("Applying the solution upgrade...");
					Log("Start Time: " + DateTime.Now.ToString(dateTimeFormat));

					ReportExtendedProgress(
						worker,
						$"Applying upgrade: '{solution.FriendlyName}'{Environment.NewLine}[{targetService.ConnectionName}]...",
						lastImportDurationString,
						resetTimer: false);

					var applyUpgradeRequest = new DeleteAndPromoteRequest
					{
						UniqueName = solution.UniqueName,
					};

					var isApplyCompleted =
						ExecuteWithRetries(
							() => targetServiceClient.Execute(applyUpgradeRequest),
							worker,
							$"Retry {{0}}/{{1}}: Applying upgrade: '{solution.FriendlyName}'{Environment.NewLine}[{targetService.ConnectionName}]...",
							maxRetries,
							retryDelay,
							$"applying upgrade '{solution.FriendlyName}'",
							continueOnError);

					if (!isApplyCompleted)
					{
						_runLog.DecreaseIndent();
						return false;
					}
				}
			}

			catch (FaultException ex)
			{
				var errorMessage =
					Formatter.FormatErrorStringWithXml(
						ex.Message,
						RunLog.IndentUnit);

				LogError(errorMessage);
				Log();
				_runLog.DecreaseIndent();
				return false;
			}
			catch (Exception ex)
			{
				var errorMessage =
					Formatter.FormatErrorStringWithXml(
						ex.Message,
						RunLog.IndentUnit);

				LogError(errorMessage);
				Log();
				_runLog.DecreaseIndent();
				return false;
			}

			_runLog.DecreaseIndent();
			return true;
		}


		// ============================================================================
		/// <summary>
		/// Runs the action, retrying it after a delay when it fails.
		/// The first attempt always runs; an abort request ends the retry
		/// delay early and starts no further retry.
		/// </summary>
		/// <returns>True only when the action completed; false on abort or failure.</returns>
		private bool ExecuteWithRetries(
		Action action,
		BackgroundWorker worker,
		string retryMessageFormat,
		int maxRetries,
		int retryDelaySeconds,
		string operationDescription = null,
		bool continueOnError = false)
		{
			var retryRunner = new RetryRunner(
				() => IsAbortRequested(worker),
				message => Log(message),
				message => LogError(message),
				(currentRetry, maxRetryCount) => ReportExtendedProgress(
					worker,
					string.Format(retryMessageFormat, currentRetry, maxRetryCount),
					null,
					resetTimer: false),
				milliseconds => System.Threading.Thread.Sleep(milliseconds),
				() => DateTime.UtcNow);

			return retryRunner.Execute(
				action,
				maxRetries,
				retryDelaySeconds,
				operationDescription,
				continueOnError);
		}



		// ============================================================================
		/// <summary>
		/// Checks if a solution already exists in the target environment
		/// </summary>
		/// <param name="targetService">The connection to the target environment</param>
		/// <param name="solutionUniqueName">The unique name of the solution to check</param>
		/// <returns>True if the solution exists, false otherwise</returns>
		private bool DoesSolutionExistInTarget(
			ConnectionDetail targetService,
			string solutionUniqueName)
		{
			if (targetService == null || string.IsNullOrEmpty(solutionUniqueName))
			{
				return false;
			}

			try
			{
				var targetServiceClient = targetService.ServiceClient;

				// Query to check if the solution exists
				var query = new QueryExpression("solution")
				{
					ColumnSet = new ColumnSet("uniquename", "version"),
					Criteria = new FilterExpression(LogicalOperator.And)
				};

				query.Criteria.AddCondition("uniquename", Microsoft.Xrm.Sdk.Query.ConditionOperator.Equal, solutionUniqueName);

				var result = targetServiceClient.RetrieveMultiple(query);

				// If we found at least one record, the solution exists
				return result.Entities.Count > 0;
			}
			catch (Exception ex)
			{
				LogError($"Error checking if solution exists in target: {ex.Message}");
				return false;
			}
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
			var loadSolutionsWorker = new WorkAsyncInfo
			{
				Message = "Loading all solutions...",
				MessageWidth = _workerPanelSize.Width,
				MessageHeight = _workerPanelSize.Height,
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
						try
						{
							result.OriginsSolutions =
								Service.RetrieveMultiple(
									queryOrigin);
						}
						catch (Exception ex)
						{
							LogError(ex.Message);
							result = null;
						}
					}

					if (TargetConnections?.Count == 1)
					{
						try
						{
							result.TargetSolutions =
								TargetConnections[0].GetCrmServiceClient().RetrieveMultiple(
									queryTarget);
						}
						catch (Exception ex)
						{
							LogError(ex.Message);
							result = null;
						}
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

					UpdateColumns();
					UpdateSolutionList();

					// After the rebuild, so the sync does not walk the old list
					// and re-create the configs the cleanup just removed
					SaveSettings(cleanUpNonExistingSolutions: true);

					UpdateImportOptionsVisibility();
					SetExportButtonState();
				}
			};


			WorkAsync(loadSolutionsWorker);

		}


		// ============================================================================
		private void SetExportButtonState()
		{
			var versionIsInvalid =
				!Version.ValidateFormat(
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

			// While a run is active the button is the Abort button.
			if (!_isExecuting)
			{
				button_Export.Enabled = exportEnabled;
			}


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
			for (int i = 0; i < listBoxSolutions.Items.Count; i++)
			{
				var solution = listBoxSolutions.Items[i].ItemObject as Solution;
				var config = _settings.GetSolutionConfiguration(solution.SolutionIdentifier);

				if (config != null)
				{
					listBoxSolutions.SetItemChecked(i, config.Checked);
					UpdateSolutionStatusImages(i, config);
				}
			}

			SetExportButtonState();
			UpdateTargetVersionStatusImages();
			listBoxSolutions.Refresh();
		}


		// ============================================================================
		private void SaveSettings(
			[CallerMemberName] string caller = "",
			bool cleanUpNonExistingSolutions = false,
			bool immediate = false)
		{
			// No worker calls this: the execution worker writes back through
			// WriteBackSolutionConfiguration on the UI thread. Kept as a guard,
			// since the debounce timer and the list must only be touched on the
			// UI thread (BeginInvoke, so a caller does not block on the UI thread)
			if (InvokeRequired)
			{
				BeginInvoke((MethodInvoker) (() => SaveSettings(caller, cleanUpNonExistingSolutions, immediate)));
				return;
			}

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

			// The in-memory settings are synced right away so that any list
			// rebuild from _settings restores the current check state;
			// only the disk write is debounced
			SyncSolutionConfigsFromList();

			if (immediate)
			{
				_saveDebounceTimer.Stop();
				PersistSettings(caller);
				return;
			}

			// Restart the debounce timer; rapid calls coalesce into one save
			_saveDebounceTimer.Stop();
			_saveDebounceTimer.Start();
		}


		// ============================================================================
		private void SyncSolutionConfigsFromList()
		{
			foreach (var listBoxItem in listBoxSolutions.Items)
			{
				var solution = listBoxItem.ItemObject as Solution;
				var config = _settings.GetSolutionConfiguration(solution.SolutionIdentifier, true);

				// Skip unchanged configs: re-serializing every config on each
				// keystroke-triggered save is costly with hundreds of solutions
				if (config.Checked == listBoxItem.IsChecked &&
					config.SortingIndex == listBoxItem.SortingIndex)
				{
					continue;
				}

				config.Checked = listBoxItem.IsChecked;
				config.SortingIndex = listBoxItem.SortingIndex;

				_settings.UpdateSolutionConfiguration(config);
			}
		}


		// ============================================================================
		private void PersistSettings(
			[CallerMemberName] string caller = "")
		{
			SettingsManager.Instance.Save(GetType(), _settings);

			LogDebug($"Settings have been saved ({caller}): ({_settings.SplitContainerPosition})");
		}


		// ============================================================================
		private void RemoveNonExistantSolutions()
		{
			var originIdentifiers = new HashSet<string>(
				_solutionsOrigin.Select(s => s.SolutionIdentifier),
				StringComparer.Ordinal);

			var configsForCurrentConnection =
					_settings.GetAllSolutionConfigurationdForConnection(
						_currentConnectionGuid);

			foreach (var config in configsForCurrentConnection)
			{
				if (!originIdentifiers.Contains(config.SolutionIndentifier))
				{
					_settings.RemoveSolutionConfiguration(config.SolutionIndentifier);
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
			_runLog.Log(message);
		}


		// ============================================================================
		/// <summary>
		/// Sink of <see cref="_runLog"/>: renders a finished line on the UI thread.
		/// The line, indent included, is built on the calling thread, because the
		/// worker changes the indent level between lines.
		/// </summary>
		private void AppendLogLine(string line)
		{
			// Invoke, not BeginInvoke: keeps the log order in step with the worker
			if (InvokeRequired)
			{
				richTextBox_log.Invoke(new Action(() => RenderLogLine(line)));
			}
			else
			{
				RenderLogLine(line);
			}
		}


		// ============================================================================
		private void RenderLogLine(string line)
		{
			_logger.Log(line);
			richTextBox_log.SelectionStart = richTextBox_log.Text.Length;
			richTextBox_log.ScrollToCaret();
		}

		// ============================================================================
		private void LogDebug(string message = "")
		{
			_runLog.LogDebug(message);
		}


		// ============================================================================
		private void LogError(string message)
		{
			_runLog.LogError(message);
		}


		// ============================================================================
		static string GetDurationString(
			DateTime startTime,
			bool noMilliseconds = false)
		{
			var duration = DateTime.Now - startTime;
			return GetDurationString(duration, noMilliseconds);
		}


		// ============================================================================
		static string GetDurationString(
			int seconds,
			bool noMilliseconds = false)
		{
			var duration = TimeSpan.FromSeconds(seconds);
			return GetDurationString(duration, noMilliseconds);
		}

		// ============================================================================
		static string GetDurationString(
			TimeSpan duration,
			bool noMilliseconds = false)
		{
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
			int index,
			SolutionConfiguration config = null)
		{
			if (index == -1)
			{
				return;
			}

			var solution = listBoxSolutions.Items[index].ItemObject as Solution;

			if (config == null)
			{
				config = _settings.GetSolutionConfiguration(solution.SolutionIdentifier, true);
			}

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
							solution.UniqueName,
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
							config.FileNameUnmanaged,
							solution.UniqueName,
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

			var targetByName = new Dictionary<string, Solution>(StringComparer.Ordinal);

			foreach (var targetSolution in _solutionsTarget)
			{
				targetByName[targetSolution.UniqueName] = targetSolution;
			}

			for (int i = 0; i < listBoxSolutions.Items.Count; i++)
			{
				var originSolution = listBoxSolutions.Items[i].ItemObject as Solution;

				if (targetByName.TryGetValue(originSolution.UniqueName, out var targetSolution))
				{
					(listBoxSolutions.Items[i].ItemObject as Solution).TargetVersionState =
						targetSolution.Version.ToString() == originSolution.Version.ToString()
							? Properties.Resources.match
							: Properties.Resources.missmatch;
				}
				else
				{
					(listBoxSolutions.Items[i].ItemObject as Solution).TargetVersionState =
						Properties.Resources.missing;
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

			var colFileVersionStatus =
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
					colFileVersionStatus     // 5
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

			// The unique name comes from the server: never let it carry a
			// path separator, a drive prefix or a ".." out of the folder.
			solutionName = FileAndFolderHelper.ToSafeFileNamePart(solutionName);

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
			// ============================================================================
		}


		// ============================================================================
		// ============================================================================
		/// <summary>
		/// The settings an execution run reads, copied on the UI thread when the
		/// run starts. Settings is not thread-safe, so the execution worker reads
		/// this snapshot and never touches _settings.
		/// </summary>
		private sealed class ExecutionSettingsSnapshot
		{
			private readonly Dictionary<string, SolutionConfiguration> _solutionConfigurations =
				new Dictionary<string, SolutionConfiguration>(StringComparer.Ordinal);

			public bool SaveVersionJson
			{
				get; private set;
			}
			public int RetryCount
			{
				get; private set;
			}
			public int RetryDelayInSeconds
			{
				get; private set;
			}
			public bool ContinueOnError
			{
				get; private set;
			}


			// ============================================================================
			/// <param name="settings">The settings to copy; read on the UI thread only.</param>
			/// <param name="solutions">The solutions of the run; their configurations are copied.</param>
			public ExecutionSettingsSnapshot(
				Settings settings,
				IEnumerable<Solution> solutions)
			{
				SaveVersionJson = settings.SaveVersionJson;
				RetryCount = settings.RetryCount;
				RetryDelayInSeconds = settings.RetryDelayInSeconds;
				ContinueOnError = settings.ContinueOnError;

				foreach (var solution in solutions)
				{
					if (solution == null)
					{
						continue;
					}

					var config =
						settings.GetSolutionConfiguration(
							solution.SolutionIdentifier);

					if (config == null)
					{
						continue;
					}

					// A copy: the live object belongs to _settings, which the UI
					// thread keeps changing during the run
					_solutionConfigurations[solution.SolutionIdentifier] =
						SolutionConfiguration.GetConfigFromJson(
							config.GetJson());
				}
			}


			// ============================================================================
			/// <summary>
			/// Returns the run's copy of the solution configuration.
			/// </summary>
			/// <returns>The copy; null when the solution had no configuration at the start of the run.</returns>
			public SolutionConfiguration GetSolutionConfiguration(
				string solutionIdentifier)
			{
				if (string.IsNullOrWhiteSpace(solutionIdentifier))
				{
					return null;
				}

				return
					_solutionConfigurations.TryGetValue(solutionIdentifier, out var config) ?
					config :
					null;
			}
		}


		// ============================================================================
		// ============================================================================
		private class ExtendedProgressChangedEventArgs : ProgressChangedEventArgs
		{
			public string Message
			{
				get; private set;
			}
			public string AdditionalMessage
			{
				get; private set;
			}
			public bool ResetTimer
			{
				get; private set;
			}

			// Constructor with a body to fix CS0501
			public ExtendedProgressChangedEventArgs(
				int progressPercentage,
				string message,
				string additionalMessage,
				bool resetTimer)
				: base(progressPercentage, null)
			{
				Message = message;
				AdditionalMessage = additionalMessage;
				ResetTimer = resetTimer;
			}
		}


		// ============================================================================
		/// <summary>
		/// Extended version of ReportProgress that includes additional data
		/// </summary>
		/// <param name="worker">BackgroundWorker instance</param>
		/// <param name="message">Main progress message</param>
		/// <param name="additionalData">Additional context data</param>
		private void ReportExtendedProgress(
			BackgroundWorker worker,
			string message,
			string additionalMessage = null,
			bool resetTimer = false)
		{
			if (worker == null || !worker.WorkerReportsProgress)
				return;

			worker.ReportProgress(
				0,
				new ExtendedProgressChangedEventArgs(
					0,
					message,
					additionalMessage,
					resetTimer));
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
