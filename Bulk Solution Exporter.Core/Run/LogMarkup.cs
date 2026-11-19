using System.Text;
using System.Text.RegularExpressions;


// ============================================================================
// ============================================================================
// ============================================================================
namespace Com.AiricLenz.BulkSolutionExporter.Core.Run
{

	// ============================================================================
	// ============================================================================
	// ============================================================================
	/// <summary>
	/// Owns the rules of the log mini-markup that every log view shares: the
	/// line length cap with its tag-safe truncation, and the backslash escape.
	/// A <c>\</c> before a backslash, <c>#</c>, <c>*</c>, backtick or <c>&lt;</c>
	/// makes that character literal, so it never acts as markup; any other
	/// backslash is literal itself.
	/// </summary>
	public static class LogMarkup
	{
		// Longest rendered line kept in full, so one huge message cannot stall a log view.
		public const int MaxLineLength = 10000;

		// Appended to a line cut at MaxLineLength.
		public const string TruncationMarker = " … [truncated]";

		private const char EscapeCharacter = '\\';

		// Every character an EscapeCharacter before it makes literal.
		private const string EscapableCharacters = "\\#*`<";

		private const char TagStart = '<';

		// Length of an escape: the EscapeCharacter plus the character it escapes.
		private const int EscapeLength = 2;

		// Matches a color token starting exactly at the search position.
		private static readonly Regex ColorTokenRegex = new Regex(@"\G<color=#[0-9A-Fa-f]{6}>");


		// ============================================================================
		/// <summary>
		/// Cuts a line longer than <see cref="MaxLineLength"/> (trailing
		/// whitespace not counted) and appends <see cref="TruncationMarker"/>.
		/// The cut moves back before a color token or end tag it would split and
		/// before an escape it would split, so the kept text never ends on a
		/// partial tag or on a backslash escaping the cut-off character. A color
		/// token left open is closed before the marker, so the kept text keeps
		/// its color; the cut then also moves back before a trailing literal
		/// backslash, which would otherwise escape the appended end tag. An
		/// escaped token or end tag is plain text, never a tag.
		/// </summary>
		/// <returns>The line unchanged when it is within the limit; an empty string when <paramref name="line"/> is null.</returns>
		public static string TruncateLine(string line)
		{
			if (line == null)
			{
				return string.Empty;
			}

			string trimmedLine = line.TrimEnd();

			if (trimmedLine.Length <= MaxLineLength)
			{
				return line;
			}

			int cutIndex = MaxLineLength;
			bool isColorTokenOpen = false;
			int lastElementIndex = -1;
			int index = 0;

			while (index < cutIndex)
			{
				int elementLength = 1;
				bool isColorToken = false;
				bool isEndTag = false;

				if (IsEscape(trimmedLine, index))
				{
					elementLength = EscapeLength;
				}
				else if (trimmedLine[index] == TagStart)
				{
					Match colorTokenMatch = ColorTokenRegex.Match(trimmedLine, index);

					if (colorTokenMatch.Success)
					{
						elementLength = colorTokenMatch.Length;
						isColorToken = true;
					}
					else if (string.CompareOrdinal(trimmedLine, index, RunLog.ColorEndTag, 0, RunLog.ColorEndTag.Length) == 0)
					{
						elementLength = RunLog.ColorEndTag.Length;
						isEndTag = true;
					}
				}

				if (index + elementLength > cutIndex)
				{
					cutIndex = index;
					break;
				}

				if (isColorToken)
				{
					isColorTokenOpen = true;
				}
				else if (isEndTag)
				{
					isColorTokenOpen = false;
				}

				lastElementIndex = index;
				index += elementLength;
			}

			if (isColorTokenOpen &&
				IsLiteralEscapeCharacter(trimmedLine, lastElementIndex, cutIndex))
			{
				cutIndex = lastElementIndex;
			}

			string keptText = trimmedLine.Substring(0, cutIndex);

			if (isColorTokenOpen)
			{
				keptText += RunLog.ColorEndTag;
			}

			return keptText + TruncationMarker;
		}


		// ============================================================================
		/// <summary>
		/// Escapes the text so every character of it renders literally: each
		/// backslash, <c>#</c>, <c>*</c>, backtick and <c>&lt;</c> gets a
		/// <c>\</c> before it; every other character is kept unchanged.
		/// </summary>
		/// <returns>An empty string when <paramref name="text"/> is null.</returns>
		public static string Escape(string text)
		{
			if (text == null)
			{
				return string.Empty;
			}

			var escapedText = new StringBuilder(text.Length);

			foreach (char character in text)
			{
				if (IsEscapable(character))
				{
					escapedText.Append(EscapeCharacter);
				}

				escapedText.Append(character);
			}

			return escapedText.ToString();
		}


		// ============================================================================
		/// <summary>
		/// Returns true when the character at <paramref name="index"/> is an
		/// <see cref="EscapeCharacter"/> followed by an escapable character.
		/// </summary>
		private static bool IsEscape(
			string text,
			int index)
		{
			return
				text[index] == EscapeCharacter &&
				index + 1 < text.Length &&
				IsEscapable(text[index + 1]);
		}


		// ============================================================================
		/// <summary>
		/// Returns true when the element from <paramref name="elementIndex"/> to
		/// <paramref name="elementEnd"/> is a lone <see cref="EscapeCharacter"/>
		/// that escapes nothing, so it is literal.
		/// </summary>
		private static bool IsLiteralEscapeCharacter(
			string text,
			int elementIndex,
			int elementEnd)
		{
			return
				elementIndex >= 0 &&
				elementEnd - elementIndex == 1 &&
				text[elementIndex] == EscapeCharacter;
		}


		// ============================================================================
		private static bool IsEscapable(char character)
		{
			return EscapableCharacters.IndexOf(character) >= 0;
		}

	}
}
