@echo off

REM Sets the version in BulkSolutionExporter.nuspec and Properties\AssemblyInfo.cs.
REM No argument: today's date and the next build number. Or pass -Version YYYY.M.D.N.

powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Set Version.ps1" %*
exit /b %ERRORLEVEL%
