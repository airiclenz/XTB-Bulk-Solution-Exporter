@echo off

REM Uploads the package NuGet Pack.bat built for the current nuspec version to nuget.org.
REM Needs the NUGET_API_KEY environment variable. Pass -DryRun to check without uploading.

powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0NuGet Push.ps1" %*
exit /b %ERRORLEVEL%
