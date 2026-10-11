using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Com.AiricLenz.Extentions;
using Newtonsoft.Json;


// ============================================================================
// ============================================================================
// ============================================================================
namespace Com.AiricLenz.XTB.Plugin.Schema
{

	// ============================================================================
	// ============================================================================
	// ============================================================================
	internal class ExportedSolutionVersions
	{

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
		public static bool TryLoadExportedSolution(
			string exportFilePath,
			Solution solution,
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
					BulkSolutionExporter_Plugin.JsonSettingsFileName);

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
				FindMatch(exportedSolutions.Managed, solution.UniqueName, fileName) ??
				FindMatch(exportedSolutions.Unmanaged, solution.UniqueName, fileName);

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
	class ExportedSolution
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
