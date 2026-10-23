---
title: Build with dotnet build; run the tests on Windows
labels: [build]
refs: [Bulk Solution Exporter.sln, .vscode/tasks.json#build-debug, Bulk Solution Exporter.Tests/Bulk Solution Exporter.Tests.csproj, Bulk Solution Exporter/Bulk Solution Exporter.csproj, Bulk Solution Exporter/Properties/launchSettings.json]
---


The plugin and its test project are SDK-style .NET Framework 4.8 (`net48`) projects; the shared core library `Bulk Solution Exporter.Core` targets `net48;net8.0` and its test project `Bulk Solution Exporter.Core.Tests` targets `net8.0`. `Microsoft.NETFramework.ReferenceAssemblies` supplies the framework reference assemblies, so the .NET SDK builds them all on Windows, macOS and Linux:

```sh
dotnet build "Bulk Solution Exporter.sln" -c Debug
```

Outputs stay in `Bulk Solution Exporter/bin/Debug/` and `bin/Release/` (no `net48` subfolder). A Debug build also copies the plugin DLL, its pdb and the core DLL to `bin/Debug/Plugins`, and `bin/Debug` holds the XrmToolBox debug host (`XrmToolBox.exe /overridepath:.`, started by `Properties/launchSettings.json` in VS and `.vscode/launch.json` in VS Code).

The `.resx` resources are embedded preserialized (`GenerateResourceUsePreserializedResources`), so `System.Resources.Extensions.dll` must sit next to the plugin DLL at run time (nuspec, `Deploy Locally.bat` and the `build-solution` task ship it).

Tests are an MSTest net48 project (`Bulk Solution Exporter.Tests`), run on Windows:

```sh
dotnet test "Bulk Solution Exporter.Tests"
dotnet test "Bulk Solution Exporter.Tests" --filter "<filter>"
```

On macOS/Linux the tests only build: the RichTextBox tests need Windows.

The core tests (MSTest, `net8.0`, `RollForward Major`) run on every platform:

```sh
dotnet test "Bulk Solution Exporter.Core.Tests"
```

VS Code tasks: `build-debug` (build only), `build-solution` (build + copy the Debug plugin DLL, the core DLL and `System.Resources.Extensions.dll` into `%APPDATA%\MscrmTools\XrmToolBox\Plugins`; Windows only, fails if XrmToolBox has the DLL locked) and `test`.

Live behaviour (export/import against Dataverse, abort, git, settings persistence) can only be checked manually in XrmToolBox.
