---
title: Log truncation can leave a partial color tag visible
status: open
type: bug
priority: 3
labels: [audit-followup]
created: 2026-10-11T20:15:00Z
---

The log line truncation added in plan item 12 (`Helpers/MarkdownParser.cs`) can cut inside a `<color=...>` / `</color>` tag, leaving the partial tag visible as literal text.

## Acceptance

- Truncation never splits a tag: cut before an unterminated tag and close any open color span.

## Log

- 2026-10-11 airic: migrated to capcom from the ToDo.md known-issues list (deferred from the 2026-10-11 audit fixes).
