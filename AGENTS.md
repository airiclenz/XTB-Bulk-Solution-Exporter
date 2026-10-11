<!-- BEGIN CAPCOM -->
<!-- Managed by capcom: run "capcom agents" to refresh. Edits inside these markers are overwritten. -->
## Issues and knowledge: capcom

capcom is the issue register of record for this repository. Issues and Knowledge Notes are
markdown files under `.capcom/`, versioned with the code; track work there and nowhere else.

- `capcom ready`: the issues ready to work on, most urgent first
- `capcom show <id>`: one issue in full
- `capcom create --id <slug> --title <title>`: file a new issue
- `capcom close <id> --reason <text>`: close an issue
- `capcom know tree`: the outline of the Knowledge tree; `capcom know show <id>` reads a Note

Knowledge tree, top level:

- `build-and-test`: Build with VS MSBuild and test with vstest, not dotnet
- `code-style`: Code style: tabs, Allman, method dividers, no magic numbers
- `copilot-instructions-stale`: copilot-instructions.md describes a different project
- `repo-docs-layout`: Where plans, handoffs, release notes and open work live
- `run-and-abort-rules`: Run invariants: checked-solution snapshot, abort between steps, retries
- `ui-thread-rules`: UI objects only on the UI thread; the worker uses the settings snapshot
- `untrusted-dataverse-data`: Treat Dataverse names, XML and git text as untrusted input
- `versioning-and-release`: Versions must agree across nuspec, AssemblyInfo and packages.config
<!-- END CAPCOM -->
