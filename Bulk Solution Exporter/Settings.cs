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

		private List<string> _solutionConfigurations;
		private Dictionary<string, SolutionConfiguration> _configCache;
		private Dictionary<string, int> _configIndexMap;



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

		public int RetryCount
		{
			get; set;
		} = 3;

		public int RetryDelayInSeconds
		{
			get; set;
		} = 10;

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

		public bool ContinueOnError
		{
			get; set;
		} = false;

		public bool AutoDisableExportButtons
		{
			get; set;
		} = false;

		public List<string> SolutionConfigurations
		{
			get { return _solutionConfigurations; }
			set
			{
				_solutionConfigurations = value;
				InvalidateCache();
			}
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
		private void InvalidateCache()
		{
			_configCache = null;
			_configIndexMap = null;
		}


		// ============================================================================
		private void EnsureCache()
		{
			if (_configCache != null)
			{
				return;
			}

			_configCache = new Dictionary<string, SolutionConfiguration>(StringComparer.Ordinal);
			_configIndexMap = new Dictionary<string, int>(StringComparer.Ordinal);

			for (int i = 0; i < _solutionConfigurations.Count; i++)
			{
				var config = SolutionConfiguration.GetConfigFromJson(_solutionConfigurations[i]);

				if (config == null)
				{
					continue;
				}

				_configCache[config.SolutionIndentifier] = config;
				_configIndexMap[config.SolutionIndentifier] = i;
			}
		}


		// ============================================================================
		private void SyncBackingListFromCache()
		{
			if (_configCache == null)
			{
				return;
			}

			_solutionConfigurations.Clear();
			_configIndexMap.Clear();

			foreach (var kvp in _configCache)
			{
				_configIndexMap[kvp.Key] = _solutionConfigurations.Count;
				_solutionConfigurations.Add(kvp.Value.GetJson());
			}
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

			EnsureCache();

			if (_configCache.TryGetValue(solutionIdentifier, out var config))
			{
				return config;
			}

			if (createIfNotFound)
			{
				var newSolutionConfig = new SolutionConfiguration(solutionIdentifier);
				_configCache[solutionIdentifier] = newSolutionConfig;
				_configIndexMap[solutionIdentifier] = _solutionConfigurations.Count;
				_solutionConfigurations.Add(newSolutionConfig.GetJson());
				return newSolutionConfig;
			}

			return null;
		}


		// ============================================================================
		public SolutionConfiguration AddSolutionConfiguration(
			string solutionIdentifier)
		{
			// does it already exist?
			var existingConfig = GetSolutionConfiguration(solutionIdentifier);

			if (existingConfig != null)
			{
				return existingConfig;
			}

			EnsureCache();

			var newSolutionConfig = new SolutionConfiguration(solutionIdentifier);
			_configCache[solutionIdentifier] = newSolutionConfig;
			_configIndexMap[solutionIdentifier] = _solutionConfigurations.Count;
			_solutionConfigurations.Add(newSolutionConfig.GetJson());

			return newSolutionConfig;
		}


		// ============================================================================
		public bool UpdateSolutionConfiguration(
			SolutionConfiguration config)
		{
			EnsureCache();

			if (!_configCache.ContainsKey(config.SolutionIndentifier))
			{
				return false;
			}

			_configCache[config.SolutionIndentifier] = config;
			_solutionConfigurations[_configIndexMap[config.SolutionIndentifier]] = config.GetJson();

			return true;
		}


		// ============================================================================
		public void RemoveSolutionConfiguration(
			string solutionIdentifier)
		{
			EnsureCache();

			if (!_configCache.ContainsKey(solutionIdentifier))
			{
				return;
			}

			_configCache.Remove(solutionIdentifier);
			_configIndexMap.Remove(solutionIdentifier);
			SyncBackingListFromCache();
		}


		// ============================================================================
		public List<SolutionConfiguration> GetAllSolutionConfigurationdForConnection(
			Guid connectionGuid)
		{
			EnsureCache();

			var resultList = new List<SolutionConfiguration>();
			var guidString = connectionGuid.ToString().ToLower();

			foreach (var config in _configCache.Values)
			{
				if (config.SolutionIndentifier.StartsWith(guidString))
				{
					resultList.Add(config);
				}
			}

			return resultList;
		}



	}
}