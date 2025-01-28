using System;
using System.Activities.Presentation.Debug;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Drawing;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.Windows.Forms;
using System.Windows.Media.Animation;
using System.Xml.Serialization;
using Com.AiricLenz.Extentions;
using Com.AiricLenz.XTB.Components;
using Com.AiricLenz.XTB.Plugin.Helpers;
using Com.AiricLenz.XTB.Plugin.Schema;
using McTools.Xrm.Connection;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
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
		private IOrganizationService _targetServiceClient = null;

		private Settings _settings;
		private bool _codeUpdate = true;
		private Guid _currentConnectionGuid;
		private SolutionConfiguration _currentSolutionConfig;

		private List<string> _sessionFiles = new List<string>();
		private List<Solution> _solutionsOrigin = new List<Solution>();
		private List<Solution> _solutionsTarget = new List<Solution>();

		private Solution _currentSolution;
		private object origin;
		private object target;


		// ##################################################
		// ##################################################

		#region Custom Logic


		// ============================================================================
		private void UpdateSolutionSettingsScreen()
		{
			if (listBoxSolutions.SelectedIndex == -1)
			{
				_codeUpdate = true;

				_currentSolution = null;
				_currentSolutionConfig = null;

				button_browseManaged.Enabled = false;
				button_browseUnmananged.Enabled = false;

				textBox_managed.Text = string.Empty;
				textBox_unmanaged.Text = string.Empty;

				label_title.Text = "No Solution selected (click a solution in the list)";
				_codeUpdate = false;

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

			_codeUpdate = true;
			textBox_managed.Text = _currentSolutionConfig.FileNameManaged;
			textBox_unmanaged.Text = _currentSolutionConfig.FileNameUnmanaged;
			_codeUpdate = false;

			button_browseManaged.Enabled = true;
			button_browseUnmananged.Enabled = true;

			label_title.Text = _currentSolution.FriendlyName;

			UpdateFileWarningStatus();

		}

		// ============================================================================
		private void UpdateFileWarningStatus()
		{
			if (_currentSolution == null)
			{
				pictureBox_warningManaged.Visible = false;
				pictureBox_warningUnmanaged.Visible = false;

				return;
			}

			// TODO

			//pictureBox_warningManaged.Visible = isManagedVersionOutdated;
			//pictureBox_warningUnmanaged.Visible = isUnmanagedVersionOutdated;
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

			var startTime = DateTime.Now;

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


			var duration = GetDurationString(startTime);

			Log("Publish All was completed (" + duration + ").");
		}


		// ============================================================================
		private void UpdatecheckedVersionNumbers()
		{
			if (!flipSwitch_updateVersion.IsOn ||
				listBoxSolutions.CheckedItems.Count == 0)
			{
				return;
			}

			Log("Updating version numbers now:");

			for (int i = 0; i < listBoxSolutions.CheckedItems.Count; i++)
			{
				var listItem = listBoxSolutions.CheckedItems[i];
				var solution = listItem.ItemObject as Solution;

				var solutionConfig =
					_settings.GetSolutionConfiguration(
						solution.SolutionIdentifier);

				Log("|   Updating version number for solution '" + solution.FriendlyName + "'");

				UpdateVersionNumber(solution);

				if (i < listBoxSolutions.CheckedItems.Count - 1)
				{
					Log("|");
				}
			}

			Log();
		}



		// ============================================================================
		private void ExportCheckedSolutions()
		{
			if (flipSwitch_exportManaged.IsOff &&
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
				Log("Exporting solution '" + solution.FriendlyName + "':");

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
					if (exportedSolutions.Managed[i].SolutionId.ToLower() == solution.SolutionId.ToString().ToLower() &&
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
					if (exportedSolutions.Unmanaged[i].SolutionId.ToLower() == solution.SolutionId.ToString().ToLower() &&
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
		private void ImportCheckedSolutions()
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

			for (int i = 0; i < listBoxSolutions.CheckedItems.Count; i++)
			{
				var listItem = listBoxSolutions.CheckedItems[i];
				var solution = listItem.ItemObject as Solution;

				var solutionConfig =
					_settings.GetSolutionConfiguration(
						solution.SolutionIdentifier);

				var solutionFile =
					importManagd ? solutionConfig.FileNameManaged : solutionConfig.FileNameUnmanaged;

				var startTime = DateTime.Now;

				Log("Importing solution '" + solution.FriendlyName + "':");

				if (ImportSolution(solutionFile, importManagd))
				{

					var duration = GetDurationString(startTime);
					Log("|   The import was succesful.");
					Log("|   Duration: " + duration);
				}

				if (i < listBoxSolutions.Items.Count - 1)
				{
					Log();
				}
			}
		}



		// ============================================================================
		private bool ExportSolution(
			Solution solution,
			SolutionConfiguration solutionConfiguration)
		{
			if (flipSwitch_exportManaged.IsOn)
			{
				ExportToFile(
					solution,
					solutionConfiguration,
					true);
			}

			if (flipSwitch_exportManaged.IsOn &&
				flipSwitch_exportUnmanaged.IsOn)
			{
				Log("|");
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

			Log("|   Exporting as " + (isManaged ? "" : "un") + "managed solution now...");


			if (isManaged && string.IsNullOrWhiteSpace(solutionConfiguration.FileNameManaged) ||
				!isManaged && string.IsNullOrWhiteSpace(solutionConfiguration.FileNameUnmanaged))
			{
				Log("|   No file name for the " + (isManaged ? "" : "un") + "managed solution was given!");
				return;
			}

			var startTime = DateTime.Now;

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
			Log("|   Exported to file: " + filePath);
			Log("|   Duration: " + duration);


			if (_settings.SaveVersionJson)
			{
				SaveVersionJson(
					filePath,
					solution,
					isManaged);
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
				var errorMessage =
					Formatter.FormatErrorStringWithXml(
						ex.Message);

				Log("|   " + errorMessage);

				return false;
			}
			catch (Exception ex)
			{
				var errorMessage =
					Formatter.FormatErrorStringWithXml(
						ex.Message);

				Log("|   " + errorMessage);

				return false;
			}

			return true;
		}


		// ============================================================================
		private void LoadAllSolutions()
		{
			WorkAsync(new WorkAsyncInfo
			{
				Message = "Getting All Solutions",
				Work = (worker, args) =>
				{
					// ORIGIN SOLUTIONS
					var queryOrigin = new QueryExpression("solution");

					queryOrigin.Criteria.AddCondition(
						"ismanaged",
						ConditionOperator.Equal,
						false);

					queryOrigin.Criteria.AddCondition(
						"isvisible",
						ConditionOperator.Equal,
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
						ConditionOperator.Equal,
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

					if (_targetServiceClient != null)
					{
						result.TargetSolutions =
							_targetServiceClient.RetrieveMultiple(
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

					SaveSettings(true);
					UpdateColumns();
					UpdateSolutionList();
					UpdateImportOptionsVisibility();
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
		private void Log(string message = "")
		{
			message += Environment.NewLine;

			if (textBox_log.InvokeRequired)
			{
				textBox_log.Invoke(new Action(() => textBox_log.Text += message));
				textBox_log.Invoke(new Action(() => textBox_log.SelectionStart = textBox_log.Text.Length));
				textBox_log.Invoke(new Action(() => textBox_log.ScrollToCaret()));
			}
			else
			{
				textBox_log.Text += message;
				textBox_log.SelectionStart = textBox_log.Text.Length;
				textBox_log.ScrollToCaret();
			}

		}

		// ============================================================================
		static string GetDurationString(
			DateTime startTime)
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

			if (minutes > 0 || hours > 0)
			{
				result += $"{minutes}min ";
			}

			if (seconds >= 1 || (hours == 0 && minutes == 0))
			{
				if (minutes > 0 || hours > 0)
				{
					result += $"{Math.Round(seconds)}sec";
				}
				else
				{
					result += $"{seconds:F1}sec";
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

			var managedUpToDate = true;
			var unmanagedUpToDate = true;


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
							exportedSolutionManaged.Version == solution.Version.ToString();
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
							exportedSolutionUnmanaged.Version == solution.Version.ToString();
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
					Enabled = _settings.ShowFriendlySolutionNames
				};

			var colLogicalName =
				new ColumnDefinition
				{
					Header = "Logical Name",
					PropertyName = "UniqueName",
					TooltipText = "The logical-name of the solution.",
					Width = (_settings.ShowFriendlySolutionNames ? "45%" : "100%"),
					Enabled = _settings.ShowLogicalSolutionNames,
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
					Enabled = _targetServiceClient != null,
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
				};

			listBoxSolutions.Columns =
				new List<ColumnDefinition>
				{
					colFriendlyName,		// 0
					colLogicalName,			// 1
					colVersion,				// 2
					colTargetVersionState,	// 3
					colFileStatusImage,		// 4
					colFileVersionState		// 5
				};

			listBoxSolutions.Refresh();

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



		#endregion

		// ##################################################
		// ##################################################

		#region Plugin Events

		// ============================================================================
		public BulkSolutionExporter_PluginControl()
		{
			InitializeComponent();

			textBox_log.Text = string.Empty;

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

				listBoxSolutions.ShowTooltips = _settings.ShowToolTips;

				_codeUpdate = false;
			}

			button_loadSolutions.Enabled = Service != null;

			LoadAllSolutions();
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
			SaveSettings(true);
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

				LoadAllSolutions();

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
				button_addAdditionalConnection.Text = "Target is " + detail.ConnectionName;

				LoadAllSolutions();
			}
		}


		// ============================================================================
		private void button_loadSolutions_Click(object sender, EventArgs e)
		{
			LoadAllSolutions();
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

			ExportButtonSetState();
			SaveSettings();
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

			ExportButtonSetState();
			SaveSettings();
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

			ExportButtonSetState();
			SaveSettings();
		}


		// ============================================================================
		private void flipSwitch_gitCommit_Toggled(object sender, EventArgs e)
		{
			_settings.GitCommit = flipSwitch_gitCommit.IsOn;
			flipSwitch_pushCommit.Enabled = flipSwitch_gitCommit.IsOn;

			UpdateGitOptionsVisibility();
			ExportButtonSetState();
			SaveSettings();
		}

		// ============================================================================
		private void flipSwitch_pushCommit_Toggled(object sender, EventArgs e)
		{
			_settings.PushCommit = flipSwitch_pushCommit.IsOn;
			ExportButtonSetState();
			SaveSettings();

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
			ExportButtonSetState();
			SaveSettings();
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
			ExportButtonSetState();
			SaveSettings();
		}


		// ============================================================================
		private void textBox_commitMessage_TextChanged(object sender, EventArgs e)
		{
			_settings.CommitMessage = textBox_commitMessage.Text;
			ExportButtonSetState();
			SaveSettings();
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
			SaveSettings();
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

		}


		// ============================================================================
		private void listBoxSolutions_ItemOrderChanged(object sender, EventArgs e)
		{
			SaveSettings();
		}


		// ============================================================================
		private void textBox_managed_TextChanged(object sender, EventArgs e)
		{
			if (listBoxSolutions.SelectedIndex == -1 ||
				_codeUpdate)
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
				_codeUpdate)
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
					UpdatecheckedVersionNumbers();
					ExportCheckedSolutions();
					HandleGit();
					ImportCheckedSolutions();
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


					SetUiEnabledState(true);
				}
			});
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

			flipSwitch_publishTarget.IsLocked = !state;
			flipSwitch_gitCommit.IsLocked = !state;
			flipSwitch_pushCommit.IsLocked = !state;

			button_browseManaged.Enabled = state;
			button_browseUnmananged.Enabled = state;

			textBox_versionFormat.Enabled = state;
			textBox_commitMessage.Enabled = state;
			textBox_managed.Enabled = state;
			textBox_unmanaged.Enabled = state;
		}


		// ============================================================================
		private void listSolutions_ItemCheck(object sender, EventArgs e)
		{
			ExportButtonSetState();
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
		private void button_sortAsc_Click(object sender, EventArgs e)
		{
			listBoxSolutions.SortAlphabetically(SortOrder.Ascending);
			SaveSettings();
		}

		// ============================================================================
		private void button_sortDesc_Click(object sender, EventArgs e)
		{
			listBoxSolutions.SortAlphabetically(SortOrder.Descending);
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