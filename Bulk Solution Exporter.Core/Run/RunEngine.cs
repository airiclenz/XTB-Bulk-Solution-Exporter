using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Com.AiricLenz.BulkSolutionExporter.Core.IO;
using Com.AiricLenz.BulkSolutionExporter.Core.Schema;
using Newtonsoft.Json;


// ============================================================================
// ============================================================================
// ============================================================================
namespace Com.AiricLenz.BulkSolutionExporter.Core.Run
{

	// ============================================================================
	// ============================================================================
	// ============================================================================
	/// <summary>
	/// Runs the steps of one Execute for any host: publish all in the source,
	/// version update, and export to the configured files (plus the export
	/// folder's version file). Dataverse is reached only through
	/// <see cref="IDataverseGateway"/>; everything the host shows goes to
	/// <see cref="IRunSink"/>.
	/// <para>
	/// Abort is the cancellation token, checked between steps, never inside
	/// one: a running Dataverse call finishes and nothing after it starts. The
	/// first observed abort logs <see cref="AbortRequestedMessage"/> once.
	/// </para>
	/// <para>
	/// Errors: a failed source publish all is logged and the run continues. An
	/// invalid version format, a missing file name or an invalid file path is
	/// logged, that step is skipped and the run continues. Any other exception,
	/// such as a failed version update or export, ends the run and is thrown
	/// out of <see cref="Run"/> unchanged. Nothing is retried.
	/// </para>
	/// </summary>
	public sealed class RunEngine
	{

		/// <summary>
		/// The log line of the first observed abort request.
		/// </summary>
		public const string AbortRequestedMessage = "Abort requested — skipping remaining steps.";

		private const string StartTimeFormat = "yyyy-MM-dd HH:mm:ss";
		private const int SecondsPerMinute = 60;

		private readonly RunRequest _request;
		private readonly IDataverseGateway _source;
		private readonly IRunSink _sink;
		private readonly CancellationToken _cancellationToken;
		private readonly RunLog _runLog;
		private readonly List<RunSolution> _solutions;
		private bool _isAbortLogged = false;


		// ============================================================================
		private RunEngine(
			RunRequest request,
			IDataverseGateway source,
			IRunSink sink,
			CancellationToken cancellationToken)
		{
			_request = request;
			_source = source;
			_sink = sink;
			_cancellationToken = cancellationToken;

			_runLog =
				new RunLog(
					sink.Log,
					isDebugEnabled: false);

			_solutions = new List<RunSolution>();

			foreach (var runSolution in request.Solutions)
			{
				_solutions.Add(CopyRunSolution(runSolution));
			}
		}


		// ============================================================================
		/// <summary>
		/// Runs the steps the request switches on, for exactly the request's
		/// solutions, on the calling thread; returns when the run ended or was
		/// aborted.
		/// </summary>
		/// <param name="request">The run's settings and solutions; not changed by the run.</param>
		/// <param name="source">The environment the solutions are versioned in and exported from.</param>
		/// <param name="targets">The environments to import into; the import phase is not part of the engine yet.</param>
		/// <param name="sink">Receives the run's log, progress and changes.</param>
		/// <param name="cancellationToken">Requests an abort between steps.</param>
		/// <exception cref="ArgumentNullException">An argument, or the request's solution list, is null.</exception>
		/// <exception cref="ArgumentException">A request solution entry or its solution is null.</exception>
		public static void Run(
			RunRequest request,
			IDataverseGateway source,
			IReadOnlyList<IDataverseGateway> targets,
			IRunSink sink,
			CancellationToken cancellationToken)
		{
			ValidateArguments(
				request,
				source,
				targets,
				sink);

			var engine =
				new RunEngine(
					request,
					source,
					sink,
					cancellationToken);

			engine.Execute();
		}


		// ============================================================================
		private static void ValidateArguments(
			RunRequest request,
			IDataverseGateway source,
			IReadOnlyList<IDataverseGateway> targets,
			IRunSink sink)
		{
			if (request == null)
			{
				throw new ArgumentNullException(nameof(request));
			}

			if (source == null)
			{
				throw new ArgumentNullException(nameof(source));
			}

			if (targets == null)
			{
				throw new ArgumentNullException(nameof(targets));
			}

			if (sink == null)
			{
				throw new ArgumentNullException(nameof(sink));
			}

			if (request.Solutions == null)
			{
				throw new ArgumentNullException(nameof(request), "The request has no solution list.");
			}

			foreach (var runSolution in request.Solutions)
			{
				if (runSolution?.Solution == null)
				{
					throw new ArgumentException("A request solution entry has no solution.", nameof(request));
				}
			}
		}


		// ============================================================================
		/// <summary>
		/// The run's own copy of a request entry: the run changes its version and
		/// durations without touching the host's objects.
		/// </summary>
		private static RunSolution CopyRunSolution(
			RunSolution runSolution)
		{
			var solution = runSolution.Solution;

			var solutionCopy = new SolutionInfo
			{
				SolutionId = solution.SolutionId,
				UniqueName = solution.UniqueName,
				FriendlyName = solution.FriendlyName,
				Version = CopyVersion(solution.Version),
				IsManaged = solution.IsManaged,
				Description = solution.Description,
			};

			return
				new RunSolution(
					solutionCopy,
					CopyConfiguration(runSolution.Configuration));
		}


		// ============================================================================
		private static Version CopyVersion(
			Version version)
		{
			return
				version == null ?
				null :
				new Version(version.ToString());
		}


		// ============================================================================
		private static SolutionConfiguration CopyConfiguration(
			SolutionConfiguration configuration)
		{
			return
				configuration == null ?
				null :
				SolutionConfiguration.GetConfigFromJson(configuration.GetJson());
		}


		// ============================================================================
		private void Execute()
		{
			try
			{
				if (_request.ShouldPublishSource &&
					!IsAbortRequested())
				{
					_sink.Progress(
						$"Publishing all...{Environment.NewLine}[{_source.ConnectionName}]",
						null,
						isNewStep: true);

					PublishAll(_source);
				}

				if (!IsAbortRequested())
				{
					UpdateVersionNumbers();
				}

				if (!IsAbortRequested())
				{
					ExportSolutions();
				}

				// Logs an abort requested during the last step.
				IsAbortRequested();
			}
			catch
			{
				// Logs an abort requested while the failing step was running.
				IsAbortRequested();
				throw;
			}
		}


		// ============================================================================
		/// <summary>
		/// Returns whether an abort was requested; the first time it is
		/// observed, logs <see cref="AbortRequestedMessage"/>.
		/// </summary>
		private bool IsAbortRequested()
		{
			if (!_cancellationToken.IsCancellationRequested)
			{
				return false;
			}

			if (!_isAbortLogged)
			{
				_isAbortLogged = true;
				_runLog.Log(RunLog.Colorize(RunLog.ColorOrange, AbortRequestedMessage));
			}

			return true;
		}


		// ============================================================================
		/// <summary>
		/// Publishes all in one environment. A failure is logged and the run continues.
		/// </summary>
		private void PublishAll(
			IDataverseGateway gateway)
		{
			_runLog.Log("##### Publishing All now (" + RunLog.Colorize(RunLog.ColorConnection, gateway.ConnectionName) + ")");
			_runLog.IncreaseIndent();

			_runLog.Log("Start Time: " + DateTime.Now.ToString(StartTimeFormat));
			var stopwatch = Stopwatch.StartNew();

			try
			{
				gateway.PublishAll();
			}
			catch (Exception ex)
			{
				// A failed publish leaves the solutions as they are, so the
				// remaining steps still make sense.
				_runLog.LogError(ex.Message);
				_runLog.DecreaseIndent();
				return;
			}

			_runLog.Log("Publish All was completed.");
			_runLog.Log("Duration: " + FormatDuration(stopwatch.Elapsed));

			_runLog.DecreaseIndent();
			_runLog.Log();
		}


		// ============================================================================
		private void UpdateVersionNumbers()
		{
			if (!_request.ShouldUpdateVersion ||
				_solutions.Count == 0)
			{
				return;
			}

			_sink.Progress(
				"Updating Version Numbers...",
				null,
				isNewStep: true);

			_runLog.Log("##### Updating Version Numbers:");
			_runLog.IncreaseIndent();

			for (int i = 0; i < _solutions.Count; i++)
			{
				if (IsAbortRequested())
				{
					break;
				}

				var solution = _solutions[i].Solution;

				_runLog.Log("Updating version number for solution " + FormatSolutionName(solution));

				UpdateVersionNumber(solution);

				if (i < _solutions.Count - 1)
				{
					_runLog.Log();
				}
			}

			_runLog.DecreaseIndent();
			_runLog.Log();
		}


		// ============================================================================
		/// <summary>
		/// Advances the solution's version by the request's format and writes it
		/// to the source. An invalid format or a missing version is logged and
		/// the solution is skipped.
		/// </summary>
		private void UpdateVersionNumber(
			SolutionInfo solution)
		{
			_runLog.IncreaseIndent();

			if (solution.Version == null)
			{
				_runLog.LogError("The solution has no version number!");
				_runLog.DecreaseIndent();
				return;
			}

			var oldVersionString = solution.Version.ToString();
			var newVersion = CopyVersion(solution.Version);

			if (!newVersion.IncreaseVersionNumber(_request.VersionFormat ?? string.Empty))
			{
				_runLog.LogError("The version format is invalid!");
				_runLog.DecreaseIndent();
				return;
			}

			_source.UpdateSolutionVersion(
				solution.SolutionId,
				newVersion.ToString());

			solution.Version = newVersion;

			_sink.SolutionVersionChanged(
				solution.SolutionId,
				CopyVersion(newVersion));

			_runLog.Log("Updated the version number: **" + oldVersionString + "** → **" + newVersion + "**");

			_runLog.DecreaseIndent();
		}


		// ============================================================================
		private void ExportSolutions()
		{
			if (!_request.ShouldExportManaged &&
				!_request.ShouldExportUnmanaged)
			{
				return;
			}

			_runLog.Log("##### Exporting:");
			_runLog.IncreaseIndent();

			for (int i = 0; i < _solutions.Count; i++)
			{
				if (IsAbortRequested())
				{
					break;
				}

				var runSolution = _solutions[i];

				_runLog.Log("**Exporting solution " + FormatSolutionName(runSolution.Solution) + ":**");

				ExportSolution(runSolution);

				if (i < _solutions.Count - 1)
				{
					_runLog.Log();
				}

				_sink.SolutionProcessed(runSolution.Solution.SolutionId);
			}

			_runLog.DecreaseIndent();
			_runLog.Log();
		}


		// ============================================================================
		/// <summary>
		/// Exports one solution as managed and/or unmanaged. An abort between
		/// the two exports skips the second one.
		/// </summary>
		private void ExportSolution(
			RunSolution runSolution)
		{
			_runLog.IncreaseIndent();

			if (_request.ShouldExportManaged)
			{
				ExportAndRecordDuration(
					runSolution,
					isManaged: true);
			}

			if (IsAbortRequested())
			{
				_runLog.DecreaseIndent();
				return;
			}

			if (_request.ShouldExportManaged &&
				_request.ShouldExportUnmanaged)
			{
				_runLog.Log();
			}

			if (_request.ShouldExportUnmanaged)
			{
				ExportAndRecordDuration(
					runSolution,
					isManaged: false);
			}

			_runLog.DecreaseIndent();
		}


		// ============================================================================
		/// <summary>
		/// Exports one file and records the export's duration in the run's
		/// configuration, reported to the sink.
		/// </summary>
		private void ExportAndRecordDuration(
			RunSolution runSolution,
			bool isManaged)
		{
			var configuration = runSolution.Configuration;

			var lastDurationInSeconds =
				configuration == null ? 0 :
				isManaged ? configuration.LastExportDurationManagedInSeconds :
				configuration.LastExportDurationUnamangedInSeconds;

			var lastDurationString =
				lastDurationInSeconds != 0 ?
				$" (Last duration: {FormatDuration(TimeSpan.FromSeconds(lastDurationInSeconds), isMillisecondsOmitted: true)})" :
				null;

			_sink.Progress(
				$"Exporting as {(isManaged ? "" : "un")}managed...{Environment.NewLine}'{runSolution.Solution.FriendlyName}'",
				lastDurationString,
				isNewStep: true);

			var durationInSeconds =
				ExportToFile(
					runSolution,
					isManaged);

			// A sub-second export rounds to 0 and keeps the last duration.
			if (durationInSeconds <= 0 ||
				configuration == null)
			{
				return;
			}

			if (isManaged)
			{
				configuration.LastExportDurationManagedInSeconds = durationInSeconds;
			}
			else
			{
				configuration.LastExportDurationUnamangedInSeconds = durationInSeconds;
			}

			_sink.ConfigurationChanged(CopyConfiguration(configuration));
		}


		// ============================================================================
		/// <summary>
		/// Exports the solution and writes it to its configured file, exactly
		/// as configured.
		/// </summary>
		/// <returns>The export's duration in whole seconds; -1 when the file
		/// name is missing or invalid and nothing was exported.</returns>
		private int ExportToFile(
			RunSolution runSolution,
			bool isManaged)
		{
			var solution = runSolution.Solution;
			var configuration = runSolution.Configuration;
			var managedPrefix = isManaged ? "" : "un";

			_runLog.Log("Exporting as " + managedPrefix + "managed solution now...");
			_runLog.IncreaseIndent();

			var filePath =
				configuration == null ? null :
				isManaged ? configuration.FileNameManaged :
				configuration.FileNameUnmanaged;

			if (string.IsNullOrWhiteSpace(filePath))
			{
				_runLog.LogError("No file name for the " + managedPrefix + "managed solution was given!");
				_runLog.DecreaseIndent();
				return -1;
			}

			// The configured path may come from a hand-edited settings store:
			// never write outside a rooted, valid file path.
			if (!FileAndFolderHelper.IsValidSolutionFilePath(filePath))
			{
				_runLog.LogError("The file path '" + filePath + "' is not a valid absolute file path. The file is skipped.");
				_runLog.DecreaseIndent();
				return -1;
			}

			_runLog.Log("Start Time: " + DateTime.Now.ToString(StartTimeFormat));
			var stopwatch = Stopwatch.StartNew();

			var solutionFile =
				_source.ExportSolution(
					solution.UniqueName,
					isManaged);

			Directory.CreateDirectory(Path.GetDirectoryName(filePath));

			File.WriteAllBytes(
				filePath,
				solutionFile);

			var duration = stopwatch.Elapsed;

			_runLog.Log("Duration: " + FormatDuration(duration));

			if (_request.ShouldSaveVersionJson)
			{
				SaveVersionJson(
					filePath,
					solution,
					isManaged);
			}

			_runLog.DecreaseIndent();
			return (int) duration.TotalSeconds;
		}


		// ============================================================================
		/// <summary>
		/// Records the exported file's solution version in the version file
		/// next to it, adding an entry when the file has none yet.
		/// </summary>
		private static void SaveVersionJson(
			string exportedFilePath,
			SolutionInfo solution,
			bool isManaged)
		{
			var jsonFilePath =
				Path.Combine(
					Path.GetDirectoryName(exportedFilePath),
					ExportedSolutionVersions.FileName);

			var exportedSolutions = new ExportedSolutionVersions();

			if (File.Exists(jsonFilePath))
			{
				var json = File.ReadAllText(jsonFilePath);

				exportedSolutions =
					JsonConvert.DeserializeObject<ExportedSolutionVersions>(json) ??
					new ExportedSolutionVersions();
			}

			var entries =
				isManaged ?
				exportedSolutions.Managed :
				exportedSolutions.Unmanaged;

			var fileName = Path.GetFileName(exportedFilePath);
			var versionString = solution.Version?.ToString();
			var isFound = false;

			foreach (var entry in entries)
			{
				if (entry.Matches(solution.UniqueName, fileName))
				{
					entry.Version = versionString;
					isFound = true;
				}
			}

			if (!isFound)
			{
				entries.Add(
					new ExportedSolution
					{
						SolutionId = solution.SolutionId.ToString().ToLower(),
						FileName = fileName,
						Version = versionString,
						LogicalSolutionName = solution.UniqueName,
					});
			}

			File.WriteAllText(
				jsonFilePath,
				JsonConvert.SerializeObject(exportedSolutions, Formatting.Indented));
		}


		// ============================================================================
		private static string FormatSolutionName(
			SolutionInfo solution)
		{
			return RunLog.Colorize(RunLog.ColorSolution, "'" + solution.FriendlyName + "'");
		}


		// ============================================================================
		/// <summary>
		/// Formats a duration for the log, as "1h 02min 03sec", "2min 03.4sec"
		/// or "3.4sec".
		/// </summary>
		/// <param name="isMillisecondsOmitted">True rounds a duration under a minute to whole seconds.</param>
		private static string FormatDuration(
			TimeSpan duration,
			bool isMillisecondsOmitted = false)
		{
			var hours = duration.Hours;
			var minutes = duration.Minutes;
			var seconds = duration.TotalSeconds % SecondsPerMinute;
			var result = string.Empty;

			if (hours > 0)
			{
				result += $"{hours}h ";
			}

			if (minutes > 0 ||
				hours > 0)
			{
				result +=
					hours > 0 ?
					$"{minutes:D2}min " :
					$"{minutes}min ";
			}

			if (seconds > 0 ||
				hours > 0 ||
				minutes > 0)
			{
				result += FormatSeconds(
					seconds,
					isRounded: minutes > 0 || hours > 0 || isMillisecondsOmitted,
					isPadded: minutes > 0);
			}

			return result.Trim();
		}


		// ============================================================================
		/// <param name="isRounded">True rounds to whole seconds.</param>
		/// <param name="isPadded">True pads whole seconds to two digits.</param>
		private static string FormatSeconds(
			double seconds,
			bool isRounded,
			bool isPadded)
		{
			if (isRounded)
			{
				return
					isPadded ?
					$"{Math.Round(seconds):00}sec" :
					$"{Math.Round(seconds)}sec";
			}

			// Always a dot as the decimal separator, whatever the culture.
			return $"{seconds:F1}sec".Replace(",", ".");
		}

	}
}
