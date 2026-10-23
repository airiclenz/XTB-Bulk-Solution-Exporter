---
title: Remove the dead RetryRunner.IsRunAborted
status: open
type: task
priority: 4
labels: [pptb-port-followup]
created: 2026-10-23T16:25:48Z
---

`RetryRunner.IsRunAborted` (`Bulk Solution Exporter.Core/Run/RetryRunner.cs:178`) has no production caller since plan item 9: the plugin now uses `_runResult?.IsAborted ?? _runCancellation.IsCancellationRequested`. Only its test calls it (`Bulk Solution Exporter.Core.Tests/RetryRunnerTests.cs:321`, cref at `:18`). The coding-standards delete-dead-code rule applies.

## Acceptance

- `RetryRunner.IsRunAborted` and its `RetryRunnerTests` case (and the cref) are gone; the solution builds and the core tests pass.
