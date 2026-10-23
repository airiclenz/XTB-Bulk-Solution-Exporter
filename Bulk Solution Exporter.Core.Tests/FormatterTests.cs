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
	/// Covers the size guard of <see cref="Formatter.FormatErrorStringWithXml"/>.
	/// </summary>
	[TestClass]
	public class FormatterTests
	{

		// ============================================================================
		[TestMethod]
		public void FormatErrorStringWithXml_OverLimitMessage_ReturnsItUnchanged()
		{
			// Arrange
			string errorString =
				"Some dependencies are missing. " +
				"<MissingDependencies><MissingDependency /></MissingDependencies>" +
				new string('x', Formatter.MaxFormattableErrorLength);

			// Act
			string formatted = Formatter.FormatErrorStringWithXml(errorString, "  ");

			// Assert
			Assert.AreEqual(errorString, formatted);
		}

	}
}
