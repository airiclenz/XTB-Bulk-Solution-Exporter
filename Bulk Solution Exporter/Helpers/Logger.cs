using System.Windows.Forms;
using Com.AiricLenz.BulkSolutionExporter.Core.Extensions;

// ============================================================================
// ============================================================================
// ============================================================================
namespace Com.AiricLenz.XTB.Plugin.Helpers
{

	// ============================================================================
	// ============================================================================
	// ============================================================================
	internal class Logger
	{
		private MarkdownParser _markdownParser;
		private string _indent = "|   ";
		private int _indentLevel = 0;


		// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		public string Indent
		{
			get => _indent;
			set => _indent = value;
		}


		// ============================================================================
		public Logger(RichTextBox richTextBox)
		{
			_markdownParser = new MarkdownParser(richTextBox);
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
		public void Log(string message)
		{
			Log(message, GenerateIndent());
		}

		// ============================================================================
		/// <summary>
		/// Appends a message with an indent the caller captured beforehand
		/// (via <see cref="GetCurrentIndent"/>), so a line marshalled to the UI
		/// thread keeps the indent level of the thread that logged it.
		/// </summary>
		public void Log(
			string message,
			string indent)
		{
			if (message.IsEmpty())
			{
				_markdownParser.ParseAndAppend(string.Empty);
			}
			else
			{
				_markdownParser.ParseAndAppend(indent + message);
			}

		}

		// ============================================================================
		public string GetCurrentIndent()
		{
			return GenerateIndent();
		}

		// ============================================================================
		private string GenerateIndent()
		{
			var resultIndent = string.Empty;

			for (int i = 0; i < _indentLevel; i++)
			{
				resultIndent += _indent;
			}

			return resultIndent;
		}


	}
}
