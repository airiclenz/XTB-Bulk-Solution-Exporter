---
title: PPTB SolutionProcessed returns before the UI refreshes
status: open
type: bug
priority: 3
labels: [pptb, pptb-port-followup]
created: 2026-10-23T16:25:48Z
---

`IRunSink.SolutionProcessed` (`Bulk Solution Exporter.Core/Run/IRunSink.cs:80`) says the host "must finish before returning: the engine's next step may write that file". The PPTB worker's `RpcRunSink.SolutionProcessed` (`Bulk Solution Exporter.PptbWorker/RpcRunSink.cs:127`) sends `run/solutionProcessed` as a fire-and-forget notification, so the PPTB UI may refresh a solution's file status after the engine has moved on and written the version file again.

## Acceptance

- The PPTB host's handling of `SolutionProcessed` completes before the engine's next step (for example a request/response instead of a notification), or the contract and both hosts are changed together so the refresh cannot read a later state.
