# Power Platform ToolBox port — shared core, .NET worker, React UI

**Goal:** Bulk Solution Exporter runs in Power Platform ToolBox (PPTB) with full XrmToolBox feature parity, while the XTB plugin keeps working unchanged for its users. Both hosts drive one shared C# run engine in a new core library.
**Date:** 2026-10-20
**Status:** unexecuted
**sized for:** ~200k-context host
**base:** 4a1296e

**Sources:**
- https://docs.powerplatformtoolbox.com — tool-development/dotnet-workers, manifest, pptb-config, api-reference
- `@pptb/types@1.2.7` (`toolboxAPI.d.ts`, `dataverseAPI.d.ts`)
- https://github.com/PowerPlatformToolBox/sample-tools/tree/main/new/html-sample (worker .csproj, package.json, pptb.config.json)
- capcom knowledge: `run-and-abort-rules`, `untrusted-dataverse-data`, `code-style`, `versioning-and-release`, `build-and-test`

**Ratified design calls (user, 2026-10-20):**
- **Repo layout:** same repo; UI-free logic moves to `Bulk Solution Exporter.Core` (SDK-style, `net48;net8.0`), used by the XTB plugin and the PPTB worker.
- **PPTB architecture:** TypeScript UI plus a .NET 8 worker (StreamJsonRpc, `jsonrpc-stdio-v1`, framework-dependent dotnet tool on nuget.org, declared in `pptb.config.json`).
- **Run engine:** one C# `RunEngine` in core; Dataverse is reached only through `IDataverseGateway`. XTB adapter = `IOrganizationService`; PPTB adapter = worker→TS reverse calls that use `dataverseAPI`.
- **UI stack:** React 18 + Fluent UI v9, Vite IIFE single bundle.
- **Scope:** full parity in the first PPTB release (multiple targets, holding upgrade, CSV, versions JSON, git branch/commit/push, retries, abort, file wizard, status icons, settings).
- **PPTB settings:** `toolboxAPI.settings`, per-solution config keyed by lower-cased environment URL + `.` + unique name; no XTB settings import.
- **Plugin build:** the plugin .csproj becomes SDK-style net48 so `dotnet build` works on macOS.
- **Names:** npm `@airiclenz/pptb-bulk-solution-exporter`, NuGet `AiricLenz.BulkSolutionExporter.PptbWorker`, display name "Bulk Solution Exporter".
- **XTB run toggles:** read from the on-screen switches/text boxes on the UI thread, as today (user, 2026-10-20).

**Standing requirements:**
- skills: coding-standards
- C# follows capcom `code-style` (tabs, Allman, `// ===` method dividers, named constants). Every moved core type AND every member the plugin, worker or tests call is `public`; no `InternalsVisibleTo` from core.
- Never edit `XTB Components/` (submodule).
- `run-and-abort-rules` and `untrusted-dataverse-data` hold for both hosts.
- Deviations from item text land as a dated NOTES line under the item.
- XTB runtime behaviour (live export/import in XrmToolBox) is checked manually on Windows by the user; items prove it with builds and tests.

**Out of scope:** importing XTB settings into PPTB; the VS Code PPTB host; open capcom ideas (`connection-profiles`, `powershell-run-hooks`, `data-transfer`, `dynamic-file-names`, `commit-message-placeholders`); version bumps or publishing a release.

**Regression check (2026-10-20, 4a1296e):**
- 1: guard folded (System.Resources.Extensions shipping per writer decision)
- 2: guard folded (writer decision; plan-wide public-members rule moved to Standing requirements)
- 3: guard folded (core members public per Standing requirements instead of InternalsVisibleTo)
- 4: guard folded (public members per Standing requirements)
- 5: guard folded
- 6: guard folded; yields to `Helpers/Formatter.cs:16-20` (per-line cap)
- 7: recast (writer decision); yields to `.capcom/issues/release-2026-03-05/install-upgrade-retries.md:12`
- 8: guard folded (GitClient per writer decision)
- 9: recast (writer decision); yields to `BulkSolutionExporter_PluginControl.cs:2189` and `:3105` ("Invoke, not BeginInvoke")
- 10: guard folded (notifications per writer decision)
- 11: guard folded (writer decision)
- 12: guard folded; supersedes `.capcom/knowledge/versioning-and-release.md:10` ("GPL-3.0-only everywhere") for the npm package
- 13: guard folded
- 14: guard folded
- 15: guard folded
- 16: guard folded
- 7 (re-check): guard folded (gateway exceptions propagate; per-solution `SolutionProcessed` sink event; invalid format/path logged and skipped)
- 9 (re-check): recast (writer decision: run toggles from the controls); yields to `.capcom/issues/worker-reads-ui-controls.md:14`
- 10 (re-check): guard extended (`run/solutionProcessed` notification per writer decision on 7)

## 1. Convert the plugin and test projects to SDK-style net48 — ✅ DONE (2026-10-23)

NOTES (2026-10-23): build only — verified on Linux arm64 with .NET SDK 8.0.426 (`dotnet build` Debug and Release exit 0, no compiler warnings); `dotnet test` was not run (no Windows/mono here) and must be run on Windows.

NOTES (2026-10-23): System.Resources.Extensions ships (nuspec `lib\net48\Plugins`, Deploy Locally.bat, build-solution task): could not confirm XrmToolBox provides it. Pinned to 8.0.0, not 10.0.12, because 10.x pulls System.Formats.Nrbf, System.Reflection.Metadata, System.Collections.Immutable and Microsoft.Bcl.HashCode into the plugin folder; 8.0.0 needs only System.Memory, which XrmToolBox already loads. NuGet Pack.ps1 now also requires that DLL in the package.

NOTES (2026-10-23): manifest resource names and assembly references of the new DLL match the shipped 2026.10.8.29 DLL exactly (compared with System.Reflection.Metadata).

NOTES (2026-10-23): added `CopyLocalLockFileAssemblies true` (SDK net48 libraries do not copy package DLLs by default) so bin\Debug still holds the runnable XrmToolBox debug host; the former `Private=False` references (Microsoft.Web.Xdt, System.Security.Cryptography.Algorithms/Cng/Encoding/Primitives/X509Certificates) use `ExcludeAssets="runtime"` to stay out of the output as before.

NOTES (2026-10-23): Release keeps `DEBUG` defined (`$(DefineConstants);DEBUG`), as the old Release config did, so `LogDebug` still works in shipped builds behind the debug-mode setting.

NOTES (2026-10-23): .vscode/launch.json needed no change (still runs bin\Debug\XrmToolBox.exe after build-debug); tasks.json build tasks now call `dotnet build` and a new `test` task runs `dotnet test`.

NOTES (2026-10-23): consequential edit — AGENTS.md: made necessary by the build-and-test and versioning-and-release title changes (capcom refreshed its knowledge block; the refresh also added the `capcom next` line from the installed capcom version).

NOTES (2026-10-23): the PackageReference restore now reports NU1902/NU1903 for System.Security.Cryptography.Pkcs 9.0.5 (same version packages.config pinned; pre-existing). The open issue `remove-unused-ai-packages` still names `packages.config` in its body.

**What:**
**Goal:** `dotnet build "Bulk Solution Exporter.sln"` succeeds on macOS and Windows; plugin assembly name, root namespace and embedded resources are unchanged.
**Approach (assumed at the header base):** Rewrite `Bulk Solution Exporter/Bulk Solution Exporter.csproj` as `<Project Sdk="Microsoft.NET.Sdk">` with `TargetFramework net48`, `UseWindowsForms true`, `GenerateResourceUsePreserializedResources true` + `System.Resources.Extensions`, `Microsoft.NETFramework.ReferenceAssemblies` (PrivateAssets all). Turn every `packages.config` entry into a `PackageReference` with the same version (the XrmToolBox package version stays the one `versioning-and-release` names) and delete `packages.config`. Keep `AssemblyInfo.cs` (`GenerateAssemblyInfo false`). Update `Scripts/NuGet Pack.ps1` so its XrmToolBox dependency check reads the `PackageReference` and it builds with `dotnet build -c Release`. Update `.vscode/tasks.json` and the capcom note `build-and-test` (`capcom know` edit) to the `dotnet build` / `dotnet test` commands.
**Regression guard.** Replace the `PostBuildEvent` (empty `$(ConfigurationName)` in SDK style → MSB3073) with `<Target AfterTargets="Build" Condition="'$(Configuration)'=='Debug'">` that `<Copy>`s `$(TargetPath)` + pdb to `$(OutDir)Plugins`; set `AppendTargetFrameworkToOutputPath false` so outputs stay `bin\Debug\` / `bin\Release\` (nuspec, `Deploy Locally.bat`, `launch.json`, tasks.json depend on it). Add `Properties/launchSettings.json` with an `Executable` profile running `bin\Debug\XrmToolBox.exe` with `/overridepath:.` (SDK ignores `StartProgram`). Ship `System.Resources.Extensions.dll` via the nuspec `<files>` (`lib\net48\Plugins`) and `Deploy Locally.bat` unless the executor confirms XrmToolBox already provides that assembly version (record which in NOTES). Update the capcom note `versioning-and-release` (title, line 7) and the nuspec:45 comment from `packages.config` to the `PackageReference`. On macOS the test Acceptance is build-only (mono cannot run the Windows-only GitHelper/FileAndFolderHelper/RichTextBox tests); the full `dotnet test` runs on Windows.
**Files:** `Bulk Solution Exporter/Bulk Solution Exporter.csproj`, `Bulk Solution Exporter/packages.config`, `Bulk Solution Exporter/Scripts/NuGet Pack.ps1`, `Bulk Solution Exporter.Tests/Bulk Solution Exporter.Tests.csproj`, `.vscode/tasks.json`, `.capcom/knowledge/build-and-test.md`, `.vscode/launch.json`, `Bulk Solution Exporter/Properties/launchSettings.json`, `Bulk Solution Exporter/BulkSolutionExporter.nuspec`, `Bulk Solution Exporter/Scripts/Deploy Locally.bat`, `.capcom/knowledge/versioning-and-release.md`
**Read first:** `Bulk Solution Exporter/Bulk Solution Exporter.csproj — PostBuildEvent, OutputPath, StartProgram, Import XTB Components.projitems; Bulk Solution Exporter/BulkSolutionExporter.nuspec — files, dependencies; Bulk Solution Exporter/Scripts/NuGet Pack.ps1 — $packagesFile, build block, content check`
`.vscode/tasks.json — build-solution, build-debug; .vscode/launch.json — Debug in XrmToolBox; .capcom/knowledge/build-and-test.md; .capcom/knowledge/versioning-and-release.md`
**Tests:** existing suite, unchanged.
**Acceptance:**
- `dotnet build "Bulk Solution Exporter.sln" -c Debug` exits 0.
- `dotnet test "Bulk Solution Exporter.Tests"` exits 0 on Windows. On macOS this item is build-only; record "build only" in NOTES.
- `ls "Bulk Solution Exporter/bin/Debug/Com.AiricLenz.XTB.Plugin.BulkSolutionExporter.dll" "Bulk Solution Exporter/bin/Debug/Plugins/Com.AiricLenz.XTB.Plugin.BulkSolutionExporter.dll"` succeeds; after `dotnet build -c Release`, `ls "Bulk Solution Exporter/bin/Release/"` lists every file each nuspec `<file src>` names.
**Commit:** `build: convert plugin and tests to SDK-style net48 projects`

## 2. Create the core library and its net8 test project — ✅ DONE (2026-10-23)

NOTES (2026-10-23): the moved formatter test lives in a new `Bulk Solution Exporter.Core.Tests/FormatterTests.cs` (one test class per source class), not in VersionTests.cs; the plan's Files line named no file for it.

NOTES (2026-10-23): `StringExtention` is in namespace `Com.AiricLenz.BulkSolutionExporter.Core.Extensions` (mirrors its folder); `Formatter`, `Version` and `VersionTokens` are in `Com.AiricLenz.BulkSolutionExporter.Core`. `IncreaseVersionNumber` takes `DateTime? today = null` (a `DateTime` default cannot be `DateTime.Now`) and reads the date once per call.

NOTES (2026-10-23): `Bulk Solution Exporter/Settings.cs` left unchanged: it never names `Version`, so `using System;` cannot bind `System.Version` there. Solution.cs and BulkSolutionExporter_PluginControl.cs got the `using Version = Com.AiricLenz.BulkSolutionExporter.Core.Version;` alias; the control's fully qualified `Schema.Version.ValidateFormat` call now reads `Version.ValidateFormat`.

NOTES (2026-10-23): consequential edit — Bulk Solution Exporter/Helpers/Logger.cs: made necessary by moving `StringExtention` to the core Extensions namespace (using updated).

NOTES (2026-10-23): consequential edit — Bulk Solution Exporter/Schema/ExportedSolutionVersions.cs: made necessary by moving `StringExtention` to the core Extensions namespace (using updated).

NOTES (2026-10-23): consequential edit — .capcom/knowledge/build-and-test.md: made necessary by adding the core and core-test projects and the core DLL to the Debug Plugins copy and the build-solution task (text only; title unchanged).

NOTES (2026-10-23): consequential edit — .capcom/knowledge/versioning-and-release.md: made necessary by Deploy Locally.bat now also copying the core DLL.

NOTES (2026-10-23): verified on Linux with .NET SDK 8.0.426: solution builds Debug (0 errors; only the pre-existing NU1902/NU1903 Pkcs warnings), plugin builds Release with `Com.AiricLenz.BulkSolutionExporter.Core.dll` in bin\Release, core tests pass 10/10; the net48 plugin tests (SmokeTests, MarkdownParserTests) only build here and must be run on Windows.

**What:**
**Goal:** `Bulk Solution Exporter.Core` (`net48;net8.0`, namespace `Com.AiricLenz.BulkSolutionExporter.Core`) and `Bulk Solution Exporter.Core.Tests` (MSTest, `net8.0`, `RollForward Major`) are in the solution, the plugin references the core, and `StringExtention`, `Formatter` and `Version`/`VersionTokens` live in core with their tests.
**Approach (assumed at the header base):** New SDK projects. Move `Extentions/StringExtention.cs`, `Helpers/Formatter.cs`, `Schema/Version.cs` with `git mv`. Make them `public`, drop the stray `System.Windows.Forms` using, and give `Version.IncreaseVersionNumber` an optional `DateTime` for today (default `DateTime.Now`). Core references `Newtonsoft.Json` at the plugin's version. Move `VersionTests.cs` and the formatter tests into core tests. Fix plugin `using`s.
**Regression guard.** Core `AssemblyName` is `Com.AiricLenz.BulkSolutionExporter.Core`; its net48 DLL is added to the nuspec `<files>` (`lib\net48\Plugins`), `Deploy Locally.bat`, the tasks.json copy and the Debug `Plugins` copy. `SmokeTests.InternalType_Reference_ResolvesToPluginAssembly` points at a type that stays in the plugin (e.g. `ColorHelper`). `MarkdownParserTests.cs` is the source of the moved formatter test (`FormatErrorStringWithXml_OverLimitMessage_ReturnsItUnchanged`); `Formatter.MaxFormattableErrorLength` becomes `public`. `Schema/Solution.cs`, `Settings.cs` and `BulkSolutionExporter_PluginControl.cs` get an explicit using/alias for core `Version` so `using System;` cannot bind `System.Version`.
**Files:** `Bulk Solution Exporter.Core/Bulk Solution Exporter.Core.csproj`, `Bulk Solution Exporter.Core/Extensions/StringExtention.cs`, `Bulk Solution Exporter.Core/Formatter.cs`, `Bulk Solution Exporter.Core/Version.cs`, `Bulk Solution Exporter.Core.Tests/Bulk Solution Exporter.Core.Tests.csproj`, `Bulk Solution Exporter.Core.Tests/VersionTests.cs`, `Bulk Solution Exporter.sln`, `Bulk Solution Exporter/Bulk Solution Exporter.csproj`, `Bulk Solution Exporter/BulkSolutionExporter.nuspec`, `Bulk Solution Exporter/Scripts/Deploy Locally.bat`, `.vscode/tasks.json`, `Bulk Solution Exporter.Tests/SmokeTests.cs`, `Bulk Solution Exporter.Tests/MarkdownParserTests.cs`, `Bulk Solution Exporter/Schema/Solution.cs`, `Bulk Solution Exporter/Settings.cs`, `Bulk Solution Exporter/BulkSolutionExporter_PluginControl.cs`
**Read first:** `Bulk Solution Exporter/Schema/Version.cs — Version.IncreaseVersionNumber, ValidateFormat, VersionTokens; Bulk Solution Exporter/Helpers/Formatter.cs — MaxFormattableErrorLength; Bulk Solution Exporter/Schema/Solution.cs — Version property`
`Bulk Solution Exporter.Tests/SmokeTests.cs — InternalType_Reference_ResolvesToPluginAssembly; Bulk Solution Exporter.Tests/MarkdownParserTests.cs — FormatErrorStringWithXml_OverLimitMessage_ReturnsItUnchanged; Bulk Solution Exporter.Tests/VersionTests.cs; Bulk Solution Exporter/BulkSolutionExporter.nuspec — files`
**Tests:** moved version and formatter tests (formatter test from `MarkdownParserTests.cs`), plus one test that pins `YYYY.MM.DD.+` with a fixed date; `SmokeTests` assembly-name test retargeted to a plugin type.
**Acceptance:** `dotnet build "Bulk Solution Exporter.sln"` exits 0; `dotnet test "Bulk Solution Exporter.Core.Tests"` exits 0.
**Commit:** `refactor(core): add shared core library with version and formatting helpers`
Depends on item 1.

## 3. Move file and git helpers to core, cross-platform — ✅ DONE (2026-10-23)

NOTES (2026-10-23): `FileAndFolderHelper` and `GitHelper` are in namespace `Com.AiricLenz.BulkSolutionExporter.Core.IO` (mirrors the folder, as item 2 did for `Extensions`); the plugin control gained `using Com.AiricLenz.BulkSolutionExporter.Core.IO;`. The moved tests use the flat `Com.AiricLenz.BulkSolutionExporter.Core.Tests` namespace like the other core tests.

NOTES (2026-10-23): `FindRepositoryRootPath` now stops when the current folder is empty (a start path with no directory part), where it used to resolve `..\` against the process working directory and walk up from there; a repo root found above the start folder is now returned without the trailing backslash `Path.GetFullPath(..\)` left on it (only used as git's working directory and in a debug log line). `IsValidPath` stays `internal` (no caller outside the class).

NOTES (2026-10-23): on .NET Framework, `RunProcess` quotes EVERY argument with `QuoteArgument` (including plain ones such as `branch`, `-F`, `--`), so git receives the same argv as before; `RunProcess` throws `ArgumentNullException` for a null list or a null entry.

NOTES (2026-10-23): the large-output test off Windows runs `sh -c "head -c 200000 /dev/zero | tr '\000' e >&2; ..."`; `FindRepositoryRootPath_NoRepository_ReturnsEmpty` reports Inconclusive when the temp folder itself lies inside a git repository (e.g. a dotfiles repo in the user profile).

NOTES (2026-10-23): consequential edit — .capcom/knowledge/build-and-test.md: made necessary by moving the GitHelper and FileAndFolderHelper tests to the net8 core tests (they now run on macOS/Linux).

NOTES (2026-10-23): `.capcom/issues/git-stdin-ioexception.md` still names `Helpers/GitHelper.cs` in its body; it now lives at `Bulk Solution Exporter.Core/IO/GitHelper.cs` (issue left untouched, as implementers never edit tickets).

NOTES (2026-10-23): verified on Linux with .NET SDK 8.0.426: solution builds (0 errors; only the pre-existing NU1902/NU1903 Pkcs warnings), core tests pass 35/35 including the temp-repo commit tests; `capcom know verify` and `capcom check` are clean.

**What:**
**Goal:** `FileAndFolderHelper` and `GitHelper` live in core, behave identically on net48, and on net8 run correctly on macOS/Linux: the repo-root walk terminates and git arguments are passed without hand-quoting.
**Approach (assumed at the header base):** `git mv` both into `Bulk Solution Exporter.Core/IO/`. Replace the `Path.Combine(x, @"..\")` walk in `FindRepositoryRootPath` with `Directory.GetParent`. In `GitHelper.RunProcess`, use `ProcessStartInfo.ArgumentList` under `#if NET` and keep `QuoteArgument` for net48. Commit message stays on stdin; dash refusal stays. Move `FileAndFolderHelperTests` and `GitHelperTests` into core tests.
**Regression guard.** `ToSafeFileNamePart` strips the Windows invalid-char set plus `:` and `\` explicitly on every platform; `IsValidSolutionFilePath_AbsoluteZipPath_ReturnsTrue` builds its input from `Path.GetTempPath()`. `RunProcess_LargeOutputOnBothStreams_Completes` uses `sh -c` off Windows, powershell on Windows. `RunProcess` takes `IReadOnlyList<string>` on both targets (net48 joins with `QuoteArgument`, net8 fills `ArgumentList`); update `RunGit` and every `RunProcess` call; `QuoteArgument` stays compiled on both targets. `FindRepositoryRootPath`, `IsValidSolutionFilePath`, `ToSafeFileNamePart`, `RunProcess`, `QuoteArgument`, `ParseBranchList` are `public` (Standing requirements). Update capcom note `untrusted-dataverse-data` refs and its QuoteArgument wording (`capcom know` edit).
**Files:** `Bulk Solution Exporter.Core/IO/FileAndFolderHelper.cs`, `Bulk Solution Exporter.Core/IO/GitHelper.cs`, `Bulk Solution Exporter.Core.Tests/FileAndFolderHelperTests.cs`, `Bulk Solution Exporter.Core.Tests/GitHelperTests.cs`, `Bulk Solution Exporter/BulkSolutionExporter_PluginControl.cs`, `.capcom/knowledge/untrusted-dataverse-data.md`
**Read first:** `Bulk Solution Exporter/Helpers/FileAndFolderHelper.cs — FindRepositoryRootPath, ToSafeFileNamePart, IsValidSolutionFilePath; Bulk Solution Exporter/Helpers/GitHelper.cs — RunProcess, QuoteArgument, Commit; Bulk Solution Exporter.Tests/GitHelperTests.cs — RunProcess_LargeOutputOnBothStreams_Completes, RunGit`
`Bulk Solution Exporter.Tests/FileAndFolderHelperTests.cs — ToSafeFileNamePart_HostileName_ContainsNoPathElements, IsValidSolutionFilePath_AbsoluteZipPath_ReturnsTrue; Bulk Solution Exporter/BulkSolutionExporter_PluginControl.cs — InitializeGitHelper; .capcom/knowledge/untrusted-dataverse-data.md`
**Tests:** new `FindRepositoryRootPath` tests (temp dir with `.git` two levels up; no repo → `string.Empty`); `GitHelper` commit test on a real temp repo passes on macOS.
**Acceptance:** `dotnet test "Bulk Solution Exporter.Core.Tests" --filter "FullyQualifiedName~FileAndFolder|FullyQualifiedName~GitHelper"` exits 0; solution builds.
**Commit:** `refactor(core): move file and git helpers to core and make them cross-platform`
Depends on item 2.

## 4. Move RetryRunner to core with a host-neutral error classifier — ✅ DONE (2026-10-23)

NOTES (2026-10-23): `RetryRunner` (namespace `Com.AiricLenz.BulkSolutionExporter.Core.Run`, mirrors the folder) catches any `Exception` whose message, or any inner exception's message in the `InnerException` chain, contains the fault text (ordinal match, private `IsMessageInChain`). Before, only a `FaultException` matched, so a non-fault exception that carries the fault text is now classified as a fault too. The XTB plugin still passes `FaultException`s, and these classify exactly as before.

NOTES (2026-10-23): the class, `IsRunAborted` and the three constants are `public`. The plugin control gained `using Com.AiricLenz.BulkSolutionExporter.Core.Run;`. Its import `catch (FaultException)` and its `System.ServiceModel` using stay unchanged, because nothing in this item changes the plugin's behaviour. The moved tests use the flat `Com.AiricLenz.BulkSolutionExporter.Core.Tests` namespace. Added test: `Execute_ImportAlreadyRunningInInnerException_RetriesAndReturnsTrue`, which wraps the fault in an `InvalidOperationException` and uses continueOnError false.

NOTES (2026-10-23): `Bulk Solution Exporter.Tests.csproj` still has its `System.ServiceModel` reference, which nothing in that project uses now. It was left alone because the item's Files do not list it.

NOTES (2026-10-23): checked on Linux with .NET SDK 8.0.426. The solution builds with 0 errors (only the NU190x Pkcs warnings, which were already there). The RetryRunner filter passes 15/15 core tests. `capcom check` and `capcom know verify` are clean.

**What:**
**Goal:** `RetryRunner` lives in core with no `System.ServiceModel` dependency. Its retry/fail decisions match today's `run-and-abort-rules` for any exception whose message carries the Dataverse fault text.
**Approach (assumed at the header base):** `git mv` to `Bulk Solution Exporter.Core/Run/RetryRunner.cs`. Replace `FaultException` matching with message-text classification over `Exception.Message`, including inner exceptions. Add `DataverseOperationException` (core) that gateways throw with the server message. Keep the injected `isAbortRequested`, `log`, `sleep`, `getUtcNow` delegates. Move `RetryRunnerTests`.
**Regression guard.** `RetryRunner.IsRunAborted` (called by the plugin and `RetryRunnerTests`) and every constant a test reads (`ImportAlreadyRunningFaultText`, `UpgradeAlreadyExistsFaultText`, `RetryWaitSliceInMilliseconds`) are `public` (Standing requirements). Update the capcom note `run-and-abort-rules` (refs line 4, text line 14) to the core path and the message-based fault wording.
**Files:** `Bulk Solution Exporter.Core/Run/RetryRunner.cs`, `Bulk Solution Exporter.Core/Run/DataverseOperationException.cs`, `Bulk Solution Exporter.Core.Tests/RetryRunnerTests.cs`, `Bulk Solution Exporter/BulkSolutionExporter_PluginControl.cs`, `.capcom/knowledge/run-and-abort-rules.md`
**Read first:** `Bulk Solution Exporter/Helpers/RetryRunner.cs — Execute, IsRunAborted, ImportAlreadyRunningFaultText, UpgradeAlreadyExistsFaultText; Bulk Solution Exporter/BulkSolutionExporter_PluginControl.cs — ExecuteWithRetries, IsRunAborted call site, import catch (FaultException)`
`Bulk Solution Exporter.Tests/RetryRunnerTests.cs — FakeRetryEnvironment, IsRunAborted_CancelledOrLatchSet_ReportsAbort; .capcom/knowledge/run-and-abort-rules.md`
**Tests:** every existing RetryRunner case, with `FaultException` swapped for `DataverseOperationException`; one case wrapping the fault text in an inner exception.
**Acceptance:** `dotnet test "Bulk Solution Exporter.Core.Tests" --filter "FullyQualifiedName~RetryRunner"` exits 0; solution builds.
**Commit:** `refactor(core): move RetryRunner to core with message-based fault classification`
Depends on item 2.

## 5. Move solution schema and versions file to core — ✅ DONE (2026-10-23)

NOTES (2026-10-23): `SolutionConfiguration`, `ExportedSolutionVersions`, `ExportedSolution` (now `public`) and the new `SolutionInfo` live in namespace `Com.AiricLenz.BulkSolutionExporter.Core.Schema`, which matches the folder. `ExportedSolutionVersions.FileName` (`"exported_versions.json"`) replaces `BulkSolutionExporter_Plugin.JsonSettingsFileName`. The JSON property names are unchanged, `SolutionIndentifier` included. `SolutionConfiguration` gained a public parameterless ctor.

NOTES (2026-10-23): `Solution` now takes `(Guid, SolutionInfo)`, exposes the wrapped data as `Info` and keeps getter-only `SolutionId`/`UniqueName`/`FriendlyName`/`Version` that delegate to it. Their `JsonProperty`/`JsonIgnore` attributes stay. The setters are gone because only `ConvertFrom` set them. `Solution.cs` imports `SolutionInfo` through a `using` alias, because `Microsoft.Xrm.Sdk.SolutionInfo` makes the bare name ambiguous. `ConvertFrom` also fills `IsManaged`/`Description` from the `ismanaged`/`description` columns the queries already fetch. It reads them with `GetAttributeValue`, so a missing or null value gives the default.

NOTES (2026-10-23): added `Bulk Solution Exporter.Core.Tests/SolutionConfigurationTests.cs`, which the item's Files does not list, for the required round-trip test. It follows one test class per source class, and the Acceptance filter `FullyQualifiedName~SolutionConfiguration` targets it. It pins a literal JSON in the shape `GetJson` writes today, deserializes it and re-serializes it byte-identically, and has one check that the identifier is read back.

NOTES (2026-10-23): checked on Linux with .NET SDK 8.0.426. `dotnet build "Bulk Solution Exporter.sln" -c Debug` gives 0 errors. The acceptance filter passes 6/6 and the full core suite passes 56/56. The grid sort/display check stays manual on Windows (user).

**What:**
**Goal:** Core owns `SolutionConfiguration`, `ExportedSolutionVersions` and a UI-free `SolutionInfo` (id, unique/friendly name, version, managed flag, description). The plugin's `Solution` keeps only its UI state and wraps `SolutionInfo`. The `exported_versions.json` file name is a core constant.
**Approach (assumed at the header base):** `git mv` `Schema/SolutionConfiguration.cs` and `Schema/ExportedSolutionVersions.cs` to `Bulk Solution Exporter.Core/Schema/`. Move `BulkSolutionExporter_Plugin.JsonSettingsFileName` to `ExportedSolutionVersions.FileName`. Keep the JSON property names byte-identical, including the `SolutionIndentifier` spelling (persisted data). Add a parameterless ctor. `Solution.ConvertFrom(Entity, Guid)` stays in the plugin and builds a `SolutionInfo`. Move `ExportedSolutionVersionsTests`.
**Regression guard.** `Solution` keeps public `FriendlyName`, `UniqueName`, `Version` and `SolutionId` properties that delegate to its `SolutionInfo` (the grid binds columns by reflection on these names). `TryLoadExportedSolution` becomes `(string exportFilePath, string uniqueName, out ExportedSolution)`; update the 4 control callers and the test helper `CreateSolution` → a unique-name constant. `ExportedSolution` becomes `public`. `Settings.cs` switches its `using` to the core schema namespace.
**Files:** `Bulk Solution Exporter.Core/Schema/SolutionConfiguration.cs`, `Bulk Solution Exporter.Core/Schema/ExportedSolutionVersions.cs`, `Bulk Solution Exporter.Core/Schema/SolutionInfo.cs`, `Bulk Solution Exporter/Schema/Solution.cs`, `Bulk Solution Exporter/BulkSolutionExporter_Plugin.cs`, `Bulk Solution Exporter.Core.Tests/ExportedSolutionVersionsTests.cs`, `Bulk Solution Exporter/BulkSolutionExporter_PluginControl.cs`, `Bulk Solution Exporter/Settings.cs`
**Read first:** `Bulk Solution Exporter/Schema/ExportedSolutionVersions.cs — TryLoadExportedSolution, FindMatch; Bulk Solution Exporter/Schema/Solution.cs — Solution, ConvertFrom, SolutionIdentifier; Bulk Solution Exporter/BulkSolutionExporter_PluginControl.cs — UpdateColumns, UpdateSolutionStatusImages`
`Bulk Solution Exporter/Settings.cs — GetSolutionConfiguration; XTB Components/Sortable Checklist/SortableCheckList.cs — property lookup by PropertyName; Bulk Solution Exporter.Tests/ExportedSolutionVersionsTests.cs — CreateSolution`
**Tests:** a round-trip test: the JSON from today's `GetJson` deserializes and re-serializes byte-identically.
**Acceptance:** `dotnet test "Bulk Solution Exporter.Core.Tests" --filter "FullyQualifiedName~ExportedSolution|FullyQualifiedName~SolutionConfiguration"` exits 0; solution builds. Manual Windows check (user): the solution grid shows friendly/unique names and versions and sorts by them.
**Commit:** `refactor(core): move solution schema and exported versions file to core`
Depends on items 2, 4.

## 6. Core run log: markup lines with indentation — ✅ DONE (2026-10-23)

NOTES (2026-10-23): re-derived from `Bulk Solution Exporter/Helpers/Formatter.cs` (Read first anchor). Item 2 moved `Formatter` to `Bulk Solution Exporter.Core/Formatter.cs`, and the file was not changed here.

NOTES (2026-10-23): `RunLog` (namespace `Com.AiricLenz.BulkSolutionExporter.Core.Run`) takes `(Action<string> sink, bool isDebugEnabled)`. It owns the indent level (`IncreaseIndent`/`DecreaseIndent`/`ResetIndent`/`CurrentIndent`), the public `Color*`/`ColorEndTag` constants, `IndentUnit` (the plugin's former `_logger.Indent` value), `Log`/`LogError`/`LogDebug`, and static markup builders `Heading(level, text)`/`Bold`/`Italic`/`Code`/`Colorize` for the coming RunEngine. The plugin keeps its call sites as they are. Its private `Color*` constants now alias `RunLog`'s, and the unused `ColorIndent` alias was dropped. `FormatErrorStringWithXml` gets `RunLog.IndentUnit`.

NOTES (2026-10-23): the cap is applied per line. `RunLog` splits on `\r\n`/`\n` and keeps the separators. It cuts only a line whose trimmed length is over `MaxLineLength` (10000), using the same colour-end-tag closing and `TruncationMarker` as `MarkdownParser.TruncateLine`. `MarkdownParser` keeps its own constants and truncation. When it re-truncates a line `RunLog` already cut, the result is the same string, so the plugin's log output does not change. The line, indent included, is now built on the logging thread and only the finished string is sent to the UI thread with `Invoke`, so the indent level is still captured on the worker thread.

NOTES (2026-10-23): `Logger` is now only the RichTextBox renderer (`Log(line)` → `MarkdownParser.ParseAndAppend`). The plugin builds `RunLog` with `#if DEBUG isDebugMode #else false`.

NOTES (2026-10-23): consequential edit — .capcom/knowledge/ui-thread-rules.md: made necessary by moving the `Invoke` from `Log` to its `RunLog` sink `AppendLogLine`. `capcom check` passes.

NOTES (2026-10-23): checked on Linux with .NET SDK 8. `dotnet build "Bulk Solution Exporter.sln"` gives 0 errors in both Debug and Release. The only warnings are the NU1902/NU1903 ones that were there before. The acceptance filter passes 11/11 and the full core suite passes 67/67. The live log look in XrmToolBox stays a manual check on Windows (user).

**What:**
**Goal:** Core has `RunLog`, which produces the log mini-markup strings (headings, bold/italic/code, `<color=#RRGGBB>`, indent, 10000-char cap) and hands each line to an `Action<string>` sink. The plugin renders the same strings through its existing `MarkdownParser`, so log output is unchanged.
**Approach (assumed at the header base):** Lift the string-building half of the control's `Log`/`LogError`/`LogDebug` and the indent state of `Helpers/Logger.cs` into `Bulk Solution Exporter.Core/Run/RunLog.cs`, including the colour/markup constants. `Logger` becomes the RichTextBox renderer only. The markup string is the contract both UIs parse.
**Regression guard.** The 10000-char cap applies per rendered line (split on `"\n"`/`NewLine`, same colour-end-tag closing and `TruncationMarker` as `MarkdownParser.TruncateLine`) or is left to the renderer — never to the whole message; this yields to `Helpers/Formatter.cs:16-20` (the cap is MarkdownParser's per-line limit). `MarkdownParser` keeps `MaxLogLineLength`/`TruncationMarker`. `RunLog` takes a runtime `isDebugEnabled` flag; the plugin passes `#if DEBUG isDebugMode #else false`.
**Files:** `Bulk Solution Exporter.Core/Run/RunLog.cs`, `Bulk Solution Exporter/Helpers/Logger.cs`, `Bulk Solution Exporter/BulkSolutionExporter_PluginControl.cs`, `Bulk Solution Exporter.Core.Tests/RunLogTests.cs`
**Read first:** `Bulk Solution Exporter/Helpers/Logger.cs — Log(message, indent), GetCurrentIndent, IncreaseIndent; Bulk Solution Exporter/BulkSolutionExporter_PluginControl.cs — Log, LogDebug, LogError, Color* constants, ImportSolution`
`Bulk Solution Exporter/Helpers/MarkdownParser.cs — ParseAndAppend, TruncateLine; Bulk Solution Exporter/Helpers/Formatter.cs — FormatErrorStringWithXml; Bulk Solution Exporter.Tests/MarkdownParserTests.cs — ParseAndAppend_HugeLine_EndsWithTruncationMarker`
**Tests:** `RunLogTests` pin the exact strings for heading, error, debug (with `isDebugEnabled` on and off), indented line and per-line truncation (a multi-line message longer than 10000 chars in total, each line shorter, is not cut).
**Acceptance:** `dotnet test "Bulk Solution Exporter.Core.Tests" --filter "FullyQualifiedName~RunLog"` exits 0; solution builds.
**Commit:** `refactor(core): extract log markup building into RunLog`
Depends on item 2.

## 7. RunEngine part 1: options, gateway, publish/version/export phases — ✅ DONE (2026-10-23)

NOTES (2026-10-23): `RunEngine.Run` is a public static method that returns `void`. Item 8 adds `RunResult`. Inside, it builds a private per-run instance that holds the `RunLog`, the abort-logged latch and the run's own copies of the request entries: a `SolutionInfo` copy with a cloned `Version`, and a configuration copy made by a JSON round trip. The engine never changes the `RunRequest` or the host's objects. The new version and changed durations reach the host only through `IRunSink.SolutionVersionChanged` / `ConfigurationChanged`, and both pass copies. The `targets` argument is checked for null but not used yet; the import phase is item 8.

NOTES (2026-10-23): `RunRequest` booleans use the `Should*` prefix that coding-standards requires: `ShouldPublishSource`, `ShouldUpdateVersion`, `ShouldExportManaged`/`Unmanaged`, `ShouldCommitToGit`, `ShouldPushCommit`, `ShouldImportManaged`/`Unmanaged`, `ShouldUpgrade`, `ShouldOverwriteCustomizations`, `ShouldPublishTargets`, `ShouldSaveVersionJson`, `ShouldContinueOnError`. It also has `VersionFormat`, `CommitMessage`, `RetryCount`, `RetryDelayInSeconds` and `List<RunSolution> Solutions`. The plan's list missed one setting, which is added as `ShouldEnableAutomation`: the import's `PublishWorkflows` comes from `flipSwitch_enableAutomation`. `RunSolution` (solution + configuration pair) is a second type in `RunRequest.cs`. `ImportOptions` (`IsManaged`, `IsHolding`, `ShouldPublishWorkflows`, `ShouldOverwriteUnmanagedCustomizations`) is a second type in `IDataverseGateway.cs`, following the two-types-per-file precedent of `ExportedSolutionVersions.cs`.

NOTES (2026-10-23): `IRunSink.Progress(stepStatus)` became `Progress(string message, string detail, bool isNewStep)`. These are the plugin's `ReportExtendedProgress` message, additionalMessage and resetTimer, because the apply-upgrade step (item 8) reports with `resetTimer: false`. `IDataverseGateway.UpdateSolutionVersion` takes the version as a string. `ExportSolution(uniqueName, isManaged)` returns `byte[]`.

NOTES (2026-10-23): `GetDefaultFileName` was not ported. The regression guard keeps it off the export path, so nothing in this item calls it, and it stays in the plugin for item 11 (`files/suggestFileNames`) to move into core. `SaveVersionJson` and the duration formatting (`GetDurationString` → private `FormatDuration`) are ported into `RunEngine`, as the item's Files list allows. The plugin keeps its own copies until item 9 routes the run through the engine. A natural later home for the version-file write is `ExportedSolutionVersions`, which already owns the read.

NOTES (2026-10-23): two small deviations from the plugin, to avoid crashes. A solution with no version (`SolutionInfo.Version == null`) logs "The solution has no version number!" and is skipped in the version phase; the plugin would throw a NullReferenceException. A version file whose content is `null` is treated as empty when the version file is saved. Two behaviours are kept as they were: a malformed version file still throws out of the run, as in the plugin, and the dead "Export aborted due to an error!" branch was dropped, because the plugin's `ExportSolution` never returned false. `SolutionProcessed` is raised after each solution's export step. That includes a solution whose files were skipped for a bad name, and a solution whose unmanaged export was skipped by an abort, matching the plugin's `RefreshSolutionInListBox` call sites.

NOTES (2026-10-23): `RunEngine.cs` is about 725 lines, over the coding-standards guide of about 400, because of the house style (dividers, one argument per line). Item 8 adds git and import, so it should split the coordinator into `partial` files by phase. The test sink `RecordingRunSink` is an internal second type in `RunEngineExportTests.cs`, so item 8's import tests can reuse it or move it to `Fakes/`.

NOTES (2026-10-23): checked on Linux with .NET SDK 8. `dotnet test "Bulk Solution Exporter.Core.Tests" --filter "FullyQualifiedName~RunEngine"` passes 14/14 and the full core suite passes 81/81. `dotnet build "Bulk Solution Exporter.sln" -c Debug` gives 0 errors; the only warnings are the NU1902/NU1903 ones that were there before.

**What:** Recast at the regression check (2026-10-20).
**Goal:** Core `RunEngine.Run(RunRequest, IDataverseGateway source, IReadOnlyList<IDataverseGateway> targets, IRunSink sink, CancellationToken)` executes publish-all (source), version update and export (file write + `exported_versions.json`) for exactly the solutions in the request. Behaviour matches the plugin's phases per `run-and-abort-rules`, proven against an in-memory fake gateway.
**Approach (assumed at the header base):** Deep module, small interface. `RunRequest` = every run setting (the `Settings` toggles, version format, commit message, retry count/delay, continue-on-error, save-version-json) plus the checked `SolutionInfo` list, each paired with its `SolutionConfiguration` clone. `IDataverseGateway`: `ConnectionName`, `PublishAll()`, `UpdateSolutionVersion(id, version)`, `ExportSolution(uniqueName, managed) → byte[]`, `SolutionExists(uniqueName) → bool`, `ImportSolution(bytes, ImportOptions)`, `DeleteAndPromote(uniqueName)`, `RetrieveSolutions(includeManaged) → IReadOnlyList<SolutionInfo>`. `IRunSink`: `Log(string)`, `SolutionVersionChanged(id, version)`, `ConfigurationChanged(SolutionConfiguration)`, `Progress(stepStatus)`. Port the logic of `PublishAll`, `UpdateCheckedVersionNumbers`/`UpdateVersionNumberInSource`, `ExportCheckedSolutions`/`ExportSolution`/`ExportToFile`, `SaveVersionJson` and `GetDefaultFileName`. File paths pass `IsValidSolutionFilePath`. Abort = token, checked between steps.
**Regression guard.** Publish/version/export are NOT retried: an export or version failure ends the run; source `PublishAll` logs `ex.Message` and continues (as `PluginControl.cs:1677-1684`). This yields to `.capcom/issues/release-2026-03-05/install-upgrade-retries.md:12` (retries for installs and upgrades only); the "Cannot start another" retry test moves to item 8. The engine writes only to each solution's configured `FileNameManaged`/`FileNameUnmanaged` path — no output root; `GetDefaultFileName` is not on the export path. `RunRequest` pairs each `SolutionInfo` with its `SolutionConfiguration` clone. Abort logs "Abort requested — skipping remaining steps." once. `IDataverseGateway.ConnectionName` keeps the logs "Publishing All now (<conn>)", "Handling Target *<conn>*", "[<conn>]".
Only a thrown gateway call ends the run: a gateway exception from version update or export propagates out of `RunEngine.Run` unchanged (the host keeps `args.Error`, its MessageBox and the no-auto-disable path, `PluginControl.cs:1352`, `:1373-1379`). An invalid version format, a missing file name and an invalid file path are logged and skipped, and the run continues (as `:2162`/`:1727`, `:2084`, `:2094`). `IRunSink` gains `SolutionProcessed(solutionId)`, raised after each solution's export (item 8: after each import too); its contract says the host handles it synchronously (the list refresh must not race `SaveVersionJson`, `.capcom/knowledge/ui-thread-rules.md:10`).
**Files:** `Bulk Solution Exporter.Core/Run/RunEngine.cs`, `Bulk Solution Exporter.Core/Run/RunRequest.cs`, `Bulk Solution Exporter.Core/Run/IDataverseGateway.cs`, `Bulk Solution Exporter.Core/Run/IRunSink.cs`, `Bulk Solution Exporter.Core.Tests/Fakes/FakeDataverseGateway.cs`, `Bulk Solution Exporter.Core.Tests/RunEngineExportTests.cs`
**Read first:** `Bulk Solution Exporter/BulkSolutionExporter_PluginControl.cs — ExecuteOperations, ExportCheckedSolutions, ExportToFile, UpdateVersionNumberInSource, SaveVersionJson; Bulk Solution Exporter/BulkSolutionExporter_PluginControl.cs — RefreshSolutionInListBox, WriteBackSolutionConfiguration`
`.capcom/knowledge/ui-thread-rules.md`
**Tests:** only the checked solutions are processed; abort after export of solution 1 stops solution 2 and logs the abort line once; a non-rooted traversal file name is rejected; the version JSON is written; an export failure ends the run without retry; a source publish-all failure is logged and the run continues; the configured file path is written as-is; a gateway exception from version update or export is thrown out of `Run` and no later phase is called; an invalid version format, a missing file name and an invalid path are logged and the next solution is still exported; `SolutionProcessed` is raised once per exported solution.
**Acceptance:** `dotnet test "Bulk Solution Exporter.Core.Tests" --filter "FullyQualifiedName~RunEngine"` exits 0.
**Commit:** `feat(core): add RunEngine with publish, version and export phases`
Depends on items 3, 4, 5, 6.

## 8. RunEngine part 2: git and import phases — ✅ DONE (2026-10-23)

NOTES (2026-10-23): `RunEngine` is now a `partial` class, as item 7's NOTES suggested. The new git phase is in `RunEngine.Git.cs` and the new target/import phase is in `RunEngine.Import.cs`. Neither file is in the plan's Files list. No existing code was moved: `RunEngine.cs` keeps publish, version and export and is about 840 lines. `RunEngine.Import.cs` is about 450 lines, a little over the ~400 guide, because of the house style.

NOTES (2026-10-23): git is injected as a client factory, `Func<string, IGitClient>`, because the engine derives the repository root itself. There are two public `Run` overloads. The 5-argument overload that hosts call uses `root => new GitClient(root)`. A 6-argument overload takes the factory, and the tests pass `FakeGitClient.Create`. Both now return `RunResult` instead of `void`. `RunResult` holds `IsAborted`, `Duration` and `StepOutcomes`. `StepOutcome` is a second type in `RunResult.cs` and has one entry per export file and per import attempt: IsImport, SolutionId, ConnectionName, IsManaged, IsSuccessful, Duration. `Run` now also rejects a null target entry and a null factory.

NOTES (2026-10-23): the repository root comes from the first configured file path among the run's request solutions, managed before unmanaged. `InitializeGitHelper` instead scans every stored configuration in `_settings`, which the engine never sees. The git phase stages exactly the files this run exported (`_sessionFiles`, filled in `ExportToFile`), as the plugin does.

NOTES (2026-10-23): the import keeps these plugin behaviours: managed wins when both import switches are on; RetryCount ≤ 0 → 3 and delay ≤ 0 → 5 s; a `SolutionExists` failure is logged and treated as not installed (it is checked for every import, as in the plugin); a failed commit still pushes; `ImportSolution` logs every exception and returns false. One small difference: a solution with no configuration gets the invalid-path error. The plugin returned false without logging anything. The retry wait sleeps on the cancellation token's wait handle, so an abort ends the wait at once.

NOTES (2026-10-23): `FakeDataverseGateway` gained `ImportedOptions` and `ImportFaultMessages`, which queues one armed fault per import call, for the import tests. `IRunSink.SolutionProcessed`'s doc now names the import, since the engine raises it after each import that does not end the loop, as `RefreshSolutionInListBox` did.

NOTES (2026-10-23): checked on Linux with .NET SDK 8. `dotnet test "Bulk Solution Exporter.Core.Tests" --filter "FullyQualifiedName~RunEngine"` passes 24/24 (10 new) and the full core suite passes 91/91. `dotnet build "Bulk Solution Exporter.sln" -c Debug` gives 0 errors; the only warnings are the NU1902/NU1903 ones that were there before. The retry tests wait the 1 s minimum delay.

**What:**
**Goal:** `RunEngine` also runs the git phase (add session files, commit, optional push; skipped entirely on abort) and, per target, import with holding-upgrade + `DeleteAndPromote`, optional publish-all, and continue-on-error. Every engine-side rule in `run-and-abort-rules` (git skipped on abort; "Cannot start another [Import]" retried; "_Upgrade already exists" fails without promote; exhausted retries → failure; holding install + promote; continue-on-error) has a fake-gateway test; host-side rules (UI-thread snapshot, settings flush, no auto-disable on abort, "Aborted by user." line) belong to item 9.
**Approach (assumed at the header base):** Port `HandleGit`, `ImportCheckedSolutions`/`ImportSolution`/`DoesSolutionExistInTarget` and target `PublishAll`. Holding = managed && exists && upgrade on; holding install + promote is one step. "_Upgrade already exists" fails that solution without promote. The result is `RunResult` (aborted flag, per-solution outcomes, step durations), so the hosts render Done/Aborted. Git goes through an injected `IGitClient` that wraps core `GitHelper`. Preserve today's import quirks: RetryCount ≤ 0 → 3 retries, delay ≤ 0 → 5 s; a `SolutionExists` error is logged and treated as not installed; a commit failure still attempts push; `ImportSolution` catches everything and returns false.
**Regression guard.** Core ships a production `GitClient : IGitClient` wrapping `GitHelper`. The engine derives the repo root from the first configured file path via `FindRepositoryRootPath`, exactly as `InitializeGitHelper` does.
**Files:** `Bulk Solution Exporter.Core/Run/RunEngine.cs`, `Bulk Solution Exporter.Core/Run/RunResult.cs`, `Bulk Solution Exporter.Core/Run/IGitClient.cs`, `Bulk Solution Exporter.Core/Run/GitClient.cs`, `Bulk Solution Exporter.Core.Tests/Fakes/FakeGitClient.cs`, `Bulk Solution Exporter.Core.Tests/RunEngineImportTests.cs`
**Read first:** `Bulk Solution Exporter/BulkSolutionExporter_PluginControl.cs — ExecuteOperations (target loop), ImportSolution, ExecuteWithRetries, DoesSolutionExistInTarget, HandleGit, InitializeGitHelper`
`Bulk Solution Exporter/Helpers/GitHelper.cs — AddFile, Commit, Push; Bulk Solution Exporter/Helpers/RetryRunner.cs — Execute, UpgradeAlreadyExistsFaultText; .capcom/knowledge/run-and-abort-rules.md`
**Tests:** holding path calls `DeleteAndPromote`; an unmanaged import never uses holding; abort skips git; continue-on-error moves on to the next solution; two targets are each imported; exhausted retries → failure; "_Upgrade already exists" fails the solution without calling `DeleteAndPromote`; an import retry on "Cannot start another" succeeds; the git repo root comes from the first configured file path.
**Acceptance:** `dotnet test "Bulk Solution Exporter.Core.Tests" --filter "FullyQualifiedName~RunEngine"` exits 0.
**Commit:** `feat(core): add git and import phases to RunEngine`
Depends on item 7.

## 9. XTB plugin runs through RunEngine — ✅ DONE (2026-10-23)

NOTES (2026-10-23): build only for `Bulk Solution Exporter.Tests`: on Linux arm64 with .NET SDK 8.0.426, `dotnet build "Bulk Solution Exporter.sln"` exits 0 with no compiler warnings (only the NU1902/NU1903 warnings that were already there). The net48 tests were not run because there is no Windows/mono here. `dotnet test "Bulk Solution Exporter.Core.Tests"` passes 91/91.

NOTES (2026-10-23): `RunRequestFactory.Create(Switches, Settings, solutions)` takes a nested `RunRequestFactory.Switches` value object. `ExecuteOperations` fills it from the controls on the UI thread (`ReadRunSwitches`), so no file under `Run/` names a control. The factory copies each configuration on the UI thread and passes `Solution.Info` as the request's solution; the engine copies that before it changes anything.

NOTES (2026-10-23): the Work body is now the named method `ExecuteRunOnWorker`, which reads no control, list box or `_settings`. The sink's handlers are `ApplySolutionVersion` (sets the listed `Solution.Info.Version`, then refreshes the list), `RefreshProcessedSolution` and `ApplyRunConfiguration`. `ApplyRunConfiguration` replaces `WriteBackSolutionConfiguration`: it merges all four duration fields of the run's copy into `_settings` and saves. All of them run synchronously through `ControlRunSink` (Invoke). Progress goes through `ReportExtendedProgress` (the worker's ReportProgress).

NOTES (2026-10-23): deleted the old phase methods and members that only the run used: PublishAll, UpdateCheckedVersionNumbers, ExportCheckedSolutions, SaveVersionJson, ImportCheckedSolutions, ExportSolution, ExportToFile, UpdateVersionNumberInSource, HandleGit, ImportSolution, ExecuteWithRetries, DoesSolutionExistInTarget, WriteBackSolutionConfiguration, IsAbortRequested and ExecutionSettingsSnapshot. Also deleted the `_sessionFiles` and `_isAbortRequested` fields, the int overload of GetDurationString, the ColorSolution/ColorConnection/ColorTeeth/dateTimeFormat constants, and the now-unused System.ServiceModel, Microsoft.Crm.Sdk.Messages and Newtonsoft.Json usings. `GetDefaultFileName` and `RefreshSolutionInListBox` stay.

NOTES (2026-10-23): a target connection without a `ServiceClient` is now logged ("has no connection and is skipped") and left out of the run, because the engine's target list cannot hold null. Before, the plugin logged its target heading, imported nothing, and Publish All failed with a logged error. The gateway also throws `InvalidOperationException` when an export response has no file, which honours `IDataverseGateway.ExportSolution`'s "never null" contract.

NOTES (2026-10-23): consequential edit — AGENTS.md: made necessary by the `ui-thread-rules` title change (`capcom agents` refreshed its knowledge block; `capcom check` is clean). `.capcom/issues/worker-reads-ui-controls.md` still names `ExecutionSettingsSnapshot`. Those are the intended grep hits: the ticket is left for the closeout.

NOTES (2026-10-23): core `RetryRunner.IsRunAborted` (and its test in RetryRunnerTests) no longer has a production caller. The plugin now uses `_runResult?.IsAborted ?? _runCancellation.IsCancellationRequested`. It was left in place because it is outside this item's per-phase deletion scope. The pre-existing unused `ColorError`/`ColorFile` constants were also left in place.

**What:** Recast at the regression check (2026-10-20).
**Goal:** The plugin's Execute path builds a complete `RunRequest` on the UI thread and runs `RunEngine` inside `WorkAsync`, through an `OrganizationServiceGateway` adapter. No worker-thread code reads a UI control. Log output, abort and settings write-back behave as before.
**Approach (assumed at the header base):** New `Bulk Solution Exporter/Run/OrganizationServiceGateway.cs` (wraps `IOrganizationService`: `PublishAllXmlRequest`, `Entity("solution")` update, `ExportSolutionRequest`, `QueryExpression` exists check, `ImportSolutionRequest`, `DeleteAndPromoteRequest`; turns `FaultException` into `DataverseOperationException`; `ConnectionName` = `ConnectionDetail.ConnectionName`). New `Run/ControlRunSink.cs` (marshals to UI as the guard below says). `ExecuteOperations` builds `RunRequest` through `RunRequestFactory` (as the guard below says), replacing `ExecutionSettingsSnapshot`. Delete only the old per-phase methods whose sole callers were in `ExecuteOperations`. `PostWorkCallBack` reads `RunResult` from a field.
**Regression guard.** `ControlRunSink` uses synchronous `Invoke` for `SolutionVersionChanged` and `Log`, `BeginInvoke` only for progress text (yields to `PluginControl.cs:2189` and `:3105`, "Invoke, not BeginInvoke"). `RunResult` is kept in a field; `isAborted = RunResult?.Aborted ?? cts.IsCancellationRequested`. `IRunSink.Progress` is routed via `ReportProgress` so the `isAborting` guard holds. `GetDefaultFileName` stays callable by browse/wizard. Static `RunRequestFactory` lives in the plugin; `SmokeTests` uses its own tiny fake gateway. Behaviour clauses of the Goal are the user's manual Windows check.
RunRequestFactory builds publish/version/export/git/push/import/upgrade/overwrite/publish-target toggles, version format and commit message from the controls' IsOn/Text on the UI thread; only SaveVersionJson, retry count/delay, ContinueOnError and the solution configs come from Settings; the smoke test passes explicit toggles rather than relying on `new Settings()` defaults. The CancellationTokenSource is cancelled unconditionally before the if/else in button_Export_Click. ControlRunSink handles item 7's per-solution processed event synchronously (Invoke), like SolutionVersionChanged. Rule: every capcom ref or text naming a deleted symbol is updated (`grep -rn 'ExecutionSettingsSnapshot\|IsAbortRequested\|worker-reads-ui-controls' .capcom` shows only intended hits). This yields to `.capcom/issues/worker-reads-ui-controls.md:14` (switch values captured on the UI thread, not Settings).
**Files:** `Bulk Solution Exporter/Run/OrganizationServiceGateway.cs`, `Bulk Solution Exporter/Run/ControlRunSink.cs`, `Bulk Solution Exporter/Run/RunRequestFactory.cs`, `Bulk Solution Exporter/BulkSolutionExporter_PluginControl.cs`, `Bulk Solution Exporter.Tests/SmokeTests.cs`, `.capcom/knowledge/run-and-abort-rules.md`, `.capcom/knowledge/ui-thread-rules.md`
**Read first:** `Bulk Solution Exporter/BulkSolutionExporter_PluginControl.cs — ExecuteOperations, button_Export_Click, OnPluginControl_Load, ExecutionSettingsSnapshot; Bulk Solution Exporter/BulkSolutionExporter_PluginControl.designer.cs — flipSwitch_updateVersion`
`Bulk Solution Exporter/Settings.cs — Settings ctor; Bulk Solution Exporter.Tests/SmokeTests.cs — SmokeTests; .capcom/issues/worker-reads-ui-controls.md`
**Closes:** worker-reads-ui-controls
**Tests:** smoke test builds a `RunRequest` via `RunRequestFactory`, passing explicit toggles rather than relying on `new Settings()` defaults, and runs it against the test's own tiny fake gateway.
**Acceptance:** `dotnet build "Bulk Solution Exporter.sln"` exits 0; `grep -nE "flipSwitch_|textBox_" "Bulk Solution Exporter/Run/"*.cs` prints nothing; the extracted Work method grepped for `flipSwitch_|textBox_|listBox` prints nothing; `dotnet test "Bulk Solution Exporter.Core.Tests"` exits 0; `dotnet test "Bulk Solution Exporter.Tests"` exits 0 on Windows; on macOS it is build-only and "build only" is recorded in NOTES (item 1's rule); `grep -rn 'ExecutionSettingsSnapshot\|IsAbortRequested\|worker-reads-ui-controls' .capcom` shows only intended hits.
**Commit:** `refactor(plugin): run Execute through the shared RunEngine`
Depends on item 8.

## 10. PPTB worker: host, protocol and run method — ✅ DONE (2026-10-23)

NOTES (2026-10-23): added three files the plan's Files list does not name. `RpcRunSink.cs` is the `IRunSink` that sends the `run/*` notifications. `RunReport.cs` holds `RunReport` and `StepReport`, the `run/start` answer: `RunResult` with durations as seconds (`durationInSeconds`), because Newtonsoft would write a `TimeSpan` as "hh:mm:ss". `VersionJsonConverter.cs` sends the core `Version` (a custom class, not `System.Version`) as its dotted string. The tests add `Fakes/FakeToolHost.cs`, the TS-side fake.

NOTES (2026-10-23): this is the wire format for items 11–15. It uses named parameters and camelCase names (`WorkerMethods.CreateFormatter`: Newtonsoft `CamelCasePropertyNamesContractResolver` plus `VersionJsonConverter`). `run/start(request, sourceName, targetNames)` returns `{isAborted, durationInSeconds, steps[]}`. Only one run can be active at a time; a second `run/start` while a run is active gets an error. `run/abort()` returns true when a run was active. A JSON-RPC cancellation of `run/start` also aborts the run. Any exception that ends the run is written to stderr and returned as the request's error.

NOTES (2026-10-23): every reverse call carries `target` (0 = source, 1..n = targets in the order of `targetNames`). `exportSolution` returns a base64 string; null or empty throws `InvalidOperationException`, as the plugin's gateway does. `importSolution` sends `solutionFile` as base64, plus `isManaged`, `isHolding`, `shouldPublishWorkflows` and `shouldOverwriteUnmanagedCustomizations`. `retrieveSolutions` returns an array of `SolutionInfo`, where `null` counts as empty. Only `RemoteInvocationException` becomes a `DataverseOperationException`. A lost connection or a method-not-found error passes through unchanged and ends the run. Reverse calls ignore the abort, so a running call always finishes.

NOTES (2026-10-23): notifications are fire-and-forget. `IRunSink.SolutionProcessed` says the host must finish before it returns, but a notification only waits until the message is written, so the UI may refresh a solution's file status after the engine has moved on. Items 13–15 should not rely on that ordering.

NOTES (2026-10-23): no `<Version>` is set in the worker .csproj, so the package packs as 1.0.0. The worker version and its match with `pptb.config.json` are left to items 12 and 16 (no version identifiers changed). The tests' project does not inherit `RestorePackagesWithLockFile`, so it has no `packages.lock.json`. The test project uses MSTest, as `Bulk Solution Exporter.Core.Tests` does. `Program.Main` points `Console.Out` at stderr after it takes the raw stdout stream, so stray console output cannot corrupt the protocol.

NOTES (2026-10-23): checked on Linux arm64 with .NET SDK 8.0.426. `dotnet test "Bulk Solution Exporter.PptbWorker.Tests"` passes 9/9. `dotnet pack "Bulk Solution Exporter.PptbWorker" -c Release -o artifacts/worker` packs `tools/net8.0/any/` with `DotnetToolSettings.xml`, `Com.AiricLenz.BulkSolutionExporter.Core.dll`, the runtimeconfig/deps files and every dependency. The packed output was then deleted, since `artifacts/` is gitignored. A real stdio smoke test (a framed `platform/initialize` sent to the built DLL) got the framed answer on stdout and the ready line on stderr. `dotnet build "Bulk Solution Exporter.sln" -c Debug` gives 0 errors and only the NU1902/NU1903 warnings that were already there. `NuGet Pack.ps1` builds the whole sln, so it now builds the worker too; this needs the .NET 8 SDK, which the core already requires.

NOTES (2026-10-23): pre-existing in core: `GitHelper` sets `RedirectStandardInput` only when it has input, so a git child process inherits the worker's stdin, which is the JSON-RPC input stream. Git normally does not read stdin for add/commit/push, but a git that does could take protocol bytes.

**What:**
**Goal:** `Bulk Solution Exporter.PptbWorker` (net8.0, `PackAsTool`, PackageId `AiricLenz.BulkSolutionExporter.PptbWorker`, command `bse-pptb-worker`, StreamJsonRpc) answers `platform/initialize` (`jsonrpc-stdio-v1`, v1) and exposes `run/start` and `run/abort`. It runs `RunEngine` with a `RpcDataverseGateway` that reverse-calls TS (`dataverse/publishAll`, `dataverse/updateSolutionVersion`, `dataverse/exportSolution` → base64, `dataverse/solutionExists`, `dataverse/importSolution` ← base64, `dataverse/deleteAndPromote`, `dataverse/retrieveSolutions`; each takes a `target` index, 0 = source), and streams `run/log`, `run/progress`, `run/solutionVersionChanged`, `run/configurationChanged` and `run/solutionProcessed` notifications.
**Approach (assumed at the header base):** Follow the sample worker .csproj (no RID, no self-contained, `RestorePackagesWithLockFile`). stdout carries only JSON-RPC; diagnostics go to stderr. The JSON-RPC loop keeps dispatching during reverse calls (run on a background task). Remote errors become `DataverseOperationException`.
**Regression guard.** The protocol also emits `run/solutionVersionChanged`, `run/configurationChanged` and `run/solutionProcessed` (from item 7's `IRunSink.SolutionProcessed`) notifications (items 13–15 consume them). The `packages.lock.json` files that `RestorePackagesWithLockFile` writes (worker, and the tests' if the setting is inherited) are committed.
**Files:** `Bulk Solution Exporter.PptbWorker/Bulk Solution Exporter.PptbWorker.csproj`, `Bulk Solution Exporter.PptbWorker/Program.cs`, `Bulk Solution Exporter.PptbWorker/WorkerMethods.cs`, `Bulk Solution Exporter.PptbWorker/RpcDataverseGateway.cs`, `Bulk Solution Exporter.PptbWorker.Tests/Bulk Solution Exporter.PptbWorker.Tests.csproj`, `Bulk Solution Exporter.PptbWorker.Tests/WorkerProtocolTests.cs`, `Bulk Solution Exporter.sln`, `Bulk Solution Exporter.PptbWorker/packages.lock.json`, `Bulk Solution Exporter.PptbWorker.Tests/packages.lock.json` (if inherited)
**Read first:** `Bulk Solution Exporter.sln; Bulk Solution Exporter/Scripts/NuGet Pack.ps1 — msbuild $solutionFile /t:Rebuild (builds every sln project, worker included); Bulk Solution Exporter/Helpers/GitHelper.cs — GitHelper (IGitClient backing); Bulk Solution Exporter/Helpers/RetryRunner.cs — RetryRunner`
**Tests:** in-process duplex-pipe JSON-RPC: initialize handshake; `run/start` against a TS-side fake that answers the reverse calls writes the ZIP to a temp dir; `run/abort` mid-run yields an aborted result; a version update emits `run/solutionVersionChanged`, a configuration change emits `run/configurationChanged` and each exported solution emits `run/solutionProcessed`.
**Acceptance:** `dotnet test "Bulk Solution Exporter.PptbWorker.Tests"` exits 0. `dotnet pack "Bulk Solution Exporter.PptbWorker" -c Release -o artifacts/worker` produces a nupkg whose `tools/net8.0/any/` holds `DotnetToolSettings.xml` and the core DLL (`unzip -l`).
**Commit:** `feat(pptb): add .NET worker hosting RunEngine over JSON-RPC`
Depends on item 8.

## 11. PPTB worker: workspace methods — ✅ DONE (2026-10-23)

NOTES (2026-10-23): re-derived from the assumption that the worker can reach `GetDefaultFileName`. It was a private method of the plugin control, so it moved to core as `FileAndFolderHelper.GetDefaultSolutionFileName(uniqueName, isManaged)`, and the plugin's four call sites now call it. The wizard's "stays inside the selected folder" check moved with it as `FileAndFolderHelper.IsInsideFolder`, which the plugin wizard and `files/suggestFileNames` both use. `FileAndFolderHelper.IsValidPath` changed from internal to public so the worker can validate folder paths (Standing requirements: every member the worker calls is public).

NOTES (2026-10-23): added files the plan's Files list does not name: `GitBranches.cs` (the answer of `git/branches` and `git/switchBranch`) and `SuggestedFileNames.cs` (one entry of `files/suggestFileNames`), one type per file. `SolutionFileStatus.cs` also holds `ExportFileStatus`, the status of one export file. `WorkerMethods.cs` gets `IsRunActive`, and `Attach` now also registers `WorkspaceMethods`.

NOTES (2026-10-23): wire format for items 13–15, using the same named, camelCase parameters as item 10. `git/branches(filePath)` returns `{repositoryRoot, activeBranch, branches[]}`. It is empty (null root, no branches) when the file is in no repository. The repository is found by walking up from the file's folder, as `InitializeGitHelper` does, so the UI passes the first configured export path. `git/switchBranch(filePath, branchName)` returns the same shape after the switch. It returns an error when the name is not in the local branch list, when a run is active, when the file is in no repository, or when git fails (the message is git's stderr). `files/status(solutions: RunSolution[], shouldSaveVersionJson)` returns `[{uniqueName, managed, unmanaged, isOutdated}]` in input order. Each file part is `{isConfigured, isPresent, exportedVersion, isOutdated}`. `files/exportCsv(filePath, solutions: SolutionInfo[])` returns null. The UI picks the solutions (the checked ones, or all when none are checked, as the plugin does). `files/suggestFileNames(folderPath, uniqueNames[])` returns `[{uniqueName, fileNameManaged, fileNameUnmanaged}]`. Both paths are null when either would leave the folder; the plugin logs an error and skips that solution in the same case.

NOTES (2026-10-23): validation. A file path must be absolute, must end in a valid file name (`IsValidSolutionFilePath`) and must have no `..` segment. A folder path must be absolute (`IsValidPath`) and must have no `..` segment. A list with a null entry is refused. The staleness rules were ported unchanged from `UpdateSolutionStatusImages`/`UpdateFileWarningStatus`, with one change: `SolutionFileStatus.Evaluate` reads the version file only for a path that passes `IsValidSolutionFilePath`. Any other path counts as unrecorded (outdated unless the file exists), so a malformed configured path no longer reaches `Path.GetDirectoryName`. A null solution version compares as unequal instead of throwing.

NOTES (2026-10-23): CSV hardening. The header and line format are unchanged (`Environment.NewLine` line ends, unquoted plain fields, `SolutionId` in "D" format). A field that holds `,`, `"`, CR or LF is now quoted, RFC 4180 style. Spreadsheet formula prefixes (`=`, `+`, `-`, `@`) are not neutralised, because that would change the data.

NOTES (2026-10-23): the workspace methods are synchronous, so StreamJsonRpc runs them one at a time and each blocks the dispatcher while git runs. They are short, local operations. The pre-existing stdin inheritance by git child processes (item 10 NOTES) applies to `git branch` and `git checkout` too.

NOTES (2026-10-23): checked on Linux arm64, .NET SDK 8.0.426. `dotnet test "Bulk Solution Exporter.PptbWorker.Tests"` passes 30/30 (21 new). `dotnet test "Bulk Solution Exporter.Core.Tests"` passes 91/91. `dotnet build "Bulk Solution Exporter.sln" -c Debug` gives 0 errors and only the NU1902/NU1903 warnings that were already there. The plugin needed a `SolutionInfo` using alias, as `Schema/Solution.cs` has, because `Microsoft.Xrm.Sdk.SolutionInfo` makes the name ambiguous. The plugin-side changes (status icons, warning icons, CSV export, file wizard) still need the user's manual check in XrmToolBox on Windows.

**What:**
**Goal:** The worker exposes the non-run operations the UI needs: `git/branches`, `git/switchBranch`, `files/status` (per solution: exported version and staleness from `exported_versions.json`, file present), `files/exportCsv` (columns `Unique Name, Solution Guid, Version`), `files/suggestFileNames` (the file-location wizard's names via `GetDefaultFileName`). All inputs are validated per `untrusted-dataverse-data`.
**Approach (assumed at the header base):** Thin methods over core `GitHelper`, `ExportedSolutionVersions`, `FileAndFolderHelper` and the CSV writer, which moves from the control's `ExportSolutionCsv` into core `Schema/SolutionCsv.cs` (the plugin calls the core version).
**Regression guard.** The `files/status` staleness rules are ported from the control's `UpdateSolutionStatusImages`/`UpdateFileWarningStatus` into core (gated on `SaveVersionJson` as today) and the plugin calls the core version — not re-derived.
**Files:** `Bulk Solution Exporter.PptbWorker/WorkspaceMethods.cs`, `Bulk Solution Exporter.Core/Schema/SolutionCsv.cs`, `Bulk Solution Exporter.Core/Schema/SolutionFileStatus.cs`, `Bulk Solution Exporter/BulkSolutionExporter_PluginControl.cs`, `Bulk Solution Exporter.PptbWorker.Tests/WorkspaceMethodsTests.cs`
**Read first:** `Bulk Solution Exporter/BulkSolutionExporter_PluginControl.cs — ExportSolutionCsv, UpdateSolutionStatusImages, UpdateFileWarningStatus, button_fileWizzard_Click, InitializeGitHelper`
`Bulk Solution Exporter/Schema/ExportedSolutionVersions.cs — TryLoadExportedSolution; Bulk Solution Exporter/Helpers/GitHelper.cs — GetAllBranches, SwitchToBranch`
**Tests:** branch listing on a temp repo; stale-file detection (ported control rules, including the `SaveVersionJson` gate); CSV content pinned; a traversal file name is refused.
**Acceptance:** `dotnet test "Bulk Solution Exporter.PptbWorker.Tests"` exits 0; solution builds.
**Commit:** `feat(pptb): add git, file status and CSV methods to the worker`
Depends on item 10.

## 12. PPTB tool scaffold, manifest and Dataverse bridge

**What:**
**Goal:** `pptb/` is a buildable PPTB tool with a valid manifest (`npm run validate` = `pptb-validate` passes). Its TS Dataverse bridge answers every reverse call from item 10 using `dataverseAPI` against the right connection index.
**Approach (assumed at the header base):** Vite + React 18 + Fluent UI v9 + Vitest, IIFE single-bundle config per the PPTB samples. `package.json`: name `@airiclenz/pptb-bulk-solution-exporter`, displayName "Bulk Solution Exporter", license `GPL-3.0`, icon `icons/tool.svg` (from `Images/`, `fill="currentColor"`), `features: { connections: { min: 1, max: 10 }, minAPI: "1.2.7" }`, `configurations.repository` = the GitHub repo. `pptb.config.json` declares worker `engine` (packageId/command from item 10, `net8.0`, `minimumRuntimeVersion 8.0.0`, `rollForward Major`, `platforms ["all"]`); `packageVersion` is a literal in `pptb.config.json` (item 16's check enforces it equals the worker .csproj `Version`). `src/dataverseBridge.ts`: ExportSolution via `execute` (action), import via `execute` `ImportSolution` (with `HoldingSolution`), `DeleteAndPromote`, `PublishAllXml`, `update("solution")`, solutions via `queryData`.
**Regression guard.** `package.json` also carries `description`, `contributors: [{name:"Airic Lenz"}]` and `configurations.readmeUrl` = `https://raw.githubusercontent.com/airiclenz/XTB-Bulk-Solution-Exporter/master/README.md` (`@pptb/validate` 1.0.5 requires both). The source icon is `pptb/public/icons/tool.svg` (bundled under dist). The worker entry has `kind: "dotnet-tool"`. License `GPL-3.0` is forced by `APPROVED_LICENSES`; it supersedes `.capcom/knowledge/versioning-and-release.md:10` ("GPL-3.0-only everywhere") for the npm package (item 16 records the exception). `pptb/package-lock.json` is committed; `.gitignore` gains `pptb/dist/` and `pptb/*.tgz`. A rejected `dataverseAPI.execute` surfaces its original message text unchanged (RetryRunner classifies by message text).
**Files:** `pptb/package.json`, `pptb/pptb.config.json`, `pptb/vite.config.ts`, `pptb/tsconfig.json`, `pptb/index.html`, `pptb/src/main.tsx`, `pptb/src/dataverseBridge.ts`, `pptb/src/dataverseBridge.test.ts`, `pptb/package-lock.json`, `pptb/public/icons/tool.svg`, `.gitignore`
**Read first:** `pptb/package.json — license, contributors, configurations, features; scratch @pptb/validate dist/validate.js — validatePackageJson, validateWorkers, APPROVED_LICENSES; @pptb/types dataverseAPI.d.ts — execute, queryData, update, ConnectionTarget`
`@pptb/types toolboxAPI.d.ts — WorkerSessionOptions.requests; Bulk Solution Exporter/Helpers/RetryRunner.cs — ImportAlreadyRunningFaultText, UpgradeAlreadyExistsFaultText; .gitignore`
**Tests:** Vitest with a mocked `window.dataverseAPI`: each bridge method calls the expected operation with the right connection target; a holding import sets `HoldingSolution: true`; a rejected `dataverseAPI.execute` surfaces its original message text unchanged in the reverse-call error.
**Acceptance:** `cd pptb && npm ci && npm run build && npm test && npm run validate` exit 0 (`npm run validate` needs network: it HEAD-fetches the repository and readme URLs); `dist/index.html` has no `type="module"`.
**Commit:** `feat(pptb): scaffold PPTB tool with manifest and Dataverse bridge`
Depends on item 10.

## 13. PPTB settings store and worker client

**What:**
**Goal:** `src/settingsStore.ts` persists the global settings (the `Settings` fields, minus XTB-only UI ones) and per-solution `SolutionConfiguration`s through `toolboxAPI.settings`, keyed by lower-cased environment URL + `.` + unique name. `src/workerClient.ts` wraps `toolboxAPI.workers.connect('engine', …)`: it registers the bridge, awaits `ready`, offers typed `startRun`/`abort`/workspace calls and log/progress subscriptions, and stops the worker on unload.
**Approach (assumed at the header base):** Typed mirrors of `RunRequest`, `RunResult`, `SolutionConfiguration` in `src/types.ts`, field names matching the C# JSON. Saves are debounced. `ConfigurationChanged` notifications write back through the store.
**Regression guard.** The client routes item 10's `run/configurationChanged` and `run/solutionVersionChanged` notifications to the store. Defaults are those of `new Settings()` (`SaveVersionJson` false, `Upgrade` true, `VersionFormat` "YYYY.MM.DD.+", `RetryCount` 3, `RetryDelayInSeconds` 10), not the property initializers; excluded XTB-only fields: sorting, splitter, tooltips, `LastUsedOrganizationWebappUrl`, `ConnectionTimeoutInMinutes`.
**Files:** `pptb/src/types.ts`, `pptb/src/settingsStore.ts`, `pptb/src/settingsStore.test.ts`, `pptb/src/workerClient.ts`, `pptb/src/workerClient.test.ts`
**Read first:** `Bulk Solution Exporter/Settings.cs — Settings ctor, SaveVersionJson, Upgrade, RetryCount; Bulk Solution Exporter/Schema/SolutionConfiguration.cs — SolutionIndentifier, LastExport*/LastImport*`
`@pptb/types toolboxAPI.d.ts — WorkersAPI.connect, WorkerSession, SettingsAPI, Connection.url; pptb/src/dataverseBridge.ts`
**Tests:** keying by URL (case-insensitive); defaults match `new Settings()` (values above); the worker client registers every bridge method from item 10 by name; `run/configurationChanged` and `run/solutionVersionChanged` notifications reach the store.
**Acceptance:** `cd pptb && npm test && npm run build` exit 0.
**Commit:** `feat(pptb): add settings store and typed worker client`
Depends on item 12.

## 14. PPTB UI: solution grid and run options

**What:**
**Goal:** The main view lists source solutions in a Fluent `DataGrid`: checkbox, friendly/logical name toggle, version, target version status icon (equal/different/missing, single target), file status icon (from `files/status`), sortable columns, text filter, persisted check state and order. Above it are the run-option switches (publish source, update version + format with `Version.ValidateFormat` rules, export managed/unmanaged, git commit/push + message, import managed/unmanaged, upgrade, overwrite, publish target).
**Approach (assumed at the header base):** Components in `src/components/`. Solutions load through the bridge (`retrieveSolutions`) for index 0 and, when exactly one target exists, for index 1. Theme follows `toolboxAPI.utils.getCurrentTheme`.
**Regression guard.** `versionFormat` mirrors `Version.ValidateFormat`: 2–4 dot-separated parts, each exactly one of `+ # YYYY MM DD`; accept "YYYY.MM.DD.+", "#.+"; reject "1", "#.#.#.#.#", "YYYY.X", "". At least one of friendly/logical name is always shown (turning one off forces the other on, as `SettingsForm` does).
**Files:** `pptb/src/App.tsx`, `pptb/src/components/SolutionGrid.tsx`, `pptb/src/components/RunOptions.tsx`, `pptb/src/components/SolutionGrid.test.tsx`, `pptb/src/versionFormat.ts`, `pptb/src/versionFormat.test.ts`
**Read first:** `Bulk Solution Exporter/Schema/Version.cs — ValidateFormat, VersionTokens; Bulk Solution Exporter.Tests/VersionTests.cs; Bulk Solution Exporter/BulkSolutionExporter_PluginControl.cs — SetExportButtonState, column setup (ShowFriendly/ShowLogicalSolutionNames)`
`Bulk Solution Exporter/SettingsForm.cs — name flip switch handlers; pptb/src/settingsStore.ts`
**Tests:** React Testing Library: filtering, sorting, check persistence via a mocked store, the name toggle never hides both name columns; `versionFormat` accepts and rejects the cases pinned in the guard.
**Acceptance:** `cd pptb && npm test && npm run build` exit 0.
**Commit:** `feat(pptb): add solution grid and run options`
Depends on item 13.

## 15. PPTB UI: run panel, log view, settings dialog, file wizard, git branch

**What:**
**Goal:** Execute starts `run/start` with the checked snapshot. The UI is disabled while running and Abort calls `run/abort`. The log view renders the `RunLog` markup safely: text nodes only, no `innerHTML`, `<color>` restricted to `#RRGGBB`. The settings dialog edits retry/continue-on-error/save-version-json/auto-disable. The file-location wizard sets per-solution paths through `selectPath` + `files/suggestFileNames`. A branch picker uses `git/branches`/`git/switchBranch`. CSV export uses `files/exportCsv`. `toolboxAPI.utils.preventClose` is held during a run.
**Approach (assumed at the header base):** `src/logMarkup.ts` is a pure parser of the item-6 markup into spans, with tests mirroring `MarkdownParserTests` cases.
**Regression guard.** Execute is enabled only under `SetExportButtonState`'s rules (≥1 solution checked, ≥1 action on, commit message ≥5 chars when commit is on, valid version format when update-version is on), and `AutoDisableExportButtons` is honoured. The settings dialog edits retry count and retry delay explicitly. The log's markup colours (`Color*` constants, e.g. #000077, #770000) map to theme-aware tokens so they stay readable on the PPTB dark theme.
**Files:** `pptb/src/logMarkup.ts`, `pptb/src/logMarkup.test.ts`, `pptb/src/components/RunPanel.tsx`, `pptb/src/components/SettingsDialog.tsx`, `pptb/src/components/FileWizard.tsx`, `pptb/src/components/GitBranchPicker.tsx`, `pptb/src/App.tsx`, `pptb/src/components/RunPanel.test.tsx`
**Read first:** `Bulk Solution Exporter/Helpers/MarkdownParser.cs — ProcessFormattedText, TruncateLine, MaxNestingDepth, _colorTokenRegex; Bulk Solution Exporter.Tests/MarkdownParserTests.cs; Bulk Solution Exporter/BulkSolutionExporter_PluginControl.cs — SetExportButtonState, Color* constants`
`Bulk Solution Exporter/SettingsForm.cs; @pptb/types toolboxAPI.d.ts — preventClose, releasePreventClose, selectPath`
**Tests:** the markup parser covers headings, nesting cap, colour, and a hostile `<script>` string rendered as text; one test that Execute stays disabled when commit is on and the message is shorter than 5 chars.
**Acceptance:** `cd pptb && npm test && npm run build && npm run validate` exit 0 (`npm run validate` needs network).
**Commit:** `feat(pptb): add run panel, log view, settings, file wizard and branch picker`
Depends on item 14.

## 16. Packaging scripts and docs

**What:**
**Goal:** One script per artifact builds a publishable package: `pptb/scripts/pack-worker.sh` (`dotnet pack` → `artifacts/worker`) and `npm pack` in `pptb/`, with a check that `pptb.config.json`'s `packageVersion` equals the worker .csproj `Version`. `README.md` gains a "Power Platform ToolBox" section (install, the .NET 8 runtime requirement, connection 0 = source, 1..n = targets). The capcom note `versioning-and-release` describes the worker/npm versions and the nuget-then-npm publish order. A new capcom note `pptb-architecture` covers core/worker/UI and the RPC method list.
**Regression guard.** Acceptance asserts the `npm pack --dry-run --json` listing contains `dist/index.html`, `pptb.config.json` and the icon; `package.json` `files` is set accordingly. The `versioning-and-release` note states the npm (3-part semver) and worker NuGet version schemes and amends its "GPL-3.0-only everywhere" line for the npm `GPL-3.0` exception. `check-worker-version.mjs` takes path arguments so the mismatch test can target a temp copy.
**Files:** `pptb/scripts/pack-worker.sh`, `pptb/scripts/check-worker-version.mjs`, `pptb/package.json`, `README.md`, `.capcom/knowledge/versioning-and-release.md`, `.capcom/knowledge/pptb-architecture.md`, `.gitignore`
**Read first:** `.capcom/knowledge/versioning-and-release.md — version format, licence line; README.md — Quick Start, License; .gitignore — artifacts/, node_modules/; pptb/package.json — files, scripts; pptb/pptb.config.json — workers.engine.packageVersion`
**Tests:** `check-worker-version.mjs`, given paths to a deliberately mismatched temp copy, fails.
**Acceptance:** `sh pptb/scripts/pack-worker.sh && cd pptb && node scripts/check-worker-version.mjs && npm pack --dry-run --json` exit 0 and the JSON file list contains `dist/index.html`, `pptb.config.json` and the icon; `capcom know show pptb-architecture` prints the note.
**Commit:** `docs(pptb): add packaging scripts and PPTB documentation`
Depends on items 11, 15.
