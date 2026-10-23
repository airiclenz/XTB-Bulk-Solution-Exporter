using System;
using System.Windows.Forms;
using Com.AiricLenz.BulkSolutionExporter.Core.Run;
using Com.AiricLenz.BulkSolutionExporter.Core.Schema;
using Version = Com.AiricLenz.BulkSolutionExporter.Core.Version;


// ============================================================================
// ============================================================================
// ============================================================================
namespace Com.AiricLenz.XTB.Plugin.Run
{

	// ============================================================================
	// ============================================================================
	// ============================================================================
	/// <summary>
	/// The XrmToolBox adapter of <see cref="IRunSink"/>: hands each call of a
	/// run on the execution worker to the plugin control's UI thread. Log
	/// lines, version changes, configuration changes and processed solutions
	/// run synchronously there (Invoke, not BeginInvoke: the log keeps its
	/// order, and a list refresh that reads the export folder's version file
	/// finishes before the engine's next step writes it). Progress goes to the
	/// worker's ReportProgress, which does not wait for the UI thread.
	/// </summary>
	internal sealed class ControlRunSink : IRunSink
	{

		private readonly Control _uiControl;
		private readonly Action<string> _renderLogLine;
		private readonly Action<Guid, Version> _applySolutionVersion;
		private readonly Action<SolutionConfiguration> _applyConfiguration;
		private readonly Action<Guid> _refreshSolution;
		private readonly Action<string, string, bool> _reportProgress;


		// ============================================================================
		/// <summary>
		/// Creates the sink of one run.
		/// </summary>
		/// <param name="uiControl">The control whose UI thread runs the handlers.</param>
		/// <param name="renderLogLine">Shows one finished log line; runs on the UI thread.</param>
		/// <param name="applySolutionVersion">Applies a solution's new version to the list; runs on the UI thread.</param>
		/// <param name="applyConfiguration">Merges the run's configuration change into the settings; runs on the UI thread.</param>
		/// <param name="refreshSolution">Refreshes a processed solution in the list; runs on the UI thread.</param>
		/// <param name="reportProgress">Reports (message, detail, is new step) through the worker's ReportProgress; runs on the run's thread.</param>
		/// <exception cref="ArgumentNullException">Any argument is null.</exception>
		public ControlRunSink(
			Control uiControl,
			Action<string> renderLogLine,
			Action<Guid, Version> applySolutionVersion,
			Action<SolutionConfiguration> applyConfiguration,
			Action<Guid> refreshSolution,
			Action<string, string, bool> reportProgress)
		{
			_uiControl = uiControl ?? throw new ArgumentNullException(nameof(uiControl));
			_renderLogLine = renderLogLine ?? throw new ArgumentNullException(nameof(renderLogLine));
			_applySolutionVersion = applySolutionVersion ?? throw new ArgumentNullException(nameof(applySolutionVersion));
			_applyConfiguration = applyConfiguration ?? throw new ArgumentNullException(nameof(applyConfiguration));
			_refreshSolution = refreshSolution ?? throw new ArgumentNullException(nameof(refreshSolution));
			_reportProgress = reportProgress ?? throw new ArgumentNullException(nameof(reportProgress));
		}


		// ============================================================================
		/// <summary>
		/// Shows the line in the log on the UI thread and waits for it.
		/// </summary>
		public void Log(
			string line)
		{
			InvokeOnUiThread(() => _renderLogLine(line));
		}


		// ============================================================================
		/// <summary>
		/// Applies the new version on the UI thread and waits for it.
		/// </summary>
		public void SolutionVersionChanged(
			Guid solutionId,
			Version version)
		{
			InvokeOnUiThread(() => _applySolutionVersion(solutionId, version));
		}


		// ============================================================================
		/// <summary>
		/// Merges the configuration change on the UI thread and waits for it.
		/// </summary>
		public void ConfigurationChanged(
			SolutionConfiguration configuration)
		{
			InvokeOnUiThread(() => _applyConfiguration(configuration));
		}


		// ============================================================================
		/// <summary>
		/// Reports the progress through the worker without waiting for the UI thread.
		/// </summary>
		public void Progress(
			string message,
			string detail,
			bool isNewStep)
		{
			_reportProgress(
				message,
				detail,
				isNewStep);
		}


		// ============================================================================
		/// <summary>
		/// Refreshes the solution on the UI thread and waits for it.
		/// </summary>
		public void SolutionProcessed(
			Guid solutionId)
		{
			InvokeOnUiThread(() => _refreshSolution(solutionId));
		}


		// ============================================================================
		private void InvokeOnUiThread(
			Action action)
		{
			if (_uiControl.InvokeRequired)
			{
				_uiControl.Invoke(action);
			}
			else
			{
				action();
			}
		}

	}
}
