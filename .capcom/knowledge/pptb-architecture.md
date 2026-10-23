---
title: "PPTB port: core, .NET worker and React UI, and the RPC method list"
labels: [architecture, pptb]
refs: [Bulk Solution Exporter.Core/Run/RunEngine.cs, Bulk Solution Exporter.PptbWorker/WorkerMethods.cs, Bulk Solution Exporter.PptbWorker/WorkspaceMethods.cs, Bulk Solution Exporter.PptbWorker/RpcDataverseGateway.cs, Bulk Solution Exporter.PptbWorker/RpcRunSink.cs, pptb/src/workerClient.ts, pptb/src/dataverseBridge.ts, pptb/src/settingsStore.ts, pptb/src/App.tsx, pptb/pptb.config.json]
---

The Power Platform ToolBox (PPTB) port is three layers; only the UI talks to PPTB and Dataverse.

- **Core** (`Bulk Solution Exporter.Core`, `net48;net8.0`): the UI-free logic both hosts share: `RunEngine` (publish, version, export, git, import phases), `RetryRunner`, `RunLog` markup, the solution schema, versions file, CSV and file/git helpers. It reaches Dataverse only through `IDataverseGateway` (target 0 = source, 1..n = targets) and reports through `IRunSink`. The XrmToolBox plugin adapts the gateway to `IOrganizationService`.
- **Worker** (`Bulk Solution Exporter.PptbWorker`, `net8.0`, NuGet .NET tool `AiricLenz.BulkSolutionExporter.PptbWorker`, command `bse-pptb-worker`): PPTB starts it from `pptb/pptb.config.json` (worker id `engine`) and speaks StreamJsonRpc `jsonrpc-stdio-v1` (version 1) on stdin/stdout; stdout is reserved for the protocol, diagnostics go to stderr. `WorkerMethods` (run), `WorkspaceMethods` (git and files), `RpcDataverseGateway` (the gateway as reverse calls) and `RpcRunSink` (the sink as notifications). One run at a time.
- **UI** (`pptb/`, React 18 + Fluent UI v9, Vite IIFE single bundle in `dist/`): `App.tsx` reads the PPTB connections (slot 0 = source, every further slot a target, in slot order), `workerClient.ts` holds the typed worker calls and notification subscriptions, `dataverseBridge.ts` answers the reverse calls through `window.dataverseAPI` (solution files as base64; rejections keep Dataverse's message text, which `RetryRunner` classifies), `settingsStore.ts` persists settings and per-solution configurations in `toolboxAPI.settings` (key: lower-cased environment URL + `.` + unique name).

RPC methods (names are constants in `WorkerMethods.cs`/`WorkspaceMethods.cs`/`RpcDataverseGateway.cs`/`RpcRunSink.cs` and mirrored in `workerClient.ts`/`dataverseBridge.ts`; change both sides together):

UI -> worker requests:
- `platform/initialize`: PPTB's handshake; refuses any protocol other than `jsonrpc-stdio-v1` version 1.
- `run/start`: runs the `RunRequest` with the source and target display names; returns the run report (aborted, duration, step outcomes). Cancelling the request aborts the run.
- `run/abort`: requests an abort between steps; true when a run was active.
- `git/branches`: the local branches and the checked-out branch of the repository holding a file.
- `git/switchBranch`: checks out a listed local branch; refused while a run is active.
- `files/status`: each solution's export file state (configured, present, recorded version, outdated).
- `files/exportCsv`: writes the solution CSV (`Unique Name, Solution Guid, Version`).
- `files/suggestFileNames`: the file wizard's sanitized export paths for a folder.

Worker -> UI notifications (during `run/start`):
- `run/log`: one markup log line. `run/progress`: the progress state. `run/solutionVersionChanged`: a solution's new version. `run/configurationChanged`: a solution configuration to persist. `run/solutionProcessed`: one solution finished its export or one import, so the UI may refresh its file status.

Worker -> UI reverse calls (each carries `target`, the gateway index):
- `dataverse/publishAll`, `dataverse/updateSolutionVersion`, `dataverse/exportSolution`, `dataverse/solutionExists`, `dataverse/importSolution` (holding upgrade, managed, publish workflows, overwrite flags), `dataverse/deleteAndPromote`, `dataverse/retrieveSolutions`.

File paths from the UI are validated in the worker (absolute, no `..`), and everything from Dataverse or git is untrusted (see `untrusted-dataverse-data`). Packaging and the version rules are in `versioning-and-release`.
