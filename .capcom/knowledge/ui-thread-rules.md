---
title: UI objects only on the UI thread; the worker uses the run request
labels: [invariant, concurrency]
refs: [Bulk Solution Exporter/Run/ControlRunSink.cs, Bulk Solution Exporter/BulkSolutionExporter_PluginControl.cs#private void RefreshSolutionInListBox(, Bulk Solution Exporter/BulkSolutionExporter_PluginControl.cs#_saveDebounceTimer.Interval, Bulk Solution Exporter/Settings.cs#private void EnsureCache(), Bulk Solution Exporter/Helpers/Logger.cs]
---

The run executes on an XrmToolBox `WorkAsyncInfo` / `BackgroundWorker`; the UI thread keeps working meanwhile.

- No UI object (controls, `listBoxSolutions`, timers) is touched from a non-UI thread. When `InvokeRequired`, re-dispatch with `BeginInvoke` and return.
- Documented exceptions that use `Invoke` (synchronous): `ControlRunSink`, the run engine's sink, for log lines, version changes, configuration changes and processed solutions (the per-solution `RefreshSolutionInListBox`); and `AppendLogLine`, the sink of the plugin's own core `RunLog`, which builds each line with its indent on the logging thread. Only the sink's progress goes through the worker's `ReportProgress` without waiting. The list refresh must stay synchronous because it reads the version JSON that the engine's next export writes; async would race into a sharing-violation `IOException`.
- `Settings` (its dictionary config cache) is deliberately not thread-safe. The worker uses the per-run `RunRequest` built by `RunRequestFactory` on the UI thread, never `_settings` or a control; the run's duration changes are merged back by `ApplyRunConfiguration` on the UI thread.
- `SaveSettings` writes check state into `_settings` before returning; only the disk save (`SettingsManager.Instance.Save`) is debounced (500 ms UI timer).
