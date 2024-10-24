using System;
using System.Activities.Expressions;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Com.AiricLenz.Extentions;
using Com.AiricLenz.XTB.Plugin.Schema;
using Newtonsoft.Json;


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
		public string LastUsedOrganizationWebappUrl
		{
			get; set;
		}

		public bool PublishAllPreExport
		{
			get; set;
		}

		public bool PublishAllPostImport
		{
			get; set;
		}

		public bool UpdateVersion
		{
			get; set;
		}

		public bool ExportManaged
		{
			get; set;
		}

		public bool ExportUnmanaged
		{
			get; set;
		}

		public bool GitCommit
		{
			get; set;
		}

		public bool PushCommit
		{
			get; set;
		}

		public bool ImportManaged
		{
			get; set;
		}

		public bool ImportUnmanaged
		{
			get; set;
		}

		public string CommitMessage
		{
			get; set;
		}

		public string VersionFormat
		{
			get; set;
		}

		public bool EnableAutomation
		{
			get; set;
		}

		public bool OverwriteCustomizations
		{
			get; set;
		}



		public List<string> SolutionConfigurations
		{
			get; set;
		}



		// ============================================================================
		public Settings()
		{
			LastUsedOrganizationWebappUrl = string.Empty;

			PublishAllPreExport = false;
			UpdateVersion = false;
			ExportManaged = false;
			ExportUnmanaged = false;
			GitCommit = false;
			PushCommit = false;
			ImportManaged = false;
			ImportUnmanaged = false;

			VersionFormat = "YYYY.MM.DD.+";
			CommitMessage = "XTB Commit - YYYY.MM.DD";
			EnableAutomation = true;
			OverwriteCustomizations = false;

			SolutionConfigurations = new List<string>();
		}


		// ============================================================================
		public void SetCheckedStatus(
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
		public void SetSortingIndex(
			string solutionIdentifier,
			int index)
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
					config.SortingIndex = index;

					SolutionConfigurations[i] =
						JsonConvert.SerializeObject(config);

					return;
				}
			}
		}


		// ============================================================================
		public SolutionConfiguration GetOrCreateSolutionConfiguration(
			string solutionIdentifier,
			out bool isNew)
		{

			for (int i = 0; i < SolutionConfigurations.Count; i++)
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

			for (int i = 0; i < SolutionConfigurations.Count; i++)
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