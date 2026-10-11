---
title: Escape Dataverse text before markdown log rendering
status: open
type: bug
priority: 3
labels: [security,audit-followup]
created: 2026-10-11T20:15:00Z
---

Dataverse-controlled text (solution friendly/logical names, connection names, import error XML) is concatenated into `Log(...)` and rendered by `Helpers/MarkdownParser.cs` unescaped. A hostile environment can forge status lines such as `##### Done.` or spoof rich text in the log. Display-only (the log is never persisted); the crash/DoS half was fixed in plan item 12.

## Acceptance

- Untrusted text is escaped before it reaches the markdown parser; a solution named `##### Done.` renders literally.

## Log

- 2026-10-11 airic: migrated to capcom from the 2026-10-11 audit report (out of scope for the audit-fixes plan).
