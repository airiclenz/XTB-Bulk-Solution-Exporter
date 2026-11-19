---
name: release-xtb
description: Cut and publish an XrmToolBox plugin release end to end — gate check, release notes, version confirmation, version bump and package, pre-publish checks, then push master and the tag and create the GitHub release, whose workflow uploads the package to nuget.org via Trusted Publishing. Use for "cut a release", "publish a release", "push a new nuget version". Optional argument YYYY.M.D.N proposes the version.
---

Read `.agents/skills/release-xtb/SKILL.md` and follow it exactly. That file is the canonical skill; this one only points Claude Code at it. Pass on any argument you were given.
