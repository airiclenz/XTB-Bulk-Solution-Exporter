---
title: Build with dotnet build; run the tests on Windows
labels: [build]
refs: [Bulk Solution Exporter.sln, .vscode/tasks.json#build-debug, Bulk Solution Exporter.Tests/Bulk Solution Exporter.Tests.csproj, Bulk Solution Exporter/Bulk Solution Exporter.csproj, Bulk Solution Exporter/Properties/launchSettings.json]
---


Both projects are SDK-style .NET Framework 4.8 (`net48`) projects. `Microsoft.NETFramework.ReferenceAssemblies` supplies the framework reference assemblies, so the .NET SDK builds them on Windows, macOS and Linux:

```sh
dotnet build "Bulk Solution Exporter.sln" -c Debug
```

Outputs stay in `Bulk Solution Exporter/bin/Debug/` and `bin/Release/` (no `net48` subfolder). A Debug build also copies the plugin DLL and pdb to `bin/Debug/Plugins`, and `bin/Debug` holds the XrmToolBox debug host (`XrmToolBox.exe /overridepath:.`, started by `Properties/launchSettings.json` in VS and `.vscode/launch.json` in VS Code).

The `.resx` resources are embedded preserialized (`GenerateResourceUsePreserializedResources`), so `System.Resources.Extensions.dll` must sit next to the plugin DLL at run time (nuspec, `Deploy Locally.bat` and the `build-solution` task ship it).

Tests are an MSTest net48 project (`Bulk Solution Exporter.Tests`), run on Windows:

```sh
dotnet test "Bulk Solution Exporter.Tests"
dotnet test "Bulk Solution Exporter.Tests" --filter "<filter>"
```

On macOS/Linux the tests only build: the GitHelper, FileAndFolderHelper and RichTextBox tests need Windows.

VS Code tasks: `build-debug` (build only), `build-solution` (build + copy the Debug plugin DLL and `System.Resources.Extensions.dll` into `%APPDATA%\MscrmTools\XrmToolBox\Plugins`; Windows only, fails if XrmToolBox has the DLL locked) and `test`.

Live behaviour (export/import against Dataverse, abort, git, settings persistence) can only be checked manually in XrmToolBox.
