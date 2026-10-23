using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Com.AiricLenz.BulkSolutionExporter.Core.Run;
using Com.AiricLenz.BulkSolutionExporter.Core.Schema;
using Com.AiricLenz.BulkSolutionExporter.Core.Tests.Fakes;
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
	/// Covers the git and target phases of <see cref="RunEngine.Run(RunRequest, IDataverseGateway, IReadOnlyList{IDataverseGateway}, IRunSink, Func{string, IGitClient}, CancellationToken)"/>
	/// against in-memory gateways and git client, with the solution files in a
	/// per-test temp directory.
	/// </summary>
	[TestClass]
	public class RunEngineImportTests
	{

		private const string SolutionA = "SolutionA";
		private const string SolutionB = "SolutionB";
		private const string StartVersion = "1.0.0.1";
		private const string GitFolderName = ".git";
		private const string CommitMessage = "Export solutions";

		// Retries in these tests wait the shortest delay the engine accepts.
		private const int RetryCount = 1;
		private const int RetryDelayInSeconds = 1;

		private string _tempDirectory;
		private FakeDataverseGateway _source;
		private FakeDataverseGateway _target;
		private FakeGitClient _gitClient;
		private RecordingRunSink _sink;


		// ============================================================================
		[TestInitialize]
		public void CreateTempDirectory()
		{
			_tempDirectory =
				Path.Combine(
					Path.GetTempPath(),
					Path.GetRandomFileName());

			Directory.CreateDirectory(_tempDirectory);

			_source = new FakeDataverseGateway();
			_target = new FakeDataverseGateway("Target");
			_gitClient = new FakeGitClient();
			_sink = new RecordingRunSink();
		}


		// ============================================================================
		[TestCleanup]
		public void DeleteTempDirectory()
		{
			if (Directory.Exists(_tempDirectory))
			{
				Directory.Delete(_tempDirectory, true);
			}
		}


		// ============================================================================
		[TestMethod]
		public void Run_ManagedUpgradeOfInstalledSolution_InstallsHoldingAndPromotes()
		{
			var request = CreateImportRequest(SolutionA);
			request.ShouldUpgrade = true;
			_target.Solutions.Add(CreateSolution(SolutionA));

			var result = Run(request, _target);

			Assert.IsTrue(_target.ImportedOptions.Single().IsHolding);
			Assert.AreEqual("DeleteAndPromote:" + SolutionA, _target.Calls.Last());
			Assert.IsTrue(result.StepOutcomes.Single().IsSuccessful);
		}


		// ============================================================================
		[TestMethod]
		public void Run_UnmanagedImportOfInstalledSolution_NeverUsesHolding()
		{
			var request = CreateImportRequest(SolutionA);
			request.ShouldImportManaged = false;
			request.ShouldImportUnmanaged = true;
			request.ShouldUpgrade = true;
			_target.Solutions.Add(CreateSolution(SolutionA));

			Run(request, _target);

			var options = _target.ImportedOptions.Single();
			Assert.IsFalse(options.IsHolding);
			Assert.IsFalse(options.IsManaged);
			Assert.IsFalse(_target.Calls.Any(call => call.StartsWith("DeleteAndPromote")));
		}


		// ============================================================================
		[TestMethod]
		public void Run_AbortDuringExport_SkipsGitAndImport()
		{
			Directory.CreateDirectory(Path.Combine(_tempDirectory, GitFolderName));
			var request = CreateImportRequest(SolutionA);
			request.ShouldExportManaged = true;
			request.ShouldCommitToGit = true;
			request.ShouldPushCommit = true;

			RunResult result;

			using (var cancellation = new CancellationTokenSource())
			{
				_source.AfterExport = (uniqueName, isManaged) => cancellation.Cancel();

				result =
					RunEngine.Run(
						request,
						_source,
						new[] { _target },
						_sink,
						_gitClient.Create,
						cancellation.Token);
			}

			Assert.IsTrue(result.IsAborted);
			Assert.IsNull(_gitClient.RepositoryRootPath);
			Assert.AreEqual(0, _gitClient.Calls.Count);
			Assert.AreEqual(0, _target.Calls.Count);
		}


		// ============================================================================
		[TestMethod]
		public void Run_CommitAndPush_UsesTheRepositoryOfTheFirstConfiguredFilePath()
		{
			var firstRepository = CreateRepository("first");
			var secondRepository = CreateRepository("second");
			var request = CreateImportRequest(SolutionA, SolutionB);
			request.ShouldImportManaged = false;
			request.ShouldExportManaged = true;
			request.ShouldCommitToGit = true;
			request.ShouldPushCommit = true;
			request.CommitMessage = CommitMessage;
			request.Solutions[0].Configuration.FileNameManaged = Path.Combine(firstRepository, "nested", "A.zip");
			request.Solutions[1].Configuration.FileNameManaged = Path.Combine(secondRepository, "B.zip");

			Run(request);

			Assert.AreEqual(firstRepository, _gitClient.RepositoryRootPath);

			CollectionAssert.AreEqual(
				new[]
				{
					"AddFile:" + request.Solutions[0].Configuration.FileNameManaged,
					"AddFile:" + request.Solutions[1].Configuration.FileNameManaged,
					"Commit:" + CommitMessage,
					"Push",
				},
				_gitClient.Calls);
		}


		// ============================================================================
		[TestMethod]
		public void Run_ImportFailsWithContinueOnError_ImportsTheNextSolution()
		{
			var request = CreateImportRequest(SolutionA, SolutionB);
			request.ShouldContinueOnError = true;
			File.Delete(request.Solutions[0].Configuration.FileNameManaged);

			var result = Run(request, _target);

			CollectionAssert.AreEqual(new[] { SolutionB }, ImportedNames(_target));
			Assert.IsFalse(result.StepOutcomes[0].IsSuccessful);
			Assert.IsTrue(result.StepOutcomes[1].IsSuccessful);
		}


		// ============================================================================
		[TestMethod]
		public void Run_ImportFailsWithoutContinueOnError_StopsTheTargetsImports()
		{
			var request = CreateImportRequest(SolutionA, SolutionB);
			File.Delete(request.Solutions[0].Configuration.FileNameManaged);

			Run(request, _target);

			Assert.AreEqual(0, ImportedNames(_target).Length);
			Assert.IsTrue(_sink.Lines.Any(line => line.Contains("Continue-On-Error is disabled")));
		}


		// ============================================================================
		[TestMethod]
		public void Run_TwoTargets_ImportsIntoEachAndPublishesEach()
		{
			var secondTarget = new FakeDataverseGateway("Second Target");
			var request = CreateImportRequest(SolutionA, SolutionB);
			request.ShouldPublishTargets = true;

			var result = Run(request, _target, secondTarget);

			foreach (var target in new[] { _target, secondTarget })
			{
				CollectionAssert.AreEqual(new[] { SolutionA, SolutionB }, ImportedNames(target));
				Assert.AreEqual("PublishAll", target.Calls.Last());
			}

			Assert.AreEqual(4, result.StepOutcomes.Count(outcome => outcome.IsImport && outcome.IsSuccessful));
			Assert.AreEqual(4, _sink.ProcessedSolutionIds.Count);
		}


		// ============================================================================
		[TestMethod]
		public void Run_ImportRetriesExhausted_FailsTheSolution()
		{
			var request = CreateImportRequest(SolutionA);
			_target.ImportFaultMessages.Enqueue(RetryRunner.ImportAlreadyRunningFaultText);
			_target.ImportFaultMessages.Enqueue(RetryRunner.ImportAlreadyRunningFaultText);

			var result = Run(request, _target);

			Assert.AreEqual(RetryCount + 1, _target.ImportedOptions.Count);
			Assert.IsFalse(result.StepOutcomes.Single().IsSuccessful);
			Assert.IsTrue(_sink.Lines.Any(line => line.Contains("Gave up")));
		}


		// ============================================================================
		[TestMethod]
		public void Run_UpgradeAlreadyExists_FailsTheSolutionWithoutPromote()
		{
			var request = CreateImportRequest(SolutionA);
			request.ShouldUpgrade = true;
			_target.Solutions.Add(CreateSolution(SolutionA));
			_target.ImportFaultMessages.Enqueue(SolutionA + RetryRunner.UpgradeAlreadyExistsFaultText);

			var result = Run(request, _target);

			Assert.AreEqual(1, _target.ImportedOptions.Count);
			Assert.IsFalse(_target.Calls.Any(call => call.StartsWith("DeleteAndPromote")));
			Assert.IsFalse(result.StepOutcomes.Single().IsSuccessful);
		}


		// ============================================================================
		[TestMethod]
		public void Run_TargetBusyWithAnotherImport_RetriesAndSucceeds()
		{
			var request = CreateImportRequest(SolutionA);
			_target.ImportFaultMessages.Enqueue(RetryRunner.ImportAlreadyRunningFaultText);

			var result = Run(request, _target);

			Assert.AreEqual(2, _target.ImportedOptions.Count);
			Assert.IsTrue(result.StepOutcomes.Single().IsSuccessful);
		}


		// ============================================================================
		/// <summary>
		/// Runs the request against the test's source, git client and the given targets.
		/// </summary>
		private RunResult Run(
			RunRequest request,
			params IDataverseGateway[] targets)
		{
			return
				RunEngine.Run(
					request,
					_source,
					targets,
					_sink,
					_gitClient.Create,
					CancellationToken.None);
		}


		// ============================================================================
		/// <summary>
		/// A managed-import request for the named solutions, each configured to a
		/// managed and an unmanaged file in the temp directory; both files exist.
		/// </summary>
		private RunRequest CreateImportRequest(
			params string[] uniqueNames)
		{
			var request = new RunRequest
			{
				ShouldImportManaged = true,
				RetryCount = RetryCount,
				RetryDelayInSeconds = RetryDelayInSeconds,
			};

			foreach (var uniqueName in uniqueNames)
			{
				var configuration = new SolutionConfiguration(uniqueName)
				{
					FileNameManaged = Path.Combine(_tempDirectory, "Managed." + uniqueName + ".zip"),
					FileNameUnmanaged = Path.Combine(_tempDirectory, "Unmanaged." + uniqueName + ".zip"),
				};

				File.WriteAllText(configuration.FileNameManaged, FakeDataverseGateway.ExportContentPrefix + uniqueName);
				File.WriteAllText(configuration.FileNameUnmanaged, FakeDataverseGateway.ExportContentPrefix + uniqueName);

				request.Solutions.Add(
					new RunSolution(
						CreateSolution(uniqueName),
						configuration));
			}

			return request;
		}


		// ============================================================================
		/// <summary>
		/// Creates a folder with a ".git" folder in the temp directory.
		/// </summary>
		/// <returns>The repository root folder.</returns>
		private string CreateRepository(
			string folderName)
		{
			var repositoryRootPath = Path.Combine(_tempDirectory, folderName);
			Directory.CreateDirectory(Path.Combine(repositoryRootPath, GitFolderName));
			return repositoryRootPath;
		}


		// ============================================================================
		private static SolutionInfo CreateSolution(
			string uniqueName)
		{
			return new SolutionInfo
			{
				SolutionId = Guid.NewGuid(),
				UniqueName = uniqueName,
				FriendlyName = uniqueName,
				Version = new Version(StartVersion),
			};
		}


		// ============================================================================
		/// <summary>
		/// The unique names of the target's imports, in call order.
		/// </summary>
		private static string[] ImportedNames(
			FakeDataverseGateway target)
		{
			var importPrefix = "ImportSolution:" + FakeDataverseGateway.ExportContentPrefix;

			return
				target.Calls
					.Where(call => call.StartsWith(importPrefix))
					.Select(call => call.Substring(importPrefix.Length))
					.ToArray();
		}

	}
}
