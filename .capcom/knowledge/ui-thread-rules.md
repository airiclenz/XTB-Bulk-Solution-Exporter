---
title: UI objects only on the UI thread; the worker uses the settings snapshot
labels: [invariant, concurrency]
refs: [Bulk Solution Exporter/BulkSolutionExporter_PluginControl.cs#private void RefreshSolutionInListBox(, Bulk Solution Exporter/BulkSolutionExporter_PluginControl.cs#_saveDebounceTimer.Interval, Bulk Solution Exporter/Settings.cs#private void EnsureCache(), Bulk Solution Exporter/Helpers/Logger.cs]
---

The run executes on an XrmToolBox `WorkAsyncInfo` / `BackgroundWorker`; the UI thread keeps working meanwhile.

- No UI object (controls, `listBoxSolutions`, timers) is touched from a non-UI thread. When `InvokeRequired`, re-dispatch with `BeginInvoke` and return.
- Documented exceptions that use `Invoke` (synchronous): `Log` (through `AppendLogLine`, the sink of its core `RunLog`, which builds each line with its indent on the logging thread) and `RefreshSolutionInListBox` (the worker's per-solution list refresh). The latter must stay synchronous because the refresh reads the version JSON that the worker's next `SaveVersionJson` writes; async would race into a sharing-violation `IOException`.
- `Settings` (its dictionary config cache) is deliberately not thread-safe. The worker must use the per-run `ExecutionSettingsSnapshot`, never `_settings` directly.
- `SaveSettings` writes check state into `_settings` before returning; only the disk save (`SettingsManager.Instance.Save`) is debounced (500 ms UI timer).
- Known gap: the worker still reads some flip switches directly, see issue `worker-reads-ui-controls`.
