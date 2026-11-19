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
	/// - <c>\</c> before a backslash, <c>#</c>, <c>*</c>, backtick or <c>&lt;</c>
	///   renders that character literally, so it never acts as markup; any other
	///   backslash is literal. <see cref="LogMarkup.Escape"/> escapes untrusted text.
	/// A line is prefixed with the current indent and every rendered line of it
	/// is capped at <see cref="MaxLineLength"/> characters by
	/// <see cref="LogMarkup.TruncateLine"/>.
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

		// Longest rendered line kept in full; owned by LogMarkup.
		public const int MaxLineLength = LogMarkup.MaxLineLength;

		// Appended to a line cut at MaxLineLength; owned by LogMarkup.
		public const string TruncationMarker = LogMarkup.TruncationMarker;

		private const int MinHeadingLevel = 1;
		private const int MaxHeadingLevel = 6;
		private const char HeadingMarker = '#';

		// Splits a message into lines, keeping the separators so they survive unchanged.
		private static readonly Regex LineSeparatorRegex = new Regex(@"(\r\n|\n)");

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
		/// Caps every line of the text at <see cref="MaxLineLength"/> with
		/// <see cref="LogMarkup.TruncateLine"/>; lines within the limit and the
		/// line separators are kept unchanged.
		/// </summary>
		private static string TruncateLines(string text)
		{
			string[] parts = LineSeparatorRegex.Split(text);

			if (parts.Length == 1)
			{
				return LogMarkup.TruncateLine(text);
			}

			var result = new StringBuilder(text.Length);

			foreach (string part in parts)
			{
				result.Append(LogMarkup.TruncateLine(part));
			}

			return result.ToString();
		}

	}
}
