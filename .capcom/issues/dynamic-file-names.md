---
title: Support dynamic placeholders in export file names
status: open
type: feature
priority: 3
labels: [idea]
created: 2026-10-11T20:15:00Z
---

Allow dynamic content in the export file names (e.g. version, date, connection). Any placeholder expansion must still go through the file-name sanitizing in `FileAndFolderHelper.ToSafeFileNamePart` / `IsValidSolutionFilePath`, since solution names come from Dataverse.

## Log

- 2026-10-11 airic: migrated to capcom from the ToDo.md idea list.
