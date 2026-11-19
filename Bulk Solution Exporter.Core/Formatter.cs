using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using Com.AiricLenz.BulkSolutionExporter.Core.Run;

// ============================================================================
// ============================================================================
// ============================================================================
namespace Com.AiricLenz.BulkSolutionExporter.Core
{

	// ============================================================================
	public class Formatter
	{
		// Longest error message reformatted; a longer one is only escaped
		// so the regex scan and XML parse stay bounded. Not below
		// MarkdownParser.MaxLogLineLength, so every line the log can show in
		// full is still formatted.
		public const int MaxFormattableErrorLength = 100000;


		// ============================================================================
		/// <summary>
		/// Pretty-prints the missing-dependencies XML of an import error and
		/// breaks its lead-in sentences onto their own lines. The message is
		/// untrusted server text: every return path passes it through
		/// <see cref="LogMarkup.Escape"/>, so it never acts as log markup, while
		/// <paramref name="currentIndent"/> is inserted after the escape and keeps
		/// its own markup. Messages longer than
		/// <see cref="MaxFormattableErrorLength"/> are only escaped, not reformatted.
		/// </summary>
		public static string FormatErrorStringWithXml(
			string errorString,
			string currentIndent = "")
		{
			if (errorString.Length > MaxFormattableErrorLength)
			{
				return LogMarkup.Escape(errorString);
			}

			// Use a regex to extract the XML portion
			var match = Regex.Match(
				errorString,
				@"(<MissingDependencies>.*?</MissingDependencies>)",
				RegexOptions.Singleline);

			if (!match.Success)
			{
				// If no match, just return the original string
				return LogMarkup.Escape(errorString);
			}

			string xmlContent = match.Groups[1].Value;

			string formattedXml;
			try
			{
				// Parse the XML and reformat
				var doc = XDocument.Parse(xmlContent);

				var sb = new StringBuilder();
				var settings = new XmlWriterSettings
				{
					Indent = true,
					OmitXmlDeclaration = true
				};

				using (var writer = XmlWriter.Create(sb, settings))
				{
					doc.WriteTo(writer);
				}

				formattedXml = sb.ToString();
			}
			catch (Exception)
			{
				// If something goes wrong, fallback to original XML segment
				formattedXml = xmlContent;
			}

			// Replace the original XML snippet with the formatted version
			string result =
				LogMarkup.Escape(
					errorString.Replace(xmlContent, formattedXml));

			result = result.Replace(
				"Some dependencies are missing. ",
				"Some dependencies are missing." + Environment.NewLine + currentIndent);

			result = result.Replace(
				"The missing dependencies are :",
				"The missing dependencies are:" + Environment.NewLine + currentIndent);

			return result;
		}
	}

}

