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
	/// Covers <see cref="ExportedSolutionVersions.TryLoadExportedSolution"/>
	/// against version files written into a per-test temp directory.
	/// </summary>
	[TestClass]
	public class ExportedSolutionVersionsTests
	{

		private const string SolutionUniqueName = "MySolution";
		private const string ExportFileName = "MySolution_managed.zip";
		private const string TruncatedJson = "{ \"Managed\": [ { \"LogicalSolutionName\": \"My";
		private const string NullJson = "null";

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
