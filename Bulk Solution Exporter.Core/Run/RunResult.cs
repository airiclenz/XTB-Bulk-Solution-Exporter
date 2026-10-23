using System;
using System.Collections.Generic;


// ============================================================================
// ============================================================================
// ============================================================================
namespace Com.AiricLenz.BulkSolutionExporter.Core.Run
{

	// ============================================================================
	// ============================================================================
	// ============================================================================
	/// <summary>
	/// How a <see cref="RunEngine"/> run ended: whether it was aborted, how long
	/// it took and the outcome of each export and import step, so the host can
	/// report Done or Aborted. A run that threw has no result.
	/// </summary>
	public sealed class RunResult
	{

		// ============================================================================
		/// <summary>
		/// Creates a result.
		/// </summary>
		/// <exception cref="ArgumentNullException"><paramref name="stepOutcomes"/> is null.</exception>
		public RunResult(
			bool isAborted,
			TimeSpan duration,
			IReadOnlyList<StepOutcome> stepOutcomes)
		{
			IsAborted = isAborted;
			Duration = duration;
			StepOutcomes = stepOutcomes ?? throw new ArgumentNullException(nameof(stepOutcomes));
		}


		// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		/// <summary>
		/// True when an abort was requested during the run; later steps were skipped.
		/// </summary>
		public bool IsAborted
		{
			get;
		}


		// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		/// <summary>
		/// How long the whole run took.
		/// </summary>
		public TimeSpan Duration
		{
			get;
		}


		// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		/// <summary>
		/// One entry per export file and per import attempt, in run order. A step
		/// that was skipped before it started (an abort, an earlier failure
		/// without continue-on-error) has no entry.
		/// </summary>
		public IReadOnlyList<StepOutcome> StepOutcomes
		{
			get;
		}

	}



	// ============================================================================
	// ============================================================================
	// ============================================================================
	/// <summary>
	/// The outcome of one solution's export file or one solution's import into
	/// one target.
	/// </summary>
	public sealed class StepOutcome
	{

		// ============================================================================
		/// <summary>
		/// Creates an outcome.
		/// </summary>
		/// <param name="isImport">True for an import, false for an export.</param>
		/// <param name="solutionId">The solution record id in the source.</param>
		/// <param name="connectionName">The environment the step ran against.</param>
		/// <param name="isManaged">True for the managed file, false for the unmanaged one.</param>
		/// <param name="isSuccessful">True when the step completed.</param>
		/// <param name="duration">How long the step took; zero for a step skipped by validation.</param>
		public StepOutcome(
			bool isImport,
			Guid solutionId,
			string connectionName,
			bool isManaged,
			bool isSuccessful,
			TimeSpan duration)
		{
			IsImport = isImport;
			SolutionId = solutionId;
			ConnectionName = connectionName;
			IsManaged = isManaged;
			IsSuccessful = isSuccessful;
			Duration = duration;
		}


		// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		/// <summary>
		/// True for an import into a target, false for an export from the source.
		/// </summary>
		public bool IsImport
		{
			get;
		}


		// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		/// <summary>
		/// The solution record id in the source.
		/// </summary>
		public Guid SolutionId
		{
			get;
		}


		// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		/// <summary>
		/// The display name of the environment the step ran against.
		/// </summary>
		public string ConnectionName
		{
			get;
		}


		// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		/// <summary>
		/// True for the managed solution file, false for the unmanaged one.
		/// </summary>
		public bool IsManaged
		{
			get;
		}


		// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		/// <summary>
		/// True when the step completed: the file was written, or the import
		/// (and, for an upgrade, its apply-upgrade) finished.
		/// </summary>
		public bool IsSuccessful
		{
			get;
		}


		// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		/// <summary>
		/// How long the step took.
		/// </summary>
		public TimeSpan Duration
		{
			get;
		}

	}
}
