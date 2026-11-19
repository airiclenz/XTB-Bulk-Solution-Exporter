---
title: Log truncation can leave a partial color tag visible
status: done
type: bug
priority: 3
labels: [audit-followup, release-2026-11-19]
created: 2026-10-11T20:15:00Z
closed: 2026-11-19T15:56:37Z
reason: "Resolved by the robustness and log escaping plan (2026-11-18 - 01), archived in 526aa56: items 6-8 add tag-safe truncation in LogMarkup, MarkdownParser and logMarkup.ts."
---

The log line truncation added in plan item 12 (`Helpers/MarkdownParser.cs`) can cut inside a `<color=...>` / `</color>` tag, leaving the partial tag visible as literal text.

## Acceptance

- Truncation never splits a tag: cut before an unterminated tag and close any open color span.

## Log

- 2026-10-11 airic: migrated to capcom from the ToDo.md known-issues list (deferred from the 2026-10-11 audit fixes).
