using Com.AiricLenz.XTB.Plugin.Schema;
using Com.AiricLenz.Extentions;
using Newtonsoft.Json;
using System;
using System.Activities.Expressions;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


// ============================================================================
// ============================================================================
// ============================================================================
namespace Com.AiricLenz.XTB.Plugin
{

    // ============================================================================
    // ============================================================================
    // ============================================================================
    /// <summary>
    /// This class can help you to store settings for your plugin
    /// </summary>
    /// <remarks>
    /// This class must be XML serializable
    /// </remarks>
    public class Settings
    {
        public string LastUsedOrganizationWebappUrl { get; set; }

        public bool ExportManaged { get; set; }
        public bool ExportUnmanaged { get; set; }
        public bool UpdateVersion { get; set; }
        public string VersionFormat { get; set; }
        public List<string> SolutionConfigurations { get; set; }



        // ============================================================================
        public Settings()
        {
            LastUsedOrganizationWebappUrl = string.Empty;
            ExportManaged = false;
            ExportUnmanaged = false;
            UpdateVersion = false;
            VersionFormat = "YYYY.MM.DD.+";

            SolutionConfigurations = new List<string>();
        }


        // ============================================================================
        public void SetSelectedStatus(
            string solutionIdentifier,
            bool selectedState)
        {
            for (int i = 0; i < SolutionConfigurations.Count; i++)
            {

                var config =
                    JsonConvert.DeserializeObject<SolutionConfiguration>(
                        SolutionConfigurations[i]);

                if (config == null)
                {
                    continue;
                }

                if (config.SolutionIndentifier == solutionIdentifier)
                {
                    config.Selected = selectedState;

                    SolutionConfigurations[i] =
                        JsonConvert.SerializeObject(config);

                    return;
                }
            }
        }


        // ============================================================================
        public SolutionConfiguration GetSolutionConfiguration(
            string solutionIdentifier,
            out bool isNew)
        {

            for (int i = 0; i<SolutionConfigurations.Count; i++)
            {

                var config = 
                    SolutionConfiguration.GetConfigFromJson(
                        SolutionConfigurations[i]);

                if (config == null)
                {
                    continue;
                }

                if (config.SolutionIndentifier == solutionIdentifier)
                {
                    isNew = false;
                    return config;
                }
            }

            var newConfig = new SolutionConfiguration(solutionIdentifier);
            SolutionConfigurations.Add(
                JsonConvert.SerializeObject(newConfig));

            isNew = true;
            return newConfig;
        }
               


        // ============================================================================
        public void AddSolutionConfiguration(
            SolutionConfiguration newConfig)
        {
            var found = false;

            for (int i = 0; i<SolutionConfigurations.Count; i++)
            {

                var config =
                    SolutionConfiguration.GetConfigFromJson(
                        SolutionConfigurations[i]);
                    

                if (config == null)
                {
                    continue;
                }

                if (config.SolutionIndentifier == newConfig.SolutionIndentifier)
                {
                    found = true;

                    SolutionConfigurations[i] = newConfig.GetJson();

                    break;
                }
            }

            if (!found)
            {
                SolutionConfigurations.Add(
                    JsonConvert.SerializeObject(newConfig));
            }

        }

    }
}