---
title: Grid functions slow for instances with large numbers of solutions
status: done
type: bug
priority: 2
labels: [release-2026-10-08]
aliases: [gh-2]
created: 2025-10-23T15:27:28Z
closed: 2026-03-05T11:58:35Z
reason: Fixed by the save debounce, a dictionary config cache and removing O(n^2) loops; shipped in release 2026.10.08.
---

With 300-400+ solutions in an environment, sorting, selecting and scrolling the solution grid became very slow. Profiling pointed at `SaveSettings`, called several times per grid event.

GitHub issue: https://github.com/airiclenz/XTB-Bulk-Solution-Exporter/issues/2

## Log

- 2025-12-14 airiclenz: asked the reporter to help debug, since hundreds of solutions are hard to reproduce.
- 2026-03-05 airiclenz: committed an optimized version (save debounce, O(1) config cache, no O(n^2) loops).
- 2026-03-05 reporter: confirmed all speed issues solved on an environment with 200+ solutions.
- 2026-10-11 airic: migrated to capcom from GitHub issue #2 (closed).
- 2026-10-11 Airic Lenz: listed in the release 2026.10.08 notes as the 300-400+ solution performance optimization (dictionary config cache, O(n^2) loops removed, save debounce).
