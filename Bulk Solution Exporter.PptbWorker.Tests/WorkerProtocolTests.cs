using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Com.AiricLenz.BulkSolutionExporter.Core.Run;
using Com.AiricLenz.BulkSolutionExporter.Core.Schema;
using Com.AiricLenz.BulkSolutionExporter.PptbWorker.Tests.Fakes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nerdbank.Streams;
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
	/// Drives the worker's JSON-RPC protocol end to end over an in-process
	/// duplex pipe, with <see cref="FakeToolHost"/> answering the worker's
	/// Dataverse reverse calls the way the tool's UI does. Export files go to
	/// a per-test temp directory.
	/// </summary>
	[TestClass]
	public class WorkerProtocolTests
	{

		private const string SolutionName = "SolutionA";
		private const string StartVersion = "1.0.0.1";
		private const string IncrementFormat = "#.#.#.+";
		private const string IncrementedVersion = "1.0.0.2";
		private const string SourceName = "Source";
		private const string TargetName = "Target";
		private const string UpgradeFault = "SolutionA_Upgrade already exists";
		private const int ExportDelayInMilliseconds = 1100;
		private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(30);

		private string _tempDirectory;
		private FakeToolHost _host;
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

			_host = new FakeToolHost();

			_client =
				new JsonRpc(
					new HeaderDelimitedMessageHandler(
						clientStream,
						clientStream,
						WorkerMethods.CreateFormatter()));

			_client.AddLocalRpcTarget(_host);
			_client.StartListening();
		}


		// ============================================================================
		[TestCleanup]
		public void Disconnect()
		{
			_client.Dispose();
			_workerRpc.Dispose();

			if (Directory.Exists(_tempDirectory))
			{
				Directory.Delete(_tempDirectory, true);
			}
		}


		// ============================================================================
		[TestMethod]
		public async Task Initialize_SupportedProtocol_EchoesProtocolAndVersion()
		{
			var answer =
				await _client.InvokeWithParameterObjectAsync<InitializeAnswer>(
					"platform/initialize",
					new
					{
						protocol = WorkerMethods.ProtocolName,
						protocolVersion = WorkerMethods.ProtocolVersion
					});

			Assert.AreEqual(WorkerMethods.ProtocolName, answer.Protocol);
			Assert.AreEqual(WorkerMethods.ProtocolVersion, answer.ProtocolVersion);
		}


		// ============================================================================
		[TestMethod]
		public async Task Initialize_UnsupportedVersion_ReturnsError()
		{
			await Assert.ThrowsExceptionAsync<RemoteInvocationException>(
				() => _client.InvokeWithParameterObjectAsync<InitializeAnswer>(
					"platform/initialize",
					new
					{
						protocol = WorkerMethods.ProtocolName,
						protocolVersion = WorkerMethods.ProtocolVersion + 1
					}));
		}


		// ============================================================================
		[TestMethod]
		public async Task StartRun_ManagedExport_WritesZipFromSourceToFolder()
		{
			var request = CreateExportRequest();

			var report = await StartRunAsync(request);

			Assert.IsFalse(report.IsAborted);
			Assert.IsTrue(report.Steps.Single().IsSuccessful);
			Assert.IsTrue(_host.Calls.Contains("exportSolution:" + WorkerMethods.SourceTargetIndex));
			CollectionAssert.AreEqual(
				_host.ExportFile,
				File.ReadAllBytes(request.Solutions[0].Configuration.FileNameManaged));
		}


		// ============================================================================
		[TestMethod]
		public async Task AbortRun_DuringExport_ReturnsAbortedResultAndSkipsLaterSteps()
		{
			var request = CreateExportRequest();
			request.ShouldExportUnmanaged = true;
			var exportGate = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
			_host.ExportGate = exportGate.Task;

			var runTask = StartRunAsync(request);
			await _host.ExportStarted.Task.WaitAsync(Timeout);
			var isAbortAccepted = await _client.InvokeAsync<bool>("run/abort");
			exportGate.SetResult(true);
			var report = await runTask;

			Assert.IsTrue(isAbortAccepted);
			Assert.IsTrue(report.IsAborted);
			Assert.AreEqual(1, _host.Calls.Count(call => call.StartsWith("exportSolution")));
		}


		// ============================================================================
		[TestMethod]
		public async Task AbortRun_NoActiveRun_ReturnsFalse()
		{
			var isAbortAccepted = await _client.InvokeAsync<bool>("run/abort");

			Assert.IsFalse(isAbortAccepted);
		}


		// ============================================================================
		[TestMethod]
		public async Task StartRun_VersionUpdate_NotifiesSolutionVersionChanged()
		{
			var request = CreateExportRequest();
			request.ShouldExportManaged = false;
			request.ShouldUpdateVersion = true;
			request.VersionFormat = IncrementFormat;
			var solutionId = request.Solutions[0].Solution.SolutionId;

			await StartRunAsync(request);
			await WaitUntilAsync(() => !_host.VersionChanges.IsEmpty);

			Assert.AreEqual(IncrementedVersion, _host.UpdatedVersions.Single());
			Assert.AreEqual(solutionId, _host.VersionChanges.Single().Key);
			Assert.AreEqual(IncrementedVersion, _host.VersionChanges.Single().Value);
		}


		// ============================================================================
		[TestMethod]
		public async Task StartRun_TimedExport_NotifiesConfigurationChanged()
		{
			var request = CreateExportRequest();
			_host.ExportDelay = TimeSpan.FromMilliseconds(ExportDelayInMilliseconds);

			await StartRunAsync(request);
			await WaitUntilAsync(() => !_host.ConfigurationChanges.IsEmpty);

			var configuration = _host.ConfigurationChanges.Single();
			Assert.AreEqual(SolutionName, (string) configuration["solutionIndentifier"]);
			Assert.IsTrue((int) configuration["lastExportDurationManagedInSeconds"] >= 1);
		}


		// ============================================================================
		[TestMethod]
		public async Task StartRun_Export_NotifiesSolutionProcessedAndLog()
		{
			var request = CreateExportRequest();
			var solutionId = request.Solutions[0].Solution.SolutionId;

			await StartRunAsync(request);
			await WaitUntilAsync(() => !_host.ProcessedSolutions.IsEmpty);

			Assert.AreEqual(solutionId, _host.ProcessedSolutions.Single());
			Assert.IsFalse(_host.LogLines.IsEmpty);
		}


		// ============================================================================
		[TestMethod]
		public async Task StartRun_ImportErrorFromHost_ReachesEngineWithMessage()
		{
			var request = CreateExportRequest();
			request.ShouldImportManaged = true;
			_host.ImportFaultMessage = UpgradeFault;

			var report = await StartRunAsync(request, TargetName);
			await WaitUntilAsync(() => _host.LogLines.Any(line => line.Contains(UpgradeFault)));

			var import = report.Steps.Single(step => step.IsImport);
			Assert.IsFalse(import.IsSuccessful);
			Assert.AreEqual(TargetName, import.ConnectionName);
			Assert.IsTrue(_host.Calls.Contains("importSolution:" + (WorkerMethods.SourceTargetIndex + 1)));
		}


		// ============================================================================
		/// <summary>
		/// Sends <c>run/start</c> with the source and the given targets and
		/// waits for its answer.
		/// </summary>
		private Task<RunReport> StartRunAsync(
			RunRequest request,
			params string[] targetNames)
		{
			return
				_client.InvokeWithParameterObjectAsync<RunReport>(
					"run/start",
					new
					{
						request,
						sourceName = SourceName,
						targetNames
					})
				.WaitAsync(Timeout);
		}


		// ============================================================================
		/// <summary>
		/// A request that exports one solution as managed into the temp directory.
		/// </summary>
		private RunRequest CreateExportRequest()
		{
			var solution = new SolutionInfo
			{
				SolutionId = Guid.NewGuid(),
				UniqueName = SolutionName,
				FriendlyName = SolutionName,
				Version = new Version(StartVersion),
			};

			var configuration = new SolutionConfiguration(SolutionName)
			{
				FileNameManaged = Path.Combine(_tempDirectory, "Managed." + SolutionName + ".zip"),
				FileNameUnmanaged = Path.Combine(_tempDirectory, "Unmanaged." + SolutionName + ".zip"),
			};

			var request = new RunRequest
			{
				ShouldExportManaged = true,
			};

			request.Solutions.Add(new RunSolution(solution, configuration));
			return request;
		}


		// ============================================================================
		/// <summary>
		/// Waits until a notification arrived: the client handles notifications
		/// on its own threads, possibly after the run's answer.
		/// </summary>
		private static async Task WaitUntilAsync(
			Func<bool> condition)
		{
			const int PollIntervalInMilliseconds = 10;
			var deadline = DateTime.UtcNow + Timeout;

			while (!condition())
			{
				if (DateTime.UtcNow > deadline)
				{
					Assert.Fail("The expected notification did not arrive.");
				}

				await Task.Delay(PollIntervalInMilliseconds);
			}
		}

	}



	// ============================================================================
	// ============================================================================
	// ============================================================================
	/// <summary>
	/// The answer of <c>platform/initialize</c>.
	/// </summary>
	public sealed class InitializeAnswer
	{

		// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		public string Protocol
		{
			get; set;
		}


		// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		public int ProtocolVersion
		{
			get; set;
		}

	}
}
