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
		public void ParseAndAppend(string markdown)
		{
			int originalSelectionStart = _richTextBox.SelectionStart;
			int originalSelectionLength = _richTextBox.SelectionLength;

			_richTextBox.SelectionStart = _richTextBox.TextLength;
			_richTextBox.SelectionLength = 0;

			string[] lines = markdown.Split(new[] { Environment.NewLine, "\n" },
				StringSplitOptions.None);

			foreach (string line in lines)
			{
				ProcessLine(line.TrimEnd());
				_richTextBox.AppendText(Environment.NewLine);
			}

			_richTextBox.SelectionStart = originalSelectionStart;
			_richTextBox.SelectionLength = originalSelectionLength;
		}


		// ============================================================================
		private void ProcessLine(string line)
		{
			if (line.StartsWith("#"))
			{
				int headerLevel = 0;
				while (headerLevel < line.Length && headerLevel < 6 && line[headerLevel] == '#')
				{
					headerLevel++;
				}

				if (headerLevel > 0 && headerLevel <= 6)
				{
					float fontSize = _richTextBox.Font.Size + (6 - headerLevel) * 2;
					string headerText = line.Substring(headerLevel).Trim();
					ProcessFormattedText(headerText, new FontStyle[] { FontStyle.Bold }, fontSize);
					return;
				}
			}

			ProcessFormattedText(line, Array.Empty<FontStyle>(), _richTextBox.Font.Size);
		}


		// ============================================================================
		private void ProcessFormattedText(string text, FontStyle[] currentStyles, float fontSize, Color? textColor = null)
		{
			int currentPos = 0;

			while (currentPos < text.Length)
			{
				// Find the next formatting marker
				var nextMarker = FindNextMarker(text, currentPos);

				if (!nextMarker.found)
				{
					// No more markers, append the rest as plain text with current styles
					AppendWithStyles(text.Substring(currentPos), currentStyles, fontSize, textColor);
					break;
				}

				// Append text before the marker
				if (nextMarker.position > currentPos)
				{
					AppendWithStyles(text.Substring(currentPos, nextMarker.position - currentPos),
						currentStyles, fontSize, textColor);
				}

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
