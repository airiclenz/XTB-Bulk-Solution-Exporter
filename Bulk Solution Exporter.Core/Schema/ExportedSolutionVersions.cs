using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Com.AiricLenz.BulkSolutionExporter.Core.Extensions;
using Newtonsoft.Json;


// ============================================================================
// ============================================================================
// ============================================================================
namespace Com.AiricLenz.BulkSolutionExporter.Core.Schema
{

	// ============================================================================
	// ============================================================================
	// ============================================================================
	/// <summary>
	/// The content of the version file that sits next to the exported
	/// solution files and records which solution version each export file holds.
	/// </summary>
	public class ExportedSolutionVersions
	{

		// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		/// <summary>
		/// The name of the version file inside the export folder.
		/// </summary>
		public const string FileName = "exported_versions.json";

		// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		public List<ExportedSolution> Managed
		{
			get; set;
		} = new List<ExportedSolution>();


		// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		public List<ExportedSolution> Unmanaged
		{
			get; set;
		} = new List<ExportedSolution>();


		// ============================================================================
		/// <summary>
		/// Looks up the version-file entry for one export file.
		/// </summary>
		/// <param name="exportFilePath">The full path of the exported solution file;
		/// the version file is read from the same folder.</param>
		/// <param name="uniqueName">The solution's unique name, matched case-insensitively.</param>
		/// <param name="exportedSolutionResult">The matching entry, or <c>null</c>.</param>
		/// <returns><c>true</c> when the version file exists, is readable and holds a
		/// matching entry; otherwise <c>false</c>.</returns>
		public static bool TryLoadExportedSolution(
			string exportFilePath,
			string uniqueName,
			out ExportedSolution exportedSolutionResult)
		{
			if (string.IsNullOrWhiteSpace(exportFilePath))
			{
				exportedSolutionResult = null;
				return false;
			}

			var exportParth =
				Path.GetDirectoryName(
					exportFilePath);

			var fileName =
				Path.GetFileName(
					exportFilePath);

			var jsonFilePath =
				Path.Combine(
					exportParth,
					FileName);

			if (!File.Exists(jsonFilePath))
			{
				exportedSolutionResult = null;
				return false;
			}

			ExportedSolutionVersions exportedSolutions;

			try
			{
				string json = File.ReadAllText(jsonFilePath);
				exportedSolutions =
					JsonConvert.DeserializeObject<ExportedSolutionVersions>(json);
			}
			catch (Exception ex) when (
				ex is IOException ||
				ex is UnauthorizedAccessException ||
				ex is JsonException)
			{
				// An unreadable or malformed version file is reported as
				// "no exported version known" - the caller treats the
				// solution as outdated instead of failing the UI.
				exportedSolutionResult = null;
				return false;
			}

			if (exportedSolutions == null)
			{
				exportedSolutionResult = null;
				return false;
			}

			exportedSolutionResult =
				FindMatch(exportedSolutions.Managed, uniqueName, fileName) ??
				FindMatch(exportedSolutions.Unmanaged, uniqueName, fileName);

			return exportedSolutionResult != null;
		}


		// ============================================================================
		private static ExportedSolution FindMatch(
			List<ExportedSolution> exportedSolutions,
			string logicalSolutionName,
			string fileName)
		{
			if (exportedSolutions == null)
			{
				return null;
			}

			foreach (var exportedSolution in exportedSolutions)
			{
				if (exportedSolution != null &&
					exportedSolution.Matches(logicalSolutionName, fileName))
				{
					return exportedSolution;
				}
			}

			return null;
		}

	}



	// ============================================================================
	// ============================================================================
	// ============================================================================
	/// <summary>
	/// One entry of the version file: the version of one solution in one export file.
	/// </summary>
	public class ExportedSolution
	{
		// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		public string LogicalSolutionName
		{
			get; set;
		}

		// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		public string Version
		{
			get; set;
		}

		// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		public string FileName
		{
			get; set;
		}

		// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		public string SolutionId
		{
			get; set;
		}


		// ============================================================================
		/// <summary>
		/// The one identity rule shared by the version-file reader and writer:
		/// the logical solution name matches case-insensitively, the file name
		/// exactly.
		/// </summary>
		public bool Matches(
			string logicalSolutionName,
			string fileName)
		{
			return
				!LogicalSolutionName.IsEmpty() &&
				string.Equals(
					LogicalSolutionName,
					logicalSolutionName,
					StringComparison.OrdinalIgnoreCase) &&
				FileName == fileName;
		}


	}
}
