---
title: Build with VS MSBuild and test with vstest, not dotnet
labels: [build]
refs: [Bulk Solution Exporter.sln, .vscode/tasks.json#build-debug, Bulk Solution Exporter.Tests/Bulk Solution Exporter.Tests.csproj]
---

The main project is a pre-SDK-style .NET Framework 4.8 `.csproj`. The .NET SDK (`dotnet build`) cannot compile it (MSB3822/MSB3823 at resource generation). Use Visual Studio's MSBuild, found via `vswhere`:

```powershell
$m = & 'C:\Program Files (x86)\Microsoft Visual Studio\Installer\vswhere.exe' -latest -requires Microsoft.Component.MSBuild -find 'MSBuild\**\Bin\MSBuild.exe' | Select-Object -First 1
& $m 'Bulk Solution Exporter.sln' /restore /p:RestorePackagesConfig=true /p:Configuration=Debug /v:minimal
```

Tests are an SDK-style net48 MSTest project (`Bulk Solution Exporter.Tests`), built by the same MSBuild call and run with `vstest.console.exe`:

```powershell
$t = & 'C:\Program Files (x86)\Microsoft Visual Studio\Installer\vswhere.exe' -latest -find '**\vstest.console.exe' | Select-Object -First 1
& $t 'Bulk Solution Exporter.Tests\bin\Debug\net48\Com.AiricLenz.XTB.Plugin.BulkSolutionExporter.Tests.dll' '/TestCaseFilter:<filter>'
```

VS Code tasks: `build-debug` (build only) and `build-solution` (build + copy the Debug DLL into `%APPDATA%\MscrmTools\XrmToolBox\Plugins`; fails if XrmToolBox has the DLL locked).

Live behaviour (export/import against Dataverse, abort, git, settings persistence) can only be checked manually in XrmToolBox.
