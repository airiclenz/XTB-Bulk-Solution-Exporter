---
title: Move ColorHelper out of the plugin into XTB Components
status: open
type: task
priority: 3
labels: [architecture]
created: 2026-10-11T20:15:00Z
---

`Bulk Solution Exporter/Helpers/ColorHelper.cs` (`MixColors`) exists only for callers in the `XTB Components` submodule (`FlipSwitch`, `SortableCheckList`), which reach back into the plugin's internals. The dependency seam is inverted: the submodule cannot be reused or versioned on its own.

Needs an architecture review before refactoring; the change spans two repositories (`github.com/airiclenz/XTB-Components`).

## Log

- 2026-10-11 airic: migrated to capcom from the 2026-10-11 audit report (out of scope for the audit-fixes plan).
