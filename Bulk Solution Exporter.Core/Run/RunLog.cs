using System;
using System.Text;
using System.Text.RegularExpressions;
using Com.AiricLenz.BulkSolutionExporter.Core.Extensions;


// ============================================================================
// ============================================================================
// ============================================================================
namespace Com.AiricLenz.BulkSolutionExporter.Core.Run
{

	// ============================================================================
	// ============================================================================
	// ============================================================================
	/// <summary>
	/// Builds the run log's lines in the log mini-markup and hands each finished
	/// line to a sink. The markup is the contract every log view parses:
	/// - <c># Heading</c> (one to six <c>#</c>, rest of the line is the heading)
	/// - <c>**bold**</c>, <c>*italic*</c> and <c>`code`</c>
	/// - <c>&lt;color=#RRGGBB&gt;colored&lt;/color&gt;</c>
	/// A line is prefixed with the current indent and every rendered line of it
	/// is capped at <see cref="MaxLineLength"/> characters.
	/// </summary>
	public class RunLog
	{
		public const string ColorError = "<color=#770000>";
		public const string ColorSolution = "<color=#000077>";
		public const string ColorFile = "<color=#777700>";
		public const string ColorIndent = "<color=#DDDDDD>";
		public const string ColorGreen = "<color=#227700>";
		public const string ColorOrange = "<color=#CC6600>";
		public const string ColorConnection = "<color=#337799>";
		public const string ColorTeeth = "<color=#CCCCCE>";
		public const string ColorEndTag = "</color>";

		// Prefix of one indent level: a light vertical bar plus padding.
		public const string IndentUnit = ColorIndent + "|" + ColorEndTag + "   ";

		// Longest rendered line kept in full; same limit as the plugin's MarkdownParser,
		// so one huge message cannot stall a log view.
		public const int MaxLineLength = 10000;

		// Appended to a line cut at MaxLineLength.
		public const string TruncationMarker = " … [truncated]";

		private const int MinHeadingLevel = 1;
		private const int MaxHeadingLevel = 6;
		private const char HeadingMarker = '#';

		// Splits a message into lines, keeping the separators so they survive unchanged.
		private static readonly Regex LineSeparatorRegex = new Regex(@"(\r\n|\n)");

		private static readonly Regex ColorTokenRegex = new Regex(@"<color=#([0-9A-Fa-f]{6})>");

		private readonly Action<string> _sink;
		private readonly bool _isDebugEnabled;
		private int _indentLevel = 0;


		// ============================================================================
		/// <param name="sink">Receives every finished line, on the logging thread.</param>
		/// <param name="isDebugEnabled">When false, <see cref="LogDebug"/> logs nothing.</param>
		public RunLog(
			Action<string> sink,
			bool isDebugEnabled)
		{
			_sink = sink ?? throw new ArgumentNullException(nameof(sink));
			_isDebugEnabled = isDebugEnabled;
		}


		// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		/// <summary>
		/// The indent prefix of the current level.
		/// </summary>
		public string CurrentIndent
		{
			get
			{
				var indent = new StringBuilder();

				for (int level = 0; level < _indentLevel; level++)
				{
					indent.Append(IndentUnit);
				}

				return indent.ToString();
			}
		}


		// ============================================================================
		public int IncreaseIndent()
		{
			return ++_indentLevel;
		}


		// ============================================================================
		public int DecreaseIndent()
		{
			if (_indentLevel > 0)
			{
				_indentLevel--;
			}

			return _indentLevel;
		}


		// ============================================================================
		public void ResetIndent()
		{
			_indentLevel = 0;
		}


		// ============================================================================
		/// <summary>
		/// Logs the message at the current indent; an empty or whitespace-only
		/// message logs an empty line without indent.
		/// </summary>
		public void Log(string message = "")
		{
			if (message.IsEmpty())
			{
				_sink(string.Empty);
				return;
			}

			_sink(TruncateLines(CurrentIndent + message));
		}


		// ============================================================================
		/// <summary>
		/// Logs the message in the error color.
		/// </summary>
		public void LogError(string message)
		{
			Log(Colorize(ColorError, message));
		}


		// ============================================================================
		/// <summary>
		/// Logs the message only when debug output is enabled.
		/// </summary>
		public void LogDebug(string message = "")
		{
			if (!_isDebugEnabled)
			{
				return;
			}

			Log(message);
		}


		// ============================================================================
		/// <param name="level">Heading level, 1 (largest) to 6.</param>
		public static string Heading(
			int level,
			string text)
		{
			if (level < MinHeadingLevel || level > MaxHeadingLevel)
			{
				throw new ArgumentOutOfRangeException(nameof(level));
			}

			return new string(HeadingMarker, level) + " " + text;
		}


		// ============================================================================
		public static string Bold(string text)
		{
			return "**" + text + "**";
		}


		// ============================================================================
		public static string Italic(string text)
		{
			return "*" + text + "*";
		}


		// ============================================================================
		public static string Code(string text)
		{
			return "`" + text + "`";
		}


		// ============================================================================
		/// <param name="colorToken">One of the <c>Color*</c> constants.</param>
		public static string Colorize(
			string colorToken,
			string text)
		{
			return colorToken + text + ColorEndTag;
		}


		// ============================================================================
		/// <summary>
		/// Caps every line of the text at <see cref="MaxLineLength"/>; lines
		/// within the limit and the line separators are kept unchanged.
		/// </summary>
		private static string TruncateLines(string text)
		{
			string[] parts = LineSeparatorRegex.Split(text);

			if (parts.Length == 1)
			{
				return TruncateLine(text);
			}

			var result = new StringBuilder(text.Length);

			foreach (string part in parts)
			{
				result.Append(TruncateLine(part));
			}

			return result.ToString();
		}


		// ============================================================================
		/// <summary>
		/// Cuts a line longer than <see cref="MaxLineLength"/> (trailing
		/// whitespace not counted) and appends <see cref="TruncationMarker"/>.
		/// A color token left open by the cut is closed before the marker, so
		/// the kept text keeps its color.
		/// </summary>
		private static string TruncateLine(string line)
		{
			string trimmedLine = line.TrimEnd();

			if (trimmedLine.Length <= MaxLineLength)
			{
				return line;
			}

			string keptText = trimmedLine.Substring(0, MaxLineLength);

			if (HasUnclosedColorToken(keptText))
			{
				keptText += ColorEndTag;
			}

			return keptText + TruncationMarker;
		}


		// ============================================================================
		/// <summary>
		/// Returns true when a color token follows the last end tag; a parser
		/// pairs every token with the next end tag, so that token is unclosed.
		/// </summary>
		private static bool HasUnclosedColorToken(string text)
		{
			int lastEndTagIndex = text.LastIndexOf(ColorEndTag, StringComparison.Ordinal);

			int searchStart =
				lastEndTagIndex < 0
					? 0
					: lastEndTagIndex + ColorEndTag.Length;

			return ColorTokenRegex.IsMatch(text, searchStart);
		}

	}
}
