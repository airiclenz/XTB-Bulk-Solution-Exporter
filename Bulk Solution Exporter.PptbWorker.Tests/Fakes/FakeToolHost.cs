using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Com.AiricLenz.BulkSolutionExporter.Core.Run;
using Newtonsoft.Json.Linq;
using StreamJsonRpc;


// ============================================================================
// ============================================================================
// ============================================================================
namespace Com.AiricLenz.BulkSolutionExporter.PptbWorker.Tests.Fakes
{

	// ============================================================================
	// ============================================================================
	// ============================================================================
	/// <summary>
	/// Stands in for the tool's TypeScript UI on the other end of the worker's
	/// connection: answers the worker's Dataverse reverse calls from memory and
	/// records every call and notification. Handlers may run concurrently.
	/// </summary>
	public sealed class FakeToolHost
	{

		// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		/// <summary>
		/// The bytes every export answers with (sent as base64).
		/// </summary>
		public byte[] ExportFile
		{
			get; set;
		} = { 0x50, 0x4B, 0x03, 0x04, 0x2A };


		// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		/// <summary>
		/// When set, every export waits for it before it answers.
		/// </summary>
		public Task ExportGate
		{
			get; set;
		}


		// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		/// <summary>
		/// How long every export takes before it answers.
		/// </summary>
		public TimeSpan ExportDelay
		{
			get; set;
		} = TimeSpan.Zero;


		// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		/// <summary>
		/// When set, every import (<c>dataverse/startImport</c> and
		/// <c>dataverse/importSolution</c>) fails with this message.
		/// </summary>
		public string ImportFaultMessage
		{
			get; set;
		}


		// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		/// <summary>
		/// The states <c>dataverse/getImportOperationState</c> answers, one per
		/// call; when it is empty the call answers
		/// <see cref="ImportOperationStatus.Succeeded"/>.
		/// </summary>
		public ConcurrentQueue<ImportOperationState> OperationStates
		{
			get;
		} = new ConcurrentQueue<ImportOperationState>();


		// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		/// <summary>
		/// Completes when the first export call arrived.
		/// </summary>
		public TaskCompletionSource<bool> ExportStarted
		{
			get;
		} = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);


		// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		/// <summary>
		/// Every reverse call as "method:target", in arrival order.
		/// </summary>
		public ConcurrentQueue<string> Calls
		{
			get;
		} = new ConcurrentQueue<string>();


		// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		/// <summary>
		/// The version of every <c>dataverse/updateSolutionVersion</c> call.
		/// </summary>
		public ConcurrentQueue<string> UpdatedVersions
		{
			get;
		} = new ConcurrentQueue<string>();


		// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		/// <summary>
		/// The <c>line</c> of every <c>run/log</c> notification.
		/// </summary>
		public ConcurrentQueue<string> LogLines
		{
			get;
		} = new ConcurrentQueue<string>();


		// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		/// <summary>
		/// The parameters of every <c>run/solutionVersionChanged</c> notification.
		/// </summary>
		public ConcurrentQueue<KeyValuePair<Guid, string>> VersionChanges
		{
			get;
		} = new ConcurrentQueue<KeyValuePair<Guid, string>>();


		// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		/// <summary>
		/// The <c>configuration</c> of every <c>run/configurationChanged</c> notification.
		/// </summary>
		public ConcurrentQueue<JObject> ConfigurationChanges
		{
			get;
		} = new ConcurrentQueue<JObject>();


		// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		/// <summary>
		/// The <c>solutionId</c> of every <c>run/solutionProcessed</c> notification.
		/// </summary>
		public ConcurrentQueue<Guid> ProcessedSolutions
		{
			get;
		} = new ConcurrentQueue<Guid>();


		// ============================================================================
		[JsonRpcMethod("dataverse/publishAll")]
		public void PublishAll(
			int target)
		{
			Calls.Enqueue("publishAll:" + target);
		}


		// ============================================================================
		[JsonRpcMethod("dataverse/updateSolutionVersion")]
		public void UpdateSolutionVersion(
			int target,
			Guid solutionId,
			string version)
		{
			Calls.Enqueue("updateSolutionVersion:" + target);
			UpdatedVersions.Enqueue(version);
		}


		// ============================================================================
		[JsonRpcMethod("dataverse/exportSolution")]
		public async Task<string> ExportSolutionAsync(
			int target,
			string uniqueName,
			bool isManaged)
		{
			Calls.Enqueue("exportSolution:" + target);
			ExportStarted.TrySetResult(true);

			if (ExportGate != null)
			{
				await ExportGate;
			}

			if (ExportDelay > TimeSpan.Zero)
			{
				await Task.Delay(ExportDelay);
			}

			return Convert.ToBase64String(ExportFile);
		}


		// ============================================================================
		[JsonRpcMethod("dataverse/solutionExists")]
		public bool SolutionExists(
			int target,
			string uniqueName)
		{
			Calls.Enqueue("solutionExists:" + target);
			return false;
		}


		// ============================================================================
		[JsonRpcMethod("dataverse/importSolution")]
		public void ImportSolution(
			int target,
			string solutionFile,
			bool isManaged,
			bool isHolding,
			bool shouldPublishWorkflows,
			bool shouldOverwriteUnmanagedCustomizations)
		{
			Calls.Enqueue("importSolution:" + target);

			if (ImportFaultMessage != null)
			{
				throw new InvalidOperationException(ImportFaultMessage);
			}
		}


		// ============================================================================
		/// <summary>
		/// Answers a fresh async operation id as a string, or fails with
		/// <see cref="ImportFaultMessage"/> when it is set.
		/// </summary>
		[JsonRpcMethod("dataverse/startImport")]
		public string StartImport(
			int target,
			string solutionFile,
			bool isManaged,
			bool isUpgrade,
			bool shouldPublishWorkflows,
			bool shouldOverwriteUnmanagedCustomizations)
		{
			Calls.Enqueue("startImport:" + target);

			if (ImportFaultMessage != null)
			{
				throw new InvalidOperationException(ImportFaultMessage);
			}

			return Guid.NewGuid().ToString();
		}


		// ============================================================================
		/// <summary>
		/// Answers the next scripted state of <see cref="OperationStates"/> as
		/// <c>{ status, message }</c> with <c>status</c> as the member name, as
		/// the tool's bridge does.
		/// </summary>
		[JsonRpcMethod("dataverse/getImportOperationState")]
		public object GetImportOperationState(
			int target,
			string asyncOperationId)
		{
			Calls.Enqueue("getImportOperationState:" + target);

			if (!OperationStates.TryDequeue(out var operationState))
			{
				operationState = new ImportOperationState { Status = ImportOperationStatus.Succeeded };
			}

			return
				new
				{
					status = operationState.Status.ToString(),
					message = operationState.Message ?? string.Empty
				};
		}


		// ============================================================================
		[JsonRpcMethod("dataverse/deleteAndPromote")]
		public void DeleteAndPromote(
			int target,
			string uniqueName)
		{
			Calls.Enqueue("deleteAndPromote:" + target);
		}


		// ============================================================================
		[JsonRpcMethod("dataverse/retrieveSolutions")]
		public object[] RetrieveSolutions(
			int target,
			bool includeManaged)
		{
			Calls.Enqueue("retrieveSolutions:" + target);
			return new object[0];
		}


		// ============================================================================
		[JsonRpcMethod("run/log")]
		public void Log(
			string line)
		{
			LogLines.Enqueue(line);
		}


		// ============================================================================
		[JsonRpcMethod("run/progress")]
		public void Progress(
			string message,
			string detail,
			bool isNewStep)
		{
		}


		// ============================================================================
		[JsonRpcMethod("run/solutionVersionChanged")]
		public void SolutionVersionChanged(
			Guid solutionId,
			string version)
		{
			VersionChanges.Enqueue(new KeyValuePair<Guid, string>(solutionId, version));
		}


		// ============================================================================
		[JsonRpcMethod("run/configurationChanged")]
		public void ConfigurationChanged(
			JObject configuration)
		{
			ConfigurationChanges.Enqueue(configuration);
		}


		// ============================================================================
		[JsonRpcMethod("run/solutionProcessed")]
		public void SolutionProcessed(
			Guid solutionId)
		{
			ProcessedSolutions.Enqueue(solutionId);
		}

	}
}
