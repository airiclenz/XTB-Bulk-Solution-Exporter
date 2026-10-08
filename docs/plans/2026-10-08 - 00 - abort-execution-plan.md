# Plan: Abort a running execution

**Goal:** While an execution runs, the toolbar's "Execute" button becomes an "Abort" button. Clicking it (after confirmation) stops the run after the current server step, skips every remaining step, and returns the UI to its normal state.
**Date:** 2026-10-08
**Status:** unexecuted
**sized for:** ~200k-context host
**base:** 83a7ab9

**Sources:**
- `Bulk Solution Exporter/BulkSolutionExporter_PluginControl.cs`
- `Bulk Solution Exporter/BulkSolutionExporter_PluginControl.designer.cs`
- XrmToolBox `PluginControlBase.CancelWorker()` / `WorkAsyncInfo.IsCancelable` (package `XrmToolBoxPackage` 1.2025.10.74)

**Ratified design calls (user, 2026-10-08):**
- **Granularity:** abort between steps. A running Dataverse call (export, import, Publish All, version update) finishes; nothing after it starts. Retry wait delays end at once.
- **Upgrade pair:** a holding install and its apply-upgrade (`DeleteAndPromoteRequest`) count as one step. Abort takes effect after the apply.
- **Git on abort:** skip the Git phase entirely (no commit, no push). Exported files stay on disk.
- **Confirmation:** Yes/No dialog "Abort after the current step finishes?" before the abort is requested.
- **Button look:** running = text `" Abort "`, image `Properties.Resources.delete_32px`, tooltip "Abort the execution"; after confirmed click = text `" Aborting... "`, disabled; after the run = `" Execute "` with its original image and tooltip.
- **After abort:** export switches are NOT auto-disabled; the solution list still reloads; the log ends with orange `##### Aborted by user.` instead of green `##### Done.`

**Standing requirements:**
- skills: coding-standards
- Match the file's existing style: tabs, `// ====` separators before methods, Allman braces.
- No automated test project exists. Acceptance = solution build + greps; Tests = manual steps in XrmToolBox.

**Out of scope:**
- Interrupting an in-flight Dataverse request (no async import/export jobs).
- Aborting `LoadAllSolutions` or any worker other than the execution worker.
- Closing the tool while a run is active.
- Rolling back steps that already completed.

**Regression check (2026-10-08, 83a7ab9):**
- 1: guard folded
- 2: guard folded (writer decision)
- 1: Approach "(no new field)" wording aligned with guard (c) (writer decision)
- 2: report guards folded — re-check `_isExecuting` after Yes; `CancelAsync()` on the captured execution worker alongside `CancelWorker()` (writer decision)

## 1. Make the execution worker stop between steps when cancellation is requested — ✅ DONE (2026-10-08)

NOTES (2026-10-08): `ExecuteWithRetries` now returns `bool` (false = abort requested before a retry could run); `ImportSolution` returns false on that without the success log, and `ImportCheckedSolutions` breaks on `!importResult && IsAbortRequested(worker)` before the Continue-On-Error error. The abort flag is a `volatile bool _isAbortRequested`, set and logged once inside `IsAbortRequested`. The `Work` lambda sets `args.Cancel = true` and returns without touching `args.Result` when an abort was requested.
NOTES (2026-10-08): the `Work` lambda body is wrapped in `try { ... } catch { IsAbortRequested(worker); throw; }`, so a step that throws after the abort request still sets the abort flag and `PostWorkCallBack` ends with `##### Aborted by user.` (guard (c)).
NOTES (2026-10-08): retry: the retry-wait slice length is the named constant `RetryWaitSliceInMilliseconds = 250` (next to the other constants) instead of a bare `250`; no other bare magic numbers in the item's new code.

**What:**
**Goal:** When `CancellationPending` is set on the execution worker, the run starts no further step: no further version update, export (managed or unmanaged), Git phase, target, import, or Publish All. A holding install still runs its apply-upgrade. A retry delay in `ExecuteWithRetries` ends within ~250 ms and no further retry starts. The worker marks the run cancelled. `PostWorkCallBack` then logs orange `##### Aborted by user.` instead of green `##### Done.`, skips the auto-disable of export switches, and still calls `LoadAllSolutions()` and `SetUiEnabledState(true)`.
**Approach (assumed at the header base):** In `ExecuteOperations`, `IsCancelable = true` is already set, so `worker.CancellationPending` is the abort signal, and one private abort flag (set where the abort is first observed, reset in `ExecuteOperations`) decides "aborted" in `PostWorkCallBack`. Add a private helper `IsAbortRequested(BackgroundWorker worker)` returning `worker?.CancellationPending == true`. Check it in the `Work` lambda before Publish All (source), before `UpdateCheckedVersionNumbers`, `ExportCheckedSolutions`, `HandleGit`, before each target and before each target's Publish All. Check it inside the per-solution loops of `UpdateCheckedVersionNumbers`, `ExportCheckedSolutions`, `ImportCheckedSolutions`, and in `ExportSolution` between the managed and unmanaged export. Do not check it between the two `ExecuteWithRetries` calls in `ImportSolution`. In `ExecuteWithRetries`, replace `Thread.Sleep(retryDelaySeconds * 1000)` with a wait loop that sleeps in ≤250 ms slices and returns early when the abort is requested; skip further retries then. At the end of `Work`, set `args.Cancel = true` when the abort was requested. In `PostWorkCallBack`, read `args.Cancelled` (never `args.Result` on a cancelled run — it throws). Add a `ColorOrange` constant next to `ColorGreen`, in the same `<color=#…>` form. Log one line `Abort requested — skipping remaining steps.` at the point the abort is first observed.
**Regression guard.** (a) `ExecuteWithRetries` makes its abort exit visible to the caller (return `bool` or throw `OperationCanceledException`): `ImportSolution` then returns `false` without the success log, and `ImportCheckedSolutions` skips the success/duration block and does not log the Continue-On-Error error.
(b) On abort, `ExportSolution` skips the unmanaged export, still runs `_logger.DecreaseIndent()` and returns `true`; the loops exit with `break`, not `return`, so the `DecreaseIndent()`/`Log()` after them still run.
(c) Decide "aborted" as `args.Cancelled || <abort was requested>`, using a flag set where the abort is first observed (the spot that logs `Abort requested — skipping remaining steps.`) and reset in `ExecuteOperations` — so an in-flight step that throws after the abort request still ends with `##### Aborted by user.`
**Files:** Bulk Solution Exporter/BulkSolutionExporter_PluginControl.cs
**Read first:** Bulk Solution Exporter/BulkSolutionExporter_PluginControl.cs — ExecuteOperations (Work lambda, PostWorkCallBack), ExecuteWithRetries, ImportSolution, ImportCheckedSolutions, ExportCheckedSolutions, ExportSolution, UpdateCheckedVersionNumbers;
XrmToolBox.Extensibility.dll — WorkAsyncInfo.PerformWork (calls Work directly, no wrapping)
**Tests:** Manual, in XrmToolBox: start a run with 3+ checked solutions and export on. Call the abort from item 2's button (or, before item 2 lands, temporarily via a debugger `CancelWorker()`). The current export finishes; the log shows the abort line and ends with `##### Aborted by user.`; no further solution is exported, no Git commit runs, and export switches stay on with auto-disable enabled. A full run without abort still ends with `##### Done.` With Continue-On-Error on and an import that fails, abort during the retry wait: no `The import was successful.` line, no Continue-On-Error error, no new last-import duration saved. With managed and unmanaged export on, abort during the managed export: no `**Export aborted due to an error!**` line, and `##### Aborted by user.` is not indented. Abort, then let the in-flight export fail (e.g. a read-only target folder): the log still ends with `##### Aborted by user.`
**Acceptance:**
- `powershell -NoProfile -Command "$m = & 'C:\Program Files (x86)\Microsoft Visual Studio\Installer\vswhere.exe' -latest -requires Microsoft.Component.MSBuild -find 'MSBuild\**\Bin\MSBuild.exe' | Select-Object -First 1; & $m 'Bulk Solution Exporter.sln' /p:Configuration=Debug /v:minimal; exit $LASTEXITCODE"` exits 0
- `grep -n "CancellationPending" "Bulk Solution Exporter/BulkSolutionExporter_PluginControl.cs"` shows the check in the helper
- `grep -n "IsAbortRequested(" "Bulk Solution Exporter/BulkSolutionExporter_PluginControl.cs"` shows calls in the `Work` lambda, `UpdateCheckedVersionNumbers`, `ExportCheckedSolutions`, `ExportSolution`, `ImportCheckedSolutions` and `ExecuteWithRetries`
- `grep -n "Thread.Sleep(retryDelaySeconds \* 1000)" "Bulk Solution Exporter/BulkSolutionExporter_PluginControl.cs"` finds nothing
- `grep -n "Aborted by user\|args.Cancelled\|args.Cancel = true" "Bulk Solution Exporter/BulkSolutionExporter_PluginControl.cs"` shows all three
**Commit:** feat(export): stop the execution between steps when cancellation is requested

## 2. Turn the Execute button into an Abort button while a run is active — ✅ DONE (2026-10-08)

NOTES (2026-10-08): the three looks live in a private nested `enum ExecuteButtonMode { Execute, Abort, Aborting }` passed to `SetExecuteButtonMode(...)`. The Execute look restores the captured designer image and tooltip, and its enabled state comes from calling `SetExportButtonState()`. The delete icon is loaded once into a `readonly Image _abortButtonImage`, so a run does not create a new bitmap from the resx.
NOTES (2026-10-08): `PostWorkCallBack` sets `_isExecuting = false`, clears the captured worker and restores the Execute look right after `StopProgressTimer()`. That is before the error MessageBox and `LoadAllSolutions()`, and so still before `SetUiEnabledState(true)`. While `_isExecuting` is true, `SetUiEnabledState` leaves `button_Export` alone instead of forcing it on, so that `SetExecuteButtonMode` owns it in both Abort and Aborting. The captured worker is the field `volatile BackgroundWorker _executionBackgroundWorker`, set on the first line of the `Work` lambda.
NOTES (2026-10-08): no CHANGELOG entry, because the repo has no CHANGELOG file (item 1 added none either).

**What:** Depends on item 1.
**Goal:** While the execution worker runs, `button_Export` is enabled and shows `" Abort "`, the `delete_32px` image and tooltip "Abort the execution". Clicking it shows a Yes/No dialog "Abort after the current step finishes?"; on Yes it calls `CancelWorker()` and the button shows `" Aborting... "` and is disabled. When the run ends (completed, failed or aborted), the button shows `" Execute "` with its original designer image and tooltip, and its enabled state comes from `SetExportButtonState()` again.
**Approach (assumed at the header base):** Add a private `bool _isExecuting`. Capture the designer's `button_Export.Image` and `ToolTipText` once in the constructor after `InitializeComponent()`, so restore never reloads them from the resx. In `button_Export_Click`, branch first: when `_isExecuting`, run the abort path (dialog → `CancelWorker()` → "Aborting..." state) and return; a click after the run already ended does nothing. Otherwise keep the current execute path. Set `_isExecuting = true` and switch to the Abort look right after `SetUiEnabledState(false)`. In `PostWorkCallBack`, set `_isExecuting = false` and restore the Execute look before `SetUiEnabledState(true)`. `SetUiEnabledState` must leave `button_Export` enabled while `_isExecuting` is true. `SetExportButtonState` must not touch `button_Export` while `_isExecuting` is true; it is called from many handlers, and `RefreshSolutionInListBox` → `UpdateSolutionList` can reach it mid-run. Keep the look switching in one private method `SetExecuteButtonMode(...)` — one place for text, image, tooltip and enabled state.
**Regression guard.** CancelWorker() removes and disposes the XrmToolBox working panel at once, and the 1-s ProgressTimer tick recreates it. After CancelWorker() on Abort-Yes, set _progressBaseMessage to "Aborting...{NewLine}Waiting for the current step to finish." and call SetWorkingMessage with it (same width/height as the other calls), so the recreated panel shows the abort state; item 2 Tests expect the panel to briefly disappear and come back with that message.
After the dialog returns Yes, check `_isExecuting` again; if it is false, do nothing (no cancel, no "Aborting..." state).
Capture the execution BackgroundWorker in the `Work` lambda into a field and call its `CancelAsync()` in the abort path, alongside `CancelWorker()`.
**Files:** Bulk Solution Exporter/BulkSolutionExporter_PluginControl.cs
**Read first:** Bulk Solution Exporter/BulkSolutionExporter_PluginControl.cs — button_Export_Click, SetUiEnabledState, SetExportButtonState, ExecuteOperations (PostWorkCallBack), RefreshSolutionInListBox;
Bulk Solution Exporter/BulkSolutionExporter_PluginControl.designer.cs — button_Export (Image from resx, ToolTipText "Export the Solutions"); Bulk Solution Exporter/Properties/Resources.Designer.cs — delete_32px;
XrmToolBox.Extensibility.dll — PluginControlBase.CancelWorker → Worker.CancelWorker
**Tests:** Manual, in XrmToolBox: start a run. The button shows Abort with the delete icon while every other control is locked. Click Abort → No: the run continues and the button still shows Abort. Click Abort → Yes: the button shows "Aborting..." greyed; the working panel briefly disappears and comes back within ~1 s showing "Aborting..." / "Waiting for the current step to finish."; after the current step the log ends with `##### Aborted by user.` and the button shows "Execute" with the export icon. Start and finish a run without abort: the button returns to "Execute" and is enabled per the usual rules. Check that the working panel does not cover the toolbar button. Click Abort and leave the dialog open until the run finishes, then click Yes: nothing is cancelled and the button shows "Execute", not "Aborting...". Change the main connection mid-run, then Abort → Yes: the run still aborts after the current step.
**Acceptance:**
- `powershell -NoProfile -Command "$m = & 'C:\Program Files (x86)\Microsoft Visual Studio\Installer\vswhere.exe' -latest -requires Microsoft.Component.MSBuild -find 'MSBuild\**\Bin\MSBuild.exe' | Select-Object -First 1; & $m 'Bulk Solution Exporter.sln' /p:Configuration=Debug /v:minimal; exit $LASTEXITCODE"` exits 0
- `grep -n "CancelWorker()\|CancelAsync()" "Bulk Solution Exporter/BulkSolutionExporter_PluginControl.cs"` shows both calls in the abort path of `button_Export_Click`
- `grep -n "\" Abort \"\|\" Aborting... \"\|\" Execute \"\|Abort after the current step finishes?" "Bulk Solution Exporter/BulkSolutionExporter_PluginControl.cs"` shows all four strings
- `grep -n "_isExecuting" "Bulk Solution Exporter/BulkSolutionExporter_PluginControl.cs"` shows uses in `button_Export_Click`, `SetUiEnabledState`, `SetExportButtonState` and `PostWorkCallBack` of `ExecuteOperations`
**Commit:** feat(ui): turn the Execute button into an Abort button while a run is active
