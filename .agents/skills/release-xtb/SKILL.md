---
name: release-xtb
description: >
  Cuts and publishes a release of the XrmToolBox plugin: gate check (clean
  master, build, tests), release notes drafted from the commits and closed
  capcom issues since the last Release_v tag, a version the owner confirms,
  local release commits (version bump, package, capcom release issue), then
  after a dry run and one explicit confirmation pushes master and the tag,
  creates the GitHub release with the .nupkg and .dll attached, and uploads
  the package to nuget.org, where the XrmToolBox Tool Library picks it up.
  Use for "cut a release", "publish a release", "release to XrmToolBox",
  "push a new nuget version". Optional argument YYYY.M.D.N proposes the
  version. Not for the PPTB tool (its worker and npm packages ship apart).
---

# Release the XrmToolBox plugin

## Mission

Turn everything shipped since the last `Release_v*` tag into a published
plugin release: notes the owner approved, a version the owner confirmed, a
GitHub release, and the package on nuget.org. The deterministic work belongs
to the scripts in `Bulk Solution Exporter/Scripts/`:

| Script | Does |
|---|---|
| `Set Version.ps1 [-Version V] [-WhatIf]` | sets V in the nuspec, `AssemblyVersion` and `AssemblyFileVersion`; no V = today's date + build number + 1; refuses V not above the current one |
| `NuGet Pack.ps1` | Release rebuild, version and XrmToolBox-dependency checks, packs into `Bulk Solution Exporter.nuget/`, checks the package content |
| `NuGet Push.ps1 [-DryRun] [-WaitMinutes N]` | pushes that package to nuget.org with `NUGET_API_KEY`, then waits until nuget.org lists it; a version nuget.org already has is skipped and succeeds |

This skill does the judgment and the asking. Background:
`capcom know show versioning-and-release`.

In this repository this skill replaces the personal `cut-release` skill.

## Boundaries

- **Irreversible steps are in step 7 only**: pushing master and the tag,
  the GitHub release, the nuget.org upload. nuget.org never deletes a
  version (it can only be unlisted), so a broken release is fixed with a new
  version, never by reusing one.
- **Ask with the host's user-question tool** (`AskUserQuestion` or similar)
  at every *Ask* and wait. No answer, or no such tool, means stop.
- **The API key is the owner's.** Never print, echo, log or write
  `NUGET_API_KEY`, and never ask the owner to paste it into the chat or run
  `! setx …` here (that puts the key in the transcript). They set it in their
  own terminal: `setx NUGET_API_KEY <key>`. `NuGet Push.ps1` also reads the
  user-level variable, so no restart is needed.
- PowerShell 5.1 runs the scripts: call them from the PowerShell tool with
  `& ".\Bulk Solution Exporter\Scripts\<name>.ps1"` from the repo root and
  read `$LASTEXITCODE`.
- Commits follow the owner's global rules: no AI attribution lines or
  trailers.
- The PPTB tool (`pptb/`, `Bulk Solution Exporter.PptbWorker/`) is not part
  of this release: leave its version files alone and its changes out of the
  notes.

## 1 — Gate (stop on any failure)

From the repo root:

```bash
git fetch origin --tags
git rev-parse --abbrev-ref HEAD                  # must be master
git status --porcelain                           # must be empty
git rev-list --left-right --count origin/master...HEAD   # left (behind) must be 0
LAST=$(git tag --list 'Release_v*' --sort=-creatordate | head -n 1); echo "LAST=$LAST"
git log --oneline "$LAST"..HEAD | wc -l          # 0 → nothing to release, stop
gh auth status
```

Then build and test (Windows; give it a long timeout):

```bash
dotnet test "Bulk Solution Exporter.Tests" -v minimal
dotnet test "Bulk Solution Exporter.Core.Tests" -v minimal
```

Any failure → **stop** and relay it verbatim; fixing it is the owner's work.
Print one ledger line: last tag and its date, commits since, current version.

## 2 — Inventory what shipped

Sources, both since `LAST`:

- `git log --format='%h %s' "$LAST"..HEAD` and, where a subject is unclear,
  `git show --stat <sha>`.
- Closed capcom issues not yet filed under a release:
  `capcom list --all --category done --no-elsewhere --json` → keep rows whose
  `path` is not below `.capcom/issues/release-*` and whose `type` is not
  `release`. Read each with `capcom show <id>`.

Classify every change:

- **Plugin-facing**: changes what an XrmToolBox user sees or gets (plugin UI,
  export/import/git behaviour, the core library the plugin runs, packaging,
  the minimum XrmToolBox version). These go in the notes.
- **PPTB-only**: touches only `pptb/` or `Bulk Solution Exporter.PptbWorker*`.
  Left out of the notes and out of the release issue.
- **Internal**: refactors, tests, docs, plans, issues, build tidying with no
  user-visible effect. Left out of the notes; a closed capcom issue of this
  kind still moves under the release issue.

Keep the list of capcom issue ids that move under the release issue
(plugin-facing and internal, not PPTB-only).

## 3 — Draft and approve the notes

Write `<scratchpad>/release-xtb-notes.md` in the house style of the last
release (`gh release view "$LAST" --json body --jq .body`):

- one opening sentence that sums the release up for a user;
- then only the sections that have entries, in this order:
  `## Breaking changes`, `## New`, `## Improved`, `## Fixed`;
- each entry `- **Short lead** — one or two plain sentences on what the user
  notices`; a breaking entry adds a line `**What to do:** …`;
- user language, no class names, commit hashes or issue ids; a raised
  minimum XrmToolBox version (nuspec `XrmToolBox` dependency changed since
  `LAST`) is always a breaking change.

Show the notes in full. *Ask*: approve, request changes, or abort. On
changes, edit the file, show the changed lines, *Ask* again. Abort → stop;
nothing has been written to the repository.

## 4 — Confirm the version

```powershell
& ".\Bulk Solution Exporter\Scripts\Set Version.ps1" -WhatIf
```

It prints `current -> proposed` (today's date, build number + 1). An argument
version replaces the proposal. Derived names, with month and day
zero-padded:

- tag `Release_vYYYY.MM.DD`, release title `Release YYYY.MM.DD`;
- release commit `Release YYYY.MM.DD.N`; capcom issue and label
  `release-YYYY-MM-DD`.

If the tag already exists on origin (a second release the same day) → say
so and *Ask* for another date or version. *Ask* the owner to confirm the
version, the proposal first plus a free-text choice; the answer is final.

## 5 — Prepare (local only)

1. `& ".\Bulk Solution Exporter\Scripts\Set Version.ps1" -Version <V>`
2. `& ".\Bulk Solution Exporter\Scripts\NuGet Pack.ps1"` — a failure stops
   the run; undo with `git checkout -- "Bulk Solution Exporter/BulkSolutionExporter.nuspec" "Bulk Solution Exporter/Properties/AssemblyInfo.cs"`.
3. Release bookkeeping in capcom (`R` = `release-YYYY-MM-DD`):
   ```bash
   capcom create --id "$R" --title "Release YYYY.MM.DD" --type release --priority 2 \
     --body "Release YYYY.MM.DD. <one line: the headline change, and the minimum XrmToolBox version if it changed>

   The changes shipped in it are the child issues (\`capcom list --all --label $R\`)."
   # for each issue id from step 2:
   capcom mv <id> --parent "$R"
   capcom update <id> --add-label "$R"
   capcom close "$R" --reason "Released YYYY.MM.DD."
   capcom check
   ```
4. Two commits:
   - `chore(issues): file release YYYY-MM-DD` — the `.capcom/` changes;
   - `Release YYYY.MM.DD.N` — the nuspec, `AssemblyInfo.cs` and the new
     `.nupkg` (the repo still commits packages; see issue
     `committed-nupkg-artifacts`).

Undo, before step 7 only: `git reset --keep HEAD~2`, then delete the new
`.nupkg` if it is untracked.

## 6 — Dry run, then the one confirmation

```powershell
& ".\Bulk Solution Exporter\Scripts\NuGet Push.ps1" -DryRun
```

```bash
git ls-remote --tags origin "Release_vYYYY.MM.DD"     # must be empty
gh release view "Release_vYYYY.MM.DD" 2>&1 | head -1  # must say release not found
ls "Bulk Solution Exporter/bin/Release/Com.AiricLenz.XTB.Plugin.BulkSolutionExporter.dll"
```

`NUGET_API_KEY is not set` → tell the owner how to create a key
(nuget.org → API keys → Create, scope *Push new packages and package
versions*, glob `Com.AiricLenz.XTB.Plugin.BulkSolutionExporter`) and to run
`setx NUGET_API_KEY <key>` **in their own terminal**; wait, then re-run the
dry run. Never continue without a passing dry run.

Show the dry-run output and the plan, then *Ask* exactly once:
"Publish YYYY.MM.DD.N? This pushes master and tag Release_vYYYY.MM.DD,
creates the GitHub release with the package attached and uploads it to
nuget.org. It cannot be undone." — options *publish* and *abort*. Anything
but *publish* is abort (see step 5's undo).

## 7 — Publish

Run in order; each step is safe to re-run, so after a failure fix the cause
and re-run from the failed step without asking again.

```bash
git push origin master
git rev-parse -q --verify "refs/tags/Release_vYYYY.MM.DD" >/dev/null || \
  git tag -a "Release_vYYYY.MM.DD" -m "Release YYYY.MM.DD"
git push origin "Release_vYYYY.MM.DD"
gh release view "Release_vYYYY.MM.DD" >/dev/null 2>&1 || \
  gh release create "Release_vYYYY.MM.DD" --title "Release YYYY.MM.DD" --latest \
    --notes-file "<scratchpad>/release-xtb-notes.md" \
    "Bulk Solution Exporter/Bulk Solution Exporter.nuget/Com.AiricLenz.XTB.Plugin.BulkSolutionExporter.<V>.nupkg" \
    "Bulk Solution Exporter/bin/Release/Com.AiricLenz.XTB.Plugin.BulkSolutionExporter.dll"
```

```powershell
& ".\Bulk Solution Exporter\Scripts\NuGet Push.ps1"
```

The push waits up to 20 minutes for nuget.org to list the version; give the
call a long timeout or run it in the background. A WARNING that it is not
listed yet is not a failure: nuget.org is still validating.

Never move or re-create a pushed tag, and never `--force` anything.

## Report

- version, tag, GitHub release URL, nuget.org package URL;
- each step's outcome: master pushed, tag pushed, release created, nuget.org
  pushed and listed (or still validating);
- XrmToolBox Tool Library: it reads nuget.org on its own schedule, so the
  update can take a while to appear; if it has not after a day, the owner
  checks the plugin on the XrmToolBox portal;
- the capcom release issue id and how many issues moved under it.
