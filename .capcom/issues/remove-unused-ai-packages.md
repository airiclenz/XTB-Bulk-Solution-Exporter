---
title: Remove the unused Microsoft.Extensions.AI references
status: open
type: chore
priority: 4
labels: [build]
created: 2026-10-11T20:15:00Z
---

`Bulk Solution Exporter/Bulk Solution Exporter.csproj` and `packages.config` reference `Microsoft.Extensions.AI` and `Microsoft.Extensions.AI.Abstractions` 10.10.x, but no `.cs` file uses them. Remove the references and packages, then build and run the tests.

## Log

- 2026-10-11 airic: migrated to capcom from the 2026-10-11 audit ground truth.
