using System;
using System.Drawing;
using Microsoft.Xrm.Sdk;
using Newtonsoft.Json;


// ============================================================================
// ============================================================================
// ============================================================================
namespace Com.AiricLenz.XTB.Plugin.Schema
{

	// ============================================================================
	// ============================================================================
	// ============================================================================
	internal class Solution : IComparable<Solution>
	{

		private Guid _connectionGuid;
		private string _friendlyName;
		private Version _version;
		private int _sortingIndex = 0;
		private Bitmap _fileStatusImage;
		private Bitmap _fileVersionState;
		private Bitmap _targetVersionState;


		// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		[JsonProperty("solution_id")]
		public Guid SolutionId
		{
			get; set;
		}


		// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		[JsonProperty("unique_name")]
		public string UniqueName
		{
			get; set;
		}


		// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		[JsonIgnore]
		public string FriendlyName
		{
			get
			{
				return _friendlyName;
			}
			set
			{
				_friendlyName = value;
			}
		}


		// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		[JsonProperty("version")]
		public Version Version
		{
			get
			{
				return _version;
			}
			set
			{
				_version = value;
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
		public Solution(
			Guid conectionGuid)
		{
			_connectionGuid = conectionGuid;
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
				(_version == null ? "" : _version.ToString()) +
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

			var solution = new Solution(connectionGuid);

			if (record.Contains("solutionid"))
			{
				solution.SolutionId = (Guid) record.Attributes["solutionid"];
			}

			if (record.Contains("uniquename"))
			{
				solution.UniqueName = (string) record.Attributes["uniquename"];
			}

			if (record.Contains("friendlyname"))
			{
				solution.FriendlyName = (string) record.Attributes["friendlyname"];
			}

			if (record.Contains("version"))
			{
				solution.Version = new Version((string) record.Attributes["version"]);
			}


			return solution;

		}


	}

}
