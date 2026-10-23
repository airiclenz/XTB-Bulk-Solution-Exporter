using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Com.AiricLenz.BulkSolutionExporter.Core.Run;
using Com.AiricLenz.BulkSolutionExporter.Core.Schema;
using Com.AiricLenz.BulkSolutionExporter.Core.Tests.Fakes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;


// ============================================================================
// ============================================================================
// ============================================================================
namespace Com.AiricLenz.BulkSolutionExporter.Core.Tests
{

	// ============================================================================
	// ============================================================================
	// ============================================================================
	/// <summary>
	/// Covers the publish, version and export phases of <see cref="RunEngine.Run"/>
	/// against an in-memory gateway, writing export files into a per-test temp
	/// directory.
	/// </summary>
	[TestClass]
	public class RunEngineExportTests
	{

		private const string SolutionA = "SolutionA";
		private const string SolutionB = "SolutionB";
		private const string SolutionC = "SolutionC";
		private const string StartVersion = "1.0.0.1";
		private const string IncrementFormat = "#.#.#.+";
		private const string IncrementedVersion = "1.0.0.2";
		private const string InvalidFormat = "not a format";
		private const string TraversalFileName = "../evil.zip";
		private const string MissingFileNameError = "No file name for the managed solution was given!";
		private const string InvalidPathError = "is not a valid absolute file path";
		private const string InvalidFormatError = "The version format is invalid!";
		private const int RetryCount = 3;

		private string _tempDirectory;
		private FakeDataverseGateway _gateway;
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

			_gateway = new FakeDataverseGateway();
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
		public void Run_RequestWithTwoOfThreeSolutions_ExportsOnlyTheRequestedOnes()
		{
			_gateway.Solutions.Add(CreateSolution(SolutionC));
			var request = CreateExportRequest(SolutionA, SolutionB);

			RunEngine.Run(request, _gateway, NoTargets(), _sink, CancellationToken.None);

			CollectionAssert.AreEqual(
				new[] { SolutionA, SolutionB },
				ExportedNames());
		}


		// ============================================================================
		[TestMethod]
		public void Run_AbortDuringFirstExport_SkipsSecondSolutionAndLogsAbortOnce()
		{
			var request = CreateExportRequest(SolutionA, SolutionB);
			request.ShouldExportUnmanaged = true;

			using (var cancellation = new CancellationTokenSource())
			{
				_gateway.AfterExport = (uniqueName, isManaged) => cancellation.Cancel();

				RunEngine.Run(request, _gateway, NoTargets(), _sink, cancellation.Token);
			}

			CollectionAssert.AreEqual(new[] { "ExportSolution:" + SolutionA + ":managed" }, _gateway.Calls);
			Assert.AreEqual(1, _sink.Lines.Count(line => line.Contains(RunEngine.AbortRequestedMessage)));
		}


		// ============================================================================
		[TestMethod]
		public void Run_TraversalFileName_IsRejectedWithoutExport()
		{
			var request = CreateExportRequest(SolutionA);
			request.Solutions[0].Configuration.FileNameManaged = TraversalFileName;

			RunEngine.Run(request, _gateway, NoTargets(), _sink, CancellationToken.None);

			Assert.AreEqual(0, ExportedNames().Length);
			Assert.IsTrue(_sink.Lines.Any(line => line.Contains(InvalidPathError)));
		}


		// ============================================================================
		[TestMethod]
		public void Run_SaveVersionJsonOn_WritesTheExportedVersion()
		{
			var request = CreateExportRequest(SolutionA);
			request.ShouldSaveVersionJson = true;

			RunEngine.Run(request, _gateway, NoTargets(), _sink, CancellationToken.None);

			var json = File.ReadAllText(Path.Combine(_tempDirectory, ExportedSolutionVersions.FileName));
			var versions = JsonConvert.DeserializeObject<ExportedSolutionVersions>(json);
			Assert.AreEqual(1, versions.Managed.Count);
			Assert.AreEqual(StartVersion, versions.Managed[0].Version);
			Assert.AreEqual(ManagedFileName(SolutionA), versions.Managed[0].FileName);
		}


		// ============================================================================
		[TestMethod]
		public void Run_ExportFails_ThrowsWithoutRetryAndSkipsLaterSolutions()
		{
			var request = CreateExportRequest(SolutionA, SolutionB);
			request.RetryCount = RetryCount;
			_gateway.FailExportFor = SolutionA;

			Assert.ThrowsException<DataverseOperationException>(
				() => RunEngine.Run(request, _gateway, NoTargets(), _sink, CancellationToken.None));

			CollectionAssert.AreEqual(new[] { SolutionA }, ExportedNames());
		}


		// ============================================================================
		[TestMethod]
		public void Run_SourcePublishAllFails_LogsErrorAndStillExports()
		{
			var request = CreateExportRequest(SolutionA);
			request.ShouldPublishSource = true;
			_gateway.ShouldFailPublishAll = true;

			RunEngine.Run(request, _gateway, NoTargets(), _sink, CancellationToken.None);

			Assert.IsTrue(_sink.Lines.Any(line => line.Contains(FakeDataverseGateway.FailureMessage)));
			CollectionAssert.AreEqual(new[] { SolutionA }, ExportedNames());
		}


		// ============================================================================
		[TestMethod]
		public void Run_ConfiguredFilePath_IsWrittenAsIs()
		{
			var request = CreateExportRequest(SolutionA);
			var filePath = Path.Combine(_tempDirectory, "nested", "Custom Name.zip");
			request.Solutions[0].Configuration.FileNameManaged = filePath;

			RunEngine.Run(request, _gateway, NoTargets(), _sink, CancellationToken.None);

			Assert.AreEqual(
				FakeDataverseGateway.ExportContentPrefix + SolutionA,
				File.ReadAllText(filePath));
		}


		// ============================================================================
		[TestMethod]
		public void Run_VersionUpdateFails_ThrowsAndCallsNoLaterStep()
		{
			var request = CreateExportRequest(SolutionA, SolutionB);
			request.ShouldUpdateVersion = true;
			request.VersionFormat = IncrementFormat;
			_gateway.FailVersionUpdateFor = request.Solutions[0].Solution.SolutionId;

			Assert.ThrowsException<DataverseOperationException>(
				() => RunEngine.Run(request, _gateway, NoTargets(), _sink, CancellationToken.None));

			Assert.AreEqual(1, _gateway.Calls.Count);
		}


		// ============================================================================
		[TestMethod]
		public void Run_InvalidVersionFormat_LogsErrorAndStillExports()
		{
			var request = CreateExportRequest(SolutionA, SolutionB);
			request.ShouldUpdateVersion = true;
			request.VersionFormat = InvalidFormat;

			RunEngine.Run(request, _gateway, NoTargets(), _sink, CancellationToken.None);

			Assert.AreEqual(2, _sink.Lines.Count(line => line.Contains(InvalidFormatError)));
			CollectionAssert.AreEqual(new[] { SolutionA, SolutionB }, ExportedNames());
		}


		// ============================================================================
		[TestMethod]
		public void Run_MissingFileName_LogsErrorAndExportsNextSolution()
		{
			var request = CreateExportRequest(SolutionA, SolutionB);
			request.Solutions[0].Configuration.FileNameManaged = null;

			RunEngine.Run(request, _gateway, NoTargets(), _sink, CancellationToken.None);

			Assert.IsTrue(_sink.Lines.Any(line => line.Contains(MissingFileNameError)));
			CollectionAssert.AreEqual(new[] { SolutionB }, ExportedNames());
		}


		// ============================================================================
		[TestMethod]
		public void Run_InvalidFilePath_LogsErrorAndExportsNextSolution()
		{
			var request = CreateExportRequest(SolutionA, SolutionB);
			request.Solutions[0].Configuration.FileNameManaged = TraversalFileName;

			RunEngine.Run(request, _gateway, NoTargets(), _sink, CancellationToken.None);

			Assert.IsTrue(_sink.Lines.Any(line => line.Contains(InvalidPathError)));
			CollectionAssert.AreEqual(new[] { SolutionB }, ExportedNames());
		}


		// ============================================================================
		[TestMethod]
		public void Run_TwoSolutionsExported_RaisesSolutionProcessedOncePerSolution()
		{
			var request = CreateExportRequest(SolutionA, SolutionB);
			request.ShouldExportUnmanaged = true;

			RunEngine.Run(request, _gateway, NoTargets(), _sink, CancellationToken.None);

			CollectionAssert.AreEqual(
				request.Solutions.Select(entry => entry.Solution.SolutionId).ToList(),
				_sink.ProcessedSolutionIds);
		}


		// ============================================================================
		[TestMethod]
		public void Run_VersionUpdated_ReportsNewVersionAndLeavesRequestUnchanged()
		{
			var request = CreateExportRequest(SolutionA);
			request.ShouldUpdateVersion = true;
			request.VersionFormat = IncrementFormat;

			RunEngine.Run(request, _gateway, NoTargets(), _sink, CancellationToken.None);

			Assert.AreEqual(IncrementedVersion, _sink.VersionChanges.Single().Value.ToString());
			Assert.AreEqual(StartVersion, request.Solutions[0].Solution.Version.ToString());
		}


		// ============================================================================
		[TestMethod]
		public void Run_VersionUpdatedAndJsonSaved_RecordsTheNewVersion()
		{
			var request = CreateExportRequest(SolutionA);
			request.ShouldUpdateVersion = true;
			request.VersionFormat = IncrementFormat;
			request.ShouldSaveVersionJson = true;

			RunEngine.Run(request, _gateway, NoTargets(), _sink, CancellationToken.None);

			ExportedSolutionVersions.TryLoadExportedSolution(
				Path.Combine(_tempDirectory, ManagedFileName(SolutionA)),
				SolutionA,
				out var exportedSolution);
			Assert.AreEqual(IncrementedVersion, exportedSolution?.Version);
		}


		// ============================================================================
		/// <summary>
		/// A managed-export request for the named solutions, each configured to a
		/// managed file in the temp directory.
		/// </summary>
		private RunRequest CreateExportRequest(
			params string[] uniqueNames)
		{
			var request = new RunRequest
			{
				ShouldExportManaged = true,
			};

			foreach (var uniqueName in uniqueNames)
			{
				var solution = CreateSolution(uniqueName);
				_gateway.Solutions.Add(solution);

				var configuration = new SolutionConfiguration(uniqueName)
				{
					FileNameManaged = Path.Combine(_tempDirectory, ManagedFileName(uniqueName)),
					FileNameUnmanaged = Path.Combine(_tempDirectory, "Unmanaged." + uniqueName + ".zip"),
				};

				request.Solutions.Add(new RunSolution(solution, configuration));
			}

			return request;
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
		private static string ManagedFileName(
			string uniqueName)
		{
			return "Managed." + uniqueName + ".zip";
		}


		// ============================================================================
		private static IReadOnlyList<IDataverseGateway> NoTargets()
		{
			return new List<IDataverseGateway>();
		}


		// ============================================================================
		/// <summary>
		/// The unique names of the gateway's managed exports, in call order.
		/// </summary>
		private string[] ExportedNames()
		{
			const string managedExportPrefix = "ExportSolution:";
			const string managedExportSuffix = ":managed";

			return
				_gateway.Calls
					.Where(call => call.StartsWith(managedExportPrefix) && call.EndsWith(managedExportSuffix))
					.Select(call => call.Substring(managedExportPrefix.Length, call.Length - managedExportPrefix.Length - managedExportSuffix.Length))
					.ToArray();
		}

	}



	// ============================================================================
	// ============================================================================
	// ============================================================================
	/// <summary>
	/// An <see cref="IRunSink"/> that records every call for assertions.
	/// </summary>
	internal sealed class RecordingRunSink : IRunSink
	{

		// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		public List<string> Lines
		{
			get;
		} = new List<string>();


		// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		public List<KeyValuePair<Guid, Version>> VersionChanges
		{
			get;
		} = new List<KeyValuePair<Guid, Version>>();


		// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		public List<SolutionConfiguration> ConfigurationChanges
		{
			get;
		} = new List<SolutionConfiguration>();


		// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		public List<string> ProgressMessages
		{
			get;
		} = new List<string>();


		// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		public List<Guid> ProcessedSolutionIds
		{
			get;
		} = new List<Guid>();


		// ============================================================================
		public void Log(
			string line)
		{
			Lines.Add(line);
		}


		// ============================================================================
		public void SolutionVersionChanged(
			Guid solutionId,
			Version version)
		{
			VersionChanges.Add(new KeyValuePair<Guid, Version>(solutionId, version));
		}


		// ============================================================================
		public void ConfigurationChanged(
			SolutionConfiguration configuration)
		{
			ConfigurationChanges.Add(configuration);
		}


		// ============================================================================
		public void Progress(
			string message,
			string detail,
			bool isNewStep)
		{
			ProgressMessages.Add(message);
		}


		// ============================================================================
		public void SolutionProcessed(
			Guid solutionId)
		{
			ProcessedSolutionIds.Add(solutionId);
		}

	}
}
