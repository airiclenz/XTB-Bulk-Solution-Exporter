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
	/// Pins the shared log markup rules: tag-safe line truncation and the
	/// backslash escape.
	/// </summary>
	[TestClass]
	public class LogMarkupTests
	{
		private const string TruncationMarker = " … [truncated]";
		private const int MaxLineLength = 10000;
		private const string ColorToken = "<color=#123456>";
		private const string ColorEndTag = "</color>";

		// How far before the cut a tag starts when a test places it across the cut.
		private const int CharactersBeforeCut = 5;


		// ============================================================================
		[TestMethod]
		public void TruncateLine_LineWithinCap_ReturnsLineUnchanged()
		{
			// Arrange
			string line = new string('x', MaxLineLength) + "   ";

			// Act
			string result = LogMarkup.TruncateLine(line);

			// Assert
			Assert.AreEqual(line, result);
		}


		// ============================================================================
		[TestMethod]
		public void TruncateLine_CutInsideColorToken_CutsBeforeToken()
		{
			// Arrange
			string head = new string('x', MaxLineLength - CharactersBeforeCut);
			string line = head + ColorToken + "colored" + ColorEndTag;

			// Act
			string result = LogMarkup.TruncateLine(line);

			// Assert
			Assert.AreEqual(head + TruncationMarker, result);
		}


		// ============================================================================
		[TestMethod]
		public void TruncateLine_CutInsideEndTag_CutsBeforeEndTagAndClosesToken()
		{
			// Arrange
			string colored = new string('x', MaxLineLength - ColorToken.Length - CharactersBeforeCut);
			string line = ColorToken + colored + ColorEndTag + "tail";

			// Act
			string result = LogMarkup.TruncateLine(line);

			// Assert
			Assert.AreEqual(ColorToken + colored + ColorEndTag + TruncationMarker, result);
		}


		// ============================================================================
		[TestMethod]
		public void TruncateLine_CutRightAfterEscapingBackslash_CutsBeforeBackslash()
		{
			// Arrange
			string head = new string('x', MaxLineLength - 1);
			string line = head + "\\#" + "tail";

			// Act
			string result = LogMarkup.TruncateLine(line);

			// Assert
			Assert.AreEqual(head + TruncationMarker, result);
		}


		// ============================================================================
		[TestMethod]
		public void TruncateLine_CutRightAfterLiteralBackslash_KeepsBackslash()
		{
			// Arrange
			string head = new string('x', MaxLineLength - 1) + "\\";
			string line = head + "tail";

			// Act
			string result = LogMarkup.TruncateLine(line);

			// Assert
			Assert.AreEqual(head + TruncationMarker, result);
		}


		// ============================================================================
		[TestMethod]
		public void TruncateLine_OpenColorTokenAndCutRightAfterLiteralBackslash_CutsBeforeBackslashAndClosesToken()
		{
			// Arrange
			string colored = new string('x', MaxLineLength - ColorToken.Length - 1);
			string line = ColorToken + colored + "\\" + "yyyy";

			// Act
			string result = LogMarkup.TruncateLine(line);

			// Assert
			Assert.AreEqual(ColorToken + colored + ColorEndTag + TruncationMarker, result);
		}


		// ============================================================================
		[TestMethod]
		public void TruncateLine_OpenColorToken_ClosesTokenBeforeMarker()
		{
			// Arrange
			string line = ColorToken + new string('x', MaxLineLength);

			// Act
			string result = LogMarkup.TruncateLine(line);

			// Assert
			string expected =
				ColorToken +
				new string('x', MaxLineLength - ColorToken.Length) +
				ColorEndTag +
				TruncationMarker;

			Assert.AreEqual(expected, result);
		}


		// ============================================================================
		[TestMethod]
		public void TruncateLine_EscapedColorToken_IsNotClosed()
		{
			// Arrange
			string line = "\\" + ColorToken + new string('x', MaxLineLength);

			// Act
			string result = LogMarkup.TruncateLine(line);

			// Assert
			Assert.AreEqual(line.Substring(0, MaxLineLength) + TruncationMarker, result);
		}


		// ============================================================================
		[TestMethod]
		public void TruncateLine_ColorTokenAfterEscapedBackslash_IsClosed()
		{
			// Arrange
			string line = "\\\\" + ColorToken + new string('x', MaxLineLength);

			// Act
			string result = LogMarkup.TruncateLine(line);

			// Assert
			Assert.AreEqual(line.Substring(0, MaxLineLength) + ColorEndTag + TruncationMarker, result);
		}


		// ============================================================================
		[TestMethod]
		public void TruncateLine_ColorTokenFollowedOnlyByEscapedEndTag_IsClosed()
		{
			// Arrange
			string line = ColorToken + "text\\" + ColorEndTag + new string('x', MaxLineLength);

			// Act
			string result = LogMarkup.TruncateLine(line);

			// Assert
			Assert.AreEqual(line.Substring(0, MaxLineLength) + ColorEndTag + TruncationMarker, result);
		}


		// ============================================================================
		[TestMethod]
		public void TruncateLine_ClosedColorToken_IsNotClosedAgain()
		{
			// Arrange
			string line = ColorToken + "text" + ColorEndTag + new string('x', MaxLineLength);

			// Act
			string result = LogMarkup.TruncateLine(line);

			// Assert
			Assert.AreEqual(line.Substring(0, MaxLineLength) + TruncationMarker, result);
		}


		// ============================================================================
		[TestMethod]
		[DataRow("\\", "\\\\")]
		[DataRow("#", "\\#")]
		[DataRow("*", "\\*")]
		[DataRow("`", "\\`")]
		[DataRow("<", "\\<")]
		public void Escape_SpecialCharacter_PrefixesBackslash(
			string text,
			string expected)
		{
			// Act
			string result = LogMarkup.Escape(text);

			// Assert
			Assert.AreEqual(expected, result);
		}


		// ============================================================================
		[TestMethod]
		public void Escape_OrdinaryCharacters_AreUnchanged()
		{
			// Arrange
			string text = "Solution_1 > v1.0 (a/b) _x_ ~y~";

			// Act
			string result = LogMarkup.Escape(text);

			// Assert
			Assert.AreEqual(text, result);
		}


		// ============================================================================
		[TestMethod]
		public void Escape_Heading_EscapesEveryHash()
		{
			// Act
			string result = LogMarkup.Escape("##### Done.");

			// Assert
			Assert.AreEqual("\\#\\#\\#\\#\\# Done.", result);
		}


		// ============================================================================
		[TestMethod]
		public void Escape_Null_ReturnsEmpty()
		{
			// Act
			string result = LogMarkup.Escape(null);

			// Assert
			Assert.AreEqual(string.Empty, result);
		}


		// ============================================================================
		[DataTestMethod]
		[DataRow("\\\\", 0, true)]
		[DataRow("\\#", 0, true)]
		[DataRow("\\*", 0, true)]
		[DataRow("\\`", 0, true)]
		[DataRow("\\<", 0, true)]
		[DataRow("a\\*", 1, true)]
		[DataRow("\\a", 0, false)]
		[DataRow("\\", 0, false)]
		[DataRow("#", 0, false)]
		public void IsEscape_CharacterAtIndex_TellsEscapeFromLiteral(
			string text,
			int index,
			bool expectedIsEscape)
		{
			// Act
			bool isEscape = LogMarkup.IsEscape(text, index);

			// Assert
			Assert.AreEqual(expectedIsEscape, isEscape);
		}

	}
}
