---
title: Stop committing built .nupkg files
status: open
type: chore
priority: 4
labels: [build]
created: 2026-10-11T20:15:00Z
---

`Bulk Solution Exporter/Bulk Solution Exporter.nuget/` holds 18 committed `.nupkg` build outputs (2024.10.7.9 to 2026.10.8.29). Decide whether release packages belong in git (GitHub releases are the usual home); if not, remove them and ignore the folder. Check `Scripts/NuGet Pack.ps1` output path first.

## Log

- 2026-10-11 airic: migrated to capcom from the 2026-10-11 audit-fixes plan (out of scope).
