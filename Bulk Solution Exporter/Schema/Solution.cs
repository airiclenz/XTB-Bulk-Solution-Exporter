using System;
using System.Drawing;
using Microsoft.Xrm.Sdk;
using Newtonsoft.Json;
using SolutionInfo = Com.AiricLenz.BulkSolutionExporter.Core.Schema.SolutionInfo;
using Version = Com.AiricLenz.BulkSolutionExporter.Core.Version;


// ============================================================================
// ============================================================================
// ============================================================================
namespace Com.AiricLenz.XTB.Plugin.Schema
{

	// ============================================================================
	// ============================================================================
	// ============================================================================
	/// <summary>
	/// One solution row of the plugin's solution list: the host-neutral
	/// <see cref="SolutionInfo"/> plus the list's UI state (sorting index
	/// and status images).
	/// </summary>
	internal class Solution : IComparable<Solution>
	{

		private readonly SolutionInfo _info;
		private Guid _connectionGuid;
		private int _sortingIndex = 0;
		private Bitmap _fileStatusImage;
		private Bitmap _fileVersionState;
		private Bitmap _targetVersionState;


		// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		[JsonProperty("solution_id")]
		public Guid SolutionId
		{
			get
			{
				return _info.SolutionId;
			}
		}


		// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		[JsonProperty("unique_name")]
		public string UniqueName
		{
			get
			{
				return _info.UniqueName;
			}
		}


		// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		[JsonIgnore]
		public string FriendlyName
		{
			get
			{
				return _info.FriendlyName;
			}
		}


		// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		[JsonProperty("version")]
		public Version Version
		{
			get
			{
				return _info.Version;
			}
		}


		// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		[JsonIgnore]
		public string SolutionIdentifier
		{
			get
			{
				return _connectionGuid.ToString().ToLower() + "." + UniqueName;
			}
		}

		// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		/// <summary>
		/// The host-neutral solution data this row wraps.
		/// </summary>
		[JsonIgnore]
		public SolutionInfo Info
		{
			get
			{
				return _info;
			}
		}

		// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		[JsonIgnore]
		public int SortingIndex
		{
			get
			{
				return _sortingIndex;
			}
			set
			{
				_sortingIndex = value;
			}
		}

		// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		[JsonIgnore]
		public Bitmap FileStatusImage
		{
			get
			{
				return _fileStatusImage;
			}
			set
			{
				_fileStatusImage = value;
			}
		}

		// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		[JsonIgnore]
		public Bitmap FileVersionState
		{
			get
			{
				return _fileVersionState;
			}
			set
			{
				_fileVersionState = value;
			}
		}


		// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		[JsonIgnore]
		public Bitmap TargetVersionState
		{
			get
			{
				return _targetVersionState;
			}
			set
			{
				_targetVersionState = value;
			}
		}



		// ============================================================================
		/// <exception cref="ArgumentNullException"><paramref name="info"/> is null.</exception>
		public Solution(
			Guid conectionGuid,
			SolutionInfo info)
		{
			if (info == null)
			{
				throw new ArgumentNullException(nameof(info));
			}

			_connectionGuid = conectionGuid;
			_info = info;
		}


		// ============================================================================
		public int CompareTo(Solution other)
		{
			return _sortingIndex.CompareTo(other.SortingIndex);
		}


		// ============================================================================
		// Overriding the ToString method
		public override string ToString()
		{
			return
				FriendlyName +
				" - [" +
				(Version == null ? "" : Version.ToString()) +
				"]";
		}


		// ============================================================================
		public static Solution ConvertFrom(
			Entity record,
			Guid connectionGuid,
			int friendlyNameLength = 40)
		{


			if (record == null)
			{
				return null;
			}

			if (record.LogicalName != "solution")
			{
				return null;
			}

			var info = new SolutionInfo();

			if (record.Contains("solutionid"))
			{
				info.SolutionId = (Guid) record.Attributes["solutionid"];
			}

			if (record.Contains("uniquename"))
			{
				info.UniqueName = (string) record.Attributes["uniquename"];
			}

			if (record.Contains("friendlyname"))
			{
				info.FriendlyName = (string) record.Attributes["friendlyname"];
			}

			if (record.Contains("version"))
			{
				info.Version = new Version((string) record.Attributes["version"]);
			}

			// GetAttributeValue maps a missing or null value to the default,
			// so a record without these columns still converts.
			info.IsManaged = record.GetAttributeValue<bool>("ismanaged");
			info.Description = record.GetAttributeValue<string>("description");


			return new Solution(connectionGuid, info);

		}


	}

}
