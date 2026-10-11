using System;
using Com.AiricLenz.XTB.Plugin.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;


// ============================================================================
// ============================================================================
// ============================================================================
namespace Com.AiricLenz.XTB.Plugin.Tests
{

	// ============================================================================
	// ============================================================================
	// ============================================================================
	/// <summary>
	/// Covers the retry loop of <see cref="RetryRunner"/> and the abort
	/// handshake between the retry wait, the action and the post-work
	/// decision <see cref="RetryRunner.IsRunAborted"/>.
	/// The clock and the sleep are fakes, so no test waits on real time.
	/// </summary>
	[TestClass]
	public class RetryRunnerTests
	{

		private const int MaxRetries = 3;
		private const int RetryDelaySeconds = 1;
		private const int SleepCallAbortingTheWait = 2;
		private const string OperationDescription = "test operation";


		// ============================================================================
		[TestMethod]
		public void Execute_AbortDuringSecondWaitSlice_ReturnsFalseWithoutFurtherSleep()
		{
			// Arrange
			var environment = new FakeRetryEnvironment
			{
				AbortOnSleepCall = SleepCallAbortingTheWait
			};
			var runner = environment.CreateRunner();
			var actionCallCount = 0;

			// Act
			var isCompleted = runner.Execute(
				() =>
				{
					actionCallCount++;
					throw new Exception("retryable");
				},
				MaxRetries,
				RetryDelaySeconds,
				OperationDescription,
				continueOnError: true);

			// Assert
			Assert.IsFalse(isCompleted);
			Assert.AreEqual(1, actionCallCount);
			Assert.AreEqual(SleepCallAbortingTheWait, environment.SleepCallCount);
		}


		// ============================================================================
		[TestMethod]
		public void Execute_AbortRaisedDuringAction_StartsNoRetry()
		{
			// Arrange
			var environment = new FakeRetryEnvironment();
			var runner = environment.CreateRunner();
			var actionCallCount = 0;

			// Act
			var isCompleted = runner.Execute(
				() =>
				{
					actionCallCount++;
					environment.IsAbortRequested = true;
					throw new Exception("failed after the abort request");
				},
				MaxRetries,
				RetryDelaySeconds,
				OperationDescription,
				continueOnError: true);

			// Assert
			Assert.IsFalse(isCompleted);
			Assert.AreEqual(1, actionCallCount);
			Assert.AreEqual(0, environment.RetryCallCount);
			Assert.AreEqual(0, environment.SleepCallCount);
		}


		// ============================================================================
		[TestMethod]
		public void Execute_SucceedsOnSecondAttempt_ReturnsTrueAfterTwoCalls()
		{
			// Arrange
			var environment = new FakeRetryEnvironment();
			var runner = environment.CreateRunner();
			var actionCallCount = 0;

			// Act
			var isCompleted = runner.Execute(
				() =>
				{
					actionCallCount++;

					if (actionCallCount == 1)
					{
						throw new Exception("first attempt fails");
					}
				},
				MaxRetries,
				RetryDelaySeconds,
				OperationDescription,
				continueOnError: true);

			// Assert
			Assert.IsTrue(isCompleted);
			Assert.AreEqual(2, actionCallCount);
			Assert.AreEqual(1, environment.RetryCallCount);
		}


		// ============================================================================
		[TestMethod]
		public void Execute_PlainExceptionWithoutContinueOnError_RethrowsOnFirstAttempt()
		{
			// Arrange
			var environment = new FakeRetryEnvironment();
			var runner = environment.CreateRunner();
			var actionCallCount = 0;

			// Act
			Assert.ThrowsException<Exception>(
				() => runner.Execute(
					() =>
					{
						actionCallCount++;
						throw new Exception("not retried");
					},
					MaxRetries,
					RetryDelaySeconds,
					OperationDescription,
					continueOnError: false));

			// Assert
			Assert.AreEqual(1, actionCallCount);
			Assert.AreEqual(0, environment.RetryCallCount);
		}


		// ============================================================================
		[TestMethod]
		[DataRow(false, false, false)]
		[DataRow(true, false, true)]
		[DataRow(false, true, true)]
		[DataRow(true, true, true)]
		public void IsRunAborted_CancelledOrLatchSet_ReportsAbort(
			bool isCancelled,
			bool isAbortRequested,
			bool expectedIsAborted)
		{
			// Act
			var isAborted = RetryRunner.IsRunAborted(
				isCancelled,
				isAbortRequested);

			// Assert
			Assert.AreEqual(expectedIsAborted, isAborted);
		}


		// ============================================================================
		// ============================================================================
		// ============================================================================
		/// <summary>
		/// Supplies the abort flag, the clock and the sleep to a
		/// <see cref="RetryRunner"/>; a sleep advances the fake clock instead
		/// of blocking.
		/// </summary>
		private sealed class FakeRetryEnvironment
		{
			private const int NeverAbort = 0;

			private DateTime _utcNow = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);


			// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
			public bool IsAbortRequested { get; set; }

			// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
			public int AbortOnSleepCall { get; set; } = NeverAbort;

			// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
			public int SleepCallCount { get; private set; }

			// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
			public int RetryCallCount { get; private set; }


			// ============================================================================
			public RetryRunner CreateRunner()
			{
				return new RetryRunner(
					() => IsAbortRequested,
					message => { },
					message => { },
					(currentRetry, maxRetries) => RetryCallCount++,
					Sleep,
					() => _utcNow);
			}


			// ============================================================================
			private void Sleep(
				int milliseconds)
			{
				SleepCallCount++;
				_utcNow = _utcNow.AddMilliseconds(milliseconds);

				if (SleepCallCount == AbortOnSleepCall)
				{
					IsAbortRequested = true;
				}
			}
		}

	}
}
