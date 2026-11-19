---
title: Writing the commit message to git stdin can throw IOException
status: done
type: bug
priority: 3
labels: [audit-followup]
created: 2026-10-11T20:15:00Z
closed: 2026-11-19T15:56:37Z
reason: "Resolved by the robustness and log escaping plan (2026-11-18 - 01), archived in 526aa56: item 3 turns a git stdin write failure into a failed result instead of an IOException."
---

`Helpers/GitHelper.cs` `WriteStandardInput` writes the commit message to git's stdin. If git exits before reading it, the write throws an uncaught `IOException` (broken pipe). Only reachable for messages larger than the pipe buffer.

## Acceptance

- A git process that exits early yields a failed git result, not an unhandled exception.

## Log

- 2026-10-11 airic: migrated to capcom from the ToDo.md known-issues list (deferred from the 2026-10-11 audit fixes).
