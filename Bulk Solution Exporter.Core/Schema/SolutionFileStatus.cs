using System;
using System.IO;
using Com.AiricLenz.BulkSolutionExporter.Core.IO;


// ============================================================================
// ============================================================================
// ============================================================================
namespace Com.AiricLenz.BulkSolutionExporter.Core.Schema
{

	// ============================================================================
	// ============================================================================
	// ============================================================================
	/// <summary>
	/// The state of a solution's two export files: whether each is configured
	/// and present, which version <see cref="ExportedSolutionVersions.FileName"/>
	/// records for it and whether it is outdated against the solution's
	/// current version.
	/// </summary>
	public sealed class SolutionFileStatus
	{

		// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		/// <summary>
		/// The unique name of the solution the status belongs to.
		/// </summary>
		public string UniqueName
		{
			get; set;
		}


		// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		/// <summary>
		/// The managed export file's state.
		/// </summary>
		public ExportFileStatus Managed
		{
			get; set;
		}


		// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		/// <summary>
		/// The unmanaged export file's state.
		/// </summary>
		public ExportFileStatus Unmanaged
		{
			get; set;
		}


		// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		/// <summary>
		/// Whether either export file is outdated.
		/// </summary>
		public bool IsOutdated
		{
			get
			{
				return
					(Managed?.IsOutdated ?? false) ||
					(Unmanaged?.IsOutdated ?? false);
			}
		}


		// ============================================================================
		/// <summary>
		/// Determines the state of the solution's configured export files.
		/// Staleness is only judged when the version file is in use
		/// (<paramref name="isVersionJsonEnabled"/>): a configured file is then
		/// outdated when it is missing, or when the version file records a
		/// version other than the solution's current one. Without the version
		/// file no export file is outdated. Never throws for a malformed path:
		/// it counts as a missing file.
		/// </summary>
		/// <param name="solution">The solution with its current version.</param>
		/// <param name="configuration">The solution's export file paths.</param>
		/// <param name="isVersionJsonEnabled">Whether exports write the version file (the "save version JSON" setting).</param>
		/// <exception cref="ArgumentNullException"><paramref name="solution"/> or <paramref name="configuration"/> is null.</exception>
		public static SolutionFileStatus Evaluate(
			SolutionInfo solution,
			SolutionConfiguration configuration,
			bool isVersionJsonEnabled)
		{
			if (solution == null)
			{
				throw new ArgumentNullException(nameof(solution));
			}

			if (configuration == null)
			{
				throw new ArgumentNullException(nameof(configuration));
			}

			return new SolutionFileStatus
			{
				UniqueName = solution.UniqueName,
				Managed =
					ExportFileStatus.Evaluate(
						configuration.FileNameManaged,
						solution,
						isVersionJsonEnabled),
				Unmanaged =
					ExportFileStatus.Evaluate(
						configuration.FileNameUnmanaged,
						solution,
						isVersionJsonEnabled)
			};
		}

	}



	// ============================================================================
	// ============================================================================
	// ============================================================================
	/// <summary>
	/// The state of one export file of a solution.
	/// </summary>
	public sealed class ExportFileStatus
	{

		// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		/// <summary>
		/// Whether a file path is configured for this export.
		/// </summary>
		public bool IsConfigured
		{
			get; set;
		}


		// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		/// <summary>
		/// Whether the configured file exists.
		/// </summary>
		public bool IsPresent
		{
			get; set;
		}


		// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		/// <summary>
		/// The version the version file records for this export file, or
		/// <c>null</c> when it records none or the version file is not in use.
		/// </summary>
		public string ExportedVersion
		{
			get; set;
		}


		// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		/// <summary>
		/// Whether the file is missing or holds another version than the
		/// solution's current one; always false while the version file is not in use.
		/// </summary>
		public bool IsOutdated
		{
			get; set;
		}


		// ============================================================================
		/// <summary>
		/// Determines one export file's state; see <see cref="SolutionFileStatus.Evaluate"/>.
		/// </summary>
		internal static ExportFileStatus Evaluate(
			string filePath,
			SolutionInfo solution,
			bool isVersionJsonEnabled)
		{
			var status = new ExportFileStatus
			{
				IsConfigured = !string.IsNullOrWhiteSpace(filePath)
			};

			if (!status.IsConfigured)
			{
				return status;
			}

			status.IsPresent = File.Exists(filePath);

			if (!isVersionJsonEnabled)
			{
				return status;
			}

			// The version file is read from the export file's folder, so only
			// a well-formed path is looked up; any other counts as unrecorded.
			ExportedSolution exportedSolution = null;

			if (FileAndFolderHelper.IsValidSolutionFilePath(filePath))
			{
				ExportedSolutionVersions.TryLoadExportedSolution(
					filePath,
					solution.UniqueName,
					out exportedSolution);
			}

			if (exportedSolution == null)
			{
				status.IsOutdated = !status.IsPresent;
				return status;
			}

			status.ExportedVersion = exportedSolution.Version;
			status.IsOutdated =
				exportedSolution.Version != solution.Version?.ToString() ||
				!status.IsPresent;

			return status;
		}

	}
}
