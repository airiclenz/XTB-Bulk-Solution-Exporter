using System;
using System.Drawing;
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
	/// with bounded recursion and cut oversized lines.
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

		private const string TagStart = "<";

		// How far before the cut a tag starts when a test places it across the cut.
		private const int CharactersBeforeCut = 5;


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
		[Timeout(RenderTimeoutInMilliseconds)]
		public void ParseAndAppend_EscapedHeading_RendersPlainText()
		{
			// Act
			RenderedSelection rendered = RenderAndSelect(@"\#\#\#\#\# Done.", 0, 1);

			// Assert
			Assert.AreEqual("##### Done.", rendered.Text, "rendered text");
			Assert.IsFalse(rendered.IsBold, "text is bold");
		}


		// ============================================================================
		[TestMethod]
		[Timeout(RenderTimeoutInMilliseconds)]
		public void ParseAndAppend_EscapedItalicMarkers_RendersPlainText()
		{
			// Act
			RenderedSelection rendered = RenderAndSelect(@"\*a\*", 1, 1);

			// Assert
			Assert.AreEqual("*a*", rendered.Text, "rendered text");
			Assert.IsFalse(rendered.IsItalic, "text is italic");
		}


		// ============================================================================
		[TestMethod]
		[Timeout(RenderTimeoutInMilliseconds)]
		public void ParseAndAppend_EscapedColorToken_RendersUncoloredText()
		{
			// Arrange
			const string literalText = "<color=#FF0000>x</color>";
			int coloredTextIndex = literalText.IndexOf('x');

			// Act
			RenderedSelection rendered = RenderAndSelect(@"\" + literalText, coloredTextIndex, 1);

			// Assert
			Assert.AreEqual(literalText, rendered.Text, "rendered text");
			Assert.AreNotEqual(Color.FromArgb(0xFF, 0x00, 0x00).ToArgb(), rendered.ColorArgb, "text color");
		}


		// ============================================================================
		[TestMethod]
		[Timeout(RenderTimeoutInMilliseconds)]
		public void ParseAndAppend_LoneTrailingBackslash_RendersBackslash()
		{
			// Act
			RenderedSelection rendered = RenderAndSelect(@"a\", 0, 1);

			// Assert
			Assert.AreEqual(@"a\", rendered.Text);
		}


		// ============================================================================
		[TestMethod]
		[Timeout(RenderTimeoutInMilliseconds)]
		public void ParseAndAppend_BoldWithEscapedAsterisk_RendersBoldText()
		{
			// Act
			RenderedSelection rendered = RenderAndSelect(@"**a\*b**", 0, 3);

			// Assert
			Assert.AreEqual("a*b", rendered.Text, "rendered text");
			Assert.IsTrue(rendered.IsBold, "text is bold");
		}


		// ============================================================================
		[TestMethod]
		[Timeout(RenderTimeoutInMilliseconds)]
		public void ParseAndAppend_ColorWithEscapedEndTag_RendersColoredText()
		{
			// Arrange
			const string expectedText = "a</color>b";

			// Act
			RenderedSelection rendered = RenderAndSelect(
				@"<color=#112233>a\</color>b</color>",
				0,
				expectedText.Length);

			// Assert
			Assert.AreEqual(expectedText, rendered.Text, "rendered text");
			Assert.AreEqual(Color.FromArgb(0x11, 0x22, 0x33).ToArgb(), rendered.ColorArgb, "text color");
		}


		// ============================================================================
		[TestMethod]
		[Timeout(RenderTimeoutInMilliseconds)]
		public void ParseAndAppend_HugeLineCutInsideColorToken_ShowsNoPartialTag()
		{
			// Arrange
			string line =
				new string('x', MarkdownParser.MaxLogLineLength - CharactersBeforeCut) +
				ErrorColorToken +
				"y" +
				ColorEndTag +
				new string('z', MarkdownParser.MaxLogLineLength);

			// Act
			RenderedSelection rendered = RenderAndSelect(line, 0, 1);

			// Assert
			Assert.IsFalse(rendered.Text.Contains(TagStart), "rendered text contains a partial tag");
		}


		// ============================================================================
		[TestMethod]
		[Timeout(RenderTimeoutInMilliseconds)]
		public void ParseAndAppend_HugeLineCutInsideEndTag_ShowsNoPartialTag()
		{
			// Arrange
			string line =
				ErrorColorToken +
				new string('x', MarkdownParser.MaxLogLineLength - ErrorColorToken.Length - CharactersBeforeCut) +
				ColorEndTag +
				new string('z', MarkdownParser.MaxLogLineLength);

			// Act
			RenderedSelection rendered = RenderAndSelect(line, 0, 1);

			// Assert
			Assert.IsFalse(rendered.Text.Contains(TagStart), "rendered text contains a partial tag");
		}


		// ============================================================================
		/// <summary>
		/// Renders one line on a small-stack thread and reads back the trimmed
		/// text and the style of the given character range.
		/// </summary>
		private static RenderedSelection RenderAndSelect(
			string line,
			int selectionStart,
			int selectionLength)
		{
			return RenderOnSmallStackThread(
				richTextBox =>
				{
					new MarkdownParser(richTextBox).ParseAndAppend(line);
					richTextBox.Select(selectionStart, selectionLength);

					return new RenderedSelection(
						richTextBox.Text.TrimEnd(),
						richTextBox.SelectionFont.Bold,
						richTextBox.SelectionFont.Italic,
						richTextBox.SelectionColor.ToArgb());
				});
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


		// ============================================================================
		// ============================================================================
		// ============================================================================
		/// <summary>
		/// The rendered text of a log line and the style of one selected range.
		/// </summary>
		private sealed class RenderedSelection
		{
			// ============================================================================
			public RenderedSelection(
				string text,
				bool isBold,
				bool isItalic,
				int colorArgb)
			{
				Text = text;
				IsBold = isBold;
				IsItalic = isItalic;
				ColorArgb = colorArgb;
			}


			// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
			public string Text
			{
				get;
			}


			// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
			public bool IsBold
			{
				get;
			}


			// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
			public bool IsItalic
			{
				get;
			}


			// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
			public int ColorArgb
			{
				get;
			}
		}
	}
}
