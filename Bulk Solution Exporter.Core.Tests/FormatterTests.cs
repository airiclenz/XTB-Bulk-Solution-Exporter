using System;
using Com.AiricLenz.BulkSolutionExporter.Core.Run;
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
	/// Covers <see cref="Formatter.FormatErrorStringWithXml"/>: the size guard,
	/// the XML reformat, and the escape of the message on every return path.
	/// </summary>
	[TestClass]
	public class FormatterTests
	{

		private const string MissingDependenciesLeadIn =
			"Some dependencies are missing. The missing dependencies are : ";

		private const string MissingDependenciesXml =
			"<MissingDependencies><MissingDependency><Required type=\"1\" /></MissingDependency></MissingDependencies>";


		// ============================================================================
		[TestMethod]
		public void FormatErrorStringWithXml_OverLimitMessage_ReturnsItEscapedWithoutReformat()
		{
			// Arrange
			string errorString =
				"Some dependencies are missing. " +
				"<MissingDependencies><MissingDependency /></MissingDependencies>" +
				new string('x', Formatter.MaxFormattableErrorLength);

			// Act
			string formatted = Formatter.FormatErrorStringWithXml(errorString, "  ");

			// Assert
			Assert.AreEqual(LogMarkup.Escape(errorString), formatted);
		}


		// ============================================================================
		[TestMethod]
		[DataRow(@"Fake failure: **x** `y` # z \ <b>")]
		[DataRow("Fake failure: <MissingDependencies><Unclosed></MissingDependencies> **x**")]
		public void FormatErrorStringWithXml_NoParsableXml_ReturnsItEscaped(
			string errorString)
		{
			// Act
			string formatted = Formatter.FormatErrorStringWithXml(errorString, RunLog.IndentUnit);

			// Assert
			Assert.AreEqual(LogMarkup.Escape(errorString), formatted);
		}


		// ============================================================================
		[TestMethod]
		public void FormatErrorStringWithXml_MissingDependencies_EscapesTheXmlLinesAndKeepsTheIndentBar()
		{
			// Arrange
			string errorString = MissingDependenciesLeadIn + MissingDependenciesXml;

			// Act
			string formatted = Formatter.FormatErrorStringWithXml(errorString, RunLog.IndentUnit);

			// Assert
			CollectionAssert.AreEqual(
				new[]
				{
					"Some dependencies are missing.",
					RunLog.IndentUnit + "The missing dependencies are:",
					RunLog.IndentUnit + @" \<MissingDependencies>",
					@"  \<MissingDependency>",
					@"    \<Required type=""1"" />",
					@"  \</MissingDependency>",
					@"\</MissingDependencies>",
				},
				formatted.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None));
		}

	}
}
