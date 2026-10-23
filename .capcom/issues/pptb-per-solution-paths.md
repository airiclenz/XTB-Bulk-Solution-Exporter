---
title: Set one solution's export paths in PPTB
status: open
type: feature
priority: 3
labels: [pptb, pptb-port-followup]
created: 2026-10-23T16:25:48Z
---

The XrmToolBox plugin lets the user browse for or type one solution's managed and unmanaged export path. The PPTB tool has no such control: only the folder wizard (`pptb/src/components/FileWizard.tsx:45`) sets paths, for every checked solution at once, so a single solution cannot get its own path.

## Acceptance

- In the PPTB tool the user can set and clear one solution's managed and unmanaged export path, and the change is saved through the settings store.
