using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Com.AiricLenz.BulkSolutionExporter.Core.Run;


// ============================================================================
// ============================================================================
// ============================================================================
namespace Com.AiricLenz.XTB.Plugin.Helpers
{

	// ============================================================================
	// ============================================================================
	// ============================================================================
	public class MarkdownParser
	{
		// Longest log line rendered; longer lines are cut to this length and
		// end with TruncationMarker, so one huge message cannot stall the UI thread.
		internal const int MaxLogLineLength = LogMarkup.MaxLineLength;

		// Appended to a line cut at MaxLogLineLength.
		internal const string TruncationMarker = LogMarkup.TruncationMarker;

		// Deepest nesting of headings and format markers parsed; text below
		// this depth is appended with only its escapes resolved. Bounds the
		// recursion so a line such as "# # # ..." cannot overflow the stack.
		internal const int MaxNestingDepth = 10;

		// Closes a color token.
		private const string ColorEndTag = RunLog.ColorEndTag;

		// Starts an escape; see LogMarkup for the escape rule.
		private const char EscapeCharacter = '\\';

		// Length of an escape: the EscapeCharacter plus the character it escapes.
		private const int EscapeLength = 2;


		// ============================================================================
		private readonly RichTextBox _richTextBox;


		private class FormatMarker
		{
			public string Start
			{
				get; set;
			}
			public string End
			{
				get; set;
			}
			public FontStyle Style
			{
				get; set;
			}
			public Color? Color
			{
				get; set;
			}
			public bool IsColorToken
			{
				get; set;
			}

			public FormatMarker(string start, string end, FontStyle style, Color? color = null, bool isColorToken = false)
			{
				Start = start;
				End = end;
				Style = style;
				Color = color;
				IsColorToken = isColorToken;
			}
		}

		private readonly List<FormatMarker> _formatMarkers;
		private readonly Regex _colorTokenRegex;


		// ============================================================================
		public MarkdownParser(RichTextBox richTextBox)
		{
			_richTextBox = richTextBox;
			_formatMarkers = new List<FormatMarker>
			{
				new FormatMarker("**", "**", FontStyle.Bold),
				new FormatMarker("*", "*", FontStyle.Italic),
				new FormatMarker("`", "`", FontStyle.Regular, Color.DarkRed),

				// Color token marker with placeholder start/end - actual values will be found by regex
				new FormatMarker("<color=", "</color>", FontStyle.Regular, null, true)
			};

			// Anchored, so a match is tried only at the given position.
			_colorTokenRegex = new Regex(@"\G<color=#([0-9A-Fa-f]{6})>");
		}


		// ============================================================================
		/// <summary>
		/// A simplified parser that tries to handle a few basic tokens:
		/// - **bold** text
		/// - *italic* text
		/// - `code` (colored)
		/// - # Heading (rest of line in bold, bigger font)
		/// - <color=#000066>colored</color> text
		/// - Regular text
		/// A backslash before a backslash, #, *, backtick or &lt; renders that
		/// character literally, never as markup; any other backslash is literal.
		/// Lines longer than <see cref="MaxLogLineLength"/> are truncated by
		/// <see cref="LogMarkup.TruncateLine"/> and end with
		/// <see cref="TruncationMarker"/>; markers nested deeper than
		/// <see cref="MaxNestingDepth"/> are rendered as plain text.
		/// </summary>
		/// <param name="message"></param>
		public void ParseAndAppend(string markdown)
		{
			int originalSelectionStart = _richTextBox.SelectionStart;
			int originalSelectionLength = _richTextBox.SelectionLength;

			_richTextBox.SelectionStart = _richTextBox.TextLength;
			_richTextBox.SelectionLength = 0;

			string[] lines =
				markdown.Split(
					new[] { Environment.NewLine, "\n" },
					StringSplitOptions.None);

			foreach (string line in lines)
			{
				ProcessFormattedText(
					LogMarkup.TruncateLine(line.TrimEnd()),
					Array.Empty<FontStyle>(),
					_richTextBox.Font.Size,
					0);

				_richTextBox.AppendText(Environment.NewLine);
			}

			_richTextBox.SelectionStart = originalSelectionStart;
			_richTextBox.SelectionLength = originalSelectionLength;
		}


		// ============================================================================
		/// <summary>
		/// Appends the text with its headings, format markers and escapes
		/// applied. Every heading or marker parses its content one level
		/// deeper; at <see cref="MaxNestingDepth"/> the text is appended with
		/// only its escapes resolved. Positions inside an escape are never
		/// markup, and every content range parsed starts and ends outside one.
		/// </summary>
		private void ProcessFormattedText(
			string text,
			FontStyle[]
			currentStyles,
			float fontSize,
			int depth,
			Color? textColor = null)
		{
			if (depth >= MaxNestingDepth)
			{
				AppendWithStyles(RemoveEscapes(text), currentStyles, fontSize, textColor);
				return;
			}

			int currentPos = 0;

			while (currentPos < text.Length)
			{

				// -----------------------------------------------------------
				// 1) Check if there's a heading (#) at the current position
				//    (e.g. "### heading text"); currentPos is never inside
				//    an escape, so an escaped "\#" is no heading
				// -----------------------------------------------------------
				if (text[currentPos] == '#')
				{
					int headingScan = currentPos;
					int headingCount = 0;

					// Count up to 6 '#' characters
					while (headingScan < text.Length && headingCount < 6 && text[headingScan] == '#')
					{
						headingCount++;
						headingScan++;
					}

					// (Optional) typical Markdown requires a space or end-of-line after the #'s
					bool hasSpaceOrEOL = (headingScan >= text.Length
										  || text[headingScan] == ' '
										  || text[headingScan] == '\r'
										  || text[headingScan] == '\n');


					if (headingCount > 0 &&
						hasSpaceOrEOL)
					{
						// Skip a single space if present
						if (headingScan < text.Length && text[headingScan] == ' ')
						{
							headingScan++;
						}

						// Now figure out how far the heading text goes.
						// For a multi-line scenario, you might parse until newline (or end of text).
						int nextLineBreak = text.IndexOfAny(new[] { '\r', '\n' }, headingScan);
						if (nextLineBreak < 0)
							nextLineBreak = text.Length;

						// Extract the heading substring
						string headingContent = text.Substring(headingScan, nextLineBreak - headingScan);

						// Example: heading size = base size + (6 - headingCount)*2
						float headingSize = _richTextBox.Font.Size + (6 - headingCount) * 2;

						// Bold is typical for headings, so add FontStyle.Bold
						var headingStyles = currentStyles.Concat(new[] { FontStyle.Bold }).ToArray();

						// Recursively parse the heading text so we still detect **bold**, <color=...>, etc.
						ProcessFormattedText(headingContent, headingStyles, headingSize, depth + 1, textColor);

						// Advance currentPos past the heading text
						currentPos = nextLineBreak;
						continue; // Move on to the next iteration
					}
				}

				// -----------------------------------------------------------
				// 2) If no heading found, fall back to your existing marker logic
				// -----------------------------------------------------------
				var nextMarker = FindNextMarker(text, currentPos);

				if (!nextMarker.found)
				{
					// No more markers => append remainder with its escapes resolved
					AppendWithStyles(
						RemoveEscapes(text.Substring(currentPos)),
						currentStyles,
						fontSize,
						textColor);

					break;
				}

				// Append text before the marker
				if (nextMarker.position > currentPos)
				{
					AppendWithStyles(
						RemoveEscapes(text.Substring(currentPos, nextMarker.position - currentPos)),
						currentStyles,
						fontSize,
						textColor);
				}

				// Then handle the marker (bold, italic, code, or color) as in your original code
				if (nextMarker.marker.IsColorToken)
				{
					// Handle color token
					var match = _colorTokenRegex.Match(text, nextMarker.position);

					if (match.Success)
					{
						string colorHex = match.Groups[1].Value;
						Color tokenColor = ColorTranslator.FromHtml("#" + colorHex);

						int contentStartInner = match.Index + match.Length;
						int endPosInner = IndexOfUnescaped(text, ColorEndTag, contentStartInner);

						if (endPosInner != -1)
						{
							string contentInner = text.Substring(contentStartInner, endPosInner - contentStartInner);
							ProcessFormattedText(contentInner, currentStyles, fontSize, depth + 1, tokenColor);
							currentPos = endPosInner + ColorEndTag.Length;
							continue;
						}
					}

					// If color token is invalid, treat it as plain text
					AppendWithStyles(
						text.Substring(nextMarker.position, nextMarker.marker.Start.Length),
						currentStyles,
						fontSize,
						textColor);

					currentPos = nextMarker.position + nextMarker.marker.Start.Length;
					continue;
				}

				// Find the end of the current format, trimming any trailing whitespace
				string remainingText = text.Substring(nextMarker.position + nextMarker.marker.Start.Length);
				int endPos = FindMatchingEnd(remainingText, nextMarker.marker.End);

				if (endPos == -1)
				{
					// No matching end marker found, append the marker as plain text and continue
					AppendWithStyles(nextMarker.marker.Start, currentStyles, fontSize, textColor);
					currentPos = nextMarker.position + nextMarker.marker.Start.Length;
					continue;
				}

				// Extract the text between markers
				int contentStart = nextMarker.position + nextMarker.marker.Start.Length;
				string content = remainingText.Substring(0, endPos);

				// Create new style array with current marker's style added
				var newStyles = currentStyles.Concat(new[] { nextMarker.marker.Style }).ToArray();

				// Recursively process the content with the new style combination
				ProcessFormattedText(content, newStyles, fontSize, depth + 1, textColor);

				// Move position past the end marker
				currentPos = contentStart + endPos + nextMarker.marker.End.Length;
			}
		}


		// ============================================================================
		/// <summary>
		/// Finds the first format marker or color token at or after
		/// <paramref name="startPos"/> that is not inside an escape.
		/// </summary>
		/// <param name="startPos">A position outside any escape.</param>
		private (bool found, int position, FormatMarker marker) FindNextMarker(string text, int startPos)
		{
			int index = startPos;

			while (index < text.Length)
			{
				if (LogMarkup.IsEscape(text, index))
				{
					index += EscapeLength;
					continue;
				}

				FormatMarker marker = FindMarkerAt(text, index);

				if (marker != null)
				{
					return (true, index, marker);
				}

				index++;
			}

			return (false, -1, null);
		}


		// ============================================================================
		/// <summary>
		/// Returns the format marker or color token starting at
		/// <paramref name="index"/>, or null. The markers are tried in list
		/// order, which puts "**" before "*", so "**" is bold, not two italics.
		/// </summary>
		private FormatMarker FindMarkerAt(string text, int index)
		{
			foreach (var marker in _formatMarkers)
			{
				bool isMarkerAtIndex =
					marker.IsColorToken
						? _colorTokenRegex.IsMatch(text, index)
						: IsAt(text, index, marker.Start);

				if (isMarkerAtIndex)
				{
					return marker;
				}
			}

			return null;
		}


		// ============================================================================
		/// <summary>
		/// Returns the position of the first end marker in the text that is
		/// not inside an escape and does not end a longer start marker, or -1.
		/// </summary>
		/// <param name="text">The text after the start marker.</param>
		private int FindMatchingEnd(string text, string endMarker)
		{
			bool isAfterEscape = false;
			int index = 0;

			while (index < text.Length)
			{
				if (LogMarkup.IsEscape(text, index))
				{
					isAfterEscape = true;
					index += EscapeLength;
					continue;
				}

				// A longer start marker ending here would cover the escaped
				// character just before, so after an escape there is none.
				if (IsAt(text, index, endMarker) &&
					(isAfterEscape || !EndsLongerStartMarker(text, index, endMarker)))
				{
					return index;
				}

				isAfterEscape = false;
				index++;
			}

			return -1;
		}


		// ============================================================================
		/// <summary>
		/// Returns true when the end marker at <paramref name="endPos"/> is the
		/// tail of a longer start marker (e.g. the second "*" of "**"), so it
		/// is no end marker.
		/// </summary>
		private bool EndsLongerStartMarker(
			string text,
			int endPos,
			string endMarker)
		{
			foreach (var marker in _formatMarkers)
			{
				if (marker.Start.Length <= endMarker.Length)
				{
					continue;
				}

				int checkPos = endPos + endMarker.Length - marker.Start.Length;

				if (checkPos >= 0 && IsAt(text, checkPos, marker.Start))
				{
					return true;
				}
			}

			return false;
		}


		// ============================================================================
		/// <summary>
		/// Returns the position of the first <paramref name="value"/> at or
		/// after <paramref name="startPos"/> that is not inside an escape, or -1.
		/// </summary>
		/// <param name="startPos">A position outside any escape.</param>
		private static int IndexOfUnescaped(
			string text,
			string value,
			int startPos)
		{
			int index = startPos;

			while (index < text.Length)
			{
				if (LogMarkup.IsEscape(text, index))
				{
					index += EscapeLength;
					continue;
				}

				if (IsAt(text, index, value))
				{
					return index;
				}

				index++;
			}

			return -1;
		}


		// ============================================================================
		/// <summary>
		/// Returns true when <paramref name="value"/> occurs in the text
		/// exactly at <paramref name="index"/> (ordinal comparison).
		/// </summary>
		private static bool IsAt(
			string text,
			int index,
			string value)
		{
			return string.CompareOrdinal(text, index, value, 0, value.Length) == 0;
		}


		// ============================================================================
		/// <summary>
		/// Replaces each escape in the text with the character it escapes; a
		/// backslash that escapes nothing is kept.
		/// </summary>
		/// <param name="text">Text that starts outside any escape.</param>
		private static string RemoveEscapes(string text)
		{
			if (text.IndexOf(EscapeCharacter) < 0)
			{
				return text;
			}

			var unescapedText = new StringBuilder(text.Length);
			int index = 0;

			while (index < text.Length)
			{
				if (LogMarkup.IsEscape(text, index))
				{
					// Skip the backslash; the escaped character is kept below.
					index++;
				}

				unescapedText.Append(text[index]);
				index++;
			}

			return unescapedText.ToString();
		}


		// ============================================================================
		private void AppendWithStyles(string text, FontStyle[] styles, float size, Color? textColor)
		{
			if (string.IsNullOrEmpty(text))
				return;

			// Store original font and color
			Font originalFont = _richTextBox.SelectionFont ?? _richTextBox.Font;
			Color originalColor = _richTextBox.SelectionColor;

			// Combine all styles
			FontStyle combinedStyle = FontStyle.Regular;
			foreach (var style in styles)
			{
				combinedStyle |= style;
			}

			// Check if any of the current styles came from a code marker
			var codeMarker = _formatMarkers.FirstOrDefault(m => m.Color.HasValue &&
				styles.Contains(m.Style));

			// Set the text color - prioritize explicit color token over code marker color
			if (textColor.HasValue)
			{
				_richTextBox.SelectionColor = textColor.Value;
			}
			else if (codeMarker != null)
			{
				_richTextBox.SelectionColor = codeMarker.Color.Value;
			}

			// Create new font with combined styles
			_richTextBox.SelectionFont = new Font(originalFont.FontFamily, size, combinedStyle);
			_richTextBox.AppendText(text);

			// Restore original font and color
			_richTextBox.SelectionFont = originalFont;
			_richTextBox.SelectionColor = originalColor;
		}
	}
}
