using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.Web.Services.Description;
using System.Windows.Forms;
using Com.AiricLenz.XTB.Plugin.Helpers;
using Com.AiricLenz.XTB.Plugin.Schema;
using LibGit2Sharp;
using McTools.Xrm.Connection;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Rest;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Client;
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
			if (listSolutions.SelectedIndex == -1)
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
				(listSolutions.SelectedItem as Solution);

			_currentSolutionConfig =
				_settings.GetSolutionConfiguration(
					_currentSolution.SolutionIdentifier,
					out bool isNew);

			if (isNew)
			{
				SaveSettings();
			}

			textBox_managed.Text = _currentSolutionConfig.FileNameManaged;
			textBox_unmanaged.Text = _currentSolutionConfig.FileNameUnmanaged;

			button_browseManaged.Enabled = true;
			button_browseUnmananged.Enabled = true;

			label_title.Text = _currentSolution.FriendlyName;
		}


		// ============================================================================
		private void PublishAll(
			IOrganizationService organizationService)
		{
			if (flipSwitch_publishSource.IsOff)
			{
				return;
			}

			Log("");
			Log("Publishing All now...");

			PublishAllXmlRequest publishRequest =
				new PublishAllXmlRequest();

			organizationService.Execute(publishRequest);

			Log("Publish All was completed.");
		}


		// ============================================================================
		private void ExportAllSolutions()
		{
			if (flipSwitch_exportManaged.IsOff &&
				flipSwitch_exportUnmanaged.IsOff)
			{
				return;
			}

			for (int i = 0; i < listSolutions.CheckedItems.Count; i++)
			{
				var listItem = listSolutions.CheckedItems[i] as Solution;
				var solution = listItem;

				var solutionConfig =
					_settings.GetSolutionConfiguration(
						solution.SolutionIdentifier,
						out bool isNew);

				if (isNew)
				{
					continue;
				}

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

			for (int i = 0; i < listSolutions.CheckedItems.Count; i++)
			{
				var listItem = listSolutions.CheckedItems[i] as Solution;
				var solution = listItem;

				var solutionConfig =
					_settings.GetSolutionConfiguration(
						solution.SolutionIdentifier,
						out bool isNew);

				if (isNew)
				{
					continue;
				}

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

			if (isManaged && string.IsNullOrWhiteSpace(solutionConfiguration.FileNameUnmanaged) ||
				!isManaged && string.IsNullOrWhiteSpace(solutionConfiguration.FileNameUnmanaged))
			{
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

			var message = "Bulk Solution Exporter Commit on " + DateTime.Now.ToString("yyyy-MM-dd / hh:mm");

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
				Log("|   The files does not exist.");
				return false;
			}

			byte[] solutionBytes = File.ReadAllBytes(solutionPath);

			// Create import request
			var importRequest = new ImportSolutionRequest()
			{
				CustomizationFile = solutionBytes,
				PublishWorkflows = true,
				//ImportJobId = Guid.NewGuid(), // Optional: Track the import job
				ConvertToManaged = isManaged // Set to true if importing as managed
			};

			// Execute import request
			try
			{
				IncreaseServiceTimeout();
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
		private void IncreaseServiceTimeout()
		{
			if (Service is OrganizationServiceProxy serviceProxy)
			{
				serviceProxy.Timeout = new TimeSpan(0, 15, 0); // Set to 15 minutes
			}
			/*
			else if (Service is ServiceClient serviceClient)
			{
				serviceClient.ClientCredentials.ServiceTimeout = TimeSpan.FromMinutes(5); // Set to 5 minutes
			}
			*/
		}


		// ============================================================================
		private void LoadAllSolutions()
		{
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
					}
					var result = args.Result as EntityCollection;
					if (result != null)
					{
						_solutions.Clear();

						var maxNameLength = 0;

						foreach (var entity in result.Entities)
						{
							var solutionFrinedlyName =
								entity.GetAttributeValue<string>(
									"friendlyname");

							maxNameLength =
								solutionFrinedlyName.Length > maxNameLength ?
								solutionFrinedlyName.Length :
								maxNameLength;
						}

						foreach (var entity in result.Entities)
						{
							_solutions.Add(
								Solution.ConvertFrom(entity, _currentConnectionGuid, maxNameLength));

						}

						UpdateSolutionList();

					}
				}
			});

			ExportButtonSetState();

		}

		// ============================================================================
		private void ExportButtonSetState()
		{
			var exportEnabled =
				(listSolutions.CheckedItems.Count > 0) &&
				(
					flipSwitch_exportManaged.IsOn ||
					flipSwitch_exportUnmanaged.IsOn ||
					flipSwitch_updateVersion.IsOn ||
					flipSwitch_importManaged.IsOn ||
					flipSwitch_importUnmanaged.IsOn
				);

			button_Export.Enabled = exportEnabled;

		}


		// ============================================================================
		private void UpdateSolutionList()
		{
			listSolutions.Items.Clear();

			foreach (var solution in _solutions)
			{
				listSolutions.Items.Add(solution);
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

				for (int i = 0; i < listSolutions.Items.Count; i++)
				{
					var solution = listSolutions.Items[i] as Solution;

					if (solution.SolutionIdentifier == config.SolutionIndentifier)
					{
						listSolutions.SetItemChecked(i, true);
					}
				}
			}

			ExportButtonSetState();

		}


		// ============================================================================
		private void SaveSettings()
		{
			if (!_codeUpdate)
			{
				SettingsManager.Instance.Save(GetType(), _settings);
			}

		}


		// ============================================================================
		private void Log(string message)
		{
			message += Environment.NewLine;

			if (textBox_log.InvokeRequired)
			{
				textBox_log.Invoke(new Action(() => textBox_log.Text += message));
			}
			else
			{
				textBox_log.Text += message;
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

				flipSwitch_publishSource.IsOn = _settings.PublishAllPreExport;
				flipSwitch_updateVersion.IsOn = _settings.UpdateVersion;

				flipSwitch_gitCommit.IsOn = _settings.GitCommit;
				flipSwitch_gitCommit.Enabled =
					flipSwitch_exportManaged.IsOn ||
					flipSwitch_exportUnmanaged.IsOn;
				flipSwitch_pushCommit.IsOn = _settings.PushCommit;
				flipSwitch_pushCommit.Enabled = flipSwitch_gitCommit.IsOn;

				textBox_versionFormat.Visible = flipSwitch_updateVersion.IsOn;
				label_versionFormat.Visible = flipSwitch_updateVersion.IsOn;
				pictureBox_info.Visible = flipSwitch_updateVersion.IsOn;

				textBox_versionFormat.Text = _settings.VersionFormat;

				_codeUpdate = false;

				ExportButtonSetState();
			}

			button_loadSolutions.Enabled = Service != null;
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
			textBox_versionFormat.Enabled = flipSwitch_updateVersion.IsOn;

			textBox_versionFormat.Visible = flipSwitch_updateVersion.IsOn;
			label_versionFormat.Visible = flipSwitch_updateVersion.IsOn;
			pictureBox_info.Visible = flipSwitch_updateVersion.IsOn;

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

			SaveSettings();
			ExportButtonSetState();
		}

		// ============================================================================
		private void textBox_versionFormat_TextChanged(object sender, EventArgs e)
		{
			_settings.VersionFormat = textBox_versionFormat.Text;
			SaveSettings();
		}


		// ============================================================================
		private void listSolutions_SelectedIndexChanged(object sender, EventArgs e)
		{
			var rowIsSelected = listSolutions.SelectedIndex != -1;

			if (rowIsSelected)
			{
				UpdateSolutionSettingsScreen();
			}

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

				foreach (Solution solution in listSolutions.Items)
				{
					if (solution.SolutionIdentifier == config.SolutionIndentifier)
					{
						var isCchecked = listSolutions.CheckedItems.Contains(solution);

						_settings.SetSelectedStatus(
							config.SolutionIndentifier,
							isCchecked);

					}
				}
			}

			SaveSettings();
		}


		// ============================================================================
		private void textBox_managed_TextChanged(object sender, EventArgs e)
		{
			_currentSolutionConfig.FileNameManaged = textBox_managed.Text;
			_settings.AddSolutionConfiguration(_currentSolutionConfig);
			SaveSettings();
		}


		// ============================================================================
		private void textBox_unmanaged_TextChanged(object sender, EventArgs e)
		{
			_currentSolutionConfig.FileNameUnmanaged = textBox_unmanaged.Text;
			_settings.AddSolutionConfiguration(_currentSolutionConfig);
			SaveSettings();
		}


		// ============================================================================
		private void button_browseManaged_Click(object sender, EventArgs e)
		{
			var selectedSolution =
				listSolutions.SelectedItem as Solution;

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
			   listSolutions.SelectedItem as Solution;

			if (selectedSolution == null)
			{
				return;
			}

			saveFileDialog1.Title = "Save Unanaged file as...";

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
					PublishAll(Service);    // Source environment
					ExportAllSolutions();
					HandleGit();
					ImportAllSolutions();

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
		private void listSolutions_ItemCheck(object sender, ItemCheckEventArgs e)
		{
			ExportButtonSetState();
		}


		// ============================================================================
		private void button_addAdditionalConnection_Click(object sender, EventArgs e)
		{
			AddAdditionalOrganization();
		}




		#endregion



	}
}