---
title: 'Run invariants: checked-solution snapshot, abort between steps, retries'
labels: [invariant, concurrency]
refs: [Bulk Solution Exporter.Core/Run/RunEngine.cs#public const string AbortRequestedMessage, Bulk Solution Exporter/Run/RunRequestFactory.cs#public static RunRequest Create(, Bulk Solution Exporter/BulkSolutionExporter_PluginControl.cs#private void button_Export_Click(, Bulk Solution Exporter.Core/Run/RetryRunner.cs#public bool Execute(, docs/plans/archived/2026-10-08 - 00 - abort-execution-plan.md, docs/plans/archived/2026-10-08 - 00 - stale-selection-after-export-plan.md]
---

Rules for a run started by Execute (`ExecuteOperations`, which runs core `RunEngine` on the execution worker):

- **The run processes exactly the solutions checked when Execute was pressed.** Version update, export and import iterate the run's `RunRequest.Solutions`, built on the UI thread, and never read `listBoxSolutions.CheckedItems`. `button_Export_Click` flushes settings before `ExecuteOperations()`.
- **The run reads everything from its `RunRequest`**, built by `RunRequestFactory` on the UI thread at run start: the run switches, version format and commit message from the on-screen controls; the version-file, retry and continue-on-error options and a copy of each solution's configuration from `Settings`. The worker never reads a control or the live `Settings` object.
- **Abort lands between steps, never mid-step.** A running Dataverse call (export, import, Publish All, version update) finishes; nothing after it starts. Retry waits end at once. An import is one server-side async operation (`StageAndUpgradeAsync` or `ImportSolutionAsync`) followed to its end, so an abort takes effect only after it. The abort is the run's `CancellationTokenSource`, cancelled by the Abort button, which also cancels the worker so the running step's progress does not replace the "Aborting..." message.
- On abort: skip the whole git phase (no commit, no push), keep exported files on disk, do not auto-disable export switches, end the log with orange `##### Aborted by user.` instead of green `##### Done.`
- A run is reported aborted when its `RunResult.IsAborted`; a run that threw has no result and is reported aborted when the abort was requested.
- Retries go through core `Run/RetryRunner.cs`, which classifies Dataverse faults by message text in the exception or any inner exception, whatever its type (`FaultException`, `DataverseOperationException`). Import faults: "Cannot start another [Import]" is retried by the normal loop; an upgrade whose target still holds a pending `<name>_Upgrade` first applies it (`DeleteAndPromote` through the retry loop, with an orange warning in the log), then installs this run's file, and a failed apply fails that solution without installing; "_Upgrade already exists" on the install itself (the pending upgrade appeared after that check) logs an error and fails that solution; exhausted retries return failure, never success.

Deliberately out of scope: interrupting an in-flight Dataverse request, aborting `LoadAllSolutions`, closing the tool mid-run, rolling back completed steps.
