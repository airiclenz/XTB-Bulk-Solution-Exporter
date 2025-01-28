using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

// ============================================================================
// ============================================================================
// ============================================================================
namespace Com.AiricLenz.XTB.Plugin.Helpers
{

	// ============================================================================
	internal class Formatter
	{

		// ============================================================================
		public static string FormatErrorStringWithXml(
			string errorString,
			string currentIndent = "")
		{
			// Use a regex to extract the XML portion
			var match = Regex.Match(
				errorString,
				@"(<MissingDependencies>.*?</MissingDependencies>)",
				RegexOptions.Singleline);

			if (!match.Success)
			{
				// If no match, just return the original string
				return errorString;
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
			string result = errorString.Replace(xmlContent, formattedXml);
			
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

