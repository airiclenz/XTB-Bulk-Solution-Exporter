using System;
using Com.AiricLenz.BulkSolutionExporter.Core.Schema;


// ============================================================================
// ============================================================================
// ============================================================================
namespace Com.AiricLenz.BulkSolutionExporter.Core.Run
{

	// ============================================================================
	// ============================================================================
	// ============================================================================
	/// <summary>
	/// Receives everything a <see cref="RunEngine"/> run reports to its host.
	/// Every member is called on the run's thread, one call at a time, and the
	/// engine continues only after the call returned: the host handles each
	/// call synchronously (marshalling to its UI thread and waiting, where it
	/// has one) except <see cref="Progress"/>, which it may hand off without
	/// waiting. Arguments are the sink's own copies; the engine does not touch
	/// them after the call.
	/// </summary>
	public interface IRunSink
	{

		// ============================================================================
		/// <summary>
		/// Appends one finished log line: indent included, in the log's markup,
		/// already length-capped. An empty string is an empty line.
		/// </summary>
		void Log(
			string line);


		// ============================================================================
		/// <summary>
		/// A solution's version number was written to the source environment.
		/// </summary>
		/// <param name="solutionId">The solution record id.</param>
		/// <param name="version">The solution's new version.</param>
		void SolutionVersionChanged(
			Guid solutionId,
			Version version);


		// ============================================================================
		/// <summary>
		/// The run changed a solution's configuration. The run only ever changes
		/// the last export and import durations: a host that keeps a live
		/// configuration merges those fields and keeps its own file names and
		/// check state, which may have changed during the run.
		/// </summary>
		/// <param name="configuration">The run's configuration of the solution, after the change.</param>
		void ConfigurationChanged(
			SolutionConfiguration configuration);


		// ============================================================================
		/// <summary>
		/// The current step changed: a new working message for the host's
		/// progress display.
		/// </summary>
		/// <param name="message">What the run is doing now; may span several lines.</param>
		/// <param name="detail">Extra information such as the step's last duration; <c>null</c> when none.</param>
		/// <param name="isNewStep">True when a new step starts, so a host's step timer restarts.</param>
		void Progress(
			string message,
			string detail,
			bool isNewStep);


		// ============================================================================
		/// <summary>
		/// The run finished its work on one solution in the current phase (its
		/// export, or its import into one target). The host may refresh what it shows for the solution, such as
		/// the file status read from the export folder's version file. It must
		/// finish before returning: the engine's next step may write that file.
		/// </summary>
		/// <param name="solutionId">The solution record id.</param>
		void SolutionProcessed(
			Guid solutionId);

	}
}
