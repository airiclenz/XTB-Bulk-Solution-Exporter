---
title: 'Run invariants: checked-solution snapshot, abort between steps, retries'
labels: [invariant, concurrency]
refs: [Bulk Solution Exporter/BulkSolutionExporter_PluginControl.cs#private bool IsAbortRequested(, Bulk Solution Exporter/BulkSolutionExporter_PluginControl.cs#private sealed class ExecutionSettingsSnapshot, Bulk Solution Exporter/BulkSolutionExporter_PluginControl.cs#private void button_Export_Click(, Bulk Solution Exporter/Helpers/RetryRunner.cs#public bool Execute(, docs/plans/archived/2026-10-08 - 00 - abort-execution-plan.md, docs/plans/archived/2026-10-08 - 00 - stale-selection-after-export-plan.md]
---

Rules for a run started by Execute (`ExecuteOperations`):

- **The run processes exactly the solutions checked when Execute was pressed.** Version update, export and import iterate a UI-thread snapshot and never read `listBoxSolutions.CheckedItems`. `button_Export_Click` flushes settings before `ExecuteOperations()`.
- **The worker reads settings from `ExecutionSettingsSnapshot`**, taken on the UI thread at run start, not from the live `Settings` object.
- **Abort lands between steps, never mid-step.** A running Dataverse call (export, import, Publish All, version update) finishes; nothing after it starts. Retry waits end at once. A holding install plus its apply-upgrade (`DeleteAndPromoteRequest`) is one step.
- On abort: skip the whole git phase (no commit, no push), keep exported files on disk, do not auto-disable export switches, end the log with orange `##### Aborted by user.` instead of green `##### Done.`
- A run is reported aborted when `args.Cancelled || abort was requested`, even if a step threw.
- Retries go through `Helpers/RetryRunner.cs`. Import faults: "Cannot start another [Import]" is retried by the normal loop; "_Upgrade already exists" logs an error and fails that solution (no apply-upgrade); exhausted retries return failure, never success.

Deliberately out of scope: interrupting an in-flight Dataverse request, aborting `LoadAllSolutions`, closing the tool mid-run, rolling back completed steps.
