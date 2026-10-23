

copy "%~dp0..\bin\Release\Com.AiricLenz.XTB.Plugin.BulkSolutionExporter.dll" "%appdata%\MscrmTools\XrmToolBox\Plugins\Com.AiricLenz.XTB.Plugin.BulkSolutionExporter.dll" /y
if errorlevel 1 exit /b 1
copy "%~dp0..\bin\Release\Com.AiricLenz.BulkSolutionExporter.Core.dll" "%appdata%\MscrmTools\XrmToolBox\Plugins\Com.AiricLenz.BulkSolutionExporter.Core.dll" /y
if errorlevel 1 exit /b 1
copy "%~dp0..\bin\Release\System.Resources.Extensions.dll" "%appdata%\MscrmTools\XrmToolBox\Plugins\System.Resources.Extensions.dll" /y
if errorlevel 1 exit /b 1
