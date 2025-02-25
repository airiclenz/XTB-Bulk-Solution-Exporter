using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Com.AiricLenz.Extentions;
using Microsoft.Crm.Sdk.Messages;

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
		public void Log(string message)
		{
			if (message.IsEmpty())
			{
				_markdownParser.ParseAndAppend(string.Empty);
			}
			else
			{
				var indent = GenerateIndent();
				_markdownParser.ParseAndAppend(indent + message);
			}
			
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
