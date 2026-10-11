using Com.AiricLenz.XTB.Plugin.Schema;
using Microsoft.VisualStudio.TestTools.UnitTesting;


// ============================================================================
// ============================================================================
// ============================================================================
namespace Com.AiricLenz.XTB.Plugin.Tests
{

	// ============================================================================
	// ============================================================================
	// ============================================================================
	/// <summary>
	/// Covers <see cref="Version.IncreaseVersionNumber"/> for every format
	/// length <see cref="Version.ValidateFormat"/> accepts.
	/// </summary>
	[TestClass]
	public class VersionTests
	{

		// ============================================================================
		[TestMethod]
		[DataRow("#.#.+", "1.2.3.4", "1.2.4")]
		[DataRow("#.+", "1.2.3.4", "1.3")]
		[DataRow("#.#.#.+", "1.0", "1.0.0.1")]
		[DataRow("#.#.#.+", "1.2.3.4", "1.2.3.5")]
		[DataRow("#.#", "1", "1.0")]
		public void IncreaseVersionNumber_Format_ProducesOnePartPerFormatPart(
			string format,
			string sourceVersion,
			string expectedVersion)
		{
			// Arrange
			var version = new Version(sourceVersion);

			// Act
			version.IncreaseVersionNumber(format);

			// Assert
			Assert.AreEqual(expectedVersion, version.ToString());
		}


		// ============================================================================
		[TestMethod]
		[DataRow("#.#.+")]
		[DataRow("#.+")]
		public void IncreaseVersionNumber_ShortFormat_ReturnsTrue(
			string format)
		{
			// Arrange
			var version = new Version("1.2.3.4");

			// Act
			var isIncreased = version.IncreaseVersionNumber(format);

			// Assert
			Assert.IsTrue(isIncreased);
		}

	}
}
