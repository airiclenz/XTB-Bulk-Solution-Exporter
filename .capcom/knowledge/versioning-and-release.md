---
title: Versions must agree across nuspec, AssemblyInfo and packages.config
labels: [release]
refs: [Bulk Solution Exporter/BulkSolutionExporter.nuspec, Bulk Solution Exporter/Properties/AssemblyInfo.cs#AssemblyFileVersion, Bulk Solution Exporter/Scripts/NuGet Pack.ps1, Bulk Solution Exporter/Scripts/Deploy Locally.bat]
---

- Version format is `YYYY.M.D.N` (e.g. `2026.10.8.29`). It must match in three places: nuspec `<version>`, `AssemblyVersion` and `AssemblyFileVersion` in `Properties/AssemblyInfo.cs`. The nuspec `XrmToolBox` dependency must equal the `XrmToolBoxPackage` version in `packages.config`.
- `Scripts/NuGet Pack.bat` → `NuGet Pack.ps1` does a Release rebuild, enforces those version checks and checks the package contents.
- `Scripts/Deploy Locally.bat` copies the Release DLL into `%APPDATA%\MscrmTools\XrmToolBox\Plugins`.
- Licence is GPL-3.0-only everywhere (LICENSE, README, nuspec).
- Release history lives in capcom: one closed `release` issue per release (`release-YYYY-MM-DD`) with each shipped change as a closed child labelled `release-YYYY-MM-DD`. List one release with `capcom list --all --label release-2026-10-08`. When cutting a release, file its `release-...` issue and move the closed changes under it.
- Minimum host: XrmToolBox 1.2025.10.74.
