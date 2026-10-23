using System.Windows.Forms;

// ============================================================================
// ============================================================================
// ============================================================================
namespace Com.AiricLenz.XTB.Plugin.Helpers
{

	// ============================================================================
	// ============================================================================
	// ============================================================================
	/// <summary>
	/// Renders finished log lines (built by RunLog) into a RichTextBox.
	/// </summary>
	internal class Logger
	{
		private MarkdownParser _markdownParser;


		// ============================================================================
		public Logger(RichTextBox richTextBox)
		{
			_markdownParser = new MarkdownParser(richTextBox);
		}

		// ============================================================================
		public void Log(string line)
		{
			_markdownParser.ParseAndAppend(line);
		}


	}
}
