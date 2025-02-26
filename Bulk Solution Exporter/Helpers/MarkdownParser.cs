using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;


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

			_colorTokenRegex = new Regex(@"<color=#([0-9A-Fa-f]{6})>");
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
					line.TrimEnd(),
					Array.Empty<FontStyle>(),
					_richTextBox.Font.Size);

				_richTextBox.AppendText(Environment.NewLine);
			}

			_richTextBox.SelectionStart = originalSelectionStart;
			_richTextBox.SelectionLength = originalSelectionLength;
		}


		// ============================================================================
		private void ProcessFormattedText(
			string text,
			FontStyle[]
			currentStyles,
			float fontSize,
			Color? textColor = null)
		{

			int currentPos = 0;

			while (currentPos < text.Length)
			{

				// -----------------------------------------------------------
				// 1) Check if there's a heading (#) at the current position
				//    (e.g. "### heading text")
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
						ProcessFormattedText(headingContent, headingStyles, headingSize, textColor);

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
					// No more markers => append remainder as-is
					AppendWithStyles(
						text.Substring(currentPos),
						currentStyles,
						fontSize,
						textColor);

					break;
				}

				// Append text before the marker
				if (nextMarker.position > currentPos)
				{
					AppendWithStyles(
						text.Substring(currentPos, nextMarker.position - currentPos),
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
						int endPosInner = text.IndexOf("</color>", contentStartInner);

						if (endPosInner != -1)
						{
							string contentInner = text.Substring(contentStartInner, endPosInner - contentStartInner);
							ProcessFormattedText(contentInner, currentStyles, fontSize, tokenColor);
							currentPos = endPosInner + "</color>".Length;
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
				ProcessFormattedText(content, newStyles, fontSize, textColor);

				// Move position past the end marker
				currentPos = contentStart + endPos + nextMarker.marker.End.Length;
			}
		}


		// ============================================================================
		private (bool found, int position, FormatMarker marker) FindNextMarker(string text, int startPos)
		{
			int nearestPos = int.MaxValue;
			FormatMarker nearestMarker = null;

			// Check for color token first
			var colorMatch = _colorTokenRegex.Match(text, startPos);
			if (colorMatch.Success && colorMatch.Index < nearestPos)
			{
				nearestPos = colorMatch.Index;
				nearestMarker = _formatMarkers.First(m => m.IsColorToken);
			}

			// Check for other markers
			foreach (var marker in _formatMarkers.Where(m => !m.IsColorToken))
			{
				int pos = text.IndexOf(marker.Start, startPos);
				if (pos != -1 && pos < nearestPos)
				{
					nearestPos = pos;
					nearestMarker = marker;
				}
			}

			return (nearestMarker != null, nearestPos, nearestMarker);
		}


		// ============================================================================
		private int FindMatchingEnd(string text, string endMarker)
		{
			int pos = 0;
			while (pos < text.Length)
			{
				int endPos = text.IndexOf(endMarker, pos);
				if (endPos == -1)
					return -1;

				// Check if this is a valid end marker (not part of another token)
				bool isValid = true;
				foreach (var marker in _formatMarkers)
				{
					if (marker.Start.Length > endMarker.Length)
					{
						int checkPos = endPos + endMarker.Length - marker.Start.Length;
						if (checkPos >= 0 && text.IndexOf(marker.Start, checkPos) == checkPos)
						{
							isValid = false;
							break;
						}
					}
				}

				if (isValid)
				{
					return endPos;
				}

				pos = endPos + 1;
			}

			return -1;
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
