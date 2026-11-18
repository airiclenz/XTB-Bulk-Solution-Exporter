using System.IO;
using Com.AiricLenz.BulkSolutionExporter.Core.Schema;
using Microsoft.VisualStudio.TestTools.UnitTesting;


// ============================================================================
// ============================================================================
// ============================================================================
namespace Com.AiricLenz.BulkSolutionExporter.Core.Tests
{

	// ============================================================================
	// ============================================================================
	// ============================================================================
	/// <summary>
	/// Covers <see cref="ExportedSolutionVersions.TryParse"/> on version-file
	/// text and <see cref="ExportedSolutionVersions.TryLoadExportedSolution"/>
	/// against version files written into a per-test temp directory.
	/// </summary>
	[TestClass]
	public class ExportedSolutionVersionsTests
	{

		private const string SolutionUniqueName = "MySolution";
		private const string ExportFileName = "MySolution_managed.zip";
		private const string TruncatedJson = "{ \"Managed\": [ { \"LogicalSolutionName\": \"My";
		private const string NullJson = "null";
		private const string NullListJson = "{ \"Managed\": null }";
		private const string NullEntryJson = "{ \"Managed\": [ null ] }";

		private string _tempDirectory;


		// ============================================================================
		[TestInitialize]
		public void CreateTempDirectory()
		{
			_tempDirectory =
				Path.Combine(
					Path.GetTempPath(),
					Path.GetRandomFileName());

			Directory.CreateDirectory(_tempDirectory);
		}


		// ============================================================================
		[TestCleanup]
		public void DeleteTempDirectory()
		{
			if (Directory.Exists(_tempDirectory))
			{
				Directory.Delete(_tempDirectory, true);
			}
		}


		// ============================================================================
		[TestMethod]
		[DataRow(TruncatedJson)]
		[DataRow(NullJson)]
		public void TryLoadExportedSolution_InvalidJson_ReturnsFalse(
			string json)
		{
			// Arrange
			WriteVersionFile(json);

			// Act
			var isLoaded =
				ExportedSolutionVersions.TryLoadExportedSolution(
					ExportFilePath(),
					SolutionUniqueName,
					out var exportedSolution);

			// Assert
			Assert.IsFalse(isLoaded);
			Assert.IsNull(exportedSolution);
		}


		// ============================================================================
		[TestMethod]
		[DataRow(TruncatedJson)]
		[DataRow(NullJson)]
		public void TryParse_UnreadableJson_ReturnsFalseAndAnEmptyRecord(
			string json)
		{
			// Act
			var isParsed =
				ExportedSolutionVersions.TryParse(
					json,
					out var exportedSolutions,
					out _);

			// Assert
			Assert.IsFalse(isParsed);
			Assert.AreEqual(0, exportedSolutions.Managed.Count + exportedSolutions.Unmanaged.Count);
		}


		// ============================================================================
		[TestMethod]
		[DataRow(NullListJson)]
		[DataRow(NullEntryJson)]
		public void TryParse_NullListOrEntry_ReturnsTrueRepairedWithEmptyLists(
			string json)
		{
			// Act
			var isParsed =
				ExportedSolutionVersions.TryParse(
					json,
					out var exportedSolutions,
					out var isRepaired);

			// Assert
			Assert.IsTrue(isParsed && isRepaired);
			Assert.AreEqual(0, exportedSolutions.Managed.Count + exportedSolutions.Unmanaged.Count);
		}


		// ============================================================================
		[TestMethod]
		public void TryParse_NullEntryBesideReadableEntry_KeepsTheReadableEntry()
		{
			// Act
			ExportedSolutionVersions.TryParse(
				"{ \"Managed\": [ null, " + EntryJson(SolutionUniqueName) + " ] }",
				out var exportedSolutions,
				out _);

			// Assert
			Assert.AreEqual(1, exportedSolutions.Managed.Count);
			Assert.AreEqual(SolutionUniqueName, exportedSolutions.Managed[0].LogicalSolutionName);
		}


		// ============================================================================
		[TestMethod]
		public void TryParse_WellFormedVersionFile_ReturnsTrueNotRepaired()
		{
			// Act
			var isParsed =
				ExportedSolutionVersions.TryParse(
					"{ \"Managed\": [ " + EntryJson(SolutionUniqueName) + " ], \"Unmanaged\": [] }",
					out _,
					out var isRepaired);

			// Assert
			Assert.IsTrue(isParsed);
			Assert.IsFalse(isRepaired);
		}


		// ============================================================================
		[TestMethod]
		[DataRow(NullListJson)]
		[DataRow(NullEntryJson)]
		public void TryLoadExportedSolution_NullListOrEntry_ReturnsFalseWithoutThrowing(
			string json)
		{
			// Arrange
			WriteVersionFile(json);

			// Act
			var isLoaded =
				ExportedSolutionVersions.TryLoadExportedSolution(
					ExportFilePath(),
					SolutionUniqueName,
					out _);

			// Assert
			Assert.IsFalse(isLoaded);
		}


		// ============================================================================
		[TestMethod]
		public void TryLoadExportedSolution_MixedCaseUnmanagedEntry_ReturnsTrue()
		{
			// Arrange
			WriteVersionFile(
				"{ \"Managed\": [], \"Unmanaged\": [ " +
				EntryJson(SolutionUniqueName) +
				" ] }");

			// Act
			var isLoaded =
				ExportedSolutionVersions.TryLoadExportedSolution(
					ExportFilePath(),
					SolutionUniqueName,
					out var exportedSolution);

			// Assert
			Assert.IsTrue(isLoaded);
			Assert.AreEqual(SolutionUniqueName, exportedSolution.LogicalSolutionName);
		}


		// ============================================================================
		[TestMethod]
		public void TryLoadExportedSolution_ManagedEntryInOtherCase_ReturnsTrue()
		{
			// Arrange
			var lowerCaseName = SolutionUniqueName.ToLowerInvariant();

			WriteVersionFile(
				"{ \"Managed\": [ " +
				EntryJson(lowerCaseName) +
				" ], \"Unmanaged\": [] }");

			// Act
			var isLoaded =
				ExportedSolutionVersions.TryLoadExportedSolution(
					ExportFilePath(),
					SolutionUniqueName,
					out var exportedSolution);

			// Assert
			Assert.IsTrue(isLoaded);
			Assert.AreEqual(lowerCaseName, exportedSolution.LogicalSolutionName);
		}


		// ============================================================================
		private void WriteVersionFile(
			string json)
		{
			File.WriteAllText(
				Path.Combine(
					_tempDirectory,
					ExportedSolutionVersions.FileName),
				json);
		}


		// ============================================================================
		private string ExportFilePath()
		{
			return
				Path.Combine(
					_tempDirectory,
					ExportFileName);
		}


		// ============================================================================
		private static string EntryJson(
			string logicalSolutionName)
		{
			return
				"{ \"LogicalSolutionName\": \"" + logicalSolutionName +
				"\", \"Version\": \"1.0.0.0\", \"FileName\": \"" + ExportFileName +
				"\", \"SolutionId\": \"00000000-0000-0000-0000-000000000000\" }";
		}

	}
}
