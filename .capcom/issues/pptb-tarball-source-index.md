---
title: PPTB tarball ships the source index.html
status: open
type: bug
priority: 3
labels: [pptb, pptb-port-followup]
created: 2026-10-23T16:25:48Z
---

npm always packs the `package.json` `main` file. `pptb/package.json:6` sets `"main": "index.html"` (plan item 12), so the PPTB tarball also carries the Vite source `index.html` at its root next to `dist/index.html`. Depending on how PPTB resolves a tool's entry page, it may load the unbuilt source page instead of the built one.

## Acceptance

- It is confirmed how PPTB resolves the entry page, and `main` (or the packed files) is set so the tarball's entry page is the built `dist/index.html` and the source `index.html` is not shipped, or is shown to be harmless.
