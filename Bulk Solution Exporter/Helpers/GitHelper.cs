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
			out string output,
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

				output = process.StandardOutput.ReadToEnd();
				errorMessage = process.StandardError.ReadToEnd();
				int exitCode = process.ExitCode;

				if (exitCode == 0)
				{
					errorMessage = string.Empty;
					return true;
				}
				
				return false;
			};
		}

		// ============================================================================
		public List<string> GetAllBranches()
		{
			if (ExecuteCommand("branch", out string output, out _))
			{
				List<string> branches = output
					.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
					.Select(branch => branch.Trim().TrimStart('*').Trim())
					.ToList();

				return branches;
			}
			
			return new List<string>();
		}


		// ============================================================================
		public string GetActiveBranch()
		{
			if (ExecuteCommand("branch", out string output, out _))
			{
				string activeBranch = output
					.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
					.FirstOrDefault(branch => branch.Trim().StartsWith("*"));

				if (activeBranch != null)
				{
					return activeBranch.Trim().TrimStart('*').Trim();
				}
				else
				{
					return string.Empty;
				}
			}
			
			return string.Empty;
		}

		// ============================================================================
		public bool SwitchToBranch(
			string branchName,
			out string errorMessage
			)
		{
			string command = $"checkout {branchName}";
			return ExecuteCommand(command, out _, out errorMessage);
		}


	}
}
