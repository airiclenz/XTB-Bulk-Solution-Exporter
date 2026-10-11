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
	/// Covers <see cref="GitHelper.RunProcess"/>, which must drain both output
	/// streams while the process runs so large outputs cannot deadlock it.
	/// </summary>
	[TestClass]
	public class GitHelperTests
	{
		// Characters written to each stream: far above the OS pipe buffer.
		private const int StreamLength = 200000;

		// Upper bound for the run; a deadlocked read never finishes.
		private const int RunTimeoutInMilliseconds = 60000;

		private const string PowerShellExecutable = "powershell.exe";


		// ============================================================================
		[TestMethod]
		[Timeout(RunTimeoutInMilliseconds)]
		public void RunProcess_LargeOutputOnBothStreams_Completes()
		{
			// Arrange
			string arguments =
				"-NoProfile -Command \"" +
				"[Console]::Error.Write('e'*" + StreamLength + "); " +
				"[Console]::Out.Write('o'*" + StreamLength + ")\"";

			// Act
			GitHelper.RunProcess(
				PowerShellExecutable,
				arguments,
				null,
				null,
				out string standardOutput,
				out string standardError);

			// Assert
			Assert.AreEqual(StreamLength, standardOutput.Length, "stdout length");
			Assert.AreEqual(StreamLength, standardError.Length, "stderr length");
		}


	}
}
