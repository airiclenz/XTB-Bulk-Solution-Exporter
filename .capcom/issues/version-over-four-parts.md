---
title: Version(string) throws on versions with more than four parts
status: open
type: bug
priority: 2
labels: [audit-followup]
created: 2026-10-11T20:15:00Z
---

`Schema/Version.cs` `Version(string)` loops over every dot-separated part of the source version and indexes a fixed four-slot array, so a source version with more than four parts throws `IndexOutOfRangeException`.

## Acceptance

- A version with more than four parts is rejected cleanly (or truncated, matching the ratified "result has as many parts as the format" rule) instead of throwing.
- Covered in `VersionTests`.

## Log

- 2026-10-11 airic: migrated to capcom from the ToDo.md known-issues list (deferred from the 2026-10-11 audit fixes).
