using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.ServiceModel;
using System.Windows.Forms;
using Com.AiricLenz.XTB.Components;
using Com.AiricLenz.XTB.Plugin.Helpers;
using Com.AiricLenz.XTB.Plugin.Schema;
using McTools.Xrm.Connection;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
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
		private IOrganizationService _targetServiceClient = null;

		private Settings _settings;
		private bool _codeUpdate = true;
		private Guid _currentConnectionGuid;
		private SolutionConfiguration _currentSolutionConfig;

		private List<string> _sessionFiles = new List<string>();
		private List<Solution> _solutions = new List<Solution>();

		private Solution _currentSolution;


		// ##################################################
		// ##################################################

		#region Custom Logic


		// ============================================================================
		private void UpdateSolutionSettingsScreen()
		{
			if (listBoxSolutions.SelectedIndex == -1)
			{
				button_browseManaged.Enabled = false;
				button_browseUnmananged.Enabled = false;

				textBox_managed.Text = string.Empty;
				textBox_unmanaged.Text = string.Empty;

				label_title.Text = "No Solution selected";

				_currentSolution = null;

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

			textBox_managed.Text = _currentSolutionConfig.FileNameManaged;
			textBox_unmanaged.Text = _currentSolutionConfig.FileNameUnmanaged;

			button_browseManaged.Enabled = true;
			button_browseUnmananged.Enabled = true;

			label_title.Text = _currentSolution.FriendlyName;
		}


		// ============================================================================
		private void PublishAll(
			IOrganizationService organizationService,
			bool isEnabled)
		{
			if (!isEnabled)
			{
				return;
			}

			Log("");
			Log("Publishing All now...");

			PublishAllXmlRequest publishRequest =
				new PublishAllXmlRequest();

			try
			{
				organizationService.Execute(publishRequest);
			}
			catch (Exception ex)
			{
				Log(ex.Message);
				return;
			}

			Log("Publish All was completed.");
		}


		// ============================================================================
		private void ExportAllSolutions()
		{
			if (flipSwitch_updateVersion.IsOff &&
				flipSwitch_exportManaged.IsOff &&
				flipSwitch_exportUnmanaged.IsOff)
			{
				return;
			}

			for (int i = 0; i < listBoxSolutions.CheckedItems.Count; i++)
			{
				var listItem = listBoxSolutions.CheckedItems[i];
				var solution = listItem.ItemObject as Solution;

				var solutionConfig =
					_settings.GetSolutionConfiguration(
						solution.SolutionIdentifier);

				Log("");
				Log("Exporting solution '" + solution.FriendlyName + "'");

				var exportResult =
					ExportSolution(
						solution,
						solutionConfig);

				if (!exportResult)
				{
					Log("");
					Log("Export aborted due to an error.");
					break;
				}

			}
		}

		// ============================================================================
		private void ImportAllSolutions()
		{
			if (_targetServiceClient == null ||
				(
					flipSwitch_importManaged.IsOff &&
					flipSwitch_importUnmanaged.IsOff
				))
			{
				return;
			}

			bool importManagd = flipSwitch_importManaged.IsOn;

			Log("");
			Log("Importing solutions now");

			for (int i = 0; i < listBoxSolutions.CheckedItems.Count; i++)
			{
				var listItem = listBoxSolutions.CheckedItems[i];
				var solution = listItem.ItemObject as Solution;

				var solutionConfig =
					_settings.GetSolutionConfiguration(
						solution.SolutionIdentifier);

				var solutionFile =
					importManagd ? solutionConfig.FileNameManaged : solutionConfig.FileNameUnmanaged;

				if (ImportSolution(solutionFile, importManagd))
				{
					Log(
						"|   Imported the " +
						(importManagd ? "managed" : "unmanaged") +
						" solution: " + solution.FriendlyName);
				}
			}
		}



		// ============================================================================
		private bool ExportSolution(
			Solution solution,
			SolutionConfiguration solutionConfiguration)
		{
			if (flipSwitch_updateVersion.IsOn)
			{
				if (!UpdateVersionNumber(solution))
				{
					return false;
				}
			}

			if (flipSwitch_exportManaged.IsOn)
			{
				ExportToFile(
					solution,
					solutionConfiguration,
					true);
			}

			if (flipSwitch_exportUnmanaged.IsOn)
			{
				ExportToFile(
					solution,
					solutionConfiguration,
					false);
			}

			return true;
		}


		// ============================================================================
		private void ExportToFile(
			Solution solution,
			SolutionConfiguration solutionConfiguration,
			bool isManaged)
		{

			if (isManaged && string.IsNullOrWhiteSpace(solutionConfiguration.FileNameManaged) ||
				!isManaged && string.IsNullOrWhiteSpace(solutionConfiguration.FileNameUnmanaged))
			{
				if (isManaged)
				{
					Log("|   No file name for a managed solution was given!");
				}
				else
				{
					Log("|   No file name for an unmanaged solution was given!");
				}

				return;
			}

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

			if (isManaged)
			{
				Log("|   Exported the solution as a managed to: " + filePath);
			}
			else
			{
				Log("|   Exported the solution as an unmanaged to: " + filePath);
			}

		}



		// ============================================================================
		private bool UpdateVersionNumber(
			Solution solution)
		{
			var oldVersionString = solution.Version.ToString();

			var versionUpdateResult =
				solution.Version.IncreaseVersionNumber(
					textBox_versionFormat.Text);

			if (!versionUpdateResult)
			{
				Log("|   The version format is invalid!");
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

			Log("|   Updated the version number: " + oldVersionString + " --> " + solution.Version);

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

			return true;
		}


		// ============================================================================
		private void HandleGit()
		{
			if (flipSwitch_gitCommit.IsOff)
			{
				return;
			}

			Log("");
			Log("Now commiting the new files to Git");

			if (_sessionFiles.Count == 0)
			{
				Log("|   No files need to be committed.");
				return;
			}

			var gitRootPath =
				FileAndFolderHelper.FindRepositoryRootPath(_sessionFiles.First());

			if (gitRootPath == string.Empty)
			{
				Log("|   No GIT repository was found where the files were saved.");
				return;
			}

			GitHelper gitHelper = new GitHelper(gitRootPath);
			string errorMessage;

			foreach (var file in _sessionFiles)
			{
				var fileShortened = file.Replace(gitRootPath, "");

				if (fileShortened.StartsWith("\\"))
				{
					// remove the first backslash
					fileShortened = fileShortened.Substring(1);
				}

				if (!gitHelper.ExecuteCommand("add \"" + fileShortened + "\"", out errorMessage))
				{
					Log("|   Error staging: " + errorMessage);
				}
			}

			var message = textBox_commitMessage.Text;


			if (!gitHelper.ExecuteCommand("commit -m \"" + message + "\"", out errorMessage))
			{
				Log("|   Error commiting: " + errorMessage);
			}
			else
			{
				Log("|   " + _sessionFiles.Count + " File(s) was/weres commited: " + message);
			}


			if (flipSwitch_pushCommit.IsOff)
			{
				return;
			}

			if (!gitHelper.ExecuteCommand("push", out errorMessage))
			{
				Log("|   Error pushing: " + errorMessage);
			}
			else
			{
				Log("|   The commit has been pushed to the remote origin.");
			}


		}


		// ============================================================================
		private bool ImportSolution(
			string solutionPath,
			bool isManaged = true)
		{
			if (!File.Exists(solutionPath))
			{
				Log("|   The file '" + solutionPath + "' does not exist.");
				return false;
			}

			byte[] solutionBytes = File.ReadAllBytes(solutionPath);

			// Create import request
			var importRequest = new ImportSolutionRequest()
			{
				CustomizationFile = solutionBytes,
				PublishWorkflows = flipSwitch_enableAutomation.IsOn,
				OverwriteUnmanagedCustomizations = flipSwitch_overwrite.IsOn,
				ConvertToManaged = isManaged,
				ImportJobId = Guid.NewGuid(), // Optional: Track the import job
			};

			// Execute import request
			try
			{
				_targetServiceClient.Execute(importRequest);
			}
			catch (FaultException ex)
			{
				Log("|   Error during import: " + ex.Message);
				return false;
			}
			catch (Exception ex)
			{
				Log("|   Error during import: " + ex.Message);
				return false;
			}

			return true;
		}


		// ============================================================================
		private void LoadAllSolutions()
		{

			if (Service == null)
			{
				return;
			}

			WorkAsync(new WorkAsyncInfo
			{
				Message = "Getting Solutions",
				Work = (worker, args) =>
				{
					var query = new QueryExpression("solution");

					query.Criteria.AddCondition(
						"ismanaged",
						ConditionOperator.Equal,
						false);


					query.Criteria.AddCondition(
						"isvisible",
						ConditionOperator.Equal,
						true);


					query.ColumnSet.AddColumns(
						"friendlyname",
						"solutiontype",
						"ismanaged",
						"solutionid",
						"uniquename",
						"description",
						"version");

					args.Result = Service.RetrieveMultiple(query);
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

					var result = args.Result as EntityCollection;
					if (result != null)
					{
						_solutions.Clear();

						var maxNameLength = 0;

						// sort alphabetically after loading
						var sortedSolutionsEntities =
							result.Entities.OrderBy(
								item => item.GetAttributeValue<string>("friendlyname")).ToList();

						// check max length of all solution names
						foreach (var entity in sortedSolutionsEntities)
						{
							var solutionFrinedlyName =
								entity.GetAttributeValue<string>(
									"friendlyname");

							maxNameLength =
								solutionFrinedlyName.Length > maxNameLength ?
								solutionFrinedlyName.Length :
								maxNameLength;
						}

						// create our custom solutions records
						foreach (var entity in sortedSolutionsEntities)
						{
							var newSolution =
								Solution.ConvertFrom(
									entity,
									_currentConnectionGuid,
									maxNameLength);

							var config =
								_settings.GetSolutionConfiguration(
									newSolution.SolutionIdentifier,
									true);

							newSolution.SortingIndex = config.SortingIndex;

							_solutions.Add(
								newSolution);
						}
					}

					SaveSettings(true);
					UpdateSolutionList();
					ExportButtonSetState();
				}
			});



		}

		// ============================================================================
		private void ExportButtonSetState()
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

			_solutions.Sort();

			for (int i = 0; i < _solutions.Count; i++)
			{
				var solution = _solutions[i];

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

				if (!config.Selected)
				{
					continue;
				}

				for (int i = 0; i < listBoxSolutions.Items.Count; i++)
				{
					var solution = listBoxSolutions.Items[i].ItemObject as Solution;

					if (solution.SolutionIdentifier == config.SolutionIndentifier)
					{
						listBoxSolutions.SetItemChecked(i, true);
					}
				}
			}

			listBoxSolutions.Refresh();
			listBoxSolutions.Refresh();

			ExportButtonSetState();

		}


		// ============================================================================
		private void SaveSettings(
			bool cleanUpNonExistingSolutions = false)
		{
			if (_codeUpdate)
			{
				return;
			}

			// remove solution-configuration from the config
			// that have no solution in this environment anymore
			if (cleanUpNonExistingSolutions)
			{
				var configsForCurrentConnection =
					_settings.GetAllSolutionConfigurationdForConnection(
						_currentConnectionGuid);

				foreach (var solution in _solutions)
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

			// Save
			SettingsManager.Instance.Save(GetType(), _settings);
		}






		// ============================================================================
		private void Log(string message)
		{
			message += Environment.NewLine;

			if (textBox_log.InvokeRequired)
			{
				textBox_log.Invoke(new Action(() => textBox_log.Text += message));
				textBox_log.SelectionStart = textBox_log.Text.Length;
				textBox_log.ScrollToCaret();
			}
			else
			{
				textBox_log.Text += message;
				textBox_log.SelectionStart = textBox_log.Text.Length;
				textBox_log.ScrollToCaret();
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

			textBox_log.Text = string.Empty;

			listBoxSolutions.Columns.Add(
				new ColumnDefinition
				{
					Header = "Solution Name",
					PropertyName = "FriendlyName",
					WidthPercent = 60
				});

			listBoxSolutions.Columns.Add(
				new ColumnDefinition
				{
					Header = "Version",
					PropertyName = "Version",
					WidthPercent = 40
				});
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
				_codeUpdate = false;

				SaveSettings();

				LogWarning("Settings not found => a new settings file has been created!");
			}
			else
			{
				LogInfo("Settings found and loaded");

				flipSwitch_exportManaged.IsOn = _settings.ExportManaged;
				flipSwitch_exportUnmanaged.IsOn = _settings.ExportUnmanaged;

				flipSwitch_importManaged.IsOn = _settings.ImportManaged;
				flipSwitch_importManaged.Enabled = _targetServiceClient != null;
				flipSwitch_importUnmanaged.IsOn = _settings.ImportManaged;
				flipSwitch_importUnmanaged.Enabled = _targetServiceClient != null;
				flipSwitch_enableAutomation.IsOn = _settings.EnableAutomation;
				flipSwitch_overwrite.IsOn = _settings.OverwriteCustomizations;
				UpdateImportOptionsVisibility();

				flipSwitch_publishSource.IsOn = _settings.PublishAllPreExport;
				flipSwitch_publishTarget.IsOn = _settings.PublishAllPostImport;
				flipSwitch_updateVersion.IsOn = _settings.UpdateVersion;
				textBox_versionFormat.Text = _settings.VersionFormat;
				UpdateVersionOptionsVisibility();

				flipSwitch_gitCommit.IsOn = _settings.GitCommit;
				flipSwitch_gitCommit.Enabled =
					flipSwitch_exportManaged.IsOn ||
					flipSwitch_exportUnmanaged.IsOn;
				flipSwitch_pushCommit.IsOn = _settings.PushCommit;
				flipSwitch_pushCommit.Enabled = flipSwitch_gitCommit.IsOn;
				textBox_commitMessage.Text = _settings.CommitMessage;
				UpdateGitOptionsVisibility();

				checkButton_showLogicalNames.Checked = _settings.ShowLogicalSolutionNames;

				_codeUpdate = false;

				ExportButtonSetState();
			}

			button_loadSolutions.Enabled = Service != null;
			LoadAllSolutions();

		}


		// ============================================================================
		private void UpdateGitOptionsVisibility()
		{
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

			flipSwitch_enableAutomation.Enabled = optionEnabled;
			flipSwitch_overwrite.Enabled = optionEnabled;
			flipSwitch_publishTarget.Enabled = optionEnabled;
		}

		// ============================================================================
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
			SaveSettings();
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
			base.UpdateConnection(newService, detail, actionName, parameter);

			if (actionName != "AdditionalOrganization")
			{

				_currentConnectionGuid = detail.ConnectionId.Value;

				if (_settings != null && detail != null)
				{
					_settings.LastUsedOrganizationWebappUrl = detail.WebApplicationUrl;
					LogInfo("Connection has changed to: {0}", detail.WebApplicationUrl);
				}

				if (Service != null)
				{
					LoadAllSolutions();
				}

				button_loadSolutions.Enabled = Service != null;
			}
		}


		// ============================================================================
		protected override void ConnectionDetailsUpdated(NotifyCollectionChangedEventArgs e)
		{
			if (e.Action.Equals(NotifyCollectionChangedAction.Add))
			{
				var detail = (ConnectionDetail) e.NewItems[0];
				_targetServiceClient = detail.ServiceClient;

				flipSwitch_importManaged.Enabled = _targetServiceClient != null;
				flipSwitch_importUnmanaged.Enabled = _targetServiceClient != null;

				button_addAdditionalConnection.Enabled = false;

				UpdateImportOptionsVisibility();
			}
		}


		// ============================================================================
		private void button_loadSolutions_Click(object sender, EventArgs e)
		{
			LoadAllSolutions();
			ExportButtonSetState();
		}

		// ============================================================================
		private void flipSwitch_publish_Toggled(object sender, EventArgs e)
		{
			_settings.PublishAllPreExport = flipSwitch_publishSource.IsOn;

			SaveSettings();
			ExportButtonSetState();
		}


		// ============================================================================
		private void flipSwitch_updateVersion_Toggled(object sender, EventArgs e)
		{
			_settings.UpdateVersion = flipSwitch_updateVersion.IsOn;

			UpdateVersionOptionsVisibility();

			SaveSettings();
			ExportButtonSetState();
		}


		// ============================================================================
		private void flipSwitch_exportManaged_Toggled(object sender, EventArgs e)
		{
			_settings.ExportManaged = flipSwitch_exportManaged.IsOn;

			_codeUpdate = true;

			var gitEnabeld =
				flipSwitch_exportManaged.IsOn ||
				flipSwitch_exportUnmanaged.IsOn;

			flipSwitch_gitCommit.Enabled = gitEnabeld;
			flipSwitch_pushCommit.Enabled = gitEnabeld;

			_codeUpdate = false;

			SaveSettings();
			ExportButtonSetState();
		}

		// ============================================================================
		private void flipSwitch_exportUnmanaged_Toggled(object sender, EventArgs e)
		{
			_settings.ExportUnmanaged = flipSwitch_exportManaged.IsOn;

			_codeUpdate = true;

			var gitEnabeld =
				flipSwitch_exportManaged.IsOn ||
				flipSwitch_exportUnmanaged.IsOn;

			flipSwitch_gitCommit.Enabled = gitEnabeld;
			flipSwitch_pushCommit.Enabled = gitEnabeld;

			_codeUpdate = false;

			SaveSettings();
			ExportButtonSetState();
		}


		// ============================================================================
		private void flipSwitch_gitCommit_Toggled(object sender, EventArgs e)
		{
			_settings.GitCommit = flipSwitch_gitCommit.IsOn;
			flipSwitch_pushCommit.Enabled = flipSwitch_gitCommit.IsOn;

			UpdateGitOptionsVisibility();

			SaveSettings();
			ExportButtonSetState();
		}

		// ============================================================================
		private void flipSwitch_pushCommit_Toggled(object sender, EventArgs e)
		{
			_settings.PushCommit = flipSwitch_pushCommit.IsOn;

			SaveSettings();
			ExportButtonSetState();

		}


		// ============================================================================
		private void flipSwitch_importManaged_Toggled(object sender, EventArgs e)
		{
			_settings.ImportManaged =
				flipSwitch_importManaged.IsOn &&
				_targetServiceClient != null;

			if (flipSwitch_importManaged.IsOn &&
				flipSwitch_importUnmanaged.IsOn &&
				!_codeUpdate)
			{
				_codeUpdate = true;
				flipSwitch_importUnmanaged.IsOn = false;
				_settings.ImportUnmanaged = false;
				_codeUpdate = false;
			}

			UpdateImportOptionsVisibility();

			SaveSettings();
			ExportButtonSetState();
		}

		// ============================================================================
		private void flipSwitch_importUnmanaged_Toggled(object sender, EventArgs e)
		{
			_settings.ImportUnmanaged =
				flipSwitch_importUnmanaged.IsOn &&
				_targetServiceClient != null;

			if (flipSwitch_importManaged.IsOn &&
				flipSwitch_importUnmanaged.IsOn &&
				!_codeUpdate)
			{
				_codeUpdate = true;
				flipSwitch_importManaged.IsOn = false;
				_settings.ImportManaged = false;
				_codeUpdate = false;
			}

			UpdateImportOptionsVisibility();

			SaveSettings();
			ExportButtonSetState();
		}


		// ============================================================================
		private void textBox_commitMessage_TextChanged(object sender, EventArgs e)
		{
			_settings.CommitMessage = textBox_commitMessage.Text;
			ExportButtonSetState();
		}


		// ============================================================================
		private void textBox_commitMessage_Leave(object sender, EventArgs e)
		{
			_settings.CommitMessage = textBox_commitMessage.Text;
			SaveSettings();
		}


		// ============================================================================
		private void textBox_versionFormat_TextChanged(object sender, EventArgs e)
		{
			_settings.VersionFormat = textBox_versionFormat.Text;
			ExportButtonSetState();
		}

		// ============================================================================
		private void textBox_versionFormat_Leave(object sender, EventArgs e)
		{
			_settings.VersionFormat = textBox_versionFormat.Text;
			SaveSettings();
		}


		// ============================================================================
		private void flipSwitch_enableAutomation_Toggled(object sender, EventArgs e)
		{
			_settings.EnableAutomation = flipSwitch_enableAutomation.IsOn;
			SaveSettings();
		}

		// ============================================================================
		private void flipSwitch_overwrite_Toggled(object sender, EventArgs e)
		{
			_settings.OverwriteCustomizations = flipSwitch_overwrite.IsOn;
			SaveSettings();

		}

		// ============================================================================
		private void flipSwitch_publishTarget_Toggled(object sender, EventArgs e)
		{
			_settings.PublishAllPostImport = flipSwitch_publishTarget.IsOn;
			SaveSettings();
		}


		// ============================================================================
		private void listSolutions_SelectedIndexChanged(object sender, EventArgs e)
		{
			UpdateSolutionSettingsScreen();

			var rowIsSelected = listBoxSolutions.SelectedIndex != -1;

			button_browseManaged.Enabled = rowIsSelected;
			button_browseUnmananged.Enabled = rowIsSelected;

			ExportButtonSetState();


			// update the setting.solutionConfiurations to reflect the 
			// checked property of all solution-configs
			for (int i = 0; i < _settings.SolutionConfigurations.Count; i++)
			{
				var config =
					SolutionConfiguration.GetConfigFromJson(
						_settings.SolutionConfigurations[i]);

				foreach (SortableCheckItem listItem in listBoxSolutions.Items)
				{
					var solution = listItem.ItemObject as Solution;

					if (solution.SolutionIdentifier == config.SolutionIndentifier)
					{
						var isCchecked = listBoxSolutions.CheckedItems.Contains(listItem);

						_settings.SetCheckedStatus(
							config.SolutionIndentifier,
							isCchecked);

						break;
					}
				}
			}

			SaveSettings();
		}


		// ============================================================================
		private void listBoxSolutions_ItemOrderChanged(object sender, EventArgs e)
		{
			for (int i = 0; i < _settings.SolutionConfigurations.Count; i++)
			{
				var config =
					SolutionConfiguration.GetConfigFromJson(
						_settings.SolutionConfigurations[i]);

				var index = 0;

				for (int s = 0; s < listBoxSolutions.Items.Count; s++)
				{
					var listItem = listBoxSolutions.Items[s];
					var solution = listItem.ItemObject as Solution;

					if (solution.SolutionIdentifier == config.SolutionIndentifier)
					{
						_settings.SetSortingIndex(
							config.SolutionIndentifier,
							index);

						break;
					}

					index++;
				}
			}

			SaveSettings();
		}


		// ============================================================================
		private void textBox_managed_TextChanged(object sender, EventArgs e)
		{
			_currentSolutionConfig.FileNameManaged = textBox_managed.Text;
			_settings.UpdateSolutionConfiguration(_currentSolutionConfig);
			SaveSettings();
		}

		// ============================================================================
		private void textBox_unmanaged_TextChanged(object sender, EventArgs e)
		{
			_currentSolutionConfig.FileNameUnmanaged = textBox_unmanaged.Text;
			_settings.UpdateSolutionConfiguration(_currentSolutionConfig);
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

			if (!string.IsNullOrWhiteSpace(textBox_managed.Text))
			{
				saveFileDialog1.FileName = textBox_managed.Text;
			}
			else
			{
				saveFileDialog1.FileName = "Managed." + selectedSolution.FriendlyName + ".zip";
			}

			if (saveFileDialog1.ShowDialog() == DialogResult.OK)
			{
				textBox_managed.Text = saveFileDialog1.FileName;
			}
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

			if (!string.IsNullOrWhiteSpace(textBox_unmanaged.Text))
			{
				saveFileDialog1.FileName = textBox_unmanaged.Text;
			}
			else
			{
				saveFileDialog1.FileName = selectedSolution.FriendlyName + ".zip";
			}

			if (saveFileDialog1.ShowDialog() == DialogResult.OK)
			{
				textBox_unmanaged.Text = saveFileDialog1.FileName;
			}
		}

		// ============================================================================
		private void button_Export_Click(object sender, EventArgs e)
		{
			listBoxSolutions.DeselectAll();
			textBox_log.Text = string.Empty;
			_sessionFiles.Clear();

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

			var message = string.Join(" / ", actions) + " Solutions...";

			WorkAsync(new WorkAsyncInfo
			{
				Message = message,
				Work = (worker, args) =>
				{
					PublishAll(Service, flipSwitch_publishSource.IsOn);
					ExportAllSolutions();
					HandleGit();
					ImportAllSolutions();
					PublishAll(_targetServiceClient, flipSwitch_publishTarget.IsOn);

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
					Log("");
					Log("Done.");
				}
			});



		}

		// ============================================================================
		private void listSolutions_ItemCheck(object sender, EventArgs e)
		{
			ExportButtonSetState();
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
		}

		// ============================================================================
		private void button_invertCheck_Click(object sender, EventArgs e)
		{
			listBoxSolutions.InvertCheckOfAllItems();
		}

		// ============================================================================
		private void button_uncheckAll_Click(object sender, EventArgs e)
		{
			listBoxSolutions.UnCheckAllItems();
		}

		// ============================================================================
		private void button_sortAsc_Click(object sender, EventArgs e)
		{
			listBoxSolutions.SortAlphabetically(
				SortOrder.Ascending);
		}

		// ============================================================================
		private void button_sortDesc_Click(object sender, EventArgs e)
		{
			listBoxSolutions.SortAlphabetically(
				SortOrder.Descending);
		}


		// ============================================================================
		private void splitContainer1_Panel1_Resize(object sender, EventArgs e)
		{
			/*
			if (splitContainer1.Panel1.Width < 400)
			{
				splitContainer1.SplitterDistance = 400;
				splitContainer1.Invalidate();
			}
			*/
		}


		// ============================================================================
		private void checkButton_showLogicalNames_CheckStateChanged(object sender, EventArgs e)
		{
			var newColumnSetup =
				new List<ColumnDefinition>
				{
					new ColumnDefinition
					{
						Header = "Solution Name",
						PropertyName = "FriendlyName",
						WidthPercent = 42
					},

					new ColumnDefinition
					{
						Header = "Logical Name",
						PropertyName = "UniqueName",
						WidthPercent = 38
					},

					new ColumnDefinition
					{
						Header = "Version",
						PropertyName = "Version",
						WidthPercent = 20
					}
				};

			if (checkButton_showLogicalNames.Checked == false)
			{
				newColumnSetup.RemoveAt(1);
				newColumnSetup[0].WidthPercent = 65;
				newColumnSetup[1].WidthPercent = 35;
			}

			listBoxSolutions.Columns = newColumnSetup;
			listBoxSolutions.Refresh();

			_settings.ShowLogicalSolutionNames = checkButton_showLogicalNames.Checked;
			SaveSettings();
		}



		#endregion



	}
}