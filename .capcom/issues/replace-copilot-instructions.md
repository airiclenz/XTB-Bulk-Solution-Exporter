---
title: Replace copilot-instructions.md, it describes another project
status: open
type: chore
priority: 3
labels: [agents]
created: 2026-10-11T20:15:00Z
---

`.github/copilot-instructions.md` describes Dataverse server-side plugins (namespace `BO.D365.Dataverse.Plugin`, .NET 4.7.1, `spkl.json`, `key.snk`), none of which exist here. Agents reading it get wrong architecture rules. Replace it with a pointer to `AGENTS.md` / the capcom knowledge tree, or delete it.

## Log

- 2026-10-11 airic: migrated to capcom from the 2026-10-11 audit ground truth (caveats).
