using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

			string json = File.ReadAllText(jsonFilePath);
			var exportedSolutions =
				JsonConvert.DeserializeObject<ExportedSolutionVersions>(json);

			foreach (var exporedSolution in exportedSolutions.Managed)
			{
				if (exporedSolution.LogicalSolutionName == solution.UniqueName &&
					exporedSolution.FileName == fileName)
				{
					exportedSolutionResult = exporedSolution;
					return true;
				}
			}

			foreach (var exporedSolution in exportedSolutions.Unmanaged)
			{
				if (exporedSolution.LogicalSolutionName.ToLower() == solution.UniqueName &&
					exporedSolution.FileName == fileName)
				{
					exportedSolutionResult = exporedSolution;
					return true;
				}
			}

			exportedSolutionResult = null;
			return false;
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


	}
}
