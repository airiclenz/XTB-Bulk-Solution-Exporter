using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Com.AiricLenz.BulkSolutionExporter.Core.Run;
using Newtonsoft.Json.Serialization;
using StreamJsonRpc;


// ============================================================================
// ============================================================================
// ============================================================================
namespace Com.AiricLenz.BulkSolutionExporter.PptbWorker
{

	// ============================================================================
	// ============================================================================
	// ============================================================================
	/// <summary>
	/// The JSON-RPC methods the worker answers. Power Platform ToolBox first
	/// calls <c>platform/initialize</c>; the tool's UI then starts a run with
	/// <c>run/start</c> and may abort it with <c>run/abort</c>.
	/// <para>
	/// Wire format: named parameters, camelCase property names, versions as
	/// dotted strings (<c>"1.0.0.0"</c>), ids as GUID strings. While a run is
	/// active the worker calls back into the UI for every Dataverse request
	/// (see <see cref="RpcDataverseGateway"/>) and sends its log, progress and
	/// changes as notifications (see <see cref="RpcRunSink"/>).
	/// </para>
	/// </summary>
	public sealed class WorkerMethods
	{

		/// <summary>
		/// The protocol name Power Platform ToolBox and the worker agree on.
		/// </summary>
		public const string ProtocolName = "jsonrpc-stdio-v1";

		/// <summary>
		/// The protocol version Power Platform ToolBox and the worker agree on.
		/// </summary>
		public const int ProtocolVersion = 1;

		/// <summary>
		/// The gateway target index of the source environment; targets follow from 1.
		/// </summary>
		public const int SourceTargetIndex = 0;

		private const string RunAlreadyActiveMessage = "A run is already in progress.";
		private const string RunFailedMessagePrefix = "The run failed: ";

		private readonly JsonRpc _rpc;
		private readonly object _runLock = new object();

		// The active run's abort source; null while no run is active.
		private CancellationTokenSource _runCancellation = null;


		// ============================================================================
		/// <summary>
		/// Creates the methods for a connection; reverse calls and
		/// notifications go through <paramref name="rpc"/>.
		/// </summary>
		/// <exception cref="ArgumentNullException"><paramref name="rpc"/> is null.</exception>
		public WorkerMethods(
			JsonRpc rpc)
		{
			_rpc = rpc ?? throw new ArgumentNullException(nameof(rpc));
		}


		// ============================================================================
		/// <summary>
		/// Connects a new <see cref="WorkerMethods"/> to a stream pair with the
		/// worker's wire format (header-delimited JSON-RPC) and starts listening.
		/// </summary>
		/// <param name="sendingStream">Where the worker writes its messages (stdout).</param>
		/// <param name="receivingStream">Where the worker reads the host's messages (stdin).</param>
		/// <returns>The listening connection; dispose it to disconnect.</returns>
		/// <exception cref="ArgumentNullException">A stream is null.</exception>
		public static JsonRpc Attach(
			Stream sendingStream,
			Stream receivingStream)
		{
			if (sendingStream == null)
			{
				throw new ArgumentNullException(nameof(sendingStream));
			}

			if (receivingStream == null)
			{
				throw new ArgumentNullException(nameof(receivingStream));
			}

			var handler =
				new HeaderDelimitedMessageHandler(
					sendingStream,
					receivingStream,
					CreateFormatter());

			var rpc = new JsonRpc(handler);
			rpc.AddLocalRpcTarget(new WorkerMethods(rpc));
			rpc.StartListening();

			return rpc;
		}


		// ============================================================================
		/// <summary>
		/// Creates the message formatter of the worker's wire format: camelCase
		/// property names and solution versions as dotted strings.
		/// </summary>
		public static JsonMessageFormatter CreateFormatter()
		{
			var formatter = new JsonMessageFormatter();
			formatter.JsonSerializer.ContractResolver = new CamelCasePropertyNamesContractResolver();
			formatter.JsonSerializer.Converters.Add(new VersionJsonConverter());

			return formatter;
		}


		// ============================================================================
		/// <summary>
		/// Answers the Power Platform ToolBox handshake.
		/// </summary>
		/// <returns>The agreed protocol name and version.</returns>
		/// <exception cref="InvalidOperationException">The protocol is not <see cref="ProtocolName"/> version <see cref="ProtocolVersion"/>.</exception>
		[JsonRpcMethod("platform/initialize")]
		public object Initialize(
			string protocol,
			int protocolVersion)
		{
			if (protocol != ProtocolName ||
				protocolVersion != ProtocolVersion)
			{
				throw new InvalidOperationException("Unsupported PPTB worker protocol.");
			}

			return new
			{
				protocol,
				protocolVersion
			};
		}


		// ============================================================================
		/// <summary>
		/// Runs <see cref="RunEngine"/> for the request on a background thread,
		/// so the connection keeps dispatching the UI's answers to the run's
		/// Dataverse calls; returns when the run ended or was aborted. One run
		/// at a time.
		/// </summary>
		/// <param name="request">The run's settings and solutions.</param>
		/// <param name="sourceName">The source environment's display name (gateway target 0).</param>
		/// <param name="targetNames">The target environments' display names in import order
		/// (gateway targets 1 to n); <c>null</c> for none.</param>
		/// <param name="cancellationToken">The host's cancellation of this request; aborts the run like <c>run/abort</c>.</param>
		/// <returns>Whether the run was aborted, how long it took and each step's outcome.</returns>
		/// <exception cref="InvalidOperationException">Another run is active.</exception>
		/// <exception cref="ArgumentNullException">The request or its solution list is null.</exception>
		/// <remarks>Any exception that ends the run (such as a failed export) is
		/// returned to the caller as the request's error.</remarks>
		[JsonRpcMethod("run/start")]
		public async Task<RunReport> StartRunAsync(
			RunRequest request,
			string sourceName,
			string[] targetNames,
			CancellationToken cancellationToken)
		{
			using (var runCancellation = BeginRun(cancellationToken))
			{
				try
				{
					var source =
						new RpcDataverseGateway(
							_rpc,
							SourceTargetIndex,
							sourceName);

					var targets = CreateTargetGateways(targetNames);
					var sink = new RpcRunSink(_rpc);

					// The engine blocks on every reverse call, so it gets its own thread.
					var result =
						await Task.Factory.StartNew(
							() => RunEngine.Run(
								request,
								source,
								targets,
								sink,
								runCancellation.Token),
							CancellationToken.None,
							TaskCreationOptions.LongRunning,
							TaskScheduler.Default);

					return RunReport.FromResult(result);
				}
				catch (Exception exception)
				{
					Console.Error.WriteLine(RunFailedMessagePrefix + exception);
					throw;
				}
				finally
				{
					EndRun();
				}
			}
		}


		// ============================================================================
		/// <summary>
		/// Requests an abort of the active run. The run stops between steps: a
		/// running Dataverse call finishes and nothing after it starts.
		/// </summary>
		/// <returns>True when a run was active, false when there was nothing to abort.</returns>
		[JsonRpcMethod("run/abort")]
		public bool AbortRun()
		{
			lock (_runLock)
			{
				if (_runCancellation == null)
				{
					return false;
				}

				_runCancellation.Cancel();
				return true;
			}
		}


		// ============================================================================
		/// <summary>
		/// Registers a new active run, linked to the request's cancellation.
		/// </summary>
		/// <exception cref="InvalidOperationException">Another run is active.</exception>
		private CancellationTokenSource BeginRun(
			CancellationToken cancellationToken)
		{
			lock (_runLock)
			{
				if (_runCancellation != null)
				{
					throw new InvalidOperationException(RunAlreadyActiveMessage);
				}

				_runCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
				return _runCancellation;
			}
		}


		// ============================================================================
		/// <summary>
		/// Clears the active run, so <c>run/abort</c> no longer reaches it.
		/// </summary>
		private void EndRun()
		{
			lock (_runLock)
			{
				_runCancellation = null;
			}
		}


		// ============================================================================
		/// <summary>
		/// Creates one gateway per target name, numbered from 1 in list order.
		/// </summary>
		private List<IDataverseGateway> CreateTargetGateways(
			string[] targetNames)
		{
			var targets = new List<IDataverseGateway>();

			if (targetNames == null)
			{
				return targets;
			}

			for (var index = 0; index < targetNames.Length; index++)
			{
				targets.Add(
					new RpcDataverseGateway(
						_rpc,
						SourceTargetIndex + 1 + index,
						targetNames[index]));
			}

			return targets;
		}

	}
}
