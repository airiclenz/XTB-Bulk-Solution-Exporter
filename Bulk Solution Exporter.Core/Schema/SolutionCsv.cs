using System;
using System.Collections.Generic;
using System.IO;
using System.Text;


// ============================================================================
// ============================================================================
// ============================================================================
namespace Com.AiricLenz.BulkSolutionExporter.Core.Schema
{

	// ============================================================================
	// ============================================================================
	// ============================================================================
	/// <summary>
	/// Writes the solution information CSV: a header line, then one line per
	/// solution with its unique name, id and version, each line ended by
	/// <see cref="Environment.NewLine"/>.
	/// </summary>
	public static class SolutionCsv
	{

		/// <summary>
		/// The CSV's first line.
		/// </summary>
		public const string Header = "Unique Name, Solution Guid, Version";

		private const string Separator = ",";
		private const string Quote = "\"";
		private const string EscapedQuote = "\"\"";

		// A field holding any of these is quoted, so a crafted unique name
		// cannot add columns or rows.
		private static readonly char[] CharactersNeedingQuotes = { ',', '"', '\r', '\n' };


		// ============================================================================
		/// <summary>
		/// Writes the CSV for the solutions, in the given order, to a file;
		/// an existing file is overwritten.
		/// </summary>
		/// <param name="filePath">The CSV file to write.</param>
		/// <param name="solutions">The solutions to list; <c>null</c> entries are skipped.</param>
		/// <exception cref="ArgumentNullException"><paramref name="filePath"/> or <paramref name="solutions"/> is null.</exception>
		/// <exception cref="IOException">The file cannot be written.</exception>
		/// <exception cref="UnauthorizedAccessException">Writing the file is not allowed.</exception>
		public static void Write(
			string filePath,
			IEnumerable<SolutionInfo> solutions)
		{
			if (filePath == null)
			{
				throw new ArgumentNullException(nameof(filePath));
			}

			File.WriteAllText(
				filePath,
				Build(solutions));
		}


		// ============================================================================
		/// <summary>
		/// Builds the CSV text for the solutions.
		/// </summary>
		/// <exception cref="ArgumentNullException"><paramref name="solutions"/> is null.</exception>
		private static string Build(
			IEnumerable<SolutionInfo> solutions)
		{
			if (solutions == null)
			{
				throw new ArgumentNullException(nameof(solutions));
			}

			var builder = new StringBuilder();
			builder.Append(Header + Environment.NewLine);

			foreach (var solution in solutions)
			{
				if (solution == null)
				{
					continue;
				}

				builder.Append(
					ToField(solution.UniqueName) + Separator +
					solution.SolutionId + Separator +
					ToField(solution.Version?.ToString()) + Environment.NewLine);
			}

			return builder.ToString();
		}


		// ============================================================================
		/// <summary>
		/// Returns the text as one CSV field: unchanged when it is plain,
		/// quoted (with doubled inner quotes) when it holds a separator, a
		/// quote or a line break; empty for <c>null</c>.
		/// </summary>
		private static string ToField(
			string text)
		{
			if (text == null)
			{
				return string.Empty;
			}

			if (text.IndexOfAny(CharactersNeedingQuotes) < 0)
			{
				return text;
			}

			return Quote + text.Replace(Quote, EscapedQuote) + Quote;
		}

	}
}
