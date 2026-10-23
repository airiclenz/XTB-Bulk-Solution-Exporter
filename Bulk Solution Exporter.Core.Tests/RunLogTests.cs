using System.Collections.Generic;
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
	/// Pins the markup strings <see cref="RunLog"/> hands to its sink: every
	/// log view parses exactly these.
	/// </summary>
	[TestClass]
	public class RunLogTests
	{
		private const string IndentUnit = "<color=#DDDDDD>|</color>   ";
		private const string TruncationMarker = " … [truncated]";
		private const int MaxLineLength = 10000;

		private List<string> _lines;


		// ============================================================================
		[TestInitialize]
		public void Initialize()
		{
			_lines = new List<string>();
		}


		// ============================================================================
		[TestMethod]
		public void Log_Heading_SinksHeadingMarkup()
		{
			// Arrange
			var runLog = CreateRunLog(isDebugEnabled: false);

			// Act
			runLog.Log(RunLog.Heading(5, "Exporting:"));

			// Assert
			CollectionAssert.AreEqual(new[] { "##### Exporting:" }, _lines);
		}


		// ============================================================================
		[TestMethod]
		public void Log_InlineMarkup_SinksBoldItalicCodeAndColor()
		{
			// Arrange
			var runLog = CreateRunLog(isDebugEnabled: false);

			// Act
			runLog.Log(
				RunLog.Bold("b") + " " +
				RunLog.Italic("i") + " " +
				RunLog.Code("c") + " " +
				RunLog.Colorize(RunLog.ColorGreen, "g"));

			// Assert
			CollectionAssert.AreEqual(
				new[] { "**b** *i* `c` <color=#227700>g</color>" },
				_lines);
		}


		// ============================================================================
		[TestMethod]
		public void LogError_Message_SinksMessageInErrorColor()
		{
			// Arrange
			var runLog = CreateRunLog(isDebugEnabled: false);

			// Act
			runLog.LogError("Import failed.");

			// Assert
			CollectionAssert.AreEqual(
				new[] { "<color=#770000>Import failed.</color>" },
				_lines);
		}


		// ============================================================================
		[TestMethod]
		public void LogDebug_DebugEnabled_SinksMessage()
		{
			// Arrange
			var runLog = CreateRunLog(isDebugEnabled: true);

			// Act
			runLog.LogDebug("debug detail");

			// Assert
			CollectionAssert.AreEqual(new[] { "debug detail" }, _lines);
		}


		// ============================================================================
		[TestMethod]
		public void LogDebug_DebugDisabled_SinksNothing()
		{
			// Arrange
			var runLog = CreateRunLog(isDebugEnabled: false);

			// Act
			runLog.LogDebug("debug detail");

			// Assert
			Assert.AreEqual(0, _lines.Count);
		}


		// ============================================================================
		[TestMethod]
		public void Log_IndentedTwoLevels_SinksLineWithTwoIndentUnits()
		{
			// Arrange
			var runLog = CreateRunLog(isDebugEnabled: false);
			runLog.IncreaseIndent();
			runLog.IncreaseIndent();

			// Act
			runLog.Log("Solution exported.");

			// Assert
			CollectionAssert.AreEqual(
				new[] { IndentUnit + IndentUnit + "Solution exported." },
				_lines);
		}


		// ============================================================================
		[TestMethod]
		public void Log_EmptyMessageWhileIndented_SinksEmptyLine()
		{
			// Arrange
			var runLog = CreateRunLog(isDebugEnabled: false);
			runLog.IncreaseIndent();

			// Act
			runLog.Log();

			// Assert
			CollectionAssert.AreEqual(new[] { string.Empty }, _lines);
		}


		// ============================================================================
		[TestMethod]
		public void DecreaseIndent_AtLevelZero_StaysAtZero()
		{
			// Arrange
			var runLog = CreateRunLog(isDebugEnabled: false);

			// Act
			int level = runLog.DecreaseIndent();
			runLog.Log("top");

			// Assert
			Assert.AreEqual(0, level);
			CollectionAssert.AreEqual(new[] { "top" }, _lines);
		}


		// ============================================================================
		[TestMethod]
		public void Log_MultiLineMessageLongerThanCapButEachLineShorter_IsNotCut()
		{
			// Arrange
			var runLog = CreateRunLog(isDebugEnabled: false);
			string line = new string('x', MaxLineLength - 1);
			string message = line + "\r\n" + line + "\n" + line;

			// Act
			runLog.Log(message);

			// Assert
			CollectionAssert.AreEqual(new[] { message }, _lines);
		}


		// ============================================================================
		[TestMethod]
		public void Log_OneLineLongerThanCap_CutsOnlyThatLine()
		{
			// Arrange
			var runLog = CreateRunLog(isDebugEnabled: false);
			string shortLine = "short";
			string longLine = new string('y', MaxLineLength + 1);

			// Act
			runLog.Log(shortLine + "\n" + longLine + "\n" + shortLine);

			// Assert
			string expected =
				shortLine + "\n" +
				new string('y', MaxLineLength) + TruncationMarker + "\n" +
				shortLine;

			CollectionAssert.AreEqual(new[] { expected }, _lines);
		}


		// ============================================================================
		[TestMethod]
		public void LogError_LineLongerThanCap_ClosesColorBeforeMarker()
		{
			// Arrange
			var runLog = CreateRunLog(isDebugEnabled: false);
			string longText = new string('z', MaxLineLength);

			// Act
			runLog.LogError(longText);

			// Assert
			string expected =
				"<color=#770000>" +
				new string('z', MaxLineLength - "<color=#770000>".Length) +
				"</color>" +
				TruncationMarker;

			CollectionAssert.AreEqual(new[] { expected }, _lines);
		}


		// ============================================================================
		private RunLog CreateRunLog(bool isDebugEnabled)
		{
			return new RunLog(_lines.Add, isDebugEnabled);
		}

	}
}
