---
title: A pending _Upgrade holding solution fails the import
status: open
type: bug
priority: 2
labels: [import]
deps: [import-async-upgrade]
created: 2026-11-18T15:07:25Z
---

When the target already holds a pending `<name>_Upgrade` holding solution (left over from an earlier failed run, or from a concurrent one), the import fails with "_Upgrade already exists". `RetryRunner.Execute` (`Bulk Solution Exporter.Core/Run/RetryRunner.cs`) logs an error and returns `false` for that solution. The target stays half-upgraded until someone applies the upgrade by hand, and every later run fails the same way.

Wanted (ratified by airic, 2026-11-18): log a warning naming the pending upgrade, apply it (`DeleteAndPromote`, waiting and retrying while "Cannot start another [Import]" says another import is running), then install this run's file with `StageAndUpgradeAsync`, so the target ends on this run's version.

This changes the rule recorded in the Knowledge Note `run-and-abort-rules` ("_Upgrade already exists" fails that solution, with no apply-upgrade). The Note must be updated with the change.

## Why

A single stranded holding solution blocks every later upgrade of that solution until someone fixes it by hand.

## Acceptance

- An upgrade that finds '_Upgrade already exists' logs a warning, applies the pending upgrade, then installs this run's file, and reports success when both finish.
- If applying the pending upgrade fails, that solution fails with the apply error in the log, and the run's file is not installed.
- The run-and-abort-rules Note describes the new behaviour.
- Core tests cover the apply-then-install path and the failed-apply path.
