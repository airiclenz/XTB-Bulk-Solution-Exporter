---
title: Run PowerShell scripts at any point of the run
status: open
type: feature
priority: 3
labels: [idea]
created: 2026-10-11T20:15:00Z
---

Allow executing a user PowerShell script at any step of the execution flow (before/after version bump, export, import, git commit). Must respect the abort semantics: a script is one step, abort lands between steps.

## Log

- 2026-10-11 airic: migrated to capcom from the ToDo.md idea list.
