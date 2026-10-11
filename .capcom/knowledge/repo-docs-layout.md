---
title: Where plans, handoffs, release notes and open work live
labels: [docs, agents]
refs: [docs/plans, README.md]
---

- `docs/plans/` holds implementation plans (house format, `YYYY-MM-DD - NN - name-plan.md`); finished plans move to `docs/plans/archived/`. Issues link a plan by basename in `plan:`.
- `docs/handoffs/` and `docs/skill-runs/` are git-ignored working output from agent skills (handoffs, audit reports, release runs). Useful history, not a source of truth.
- Open work, ideas, known issues and the release history are all tracked in capcom (see `versioning-and-release` for the release layout). There is no ToDo or CHANGELOG file.
- Bulk Solution Exporter is a WinForms XrmToolBox plugin (.NET Framework 4.8) that batch exports/imports Dataverse solutions, bumps versions and commits the exports to git. Main code: `Bulk Solution Exporter/BulkSolutionExporter_PluginControl.cs`, `Settings.cs`, `Helpers/`, `Schema/`.
