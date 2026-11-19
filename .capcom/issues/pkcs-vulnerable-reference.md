---
title: Plugin references a vulnerable System.Security.Cryptography.Pkcs 9.0.5
status: done
type: bug
priority: 3
labels: [security, build]
created: 2026-11-19T17:21:02Z
closed: 2026-11-19T17:32:44Z
reason: Crypto mirror packages raised to 9.0.23; NU1902/NU1903 gone, tests pass, package content unchanged.
---

The plugin project `Bulk Solution Exporter/Bulk Solution Exporter.csproj` has a direct `PackageReference` to `System.Security.Cryptography.Pkcs` 9.0.5, which NuGet audit flags on every build:

- NU1903 (high): https://github.com/advisories/GHSA-846f-f764-76mx
- NU1902 (moderate): https://github.com/advisories/GHSA-2fmv-rv2c-59x7

The DLL is not shipped: `BulkSolutionExporter.nuspec` packs only the plugin DLL, the core DLL and `System.Resources.Extensions.dll`, so at run time the plugin binds to whatever XrmToolBox itself ships. The reference sits beside `Microsoft.Bcl.Cryptography` 9.0.5 and `System.Formats.Asn1` 9.0.5, which look like the same mirrored set of XrmToolBox host dependencies.

Fix: raise the three 9.0.5 packages to a patched version (or drop references the plugin does not compile against), keeping the Debug host binding redirects working (`AutoGenerateBindingRedirects`, see Note `build-and-test`). Check whether the XrmToolBox host version we require ships a patched Pkcs; if not, note it, since that copy is the one that runs.

## Why

The warning is noise on every build and release and hides new audit findings; a vulnerable reference also invites shipping it by accident if the nuspec ever packs dependencies.

## Acceptance

- `dotnet build "Bulk Solution Exporter.sln" -c Release` reports no NU1902/NU1903 warnings.
- Both test projects pass, and the plugin loads and runs an export in the XrmToolBox debug host.
- `NuGet Pack.ps1` still succeeds and the package content is unchanged.

## Log

- 2026-11-19 Airic Lenz: Raised System.Security.Cryptography.Pkcs, Microsoft.Bcl.Cryptography and System.Formats.Asn1 from 9.0.5 to 9.0.23 (both advisories patched in 9.0.21). Nothing else in the graph depends on them; assembly versions stay 9.0.0.0, so the generated Debug host redirects are unchanged. Release and Debug builds: 0 warnings. Tests: 20/20 plugin, 157/157 core. nuget pack of the nuspec gives the same entries as the released 2026.11.19.30 package. XrmToolBoxPackage 1.2025.10.74 declares no Pkcs dependency and no installed XrmToolBox was found here, so the host's own Pkcs copy was not checked. Live export in the debug host not run.
