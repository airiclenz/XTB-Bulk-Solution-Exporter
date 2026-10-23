using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Com.AiricLenz.BulkSolutionExporter.Core.IO;
using Com.AiricLenz.BulkSolutionExporter.Core.Run;
using Com.AiricLenz.BulkSolutionExporter.Core.Schema;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nerdbank.Streams;
using Newtonsoft.Json;
using StreamJsonRpc;
using Version = Com.AiricLenz.BulkSolutionExporter.Core.Version;


// ============================================================================
// ============================================================================
// ============================================================================
namespace Com.AiricLenz.BulkSolutionExporter.PptbWorker.Tests
{

	// ============================================================================
	// ============================================================================
	// ============================================================================
	/// <summary>
	/// Drives the worker's workspace methods (git branches, export file
	/// status, solution CSV, wizard file names) over an in-process duplex
	/// pipe. Files and repositories live in a per-test temp directory.
	/// </summary>
	[TestClass]
	public class WorkspaceMethodsTests
	{

		private const string SolutionName = "SolutionA";
		private const string CurrentVersion = "1.0.0.2";
		private const string OlderVersion = "1.0.0.1";
		private const string InitialBranch = "main";
		private const string OtherBranch = "feature";
		private const string GitExecutable = "git";
		private const string CsvFileName = "Solution Information.csv";
		private const int RunTimeoutInMilliseconds = 60000;

		private string _tempDirectory;
		private JsonRpc _workerRpc;
		private JsonRpc _client;


		// ============================================================================
		[TestInitialize]
		public void Connect()
		{
			_tempDirectory =
				Path.Combine(
					Path.GetTempPath(),
					Path.GetRandomFileName());

			Directory.CreateDirectory(_tempDirectory);

			var (workerStream, clientStream) = FullDuplexStream.CreatePair();
			_workerRpc = WorkerMethods.Attach(workerStream, workerStream);

			_client =
				new JsonRpc(
					new HeaderDelimitedMessageHandler(
						clientStream,
						clientStream,
						WorkerMethods.CreateFormatter()));

			_client.StartListening();
		}


		// ============================================================================
		[TestCleanup]
		public void Disconnect()
		{
			_client.Dispose();
			_workerRpc.Dispose();

			if (!Directory.Exists(_tempDirectory))
			{
				return;
			}

			// git marks its object files read-only, which Directory.Delete refuses.
			foreach (var filePath in Directory.GetFiles(_tempDirectory, "*", SearchOption.AllDirectories))
			{
				File.SetAttributes(filePath, FileAttributes.Normal);
			}

			Directory.Delete(_tempDirectory, true);
		}


		// ============================================================================
		[TestMethod]
		[Timeout(RunTimeoutInMilliseconds)]
		public async Task GetBranches_FileInRepository_ListsBranchesAndActiveBranch()
		{
			// Arrange
			CreateRepositoryWithTwoBranches();

			// Act
			var branches =
				await _client.InvokeWithParameterObjectAsync<GitBranches>(
					"git/branches",
					new { filePath = ManagedFilePath() });

			// Assert
			Assert.AreEqual(Path.GetFullPath(_tempDirectory), Path.GetFullPath(branches.RepositoryRoot));
			Assert.AreEqual(InitialBranch, branches.ActiveBranch);
			CollectionAssert.AreEquivalent(new[] { InitialBranch, OtherBranch }, branches.Branches);
		}


		// ============================================================================
		[TestMethod]
		public async Task GetBranches_FileOutsideRepository_ReturnsEmptyAnswer()
		{
			var branches =
				await _client.InvokeWithParameterObjectAsync<GitBranches>(
					"git/branches",
					new { filePath = ManagedFilePath() });

			Assert.IsNull(branches.RepositoryRoot);
			Assert.AreEqual(0, branches.Branches.Count);
		}


		// ============================================================================
		[TestMethod]
		[Timeout(RunTimeoutInMilliseconds)]
		public async Task SwitchBranch_LocalBranch_ChecksItOut()
		{
			// Arrange
			CreateRepositoryWithTwoBranches();

			// Act
			var branches =
				await _client.InvokeWithParameterObjectAsync<GitBranches>(
					"git/switchBranch",
					new { filePath = ManagedFilePath(), branchName = OtherBranch });

			// Assert
			Assert.AreEqual(OtherBranch, branches.ActiveBranch);
		}


		// ============================================================================
		[TestMethod]
		[Timeout(RunTimeoutInMilliseconds)]
		public async Task SwitchBranch_UnknownBranch_ReturnsErrorAndStaysOnBranch()
		{
			// Arrange
			CreateRepositoryWithTwoBranches();

			// Act
			await Assert.ThrowsExceptionAsync<RemoteInvocationException>(
				() => _client.InvokeWithParameterObjectAsync<GitBranches>(
					"git/switchBranch",
					new { filePath = ManagedFilePath(), branchName = "--orphan" }));

			// Assert
			Assert.AreEqual(InitialBranch, RunGit("branch", "--show-current").Trim());
		}


		// ============================================================================
		[DataTestMethod]
		[DataRow(true, OlderVersion, true, DisplayName = "recorded version differs")]
		[DataRow(true, CurrentVersion, false, DisplayName = "recorded version matches")]
		[DataRow(false, CurrentVersion, true, DisplayName = "file missing")]
		[DataRow(true, null, false, DisplayName = "present but unrecorded")]
		public async Task GetFileStatus_VersionJsonEnabled_FollowsStalenessRules(
			bool isFilePresent,
			string recordedVersion,
			bool isOutdatedExpected)
		{
			// Arrange
			PrepareManagedFile(isFilePresent, recordedVersion);

			// Act
			var status = await GetManagedFileStatusAsync(true);

			// Assert
			Assert.AreEqual(isOutdatedExpected, status.IsOutdated);
		}


		// ============================================================================
		[TestMethod]
		public async Task GetFileStatus_RecordedVersion_ReportsExportedVersion()
		{
			PrepareManagedFile(true, OlderVersion);

			var status = await GetManagedFileStatusAsync(true);

			Assert.AreEqual(OlderVersion, status.ExportedVersion);
		}


		// ============================================================================
		[DataTestMethod]
		[DataRow(true, OlderVersion, DisplayName = "recorded version differs")]
		[DataRow(false, null, DisplayName = "file missing")]
		public async Task GetFileStatus_VersionJsonDisabled_IsNeverOutdated(
			bool isFilePresent,
			string recordedVersion)
		{
			// Arrange
			PrepareManagedFile(isFilePresent, recordedVersion);

			// Act
			var status = await GetManagedFileStatusAsync(false);

			// Assert
			Assert.IsFalse(status.IsOutdated);
			Assert.IsNull(status.ExportedVersion);
		}


		// ============================================================================
		[TestMethod]
		public async Task GetFileStatus_UnmanagedNotConfigured_ReportsUnconfiguredAndCurrent()
		{
			// Arrange
			var entry = CreateRunSolution();
			entry.Configuration.FileNameUnmanaged = null;

			// Act
			var statuses = await GetFileStatusAsync(entry, true);

			// Assert
			Assert.IsFalse(statuses[0].Unmanaged.IsConfigured);
			Assert.IsFalse(statuses[0].Unmanaged.IsOutdated);
		}


		// ============================================================================
		[TestMethod]
		public async Task ExportCsv_TwoSolutions_WritesPinnedContent()
		{
			// Arrange
			var first = CreateSolution(SolutionName, CurrentVersion);
			var second = CreateSolution("SolutionB", OlderVersion);
			var filePath = Path.Combine(_tempDirectory, CsvFileName);

			// Act
			await _client.InvokeWithParameterObjectAsync(
				"files/exportCsv",
				new { filePath, solutions = new[] { first, second } });

			// Assert
			var expected =
				"Unique Name, Solution Guid, Version" + Environment.NewLine +
				SolutionName + "," + first.SolutionId + "," + CurrentVersion + Environment.NewLine +
				"SolutionB," + second.SolutionId + "," + OlderVersion + Environment.NewLine;

			Assert.AreEqual(expected, File.ReadAllText(filePath));
		}


		// ============================================================================
		[TestMethod]
		public async Task ExportCsv_NameWithSeparatorAndLineBreak_IsQuoted()
		{
			// Arrange
			var solution = CreateSolution("Evil,\"Name\"\nInjected", CurrentVersion);
			var filePath = Path.Combine(_tempDirectory, CsvFileName);

			// Act
			await _client.InvokeWithParameterObjectAsync(
				"files/exportCsv",
				new { filePath, solutions = new[] { solution } });

			// Assert
			var expected =
				"Unique Name, Solution Guid, Version" + Environment.NewLine +
				"\"Evil,\"\"Name\"\"\nInjected\"," + solution.SolutionId + "," + CurrentVersion + Environment.NewLine;

			Assert.AreEqual(expected, File.ReadAllText(filePath));
		}


		// ============================================================================
		[DataTestMethod]
		[DataRow("..", DisplayName = "parent segment")]
		[DataRow("relative", DisplayName = "relative path")]
		public async Task ExportCsv_TraversalPath_IsRefused(
			string pathKind)
		{
			// Arrange
			var filePath =
				pathKind == ".." ?
				Path.Combine(_tempDirectory, "..", CsvFileName) :
				Path.Combine("..", CsvFileName);

			// Act
			await Assert.ThrowsExceptionAsync<RemoteInvocationException>(
				() => _client.InvokeWithParameterObjectAsync(
					"files/exportCsv",
					new { filePath, solutions = new[] { CreateSolution(SolutionName, CurrentVersion) } }));

			// Assert
			Assert.IsFalse(File.Exists(Path.Combine(Path.GetDirectoryName(_tempDirectory), CsvFileName)));
		}


		// ============================================================================
		[TestMethod]
		public async Task SuggestFileNames_PlainName_ProposesWizardNamesInFolder()
		{
			var suggestions =
				await _client.InvokeWithParameterObjectAsync<List<SuggestedFileNames>>(
					"files/suggestFileNames",
					new { folderPath = _tempDirectory, uniqueNames = new[] { "My  #Solution" } });

			Assert.AreEqual(Path.Combine(_tempDirectory, "Managed.My Solution.zip"), suggestions[0].FileNameManaged);
			Assert.AreEqual(Path.Combine(_tempDirectory, "Unmanaged.My Solution.zip"), suggestions[0].FileNameUnmanaged);
		}


		// ============================================================================
		[DataTestMethod]
		[DataRow("../../evil")]
		[DataRow("..\\..\\evil")]
		[DataRow("/etc/passwd")]
		public async Task SuggestFileNames_TraversalUniqueName_StaysInFolder(
			string uniqueName)
		{
			var suggestions =
				await _client.InvokeWithParameterObjectAsync<List<SuggestedFileNames>>(
					"files/suggestFileNames",
					new { folderPath = _tempDirectory, uniqueNames = new[] { uniqueName } });

			Assert.IsTrue(FileAndFolderHelper.IsInsideFolder(suggestions[0].FileNameManaged, _tempDirectory));
			Assert.AreEqual(_tempDirectory, Path.GetDirectoryName(suggestions[0].FileNameManaged));
		}


		// ============================================================================
		[TestMethod]
		public async Task SuggestFileNames_FolderWithParentSegment_IsRefused()
		{
			await Assert.ThrowsExceptionAsync<RemoteInvocationException>(
				() => _client.InvokeWithParameterObjectAsync<List<SuggestedFileNames>>(
					"files/suggestFileNames",
					new { folderPath = Path.Combine(_tempDirectory, ".."), uniqueNames = new[] { SolutionName } }));
		}


		// ============================================================================
		/// <summary>
		/// Places the managed export file and records its version in the
		/// version file, as configured by the arguments.
		/// </summary>
		/// <param name="isFilePresent">Whether the export file exists.</param>
		/// <param name="recordedVersion">The version the version file records; <c>null</c> for no version file.</param>
		private void PrepareManagedFile(
			bool isFilePresent,
			string recordedVersion)
		{
			if (isFilePresent)
			{
				File.WriteAllText(ManagedFilePath(), SolutionName);
			}

			if (recordedVersion == null)
			{
				return;
			}

			var versions = new ExportedSolutionVersions();
			versions.Managed.Add(
				new ExportedSolution
				{
					LogicalSolutionName = SolutionName,
					Version = recordedVersion,
					FileName = Path.GetFileName(ManagedFilePath())
				});

			File.WriteAllText(
				Path.Combine(_tempDirectory, ExportedSolutionVersions.FileName),
				JsonConvert.SerializeObject(versions));
		}


		// ============================================================================
		private async Task<ExportFileStatus> GetManagedFileStatusAsync(
			bool shouldSaveVersionJson)
		{
			var statuses =
				await GetFileStatusAsync(
					CreateRunSolution(),
					shouldSaveVersionJson);

			return statuses.Single().Managed;
		}


		// ============================================================================
		private Task<List<SolutionFileStatus>> GetFileStatusAsync(
			RunSolution entry,
			bool shouldSaveVersionJson)
		{
			return _client.InvokeWithParameterObjectAsync<List<SolutionFileStatus>>(
				"files/status",
				new { solutions = new[] { entry }, shouldSaveVersionJson });
		}


		// ============================================================================
		private RunSolution CreateRunSolution()
		{
			var configuration = new SolutionConfiguration(SolutionName)
			{
				FileNameManaged = ManagedFilePath(),
				FileNameUnmanaged = Path.Combine(_tempDirectory, "Unmanaged." + SolutionName + ".zip")
			};

			return new RunSolution(
				CreateSolution(SolutionName, CurrentVersion),
				configuration);
		}


		// ============================================================================
		private static SolutionInfo CreateSolution(
			string uniqueName,
			string version)
		{
			return new SolutionInfo
			{
				SolutionId = Guid.NewGuid(),
				UniqueName = uniqueName,
				FriendlyName = uniqueName,
				Version = new Version(version)
			};
		}


		// ============================================================================
		private string ManagedFilePath()
		{
			return Path.Combine(_tempDirectory, "Managed." + SolutionName + ".zip");
		}


		// ============================================================================
		/// <summary>
		/// Turns the temp directory into a repository with one commit on
		/// <see cref="InitialBranch"/> (checked out) and <see cref="OtherBranch"/>.
		/// </summary>
		private void CreateRepositoryWithTwoBranches()
		{
			RunGit("init", "-b", InitialBranch);
			RunGit("config", "user.name", "Workspace Tests");
			RunGit("config", "user.email", "workspace.tests@example.invalid");
			RunGit("config", "commit.gpgsign", "false");
			RunGit("commit", "--allow-empty", "-m", "initial");
			RunGit("branch", OtherBranch);
		}


		// ============================================================================
		private string RunGit(
			params string[] arguments)
		{
			var exitCode = GitHelper.RunProcess(
				GitExecutable,
				arguments,
				_tempDirectory,
				null,
				null,
				out var standardOutput,
				out var standardError);

			Assert.AreEqual(0, exitCode, "git " + string.Join(" ", arguments) + ": " + standardError);
			return standardOutput;
		}

	}
}
