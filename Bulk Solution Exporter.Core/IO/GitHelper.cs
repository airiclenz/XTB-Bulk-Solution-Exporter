using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// ============================================================================
// ============================================================================
// ============================================================================
namespace Com.AiricLenz.BulkSolutionExporter.Core.IO
{

	// ============================================================================
	// ============================================================================
	// ============================================================================
	public class GitHelper
	{
		// Executable started for every git command; resolved through PATH.
		private const string GitExecutable = "git";

		// Set to "0" so git fails instead of waiting for a credential prompt.
		private const string GitTerminalPromptVariable = "GIT_TERMINAL_PROMPT";
		private const string GitTerminalPromptDisabled = "0";

		// "git branch" prefixes every line with a two-character marker column:
		// "* " for the checked-out branch, "+ " for a branch checked out in
		// another worktree and two spaces for every other branch.
		private const int BranchMarkerWidth = 2;
		private const char ActiveBranchMarker = '*';
		private const char WorktreeBranchMarker = '+';

		// "git branch" lists a detached HEAD as "(HEAD detached at ...)"; no
		// real branch name starts with this character.
		private const string DetachedHeadPrefix = "(";

		// Git reads any argument starting with this prefix as an option.
		private const string OptionPrefix = "-";

		// Separates options and revisions from paths on a git command line.
		private const string EndOfOptions = "--";

		// Separates the quoted arguments of a .NET Framework command line.
		private const string ArgumentSeparator = " ";

		// Commit messages travel on stdin: git stores them byte for byte, so
		// they must be UTF-8 and must not start with a byte order mark.
		private static readonly Encoding StandardInputEncoding = new UTF8Encoding(false);


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
		/// <param name="arguments">
		/// One entry per git argument, passed to git unchanged.
		/// </param>
		/// <param name="standardInput">
		/// Text handed to git on standard input; <c>null</c> for none.
		/// </param>
		/// <param name="output">Everything git wrote to standard output.</param>
		/// <param name="errorMessage">
		/// Everything git wrote to standard error; empty when git succeeded.
		/// </param>
		/// <returns><c>true</c> when git exited with code 0.</returns>
		private bool ExecuteCommand(
			IReadOnlyList<string> arguments,
			string standardInput,
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
				standardInput,
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
		/// <param name="arguments">
		/// One entry per argument; each reaches the process unchanged, with no
		/// shell or caller-side quoting involved.
		/// </param>
		/// <param name="environmentVariables">
		/// Variables added to the inherited environment; may be <c>null</c>.
		/// </param>
		/// <param name="standardInput">
		/// Text written to the process's standard input as UTF-8 without a byte
		/// order mark, after which the input is closed; <c>null</c> closes it
		/// at once. The input is always redirected, so the process reads end of
		/// input and never the caller's own standard input (the PPTB worker
		/// speaks JSON-RPC on it).
		/// </param>
		/// <returns>The process exit code.</returns>
		/// <exception cref="ArgumentNullException">
		/// <paramref name="arguments"/> or one of its entries is <c>null</c>.
		/// </exception>
		public static int RunProcess(
			string fileName,
			IReadOnlyList<string> arguments,
			string workingDirectory,
			IDictionary<string, string> environmentVariables,
			string standardInput,
			out string standardOutput,
			out string standardError)
		{
			if (arguments == null ||
				arguments.Contains(null))
			{
				throw new ArgumentNullException(nameof(arguments));
			}

			ProcessStartInfo processStartInfo = new ProcessStartInfo()
			{
				FileName = fileName,
				RedirectStandardInput = true,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				UseShellExecute = false,
				CreateNoWindow = true,
				WorkingDirectory = workingDirectory
			};

			SetArguments(processStartInfo, arguments);

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

				if (standardInput != null)
				{
					WriteStandardInput(process, standardInput);
				}
				else
				{
					// End of input at once: an inherited stdin could hand the
					// process bytes that belong to the caller.
					process.StandardInput.Close();
				}

				process.WaitForExit();

				standardOutput = outputTask.GetAwaiter().GetResult();
				standardError = errorTask.GetAwaiter().GetResult();
				return process.ExitCode;
			}
		}

		// ============================================================================
		public List<string> GetAllBranches()
		{
			if (ExecuteCommand(new[] { "branch" }, null, out string output, out _))
			{
				return ParseBranchList(output);
			}
			
			return new List<string>();
		}


		// ============================================================================
		public string GetActiveBranch()
		{
			if (ExecuteCommand(new[] { "branch" }, null, out string output, out _))
			{
				foreach (string line in SplitLines(output))
				{
					string branchName = ParseBranchLine(line, out bool isActive);

					if (isActive &&
						branchName != null)
					{
						return branchName;
					}
				}
			}
			
			return string.Empty;
		}

		// ============================================================================
		/// <summary>
		/// Checks out a branch. The name is passed as its own argument and
		/// followed by "--", so git reads it as a branch and never as a path; a name that git would read
		/// as an option is refused without running git.
		/// </summary>
		/// <returns><c>true</c> when git switched to the branch.</returns>
		public bool SwitchToBranch(
			string branchName,
			out string errorMessage
			)
		{
			if (string.IsNullOrEmpty(branchName) ||
				branchName.StartsWith(OptionPrefix, StringComparison.Ordinal))
			{
				errorMessage = "Refusing to check out the invalid branch name '" + branchName + "'.";
				return false;
			}

			var arguments = new[] { "checkout", branchName, EndOfOptions };
			return ExecuteCommand(arguments, null, out _, out errorMessage);
		}


		// ============================================================================
		/// <summary>
		/// Stages one file. The path is passed as its own argument after "--",
		/// so it is never read as an option or a revision.
		/// </summary>
		/// <param name="absolutePath">The full path of a file inside the repository.</param>
		/// <returns><c>true</c> when git staged the file.</returns>
		/// <exception cref="ArgumentNullException"><paramref name="absolutePath"/> is <c>null</c>.</exception>
		public bool AddFile(
			string absolutePath,
			out string errorMessage)
		{
			if (absolutePath == null)
			{
				throw new ArgumentNullException(nameof(absolutePath));
			}

			var arguments = new[] { "add", EndOfOptions, absolutePath };
			return ExecuteCommand(arguments, null, out _, out errorMessage);
		}


		// ============================================================================
		/// <summary>
		/// Commits the staged changes. The message is handed to git on standard
		/// input ("commit -F -"), so it never becomes part of the command line.
		/// </summary>
		/// <returns><c>true</c> when git created the commit.</returns>
		/// <exception cref="ArgumentNullException"><paramref name="message"/> is <c>null</c>.</exception>
		public bool Commit(
			string message,
			out string errorMessage)
		{
			if (message == null)
			{
				throw new ArgumentNullException(nameof(message));
			}

			return ExecuteCommand(new[] { "commit", "-F", "-" }, message, out _, out errorMessage);
		}


		// ============================================================================
		/// <summary>
		/// Pushes the checked-out branch to its upstream.
		/// </summary>
		/// <returns><c>true</c> when git pushed successfully.</returns>
		public bool Push(
			out string errorMessage)
		{
			return ExecuteCommand(new[] { "push" }, null, out _, out errorMessage);
		}


		// ============================================================================
		/// <summary>
		/// Quotes one argument so that the Windows command-line parser
		/// (CommandLineToArgvW rules) hands it to the process unchanged.
		/// <see cref="RunProcess"/> builds its .NET Framework command line with it.
		/// </summary>
		/// <returns>The argument wrapped in double quotes, with every embedded
		/// quote and every backslash run preceding a quote escaped.</returns>
		/// <exception cref="ArgumentNullException"><paramref name="argument"/> is <c>null</c>.</exception>
		public static string QuoteArgument(
			string argument)
		{
			if (argument == null)
			{
				throw new ArgumentNullException(nameof(argument));
			}

			var quoted = new StringBuilder();
			quoted.Append('"');

			int pendingBackslashes = 0;

			foreach (char character in argument)
			{
				if (character == '\\')
				{
					pendingBackslashes++;
					continue;
				}

				if (character == '"')
				{
					// Backslashes before a quote are halved by the parser, and
					// one more escapes the quote itself.
					quoted.Append('\\', pendingBackslashes * 2 + 1);
				}
				else
				{
					quoted.Append('\\', pendingBackslashes);
				}

				quoted.Append(character);
				pendingBackslashes = 0;
			}

			// The closing quote follows these backslashes, so they are doubled.
			quoted.Append('\\', pendingBackslashes * 2);
			quoted.Append('"');

			return quoted.ToString();
		}


		// ============================================================================
		/// <summary>
		/// Extracts the branch names from the output of "git branch", without
		/// their marker column and without detached-HEAD entries.
		/// </summary>
		public static List<string> ParseBranchList(
			string output)
		{
			return SplitLines(output)
				.Select(line => ParseBranchLine(line, out _))
				.Where(branchName => branchName != null)
				.ToList();
		}


		// ============================================================================
		/// <summary>
		/// Hands every argument to the process unchanged. .NET 5 and later
		/// fill <see cref="ProcessStartInfo"/>.ArgumentList, which quotes for
		/// the current platform; .NET Framework has only the single
		/// command-line string, so each argument is quoted for the Windows
		/// parser and joined with spaces.
		/// </summary>
		private static void SetArguments(
			ProcessStartInfo processStartInfo,
			IReadOnlyList<string> arguments)
		{
#if NET
			foreach (string argument in arguments)
			{
				processStartInfo.ArgumentList.Add(argument);
			}
#else
			processStartInfo.Arguments =
				string.Join(
					ArgumentSeparator,
					arguments.Select(QuoteArgument));
#endif
		}


		// ============================================================================
		/// <summary>
		/// Writes the text to the process's standard input and closes it. The
		/// raw stream is used because the <see cref="Process.StandardInput"/>
		/// writer encodes with the console input encoding, which is not UTF-8
		/// inside XrmToolBox and adds a byte order mark under a UTF-8 console.
		/// </summary>
		private static void WriteStandardInput(
			Process process,
			string text)
		{
			Stream inputStream = process.StandardInput.BaseStream;

			try
			{
				byte[] bytes = StandardInputEncoding.GetBytes(text);
				inputStream.Write(bytes, 0, bytes.Length);
				inputStream.Flush();
			}
			finally
			{
				// Closing signals end of input; the process may still be
				// waiting for it.
				process.StandardInput.Close();
			}
		}


		// ============================================================================
		private static IEnumerable<string> SplitLines(
			string output)
		{
			return output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
		}


		// ============================================================================
		/// <summary>
		/// Reads one line of "git branch" output.
		/// </summary>
		/// <param name="isActive"><c>true</c> when the line marks the checked-out entry.</param>
		/// <returns>
		/// The branch name, or <c>null</c> when the line names no branch (a
		/// detached HEAD or an empty line).
		/// </returns>
		private static string ParseBranchLine(
			string line,
			out bool isActive)
		{
			string branchName = line;
			isActive = false;

			if (line.Length > BranchMarkerWidth &&
				line[BranchMarkerWidth - 1] == ' ')
			{
				char marker = line[0];

				if (marker == ' ' ||
					marker == ActiveBranchMarker ||
					marker == WorktreeBranchMarker)
				{
					isActive = marker == ActiveBranchMarker;
					branchName = line.Substring(BranchMarkerWidth);
				}
			}

			branchName = branchName.Trim();

			if (branchName.Length == 0 ||
				branchName.StartsWith(DetachedHeadPrefix, StringComparison.Ordinal))
			{
				return null;
			}

			return branchName;
		}


	}
}
