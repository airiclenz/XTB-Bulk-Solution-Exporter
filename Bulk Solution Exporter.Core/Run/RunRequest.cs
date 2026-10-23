using System.Collections.Generic;
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
	/// Everything one <see cref="RunEngine"/> run reads: the run's settings and
	/// the solutions it processes. The host fills it when the run starts, from
	/// its own state at that moment, so the run never reads live host state.
	/// The engine does not change the request.
	/// </summary>
	public sealed class RunRequest
	{

		// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		/// <summary>
		/// Publish all customizations in the source environment before the version update.
		/// </summary>
		public bool ShouldPublishSource
		{
			get; set;
		}


		// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		/// <summary>
		/// Advance each solution's version by <see cref="VersionFormat"/> in the source environment.
		/// </summary>
		public bool ShouldUpdateVersion
		{
			get; set;
		}


		// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		/// <summary>
		/// Export each solution as managed, to its configured managed file name.
		/// </summary>
		public bool ShouldExportManaged
		{
			get; set;
		}


		// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		/// <summary>
		/// Export each solution as unmanaged, to its configured unmanaged file name.
		/// </summary>
		public bool ShouldExportUnmanaged
		{
			get; set;
		}


		// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		/// <summary>
		/// Commit the run's exported files to their git repository.
		/// </summary>
		public bool ShouldCommitToGit
		{
			get; set;
		}


		// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		/// <summary>
		/// Push the commit after <see cref="ShouldCommitToGit"/>.
		/// </summary>
		public bool ShouldPushCommit
		{
			get; set;
		}


		// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		/// <summary>
		/// Import each solution's managed file into every target environment.
		/// </summary>
		public bool ShouldImportManaged
		{
			get; set;
		}


		// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		/// <summary>
		/// Import each solution's unmanaged file into every target environment.
		/// </summary>
		public bool ShouldImportUnmanaged
		{
			get; set;
		}


		// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		/// <summary>
		/// Import a managed solution that already exists in the target as an
		/// upgrade (holding solution plus apply-upgrade).
		/// </summary>
		public bool ShouldUpgrade
		{
			get; set;
		}


		// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		/// <summary>
		/// Overwrite unmanaged customizations in the target on import.
		/// </summary>
		public bool ShouldOverwriteCustomizations
		{
			get; set;
		}


		// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		/// <summary>
		/// Activate the imported solutions' processes and flows.
		/// </summary>
		public bool ShouldEnableAutomation
		{
			get; set;
		}


		// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		/// <summary>
		/// Publish all customizations in each target environment after its imports.
		/// </summary>
		public bool ShouldPublishTargets
		{
			get; set;
		}


		// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		/// <summary>
		/// Record each exported file's version in the export folder's
		/// <see cref="ExportedSolutionVersions.FileName"/>.
		/// </summary>
		public bool ShouldSaveVersionJson
		{
			get; set;
		}


		// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		/// <summary>
		/// Continue with the next solution when an import fails.
		/// </summary>
		public bool ShouldContinueOnError
		{
			get; set;
		}


		// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		/// <summary>
		/// The version format, two to four <see cref="VersionTokens"/> joined by dots.
		/// </summary>
		public string VersionFormat
		{
			get; set;
		}


		// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		/// <summary>
		/// The git commit message.
		/// </summary>
		public string CommitMessage
		{
			get; set;
		}


		// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		/// <summary>
		/// How often a retryable Dataverse fault is retried.
		/// </summary>
		public int RetryCount
		{
			get; set;
		}


		// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		/// <summary>
		/// The wait before each retry, in seconds.
		/// </summary>
		public int RetryDelayInSeconds
		{
			get; set;
		}


		// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		/// <summary>
		/// The solutions of the run, in processing order: exactly the solutions
		/// the user checked when the run started.
		/// </summary>
		public List<RunSolution> Solutions
		{
			get; set;
		} = new List<RunSolution>();

	}



	// ============================================================================
	// ============================================================================
	// ============================================================================
	/// <summary>
	/// One solution of a <see cref="RunRequest"/>: its source data paired with
	/// the host's copy of its configuration.
	/// </summary>
	public sealed class RunSolution
	{

		// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		/// <summary>
		/// The solution as read from the source environment.
		/// </summary>
		public SolutionInfo Solution
		{
			get; set;
		}


		// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		/// <summary>
		/// A copy of the solution's configuration, taken when the run started;
		/// <c>null</c> when the solution has none (it then has no file names).
		/// </summary>
		public SolutionConfiguration Configuration
		{
			get; set;
		}


		// ============================================================================
		/// <summary>
		/// Creates an empty entry, for deserializers that need a parameterless constructor.
		/// </summary>
		public RunSolution()
		{
		}


		// ============================================================================
		public RunSolution(
			SolutionInfo solution,
			SolutionConfiguration configuration)
		{
			Solution = solution;
			Configuration = configuration;
		}

	}
}
