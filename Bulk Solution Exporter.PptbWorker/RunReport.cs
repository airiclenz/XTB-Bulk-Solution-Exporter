using System;
using System.Collections.Generic;
using System.Linq;
using Com.AiricLenz.BulkSolutionExporter.Core.Run;


// ============================================================================
// ============================================================================
// ============================================================================
namespace Com.AiricLenz.BulkSolutionExporter.PptbWorker
{

	// ============================================================================
	// ============================================================================
	// ============================================================================
	/// <summary>
	/// The answer of <c>run/start</c>: a <see cref="RunResult"/> in the worker's
	/// wire format, with durations in seconds.
	/// </summary>
	public sealed class RunReport
	{

		// ============================================================================
		/// <summary>
		/// Creates a report.
		/// </summary>
		/// <exception cref="ArgumentNullException"><paramref name="steps"/> is null.</exception>
		public RunReport(
			bool isAborted,
			double durationInSeconds,
			IReadOnlyList<StepReport> steps)
		{
			IsAborted = isAborted;
			DurationInSeconds = durationInSeconds;
			Steps = steps ?? throw new ArgumentNullException(nameof(steps));
		}


		// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		/// <summary>
		/// True when an abort was requested during the run.
		/// </summary>
		public bool IsAborted
		{
			get;
		}


		// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		/// <summary>
		/// How long the whole run took, in seconds.
		/// </summary>
		public double DurationInSeconds
		{
			get;
		}


		// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		/// <summary>
		/// One entry per export file and per import attempt, in run order.
		/// </summary>
		public IReadOnlyList<StepReport> Steps
		{
			get;
		}


		// ============================================================================
		/// <summary>
		/// Converts an engine result.
		/// </summary>
		/// <exception cref="ArgumentNullException"><paramref name="result"/> is null.</exception>
		public static RunReport FromResult(
			RunResult result)
		{
			if (result == null)
			{
				throw new ArgumentNullException(nameof(result));
			}

			var steps =
				result.StepOutcomes
					.Select(outcome => new StepReport(
						outcome.IsImport,
						outcome.SolutionId,
						outcome.ConnectionName,
						outcome.IsManaged,
						outcome.IsSuccessful,
						outcome.Duration.TotalSeconds))
					.ToList();

			return
				new RunReport(
					result.IsAborted,
					result.Duration.TotalSeconds,
					steps);
		}

	}



	// ============================================================================
	// ============================================================================
	// ============================================================================
	/// <summary>
	/// One <see cref="StepOutcome"/> in the worker's wire format.
	/// </summary>
	public sealed class StepReport
	{

		// ============================================================================
		/// <summary>
		/// Creates a step report; see <see cref="StepOutcome"/> for the fields.
		/// </summary>
		public StepReport(
			bool isImport,
			Guid solutionId,
			string connectionName,
			bool isManaged,
			bool isSuccessful,
			double durationInSeconds)
		{
			IsImport = isImport;
			SolutionId = solutionId;
			ConnectionName = connectionName;
			IsManaged = isManaged;
			IsSuccessful = isSuccessful;
			DurationInSeconds = durationInSeconds;
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
		/// True when the step completed.
		/// </summary>
		public bool IsSuccessful
		{
			get;
		}


		// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		/// <summary>
		/// How long the step took, in seconds.
		/// </summary>
		public double DurationInSeconds
		{
			get;
		}

	}
}
