<!-- BEGIN CAPCOM -->
<!-- Managed by capcom: run "capcom agents" to refresh. Edits inside these markers are overwritten. -->
## Issues and knowledge: capcom

capcom is the issue register of record for this repository. Issues and Knowledge Notes are
markdown files under `.capcom/`, versioned with the code; track work there and nowhere else.

Without a `capcom` binary, install it from https://github.com/airiclenz/capcom#install.

- `capcom next`: the most urgent ready issue, in full
- `capcom ready`: the issues ready to work on, most urgent first
- `capcom show <id>`: one issue in full, with what blocks it
- `capcom update <id> --status active`: claim it before you start
- `capcom log <id> <text>`: note progress as work lands
- `capcom close <id> --reason <text>`: close an issue; it names the issues this made ready
- `capcom reopen <id> --reason <text>`: undo a close
- `capcom create --id <slug> --title <title> [--dep <id>]`: file a new issue
- `capcom dep add <id> <dep-id>`: make an issue wait for another; `capcom dep list <id>` shows both sides
- `capcom list --tree --depth 2`: the outline of the issues
- `capcom know tree`: the outline of the Knowledge tree; `capcom know show <id>` reads a Note

Each command's flags are in `capcom <cmd> --help`.

An issue's body says what is wrong or wanted, why, and how to tell it is done (`## Acceptance`),
readable on its own; a plan says how and in what order.

Where things are: an issue is `.capcom/issues/<id>.md`, and its children sit in the directory
`.capcom/issues/<id>/` beside it; Notes follow the same shape under `.capcom/knowledge/`.
Prefer capcom commands; after a hand edit run `capcom check`.

A standing issue (`standing: true` in its file) only groups its children: never claim or close it.

Tree lock: before a change that spans several files or commits, it is recommended to claim this
worktree with `capcom lock acquire --purpose <text>` and keep the token it prints.
`capcom lock touch --token <token>` renews it during long work,
`capcom lock release --token <token>` frees it when done, and acquiring again with `--token <token>`
resumes it after a restart or compaction. When `capcom lock check --token <token>` exits 3, or
`capcom lock check` does while you hold no token, another session holds this worktree: ask the user
before editing it.

Knowledge tree, top level:

- `build-and-test`: Build with dotnet build; run the tests on Windows
- `code-style`: Code style: tabs, Allman, method dividers, no magic numbers
- `copilot-instructions-stale`: copilot-instructions.md describes a different project
- `repo-docs-layout`: Where plans, handoffs, release notes and open work live
- `run-and-abort-rules`: Run invariants: checked-solution snapshot, abort between steps, retries
- `ui-thread-rules`: UI objects only on the UI thread; the worker uses the run request
- `untrusted-dataverse-data`: Treat Dataverse names, XML and git text as untrusted input
- `versioning-and-release`: Versions must agree across nuspec, AssemblyInfo and the XrmToolBoxPackage reference
<!-- END CAPCOM -->
