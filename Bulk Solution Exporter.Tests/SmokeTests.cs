using System;
using System.Collections.Generic;
using System.Threading;
using Com.AiricLenz.BulkSolutionExporter.Core.Run;
using Com.AiricLenz.BulkSolutionExporter.Core.Schema;
using Com.AiricLenz.XTB.Plugin.Helpers;
using Com.AiricLenz.XTB.Plugin.Run;
using Com.AiricLenz.XTB.Plugin.Schema;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Version = Com.AiricLenz.BulkSolutionExporter.Core.Version;


// ============================================================================
// ============================================================================
// ============================================================================
namespace Com.AiricLenz.XTB.Plugin.Tests
{

	// ============================================================================
	// ============================================================================
	// ============================================================================
	/// <summary>
	/// Proves the test project builds against the plugin and can see its
	/// internal types, and that a request built by the plugin runs through
	/// the shared run engine.
	/// </summary>
	[TestClass]
	public class SmokeTests
	{

		private const string PluginAssemblyName =
			"Com.AiricLenz.XTB.Plugin.BulkSolutionExporter";

		private const string UnchangedMajorMinorBuildIncreasedRevisionFormat = "#.#.#.+";
		private const string SolutionVersion = "1.0.0.0";
		private const string ExpectedUpdatedVersion = "1.0.0.1";
		private const string SolutionUniqueName = "smoke_solution";


		// ============================================================================
		[TestMethod]
		public void PluginAssembly_Load_HasPluginAssemblyName()
		{
			// Arrange
			var pluginType = typeof(BulkSolutionExporter_Plugin);

			// Act
			var assemblyName = pluginType.Assembly.GetName().Name;

			// Assert
			Assert.AreEqual(PluginAssemblyName, assemblyName);
		}


		// ============================================================================
		[TestMethod]
		public void InternalType_Reference_ResolvesToPluginAssembly()
		{
			// Arrange
			var internalType = typeof(ColorHelper);

			// Act
			var assemblyName = internalType.Assembly.GetName().Name;

			// Assert
			Assert.AreEqual(PluginAssemblyName, assemblyName);
		}


		// ============================================================================
		[TestMethod]
		public void RunRequestFactory_VersionUpdateSwitchedOn_RunEngineWritesNewVersion()
		{
			// Arrange
			var settings = new Settings
			{
				SaveVersionJson = false,
				RetryCount = 0,
				RetryDelayInSeconds = 0,
				ContinueOnError = false,
			};

			var switches = new RunRequestFactory.Switches
			{
				ShouldPublishSource = false,
				ShouldUpdateVersion = true,
				ShouldExportManaged = false,
				ShouldExportUnmanaged = false,
				ShouldCommitToGit = false,
				ShouldPushCommit = false,
				ShouldImportManaged = false,
				ShouldImportUnmanaged = false,
				ShouldUpgrade = false,
				ShouldOverwriteCustomizations = false,
				ShouldEnableAutomation = false,
				ShouldPublishTargets = false,
				VersionFormat = UnchangedMajorMinorBuildIncreasedRevisionFormat,
				CommitMessage = string.Empty,
			};

			var solution =
				new Solution(
					Guid.NewGuid(),
					new SolutionInfo
					{
						SolutionId = Guid.NewGuid(),
						UniqueName = SolutionUniqueName,
						FriendlyName = SolutionUniqueName,
						Version = new Version(SolutionVersion),
					});

			var request =
				RunRequestFactory.Create(
					switches,
					settings,
					new[] { solution });

			var gateway = new VersionRecordingGateway();

			// Act
			RunEngine.Run(
				request,
				gateway,
				new List<IDataverseGateway>(),
				new SilentRunSink(),
				CancellationToken.None);

			// Assert
			Assert.AreEqual(ExpectedUpdatedVersion, gateway.LastWrittenVersion);
		}



		// ============================================================================
		// ============================================================================
		// ============================================================================
		/// <summary>
		/// A gateway that records version updates; every other call is not
		/// expected by the smoke test and throws.
		/// </summary>
		private sealed class VersionRecordingGateway : IDataverseGateway
		{

			// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
			public string ConnectionName
			{
				get;
			} = "Smoke Source";


			// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
			/// <summary>
			/// The version of the last version update; null when none ran.
			/// </summary>
			public string LastWrittenVersion
			{
				get; private set;
			}


			// ============================================================================
			public void UpdateSolutionVersion(
				Guid solutionId,
				string version)
			{
				LastWrittenVersion = version;
			}


			// ============================================================================
			public void PublishAll()
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



		// ============================================================================
		// ============================================================================
		// ============================================================================
		/// <summary>
		/// A sink that ignores everything the run reports.
		/// </summary>
		private sealed class SilentRunSink : IRunSink
		{

			// ============================================================================
			public void Log(
				string line)
			{
			}


			// ============================================================================
			public void SolutionVersionChanged(
				Guid solutionId,
				Version version)
			{
			}


			// ============================================================================
			public void ConfigurationChanged(
				SolutionConfiguration configuration)
			{
			}


			// ============================================================================
			public void Progress(
				string message,
				string detail,
				bool isNewStep)
			{
			}


			// ============================================================================
			public void SolutionProcessed(
				Guid solutionId)
			{
			}

		}

	}
}
