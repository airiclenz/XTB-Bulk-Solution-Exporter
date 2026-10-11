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
	/// Covers the clamping of <see cref="Settings.ConnectionTimeoutInMinutes"/>
	/// to its allowed range.
	/// </summary>
	[TestClass]
	public class SettingsTests
	{

		private const int MinTimeout = 2;
		private const int MaxTimeout = 600;


		// ============================================================================
		[TestMethod]
		[DataRow(1000, MaxTimeout)]
		[DataRow(1, MinTimeout)]
		[DataRow(120, 120)]
		public void ConnectionTimeoutInMinutes_Assigned_StoresClampedValue(
			int assignedTimeout,
			int expectedTimeout)
		{
			// Arrange
			var settings = new Settings();

			// Act
			settings.ConnectionTimeoutInMinutes = assignedTimeout;

			// Assert
			Assert.AreEqual(expectedTimeout, settings.ConnectionTimeoutInMinutes);
		}


		// ============================================================================
		[TestMethod]
		public void ConnectionTimeoutInMinutes_TooLowThenTooHigh_StoresMaximum()
		{
			// Arrange
			var settings = new Settings();
			settings.ConnectionTimeoutInMinutes = 1;

			// Act
			settings.ConnectionTimeoutInMinutes = 1000;

			// Assert
			Assert.AreEqual(MaxTimeout, settings.ConnectionTimeoutInMinutes);
		}

	}
}
