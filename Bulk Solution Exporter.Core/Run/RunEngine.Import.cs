using System;
using System.Diagnostics;
using System.IO;
using Com.AiricLenz.BulkSolutionExporter.Core.IO;
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
	/// The target phase of <see cref="RunEngine"/>: per target, imports the
	/// run's exported files (as an upgrade when the target has the managed
	/// solution and the request upgrades) and optionally publishes all.
	/// </summary>
	public sealed partial class RunEngine
	{

		// Used when the request's retry count or delay is not positive.
		private const int DefaultRetryCount = 3;
		private const int DefaultRetryDelayInSeconds = 5;

		// Dataverse holds a staged, not yet applied upgrade as "<uniqueName>_Upgrade".
		private const string HoldingSolutionSuffix = "_Upgrade";

		private const string TargetSeparatorLine =
			RunLog.ColorTeeth +
			":::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::" +
			RunLog.ColorEndTag;


		// ============================================================================
		/// <summary>
		/// Imports into every target in order and publishes all there when the
		/// request asks for it; an abort skips the remaining targets.
		/// </summary>
		private void HandleTargets()
		{
			foreach (var target in _targets)
			{
				if (IsAbortRequested())
				{
					break;
				}

				_runLog.Log(TargetSeparatorLine);
				_runLog.Log("##### Handling Target *" + RunLog.Colorize(RunLog.ColorConnection, LogMarkup.Escape(target.ConnectionName)) + "*:");
				_runLog.Log(TargetSeparatorLine);
				_runLog.Log();

				ImportSolutions(target);

				if (_request.ShouldPublishTargets &&
					!IsAbortRequested())
				{
					_sink.Progress(
						$"Publishing all...{Environment.NewLine}[{target.ConnectionName}]",
						null,
						isNewStep: true);

					PublishAll(target);
				}
			}
		}


		// ============================================================================
		/// <summary>
		/// Imports the run's solutions into one target, managed when the request
		/// imports managed, else unmanaged. A failed import ends the loop unless
		/// the request continues on error; an abort ends it without an error.
		/// </summary>
		private void ImportSolutions(
			IDataverseGateway target)
		{
			if (!_request.ShouldImportManaged &&
				!_request.ShouldImportUnmanaged)
			{
				return;
			}

			var isManaged = _request.ShouldImportManaged;

			_runLog.Log("##### Importing:");
			_runLog.IncreaseIndent();

			for (int i = 0; i < _solutions.Count; i++)
			{
				if (IsAbortRequested())
				{
					break;
				}

				var runSolution = _solutions[i];

				_runLog.Log("**Importing solution " + FormatSolutionName(runSolution.Solution) + ":**");

				var stopwatch = Stopwatch.StartNew();

				var isImported =
					ImportSolution(
						target,
						runSolution,
						isManaged);

				if (isImported)
				{
					RecordImportSuccess(
						target,
						runSolution,
						isManaged,
						stopwatch.Elapsed,
						isLastSolution: i == _solutions.Count - 1);
				}

				// An import ended by the abort is not reported as an import error.
				if (!isImported &&
					IsAbortRequested())
				{
					break;
				}

				if (!isImported &&
					!_request.ShouldContinueOnError)
				{
					_runLog.LogError("The import was not successful and Continue-On-Error is disabled.");
					break;
				}

				_sink.SolutionProcessed(runSolution.Solution.SolutionId);
			}

			_runLog.DecreaseIndent();
			_runLog.Log();
		}


		// ============================================================================
		/// <summary>
		/// Logs a finished import and records its duration in the run's
		/// configuration, reported to the sink.
		/// </summary>
		private void RecordImportSuccess(
			IDataverseGateway target,
			RunSolution runSolution,
			bool isManaged,
			TimeSpan duration,
			bool isLastSolution)
		{
			_runLog.IncreaseIndent();
			_runLog.Log("The import was successful.");
			_runLog.Log("Duration: " + FormatDuration(duration));

			if (!isLastSolution)
			{
				_runLog.Log();
			}

			_runLog.DecreaseIndent();

			_stepOutcomes.Add(
				new StepOutcome(
					isImport: true,
					runSolution.Solution.SolutionId,
					target.ConnectionName,
					isManaged,
					isSuccessful: true,
					duration));

			// A successful import always has a configuration: its file path came from it.
			var configuration = runSolution.Configuration;
			var durationInSeconds = (int) duration.TotalSeconds;

			if (isManaged)
			{
				configuration.LastImportDurationManagedInSeconds = durationInSeconds;
			}
			else
			{
				configuration.LastImportDurationUnmanagedInSeconds = durationInSeconds;
			}

			_sink.ConfigurationChanged(CopyConfiguration(configuration));
		}


		// ============================================================================
		/// <summary>
		/// Imports one solution's exported file into a target. A managed file
		/// whose solution the target already has is, when the request upgrades,
		/// installed as an upgrade in the same single server operation.
		/// </summary>
		/// <returns>True when the import finished; false when the
		/// file is missing or invalid, the import failed or was given up, or an
		/// abort ended a retry. Never throws for a Dataverse failure.</returns>
		private bool ImportSolution(
			IDataverseGateway target,
			RunSolution runSolution,
			bool isManaged)
		{
			var solution = runSolution.Solution;
			var configuration = runSolution.Configuration;

			var filePath =
				configuration == null ? null :
				isManaged ? configuration.FileNameManaged :
				configuration.FileNameUnmanaged;

			_runLog.IncreaseIndent();

			// The configured path may come from a hand-edited settings store:
			// never read outside a rooted, valid file path.
			if (!FileAndFolderHelper.IsValidSolutionFilePath(filePath))
			{
				_runLog.LogError("The file path '" + LogMarkup.Escape(filePath) + "' is not a valid absolute file path. The file is skipped.");
				_runLog.DecreaseIndent();
				AddSkippedOutcome(solution, target, isManaged, isImport: true);
				return false;
			}

			if (!File.Exists(filePath))
			{
				_runLog.LogError("The file '" + LogMarkup.Escape(filePath) + "' does not exist.");
				_runLog.DecreaseIndent();
				AddSkippedOutcome(solution, target, isManaged, isImport: true);
				return false;
			}

			var solutionFile = File.ReadAllBytes(filePath);
			var stopwatch = Stopwatch.StartNew();
			bool isImported;

			try
			{
				isImported =
					InstallSolution(
						target,
						runSolution,
						solutionFile,
						isManaged);
			}
			catch (Exception ex)
			{
				// RetryRunner rethrows a non-retryable fault when the request
				// does not continue on error: that fails this solution only.
				_runLog.LogError(
					Formatter.FormatErrorStringWithXml(
						ex.Message,
						RunLog.IndentUnit));

				_runLog.Log();
				isImported = false;
			}

			_runLog.DecreaseIndent();

			if (!isImported)
			{
				_stepOutcomes.Add(
					new StepOutcome(
						isImport: true,
						solution.SolutionId,
						target.ConnectionName,
						isManaged,
						isSuccessful: false,
						stopwatch.Elapsed));
			}

			return isImported;
		}


		// ============================================================================
		/// <summary>
		/// Installs the solution file as one server-side import operation
		/// (an upgrade when the target has the managed solution and the request
		/// upgrades) and waits for it to end; starting and waiting are retried
		/// together, so a busy target re-submits the import. An upgrade first
		/// applies a pending upgrade the target still holds.
		/// </summary>
		/// <returns>True when the operation succeeded; false when applying a
		/// pending upgrade failed, the operation was given up, the run lost track
		/// of it, or an abort ended a retry.</returns>
		/// <exception cref="Exception">A non-retryable fault while the request
		/// does not continue on error.</exception>
		private bool InstallSolution(
			IDataverseGateway target,
			RunSolution runSolution,
			byte[] solutionFile,
			bool isManaged)
		{
			var solution = runSolution.Solution;

			var isInstalledAlready =
				DoesSolutionExist(
					target,
					solution.UniqueName);

			var isUpgrade =
				isManaged &&
				isInstalledAlready &&
				_request.ShouldUpgrade;

			var options = new ImportOptions
			{
				IsManaged = isManaged,
				IsUpgrade = isUpgrade,
				ShouldPublishWorkflows = _request.ShouldEnableAutomation,
				ShouldOverwriteUnmanagedCustomizations = _request.ShouldOverwriteCustomizations,
			};

			var lastDurationString = FormatLastImportDuration(runSolution.Configuration, isManaged);
			var stepName = isUpgrade ? "upgrade" : "update";
			var targetSuffix = $"'{solution.FriendlyName}'{Environment.NewLine}[{target.ConnectionName}]...";

			if (isUpgrade &&
				!ApplyPendingUpgrade(target, solution, targetSuffix))
			{
				return false;
			}

			_runLog.Log("Installing the solution " + stepName + "...");
			_runLog.Log("Start Time: " + DateTime.Now.ToString(StartTimeFormat) + (isUpgrade ? lastDurationString : null));

			_sink.Progress(
				$"Installing {stepName}: {targetSuffix}",
				lastDurationString,
				isNewStep: true);

			// The poll sleep ignores the abort: a started operation is followed to its end.
			// Its messages carry server text and no markup: each is escaped whole.
			var waiter =
				new ImportOperationWaiter(
					target,
					_pollSleep,
					() => DateTime.UtcNow,
					message => _runLog.Log(LogMarkup.Escape(message)),
					elapsed => _sink.Progress(
						$"Installing {stepName}: {targetSuffix} ({FormatDuration(elapsed, isMillisecondsOmitted: true)})",
						lastDurationString,
						isNewStep: false));

			var maxPollFailures = GetMaxRetries();

			return
				ExecuteWithRetries(
					() =>
					{
						var asyncOperationId = target.StartImport(solutionFile, options);
						waiter.WaitForCompletion(asyncOperationId, maxPollFailures);
					},
					$"Retry {{0}}/{{1}}: Installing solution: {targetSuffix}",
					$"installing solution '{solution.FriendlyName}'");
		}


		// ============================================================================
		/// <summary>
		/// Applies the solution's pending upgrade (<c>&lt;uniqueName&gt;_Upgrade</c>)
		/// on the target with <c>DeleteAndPromote</c> when the target has one, so
		/// this run's upgrade can be installed after it.
		/// </summary>
		/// <param name="targetSuffix">Names the solution and the target in progress messages.</param>
		/// <returns>True when no pending upgrade exists or it was applied; false
		/// when applying it failed or was given up, or the abort was requested.</returns>
		/// <exception cref="Exception">A non-retryable fault while the request
		/// does not continue on error.</exception>
		private bool ApplyPendingUpgrade(
			IDataverseGateway target,
			SolutionInfo solution,
			string targetSuffix)
		{
			var pendingUpgradeName = solution.UniqueName + HoldingSolutionSuffix;

			if (!DoesSolutionExist(
					target,
					pendingUpgradeName))
			{
				return true;
			}

			_runLog.Log(
				RunLog.Colorize(
					RunLog.ColorOrange,
					$"A pending upgrade '{LogMarkup.Escape(pendingUpgradeName)}' exists on the target; applying it first."));

			_sink.Progress(
				$"Applying pending upgrade: {targetSuffix}",
				null,
				isNewStep: true);

			var isApplied =
				ExecuteWithRetries(
					() => target.DeleteAndPromote(solution.UniqueName),
					$"Retry {{0}}/{{1}}: Applying pending upgrade: {targetSuffix}",
					$"applying pending upgrade '{pendingUpgradeName}'");

			// The apply is a step of its own: an abort requested during it skips the install.
			return
				isApplied &&
				!IsAbortRequested();
		}


		// ============================================================================
		/// <summary>
		/// The progress detail with the solution's last import duration; null
		/// when none was recorded.
		/// </summary>
		private static string FormatLastImportDuration(
			SolutionConfiguration configuration,
			bool isManaged)
		{
			var lastDurationInSeconds =
				isManaged ?
				configuration.LastImportDurationManagedInSeconds :
				configuration.LastImportDurationUnmanagedInSeconds;

			return
				lastDurationInSeconds > 0 ?
				$" (Last Import: {FormatDuration(TimeSpan.FromSeconds(lastDurationInSeconds), isMillisecondsOmitted: true)})" :
				null;
		}


		// ============================================================================
		/// <summary>
		/// Runs one target call through <see cref="RetryRunner"/> with the
		/// request's retry settings, reporting each retry to the sink. The
		/// retry wait ends at once on abort.
		/// </summary>
		/// <param name="retryMessageFormat">The progress message of a retry; {0} is the retry, {1} the maximum.</param>
		/// <param name="operationDescription">Names the call in log lines; raw
		/// text, since every <see cref="RetryRunner"/> message is escaped whole.</param>
		/// <returns>True only when the call completed.</returns>
		/// <exception cref="Exception">A non-retryable fault while the request
		/// does not continue on error.</exception>
		private bool ExecuteWithRetries(
			Action action,
			string retryMessageFormat,
			string operationDescription)
		{
			var maxRetries = GetMaxRetries();

			var retryDelayInSeconds =
				_request.RetryDelayInSeconds > 0 ?
				_request.RetryDelayInSeconds :
				DefaultRetryDelayInSeconds;

			// RetryRunner messages carry exception text and no markup: each is escaped whole.
			var retryRunner =
				new RetryRunner(
					IsAbortRequested,
					message => _runLog.Log(LogMarkup.Escape(message)),
					message => _runLog.LogError(LogMarkup.Escape(message)),
					(currentRetry, maxRetryCount) => _sink.Progress(
						string.Format(retryMessageFormat, currentRetry, maxRetryCount),
						null,
						isNewStep: false),
					milliseconds => _cancellationToken.WaitHandle.WaitOne(milliseconds),
					() => DateTime.UtcNow);

			return
				retryRunner.Execute(
					action,
					maxRetries,
					retryDelayInSeconds,
					operationDescription,
					_request.ShouldContinueOnError);
		}


		// ============================================================================
		/// <summary>
		/// The request's retry count, or <see cref="DefaultRetryCount"/> when it
		/// is not positive.
		/// </summary>
		private int GetMaxRetries()
		{
			return
				_request.RetryCount > 0 ?
				_request.RetryCount :
				DefaultRetryCount;
		}


		// ============================================================================
		/// <summary>
		/// Returns whether the target has the solution. A failed check is
		/// logged and counts as not installed, so the file is imported as a
		/// plain update.
		/// </summary>
		private bool DoesSolutionExist(
			IDataverseGateway target,
			string uniqueName)
		{
			if (string.IsNullOrEmpty(uniqueName))
			{
				return false;
			}

			try
			{
				return target.SolutionExists(uniqueName);
			}
			catch (Exception ex)
			{
				_runLog.LogError($"Error checking if solution exists in target: {LogMarkup.Escape(ex.Message)}");
				return false;
			}
		}

	}
}
