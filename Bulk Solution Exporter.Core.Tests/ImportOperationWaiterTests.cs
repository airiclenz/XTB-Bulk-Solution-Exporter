using System;
using System.Collections.Generic;
using Com.AiricLenz.BulkSolutionExporter.Core.Run;
using Com.AiricLenz.BulkSolutionExporter.Core.Schema;
using Microsoft.VisualStudio.TestTools.UnitTesting;


// ============================================================================
// ============================================================================
// ============================================================================
namespace Com.AiricLenz.BulkSolutionExporter.Core.Tests
{

	// ============================================================================
	// ============================================================================
	// ============================================================================
	/// <summary>
	/// Covers how <see cref="ImportOperationWaiter"/> follows an import
	/// operation to its end: the final states, and failed reads of the state
	/// within and beyond the tolerated limit.
	/// The clock and the sleep are fakes, so no test waits on real time.
	/// </summary>
	[TestClass]
	public class ImportOperationWaiterTests
	{

		private const int MaxConsecutivePollFailures = 2;
		private const int RunningPollsBeforeSuccess = 2;
		private const string ServerFailureMessage = "The solution file is invalid.";
		private const string ServerCancelMessage = "Canceled by an administrator.";
		private const string PollFaultMessage = "The request timed out.";

		private static readonly Guid OperationId = new Guid("6f1c2a3b-0000-4000-8000-000000000042");


		// ============================================================================
		[TestMethod]
		public void WaitForCompletion_RunningTwiceThenSucceeded_ReturnsAfterThreePolls()
		{
			// Arrange
			var gateway = new PollingGateway();

			for (var poll = 0; poll < RunningPollsBeforeSuccess; poll++)
			{
				gateway.EnqueueState(ImportOperationStatus.Running);
			}

			gateway.EnqueueState(ImportOperationStatus.Succeeded);
			var waiter = gateway.CreateWaiter();

			// Act
			waiter.WaitForCompletion(OperationId, MaxConsecutivePollFailures);

			// Assert
			Assert.AreEqual(RunningPollsBeforeSuccess + 1, gateway.PollCount);
		}


		// ============================================================================
		[TestMethod]
		public void WaitForCompletion_Failed_ThrowsWithServerMessage()
		{
			// Arrange
			var gateway = new PollingGateway();
			gateway.EnqueueState(ImportOperationStatus.Failed, ServerFailureMessage);
			var waiter = gateway.CreateWaiter();

			// Act
			var exception = Assert.ThrowsException<DataverseOperationException>(
				() => waiter.WaitForCompletion(OperationId, MaxConsecutivePollFailures));

			// Assert
			Assert.AreEqual(ServerFailureMessage, exception.Message);
		}


		// ============================================================================
		[TestMethod]
		public void WaitForCompletion_Canceled_ThrowsCanceledTextWithServerMessage()
		{
			// Arrange
			var gateway = new PollingGateway();
			gateway.EnqueueState(ImportOperationStatus.Canceled, ServerCancelMessage);
			var waiter = gateway.CreateWaiter();

			// Act
			var exception = Assert.ThrowsException<DataverseOperationException>(
				() => waiter.WaitForCompletion(OperationId, MaxConsecutivePollFailures));

			// Assert
			Assert.AreEqual(
				$"{ImportOperationWaiter.ImportCanceledFaultText} {ServerCancelMessage}",
				exception.Message);
		}


		// ============================================================================
		[TestMethod]
		public void WaitForCompletion_PollFailuresWithinLimitThenSucceeded_Returns()
		{
			// Arrange
			var gateway = new PollingGateway();

			for (var failure = 0; failure < MaxConsecutivePollFailures; failure++)
			{
				gateway.EnqueuePollFault(PollFaultMessage);
			}

			gateway.EnqueueState(ImportOperationStatus.Running);

			for (var failure = 0; failure < MaxConsecutivePollFailures; failure++)
			{
				gateway.EnqueuePollFault(PollFaultMessage);
			}

			gateway.EnqueueState(ImportOperationStatus.Succeeded);
			var waiter = gateway.CreateWaiter();

			// Act
			waiter.WaitForCompletion(OperationId, MaxConsecutivePollFailures);

			// Assert
			var failedPollsThenGoodPoll = MaxConsecutivePollFailures + 1;
			Assert.AreEqual(failedPollsThenGoodPoll + failedPollsThenGoodPoll, gateway.PollCount);
		}


		// ============================================================================
		[TestMethod]
		public void WaitForCompletion_PollFailuresBeyondLimit_ThrowsTrackingLost()
		{
			// Arrange
			var gateway = new PollingGateway();

			for (var failure = 0; failure <= MaxConsecutivePollFailures; failure++)
			{
				gateway.EnqueuePollFault(PollFaultMessage);
			}

			gateway.EnqueueState(ImportOperationStatus.Succeeded);
			var waiter = gateway.CreateWaiter();

			// Act
			var exception = Assert.ThrowsException<DataverseOperationException>(
				() => waiter.WaitForCompletion(OperationId, MaxConsecutivePollFailures));

			// Assert
			Assert.AreEqual(
				$"{RetryRunner.ImportTrackingLostFaultText} {OperationId}.",
				exception.Message);
		}




		// ============================================================================
		// ============================================================================
		// ============================================================================
		/// <summary>
		/// A gateway that answers only <see cref="GetImportOperationState"/>,
		/// from a queue of states and faults, and builds the waiter under test
		/// with a sleep that advances a fake clock instead of blocking.
		/// </summary>
		private sealed class PollingGateway : IDataverseGateway
		{
			private readonly Queue<Func<ImportOperationState>> _pollAnswers =
				new Queue<Func<ImportOperationState>>();

			private DateTime _utcNow = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);


			// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
			public int PollCount { get; private set; }

			// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
			public string ConnectionName => "Test environment";


			// ============================================================================
			public ImportOperationWaiter CreateWaiter()
			{
				return new ImportOperationWaiter(
					this,
					milliseconds => _utcNow = _utcNow.AddMilliseconds(milliseconds),
					() => _utcNow,
					message => { },
					elapsed => { });
			}


			// ============================================================================
			public void EnqueueState(
				ImportOperationStatus status,
				string message = null)
			{
				_pollAnswers.Enqueue(
					() => new ImportOperationState
					{
						Status = status,
						Message = message
					});
			}


			// ============================================================================
			public void EnqueuePollFault(
				string message)
			{
				_pollAnswers.Enqueue(
					() => throw new DataverseOperationException(message));
			}


			// ============================================================================
			public ImportOperationState GetImportOperationState(
				Guid asyncOperationId)
			{
				PollCount++;
				return _pollAnswers.Dequeue()();
			}


			// ============================================================================
			public void PublishAll()
			{
				throw new NotSupportedException();
			}


			// ============================================================================
			public void UpdateSolutionVersion(
				Guid solutionId,
				string version)
			{
				throw new NotSupportedException();
			}


			// ============================================================================
			public byte[] ExportSolution(
				string uniqueName,
				bool isManaged)
			{
				throw new NotSupportedException();
			}


			// ============================================================================
			public bool SolutionExists(
				string uniqueName)
			{
				throw new NotSupportedException();
			}


			// ============================================================================
			public void ImportSolution(
				byte[] solutionFile,
				ImportOptions options)
			{
				throw new NotSupportedException();
			}


			// ============================================================================
			public Guid StartImport(
				byte[] solutionFile,
				ImportOptions options)
			{
				throw new NotSupportedException();
			}


			// ============================================================================
			public void DeleteAndPromote(
				string uniqueName)
			{
				throw new NotSupportedException();
			}


			// ============================================================================
			public IReadOnlyList<SolutionInfo> RetrieveSolutions(
				bool includeManaged)
			{
				throw new NotSupportedException();
			}
		}

	}
}
