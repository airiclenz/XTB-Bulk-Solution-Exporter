# Async solution import — StageAndUpgradeAsync, ImportSolutionAsync and pending-upgrade apply

**Goal:** Every solution import runs as one server-side async operation that the run polls to its end, so a client timeout or retry can no longer leave an upgrade stranded as a holding solution or start a duplicate import. An upgrade that finds a pending `<name>_Upgrade` applies it first, then installs this run's file.
**Date:** 2026-11-18
**Status:** unexecuted
**sized for:** ~200k-context host
**base:** 51b069f
**Closes:** import-async-upgrade

**Sources:**
- capcom issues `import-async-upgrade`, `pending-upgrade-apply`
- capcom knowledge: `run-and-abort-rules`, `pptb-architecture`, `untrusted-dataverse-data`, `code-style`, `build-and-test`
- Microsoft Learn Web API reference: `StageAndUpgradeAsync` action, `ImportSolutionAsync` action, `asyncoperation` table (statecode 3 = Completed; statuscode 30 Succeeded, 31 Failed, 32 Canceled)

**Ratified design calls (airic, 2026-11-18):**
- **Upgrade call:** a managed import of an installed solution with Upgrade on is one `StageAndUpgradeAsync` call; no client-side `DeleteAndPromote` follows a fresh install.
- **Plain imports:** every other import (managed update, unmanaged) is `ImportSolutionAsync`.
- **Wait:** the run polls the async operation until it reaches a final state, with no cap on the wait. Abort takes effect after the operation ends ("abort between steps" holds).
- **Pending upgrade:** an existing `<name>_Upgrade` is applied (`DeleteAndPromote`, with a warning in the log), then this run's file is installed with `StageAndUpgradeAsync`.

**Standing requirements:**
- skills: coding-standards
- C# follows capcom `code-style` (tabs, Allman, `// ===` method dividers, named constants); new core members the hosts or tests call are `public`.
- Never edit `XTB Components/` (submodule).
- `untrusted-dataverse-data` holds: operation ids and messages from Dataverse are untrusted text.
- RPC method names change on both sides together (`RpcDataverseGateway.cs` and `dataverseBridge.ts`).
- Live imports against a real environment are checked manually by the user; items prove behaviour with builds and tests.

**Out of scope:** cancelling a running server operation on abort; a cap on the wait; changes to export, Publish All or version update; version bumps or a release.

**Regression check (2026-11-18, 51b069f):**
- 2: guard folded (decision: `ImportFaultMessage` moves onto `startImport`; Acceptance grep for both handlers)
- 4: guard folded (`ImportedNames` prefix, worker fault test, poll-fault queue, public poll-sleep seam, retry-count helper); Goal sentence and Acceptance grep replaced by decision; supersedes the holding-install-plus-apply-upgrade step rule (`run-and-abort-rules.md:11`)
- 5: guard folded (decision: `FakeToolHost.ImportFaultMessage` stays; quoted, widened grep; `startImport` rejection case and `tsc`; holding-comment rule); supersedes the `RunEngine.cs:31-32` class doc
- 6: guard folded (`FailDeleteAndPromoteFor` fault switch; Note rule covers every `_Upgrade`/apply-upgrade line)

## 1. Add the async-import members to the gateway contract and both host gateways — ✅ DONE (2026-11-18)

NOTES (2026-11-18): `ImportOperationStatus` lives in `ImportOperationState.cs` beside the state class (the plan lists no separate file for it), as `ImportOptions` shares `IDataverseGateway.cs`.
NOTES (2026-11-18): untrusted answers fail fast: `OrganizationServiceGateway.StartImport` throws `InvalidOperationException` when the response has no Guid `AsyncOperationId`; `RpcDataverseGateway.StartImport` throws `InvalidOperationException` on a null/empty id (then `Guid.Parse`, `FormatException` on a non-GUID); `RpcDataverseGateway.GetImportOperationState` throws `InvalidOperationException` on a null answer.
NOTES (2026-11-18): the new reverse calls `dataverse/startImport` and `dataverse/getImportOperationState` are worker-side only here; the bridge handlers and the `FakeToolHost` side are item 2's scope.

**What:**
**Goal:** `IDataverseGateway` has `Guid StartImport(byte[] solutionFile, ImportOptions options)` and `ImportOperationState GetImportOperationState(Guid asyncOperationId)`; `ImportOptions` has `IsUpgrade`; the XrmToolBox gateway and the worker's RPC gateway implement both members; every gateway fake compiles.
**Approach (assumed at the header base):** In `Bulk Solution Exporter.Core/Run/`, add `ImportOperationState` (new file) holding `ImportOperationStatus Status` (`Running`, `Succeeded`, `Failed`, `Canceled`) and `string Message`. Add the two members to `IDataverseGateway` beside `ImportSolution` (which stays until item 5) and `bool IsUpgrade` to `ImportOptions` (true = `StageAndUpgradeAsync`, false = `ImportSolutionAsync`).
- `OrganizationServiceGateway`: build `new OrganizationRequest("StageAndUpgradeAsync")` or `("ImportSolutionAsync")` by name (the referenced SDK has no typed async classes). Parameters `CustomizationFile`, `PublishWorkflows`, `OverwriteUnmanagedCustomizations`, `ConvertToManaged`, `ImportJobId = Guid.NewGuid()`. Read `AsyncOperationId` from `response.Results`. `GetImportOperationState` retrieves `asyncoperation` columns `statecode`, `statuscode`, `message`, `friendlymessage` through `Translate`. It maps statecode 3 + statuscode 30/31/32 to Succeeded/Failed/Canceled, anything else to Running. `Message` is `friendlymessage` and `message` joined by a newline, empty parts skipped.
- `RpcDataverseGateway`: new reverse calls `dataverse/startImport` (same parameters as `dataverse/importSolution`, `isUpgrade` replacing `isHolding`, answering the operation id as a string parsed with `Guid.Parse`) and `dataverse/getImportOperationState` (`asyncOperationId` as a string, answering `{ status, message }` with status as the enum name).
- `FakeDataverseGateway`: `StartImport` records `StartImport:<file text>`, stores the options in `ImportedOptions`, and dequeues `ImportFaultMessages` exactly as `ImportSolution` does. A scriptable `Queue<ImportOperationState> OperationStates` (empty means Succeeded) answers `GetImportOperationState` and records `GetImportOperationState`. `SmokeTests`' stub gateway throws `NotSupportedException` as its siblings do.
**Files:** `Bulk Solution Exporter.Core/Run/IDataverseGateway.cs`, `Bulk Solution Exporter.Core/Run/ImportOperationState.cs`, `Bulk Solution Exporter/Run/OrganizationServiceGateway.cs`, `Bulk Solution Exporter.PptbWorker/RpcDataverseGateway.cs`, `Bulk Solution Exporter.Core.Tests/Fakes/FakeDataverseGateway.cs`, `Bulk Solution Exporter.Tests/SmokeTests.cs`
**Read first:** Bulk Solution Exporter.Core/Run/IDataverseGateway.cs — IDataverseGateway, ImportOptions; Bulk Solution Exporter.Core.Tests/Fakes/FakeDataverseGateway.cs — ImportSolution, ImportFaultMessages;
Bulk Solution Exporter/Run/OrganizationServiceGateway.cs — ImportSolution, Translate; Bulk Solution Exporter.PptbWorker/RpcDataverseGateway.cs — ImportSolution; Bulk Solution Exporter.Tests/SmokeTests.cs — VersionRecordingGateway
**Tests:** builds only; behaviour is tested in items 2–4.
**Acceptance:**
- `dotnet build "Bulk Solution Exporter.sln" -c Debug` exits 0.
- `dotnet test "Bulk Solution Exporter.Core.Tests"` exits 0.
**Commit:** `feat(core): add async import start and operation state to the gateway`

## 2. Answer the async-import reverse calls in the PPTB bridge

Depends on item 1.

**What:**
**Goal:** `dataverseBridge.ts` answers `dataverse/startImport` and `dataverse/getImportOperationState`; `FakeToolHost` answers both for worker tests; vitest covers both handlers.
**Approach (assumed at the header base):** Add both names to `DataverseMethod`. `startImport` executes `StageAndUpgradeAsync` when `isUpgrade` is true, else `ImportSolutionAsync`, with the parameters of the existing `ImportSolution` handler minus `HoldingSolution`, and returns the response's `AsyncOperationId` as a string. A missing or non-string id rejects with a message of its own. `getImportOperationState` validates `asyncOperationId` as a GUID (untrusted; reject otherwise) and runs `queryData("asyncoperations?$select=statecode,statuscode,message,friendlymessage&$filter=asyncoperationid eq <id>", slot)`. It maps statecode/statuscode as item 1 does and returns `{ status, message }`; no row rejects with "The import operation <id> was not found.". Messages pass through `callDataverse` unchanged. `FakeToolHost` gets `[JsonRpcMethod]` handlers for both, recording `startImport:<target>` and `getImportOperationState:<target>`, with a scriptable state queue (empty means `Succeeded`).
**Regression guard.** FakeToolHost's existing ImportFaultMessage moves onto the new startImport handler (startImport rejects with it when set), so worker tests keep a scripted import fault. Because the build passes without either fake handler and no worker test calls them before item 4, Acceptance checks that both `FakeToolHost` handlers exist.
**Files:** `pptb/src/dataverseBridge.ts`, `pptb/src/dataverseBridge.test.ts`, `Bulk Solution Exporter.PptbWorker.Tests/Fakes/FakeToolHost.cs`
**Read first:** pptb/src/dataverseBridge.ts — DataverseMethod, executeAction, callDataverse; pptb/src/dataverseBridge.test.ts — "answers every worker reverse call", importSolution describe;
Bulk Solution Exporter.PptbWorker.Tests/Fakes/FakeToolHost.cs — ImportSolution, ImportFaultMessage; pptb/src/workerClient.ts — WorkerClient connect (requests loop over DataverseMethod)
**Tests:** vitest: startImport upgrade → `StageAndUpgradeAsync`; plain → `ImportSolutionAsync`; returns the id; getImportOperationState maps 3/30, 3/31 (message kept), 0/0 → Running, rejects a non-GUID id, rejects on no row.
**Acceptance:**
- `cd pptb && npm test` exits 0; `cd pptb && npx tsc --noEmit` exits 0.
- `dotnet build "Bulk Solution Exporter.PptbWorker.Tests"` exits 0.
- `grep -c 'JsonRpcMethod("dataverse/\(startImport\|getImportOperationState\)")' "Bulk Solution Exporter.PptbWorker.Tests/Fakes/FakeToolHost.cs"` prints 2.
**Commit:** `feat(pptb): answer async import start and operation state reverse calls`

## 3. Add the import-operation waiter to core

Depends on item 1.

**What:**
**Goal:** Core has `ImportOperationWaiter`. It polls `GetImportOperationState` until the operation is final, returns on Succeeded, and throws `DataverseOperationException` carrying the server message on Failed/Canceled. Consecutive poll failures beyond the given limit throw a fault whose text `RetryRunner` never retries.
**Approach (assumed at the header base):** New file `Bulk Solution Exporter.Core/Run/ImportOperationWaiter.cs`, one deep class. The constructor takes `IDataverseGateway`, `Action<int> sleep`, `Func<DateTime> getUtcNow`, `Action<string> log` and `Action<TimeSpan> onPoll` (elapsed time, for progress). `WaitForCompletion(Guid asyncOperationId, int maxConsecutivePollFailures)` loops: sleep `PollIntervalInMilliseconds` (5000, a named constant), poll, and reset the failure count on a good poll. A poll exception is logged and counted. When the count exceeds the limit it throws `DataverseOperationException` with `RetryRunner.ImportTrackingLostFaultText` ("The run lost track of import operation") plus the id. The sleep is the plain injected sleep, never the abort token, so an abort does not shorten the wait (ratified). Canceled throws with "The import operation was canceled on the server." plus the message. In `RetryRunner`, add `ImportTrackingLostFaultText` and a catch beside the `UpgradeAlreadyExistsFaultText` one that logs an error and returns false, so the import is never re-submitted.
**Files:** `Bulk Solution Exporter.Core/Run/ImportOperationWaiter.cs`, `Bulk Solution Exporter.Core/Run/RetryRunner.cs`, `Bulk Solution Exporter.Core.Tests/ImportOperationWaiterTests.cs`, `Bulk Solution Exporter.Core.Tests/RetryRunnerTests.cs`
**Read first:** Bulk Solution Exporter.Core/Run/RetryRunner.cs — Execute, UpgradeAlreadyExistsFaultText, IsMessageInChain; Bulk Solution Exporter.Core.Tests/RetryRunnerTests.cs — Execute_UpgradeAlreadyExists_ReturnsFalseWithoutRetry, FakeRetryEnvironment;
Bulk Solution Exporter.Core/Run/DataverseOperationException.cs — DataverseOperationException(string), DataverseOperationException(string, Exception); Bulk Solution Exporter.Core/Run/IDataverseGateway.cs — IDataverseGateway
**Tests:** waiter: Running×2 then Succeeded returns after 3 polls; Failed throws with the message; Canceled throws; failures within the limit then Succeeded returns; failures beyond the limit throw the tracking-lost text. RetryRunner: the tracking-lost fault returns false without a retry, with continueOnError true and false.
**Acceptance:**
- `dotnet test "Bulk Solution Exporter.Core.Tests" --filter "FullyQualifiedName~ImportOperationWaiterTests|FullyQualifiedName~RetryRunnerTests"` exits 0.
**Commit:** `feat(core): poll async import operations to completion`

## 4. Run imports as one async operation in RunEngine

Depends on items 1, 2 and 3.

**What:**
**Goal:** `RunEngine` installs each solution through `StartImport` plus `ImportOperationWaiter` inside one `ExecuteWithRetries` action. An upgrade sends `IsUpgrade = true` and makes no DeleteAndPromote call after a fresh install. A failed operation's message reaches the log, and "Cannot start another [Import]" in it re-submits after the retry delay. This fixes `import-async-upgrade` (the two-step holding install plus `DeleteAndPromote` stranded upgrades on client timeout or retry).
**Approach (assumed at the header base):** In `RunEngine.InstallSolution` (`RunEngine.Import.cs`), replace the `ImportSolution` call and the `DeleteAndPromote` block with one `ExecuteWithRetries(() => { var id = target.StartImport(file, options); waiter.WaitForCompletion(id, maxRetries); }, …)`. `options.IsUpgrade` gets today's holding condition. The waiter is built from `RunEngine`'s sleep, clock and log. `onPoll` reports `_sink.Progress($"Installing {stepName}: … ({elapsed})", lastDurationString, isNewStep: false)`. The step names keep "upgrade"/"update"; the log line "Applying the solution upgrade..." goes away. The run's existing sleep is `_cancellationToken.WaitHandle.WaitOne`, which returns early on abort, so pass the waiter a sleep that does not (`Thread.Sleep`, injected through the existing seam the tests use).
**Regression guard.** No such seam exists and Core has no `InternalsVisibleTo`: add a public `RunEngine.Run` overload taking `Action<int> pollSleep` (production passes `Thread.Sleep`, tests a no-op). Hoist `RetryCount > 0 ? RetryCount : DefaultRetryCount` out of `ExecuteWithRetries` into a helper `InstallSolution` also uses for the waiter's limit. `FakeDataverseGateway` gets `Queue<string> OperationStateFaultMessages`, thrown by `GetImportOperationState` as `DataverseOperationException`. The test helper `ImportedNames` filters on `"StartImport:"` (it serves `Run_ImportFailsWithContinueOnError_ImportsTheNextSolution`, `Run_ImportFailsWithoutContinueOnError_StopsTheTargetsImports`, `Run_TwoTargets_ImportsIntoEachAndPublishesEach`); `StartRun_ImportErrorFromHost_ReachesEngineWithMessage` keeps `ImportFaultMessage` (item 2) and asserts `"startImport:"`. This supersedes the documented rule "a holding install plus its apply-upgrade is one step" (`.capcom/knowledge/run-and-abort-rules.md:11`, `RunEngine.cs:31-32`, `RunRequest.cs:105-106`); items 5 and 6 rewrite those lines.
**Files:** `Bulk Solution Exporter.Core/Run/RunEngine.Import.cs`, `Bulk Solution Exporter.Core/Run/RunEngine.cs`, `Bulk Solution Exporter.Core.Tests/Fakes/FakeDataverseGateway.cs`, `Bulk Solution Exporter.Core.Tests/RunEngineImportTests.cs`, `Bulk Solution Exporter.PptbWorker.Tests/WorkerProtocolTests.cs`
**Read first:** Bulk Solution Exporter.Core/Run/RunEngine.Import.cs — InstallSolution, ExecuteWithRetries; Bulk Solution Exporter.Core/Run/RunEngine.cs — RunEngine (ctor), Run;
Bulk Solution Exporter.Core.Tests/RunEngineImportTests.cs — ImportedNames, Run_ManagedUpgradeOfInstalledSolution_InstallsHoldingAndPromotes; Bulk Solution Exporter.PptbWorker.Tests/WorkerProtocolTests.cs — StartRun_ImportErrorFromHost_ReachesEngineWithMessage;
Bulk Solution Exporter.PptbWorker.Tests/Fakes/FakeToolHost.cs — ImportFaultMessage
**Tests:** `Run_ManagedUpgradeOfInstalledSolution_InstallsHoldingAndPromotes` becomes an upgrade-in-one-call test: `IsUpgrade` true and, with no `<name>_Upgrade` on the target, no `DeleteAndPromote` call. New: an unmanaged import is `IsUpgrade` false; a Failed state logs its message and fails the solution; a "Cannot start another [Import]" Failed state then Succeeded re-submits once and succeeds; tracking lost (scripted with `OperationStateFaultMessages`) fails the solution with exactly one `StartImport` call. All run tests pass the no-op `pollSleep`. Worker: a managed import reaches `startImport` and `getImportOperationState` on `FakeToolHost` (its `SolutionExists` always answers false, so no upgrade path); `StartRun_ImportErrorFromHost_ReachesEngineWithMessage` asserts `"startImport:"`.
**Acceptance:**
- `dotnet test "Bulk Solution Exporter.Core.Tests"` exits 0; `dotnet test "Bulk Solution Exporter.PptbWorker.Tests"` exits 0.
- The upgrade test asserts that a run whose target has no "<name>_Upgrade" records no DeleteAndPromote call, so the check stays true after item 6 adds the pending-upgrade apply to the same file.
**Commit:** `fix(core): import solutions as one async operation polled to completion`

## 5. Remove the synchronous import path from every host

Depends on item 4.

**What:**
**Goal:** No `ImportSolution` member, `ImportOptions.IsHolding` or `dataverse/importSolution` reverse call remains in any host, fake or test, and the `pptb-architecture` Note lists the new reverse calls.
**Approach (assumed at the header base):** Delete `IDataverseGateway.ImportSolution` and `ImportOptions.IsHolding` and their implementations: `OrganizationServiceGateway.ImportSolution`, `RpcDataverseGateway.ImportSolution` and `ImportSolutionMethod`, `FakeDataverseGateway.ImportSolution`, the `SmokeTests` stub, the `FakeToolHost` `importSolution` handler, and the bridge's `ImportSolution` entry, handler, `IMPORT_SOLUTION_OPERATION` and its vitest cases. `DeleteAndPromote` stays on every side (item 6 uses it). Rule for the Note: every place in `.capcom/knowledge/pptb-architecture.md` that names `dataverse/importSolution` or the holding flag (find them with `grep -n "importSolution\|holding" .capcom/knowledge/pptb-architecture.md`). Then run `capcom check`.
**Regression guard.** FakeToolHost.ImportFaultMessage stays (now used by startImport), only the importSolution handler is removed. The bridge's "execute rejects / surfaces the original message text unchanged" vitest case moves to `startImport`, keeping `IMPORT_ALREADY_RUNNING_TEXT` used (`noUnusedLocals` would fail `tsc`). Rule for code prose: rewrite every C# comment that calls an import a holding install plus apply-upgrade or crefs `ImportSolution` (`grep -rn -i "holding\|cref=\"[A-Za-z.]*ImportSolution\"" --include='*.cs' .`), superseding the abort-step rule in the `RunEngine.cs:31-32` class doc and `RunRequest.cs:105-106`; keep the `DeleteAndPromote` `_Upgrade` docs.
**Files:** `Bulk Solution Exporter.Core/Run/IDataverseGateway.cs`, `Bulk Solution Exporter/Run/OrganizationServiceGateway.cs`, `Bulk Solution Exporter.PptbWorker/RpcDataverseGateway.cs`, `Bulk Solution Exporter.Core.Tests/Fakes/FakeDataverseGateway.cs`, `Bulk Solution Exporter.Tests/SmokeTests.cs`, `Bulk Solution Exporter.PptbWorker.Tests/Fakes/FakeToolHost.cs`, `pptb/src/dataverseBridge.ts`, `pptb/src/dataverseBridge.test.ts`, `.capcom/knowledge/pptb-architecture.md`, `Bulk Solution Exporter.Core/Run/RunEngine.cs`, `Bulk Solution Exporter.Core/Run/RunRequest.cs`
**Read first:** Bulk Solution Exporter.Core/Run/IDataverseGateway.cs — ImportSolution, ImportOptions.IsHolding; Bulk Solution Exporter.PptbWorker/RpcDataverseGateway.cs — ImportSolutionMethod; Bulk Solution Exporter.Core.Tests/Fakes/FakeDataverseGateway.cs — ImportSolution;
Bulk Solution Exporter.PptbWorker.Tests/Fakes/FakeToolHost.cs — ImportSolution; pptb/src/dataverseBridge.test.ts — IMPORT_ALREADY_RUNNING_TEXT; .capcom/knowledge/pptb-architecture.md — reverse-call list;
Bulk Solution Exporter.Core/Run/RunEngine.cs — RunEngine class summary
**Tests:** existing suites; the bridge's rejection case now calls `startImport` and still expects the "Cannot start another [Import]" text unchanged.
**Acceptance:**
- `dotnet build "Bulk Solution Exporter.sln" -c Debug` exits 0; `dotnet test "Bulk Solution Exporter.Core.Tests"` and `dotnet test "Bulk Solution Exporter.PptbWorker.Tests"` exit 0; `cd pptb && npm test` exits 0; `cd pptb && npx tsc --noEmit` exits 0.
- `grep -rn "importSolution\|void ImportSolution(\|ImportSolutionRequest\|IsHolding\|ImportSolutionMethod\|IMPORT_SOLUTION_OPERATION" --include='*.cs' --include='*.ts' --exclude-dir=node_modules .` prints nothing outside `bin/` and `obj/`.
- `grep -c "dataverse/startImport" .capcom/knowledge/pptb-architecture.md` prints 1 or more.
- `capcom check` reports no problem in `.capcom/knowledge/`.
**Commit:** `refactor: remove the synchronous holding import path`

## 6. Apply a pending _Upgrade before installing an upgrade

Depends on item 4.

**What:**
**Goal:** An upgrade whose target already has `<uniqueName>_Upgrade` logs a warning naming it, applies it with `DeleteAndPromote` through `ExecuteWithRetries`, then installs this run's file as in item 4. A failed apply fails that solution without installing. The `run-and-abort-rules` Note describes this.
**Approach (assumed at the header base):** In `RunEngine.InstallSolution`, when `IsUpgrade` is true, call `DoesSolutionExist(target, uniqueName + HoldingSolutionSuffix)` (`"_Upgrade"`, a named constant) before the install. When it is true, log a warning ("A pending upgrade '<name>_Upgrade' exists on the target; applying it first."), report progress "Applying pending upgrade: …", and run `ExecuteWithRetries(() => target.DeleteAndPromote(uniqueName), …)`. On false, return false and do not install. "Cannot start another [Import]" is retried by the existing `RetryRunner` rule. A "_Upgrade already exists" fault on the install itself (the holding solution appeared after the check) keeps today's handling. In `.capcom/knowledge/run-and-abort-rules.md`, rewrite the Import-faults sentence: the pending upgrade is applied first; the fault text still fails the solution when it appears on the install itself. Then run `capcom check`.
**Regression guard.** `FakeDataverseGateway` gets a `FailDeleteAndPromoteFor` switch (as `FailExportFor` does) that makes `DeleteAndPromote` throw `DataverseOperationException` for that unique name. The Note edit is a rule, not the one sentence: rewrite every line of `.capcom/knowledge/run-and-abort-rules.md` on `_Upgrade` handling or apply-upgrade, found with `grep -n "_Upgrade\|apply-upgrade\|holding" .capcom/knowledge/run-and-abort-rules.md` (line 11's holding-install-plus-apply-upgrade step included).
**Files:** `Bulk Solution Exporter.Core/Run/RunEngine.Import.cs`, `Bulk Solution Exporter.Core.Tests/Fakes/FakeDataverseGateway.cs`, `Bulk Solution Exporter.Core.Tests/RunEngineImportTests.cs`, `.capcom/knowledge/run-and-abort-rules.md`
**Read first:** Bulk Solution Exporter.Core/Run/RunEngine.Import.cs — InstallSolution, DoesSolutionExist; Bulk Solution Exporter.Core.Tests/Fakes/FakeDataverseGateway.cs — DeleteAndPromote, FailExportFor;
Bulk Solution Exporter.Core.Tests/RunEngineImportTests.cs — Run_UpgradeAlreadyExists_FailsTheSolutionWithoutPromote; Bulk Solution Exporter.Core/Run/RetryRunner.cs — UpgradeAlreadyExistsFaultText;
Bulk Solution Exporter.Core/Run/RunLog.cs — Log, ColorOrange (no LogWarning exists)
**Tests:** upgrade with `<name>_Upgrade` present → calls in order `SolutionExists:<name>_Upgrade`, `DeleteAndPromote:<name>`, `StartImport`, success. A failing `DeleteAndPromote` (armed with `FailDeleteAndPromoteFor`, Continue-On-Error on) fails the solution with no `StartImport`. A plain import never checks `_Upgrade`.
**Acceptance:**
- `dotnet test "Bulk Solution Exporter.Core.Tests" --filter "FullyQualifiedName~RunEngineImportTests"` exits 0.
- `capcom check` reports no problem in `.capcom/knowledge/`.
**Closes:** pending-upgrade-apply
**Commit:** `fix(core): apply a pending _Upgrade before installing an upgrade`
