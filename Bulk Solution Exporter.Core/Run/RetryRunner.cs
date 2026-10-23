using System;


// ============================================================================
// ============================================================================
// ============================================================================
namespace Com.AiricLenz.BulkSolutionExporter.Core.Run
{

	// ============================================================================
	// ============================================================================
	// ============================================================================
	/// <summary>
	/// Runs an action, retrying it after a delay when it fails.
	/// The first attempt always runs; an abort request ends the retry
	/// delay early and starts no further retry.
	/// Dataverse faults are told apart by their message text, searched in the
	/// exception and every inner exception, so any host's exception type works.
	/// </summary>
	public class RetryRunner
	{
		// Longest single sleep of the retry wait: an abort ends the wait within this time.
		public const int RetryWaitSliceInMilliseconds = 250;

		// Fault text of a target that is still busy with another import: retried.
		public const string ImportAlreadyRunningFaultText =
			"Cannot start another [Import] because there is a previous [Import] running at this moment";

		// Fault text of a target that already holds this solution's upgrade: never retried.
		public const string UpgradeAlreadyExistsFaultText = "_Upgrade already exists";

		private readonly Func<bool> _isAbortRequested;
		private readonly Action<string> _log;
		private readonly Action<string> _logError;
		private readonly Action<int, int> _onRetry;
		private readonly Action<int> _sleep;
		private readonly Func<DateTime> _getUtcNow;


		// ============================================================================
		/// <summary>
		/// Creates a runner whose abort check, logging, progress, sleep and
		/// clock are supplied by the caller.
		/// </summary>
		/// <param name="isAbortRequested">Returns whether the user requested an abort; asked before every retry and after every wait slice.</param>
		/// <param name="log">Writes an informational log line.</param>
		/// <param name="logError">Writes an error log line.</param>
		/// <param name="onRetry">Reports a starting retry as (current retry, maximum retries).</param>
		/// <param name="sleep">Blocks for the given number of milliseconds (one wait slice).</param>
		/// <param name="getUtcNow">Returns the current UTC time the retry delay is measured against.</param>
		/// <exception cref="ArgumentNullException">Any argument is null.</exception>
		public RetryRunner(
			Func<bool> isAbortRequested,
			Action<string> log,
			Action<string> logError,
			Action<int, int> onRetry,
			Action<int> sleep,
			Func<DateTime> getUtcNow)
		{
			_isAbortRequested = isAbortRequested ?? throw new ArgumentNullException(nameof(isAbortRequested));
			_log = log ?? throw new ArgumentNullException(nameof(log));
			_logError = logError ?? throw new ArgumentNullException(nameof(logError));
			_onRetry = onRetry ?? throw new ArgumentNullException(nameof(onRetry));
			_sleep = sleep ?? throw new ArgumentNullException(nameof(sleep));
			_getUtcNow = getUtcNow ?? throw new ArgumentNullException(nameof(getUtcNow));
		}


		// ============================================================================
		/// <summary>
		/// Runs the action, retrying it after a delay when it fails.
		/// The first attempt always runs; an abort request ends the retry
		/// delay early and starts no further retry.
		/// </summary>
		/// <param name="action">The operation to run.</param>
		/// <param name="maxRetries">Number of retries after the first attempt.</param>
		/// <param name="retryDelaySeconds">Wait before each retry.</param>
		/// <param name="operationDescription">Names the operation in log lines.</param>
		/// <param name="continueOnError">False rethrows a non-retryable error on the attempt that raised it.
		/// A "Cannot start another [Import]" fault is retried either way.</param>
		/// <returns>True only when the action completed; false when the abort was requested
		/// before a retry could run, when the target already holds the solution's upgrade,
		/// or when every attempt failed.</returns>
		/// <exception cref="Exception">continueOnError is false and the action raised an error
		/// other than "Cannot start another [Import]" or "_Upgrade already exists".</exception>
		public bool Execute(
			Action action,
			int maxRetries,
			int retryDelaySeconds,
			string operationDescription = null,
			bool continueOnError = false)
		{
			int currentRetry = 0;
			bool success = false;

			while (currentRetry <= maxRetries && !success)
			{
				try
				{
					if (currentRetry > 0)
					{
						if (_isAbortRequested())
						{
							return false;
						}

						_log($"Retry attempt {currentRetry} of {maxRetries} for {operationDescription ?? "operation"}...");
						_onRetry(currentRetry, maxRetries);

						// Wait before retrying, in short slices so an abort ends the wait at once
						var retryAt = _getUtcNow().AddSeconds(retryDelaySeconds);

						while (_getUtcNow() < retryAt)
						{
							var remainingMilliseconds = (int) (retryAt - _getUtcNow()).TotalMilliseconds;
							_sleep(Math.Max(0, Math.Min(RetryWaitSliceInMilliseconds, remainingMilliseconds)));

							if (_isAbortRequested())
							{
								return false;
							}
						}
					}

					action();
					success = true;

					if (currentRetry > 0)
					{
						_log($"Succeeded on retry {currentRetry}.");
					}
				}
				// ------------------------------------------
				catch (Exception ex) when (
					IsMessageInChain(ex, ImportAlreadyRunningFaultText))
				{
					// The target is busy with another import: wait and retry, whatever continueOnError says
					_log($"Another import is still running on the target; {operationDescription ?? "operation"} will be retried.");
				}
				// ------------------------------------------
				catch (Exception ex) when (
					IsMessageInChain(ex, UpgradeAlreadyExistsFaultText))
				{
					// A pending upgrade blocks this one; retrying cannot clear it
					_logError($"Error with {operationDescription}: {ex.Message}");
					return false;
				}
				// ------------------------------------------
				catch (Exception ex)
				{
					// Non-retryable error: preserve original behavior (log + rethrow)
					_logError($"Error with {operationDescription}: {ex.Message}");
					if (!continueOnError)
					{
						throw;
					}
				}

				currentRetry++;
			}

			if (!success)
			{
				_logError($"Gave up on {operationDescription ?? "operation"} after {maxRetries} retries.");
			}

			return success;
		}


		// ============================================================================
		/// <summary>
		/// Decides whether a finished execution run counts as aborted by the user.
		/// </summary>
		/// <param name="isCancelled">The worker marked the run cancelled (args.Cancelled).</param>
		/// <param name="isAbortRequested">The abort latch was set during the run; this also
		/// covers a step that threw after the abort request, where the run is not marked cancelled.</param>
		public static bool IsRunAborted(
			bool isCancelled,
			bool isAbortRequested)
		{
			return isCancelled || isAbortRequested;
		}


		// ============================================================================
		/// <summary>
		/// Tells whether the exception or any of its inner exceptions carries the
		/// fault text in its message; a host may wrap the Dataverse fault.
		/// </summary>
		/// <param name="exception">The raised exception; null yields false.</param>
		/// <param name="faultText">The Dataverse fault text to look for (ordinal match).</param>
		private static bool IsMessageInChain(
			Exception exception,
			string faultText)
		{
			for (var current = exception; current != null; current = current.InnerException)
			{
				if (current.Message != null &&
					current.Message.IndexOf(faultText, StringComparison.Ordinal) >= 0)
				{
					return true;
				}
			}

			return false;
		}

	}
}
