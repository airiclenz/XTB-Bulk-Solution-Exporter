using System;
using System.Collections.Generic;
using System.IO;
using Com.AiricLenz.BulkSolutionExporter.Core.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;


// ============================================================================
// ============================================================================
// ============================================================================
namespace Com.AiricLenz.BulkSolutionExporter.Core.Tests
{

	// ============================================================================
	// ============================================================================
	// ============================================================================
	/// <summary>
	/// Covers <see cref="GitHelper.RunProcess"/>, which must drain both output
	/// streams while the process runs so large outputs cannot deadlock it, and
	/// the way <see cref="GitHelper"/> hands paths, branch names and commit
	/// messages to git without letting them become options.
	/// </summary>
	[TestClass]
	public class GitHelperTests
	{
		// Characters written to each stream: far above the OS pipe buffer.
		private const int StreamLength = 200000;

		// Upper bound for the run; a deadlocked read never finishes.
		private const int RunTimeoutInMilliseconds = 60000;

		private const string PowerShellExecutable = "powershell.exe";

		private const string ShellExecutable = "sh";

		private const string GitExecutable = "git";

		// Name of the file committed by the temporary-repository tests; the
		// space makes the path need quoting.
		private const string CommittedFileName = "exported solution.zip";

		// Object id git gives a blob with no content: what "git hash-object
		// --stdin" prints when its standard input is at end of input.
		private const string EmptyBlobHash = "e69de29bb2d1d6434b8b29ae775ad8c2e48c5391";


		// ============================================================================
		[TestMethod]
		[Timeout(RunTimeoutInMilliseconds)]
		public void RunProcess_LargeOutputOnBothStreams_Completes()
		{
			// Arrange
			string fileName;
			string[] arguments;

			if (OperatingSystem.IsWindows())
			{
				fileName = PowerShellExecutable;
				arguments = new[]
				{
					"-NoProfile",
					"-Command",
					"[Console]::Error.Write('e'*" + StreamLength + "); " +
					"[Console]::Out.Write('o'*" + StreamLength + ")"
				};
			}
			else
			{
				fileName = ShellExecutable;
				arguments = new[]
				{
					"-c",
					"head -c " + StreamLength + " /dev/zero | tr '\\000' e >&2; " +
					"head -c " + StreamLength + " /dev/zero | tr '\\000' o"
				};
			}

			// Act
			GitHelper.RunProcess(
				fileName,
				arguments,
				null,
				null,
				null,
				out string standardOutput,
				out string standardError);

			// Assert
			Assert.AreEqual(StreamLength, standardOutput.Length, "stdout length");
			Assert.AreEqual(StreamLength, standardError.Length, "stderr length");
		}


		// ============================================================================
		[DataTestMethod]
		[DataRow("a b", "\"a b\"")]
		[DataRow("a\"b", "\"a\\\"b\"")]
		[DataRow("a\\", "\"a\\\\\"")]
		[DataRow("a\\\"b", "\"a\\\\\\\"b\"")]
		public void QuoteArgument_SpecialCharacters_SurviveTheWindowsParser(
			string argument,
			string expected)
		{
			// Act
			string quoted = GitHelper.QuoteArgument(argument);

			// Assert
			Assert.AreEqual(expected, quoted);
		}


		// ============================================================================
		[TestMethod]
		public void ParseBranchList_MarkersAndDetachedHead_ReturnsBranchNamesOnly()
		{
			// Arrange
			string output = "* main\n  dev\n+ wt\n  (HEAD detached at abc)";

			// Act
			List<string> branches = GitHelper.ParseBranchList(output);

			// Assert
			CollectionAssert.AreEqual(
				new[] { "main", "dev", "wt" },
				branches);
		}


		// ============================================================================
		[TestMethod]
		public void SwitchToBranch_NameStartingWithDash_IsRefused()
		{
			// Arrange
			var gitHelper = new GitHelper(Path.GetTempPath());

			// Act
			bool isSwitched = gitHelper.SwitchToBranch("--orphan", out string errorMessage);

			// Assert
			Assert.IsFalse(isSwitched);
			Assert.IsFalse(string.IsNullOrEmpty(errorMessage));
		}


		// ============================================================================
		[DataTestMethod]
		[DataRow("say \"hi\" --amend")]
		[DataRow("L\u00f6sung \u00e4")]
		[Timeout(RunTimeoutInMilliseconds)]
		public void Commit_MessageWithQuotesOrNonAscii_IsStoredVerbatim(
			string message)
		{
			string repositoryPath = CreateTemporaryRepository();

			try
			{
				// Arrange
				var gitHelper = new GitHelper(repositoryPath);
				string filePath = Path.Combine(repositoryPath, CommittedFileName);
				File.WriteAllText(filePath, message);

				// Act
				bool isAdded = gitHelper.AddFile(filePath, out string addError);
				bool isCommitted = gitHelper.Commit(message, out string commitError);

				// Assert
				Assert.IsTrue(isAdded, addError);
				Assert.IsTrue(isCommitted, commitError);

				string storedMessage = RunGit(repositoryPath, "log", "-1", "--format=%B");
				Assert.AreEqual(message, storedMessage.TrimEnd('\r', '\n'));
			}
			finally
			{
				DeleteDirectory(repositoryPath);
			}
		}


		// ============================================================================
		/// <summary>
		/// A child started without input must read end of input, never the
		/// test host's own standard input: the PPTB worker's stdin carries its
		/// JSON-RPC protocol. Run with data piped into the test command (or
		/// from a terminal) an inherited stdin makes git hash that data or
		/// block until the timeout.
		/// </summary>
		[TestMethod]
		[Timeout(RunTimeoutInMilliseconds)]
		public void RunProcess_NoStandardInput_ChildReadsEndOfInput()
		{
			// Act
			int exitCode = GitHelper.RunProcess(
				GitExecutable,
				new[] { "hash-object", "--stdin" },
				Path.GetTempPath(),
				null,
				null,
				out string standardOutput,
				out string standardError);

			// Assert
			Assert.AreEqual(0, exitCode, standardError);
			Assert.AreEqual(EmptyBlobHash, standardOutput.Trim());
		}


		// ============================================================================
		/// <summary>
		/// Creates an empty repository in a new temporary folder, configured
		/// locally so a commit needs no global identity and no signing key.
		/// </summary>
		private static string CreateTemporaryRepository()
		{
			string repositoryPath = Path.Combine(
				Path.GetTempPath(),
				"GitHelperTests " + Guid.NewGuid().ToString("N"));

			Directory.CreateDirectory(repositoryPath);

			RunGit(repositoryPath, "init");
			RunGit(repositoryPath, "config", "user.name", "GitHelper Tests");
			RunGit(repositoryPath, "config", "user.email", "githelper.tests@example.invalid");
			RunGit(repositoryPath, "config", "commit.gpgsign", "false");

			return repositoryPath;
		}


		// ============================================================================
		private static string RunGit(
			string repositoryPath,
			params string[] arguments)
		{
			int exitCode = GitHelper.RunProcess(
				GitExecutable,
				arguments,
				repositoryPath,
				null,
				null,
				out string standardOutput,
				out string standardError);

			Assert.AreEqual(0, exitCode, "git " + string.Join(" ", arguments) + ": " + standardError);
			return standardOutput;
		}


		// ============================================================================
		/// <summary>
		/// Deletes the folder recursively; git marks its object files read-only,
		/// which <see cref="Directory.Delete(string, bool)"/> alone refuses.
		/// </summary>
		private static void DeleteDirectory(
			string directoryPath)
		{
			if (!Directory.Exists(directoryPath))
			{
				return;
			}

			foreach (string filePath in Directory.GetFiles(directoryPath, "*", SearchOption.AllDirectories))
			{
				File.SetAttributes(filePath, FileAttributes.Normal);
			}

			Directory.Delete(directoryPath, true);
		}


	}
}
