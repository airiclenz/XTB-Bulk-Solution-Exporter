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
		// Executable started for every git command; resolved through PATH.
		private const string GitExecutable = "git";

		// Set to "0" so git fails instead of waiting for a credential prompt.
		private const string GitTerminalPromptVariable = "GIT_TERMINAL_PROMPT";
		private const string GitTerminalPromptDisabled = "0";


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
		/// <summary>
		/// Runs git with the given arguments in <see cref="GitRootDirectory"/>.
		/// Git never prompts for credentials: a command that would need them
		/// fails instead of hanging the caller.
		/// </summary>
		/// <param name="output">Everything git wrote to standard output.</param>
		/// <param name="errorMessage">
		/// Everything git wrote to standard error; empty when git succeeded.
		/// </param>
		/// <returns><c>true</c> when git exited with code 0.</returns>
		public bool ExecuteCommand(
			string arguments,
			out string output,
			out string errorMessage)
		{
			var environmentVariables = new Dictionary<string, string>
			{
				{ GitTerminalPromptVariable, GitTerminalPromptDisabled }
			};

			int exitCode = RunProcess(
				GitExecutable,
				arguments,
				GitRootDirectory,
				environmentVariables,
				out output,
				out errorMessage);

			if (exitCode == 0)
			{
				errorMessage = string.Empty;
				return true;
			}

			return false;
		}


		// ============================================================================
		/// <summary>
		/// Runs a process to completion and returns its exit code together with
		/// its complete standard output and standard error. Both streams are
		/// read while the process runs, so a process that writes more than the
		/// pipe buffer holds cannot block on a full pipe.
		/// </summary>
		/// <param name="environmentVariables">
		/// Variables added to the inherited environment; may be <c>null</c>.
		/// </param>
		/// <returns>The process exit code.</returns>
		internal static int RunProcess(
			string fileName,
			string arguments,
			string workingDirectory,
			IDictionary<string, string> environmentVariables,
			out string standardOutput,
			out string standardError)
		{
			ProcessStartInfo processStartInfo = new ProcessStartInfo()
			{
				FileName = fileName,
				Arguments = arguments,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				UseShellExecute = false,
				CreateNoWindow = true,
				WorkingDirectory = workingDirectory
			};

			if (environmentVariables != null)
			{
				foreach (var variable in environmentVariables)
				{
					processStartInfo.EnvironmentVariables[variable.Key] = variable.Value;
				}
			}

			using (Process process = Process.Start(processStartInfo))
			{
				// Both reads must be running before the wait: a process blocked
				// on a full pipe never exits.
				Task<string> outputTask = process.StandardOutput.ReadToEndAsync();
				Task<string> errorTask = process.StandardError.ReadToEndAsync();

				process.WaitForExit();

				standardOutput = outputTask.GetAwaiter().GetResult();
				standardError = errorTask.GetAwaiter().GetResult();
				return process.ExitCode;
			}
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
