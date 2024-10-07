using Com.AiricLenz.XTB.Plugin.Schema;
using McTools.Xrm.Connection;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Web.Services.Description;
using System.Windows.Forms;
using XrmToolBox.Extensibility;

// ============================================================================
// ============================================================================
// ============================================================================
namespace Com.AiricLenz.XTB.Plugin
{

    // ============================================================================
    // ============================================================================
    // ============================================================================
    public partial class BulkSolutionExporter_PluginControl : PluginControlBase
    {
        private Settings _settings;
        private bool _isLoad = true;
        private Guid _currentConnectionGuid;
        private SolutionConfiguration _currentSolutionConfig;

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
        private void ExportAllSolutions()
        {
            textBox_log.Text = string.Empty;

            WorkAsync(new WorkAsyncInfo
            {
                Message = "Exporting Solutions",
                Work = (worker, args) =>
                {
                    int progress = 0;
                    worker.WorkerReportsProgress = true;

                    for (int i = 0; i<listSolutions.CheckedItems.Count; i++)
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
                        worker.ReportProgress(progress, "Exporting solution " + solution.FriendlyName);

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

                        progress = (int)(i / (float)listSolutions.CheckedItems.Count * 100f);
                        worker.ReportProgress(progress);

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
                    Log("");
                    Log("Done.");
                }
            });

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

            if (flipSwitch_managed.IsOn)
            {
                ExportToFile(
                    solution,
                    solutionConfiguration,
                    true);
            }

            if (flipSwitch_unmanaged.IsOn)
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
                (ExportSolutionResponse)Service.Execute(
                    exportSolutionRequest);

            var filePath =
                isManaged ? solutionConfiguration.FileNameManaged : solutionConfiguration.FileNameUnmanaged;

            // Save the exported solution to a file
            File.WriteAllBytes(
                filePath,
                exportSolutionResponse.ExportSolutionFile);

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

                        //MessageBox.Show($"Found {result.Entities.Count} solutions");

                        foreach (var entity in result.Entities)
                        {
                            _solutions.Add(
                                Solution.ConvertFrom(entity, _currentConnectionGuid));

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
                    flipSwitch_managed.IsOn ||
                    flipSwitch_unmanaged.IsOn ||
                    flipSwitch_updateVersion.IsOn
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

                for (int i = 0; i<listSolutions.Items.Count; i++)
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
            if (!_isLoad)
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
                _isLoad = false;

                SaveSettings();

                LogWarning("Settings not found => a new settings file has been created!");
            }
            else
            {
                LogInfo("Settings found and loaded");

                flipSwitch_managed.IsOn = _settings.ExportManaged;
                flipSwitch_unmanaged.IsOn = _settings.ExportUnmanaged;
                flipSwitch_updateVersion.IsOn = _settings.UpdateVersion;

                textBox_versionFormat.Text = _settings.VersionFormat;

                _isLoad = false;
            }

            button_loadSolutions.Enabled = Service != null;
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


        // ============================================================================
        private void button_loadSolutions_Click(object sender, EventArgs e)
        {
            LoadAllSolutions();
            ExportButtonSetState();
        }

        // ============================================================================
        private void checkBox_managed_CheckedChanged(object sender, EventArgs e)
        {
            _settings.ExportManaged = flipSwitch_managed.IsOn;

            SaveSettings();
            ExportButtonSetState();
        }

        // ============================================================================
        private void checkBox_unmanaged_CheckedChanged(object sender, EventArgs e)
        {
            _settings.ExportUnmanaged = flipSwitch_managed.IsOn;

            SaveSettings();
            ExportButtonSetState();
        }

        // ============================================================================
        private void checkBox_updateVersion_CheckedChanged(object sender, EventArgs e)
        {
            _settings.UpdateVersion = flipSwitch_updateVersion.IsOn;
            textBox_versionFormat.Enabled = flipSwitch_updateVersion.IsOn;

            textBox_versionFormat.Visible = flipSwitch_updateVersion.IsOn;
            label_versionFormat.Visible = flipSwitch_updateVersion.IsOn;

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
            for (int i = 0; i<_settings.SolutionConfigurations.Count; i++)
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
            ExportAllSolutions();
        }


        // ============================================================================
        private void listSolutions_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            ExportButtonSetState();
        }


        #endregion




    }
}