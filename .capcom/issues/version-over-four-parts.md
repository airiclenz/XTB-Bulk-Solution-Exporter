---
title: Version(string) throws on versions with more than four parts
status: done
type: bug
priority: 2
labels: [audit-followup]
created: 2026-10-11T20:15:00Z
closed: 2026-11-19T15:56:37Z
reason: "Resolved by the robustness and log escaping plan (2026-11-18 - 01), archived in 526aa56: item 1 ignores version parts beyond the fourth; covered in VersionTests."
---

`Schema/Version.cs` `Version(string)` loops over every dot-separated part of the source version and indexes a fixed four-slot array, so a source version with more than four parts throws `IndexOutOfRangeException`.

## Acceptance

- A version with more than four parts is rejected cleanly (or truncated, matching the ratified "result has as many parts as the format" rule) instead of throwing.
- Covered in `VersionTests`.

## Log

- 2026-10-11 airic: migrated to capcom from the ToDo.md known-issues list (deferred from the 2026-10-11 audit fixes).
