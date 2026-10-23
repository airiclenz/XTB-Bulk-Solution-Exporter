---
title: Execution worker reads flip-switch state across threads
status: done
type: bug
priority: 3
labels: [audit-followup]
created: 2026-10-11T20:15:00Z
closed: 2026-10-23T16:27:34Z
reason: "Resolved by plan '2026-10-20 - 00 - pptb-port-plan' item 9 (archived in 5f4ae2e): run options are captured on the UI thread into a RunRequest (replacing ExecutionSettingsSnapshot) and the worker body ExecuteRunOnWorker reads no control."
---

The execution worker in `BulkSolutionExporter_PluginControl.cs` (`ExecuteOperations` DoWork) still reads UI controls directly, e.g. `flipSwitch_publishSource.IsOn` and `flipSwitch_publishTarget.IsOn`, instead of the per-run `ExecutionSettingsSnapshot` taken on the UI thread. Violates the "no UI object touched from a non-UI thread" invariant.

## Acceptance

- Every switch/textbox value the worker needs is captured in `ExecutionSettingsSnapshot` on the UI thread; the DoWork body reads no control.

## Log

- 2026-10-11 airic: migrated to capcom from the 2026-10-11 audit report (out of scope for the audit-fixes plan).
