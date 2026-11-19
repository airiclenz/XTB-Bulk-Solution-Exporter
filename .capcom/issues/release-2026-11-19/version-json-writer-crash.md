---
title: SaveVersionJson throws on a malformed exported_versions.json
status: done
type: bug
priority: 2
labels: [audit-followup, release-2026-11-19]
created: 2026-10-11T20:15:00Z
closed: 2026-11-19T15:56:37Z
reason: "Resolved by the robustness and log escaping plan (2026-11-18 - 01), archived in 526aa56: item 2 starts a fresh version record with a warning on malformed or null exported_versions.json; tested."
---

The writer `SaveVersionJson` in `Bulk Solution Exporter/BulkSolutionExporter_PluginControl.cs` deserializes `exported_versions.json` without a try or null check, so a malformed (e.g. truncated) or literal `null` file still throws on the export worker. The loader (`ExportedSolutionVersions.TryLoadExportedSolution`) was hardened in plan item 5; the writer was not.

## Acceptance

- A malformed or `null` `exported_versions.json` does not fault the export run; the writer starts a fresh record and logs a warning.
- A unit test covers both cases.

## Log

- 2026-10-11 airic: migrated to capcom from the ToDo.md known-issues list (deferred from the 2026-10-11 audit fixes).
