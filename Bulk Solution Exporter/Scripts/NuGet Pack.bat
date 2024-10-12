@echo off

cls
ECHO This will pack the compiled code into a new nuget package
ECHO :::::::::::::::::::::::::::::::::::::::::::::::::::::::::
ECHO.

setlocal
:PROMPT
SET /P AREYOUSURE=Did you update the version number in the '*.nuspec' file? Y/[N] 
IF /I "%AREYOUSURE%" NEQ "Y" GOTO END

cd ..

Scripts\\nuget.exe pack -Prop Configuration=Release

copy "Com.AiricLenz.XTB.Plugin.BulkSolutionExporter.*.nupkg" "Bulk Solution Exporter.nuget" /Y
del "Com.AiricLenz.XTB.Plugin.BulkSolutionExporter.*.nupkg"


:END
endlocal