using System;
using System.Collections.Generic;
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
		private string _toStringValue;
		private string _friendlyName;
		private Version _version;
		private int _sortingIndex = 0;

		private static int _friendlyNameLength = 40;


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
				UpdateToStringValue();
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
				UpdateToStringValue();
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
			return _toStringValue;
		}


		// ============================================================================
		public static Solution ConvertFrom(
			Entity record,
			Guid connectionGuid,
			int friendlyNameLength = 40)
		{
			_friendlyNameLength = friendlyNameLength;

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


		// ============================================================================
		private void UpdateToStringValue()
		{

			_toStringValue =
				FriendlyName.ForceToLength(_friendlyNameLength) +
				" - [" +
				(_version == null ? "" : _version.ToString()).ForceToLength(13) +
				"]";
		}
	}

}
