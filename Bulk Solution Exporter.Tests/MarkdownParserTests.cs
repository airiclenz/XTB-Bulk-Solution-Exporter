using System;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Windows.Forms;
using Com.AiricLenz.XTB.Plugin.Helpers;
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
	/// Covers <see cref="MarkdownParser"/>, which must render any log line
	/// with bounded recursion and cut oversized lines, and the size guard of
	/// <see cref="Formatter.FormatErrorStringWithXml"/>.
	/// </summary>
	[TestClass]
	public class MarkdownParserTests
	{
		// Stack of the rendering thread: far below the 1 MB default, so
		// unbounded recursion overflows it long before a full-length line ends.
		private const int SmallStackSizeInBytes = 256 * 1024;

		// Upper bound for one rendering test.
		private const int RenderTimeoutInMilliseconds = 60000;

		// Length of the oversized line; well past MaxLogLineLength.
		private const int HugeLineLength = 2000000;

		private const string HeadingPrefix = "# ";

		private const string HeadingWithBoldText = "# **x**";

		private const string ErrorColorToken = "<color=#770000>";

		private const string ColorEndTag = "</color>";

		private const string ColorTokenStart = "<color=";


		// ============================================================================
		[DataTestMethod]
		[Timeout(RenderTimeoutInMilliseconds)]
		[DataRow(HeadingPrefix)]
		[DataRow(HeadingWithBoldText)]
		public void ParseAndAppend_RepeatedHeadingsJustUnderTheLimit_RendersWithoutCrash(
			string repeatedText)
		{
			// Arrange
			int repeatCount = (MarkdownParser.MaxLogLineLength - 1) / repeatedText.Length;
			string line = string.Concat(Enumerable.Repeat(repeatedText, repeatCount));

			// Act
			int renderedLength = RenderOnSmallStackThread(
				richTextBox =>
				{
					new MarkdownParser(richTextBox).ParseAndAppend(line);
					return richTextBox.TextLength;
				});

			// Assert
			Assert.IsTrue(renderedLength > 0, "rendered text length");
		}


		// ============================================================================
		[TestMethod]
		[Timeout(RenderTimeoutInMilliseconds)]
		public void ParseAndAppend_OverLimitColoredLine_HidesTheColorToken()
		{
			// Arrange
			string line =
				ErrorColorToken +
				new string('x', MarkdownParser.MaxLogLineLength * 2) +
				ColorEndTag;

			// Act
			string renderedText = RenderOnSmallStackThread(
				richTextBox =>
				{
					new MarkdownParser(richTextBox).ParseAndAppend(line);
					return richTextBox.Text;
				});

			// Assert
			Assert.IsFalse(renderedText.Contains(ColorTokenStart), "rendered text contains a color token");
		}


		// ============================================================================
		[TestMethod]
		[Timeout(RenderTimeoutInMilliseconds)]
		public void ParseAndAppend_HugeLine_EndsWithTruncationMarker()
		{
			// Arrange
			string line = new string('x', HugeLineLength);

			// Act
			string renderedText = RenderOnSmallStackThread(
				richTextBox =>
				{
					new MarkdownParser(richTextBox).ParseAndAppend(line);
					return richTextBox.Text;
				});

			// Assert
			Assert.IsTrue(
				renderedText.TrimEnd().EndsWith(MarkdownParser.TruncationMarker.Trim()),
				"rendered text ends with the truncation marker");
		}


		// ============================================================================
		[TestMethod]
		[Timeout(RenderTimeoutInMilliseconds)]
		public void ParseAndAppend_NormalHeading_RendersBoldHeadingText()
		{
			// Arrange
			const string line = "##### Done.";
			const string headingText = "Done.";

			// Act
			Tuple<string, bool> rendered = RenderOnSmallStackThread(
				richTextBox =>
				{
					new MarkdownParser(richTextBox).ParseAndAppend(line);
					richTextBox.Select(0, headingText.Length);
					return Tuple.Create(richTextBox.Text.TrimEnd(), richTextBox.SelectionFont.Bold);
				});

			// Assert
			Assert.AreEqual(headingText, rendered.Item1, "rendered text");
			Assert.IsTrue(rendered.Item2, "heading is bold");
		}


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


		// ============================================================================
		/// <summary>
		/// Runs the render on an STA thread with a small stack, against a
		/// fresh rich text box whose window handle exists, and rethrows any
		/// failure on the test thread.
		/// </summary>
		private static TResult RenderOnSmallStackThread<TResult>(
			Func<RichTextBox, TResult> render)
		{
			TResult result = default(TResult);
			Exception failure = null;

			var renderThread = new Thread(
				() =>
				{
					try
					{
						using (var richTextBox = new RichTextBox())
						{
							richTextBox.CreateControl();
							result = render(richTextBox);
						}
					}
					catch (Exception exception)
					{
						failure = exception;
					}
				},
				SmallStackSizeInBytes);

			renderThread.SetApartmentState(ApartmentState.STA);
			renderThread.Start();
			renderThread.Join();

			if (failure != null)
			{
				ExceptionDispatchInfo.Capture(failure).Throw();
			}

			return result;
		}
	}
}
