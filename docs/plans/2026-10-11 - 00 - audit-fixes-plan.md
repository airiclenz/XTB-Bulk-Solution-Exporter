# Plan: Fix the verified 2026-10-11 audit findings

**Goal:** Close every audit finding that survived verification against `60a478d`: the export path traversal, the swallowed import faults, the settings-cache and list-box races, the git deadlock and argument injection, the log-renderer crash, the one-line correctness bugs and the build/licence drift. Add an MSTest project so each fix and the abort handshake get automated tests.
**Date:** 2026-10-11
**Status:** unexecuted
**sized for:** ~200k-context host
**base:** 60a478d
**Sources:**
- `docs/skill-runs/handoff/2026-10-11/report.md` (audit findings), `docs/skill-runs/handoff/2026-10-11/bundle.md` (invariants)
- `docs/plans/archived/2026-10-08 - 00 - abort-execution-plan.md` (abort invariants), `docs/plans/archived/2026-10-08 - 00 - stale-selection-after-export-plan.md` (marshalling invariants)

**Ratified design calls (user, 2026-10-11):**
- **Import faults:** "Cannot start another [Import]" is retried by the normal retry loop; "_Upgrade already exists" logs an error and fails that solution (no apply-upgrade); exhausted retries return failure, never success.
- **Short version formats:** truncate — the result has exactly as many parts as the format; `#.#.+` on `1.2.3.4` gives `1.2.4`.
- **Tests:** add an SDK-style net48 MSTest project, built with VS MSBuild and run with `vstest.console.exe`.
- **Licence:** GPL-3.0-only everywhere; the unused csproj package-metadata block is cleared.
- **Settings race:** per-run snapshot taken on the UI thread (keeps the documented "Settings is not thread-safe beyond marshalling" decision).
- **Log escaping:** out of scope — only the crash/DoS half of the log findings is fixed.

**Regression check (2026-10-11, 60a478d):**
- 1: guard folded
- 3: guard folded
- 4: recast — supersedes the `ExecuteWithRetries` `<returns>` doc comment and the archived abort plan's NOTES contract; decision folded (Approach "throws" sentence deleted, tests never wait on the real clock)
- 5: guard folded
- 6: guard folded
- 7: guard folded
- 8: guard folded
- 9: guard folded
- 10: guard folded
- 11: guard folded
- 12: guard folded (What corrected)
- 13: guard folded

**Commands** (run from the repo root; items reference them by name):
- `BUILD`: `powershell -NoProfile -Command "$m = & 'C:\Program Files (x86)\Microsoft Visual Studio\Installer\vswhere.exe' -latest -requires Microsoft.Component.MSBuild -find 'MSBuild\**\Bin\MSBuild.exe' | Select-Object -First 1; & $m 'Bulk Solution Exporter.sln' /restore /p:RestorePackagesConfig=true /p:Configuration=Debug /v:minimal; exit $LASTEXITCODE"`
- `TEST <filter>`: `powershell -NoProfile -Command "$t = & 'C:\Program Files (x86)\Microsoft Visual Studio\Installer\vswhere.exe' -latest -find '**\vstest.console.exe' | Select-Object -First 1; & $t 'Bulk Solution Exporter.Tests\bin\Debug\net48\Com.AiricLenz.XTB.Plugin.BulkSolutionExporter.Tests.dll' '/TestCaseFilter:<filter>'; exit $LASTEXITCODE"`

**Standing requirements:**
- skills: coding-standards
- Match the edited file's style: tabs, CRLF, Allman braces, an 80-char `// ====` divider before every method; test files follow the same style.
- No bare magic numbers in new code — named constants beside the existing ones.
- UI-thread-only objects are touched only on the UI thread; worker → UI re-dispatch uses `BeginInvoke` unless the item says otherwise.
- `XTB Components/` is a submodule — never edit it.

**Out of scope:**
- `ColorHelper` / submodule dependency seam (needs an architecture review first).
- Nuspec dependency completeness (refuted: the XrmToolBox host supplies those assemblies).
- Escaping Dataverse text before markdown rendering (forged on-screen log lines; display-only).
- Cross-thread reads of `flipSwitch_*.IsOn` / textbox text by the worker.
- Committed historical `.nupkg` files; any version bump or release.

## 1. Add the MSTest test project — ✅ DONE (2026-10-11)

NOTES (2026-10-11): MSTest chosen per the ratified design call, overriding the coding-standards C# testing extension's xUnit/FluentAssertions default; package versions pinned to Microsoft.NET.Test.Sdk 17.12.0 and MSTest.TestAdapter/TestFramework 3.6.4.
NOTES (2026-10-11): SmokeTests has two tests — the plugin assembly name via the public BulkSolutionExporter_Plugin type, and an internal type (Helpers.Formatter) resolving to the plugin assembly, which proves InternalsVisibleTo at compile time.
NOTES (2026-10-11): Scripts/NuGet Pack.ps1 not executed (it creates a package and fails its version-exists check); the test project builds to its own bin folder and the pack uses the nuspec with the plugin project as BasePath, so the package content is unchanged.

**What:**
**Regression guard.** Add `/restore /p:RestorePackagesConfig=true` to both `.vscode/tasks.json` MSBuild calls (`build-solution`, `build-debug`): they build the `.sln` without restore, so F5 fails with NETSDK1004 on a checkout where the new PackageReference test project was never restored. Item 13 edits the same task strings and must keep these flags.
**Goal:** `Bulk Solution Exporter.sln` contains a net48 MSTest project whose tests can see the plugin's `internal` types, and `BUILD` plus `TEST FullyQualifiedName~SmokeTests` both exit 0.
**Approach (assumed at the header base):** create `Bulk Solution Exporter.Tests/Bulk Solution Exporter.Tests.csproj` — SDK-style, `<TargetFramework>net48</TargetFramework>`, `AssemblyName` `Com.AiricLenz.XTB.Plugin.BulkSolutionExporter.Tests`, PackageReferences `Microsoft.NET.Test.Sdk`, `MSTest.TestFramework`, `MSTest.TestAdapter`, framework references `System.ServiceModel` and `System.Windows.Forms`, a `ProjectReference` to `Bulk Solution Exporter/Bulk Solution Exporter.csproj`. Add it to the `.sln` with Debug/Release configuration entries. Add `[assembly: InternalsVisibleTo("Com.AiricLenz.XTB.Plugin.BulkSolutionExporter.Tests")]` to `Properties/AssemblyInfo.cs` (assembly is not strong-named). One `SmokeTests` class asserting the plugin assembly loads. `Scripts/NuGet Pack.ps1` restores and rebuilds the whole `.sln` in Release — the test project must build there too and must not change the package content.
**Files:** `Bulk Solution Exporter.Tests/Bulk Solution Exporter.Tests.csproj`, `Bulk Solution Exporter.Tests/SmokeTests.cs`, `Bulk Solution Exporter.sln`, `Bulk Solution Exporter/Properties/AssemblyInfo.cs`, `.vscode/tasks.json`
**Read first:** Bulk Solution Exporter.sln — ProjectConfigurationPlatforms; Bulk Solution Exporter/Bulk Solution Exporter.csproj — AssemblyName, PostBuildEvent; Bulk Solution Exporter/Properties/AssemblyInfo.cs — AssemblyVersion;
Bulk Solution Exporter/Scripts/NuGet Pack.ps1 — nuget restore, msbuild /t:Rebuild; .vscode/tasks.json — build-debug, build-solution
**Tests:** `SmokeTests`.
**Acceptance:**
- `BUILD` exits 0; `TEST FullyQualifiedName~SmokeTests` exits 0 with ≥ 1 passed test.
- The same `BUILD` command with `/p:Configuration=Release` exits 0.
**Commit:** `test: add MSTest project for the plugin`

## 2. Fix the version-format off-by-one for 2- and 3-part formats — ✅ DONE (2026-10-11)

NOTES (2026-10-11): a 4-part format on a shorter source version now treats the missing parts as 0 before applying the token, so `+` on a missing part yields 1 (previously 0), per the item's Goal; 4-part formats on 4-part versions are unchanged.
NOTES (2026-10-11): added a named `UnsetPart` constant for the -1 sentinel in the code this item touches; the existing array initialiser keeps its literal -1 values. The `Version(string)` constructor still throws on a source version with more than 4 parts (pre-existing, not in this item's scope).

**What:** Fixes the confirmed crash in `Schema/Version.cs` `IncreaseVersionNumber`: a format with fewer than 4 parts indexes past `formatParts` and throws `IndexOutOfRangeException`, aborting the batch. Depends on item 1.
**Goal:** for every format `ValidateFormat` accepts (2–4 parts), `IncreaseVersionNumber` does not throw and the resulting version string has exactly as many dot-separated parts as the format, with no empty parts; a source part missing for a present format part reads as 0.
**Approach (assumed at the header base):** in the loop, `if (i >= formatParts.Length) { _version[i] = -1; continue; }` (guard currently `>` with no `continue`); treat an unparsed source slot (`-1`) as 0 when the format has a part for it; `UpdateVersionString` stops at the first `-1` slot instead of writing empty parts.
**Files:** `Bulk Solution Exporter/Schema/Version.cs`, `Bulk Solution Exporter.Tests/VersionTests.cs`
**Read first:** Bulk Solution Exporter/Schema/Version.cs — IncreaseVersionNumber, UpdateVersionString, Version(string), ValidateFormat, VersionTokens;
Bulk Solution Exporter/BulkSolutionExporter_PluginControl.cs — UpdateVersionNumberInSource; Bulk Solution Exporter/Schema/Solution.cs — ConvertFrom, Version
**Tests:** `VersionTests`: `#.#.+` on `1.2.3.4` → `1.2.4`; `#.+` on `1.2.3.4` → `1.3`; `#.#.#.+` on `1.0` → `1.0.0.1`; a 4-part format is unchanged in behaviour (pick one existing case, e.g. `#.#.#.+` on `1.2.3.4` → `1.2.3.5`).
**Acceptance:** `BUILD` exits 0; `TEST FullyQualifiedName~VersionTests` exits 0.
**Commit:** `fix(version): stop 2- and 3-part version formats from crashing the run`

## 3. Extract the retry runner and test the abort handshake — ✅ DONE (2026-10-11)

NOTES (2026-10-11): `RetryRunner` also takes a `Func<DateTime> getUtcNow` clock beside the five dependencies the Approach lists, so the wall-clock retry wait keeps its exact behaviour while tests advance a fake clock from the fake sleep and never wait on real time.
NOTES (2026-10-11): `IsRunAborted` is an `internal static` member of `RetryRunner` (called as `RetryRunner.IsRunAborted(...)` in the `PostWorkCallBack` lambda), not of the plugin control: a static call on `BulkSolutionExporter_PluginControl` does not compile in the test project without an `XrmToolBox.Extensibility` reference (CS0012), which this item's Files do not include.
NOTES (2026-10-11): `RetryWaitSliceInMilliseconds` moved from the plugin control into `RetryRunner` (now `internal const`, same value 250) together with the wait loop that uses it; the fault-swallowing `FaultException` filter and its commented-out block moved verbatim for item 4.

**What:** Refactor, no behaviour change: the retry/abort logic of `ExecuteWithRetries` and the abort decision of `PostWorkCallBack` become testable helpers, then gain the abort-handshake tests the audit found missing. The fault-swallowing filter is moved as-is (item 4 fixes it). Depends on item 1.
**Regression guard.** Every RetryRunner test that expects a retry passes continueOnError: true (at base, non-fault exceptions are rethrown on attempt 1 when continueOnError is false); add one test pinning that rethrow with continueOnError false. Acceptance also greps that `ExecuteWithRetries` delegates to `RetryRunner` and that the `PostWorkCallBack` lambda calls `IsRunAborted(`.
**Goal:** an `internal` `RetryRunner` in `Helpers/RetryRunner.cs` holds the retry loop with injected abort check, logger and sleep; `ExecuteWithRetries` delegates to it with its signature unchanged; `PostWorkCallBack`'s aborted decision is an `internal static` function; tests cover abort during the retry wait, abort raised during `action()`, and the `args.Cancelled == false` but latch-set case.
**Approach (assumed at the header base):** `RetryRunner` takes `Func<bool> isAbortRequested`, `Action<string> log`, `Action<string> logError`, `Action<int, int> onRetry` (progress), `Action<int> sleep` (one slice); keeps `RetryWaitSliceInMilliseconds` semantics. `IsRunAborted(bool cancelled, bool abortRequested)` returns `cancelled || abortRequested`. `ExecuteWithRetries` and the `PostWorkCallBack` lambda in `ExecuteOperations` call these.
**Files:** `Bulk Solution Exporter/Helpers/RetryRunner.cs`, `Bulk Solution Exporter/BulkSolutionExporter_PluginControl.cs`, `Bulk Solution Exporter/Bulk Solution Exporter.csproj`, `Bulk Solution Exporter.Tests/RetryRunnerTests.cs`
**Read first:** Bulk Solution Exporter/BulkSolutionExporter_PluginControl.cs — ExecuteWithRetries, IsAbortRequested, ExecuteOperations (PostWorkCallBack lambda, isAborted), RetryWaitSliceInMilliseconds, ReportExtendedProgress;
Bulk Solution Exporter/Bulk Solution Exporter.csproj — Compile Include Helpers\*; Bulk Solution Exporter/Helpers/Logger.cs — Logger;
docs/plans/archived/2026-10-08 - 00 - abort-execution-plan.md — item Goal/Regression guard (abort invariants)
**Tests:** `RetryRunnerTests` (every case that expects a retry passes `continueOnError: true`): action always throws a retryable exception and abort turns true during the 2nd sleep slice → returns false, action ran once, no further sleep; action sets abort then throws → no retry; success on 2nd attempt → true, 2 calls; with `continueOnError: false` a plain `Exception` is rethrown on attempt 1 (action ran once); `IsRunAborted(false, true)` is true.
**Acceptance:** `BUILD` exits 0; `TEST FullyQualifiedName~RetryRunnerTests` exits 0; `grep -n "RetryRunner" "Bulk Solution Exporter/BulkSolutionExporter_PluginControl.cs"` has a hit inside `ExecuteWithRetries`; `grep -n "IsRunAborted(" "Bulk Solution Exporter/BulkSolutionExporter_PluginControl.cs"` has a hit inside the `PostWorkCallBack` lambda of `ExecuteOperations`.
**Commit:** `test(abort): extract the retry runner and cover the abort handshake`

## 4. Stop reporting failed imports as successful — ✅ DONE (2026-10-11)

NOTES (2026-10-11): ImportSolution and ImportCheckedSolutions needed no code change — at the current tree they already skip DeleteAndPromoteRequest, the success log and the LastImportDuration save on a false return; only the ExecuteWithRetries doc comment changed in the control.
NOTES (2026-10-11): the runner now logs one error line ("Gave up on ... after N retries.") when every attempt failed, so an exhausted "Cannot start another [Import]" with Continue-On-Error on is not silent; the fault texts became named constants on RetryRunner.
NOTES (2026-10-11): the archived abort plan's NOTES contract line was left as historical record (archived plan, not a live doc); this item's plan text records that it is superseded.
NOTES (2026-10-11): added a fourth test beyond the plan's list — "Cannot start another [Import]" on every attempt with continueOnError false returns false (not a throw) after maxRetries + 1 calls.

**What:** Recast at the regression check (2026-10-11). Fixes the regression introduced by `8532b46` (carried through `3f59447`): the `FaultException` filter for "Cannot start another [Import]" / "_Upgrade already exists" returns success silently, and exhausted retries with ContinueOnError also return success, so `ImportSolution` runs `DeleteAndPromoteRequest` on a never-imported solution and logs "The import was successful." Depends on item 3.
**Regression guard.** Drop the throw — ImportSolution already returns its completion bool and skips DeleteAndPromoteRequest on false; keep the existing false → LogError + break-to-next-target path in ImportCheckedSolutions unchanged, and make sure no "The import was successful." / LastImportDuration save happens on false. "Cannot start another [Import]" is retried regardless of continueOnError (logged, counted against maxRetries, false when exhausted); its test runs with continueOnError false and true and uses a message containing the full text the base filter matches. Update the runner's doc comment to the new contract (true only when the action completed; false on abort or failure). RetryRunner tests pass retryDelaySeconds 0 (or use item 3's injected sleep/clock) so they never wait on the real clock.
This supersedes the old contract recorded in the `ExecuteWithRetries` doc comment (`<returns>False when the abort was requested before a retry could run; true otherwise.</returns>`, `BulkSolutionExporter_PluginControl.cs` at base) and in the NOTES of `docs/plans/archived/2026-10-08 - 00 - abort-execution-plan.md` (line 41).
**Goal:** `RetryRunner` returns true only when the action completed. "Cannot start another [Import]" is retried with a log line; "_Upgrade already exists" logs an error and returns false at once; exhausted retries return false. On a false return `ImportSolution` never issues `DeleteAndPromoteRequest`, and `ImportCheckedSolutions` neither logs "The import was successful." nor saves a `LastImportDuration*` value for it.
**Approach (assumed at the header base):** replace the `when (...Contains(...)) { return true; }` catch in the runner. `ImportSolution` returns a completion bool to `ImportCheckedSolutions`. Abort keeps its own path: callers check `IsAbortRequested` before treating false as a failure. Update every caller of `ExecuteWithRetries` (grep `ExecuteWithRetries(`) to the new contract.
**Files:** `Bulk Solution Exporter/Helpers/RetryRunner.cs`, `Bulk Solution Exporter/BulkSolutionExporter_PluginControl.cs`, `Bulk Solution Exporter.Tests/RetryRunnerTests.cs`
**Read first:** Bulk Solution Exporter/BulkSolutionExporter_PluginControl.cs — ExecuteWithRetries (FaultException when-filter, catch Exception rethrow on !continueOnError), ImportSolution (isInstallCompleted, isApplyCompleted, DeleteAndPromoteRequest, catch FaultException/Exception), ImportCheckedSolutions (importResult block, IsAbortRequested break, ContinueOnError LogError break);
Bulk Solution Exporter/Helpers/RetryRunner.cs — RetryRunner (from item 3); Bulk Solution Exporter.Tests/RetryRunnerTests.cs — RetryRunnerTests;
docs/plans/archived/2026-10-08 - 00 - abort-execution-plan.md — NOTES (ExecuteWithRetries bool contract)
**Tests:** extend `RetryRunnerTests` (every case passes retryDelaySeconds 0 or item 3's injected sleep, never the real clock): `FaultException("Cannot start another [Import] because there is a previous [Import] running at this moment")` twice then success → true, 3 calls, run once with continueOnError false and once with true; `FaultException("…_Upgrade already exists")` → false, 1 call; always-throwing action with continueOnError true → false after maxRetries + 1 calls.
**Acceptance:** `BUILD` exits 0; `TEST FullyQualifiedName~RetryRunnerTests` exits 0; `grep -n "return true" "Bulk Solution Exporter/Helpers/RetryRunner.cs"` shows no hit inside a `catch` block.
**Commit:** `fix(import): report failed and blocked imports as failures`

## 5. Clamp the connection timeout and harden the version-JSON loader — ✅ DONE (2026-10-11)

NOTES (2026-10-11): the shared predicate is `ExportedSolution.Matches(logicalSolutionName, fileName)` (name OrdinalIgnoreCase, file name exact, empty name never matches); the loader and both SaveVersionJson loops use it.
NOTES (2026-10-11): the loader catches only IOException, UnauthorizedAccessException and JsonException (not a bare Exception) and also null-guards the Managed/Unmanaged lists and null entries; the Schema class has no logger, so the false return is the only signal.
NOTES (2026-10-11): SaveVersionJson still reads and deserializes the version file without a try or null check, so a malformed file still throws on the export worker (pre-existing; outside this item's goal).

**What:** Fixes two confirmed defects: the `Settings.ConnectionTimeoutInMinutes` setter clamps the backing field instead of `value` (typing 1000 stores 1000), and `ExportedSolutionVersions.TryLoadExportedSolution` reads and deserializes outside its `try`, so a truncated `exported_versions.json` throws into the UI. Depends on item 1.
**Regression guard.** `SaveVersionJson`'s Managed and Unmanaged matches use the same `OrdinalIgnoreCase` comparison as the loader (one shared predicate); otherwise a stale `mysolution` entry is returned by the case-insensitive loader while the exact-case writer keeps appending `MySolution`, and the outdated icon never clears. The mixed-case loader test must fail at base: an Unmanaged entry `MySolution` with UniqueName `MySolution`, or a Managed entry whose case differs from UniqueName.
**Goal:** every assignment to `ConnectionTimeoutInMinutes` stores `value` clamped to [2, 600]; `TryLoadExportedSolution` returns false (never throws) for a missing, unreadable, malformed or `null` JSON file, and matches the logical solution name case-insensitively in both the managed and unmanaged loops.
**Approach (assumed at the header base):** `Math.Max(MinConnectionTimeout, Math.Min(MaxConnectionTimeout, value))` with named constants; move `File.ReadAllText` + `JsonConvert.DeserializeObject` inside the existing `try`, null-check the result, compare with `string.Equals(..., StringComparison.OrdinalIgnoreCase)`.
**Files:** `Bulk Solution Exporter/Settings.cs`, `Bulk Solution Exporter/Schema/ExportedSolutionVersions.cs`, `Bulk Solution Exporter/BulkSolutionExporter_PluginControl.cs`, `Bulk Solution Exporter.Tests/SettingsTests.cs`, `Bulk Solution Exporter.Tests/ExportedSolutionVersionsTests.cs`
**Read first:** Bulk Solution Exporter/Schema/ExportedSolutionVersions.cs — TryLoadExportedSolution, ExportedSolution; Bulk Solution Exporter/BulkSolutionExporter_PluginControl.cs — SaveVersionJson, UpdateFileWarningStatus, UpdateSolutionStatusImages;
Bulk Solution Exporter/Settings.cs — ConnectionTimeoutInMinutes; Bulk Solution Exporter/SettingsForm.cs — textBox_connectionTimeout_TextChanged; Bulk Solution Exporter/Schema/Solution.cs — UniqueName
**Tests:** `SettingsTests`: 1000 → 600, 1 → 2, 120 → 120, sequence 1 then 1000 → 600. `ExportedSolutionVersionsTests` (temp dir): truncated JSON → false; `null` JSON → false; Unmanaged entry `MySolution` with UniqueName `MySolution` → true; Managed entry `mysolution` with UniqueName `MySolution` → true.
**Acceptance:** `BUILD` exits 0; `TEST FullyQualifiedName~SettingsTests|FullyQualifiedName~ExportedSolutionVersionsTests` exits 0.
**Commit:** `fix(settings): clamp the connection timeout and never throw from the version-JSON loader`

## 6. Fix the unmanaged-switch, null-config and unmanaged-warning bugs in the control — ✅ DONE (2026-10-11)

NOTES (2026-10-11): UpdateSolutionStatusImages now creates a missing config via `GetSolutionConfiguration(solution.SolutionIdentifier, true)` (in-memory only, same as the AddSolutionConfiguration path in UpdateSolutionSettingsScreen) rather than skipping the solution.

**What:** Fixes three confirmed UI defects in `BulkSolutionExporter_PluginControl.cs`: `flipSwitch_exportUnmanaged_Toggled` persists the managed switch's state; `UpdateSolutionSettingsScreen` (and `UpdateSolutionStatusImages`) dereference a null solution config; the unmanaged branches of `UpdateFileWarningStatus` and `UpdateSolutionStatusImages` load version JSON with `FileNameManaged`. Depends on item 5.
**Regression guard.** Depends on item 5 and never lands alone: at base the loader's unmanaged loop compares `LogicalSolutionName.ToLower()` with the as-stored UniqueName, so passing `FileNameUnmanaged` without item 5 hides the unmanaged outdated-version warning for any mixed-case unique name. Acceptance uses `grep -A3` (the file argument sits on the line after `TryLoadExportedSolution(`) and checks the null-config half.
**Goal:** the persisted `ExportUnmanaged` equals the unmanaged switch's state; neither method throws when the solution has no stored config (a config is created and used); both unmanaged warning branches pass `FileNameUnmanaged` to `TryLoadExportedSolution`.
**Approach (assumed at the header base):** `_settings.ExportUnmanaged = flipSwitch_exportUnmanaged.IsOn;`; `_currentSolutionConfig = _settings.AddSolutionConfiguration(...)`; in `UpdateSolutionStatusImages` use `GetSolutionConfiguration(id, true)` (or skip the solution on null); swap the two `FileNameManaged` arguments.
**Files:** `Bulk Solution Exporter/BulkSolutionExporter_PluginControl.cs`
**Read first:** Bulk Solution Exporter/BulkSolutionExporter_PluginControl.cs — flipSwitch_exportUnmanaged_Toggled, UpdateSolutionSettingsScreen, UpdateFileWarningStatus, UpdateSolutionStatusImages, SaveVersionJson writer (LogicalSolutionName);
Bulk Solution Exporter/Settings.cs — GetSolutionConfiguration(createIfNotFound), AddSolutionConfiguration; Bulk Solution Exporter/Schema/ExportedSolutionVersions.cs — TryLoadExportedSolution
**Tests:** none automatable (WinForms control); manual: toggle unmanaged off with managed on, reopen tool → unmanaged stays off.
**Acceptance:** `BUILD` exits 0; `grep -n "ExportUnmanaged = flipSwitch_exportManaged" "Bulk Solution Exporter/BulkSolutionExporter_PluginControl.cs"` has no hit; `grep -n -A3 "TryLoadExportedSolution" "Bulk Solution Exporter/BulkSolutionExporter_PluginControl.cs"` shows `FileNameUnmanaged` in both blocks binding `exportedSolutionUnmanaged`; `grep -n -B1 "_settings.AddSolutionConfiguration(\|GetSolutionConfiguration(solution.SolutionIdentifier, true)" "Bulk Solution Exporter/BulkSolutionExporter_PluginControl.cs"` shows `_currentSolutionConfig =` bound to the add in `UpdateSolutionSettingsScreen` and a hit inside `UpdateSolutionStatusImages` (or that method skips a null config).
**Commit:** `fix(ui): persist the unmanaged switch and use the unmanaged file for its warning`

## 7. Sanitize export file names derived from Dataverse data — ✅ DONE (2026-10-11)

NOTES (2026-10-11): ExportToFile validates the file path before the ExportSolutionRequest is sent (the `filePath` local moved up), so an invalid path skips the server export too instead of exporting and then discarding the bytes; the wizard logs an error and leaves that solution's file locations unchanged when either default path would resolve outside the selected folder.

NOTES (2026-10-11): `IsValidSolutionFilePath` uses `Path.IsPathRooted` (via `IsValidPath`), so a user-typed drive-relative path such as `C:foo.zip` still counts as rooted and is accepted, as before; default names can never produce one because `:` is sanitized.

**What:** Fixes the confirmed path traversal: `GetDefaultFileName` builds names from the server-controlled `uniquename` (strips only `#`), and the file wizard `Path.Combine`s it with the chosen folder, so `..\` or drive prefixes escape it before `File.WriteAllBytes`. User-typed absolute paths anywhere stay a supported feature. Depends on item 1.
**Regression guard.** `IsValidSolutionFilePath` wraps its checks in try/catch → false, as `IsValidPath` does: on net48 `Path.IsPathRooted`/`GetFileName` throw `ArgumentException` for `C:\exports\a|b.zip`, which would end the run instead of logging. The wizard compares against `Path.GetFullPath(folder).TrimEnd('\\', '/') + Path.DirectorySeparatorChar` with `OrdinalIgnoreCase`, so a drive-root folder (`D:\`) still accepts `D:\Managed.X.zip`. `ToSafeFileNamePart` also trims leading dots, so `GetDefaultFileName` never composes `Managed..x.zip`.
**Goal:** a default file name never contains a path separator, `..`, a drive prefix or any `Path.GetInvalidFileNameChars()` character; the file wizard never stores a path outside the selected folder; `ExportToFile` and `ImportSolution` refuse (log an error, skip that file, never write it or add it to `_sessionFiles`) any path that is not rooted or whose file-name part is empty or invalid.
**Approach (assumed at the header base):** add `ToSafeFileNamePart(string)` (invalid chars → `_`, `..` removed, trailing dots/spaces trimmed, empty → a named fallback) and `IsValidSolutionFilePath(string)` to `Helpers/FileAndFolderHelper.cs`; call the first from `GetDefaultFileName`, the second from `ExportToFile` and `ImportSolution`. In `button_fileWizzard_Click`, verify `Path.GetFullPath(combined)` starts with the full selected folder path plus separator. Producers of `FileName*`: wizard, browse dialogs, textboxes, settings load; consumers: `ExportToFile`, `SaveVersionJson`, `TryLoadExportedSolution`, `File.Exists` checks, `InitializeGitHelper`, `HandleGit`, `ImportSolution`.
**Files:** `Bulk Solution Exporter/Helpers/FileAndFolderHelper.cs`, `Bulk Solution Exporter/BulkSolutionExporter_PluginControl.cs`, `Bulk Solution Exporter.Tests/FileAndFolderHelperTests.cs`
**Read first:** Bulk Solution Exporter/Helpers/FileAndFolderHelper.cs — IsValidPath; Bulk Solution Exporter/BulkSolutionExporter_PluginControl.cs — GetDefaultFileName, button_fileWizzard_Click, ExportToFile, ImportSolution, button_browseManaged_Click, button_browseUnmananged_Click;
Bulk Solution Exporter/Schema/ExportedSolutionVersions.cs — TryLoadExportedSolution
**Tests:** `FileAndFolderHelperTests`: `..\..\Windows\win.ini`, `C:evil`, `a/b`, `x:y` sanitize to names without separators/`..`/`:`; `.x` sanitizes to a name with no leading dot; a legit `Contoso_Core` is unchanged; `IsValidSolutionFilePath` rejects `name.zip` and `sub\name.zip`, rejects `C:\exports\a|b.zip` without throwing, accepts `C:\exports\Managed.Contoso.zip`.
**Acceptance:** `BUILD` exits 0; `TEST FullyQualifiedName~FileAndFolderHelperTests` exits 0; `grep -n "IsValidSolutionFilePath\|GetFullPath" "Bulk Solution Exporter/BulkSolutionExporter_PluginControl.cs"` has hits inside `ExportToFile`, `ImportSolution` and `button_fileWizzard_Click`.
**Commit:** `fix(security): keep Dataverse solution names from escaping the export folder`

## 8. Give the export worker a per-run settings snapshot

**What:** Fixes the confirmed data race: the worker reads and writes `Settings`' unlocked `_configCache` / `_solutionConfigurations` while the UI thread mutates and serializes them (`SaveSettings` re-dispatch, `SyncSolutionConfigsFromList`, debounce `PersistSettings`, file wizard). Depends on items 4, 6 and 7 (same file).
**Regression guard.** Keep item 7's IsValidSolutionFilePath guards in ExportToFile and ImportSolution when re-plumbing configs through the snapshot. Prose guard: every comment claiming SaveSettings or _settings is reached from the export worker becomes stale — find them with `grep -n "worker" "Bulk Solution Exporter/BulkSolutionExporter_PluginControl.cs"` and correct each.
The Goal's "no code that runs on the execution worker" means the seven methods named in Approach plus `PublishAll`, `SaveVersionJson` and `ExecuteWithRetries` (none reads `_settings` at base); Acceptance checks the `button_fileWizzard` and `BeginInvoke` clauses.
**Goal:** no code that runs on the execution worker reads or writes `_settings`; the worker reads a snapshot built on the UI thread in `ExecuteOperations`; the four `LastExport*/LastImport*Duration*` write-backs reach `_settings` only through a `BeginInvoke` onto the UI thread; `button_fileWizzard` is disabled while a run is active.
**Approach (assumed at the header base):** a private snapshot type holding copies (via `GetJson`/`GetConfigFromJson`) of each checked solution's `SolutionConfiguration` keyed by id, plus `SaveVersionJson`, `RetryCount`, `RetryDelayInSeconds`, `ContinueOnError`; passed into `UpdateCheckedVersionNumbers`, `ExportCheckedSolutions`, `ExportSolution`, `ExportToFile`, `ImportCheckedSolutions`, `ImportSolution`, `HandleGit`. Durations are written to the snapshot copy and pushed by a UI-thread action that applies them to `_settings.GetSolutionConfiguration(id, true)`, calls `UpdateSolutionConfiguration` and `SaveSettings()`. Drop the dead `GetSolutionConfiguration` reads (`configToBeUpdated` and the unused one in `UpdateCheckedVersionNumbers`). Add `button_fileWizzard` to `SetUiEnabledState`.
**Files:** `Bulk Solution Exporter/BulkSolutionExporter_PluginControl.cs`
**Read first:** Bulk Solution Exporter/BulkSolutionExporter_PluginControl.cs — ExecuteOperations, ExportToFile, ImportCheckedSolutions, ImportSolution, SaveSettings, SetUiEnabledState;
Bulk Solution Exporter/Settings.cs — GetSolutionConfiguration; Bulk Solution Exporter/Schema/SolutionConfiguration.cs — GetJson, GetConfigFromJson
**Tests:** none automatable; manual: multi-solution export + import with 3+ solutions completes, durations persist after reopen.
**Acceptance:** `BUILD` exits 0; inside the bodies of the seven worker methods named above and of `PublishAll`, `SaveVersionJson` and `ExecuteWithRetries`, `grep`/read shows no `_settings` reference; `grep -n "button_fileWizzard.Enabled" "Bulk Solution Exporter/BulkSolutionExporter_PluginControl.cs"` has a hit inside `SetUiEnabledState`; the duration write-back to `_settings` sits inside a `BeginInvoke`.
**Commit:** `fix(concurrency): run the export worker on a UI-thread settings snapshot`

## 9. Marshal the list-box refresh and the log indent to the UI thread

**What:** Fixes two confirmed cross-thread defects: `RefreshSolutionInListBox` iterates `listBoxSolutions.Items`, assigns `ItemObject` and calls `Invalidate()` on the worker; `PluginControl.Log` marshals three separate `Invoke`s and computes the indent on the UI thread from a worker-mutated `_indentLevel`. Depends on item 8 (same file).
**Regression guard.** Keep `Invoke` (not `BeginInvoke`) for the `UpdateSolutionList` re-dispatch, overriding the standing `BeginInvoke` rule as the item already does for `Log`: an async refresh reads the export folder's version JSON (`UpdateSolutionStatusImages` → `TryLoadExportedSolution`) while the worker's next `SaveVersionJson` `File.WriteAllText`s the same file — a sharing-violation `IOException` on either thread. Acceptance checks the single-`Invoke` log clause.
**Goal:** `RefreshSolutionInListBox` touches `listBoxSolutions` only on the UI thread (re-dispatching itself via `Invoke`, not `BeginInvoke`, when `InvokeRequired`); each log line's indent is captured on the calling thread before marshalling and the append, caret move and scroll happen in one marshalled call.
**Approach (assumed at the header base):** drop the per-item `ItemObject`/`Invalidate` loop — `UpdateSolutionList` rebuilds from `_solutionsOrigin` — and `Invoke(UpdateSolutionList)` (synchronous, so it never reads the version JSON while the worker writes it); give `Logger.Log` an overload taking a pre-computed indent and have `PluginControl.Log` (and `LogError`/`LogDebug` if they route separately) capture it before a single `Invoke` (keep `Invoke`, not `BeginInvoke`, so log order with the worker is preserved).
**Files:** `Bulk Solution Exporter/BulkSolutionExporter_PluginControl.cs`, `Bulk Solution Exporter/Helpers/Logger.cs`
**Read first:** Bulk Solution Exporter/BulkSolutionExporter_PluginControl.cs — RefreshSolutionInListBox, UpdateSolutionList, Log, LogError, UpdateSolutionStatusImages, SaveVersionJson;
Bulk Solution Exporter/Helpers/Logger.cs — Log, GenerateIndent (private; capture needs an accessor)
**Tests:** none automatable; manual: run with 3+ solutions, log indentation is consistent.
**Acceptance:** `BUILD` exits 0; `grep -n "Items\[i\].ItemObject" "Bulk Solution Exporter/BulkSolutionExporter_PluginControl.cs"` has no hit; `grep -c "richTextBox_log.Invoke" "Bulk Solution Exporter/BulkSolutionExporter_PluginControl.cs"` prints 1 (all base hits are in `Log`); `Bulk Solution Exporter/Helpers/Logger.cs` has a `Log` overload taking a pre-computed indent.
**Commit:** `fix(concurrency): marshal list refresh and log indent to the UI thread`

## 10. Drain git output without deadlocking

**What:** Fixes the confirmed deadlock in `Helpers/GitHelper.cs` `ExecuteCommand`: `WaitForExit()` runs before `ReadToEnd()` on both redirected streams, so git output above the pipe buffer hangs the worker (or the UI for `checkout`/`branch`). Depends on item 1.
**Regression guard.** Use `ReadToEndAsync` on both streams, started before `WaitForExit()`; the Approach's `BeginOutputReadLine` option is dropped (line events strip newlines: `AppendLine` adds characters and plain `Append` merges `git branch` lines in `GetAllBranches`). Output stays byte-for-byte what `ReadToEnd` returned before. Acceptance checks the `GIT_TERMINAL_PROMPT` clause.
**Goal:** process execution in `GitHelper` reads stdout and stderr concurrently with the process and returns both in full for outputs of any size; git is started with `GIT_TERMINAL_PROMPT=0` so a credential prompt fails instead of hanging.
**Approach (assumed at the header base):** extract `internal static` `RunProcess(string fileName, string arguments, string workingDirectory)` returning exit code, stdout, stderr; start both reads (`BeginOutputReadLine`/`BeginErrorReadLine` into `StringBuilder`s, or `ReadToEndAsync` on both) before `WaitForExit()`; `ExecuteCommand` calls it with `git`.
**Files:** `Bulk Solution Exporter/Helpers/GitHelper.cs`, `Bulk Solution Exporter.Tests/GitHelperTests.cs`
**Read first:** Bulk Solution Exporter/Helpers/GitHelper.cs — ExecuteCommand, GetAllBranches, GetActiveBranch, SwitchToBranch;
Bulk Solution Exporter/BulkSolutionExporter_PluginControl.cs — HandleGit, UpdateGitBranches, comboBox_gitBranches_SelectedIndexChanged, InitializeGitHelper
**Tests:** `GitHelperTests.RunProcess_LargeOutputOnBothStreams_Completes` with `[Timeout(60000)]`: run `powershell.exe -NoProfile -Command "[Console]::Error.Write('e'*200000); [Console]::Out.Write('o'*200000)"`, assert both captured lengths are 200000.
**Acceptance:** `BUILD` exits 0; `TEST FullyQualifiedName~GitHelperTests` exits 0; `grep -n "GIT_TERMINAL_PROMPT" "Bulk Solution Exporter/Helpers/GitHelper.cs"` has a hit.
**Commit:** `fix(git): read git output concurrently to avoid pipe deadlocks`

## 11. Stop building git command lines from raw text

**What:** Fixes the confirmed argument injection: `HandleGit` concatenates `"commit -m \"" + message + "\""` (a `"` in the commit message injects options) and `"add \"" + file + "\""`; `SwitchToBranch` runs `checkout {branchName}` unquoted with names parsed from `git branch` (including `(HEAD detached …)` lines). Depends on item 10.
**Regression guard.** Write the commit message as UTF-8 without BOM (`new UTF8Encoding(false)`) to `process.StandardInput.BaseStream`, then close stdin: on net48 the `StandardInput` writer uses `Console.InputEncoding` (BOM under a UTF-8 console, cp1252 in XrmToolBox), while today's `-m` stores correct UTF-8. The temp-repo test sets `user.name`, `user.email` and `commit.gpgsign=false` via `git config` in the repo before `Commit`, and compares `%B` after `TrimEnd('\r', '\n')`.
**Goal:** `GitHelper` owns every git argument string: commit messages travel via `commit -F -` on stdin; file paths and branch names are quoted per Windows command-line rules and placed after `--` (`add -- <path>`) or followed by `--` (`checkout <branch> --`); branch names starting with `-` are rejected; branch-list parsing skips detached-HEAD lines and strips `*`/`+` markers. `BulkSolutionExporter_PluginControl.cs` contains no `"commit -m` or `"add \"` string.
**Approach (assumed at the header base):** add `RunProcess` stdin support; `internal static QuoteArgument(string)` (CommandLineToArgvW rules: double backslashes before a quote, escape `"`, wrap); public `AddFile(string absolutePath)`, `Commit(string message)`, `Push()` on `GitHelper`; `HandleGit` calls them with absolute paths (drop the `Replace(GitRootDirectory, "")` shortening); extract `internal static ParseBranchList(string output)`.
**Files:** `Bulk Solution Exporter/Helpers/GitHelper.cs`, `Bulk Solution Exporter/BulkSolutionExporter_PluginControl.cs`, `Bulk Solution Exporter.Tests/GitHelperTests.cs`
**Read first:** Bulk Solution Exporter/BulkSolutionExporter_PluginControl.cs — HandleGit, UpdateGitBranches, comboBox_gitBranches_SelectedIndexChanged;
Bulk Solution Exporter/Helpers/GitHelper.cs — ExecuteCommand, GetAllBranches, GetActiveBranch, SwitchToBranch
**Tests:** `QuoteArgument` cases (`a b`, `a"b`, `a\`, `a\"b`); `ParseBranchList` on `* main\n  dev\n+ wt\n  (HEAD detached at abc)` → `main, dev, wt`; temp-repo integration (`git init`, local `git config` of `user.name`/`user.email`/`commit.gpgsign=false`, `AddFile`, `Commit("say \"hi\" --amend")` and `Commit("Lösung ä")`) → `git log -1 --format=%B`, trimmed with `TrimEnd('\r', '\n')`, equals the message.
**Acceptance:** `BUILD` exits 0; `TEST FullyQualifiedName~GitHelperTests` exits 0; `grep -n "commit -m" "Bulk Solution Exporter/BulkSolutionExporter_PluginControl.cs"` has no hit.
**Commit:** `fix(git): pass commit messages, paths and branches to git safely`

## 12. Bound the markdown log renderer and the error-XML formatter

**What:** Fixes the confirmed crash: `MarkdownParser.ProcessFormattedText` recurses once per `"# "` heading prefix with no depth limit, so a server error of `# # # …` overflows the stack and kills XrmToolBox (`StackOverflowException` is uncatchable); `Formatter.FormatErrorStringWithXml` regex-scans and XML-parses messages of any size; it runs in `ImportSolution`'s catch blocks on the export worker, not the UI thread — its length cap bounds work size only; the UI-thread concern is the `MarkdownParser` rendering. Depends on item 1.
**Regression guard.** Collapsing heading prefixes does not bound depth (`# **x**` repeated recurses once per repeat), so a `MaxNestingDepth` cap (≥ 8; heading → italic → color is legitimate) is threaded through every recursive `ProcessFormattedText` call (heading, color, marker). Heading tests are sized from the constants (just under `MaxLogLineLength`) and run on a small-stack thread (e.g. `new Thread(..., 256 * 1024)`).
When truncating, re-append `</color>` if the kept prefix has an unclosed `<color=` token (before the marker), so a long `LogError` line stays red; keep `MaxFormattableErrorLength` ≥ `MaxLogLineLength`.
**Goal:** rendering any single log line uses bounded recursion depth and handles lines of at least 1,000,000 characters without crashing; lines longer than a named maximum are truncated with a visible marker; `FormatErrorStringWithXml` returns its input unchanged when it exceeds a named maximum length.
**Approach (assumed at the header base):** collapse consecutive heading prefixes iteratively (or cap nesting with a `MaxNestingDepth` constant and render the remainder literally); `MaxLogLineLength` cap in `ParseAndAppend` with a `… [truncated]` marker; `MaxFormattableErrorLength` guard at the top of `FormatErrorStringWithXml`.
**Files:** `Bulk Solution Exporter/Helpers/MarkdownParser.cs`, `Bulk Solution Exporter/Helpers/Formatter.cs`, `Bulk Solution Exporter.Tests/MarkdownParserTests.cs`
**Read first:** Bulk Solution Exporter/Helpers/MarkdownParser.cs — ParseAndAppend, ProcessFormattedText, FindNextMarker, AppendWithStyles; Bulk Solution Exporter/Helpers/Logger.cs — Log;
Bulk Solution Exporter/BulkSolutionExporter_PluginControl.cs — LogError, ImportSolution; Bulk Solution Exporter/Helpers/Formatter.cs — FormatErrorStringWithXml
**Tests:** on an STA thread with a `RichTextBox` and a 256 KB stack: `"# "` and `"# **x**"` each repeated to just under `MaxLogLineLength` render without crash; an over-limit `<color=#770000>`-wrapped line renders without the raw `<color=` text; a 2,000,000-char line ends with the truncation marker; a normal `##### Done.` line still renders as a heading; `FormatErrorStringWithXml` on an over-limit string returns it unchanged.
**Acceptance:** `BUILD` exits 0; `TEST FullyQualifiedName~MarkdownParserTests` exits 0.
**Commit:** `fix(log): bound markdown recursion and cap oversized log input`

## 13. Make the build task and deploy script fail honestly

**What:** Fixes two confirmed pipeline defects: `.vscode/tasks.json` `build-solution` exits 0 when MSBuild fails or the deploy `Copy-Item` fails (DLL locked by a running XrmToolBox); `Scripts/Deploy Locally.bat` copies the Debug DLL via a working-directory-relative path, so after `NuGet Pack.bat` it deploys a stale Debug build. Depends on item 1.
**Regression guard.** Depends on item 1. If item 1 changed .vscode/tasks.json MSBuild command strings (e.g. /restore), item 13 keeps those flags while adding its exit-code/copy handling.
Quote both `copy` operands — the repo path has spaces, so an unquoted `%~dp0` fails: `copy "%~dp0..\bin\Release\Com.AiricLenz.XTB.Plugin.BulkSolutionExporter.dll" "%appdata%\MscrmTools\XrmToolBox\Plugins\Com.AiricLenz.XTB.Plugin.BulkSolutionExporter.dll" /y`.
**Goal:** `build-solution` exits non-zero when MSBuild fails or the copy fails, and carries the `$msCompile` problem matcher; `Deploy Locally.bat` copies the Release DLL `bin\Release\Com.AiricLenz.XTB.Plugin.BulkSolutionExporter.dll` resolved relative to the script's own folder (`%~dp0`) and exits non-zero when the copy fails.
**Approach (assumed at the header base):** in the task's PowerShell: `if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }; Copy-Item ... -Force -ErrorAction Stop`, wrapped so a copy error exits 1. In the bat: replace the active Debug line with the Release path via `%~dp0..\bin\Release\`, remove the REM'd variant, `if errorlevel 1 exit /b 1`.
**Files:** `.vscode/tasks.json`, `Bulk Solution Exporter/Scripts/Deploy Locally.bat`
**Read first:** .vscode/tasks.json — build-solution, build-debug; .vscode/launch.json — Debug in XrmToolBox preLaunchTask; Bulk Solution Exporter/Scripts/Deploy Locally.bat;
Bulk Solution Exporter/BulkSolutionExporter.nuspec — file src bin\Release; Bulk Solution Exporter/Bulk Solution Exporter.csproj — OutputPath, PostBuildEvent
**Tests:** none automatable; manual: run `build-solution` with XrmToolBox open → task fails.
**Acceptance:** `powershell -NoProfile -Command "Get-Content .vscode/tasks.json -Raw | ConvertFrom-Json | Out-Null"` exits 0; `grep -n "bin\\\\Debug" "Bulk Solution Exporter/Scripts/Deploy Locally.bat"` has no hit; `grep -n '"%~dp0' "Bulk Solution Exporter/Scripts/Deploy Locally.bat"` has a hit; `grep -c "/restore" .vscode/tasks.json` prints 2.
**Commit:** `fix(build): propagate build and deploy failures`

## 14. Declare GPL-3.0-only consistently

**What:** The csproj declares `<PackageLicenseExpression>MIT</PackageLicenseExpression>` while `LICENSE` and `README.md` are GPL v3; its package-metadata `PropertyGroup` (`PackageId`, `Version`, `Authors`, `Description`, `PackageTags`, `PackageLicenseExpression`, `RepositoryUrl`) is unused — packing is nuspec-only.
**Goal:** no file in the repo declares the plugin MIT-licensed; `BulkSolutionExporter.nuspec` declares `<license type="expression">GPL-3.0-only</license>`; the csproj carries no `Package*`, `Version`, `Authors`, `Description` or `RepositoryUrl` package-metadata properties (`ForceDesignerDPIUnaware` stays).
**Approach (assumed at the header base):** delete the package-metadata lines from that `PropertyGroup`, keep `ForceDesignerDPIUnaware`; add the `<license>` element to the nuspec `<metadata>`; leave the nuspec `<version>` untouched.
**Files:** `Bulk Solution Exporter/Bulk Solution Exporter.csproj`, `Bulk Solution Exporter/BulkSolutionExporter.nuspec`
**Read first:** Bulk Solution Exporter/Bulk Solution Exporter.csproj — package-metadata PropertyGroup, ForceDesignerDPIUnaware; Bulk Solution Exporter/BulkSolutionExporter.nuspec — metadata, dependencies, files;
Bulk Solution Exporter/Scripts/NuGet Pack.ps1 — nuspec version/dependency checks, package-content check
**Tests:** none.
**Acceptance:** `BUILD` exits 0; `grep -rn "MIT" "Bulk Solution Exporter/Bulk Solution Exporter.csproj" "Bulk Solution Exporter/BulkSolutionExporter.nuspec"` has no hit; `grep -n "GPL-3.0-only" "Bulk Solution Exporter/BulkSolutionExporter.nuspec"` has a hit.
**Commit:** `chore(license): declare GPL-3.0-only in the package metadata`
