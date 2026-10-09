@echo off

REM Builds the plugin in Release and packs it into "Bulk Solution Exporter.nuget".
REM Update the version in BulkSolutionExporter.nuspec and Properties\AssemblyInfo.cs first -
REM the script stops if they do not match.

powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0NuGet Pack.ps1"
exit /b %ERRORLEVEL%
