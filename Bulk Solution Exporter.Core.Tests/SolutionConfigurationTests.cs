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
	/// Pins the persisted JSON shape of <see cref="SolutionConfiguration"/>:
	/// the XTB settings store these strings, so the property names (including
	/// the <c>SolutionIndentifier</c> spelling) must not change.
	/// </summary>
	[TestClass]
	public class SolutionConfigurationTests
	{

		private const string SolutionIdentifier =
			"0f1e2d3c-4b5a-6978-8796-a5b4c3d2e1f0.MySolution";

		// Written by GetJson before the type moved to core.
		private const string PersistedJson =
			"{\"SolutionIndentifier\":\"" + SolutionIdentifier + "\"," +
			"\"FileNameManaged\":\"C:\\\\Exports\\\\MySolution_managed.zip\"," +
			"\"FileNameUnmanaged\":null," +
			"\"Checked\":true," +
			"\"SortingIndex\":3," +
			"\"LastExportDurationUnamangedInSeconds\":12," +
			"\"LastExportDurationManagedInSeconds\":34," +
			"\"LastImportDurationManagedInSeconds\":56," +
			"\"LastImportDurationUnmanagedInSeconds\":78}";


		// ============================================================================
		[TestMethod]
		public void GetJson_PersistedJson_RoundTripsByteIdentical()
		{
			// Arrange
			var configuration =
				SolutionConfiguration.GetConfigFromJson(
					PersistedJson);

			// Act
			var json = configuration.GetJson();

			// Assert
			Assert.AreEqual(PersistedJson, json);
		}


		// ============================================================================
		[TestMethod]
		public void GetConfigFromJson_PersistedJson_ReadsSolutionIdentifier()
		{
			// Arrange
			var json = PersistedJson;

			// Act
			var configuration = SolutionConfiguration.GetConfigFromJson(json);

			// Assert
			Assert.AreEqual(SolutionIdentifier, configuration.SolutionIndentifier);
		}

	}
}
