---
title: SaveVersionJson throws on a malformed exported_versions.json
status: open
type: bug
priority: 2
labels: [audit-followup]
created: 2026-10-11T20:15:00Z
---

The writer `SaveVersionJson` in `Bulk Solution Exporter/BulkSolutionExporter_PluginControl.cs` deserializes `exported_versions.json` without a try or null check, so a malformed (e.g. truncated) or literal `null` file still throws on the export worker. The loader (`ExportedSolutionVersions.TryLoadExportedSolution`) was hardened in plan item 5; the writer was not.

## Acceptance

- A malformed or `null` `exported_versions.json` does not fault the export run; the writer starts a fresh record and logs a warning.
- A unit test covers both cases.

## Log

- 2026-10-11 airic: migrated to capcom from the ToDo.md known-issues list (deferred from the 2026-10-11 audit fixes).
