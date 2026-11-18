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
	/// Follows a server-side import operation that
	/// <see cref="IDataverseGateway.StartImport"/> began until it reaches a
	/// final state, with no cap on the wait. The wait uses the plain injected
	/// sleep, never the abort check, so an abort takes effect only after the
	/// operation ended. A failed or canceled operation, and an operation whose
	/// state could not be read too many times in a row, end the wait with a
	/// <see cref="DataverseOperationException"/>.
	/// </summary>
	public class ImportOperationWaiter
	{
		// Wait before every read of the operation's state.
		public const int PollIntervalInMilliseconds = 5000;

		// Fault text of an operation that failed without a server message.
		public const string ImportFailedFaultText = "The import operation failed on the server.";

		// Fault text of an operation that was canceled, followed by the server message.
		public const string ImportCanceledFaultText = "The import operation was canceled on the server.";

		private readonly IDataverseGateway _gateway;
		private readonly Action<int> _sleep;
		private readonly Func<DateTime> _getUtcNow;
		private readonly Action<string> _log;
		private readonly Action<TimeSpan> _onPoll;


		// ============================================================================
		/// <summary>
		/// Creates a waiter whose gateway, sleep, clock, logging and progress are
		/// supplied by the caller.
		/// </summary>
		/// <param name="gateway">Reads the operation's state.</param>
		/// <param name="sleep">Blocks for the given number of milliseconds (one poll interval).</param>
		/// <param name="getUtcNow">Returns the current UTC time the elapsed wait is measured against.</param>
		/// <param name="log">Writes an informational log line (a failed read of the state).</param>
		/// <param name="onPoll">Reports the time waited so far, before every read of the state.</param>
		/// <exception cref="ArgumentNullException">Any argument is null.</exception>
		public ImportOperationWaiter(
			IDataverseGateway gateway,
			Action<int> sleep,
			Func<DateTime> getUtcNow,
			Action<string> log,
			Action<TimeSpan> onPoll)
		{
			_gateway = gateway ?? throw new ArgumentNullException(nameof(gateway));
			_sleep = sleep ?? throw new ArgumentNullException(nameof(sleep));
			_getUtcNow = getUtcNow ?? throw new ArgumentNullException(nameof(getUtcNow));
			_log = log ?? throw new ArgumentNullException(nameof(log));
			_onPoll = onPoll ?? throw new ArgumentNullException(nameof(onPoll));
		}


		// ============================================================================
		/// <summary>
		/// Blocks until the import operation reached a final state: waits one
		/// poll interval, reads the state, and repeats while it runs. A failed
		/// read is logged and counted; a good read resets the count.
		/// </summary>
		/// <param name="asyncOperationId">The id <see cref="IDataverseGateway.StartImport"/> returned.</param>
		/// <param name="maxConsecutivePollFailures">Failed reads in a row that are tolerated;
		/// one more ends the wait.</param>
		/// <exception cref="ArgumentOutOfRangeException">maxConsecutivePollFailures is negative.</exception>
		/// <exception cref="DataverseOperationException">The operation failed (the server's message,
		/// unchanged), was canceled (<see cref="ImportCanceledFaultText"/> and the server's message),
		/// or its state could not be read more than maxConsecutivePollFailures times in a row
		/// (<see cref="RetryRunner.ImportTrackingLostFaultText"/> and the id, which
		/// <see cref="RetryRunner"/> never retries).</exception>
		public void WaitForCompletion(
			Guid asyncOperationId,
			int maxConsecutivePollFailures)
		{
			if (maxConsecutivePollFailures < 0)
			{
				throw new ArgumentOutOfRangeException(nameof(maxConsecutivePollFailures));
			}

			var startedAtUtc = _getUtcNow();
			var consecutivePollFailures = 0;

			while (true)
			{
				_sleep(PollIntervalInMilliseconds);
				_onPoll(_getUtcNow() - startedAtUtc);

				ImportOperationState state;

				try
				{
					state = _gateway.GetImportOperationState(asyncOperationId);
				}
				catch (Exception ex)
				{
					consecutivePollFailures++;
					_log($"Reading the state of import operation {asyncOperationId} failed ({consecutivePollFailures} in a row): {ex.Message}");

					if (consecutivePollFailures > maxConsecutivePollFailures)
					{
						throw new DataverseOperationException(
							$"{RetryRunner.ImportTrackingLostFaultText} {asyncOperationId}.",
							ex);
					}

					continue;
				}

				consecutivePollFailures = 0;

				if (state.Status != ImportOperationStatus.Running)
				{
					ThrowUnlessSucceeded(state);
					return;
				}
			}
		}


		// ============================================================================
		/// <summary>
		/// Turns a failed or canceled final state into its fault; a succeeded
		/// state passes.
		/// </summary>
		/// <param name="state">A final state of the operation.</param>
		/// <exception cref="DataverseOperationException">The state is Failed or Canceled.</exception>
		private static void ThrowUnlessSucceeded(
			ImportOperationState state)
		{
			var hasMessage = !string.IsNullOrWhiteSpace(state.Message);

			switch (state.Status)
			{
				case ImportOperationStatus.Succeeded:
					return;

				case ImportOperationStatus.Canceled:
					throw new DataverseOperationException(
						hasMessage
							? $"{ImportCanceledFaultText} {state.Message}"
							: ImportCanceledFaultText);

				default:
					throw new DataverseOperationException(
						hasMessage
							? state.Message
							: ImportFailedFaultText);
			}
		}

	}
}
