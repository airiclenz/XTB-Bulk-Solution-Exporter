using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Markup;
using Com.AiricLenz.Extentions;
using Com.AiricLenz.XTB.Components;
using Microsoft.Xrm.Sdk;


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
		private Bitmap _versionState;

		// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		public Guid SolutionId
		{
			get; set;
		}

		// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		public string UniqueName
		{
			get; set;
		}

		// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
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
		public string SolutionIdentifier
		{
			get
			{
				return _connectionGuid.ToString().ToLower() + "." + UniqueName;
			}
		}

		// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
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
		public Bitmap VersionState
		{
			get
			{
				return _versionState;
			}
			set
			{
				_versionState = value;
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
