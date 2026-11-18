---
title: Managed upgrades can stay unapplied as holding solutions
status: done
type: bug
priority: 1
labels: [import, pptb]
created: 2026-11-18T15:07:25Z
closed: 2026-11-18T16:31:36Z
reason: "Resolved by the async-solution-import plan (archived in a6703aa): imports run as one StageAndUpgradeAsync/ImportSolutionAsync operation polled to completion on both hosts."
---

A managed upgrade is two client calls in `RunEngine.InstallSolution` (`Bulk Solution Exporter.Core/Run/RunEngine.Import.cs`): a synchronous `ImportSolution` with `HoldingSolution = true`, then `DeleteAndPromote`. The second call runs only when the first returns successfully to the client, so the target can be left with an unapplied `<name>_Upgrade` holding solution:

- **Client timeout.** A long import outlives the connection timeout, but the server keeps importing. With Continue-On-Error off, the solution is reported as failed and no promote runs. With it on, `RetryRunner` re-submits the same import while the first is still running. This is the observed "another instance of the same installation was started".
- **Retry window too short.** The re-submitted import gets "Cannot start another [Import]", which is retried only about 3 × 5 s by default. The run then gives up while the first import finishes on the server, and it is never promoted.
- **Retry after the holding install landed.** It gets "_Upgrade already exists" and fails at once, so there is no promote (the follow-up is `pending-upgrade-apply`).
- **Two runs on one solution.** `DeleteAndPromote` works by unique name, so one run can promote the other's holding solution, and the second run's promote then fails.

Plain imports (managed update, unmanaged) share the timeout problem: a lost response re-submits a complete import.

Both hosts are affected: the XrmToolBox plugin (`Bulk Solution Exporter/Run/OrganizationServiceGateway.cs`) and PPTB (`pptb/src/dataverseBridge.ts` through `Bulk Solution Exporter.PptbWorker/RpcDataverseGateway.cs`).

Fix (ratified by airic, 2026-11-18): an upgrade is one server-side `StageAndUpgradeAsync` call, and every other import is `ImportSolutionAsync`. The run polls the returned `asyncoperation` until it reaches a final state (statecode 3; statuscode 30 succeeded, 31 failed, 32 canceled), with no cap on the wait. Abort still takes effect only after the server operation ends.

## Why

Users see successful-looking runs whose upgrade never applied, and timeouts start duplicate imports.

## Acceptance

- Upgrades go through one StageAndUpgradeAsync call; no client-side DeleteAndPromote follows a fresh install.
- Plain managed and unmanaged imports go through ImportSolutionAsync.
- The run polls the asyncoperation until statecode 3 and reports success only on statuscode 30; a failed operation's message reaches the log.
- A lost or failed poll is retried as a poll and never re-submits the import.
- An operation that failed with 'Cannot start another [Import]' is re-submitted after the retry delay, within the run's retry count.
- Both hosts (XrmToolBox gateway and the PPTB bridge) implement it, with tests in Core.Tests, PptbWorker.Tests and pptb vitest.
