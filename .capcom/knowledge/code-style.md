---
title: 'Code style: tabs, Allman, method dividers, no magic numbers'
labels: [conventions]
refs: [.editorconfig, .gitmodules#XTB Components]
---

The style of the file being edited wins. Concretely:

- Tabs, CRLF, Allman braces, `var` discouraged, System usings first (`.editorconfig`).
- An 80-char `// ====...` divider line before every method; test files too.
- `{ }` always for control blocks; guard clauses; one argument per line for long calls.
- No bare magic numbers in new code: add a named constant next to the existing ones.
- In the worker loop prefer `break` over `return`, so the `DecreaseIndent()` / `Log()` after the loop still runs.
- Keep each UI state (text, image, tooltip, enabled) switched in one private method.

`XTB Components/` is a git submodule (`github.com/airiclenz/XTB-Components`, provides `SortableCheckList` and `FlipSwitch`): never edit it from this repo.
