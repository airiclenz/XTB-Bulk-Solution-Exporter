# Robustness fixes and log escaping — plan

**Goal:** Fix the three P2 bugs (PPTB solution paging, version-file writer crash, versions over four parts), the git stdin broken pipe and the log truncation that splits tags, remove dead code, and escape Dataverse/git text before it reaches the log markup in every log renderer.
**Date:** 2026-11-18
**Status:** unexecuted
**sized for:** ~200k-context host
**base:** 2f166d0
**Closes:** log-truncation-splits-tags; escape-log-markdown

**Sources:**
- `.capcom/issues/` — the tickets on each item's `**Closes:**` line and the header's
- `.capcom/knowledge/untrusted-dataverse-data.md`, `.capcom/knowledge/build-and-test.md`, `.capcom/knowledge/code-style.md`
- `Bulk Solution Exporter.Core/Run/RunLog.cs` — the log mini-markup contract

**Ratified design calls:**
- **PPTB paging:** keyset paging on `uniquename` (`$orderby=uniquename`, `$top=5000`, next page `uniquename gt '<last>'`); `@odata.nextLink` is not used. (user, 2026-11-18)
- **Versions over four parts:** parts beyond the fourth are ignored; nothing throws. (user, 2026-11-18)
- **Log escaping:** in scope, as a backslash escape: `\` before any of backslash, `#`, `*`, backtick or `<` renders that character literally and it never acts as markup; any other backslash is literal. Untrusted text is escaped where it is concatenated into a log message. (user, 2026-11-18)
- **One C# owner of the markup rules:** a new static `LogMarkup` in `Bulk Solution Exporter.Core/Run/` owns line truncation and `Escape`; `RunLog` and the plugin's `MarkdownParser` call it. PPTB's `logMarkup.ts` mirrors it. (writer, 2026-11-18)

**Standing requirements:**
- skills: coding-standards
- Code style per `.capcom/knowledge/code-style.md` (tabs, Allman, method dividers, named constants).
- Plugin tests (`Bulk Solution Exporter.Tests`) run on Windows only; on macOS/Linux their Acceptance is the build plus the Windows command recorded as not run.
- Any authorized deviation from item text lands as a dated NOTES line under the item.

**Out of scope:**
- `pptb-solution-processed-async`, `pptb-per-solution-paths` and every P3/P4 feature ticket.
- Changing log colors, indent rendering or the truncation limit.

**Regression check (2026-11-18, 2f166d0):**
- 2: guard folded
- 5: guard folded; supersedes the Acceptance of `.capcom/issues/pptb-solutions-paging.md` (`@odata.nextLink`)
- 6: guard folded
- 7: guard folded
- 8: guard folded
- 9: guard folded; guard amended (decision: FormatSolutionName is the single escape point for solution names; RetryRunner/ImportOperationWaiter lambda clause dropped, owned by item 10)
- 10: recast; depends on item 9 (decision: never re-escapes FormatSolutionName output, owns the RetryRunner/ImportOperationWaiter lambdas); guard folded (over-limit FormatterTests case rewritten; supersedes the Formatter.cs and FormatterTests summaries' "returned unchanged" / size-guard wording)
- 11: guard folded

## 1. Ignore version parts beyond the fourth — ✅ DONE (2026-11-18)

**What:** Fix: `Version(string)` throws `IndexOutOfRangeException` on a source version with more than four parts (ticket `version-over-four-parts`).
**Goal:** `new Version("1.2.3.4.5")` does not throw; it holds parts 1, 2, 3, 4, `ToString()` returns the source string until the version is increased, and `IncreaseVersionNumber` works on it as on `"1.2.3.4"`.
**Approach (assumed at the header base):** in `Version(string)` (`Bulk Solution Exporter.Core/Version.cs`) bound the parse loop by the slot count of `_version` (a named constant, not a literal 4) and ignore the remaining parts.
**Files:** `Bulk Solution Exporter.Core/Version.cs`, `Bulk Solution Exporter.Core.Tests/VersionTests.cs`
**Read first:** `Bulk Solution Exporter.Core/Version.cs` — Version(string), IncreaseVersionNumber, UpdateVersionString, UnsetPart; `Bulk Solution Exporter.Core.Tests/VersionTests.cs` — IncreaseVersionNumber_Format_ProducesOnePartPerFormatPart;
`Bulk Solution Exporter.Core/Run/RunEngine.cs` — new Version(version.ToString()); `Bulk Solution Exporter.PptbWorker/VersionJsonConverter.cs` — ReadJson; `Bulk Solution Exporter/Schema/Solution.cs` — new Version(record version)
**Tests:** `VersionTests`: five- and six-part sources construct, keep `ToString()`, and increase with `"#.#.#.+"` to `"1.2.3.5"`.
**Acceptance:**
- `dotnet test "Bulk Solution Exporter.Core.Tests" --filter "FullyQualifiedName~VersionTests"`
**Closes:** version-over-four-parts
**Commit:** `fix(core): ignore version parts beyond the fourth instead of throwing`

## 2. Version-file writer survives a malformed exported_versions.json — ✅ DONE (2026-11-18)

NOTES (2026-11-18): folded in the interrupted attempt's uncommitted `ExportedSolutionVersions.TryParse(json, out record, out isRepaired)` as the one shared parse path (kept as written); the reader uses it after its own IO catch.
NOTES (2026-11-18): the writer's read and warning live in a new private instance method `RunEngine.LoadVersionRecord`; `SaveVersionJson` became an instance method calling it. IO/access errors on the read still propagate, as the regression guard requires.
NOTES (2026-11-18): removed `FindMatch`'s null-list and null-entry guards, unreachable now that `TryParse` normalizes both.
NOTES (2026-11-18): the new warning concatenates the version file's full path into the log line unescaped; item 9 (escape untrusted text in RunEngine.cs log lines) should cover it.

**What:** Fix: the run engine's version-file writer throws on a malformed `exported_versions.json` and faults the export step (ticket `version-json-writer-crash`).
**Regression guard.** The shared parse method takes the JSON text, not a path. In `SaveVersionJson` only JSON-level failures (`JsonException`, a `null` root) start a fresh record; IO and access errors on the read keep today's behaviour there (a fresh record would let `WriteAllText` wipe every other solution's entries), and `TryLoadExportedSolution` keeps its own IO catch. The parse method also drops `null` elements from both lists.
**Goal:** An export with "save version JSON" on, over an `exported_versions.json` that is malformed JSON, the literal `null`, or has `null` lists, succeeds: the writer logs one warning naming the file, starts a fresh record (or keeps the readable lists) and writes the new entry.
**Approach (assumed at the header base):** extend `ExportedSolutionVersions` (`Bulk Solution Exporter.Core/Schema/ExportedSolutionVersions.cs`) with one parse method that returns false on unreadable JSON and normalizes `null` lists to empty; `TryLoadExportedSolution` and `RunEngine.SaveVersionJson` (`Bulk Solution Exporter.Core/Run/RunEngine.cs`) both call it — no second deserialization path. `SaveVersionJson` becomes an instance method (or takes the `RunLog`) so it can log the warning via `_runLog`.
**Files:** `Bulk Solution Exporter.Core/Schema/ExportedSolutionVersions.cs`, `Bulk Solution Exporter.Core/Run/RunEngine.cs`, `Bulk Solution Exporter.Core.Tests/RunEngineExportTests.cs`, `Bulk Solution Exporter.Core.Tests/ExportedSolutionVersionsTests.cs`
**Read first:** `Bulk Solution Exporter.Core/Run/RunEngine.cs` — SaveVersionJson, ExportToFile; `Bulk Solution Exporter.Core/Schema/ExportedSolutionVersions.cs` — TryLoadExportedSolution, FindMatch;
`Bulk Solution Exporter.Core/Run/RunLog.cs` — Log, ColorOrange (there is no LogWarning); `Bulk Solution Exporter.Core.Tests/RunEngineExportTests.cs` — Run_SaveVersionJsonOn_WritesTheExportedVersion;
`Bulk Solution Exporter.Core.Tests/ExportedSolutionVersionsTests.cs` — TryLoadExportedSolution_InvalidJson_ReturnsFalse
**Tests:** `RunEngineExportTests`: truncated JSON, `null`, `{"Managed":null}` and `{"Managed":[null]}` — export succeeds, a warning line is recorded, the file then holds the new entry. `ExportedSolutionVersionsTests`: the parse method on the same inputs.
**Acceptance:**
- `dotnet test "Bulk Solution Exporter.Core.Tests" --filter "FullyQualifiedName~RunEngineExportTests|FullyQualifiedName~ExportedSolutionVersionsTests"`
**Closes:** version-json-writer-crash
**Commit:** `fix(core): start a fresh version record when exported_versions.json is unreadable`

## 3. Git stdin write failure yields a failed result

**What:** Fix: `GitHelper.WriteStandardInput` lets an `IOException` (broken pipe) escape when git exits before reading its stdin (ticket `git-stdin-ioexception`).
**Goal:** `GitHelper.RunProcess` never throws `IOException` from writing standard input; when the write fails it returns a non-zero exit code (the process's own when non-zero, else a named failure constant) and `standardError` carries a line saying the input could not be written.
**Approach (assumed at the header base):** in `WriteStandardInput` (`Bulk Solution Exporter.Core/IO/GitHelper.cs`) catch `IOException` around the write/flush, still close the input in `finally` (a close that throws `IOException` is also caught), report the failure to `RunProcess`, which applies it after `WaitForExit` and the output reads.
**Files:** `Bulk Solution Exporter.Core/IO/GitHelper.cs`, `Bulk Solution Exporter.Core.Tests/GitHelperTests.cs`
**Read first:** `Bulk Solution Exporter.Core/IO/GitHelper.cs` — RunProcess, WriteStandardInput, ExecuteCommand, Commit (the only stdin caller, `commit -F -`);
`Bulk Solution Exporter.Core.Tests/GitHelperTests.cs` — RunProcess_NoStandardInput_ChildReadsEndOfInput, Commit_MessageWithQuotesOrNonAscii_IsStoredVerbatim, RunTimeoutInMilliseconds;
`Bulk Solution Exporter.PptbWorker.Tests/WorkspaceMethodsTests.cs` — RunGit (RunProcess caller)
**Tests:** `GitHelperTests`: run `git --version` with a standard input of several MB (larger than any pipe buffer) — no exception, a non-zero result, the stderr line present; the existing stdin tests still pass.
**Acceptance:**
- `dotnet test "Bulk Solution Exporter.Core.Tests" --filter "FullyQualifiedName~GitHelperTests"`
**Closes:** git-stdin-ioexception
**Commit:** `fix(core): report a git stdin write failure as a failed result`

## 4. Remove the dead RetryRunner.IsRunAborted

**What:** Delete `RetryRunner.IsRunAborted` (`Bulk Solution Exporter.Core/Run/RetryRunner.cs`), its test `IsRunAborted_CancelledOrLatchSet_ReportsAbort` and the `<see cref>` to it in the `RetryRunnerTests` class summary (reword that sentence so it stays true).
**Goal:** No symbol `IsRunAborted` exists in the repo's C# sources; the solution builds and the core tests pass.
**Approach (assumed at the header base):** plain deletion; it has no production caller.
**Files:** `Bulk Solution Exporter.Core/Run/RetryRunner.cs`, `Bulk Solution Exporter.Core.Tests/RetryRunnerTests.cs`
**Read first:** `Bulk Solution Exporter.Core/Run/RetryRunner.cs` — IsRunAborted; `Bulk Solution Exporter.Core.Tests/RetryRunnerTests.cs` — class summary cref, IsRunAborted_CancelledOrLatchSet_ReportsAbort; `.capcom/issues/remove-isrunaborted.md`
**Tests:** none new.
**Acceptance:**
- `grep -rn "IsRunAborted" --include='*.cs' .` prints nothing
- `dotnet build "Bulk Solution Exporter.sln" -c Debug`
- `dotnet test "Bulk Solution Exporter.Core.Tests" --filter "FullyQualifiedName~RetryRunnerTests"`
**Closes:** remove-isrunaborted
**Commit:** `refactor(core): remove the unused RetryRunner.IsRunAborted`

## 5. PPTB retrieveSolutions returns every page

**What:** Fix: the PPTB `dataverse/retrieveSolutions` handler reads only the first page of solutions (ticket `pptb-solutions-paging`). Keyset paging per the ratified call.
**Regression guard.** The two existing filter tests pin the full query string with `toHaveBeenCalledWith`, so they are updated to the new string (filter, then `$orderby=uniquename`, then `$top=5000`, in that order). The ratified keyset call supersedes the Acceptance of `.capcom/issues/pptb-solutions-paging.md` ("follows `@odata.nextLink`"): amend that Acceptance, or `capcom log` the substitution on the ticket, before closing it.
**Goal:** `retrieveSolutions` returns every visible solution matching the managed filter, however many pages the environment holds; each query carries `$orderby=uniquename` and `$top` of the page size constant (5000); a page shorter than the page size ends the loop; the next page's filter adds `uniquename gt <last uniquename as an OData literal>`.
**Approach (assumed at the header base):** in `createDataverseBridge`'s `[DataverseMethod.RetrieveSolutions]` handler (`pptb/src/dataverseBridge.ts`) loop over `window.dataverseAPI.queryData` through `callDataverse`, building the literal with `toODataLiteral`; map rows with `toSolutionRecord` as now.
**Files:** `pptb/src/dataverseBridge.ts`, `pptb/src/dataverseBridge.test.ts`, `.capcom/issues/pptb-solutions-paging.md`
**Read first:** `pptb/src/dataverseBridge.ts` — createDataverseBridge [DataverseMethod.RetrieveSolutions], callDataverse, toODataLiteral, toSolutionRecord; `pptb/src/dataverseBridge.test.ts` — describe("retrieveSolutions");
`pptb/src/App.tsx` — retrieveSolutions (re-sorts source by friendlyName); `Bulk Solution Exporter.PptbWorker/RpcDataverseGateway.cs` — RetrieveSolutions; `.capcom/issues/pptb-solutions-paging.md` — Acceptance
**Tests:** `dataverseBridge.test.ts` `retrieveSolutions`: a two-page answer (first page full at the page size, second short) returns all rows and the second query carries the `uniquename gt` filter with a quote-escaped name; a single short page issues one query; the two existing filter tests are updated to the new query string (filter + `$orderby=uniquename` + `$top=5000`, in that order).
**Acceptance:**
- `cd pptb && npx vitest run src/dataverseBridge.test.ts`
- `cd pptb && npx tsc --noEmit`
**Closes:** pptb-solutions-paging
**Commit:** `fix(pptb): page through every solution in retrieveSolutions`

## 6. Core LogMarkup: tag-safe truncation and Escape

**What:** Fix (part of `log-truncation-splits-tags`): `RunLog`'s line truncation can cut inside a `<color=#RRGGBB>` or `</color>` tag. Adds the ratified backslash escape to the markup contract. New `public static class LogMarkup` in `Bulk Solution Exporter.Core/Run/LogMarkup.cs` owns `public const` `MaxLineLength` and `TruncationMarker`, `TruncateLine(string)` and `Escape(string)`; `RunLog` delegates to it and keeps its public constants defined from `LogMarkup`'s. `RunLog`'s contract summary documents the escape.
**Regression guard.** An escaped end tag `\</color>` is no end tag for the unclosed-token check, just as an escaped `\<color=…>` is no token. `LogMarkup` and its constants are public because the plugin assembly calls them and Core declares no `InternalsVisibleTo`.
**Goal:** `LogMarkup.TruncateLine` never leaves a partial color token or end tag before the marker (it cuts before it), never ends the kept text on a backslash that escapes the cut-off character, and closes a still-open unescaped color token; an escaped `\<color=…>` counts as no token. `LogMarkup.Escape` prefixes each backslash, `#`, `*`, backtick or `<` with `\` and leaves every other character unchanged; `null` returns empty.
**Approach (assumed at the header base):** move `TruncateLine`/`HasUnclosedColorToken` from `RunLog` (`Bulk Solution Exporter.Core/Run/RunLog.cs`) into `LogMarkup` and extend them; `RunLog.TruncateLines` calls `LogMarkup.TruncateLine`.
**Files:** `Bulk Solution Exporter.Core/Run/LogMarkup.cs`, `Bulk Solution Exporter.Core/Run/RunLog.cs`, `Bulk Solution Exporter.Core.Tests/LogMarkupTests.cs`, `Bulk Solution Exporter.Core.Tests/RunLogTests.cs`
**Read first:** `Bulk Solution Exporter.Core/Run/RunLog.cs` — TruncateLines, TruncateLine, HasUnclosedColorToken, ColorTokenRegex;
`Bulk Solution Exporter.Core.Tests/RunLogTests.cs` — LogError_LineLongerThanCap_ClosesColorBeforeMarker, Log_OneLineLongerThanCap_CutsOnlyThatLine; `Bulk Solution Exporter/Helpers/MarkdownParser.cs` — TruncateLine, HasUnclosedColorToken
**Tests:** `LogMarkupTests`: cut lands inside `<color=#12`, inside `</col`, right after a lone `\`; open token closed; escaped token not closed; a token followed only by an escaped `\</color>` is still closed; `Escape` on each special character, on `##### Done.`, on `null`. `RunLogTests` existing truncation tests still pass.
**Acceptance:**
- `dotnet test "Bulk Solution Exporter.Core.Tests" --filter "FullyQualifiedName~LogMarkupTests|FullyQualifiedName~RunLogTests"`
**Commit:** `fix(core): truncate log lines without splitting tags; add the markup escape`

## 7. Plugin MarkdownParser: shared truncation and escape rendering

Depends on item 6.

**What:** Fix (part of `log-truncation-splits-tags`): `MarkdownParser.TruncateLine` (`Bulk Solution Exporter/Helpers/MarkdownParser.cs`) has the same tag-splitting cut. Teach the parser the ratified escape.
**Regression guard.** Every end-marker search (closing `**`, `*`, backtick, `</color>`) skips escaped characters, so escaped text wrapped by RunLog.Italic/Bold/Code/Colorize renders literally inside its marker; test `**a\*b**` and `<color=#112233>a\</color>b</color>`.
**Goal:** The plugin parser truncates through `LogMarkup.TruncateLine` (its `MaxLogLineLength`/`TruncationMarker` constants defined from `LogMarkup`'s), and renders `\x` for each escapable `x` as the literal `x`: `\#\#\#\#\# Done.` at line start is plain text, not a heading; `\*a\*` is not italic; `\<color=#FF0000>x</color>` is not colored.
**Approach (assumed at the header base):** delete the parser's own `TruncateLine`/`HasUnclosedColorToken`; in the marker scan (`FindNextMarker` and the heading check in the parse method) treat a backslash before an escapable character as a literal pair that emits only the character.
**Files:** `Bulk Solution Exporter/Helpers/MarkdownParser.cs`, `Bulk Solution Exporter.Tests/MarkdownParserTests.cs`
**Read first:** `Bulk Solution Exporter/Helpers/MarkdownParser.cs` — ParseAndAppend, ProcessFormattedText, FindNextMarker, FindMatchingEnd;
`Bulk Solution Exporter.Tests/MarkdownParserTests.cs` — RenderOnSmallStackThread, ParseAndAppend_OverLimitColoredLine_HidesTheColorToken; `Bulk Solution Exporter.Core/Run/RunLog.cs` — Colorize, Italic
**Tests:** `MarkdownParserTests`: the three escape cases above, a lone trailing backslash renders literally, a huge line cut inside a tag shows no partial tag, `**a\*b**` renders bold "a*b", `<color=#112233>a\</color>b</color>` renders colored "a</color>b".
**Acceptance:**
- `dotnet build "Bulk Solution Exporter.sln" -c Debug`
- Windows: `dotnet test "Bulk Solution Exporter.Tests" --filter "FullyQualifiedName~MarkdownParserTests"`
**Commit:** `fix(log): share tag-safe truncation and render escapes in the plugin log`

## 8. PPTB logMarkup.ts: tag-safe truncation and escape rendering

Depends on item 6 (mirrors its rules).

**What:** Fix (part of `log-truncation-splits-tags`): `truncateLine` in `pptb/src/logMarkup.ts` has the same tag-splitting cut. Mirror item 6's truncation rules and item 7's escape rendering; update the module's header comment to list the escape.
**Regression guard.** Mirror item 7's decision in logMarkup.ts: every end-marker search skips escaped characters, with the same two tests. The module exports an `escape` function mirroring `LogMarkup.Escape`; `formatError` in `pptb/src/components/RunPanel.tsx` passes `message` through it, so a worker error naming `\\server\share` keeps both backslashes.
**Goal:** `parseLogMarkup` (the module's exported parser) never shows a partial tag after truncation, closes an open unescaped token, never ends kept text on a dangling escape, and renders each escaped character literally with no markup effect, exactly as items 6 and 7 specify.
**Approach (assumed at the header base):** extend `truncateLine`/`hasUnclosedColorToken` and the marker scan in `pptb/src/logMarkup.ts`.
**Files:** `pptb/src/logMarkup.ts`, `pptb/src/logMarkup.test.ts`, `pptb/src/components/RunPanel.tsx`, `pptb/src/components/RunPanel.test.tsx`
**Read first:** `pptb/src/logMarkup.ts` — parseLogMarkup, truncateLine, hasUnclosedColorToken, findNextMarker, findMatchingEnd; `pptb/src/logMarkup.test.ts` — describe("truncation");
`pptb/src/components/RunPanel.tsx` — formatError; `pptb/src/components/RunPanel.test.tsx` — describe("log")
**Tests:** `logMarkup.test.ts`: truncation through `parseLogMarkup` on lines over `MAX_LOG_LINE_LENGTH` (`truncateLine` is not exported) — cut inside `<color=#12`, inside `</col`, right after a lone `\`; open token closed; escaped token not closed; a token followed only by an escaped `\</color>` is still closed; item 7's rendering cases (the three escape cases, a lone trailing backslash, `**a\*b**`, `<color=#112233>a\</color>b</color>`); `escape` on each special character and on `##### Done.`. `RunPanel.test.tsx` `describe("log")`: a worker error naming `\\server\share` renders both backslashes.
**Acceptance:**
- `cd pptb && npx vitest run src/logMarkup.test.ts src/components/RunPanel.test.tsx`
- `cd pptb && npx tsc --noEmit`
**Commit:** `fix(pptb): tag-safe log truncation and escape rendering`

## 9. Escape untrusted text in RunEngine.cs log lines

Depends on item 6.

**What:** Part of `escape-log-markdown`. Rule: every `Log`/`LogError`/`LogDebug` argument in `Bulk Solution Exporter.Core/Run/RunEngine.cs` that includes a non-literal string — solution unique/friendly name, connection name, file or folder path, exception or fault message, Dataverse-returned text — passes that value through `LogMarkup.Escape` before it is concatenated or wrapped by `RunLog.Colorize`/`Bold`/`Code`/`Heading`. Numbers and `FormatDuration` output are exempt. Sites: `grep -n "Log\(Error\|Debug\)\?(" "Bulk Solution Exporter.Core/Run/RunEngine.cs"`.
**Regression guard.** Escape happens exactly once per value. FormatSolutionName (RunEngine.cs) escapes the names inside it, so its output is the single escape point for those names wherever it is used (including RunEngine.Import.cs). No RetryRunner or ImportOperationWaiter callback is created in RunEngine.cs; item 10 owns them.
**Goal:** An export run of a solution whose unique and friendly name are `##### Done.` records no log line containing the unescaped `##### Done.`, and every line naming it contains `\#\#\#\#\# Done.`.
**Approach (assumed at the header base):** wrap the values at each call site; no change to `RunLog.Log`'s signature.
**Files:** `Bulk Solution Exporter.Core/Run/RunEngine.cs`, `Bulk Solution Exporter.Core.Tests/RunEngineExportTests.cs`
**Read first:** `Bulk Solution Exporter.Core/Run/RunEngine.cs` — FormatSolutionName, PublishAll, UpdateVersionNumbers, ExportSolutions, ExportToFile;
`Bulk Solution Exporter.Core.Tests/RunEngineExportTests.cs` — RecordingRunSink.Lines, CreateExportRequest; `Bulk Solution Exporter.Core/Run/RunEngine.Import.cs` — ImportSolutions (second FormatSolutionName caller)
**Tests:** `RunEngineExportTests`: the `##### Done.` solution case above, via `RecordingRunSink`.
**Acceptance:**
- `dotnet test "Bulk Solution Exporter.Core.Tests" --filter "FullyQualifiedName~RunEngineExportTests"`
**Commit:** `fix(core): escape Dataverse text in export log lines`

## 10. Escape untrusted text in import and git log lines

Depends on items 6 and 9.

**What:** Recast at the regression check (2026-11-18). Part of `escape-log-markdown`. Item 9's rule, applied to `Bulk Solution Exporter.Core/Run/RunEngine.Import.cs` (adds import result/error XML and import operation messages) and `Bulk Solution Exporter.Core/Run/RunEngine.Git.cs` (adds branch names, git stdout/stderr, commit messages). Sites: the same grep on both files.
**Regression guard.** Scope widens to every untrusted-text path into the log under Bulk Solution Exporter.Core/Run/ not owned by item 9, including RetryRunner.cs and ImportOperationWaiter.cs exception messages (reviewer 2 note). Binding: (a) add Bulk Solution Exporter.Core/Formatter.cs and Bulk Solution Exporter.Core.Tests/FormatterTests.cs to Files; FormatErrorStringWithXml escapes on every return path, after the XML reformat and before currentIndent is inserted; test that the pretty-printed XML lines and the indent bar survive. (b) Escape exactly once: RetryRunner/ImportOperationWaiter text is escaped whole at the callback lambdas in RunEngine.Import.cs; FormatSolutionName output and operationDescription are not re-escaped. (c) Keep the git clause in the Goal: add Bulk Solution Exporter.Core.Tests/Fakes/FakeGitClient.cs to Files with an error-message injection property, and a RunEngineImportTests git-failure test asserting the injected `# x` text is logged escaped. (d) Depends on item 9 (FormatSolutionName escapes inside itself); item 10 never re-escapes FormatSolutionName output, and owns the RetryRunner/ImportOperationWaiter callback lambdas in RunEngine.Import.cs. (e) The over-limit path is escaped too: rewrite and rename `FormatterTests.FormatErrorStringWithXml_OverLimitMessage_ReturnsItUnchanged` to expect `LogMarkup.Escape(errorString)` (still no reformat, no inserted newline); this supersedes the `Formatter.cs` `FormatErrorStringWithXml` summary ("returned unchanged") and the `FormatterTests` class summary ("Covers the size guard"), both updated to match.
**Goal:** An import run of a `##### Done.` solution whose import fails with an error message containing `**x**` records neither unescaped string; a git step whose output contains `# x` records it escaped.
**Approach (assumed at the header base):** wrap the values at each call site.
**Files:** `Bulk Solution Exporter.Core/Run/RunEngine.Import.cs`, `Bulk Solution Exporter.Core/Run/RunEngine.Git.cs`, `Bulk Solution Exporter.Core/Formatter.cs`, `Bulk Solution Exporter.Core.Tests/RunEngineImportTests.cs`, `Bulk Solution Exporter.Core.Tests/FormatterTests.cs`, `Bulk Solution Exporter.Core.Tests/Fakes/FakeGitClient.cs`
**Read first:** `Bulk Solution Exporter.Core/Run/RunEngine.Import.cs` — ExecuteWithRetries, InstallSolution, ImportSolution, ApplyPendingUpgrade, DoesSolutionExist, HandleTargets; `Bulk Solution Exporter.Core/Formatter.cs` — FormatErrorStringWithXml; `Bulk Solution Exporter.Core.Tests/FormatterTests.cs` — FormatErrorStringWithXml_OverLimitMessage_ReturnsItUnchanged;
`Bulk Solution Exporter.Core/Run/RunEngine.Git.cs` — CommitSessionFiles, PushCommit; `Bulk Solution Exporter.Core.Tests/Fakes/FakeGitClient.cs` — Commit, AddFile, Push; `Bulk Solution Exporter.Core.Tests/RunEngineImportTests.cs` — Run_CommitAndPush_UsesTheRepositoryOfTheFirstConfiguredFilePath, Run_ImportOperationFails_LogsTheServerMessageAndFailsTheSolution, CreateRepository, CreateFailedState;
`Bulk Solution Exporter.Core/Run/RetryRunner.cs` — Execute; `Bulk Solution Exporter.Core/Run/RunEngine.cs` — FormatSolutionName
**Tests:** `RunEngineImportTests`: the import case above; a git-failure case where `FakeGitClient`'s injected error message containing `# x` is logged escaped. `FormatterTests`: `FormatErrorStringWithXml` escapes on every return path; the pretty-printed XML lines and the indent bar survive; the over-limit case (renamed from `FormatErrorStringWithXml_OverLimitMessage_ReturnsItUnchanged`) expects `LogMarkup.Escape(errorString)` with no reformat and no inserted newline.
**Acceptance:**
- `dotnet test "Bulk Solution Exporter.Core.Tests" --filter "FullyQualifiedName~RunEngineImportTests|FullyQualifiedName~FormatterTests"`
**Commit:** `fix(core): escape Dataverse and git text in import and git log lines`

## 11. Escape untrusted text in the plugin's own log lines

Depends on item 6.

**What:** Part of `escape-log-markdown`. Item 9's rule, applied to every `Log(`, `LogError(` and `LogDebug(` call in `Bulk Solution Exporter/BulkSolutionExporter_PluginControl.cs` (sites: `grep -n "Log\(Error\|Debug\)\?(" "Bulk Solution Exporter/BulkSolutionExporter_PluginControl.cs"`). Update `.capcom/knowledge/untrusted-dataverse-data.md`: replace the "Escaping of untrusted text is still open" sentence with the rule (untrusted text goes through `LogMarkup.Escape`; both renderers honour the backslash escape).
**Regression guard.** The rule covers the `LogError`/`LogDebug` callers too (exception messages, paths, branch names). The forwarding wrappers `Log`, `LogDebug`, `LogError`, `AppendLogLine` and `RenderLogLine` are exempt — escaping there would escape all markup, every engine line and the `##### Done.` line; escape only where a caller concatenates the untrusted value.
**Goal:** No plugin log call concatenates a non-literal string without `LogMarkup.Escape`; the knowledge note states the escape rule and no longer calls it open.
**Approach (assumed at the header base):** wrap the values at each call site.
**Files:** `Bulk Solution Exporter/BulkSolutionExporter_PluginControl.cs`, `.capcom/knowledge/untrusted-dataverse-data.md`
**Read first:** `Bulk Solution Exporter/BulkSolutionExporter_PluginControl.cs` — Log, LogError, LogDebug, RenderLogLine, comboBox_gitBranches_SelectedIndexChanged, UpdateGitBranches;
`.capcom/knowledge/untrusted-dataverse-data.md` — the "Escaping of untrusted text is still open" bullet; `Bulk Solution Exporter.Core/IO/FileAndFolderHelper.cs` — FindRepositoryRootPath (message text)
**Tests:** none new (UI class, no harness); covered by the build.
**Acceptance:**
- `dotnet build "Bulk Solution Exporter.sln" -c Debug`
- `capcom check`
- `grep -n "still open" .capcom/knowledge/untrusted-dataverse-data.md` prints nothing
**Commit:** `fix(log): escape Dataverse text in the plugin's log lines`
