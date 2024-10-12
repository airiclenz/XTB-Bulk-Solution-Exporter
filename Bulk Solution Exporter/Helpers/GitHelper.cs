using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// ============================================================================
// ============================================================================
// ============================================================================
namespace Com.AiricLenz.XTB.Plugin.Helpers
{

	// ============================================================================
	// ============================================================================
	// ============================================================================
	internal class GitHelper
	{

		// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		public string GitRootDirectory
		{ get; set; }


		// ============================================================================
		public GitHelper(
			string gotRootDirectory)
		{
			GitRootDirectory = gotRootDirectory;
		}


		// ============================================================================
		public bool ExecuteCommand(
			string arguments,
			out string errorMessage)
		{
			ProcessStartInfo processStartInfo = new ProcessStartInfo()
			{
				FileName = "git",
				Arguments = arguments,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				UseShellExecute = false,
				CreateNoWindow = true,
				WorkingDirectory = GitRootDirectory
			};

			using (Process process = Process.Start(processStartInfo))
			{
				process.WaitForExit();

				string output = process.StandardOutput.ReadToEnd();
				string error = process.StandardError.ReadToEnd();
				int exitCode = process.ExitCode;

				if (exitCode == 0)
				{
					errorMessage = string.Empty;
					return true;
				}
				else
				{
					//Console.WriteLine($"Error: {error}");
					//Console.WriteLine($"Exit Code: {exitCode}");

					errorMessage = error;
					return false;
				}
			};
		}
	}
}
