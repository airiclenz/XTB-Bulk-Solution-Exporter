using System;
using System.Collections.Generic;
using Com.AiricLenz.BulkSolutionExporter.Core.Run;
using Com.AiricLenz.BulkSolutionExporter.Core.Schema;
using Com.AiricLenz.XTB.Plugin.Schema;


// ============================================================================
// ============================================================================
// ============================================================================
namespace Com.AiricLenz.XTB.Plugin.Run
{

	// ============================================================================
	// ============================================================================
	// ============================================================================
	/// <summary>
	/// Builds the <see cref="RunRequest"/> of one Execute, on the UI thread
	/// when the run starts: the run's switches as the user set them on screen,
	/// the remaining options from <see cref="Settings"/>, and a copy of each
	/// checked solution's configuration. The run then never reads a control
	/// or the live (not thread-safe) settings.
	/// </summary>
	internal static class RunRequestFactory
	{

		// ============================================================================
		/// <summary>
		/// Creates the request. Call it on the UI thread: it reads the settings.
		/// </summary>
		/// <param name="switches">The run options as shown on screen when Execute was pressed.</param>
		/// <param name="settings">Supplies the version-file, retry and continue-on-error options and the solution configurations.</param>
		/// <param name="solutions">The checked solutions, in processing order; null entries are skipped.</param>
		/// <returns>A request that shares no mutable object with <paramref name="settings"/>.</returns>
		/// <exception cref="ArgumentNullException">An argument is null.</exception>
		public static RunRequest Create(
			Switches switches,
			Settings settings,
			IEnumerable<Solution> solutions)
		{
			if (switches == null)
			{
				throw new ArgumentNullException(nameof(switches));
			}

			if (settings == null)
			{
				throw new ArgumentNullException(nameof(settings));
			}

			if (solutions == null)
			{
				throw new ArgumentNullException(nameof(solutions));
			}

			var request = new RunRequest
			{
				ShouldPublishSource = switches.ShouldPublishSource,
				ShouldUpdateVersion = switches.ShouldUpdateVersion,
				ShouldExportManaged = switches.ShouldExportManaged,
				ShouldExportUnmanaged = switches.ShouldExportUnmanaged,
				ShouldCommitToGit = switches.ShouldCommitToGit,
				ShouldPushCommit = switches.ShouldPushCommit,
				ShouldImportManaged = switches.ShouldImportManaged,
				ShouldImportUnmanaged = switches.ShouldImportUnmanaged,
				ShouldUpgrade = switches.ShouldUpgrade,
				ShouldOverwriteCustomizations = switches.ShouldOverwriteCustomizations,
				ShouldEnableAutomation = switches.ShouldEnableAutomation,
				ShouldPublishTargets = switches.ShouldPublishTargets,
				VersionFormat = switches.VersionFormat,
				CommitMessage = switches.CommitMessage,
				ShouldSaveVersionJson = settings.SaveVersionJson,
				RetryCount = settings.RetryCount,
				RetryDelayInSeconds = settings.RetryDelayInSeconds,
				ShouldContinueOnError = settings.ContinueOnError,
			};

			foreach (var solution in solutions)
			{
				if (solution == null)
				{
					continue;
				}

				request.Solutions.Add(
					new RunSolution(
						solution.Info,
						CopyConfiguration(settings, solution)));
			}

			return request;
		}


		// ============================================================================
		/// <summary>
		/// A copy of the solution's configuration: the live object belongs to
		/// the settings, which the UI thread keeps changing during the run.
		/// </summary>
		/// <returns>The copy; null when the solution has no configuration.</returns>
		private static SolutionConfiguration CopyConfiguration(
			Settings settings,
			Solution solution)
		{
			var configuration =
				settings.GetSolutionConfiguration(
					solution.SolutionIdentifier);

			return
				configuration == null ?
				null :
				SolutionConfiguration.GetConfigFromJson(configuration.GetJson());
		}



		// ============================================================================
		// ============================================================================
		// ============================================================================
		/// <summary>
		/// The run options the user sets on screen (switches and text boxes),
		/// read on the UI thread when Execute is pressed.
		/// </summary>
		public sealed class Switches
		{

			// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
			/// <summary>
			/// Publish all customizations in the source before the version update.
			/// </summary>
			public bool ShouldPublishSource
			{
				get; set;
			}


			// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
			/// <summary>
			/// Advance each solution's version by <see cref="VersionFormat"/>.
			/// </summary>
			public bool ShouldUpdateVersion
			{
				get; set;
			}


			// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
			/// <summary>
			/// Export each solution as managed.
			/// </summary>
			public bool ShouldExportManaged
			{
				get; set;
			}


			// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
			/// <summary>
			/// Export each solution as unmanaged.
			/// </summary>
			public bool ShouldExportUnmanaged
			{
				get; set;
			}


			// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
			/// <summary>
			/// Commit the exported files to git.
			/// </summary>
			public bool ShouldCommitToGit
			{
				get; set;
			}


			// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
			/// <summary>
			/// Push the commit.
			/// </summary>
			public bool ShouldPushCommit
			{
				get; set;
			}


			// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
			/// <summary>
			/// Import each solution's managed file into every target.
			/// </summary>
			public bool ShouldImportManaged
			{
				get; set;
			}


			// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
			/// <summary>
			/// Import each solution's unmanaged file into every target.
			/// </summary>
			public bool ShouldImportUnmanaged
			{
				get; set;
			}


			// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
			/// <summary>
			/// Import an installed managed solution as an upgrade.
			/// </summary>
			public bool ShouldUpgrade
			{
				get; set;
			}


			// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
			/// <summary>
			/// Overwrite unmanaged customizations on import.
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
			/// Publish all customizations in each target after its imports.
			/// </summary>
			public bool ShouldPublishTargets
			{
				get; set;
			}


			// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
			/// <summary>
			/// The version format text.
			/// </summary>
			public string VersionFormat
			{
				get; set;
			}


			// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
			/// <summary>
			/// The git commit message text.
			/// </summary>
			public string CommitMessage
			{
				get; set;
			}

		}

	}
}
