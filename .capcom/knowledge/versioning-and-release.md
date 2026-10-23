---
title: Versions must agree across nuspec, AssemblyInfo and the XrmToolBoxPackage reference
labels: [release]
refs: [Bulk Solution Exporter/BulkSolutionExporter.nuspec, Bulk Solution Exporter/Properties/AssemblyInfo.cs#AssemblyFileVersion, Bulk Solution Exporter/Scripts/NuGet Pack.ps1, Bulk Solution Exporter/Scripts/Deploy Locally.bat, Bulk Solution Exporter/Bulk Solution Exporter.csproj, Bulk Solution Exporter.PptbWorker/Bulk Solution Exporter.PptbWorker.csproj, pptb/package.json, pptb/pptb.config.json, pptb/scripts/pack-worker.sh, pptb/scripts/check-worker-version.mjs]
---

- Version format is `YYYY.M.D.N` (e.g. `2026.10.8.29`). It must match in three places: nuspec `<version>`, `AssemblyVersion` and `AssemblyFileVersion` in `Properties/AssemblyInfo.cs`. The nuspec `XrmToolBox` dependency must equal the `XrmToolBoxPackage` `PackageReference` version in `Bulk Solution Exporter.csproj`.
- `Scripts/NuGet Pack.bat` → `NuGet Pack.ps1` does a Release rebuild (`dotnet build -c Release`), enforces those version checks and checks the package contents.
- `Scripts/Deploy Locally.bat` copies the Release DLL, `Com.AiricLenz.BulkSolutionExporter.Core.dll` and `System.Resources.Extensions.dll` into `%APPDATA%\MscrmTools\XrmToolBox\Plugins`.
- Licence is GPL-3.0-only everywhere (LICENSE, README, nuspec, worker `.csproj` `PackageLicenseExpression`), except the PPTB npm package: `pptb/package.json` says `GPL-3.0`, because the approved-licence list of `@pptb/validate` (`APPROVED_LICENSES`) has `GPL-3.0` but not `GPL-3.0-only`.
- Release history lives in capcom: one closed `release` issue per release (`release-YYYY-MM-DD`) with each shipped change as a closed child labelled `release-YYYY-MM-DD`. List one release with `capcom list --all --label release-2026-10-08`. When cutting a release, file its `release-...` issue and move the closed changes under it.
- Minimum host: XrmToolBox 1.2025.10.74.

## Power Platform ToolBox (PPTB) packages

The PPTB tool ships as two packages with their own versions, separate from the plugin's `YYYY.M.D.N`:

- **Worker** (NuGet `AiricLenz.BulkSolutionExporter.PptbWorker`, a .NET tool): `<Version>` in `Bulk Solution Exporter.PptbWorker/Bulk Solution Exporter.PptbWorker.csproj`, 3-part semver (`1.0.0`). It must equal `workers.engine.packageVersion` in `pptb/pptb.config.json`, because PPTB installs exactly that version from nuget.org. `pptb/scripts/check-worker-version.mjs [config] [csproj]` enforces it.
- **Tool UI** (npm `@airiclenz/pptb-bulk-solution-exporter`): `version` in `pptb/package.json`, 3-part semver (`1.0.0`). It moves on its own; a UI-only release keeps the worker version, a worker change bumps both the `.csproj` `<Version>` and `packageVersion`.
- Build: `sh pptb/scripts/pack-worker.sh` (version check + `dotnet pack -c Release` into `artifacts/worker/`), then `npm pack` in `pptb/` (its `prepack` runs the version check and the build). `npm pack --dry-run --json` lists the tarball: `dist/index.html`, `dist/icons/tool.svg`, `pptb.config.json`.
- Publish order: the worker to nuget.org first, and only once nuget.org lists that version, the npm package. An npm package published first names a worker version PPTB cannot download, so the tool fails to start.
