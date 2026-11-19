---
title: PPTB worker pulls a vulnerable MessagePack 2.5.302 through StreamJsonRpc
status: done
type: bug
priority: 3
labels: [security, build]
created: 2026-11-19T17:39:44Z
closed: 2026-11-19T17:40:48Z
reason: StreamJsonRpc raised to 2.26.150, which pulls the patched MessagePack 2.5.307; scan clean, tests pass.
---

`Bulk Solution Exporter.PptbWorker` references `StreamJsonRpc` 2.25.29, which pulls `MessagePack` 2.5.302, flagged by NuGet audit as moderate: https://github.com/advisories/GHSA-qhrr-8q5h-9q3h (nested arrays can blow past buffer length limits; patched in 2.5.303). The worker only uses `JsonMessageFormatter`, so MessagePack never reads input, but the DLL ships with the worker.

Fix: raise `StreamJsonRpc` to 2.26.150, which depends on MessagePack 2.5.307.

## Why

Audit warnings on every build hide new findings, and the worker should not ship a flagged DLL.

## Acceptance

- `dotnet list package --vulnerable --include-transitive` reports no vulnerable packages for the worker and its tests.
- The worker and its tests build without warnings and the worker tests pass.

## Log

- 2026-11-19 Airic Lenz: Raised StreamJsonRpc 2.25.29 -> 2.26.150 (MessagePack 2.5.302 -> 2.5.307, Nerdbank.MessagePack 1.2.4 -> 1.3.88); packages.lock.json regenerated. Worker and tests build Release with 0 warnings; worker tests 31/31 pass; vulnerable scan clean for both projects.
