using System;
using System.Collections.Generic;
using System.Threading;
using Com.AiricLenz.BulkSolutionExporter.Core.Run;
using Com.AiricLenz.BulkSolutionExporter.Core.Schema;
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
	/// Reaches one Dataverse environment by calling back into the tool's UI,
	/// which runs each request through the Power Platform ToolBox Dataverse API.
	/// Every reverse call carries <c>target</c>: 0 is the source, 1 to n the
	/// targets in import order. Solution files cross the connection as base64
	/// strings. Each call blocks the run's thread until the UI answered; an
	/// error the UI returns becomes a <see cref="DataverseOperationException"/>
	/// with the UI's message unchanged. A lost connection throws the
	/// connection's own exception and ends the run.
	/// </summary>
	public sealed class RpcDataverseGateway : IDataverseGateway
	{

		private const string PublishAllMethod = "dataverse/publishAll";
		private const string UpdateSolutionVersionMethod = "dataverse/updateSolutionVersion";
		private const string ExportSolutionMethod = "dataverse/exportSolution";
		private const string SolutionExistsMethod = "dataverse/solutionExists";
		private const string ImportSolutionMethod = "dataverse/importSolution";
		private const string DeleteAndPromoteMethod = "dataverse/deleteAndPromote";
		private const string RetrieveSolutionsMethod = "dataverse/retrieveSolutions";
		private const string NoExportFileMessage = "The export returned no solution file.";

		private readonly JsonRpc _rpc;
		private readonly int _target;


		// ============================================================================
		/// <summary>
		/// Creates the gateway of one environment.
		/// </summary>
		/// <param name="rpc">The connection to the tool's UI.</param>
		/// <param name="target">The environment's index: 0 = source, 1 to n = targets.</param>
		/// <param name="connectionName">The environment's display name.</param>
		/// <exception cref="ArgumentNullException"><paramref name="rpc"/> is null.</exception>
		public RpcDataverseGateway(
			JsonRpc rpc,
			int target,
			string connectionName)
		{
			_rpc = rpc ?? throw new ArgumentNullException(nameof(rpc));
			_target = target;
			ConnectionName = connectionName;
		}


		// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		/// <summary>
		/// The environment's display name, as the UI passed it to <c>run/start</c>.
		/// </summary>
		public string ConnectionName
		{
			get;
		}


		// ============================================================================
		/// <summary>
		/// Calls <c>dataverse/publishAll</c> (<c>target</c>).
		/// </summary>
		public void PublishAll()
		{
			Invoke<object>(
				PublishAllMethod,
				new
				{
					target = _target
				});
		}


		// ============================================================================
		/// <summary>
		/// Calls <c>dataverse/updateSolutionVersion</c> (<c>target</c>,
		/// <c>solutionId</c>, <c>version</c>).
		/// </summary>
		public void UpdateSolutionVersion(
			Guid solutionId,
			string version)
		{
			Invoke<object>(
				UpdateSolutionVersionMethod,
				new
				{
					target = _target,
					solutionId,
					version
				});
		}


		// ============================================================================
		/// <summary>
		/// Calls <c>dataverse/exportSolution</c> (<c>target</c>, <c>uniqueName</c>,
		/// <c>isManaged</c>), which answers the zip file as a base64 string.
		/// </summary>
		/// <exception cref="InvalidOperationException">The answer holds no file.</exception>
		/// <exception cref="FormatException">The answer is not base64.</exception>
		public byte[] ExportSolution(
			string uniqueName,
			bool isManaged)
		{
			var solutionFileBase64 =
				Invoke<string>(
					ExportSolutionMethod,
					new
					{
						target = _target,
						uniqueName,
						isManaged
					});

			if (string.IsNullOrEmpty(solutionFileBase64))
			{
				throw new InvalidOperationException(NoExportFileMessage);
			}

			return Convert.FromBase64String(solutionFileBase64);
		}


		// ============================================================================
		/// <summary>
		/// Calls <c>dataverse/solutionExists</c> (<c>target</c>, <c>uniqueName</c>).
		/// </summary>
		public bool SolutionExists(
			string uniqueName)
		{
			return
				Invoke<bool>(
					SolutionExistsMethod,
					new
					{
						target = _target,
						uniqueName
					});
		}


		// ============================================================================
		/// <summary>
		/// Calls <c>dataverse/importSolution</c> (<c>target</c>, <c>solutionFile</c>
		/// as base64, <c>isManaged</c>, <c>isHolding</c>, <c>shouldPublishWorkflows</c>,
		/// <c>shouldOverwriteUnmanagedCustomizations</c>).
		/// </summary>
		/// <exception cref="ArgumentNullException">An argument is null.</exception>
		public void ImportSolution(
			byte[] solutionFile,
			ImportOptions options)
		{
			if (solutionFile == null)
			{
				throw new ArgumentNullException(nameof(solutionFile));
			}

			if (options == null)
			{
				throw new ArgumentNullException(nameof(options));
			}

			Invoke<object>(
				ImportSolutionMethod,
				new
				{
					target = _target,
					solutionFile = Convert.ToBase64String(solutionFile),
					isManaged = options.IsManaged,
					isHolding = options.IsHolding,
					shouldPublishWorkflows = options.ShouldPublishWorkflows,
					shouldOverwriteUnmanagedCustomizations = options.ShouldOverwriteUnmanagedCustomizations
				});
		}


		// ============================================================================
		/// <summary>
		/// Calls <c>dataverse/deleteAndPromote</c> (<c>target</c>, <c>uniqueName</c>).
		/// </summary>
		public void DeleteAndPromote(
			string uniqueName)
		{
			Invoke<object>(
				DeleteAndPromoteMethod,
				new
				{
					target = _target,
					uniqueName
				});
		}


		// ============================================================================
		/// <summary>
		/// Calls <c>dataverse/retrieveSolutions</c> (<c>target</c>,
		/// <c>includeManaged</c>), which answers an array of solutions.
		/// </summary>
		/// <returns>The solutions; empty when the answer is <c>null</c>.</returns>
		public IReadOnlyList<SolutionInfo> RetrieveSolutions(
			bool includeManaged)
		{
			var solutions =
				Invoke<List<SolutionInfo>>(
					RetrieveSolutionsMethod,
					new
					{
						target = _target,
						includeManaged
					});

			return solutions ?? new List<SolutionInfo>();
		}


		// ============================================================================
		/// <summary>
		/// Sends one reverse call and blocks until the UI answered.
		/// </summary>
		/// <exception cref="DataverseOperationException">The UI answered with an error.</exception>
		private TResult Invoke<TResult>(
			string methodName,
			object arguments)
		{
			try
			{
				// No cancellation: an abort lets a running Dataverse call finish.
				return
					_rpc.InvokeWithParameterObjectAsync<TResult>(
						methodName,
						arguments,
						CancellationToken.None)
					.GetAwaiter()
					.GetResult();
			}
			catch (RemoteInvocationException exception)
			{
				throw new DataverseOperationException(exception.Message, exception);
			}
		}

	}
}
