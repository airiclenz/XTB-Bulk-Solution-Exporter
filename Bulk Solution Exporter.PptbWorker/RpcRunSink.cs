using System;
using Com.AiricLenz.BulkSolutionExporter.Core.Run;
using Com.AiricLenz.BulkSolutionExporter.Core.Schema;
using StreamJsonRpc;
using Version = Com.AiricLenz.BulkSolutionExporter.Core.Version;


// ============================================================================
// ============================================================================
// ============================================================================
namespace Com.AiricLenz.BulkSolutionExporter.PptbWorker
{

	// ============================================================================
	// ============================================================================
	// ============================================================================
	/// <summary>
	/// Streams a run's reports to the tool's UI as JSON-RPC notifications with
	/// named parameters:
	/// <c>run/log</c> (<c>line</c>),
	/// <c>run/progress</c> (<c>message</c>, <c>detail</c>, <c>isNewStep</c>),
	/// <c>run/solutionVersionChanged</c> (<c>solutionId</c>, <c>version</c>),
	/// <c>run/configurationChanged</c> (<c>configuration</c>) and
	/// <c>run/solutionProcessed</c> (<c>solutionId</c>).
	/// Each call returns once its notification is written, so the UI receives
	/// them in run order; a notification has no answer, so the UI handles it
	/// after the engine moved on.
	/// </summary>
	public sealed class RpcRunSink : IRunSink
	{

		private const string LogMethod = "run/log";
		private const string ProgressMethod = "run/progress";
		private const string SolutionVersionChangedMethod = "run/solutionVersionChanged";
		private const string ConfigurationChangedMethod = "run/configurationChanged";
		private const string SolutionProcessedMethod = "run/solutionProcessed";

		private readonly JsonRpc _rpc;


		// ============================================================================
		/// <summary>
		/// Creates the sink of one connection.
		/// </summary>
		/// <exception cref="ArgumentNullException"><paramref name="rpc"/> is null.</exception>
		public RpcRunSink(
			JsonRpc rpc)
		{
			_rpc = rpc ?? throw new ArgumentNullException(nameof(rpc));
		}


		// ============================================================================
		/// <summary>
		/// Sends <c>run/log</c>.
		/// </summary>
		public void Log(
			string line)
		{
			Notify(
				LogMethod,
				new
				{
					line
				});
		}


		// ============================================================================
		/// <summary>
		/// Sends <c>run/solutionVersionChanged</c> with the version as its dotted string.
		/// </summary>
		public void SolutionVersionChanged(
			Guid solutionId,
			Version version)
		{
			Notify(
				SolutionVersionChangedMethod,
				new
				{
					solutionId,
					version = version?.ToString()
				});
		}


		// ============================================================================
		/// <summary>
		/// Sends <c>run/configurationChanged</c>.
		/// </summary>
		public void ConfigurationChanged(
			SolutionConfiguration configuration)
		{
			Notify(
				ConfigurationChangedMethod,
				new
				{
					configuration
				});
		}


		// ============================================================================
		/// <summary>
		/// Sends <c>run/progress</c>.
		/// </summary>
		public void Progress(
			string message,
			string detail,
			bool isNewStep)
		{
			Notify(
				ProgressMethod,
				new
				{
					message,
					detail,
					isNewStep
				});
		}


		// ============================================================================
		/// <summary>
		/// Sends <c>run/solutionProcessed</c>.
		/// </summary>
		public void SolutionProcessed(
			Guid solutionId)
		{
			Notify(
				SolutionProcessedMethod,
				new
				{
					solutionId
				});
		}


		// ============================================================================
		/// <summary>
		/// Writes one notification and blocks until it is sent.
		/// </summary>
		private void Notify(
			string methodName,
			object arguments)
		{
			_rpc.NotifyWithParameterObjectAsync(
					methodName,
					arguments)
				.GetAwaiter()
				.GetResult();
		}

	}
}
