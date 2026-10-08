# Plan: Stale solution selection restored during/after export

**Goal:** Checking/unchecking solutions must be reflected in `_settings` immediately, so no list rebuild (mid-export `RefreshSolutionInListBox`, post-export `LoadAllSolutions`) can restore an older selection. The export run processes exactly the solutions checked when Export was clicked.
**Date:** 2026-10-08
**Status:** unexecuted
**sized for:** ~200k-context host
**base:** a8c0650

**Regression check (2026-10-08, a8c0650):**
- 1: guard folded (writer decision: manual test starts after one completed export run; plus LoadAllSolutions cleanup-after-rebuild ordering, skip-unchanged sync, and an Acceptance grep for the immediate flush in button_Export_Click); yields to ToDo.md:13 (save debounce kept for the disk write)
- 2: guard folded (writer decision: manual test starts from a session with a completed prior export; plus Acceptance grep widened to -A10)
- 3: guard folded (manual steps labelled a regression check; `CheckedItems` grep is the binding check)

**Root cause (regression from `5c3cda4`):**
- `ExecuteSaveSettings` is the only path that copies list check state (`IsChecked`, `SortingIndex`) into `_settings`, and since `5c3cda4` it runs only when the 500 ms `_saveDebounceTimer` fires. Clicking Export within that window leaves `_settings` stale.
- `SaveSettings()` is called from the export `BackgroundWorker` thread (`ExportSolution`, `ImportCheckedSolutions`). It calls `Stop()`/`Start()` on a `System.Windows.Forms.Timer` from that thread. The timer's hidden window is then created on a thread with no message pump, and `Stop()` from the UI thread only posts `WM_CLOSE` and leaves `_timerID` non-zero. The debounced save never fires again for the rest of the session.
- `RefreshSolutionInListBox` → `UpdateSolutionList` (called after the first solution of every export phase) and `LoadAllSolutions` (after the run) rebuild the list from `_settings.…Checked`. That loads the old selection again, and the worker loops, which read `listBoxSolutions.CheckedItems` live, process a different set.

**Sources:**
- `Bulk Solution Exporter/BulkSolutionExporter_PluginControl.cs`
- `Bulk Solution Exporter/Settings.cs`
- commit `5c3cda4` (introduced `_saveDebounceTimer`)

**Ratified design calls:**
- **Scope:** fix the sync/timer bug AND snapshot the checked solutions at export start. Leave `RefreshSolutionInListBox`'s full-list rebuild as it is (user, 2026-10-08).
- **Debounce:** keep it, but only for writing to disk (`SettingsManager.Instance.Save`). Copying list state into `_settings` in memory is synchronous (author, 2026-10-08).

**Standing requirements:**
- skills: coding-standards
- Match the file's existing style: tabs, `// ====` separators before methods, Allman braces.
- No automated test project exists. Acceptance = solution build + greps; Tests = manual steps in XrmToolBox.

**Out of scope:**
- Changing `RefreshSolutionInListBox` so it no longer rebuilds the whole list.
- Making `Settings` thread-safe (its dictionary cache) beyond what marshalling to the UI thread provides.
- Any other settings properties or UI behaviour.

## 1. Sync list state into settings synchronously; debounce only the disk write — ✅ DONE (2026-10-08)

NOTES (2026-10-08): ExecuteSaveSettings removed outright (no immediate-path wrapper kept); SaveSettings now calls SyncSolutionConfigsFromList() then PersistSettings(caller) or the debounce timer, and the Tick lambda calls only PersistSettings().
NOTES (2026-10-08): until item 2 lands, the worker-thread SaveSettings() calls (ExportSolution, ImportCheckedSolutions) now also run SyncSolutionConfigsFromList() off the UI thread (reads listBoxSolutions.Items only, no handle access); item 2's InvokeRequired guard removes this.
NOTES (2026-10-08): repo has no CHANGELOG file (ToDo.md carries release notes); entry text left above for the closeout, ToDo.md untouched.

**What:** fix for the regression from `5c3cda4`: list check state reaches `_settings` only when the debounce timer fires.
**Regression guard.** The manual Tests steps must first complete one export run (so the pre-fix debounce timer is dead) before checking A+B and clicking Export; state that the bug also reproduces in a fresh session by clicking Export within 0.5 s of a check change. Yields to `ToDo.md:13` ("added save debounce" as the 300-400+ solution performance fix): the debounce stays on the disk write.
In `LoadAllSolutions`' `PostWorkCallBack`, move `SaveSettings(cleanUpNonExistingSolutions: true)` after `UpdateSolutionList()`, so the now-synchronous sync does not walk the old list and re-create (via `GetSolutionConfiguration(id, true)`) the configs `RemoveNonExistantSolutions` just removed.
In `SyncSolutionConfigsFromList`, set fields and call `UpdateSolutionConfiguration` (which re-serializes the config to JSON) only when `config.Checked` or `config.SortingIndex` differs from the list item, so the per-keystroke `SaveSettings()` from the text boxes does not serialize every config.
**Goal:** Every UI-thread `SaveSettings(...)` call that is not suppressed by `CodeUpdate` updates every solution config's `Checked`/`SortingIndex` in `_settings` before it returns. Only the `SettingsManager.Instance.Save` call is debounced. `button_Export_Click` flushes settings immediately before `ExecuteOperations()`.
**Approach (assumed at the header base):** Split `ExecuteSaveSettings` into two private methods. `SyncSolutionConfigsFromList()` holds the `foreach (var listBoxItem in listBoxSolutions.Items)` loop. `PersistSettings(caller)` holds `SettingsManager.Instance.Save` and the `LogDebug`. `SaveSettings` calls `SyncSolutionConfigsFromList()` right after `RemoveNonExistantSolutions` handling. With `immediate`, it then calls `PersistSettings`. Otherwise it restarts the debounce timer. The timer `Tick` lambda calls `PersistSettings()`. Remove `ExecuteSaveSettings`, or keep it as sync + persist for the immediate path; there must be no duplicated loop. In `button_Export_Click`, add `SaveSettings(immediate: true);` after `CheckIfAllSolutionFilesAreDefined()` passes and before `SetUiEnabledState(false)`.
**Files:** Bulk Solution Exporter/BulkSolutionExporter_PluginControl.cs
**Read first:** Bulk Solution Exporter/BulkSolutionExporter_PluginControl.cs — SaveSettings, ExecuteSaveSettings, BulkSolutionExporter_PluginControl (ctor _saveDebounceTimer.Tick), button_Export_Click, LoadAllSolutions, RemoveNonExistantSolutions; Bulk Solution Exporter/Settings.cs — GetSolutionConfiguration, UpdateSolutionConfiguration
**Tests:** Manual, in XrmToolBox: load solutions and complete one export run first (this kills the pre-fix debounce timer). Then check A+B and click Export. The log shows only A+B, and A+B are still checked after the run. Then uncheck B, check C, close and reopen the tool. A+C are checked. The bug also reproduces in a fresh session by clicking Export within 0.5 s of a check change.
**Acceptance:**
- `powershell -NoProfile -Command "$m = & 'C:\Program Files (x86)\Microsoft Visual Studio\Installer\vswhere.exe' -latest -requires Microsoft.Component.MSBuild -find 'MSBuild\**\Bin\MSBuild.exe' | Select-Object -First 1; & $m 'Bulk Solution Exporter.sln' /p:Configuration=Debug /v:minimal; exit $LASTEXITCODE"` exits 0
- `grep -n "SyncSolutionConfigsFromList()" "Bulk Solution Exporter/BulkSolutionExporter_PluginControl.cs"` shows a call inside `SaveSettings`
- `grep -n "_saveDebounceTimer.Tick" "Bulk Solution Exporter/BulkSolutionExporter_PluginControl.cs"` shows the tick calling only the persist method
- `grep -n "button_Export_Click\|SaveSettings(immediate: true);" "Bulk Solution Exporter/BulkSolutionExporter_PluginControl.cs"` shows a `SaveSettings(immediate: true);` line inside `button_Export_Click`, before its `ExecuteOperations()` call
- `grep -n "SaveSettings(cleanUpNonExistingSolutions: true);\|UpdateSolutionList();" "Bulk Solution Exporter/BulkSolutionExporter_PluginControl.cs"` shows, inside `LoadAllSolutions`, the `SaveSettings(cleanUpNonExistingSolutions: true);` line after `UpdateSolutionList();`
**Commit:** fix(settings): sync solution check state immediately, debounce only disk save

## 2. Marshal SaveSettings to the UI thread

**What:** fix for the regression from `5c3cda4`: `SaveSettings()` called from the export worker thread touches the WinForms debounce timer and `listBoxSolutions` off the UI thread, and the debounce timer stops working for the session. Depends on item 1.
**Regression guard.** The manual Tests steps must start from a session in which a prior export (with a duration-saving SaveSettings call from the worker) has completed, then toggle checks and run a second export to confirm the selection holds.
The Acceptance grep uses `-A10`, not `-A4`: the `SaveSettings(` signature and its `{` already fill the 4-line window (lines 2622-2626 at base), so an `InvokeRequired` guard at 2627+ would never show.
**Goal:** `SaveSettings` never touches `_saveDebounceTimer` or `listBoxSolutions` from a non-UI thread. When `InvokeRequired`, it re-dispatches itself to the UI thread with the same arguments and returns.
**Approach (assumed at the header base):** At the top of `SaveSettings`, before the `CodeUpdate` check, add `if (InvokeRequired) { BeginInvoke((MethodInvoker) (() => SaveSettings(caller, cleanUpNonExistingSolutions, immediate))); return; }`. Use `BeginInvoke`, not `Invoke`, so the worker does not block on the UI thread. The existing worker-thread call sites are in `ExportSolution` (managed and unmanaged branches) and `ImportCheckedSolutions`. They call `_settings.UpdateSolutionConfiguration` themselves before `SaveSettings()`, so their duration values are already in `_settings` and leave them unchanged.
**Files:** Bulk Solution Exporter/BulkSolutionExporter_PluginControl.cs
**Read first:** Bulk Solution Exporter/BulkSolutionExporter_PluginControl.cs — SaveSettings, ExportSolution, ImportCheckedSolutions, ClosingPlugin, RefreshSolutionInListBox (existing InvokeRequired pattern), MyPlugin_ParentChanged (BeginInvoke MethodInvoker pattern)
**Tests:** Manual, in XrmToolBox with one target connection and import on: first complete an export+import run, so the worker has made its duration-saving `SaveSettings()` call. Afterwards toggle solutions' check boxes, wait 1 s, and confirm the "Settings have been saved (…)" debug log line appears (debug logging on), or close and reopen the tool and confirm the new check state persisted. Then run a second export. The selection stays as last set during and after the run.
**Acceptance:**
- the MSBuild command from item 1 exits 0
- `grep -n -A10 "private void SaveSettings(" "Bulk Solution Exporter/BulkSolutionExporter_PluginControl.cs"` and the following lines show an `InvokeRequired` guard before any `_saveDebounceTimer` use
**Commit:** fix(settings): marshal SaveSettings to the UI thread from export worker

## 3. Snapshot checked solutions at export start

**What:** Ratified scope call: the worker phases iterate a snapshot taken on the UI thread, not `listBoxSolutions.CheckedItems`, which changes whenever the list is rebuilt.
**Regression guard.** The manual Tests steps are a regression check only: once items 1-2 are in, the mid-run `UpdateSolutionList` (via `RefreshSolutionInListBox`) restores the same check state from the synced `_settings`, so they cannot fail against the pre-item-3 tree. The `CheckedItems` Acceptance grep is the binding check.
**Goal:** `UpdateCheckedVersionNumbers`, `ExportCheckedSolutions` and `ImportCheckedSolutions` process exactly the `Solution` list captured on the UI thread in `ExecuteOperations` before `WorkAsync`. None of them reads `listBoxSolutions.CheckedItems`.
**Approach (assumed at the header base):** In `ExecuteOperations`, before building `_executionWorker`, capture `var checkedSolutions = listBoxSolutions.CheckedItems.Select(item => item.ItemObject as Solution).ToList();`. Add a `List<Solution> solutions` parameter to the three methods and pass the snapshot from the `Work` lambda (each `ImportCheckedSolutions(targetConnection, …)` call gets the same snapshot). Inside each method, replace `listBoxSolutions.CheckedItems.Count`/`[i]`/`.ItemObject` with `solutions.Count`/`solutions[i]`, including the "is last item" `Log()` checks and the early-return `Count == 0` check in `UpdateCheckedVersionNumbers`. `RefreshSolutionInListBox(solution)` calls stay.
**Files:** Bulk Solution Exporter/BulkSolutionExporter_PluginControl.cs
**Read first:** Bulk Solution Exporter/BulkSolutionExporter_PluginControl.cs — ExecuteOperations, UpdateCheckedVersionNumbers, ExportCheckedSolutions, ImportCheckedSolutions, RefreshSolutionInListBox, UpdateSolutionList; XTB Components/Sortable Checklist/SortableCheckList.cs — CheckedItems, SetItemChecked
**Tests:** Manual regression check (does not bite after items 1-2; the Acceptance grep is binding): check A+B with Update Version and Export Managed on, run. Version update, export and import each log exactly A then B. During the run the list keeps A+B checked.
**Acceptance:**
- the MSBuild command from item 1 exits 0
- `grep -n "CheckedItems" "Bulk Solution Exporter/BulkSolutionExporter_PluginControl.cs"` shows no hit inside `UpdateCheckedVersionNumbers`, `ExportCheckedSolutions` or `ImportCheckedSolutions`
**Commit:** fix(export): process a checked-solution snapshot taken at export start
