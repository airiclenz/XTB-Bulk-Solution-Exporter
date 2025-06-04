using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Com.AiricLenz.XTB.Plugin.Schema;


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

		private int _connectionTimeoutInMinutes = 120;



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

		public bool Upgrade
		{
			get; set;
		} = true;

		public string CommitMessage
		{
			get; set;
		}

		public string VersionFormat
		{
			get; set;
		} = "YYYY.MM.DD.+";

		public bool EnableAutomation
		{
			get; set;
		} = true;

		public bool OverwriteCustomizations
		{
			get; set;
		} = false;

		public bool SaveVersionJson
		{
			get; set;
		} = true;

		public bool ShowFriendlySolutionNames
		{
			get; set;
		}

		public bool ShowLogicalSolutionNames
		{
			get; set;
		}

		public bool ShowToolTips
		{
			get; set;
		} = true;

		public int SortingColumnIndex
		{
			get; set;
		}

		public SortOrder SortingColumnOrder
		{
			get; set;
		}

		public int SplitContainerPosition
		{
			get; set;
		} = 500;

		public int ConnectionTimeoutInMinutes
		{
			get
			{
				return _connectionTimeoutInMinutes;
			}
			set
			{
				if (_connectionTimeoutInMinutes < 2)
				{
					_connectionTimeoutInMinutes = 2;
				}
				else if (_connectionTimeoutInMinutes > 600)
				{
					_connectionTimeoutInMinutes = 600;
				}
				else
				{
					_connectionTimeoutInMinutes = value;
				}
			}
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
			SaveVersionJson = false;

			SolutionConfigurations = new List<string>();

			ShowToolTips = true;
			ShowFriendlySolutionNames = true;
			ShowLogicalSolutionNames = false;
		}


		// ============================================================================
		public void SetCheckedStatus(
			string solutionIdentifier,
			bool selectedState)
		{
			var config = GetSolutionConfiguration(solutionIdentifier);
			if (config == null)
			{
				return;
			}

			config.Checked = selectedState;
			UpdateSolutionConfiguration(config);
		}


		// ============================================================================
		public void SetSortingIndex(
			string solutionIdentifier,
			int index)
		{
			var config = GetSolutionConfiguration(solutionIdentifier);
			if (config == null)
			{
				return;
			}

			config.SortingIndex = index;
			UpdateSolutionConfiguration(config);
		}


		// ============================================================================
		public SolutionConfiguration GetSolutionConfiguration(
			string solutionIdentifier,
			bool createIfNotFound = false)
		{

			if (string.IsNullOrWhiteSpace(solutionIdentifier))
			{
				return null;
			}

			for (int i = 0; i < SolutionConfigurations.Count; i++)
			{
				var config =
					SolutionConfiguration.GetConfigFromJson(
						SolutionConfigurations[i]);

				if (config == null)
				{
					SolutionConfigurations.RemoveAt(i);
					continue;
				}

				if (config.SolutionIndentifier == solutionIdentifier)
				{
					return config;
				}
			}

			if (createIfNotFound)
			{
				var newSolutionConfig =
					new SolutionConfiguration(
						solutionIdentifier);

				SolutionConfigurations.Add(
					newSolutionConfig.GetJson());

				return newSolutionConfig;
			}

			return null;
		}


		// ============================================================================
		public SolutionConfiguration AddSolutionConfiguration(
			string solutionIdentifier)
		{
			// does it already exist?
			var existingConfig =
				GetSolutionConfiguration(solutionIdentifier);

			if (existingConfig != null)
			{
				return existingConfig;
			}

			var newSolutionConfig =
				new SolutionConfiguration(
					solutionIdentifier);

			SolutionConfigurations.Add(
				newSolutionConfig.GetJson());

			return newSolutionConfig;
		}


		// ============================================================================
		public bool UpdateSolutionConfiguration(
			SolutionConfiguration config)
		{
			for (int i = 0; i < SolutionConfigurations.Count; i++)
			{
				var currentConfig =
					SolutionConfiguration.GetConfigFromJson(
						SolutionConfigurations[i]);

				if (config.SolutionIndentifier == currentConfig.SolutionIndentifier)
				{
					SolutionConfigurations[i] = config.GetJson();
					return true;
				}
			}

			return false;
		}



		// ============================================================================
		public void RemoveSolutionConfiguration(
			string solutionIdentifier)
		{
			for (int i = 0; i < SolutionConfigurations.Count; i++)
			{
				var currentConfig =
					SolutionConfiguration.GetConfigFromJson(
						SolutionConfigurations[i]);

				if (currentConfig.SolutionIndentifier == solutionIdentifier)
				{
					SolutionConfigurations.RemoveAt(i);
					return;
				}
			}
		}


		// ============================================================================
		public List<SolutionConfiguration> GetAllSolutionConfigurationdForConnection(
			Guid connectionGuid)
		{
			var resultList =
				new List<SolutionConfiguration>();

			for (int i = 0; i < SolutionConfigurations.Count; i++)
			{
				var currentConfig =
					SolutionConfiguration.GetConfigFromJson(
						SolutionConfigurations[i]);

				var guidString =
					connectionGuid.ToString().ToLower();

				if (currentConfig.SolutionIndentifier.StartsWith(guidString))
				{
					resultList.Add(currentConfig);
				}
			}

			return resultList;
		}



	}
}