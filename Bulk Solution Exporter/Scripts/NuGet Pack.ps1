<#
    Builds the plugin in Release and packs it into a NuGet package that the
    XrmToolBox Tool Library accepts.

    Checks before packing:
      - nuspec <version> == AssemblyVersion == AssemblyFileVersion
      - the package for that version does not exist yet
      - the nuspec XrmToolBox dependency == the XrmToolBoxPackage PackageReference version in the .csproj
    Checks after packing:
      - the package contains only DLLs under lib\net48\Plugins (the plugin DLL and
        System.Resources.Extensions.dll) and the icon

    Needs the .NET SDK (dotnet) on the PATH; nuget.exe is found or downloaded for packing.

    The package is written to the "Bulk Solution Exporter.nuget" folder.
#>

$ErrorActionPreference = 'Stop'

$projectDir   = Split-Path -Parent $PSScriptRoot
$solutionFile = Join-Path (Split-Path -Parent $projectDir) 'Bulk Solution Exporter.sln'
$nuspecFile   = Join-Path $projectDir 'BulkSolutionExporter.nuspec'
$assemblyInfo = Join-Path $projectDir 'Properties\AssemblyInfo.cs'
$projectFile  = Join-Path $projectDir 'Bulk Solution Exporter.csproj'
$outputDir    = Join-Path $projectDir 'Bulk Solution Exporter.nuget'

function Fail([string] $message)
{
    Write-Host "ERROR: $message" -ForegroundColor Red
    exit 1
}


# ---------------------------------------------------------------------------
# Version checks
# ---------------------------------------------------------------------------

[xml] $nuspec = Get-Content -LiteralPath $nuspecFile -Raw -Encoding UTF8
$packageId      = $nuspec.package.metadata.id
$packageVersion = $nuspec.package.metadata.version

$assemblyText = Get-Content -LiteralPath $assemblyInfo -Raw
$assemblyVersion     = [regex]::Match($assemblyText, '(?m)^\s*\[assembly:\s*AssemblyVersion\("([^"]+)"\)\]').Groups[1].Value
$assemblyFileVersion = [regex]::Match($assemblyText, '(?m)^\s*\[assembly:\s*AssemblyFileVersion\("([^"]+)"\)\]').Groups[1].Value

if (-not $assemblyVersion -or -not $assemblyFileVersion)
{
    Fail "Could not read AssemblyVersion / AssemblyFileVersion from $assemblyInfo"
}

if (([version] $packageVersion -ne [version] $assemblyVersion) -or ([version] $packageVersion -ne [version] $assemblyFileVersion))
{
    Fail ("Version mismatch - nuspec: $packageVersion, AssemblyVersion: $assemblyVersion, AssemblyFileVersion: $assemblyFileVersion. " +
          "XrmToolBox requires the package version to match the plugin assembly version.")
}

$packageFile = Join-Path $outputDir "$packageId.$packageVersion.nupkg"
if (Test-Path -LiteralPath $packageFile)
{
    Fail "$packageFile already exists. Bump the version in the nuspec and AssemblyInfo.cs first."
}

[xml] $project = Get-Content -LiteralPath $projectFile -Raw
$builtAgainst = ($project.SelectNodes('//PackageReference') | Where-Object { $_.Include -eq 'XrmToolBoxPackage' }).Version
$declared     = ($nuspec.package.metadata.dependencies.SelectNodes('.//*[local-name()="dependency"]') | Where-Object { $_.id -eq 'XrmToolBox' }).version

if (-not $declared)
{
    Fail "The nuspec does not declare a dependency on XrmToolBox."
}

if ($declared -ne $builtAgainst)
{
    Fail "The nuspec declares XrmToolBox $declared but the plugin is built against XrmToolBoxPackage $builtAgainst (Bulk Solution Exporter.csproj)."
}

Write-Host "Packing $packageId $packageVersion (XrmToolBox $declared)" -ForegroundColor Cyan


# ---------------------------------------------------------------------------
# Tools
# ---------------------------------------------------------------------------

$nuget = (Get-Command 'nuget.exe' -ErrorAction SilentlyContinue).Source
if (-not $nuget)
{
    $nuget = Join-Path $PSScriptRoot 'nuget.exe'
    if (-not (Test-Path -LiteralPath $nuget))
    {
        Write-Host 'nuget.exe not found - downloading it to the Scripts folder'
        [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
        Invoke-WebRequest -Uri 'https://dist.nuget.org/win-x86-commandline/latest/nuget.exe' -OutFile $nuget -UseBasicParsing
    }
}

$dotnet = (Get-Command 'dotnet' -ErrorAction SilentlyContinue).Source
if (-not $dotnet)
{
    Fail 'dotnet not found - is the .NET SDK installed?'
}


# ---------------------------------------------------------------------------
# Build
# ---------------------------------------------------------------------------

& $dotnet build $solutionFile -c Release --no-incremental -v minimal -nologo
if ($LASTEXITCODE -ne 0) { Fail 'Release build failed.' }


# ---------------------------------------------------------------------------
# Pack - from the nuspec only, so no project content files end up in the package
# ---------------------------------------------------------------------------

New-Item -ItemType Directory -Force -Path $outputDir | Out-Null

& $nuget pack $nuspecFile -BasePath $projectDir -OutputDirectory $outputDir -NonInteractive
if ($LASTEXITCODE -ne 0) { Fail 'NuGet pack failed.' }


# ---------------------------------------------------------------------------
# Verify the package content
# ---------------------------------------------------------------------------

Add-Type -AssemblyName System.IO.Compression.FileSystem
$zip = [IO.Compression.ZipFile]::OpenRead($packageFile)
try
{
    $entries = $zip.Entries | ForEach-Object { $_.FullName }
}
finally
{
    $zip.Dispose()
}

$unexpected = $entries | Where-Object {
    $_ -notmatch '^lib/net48/Plugins/[^/]+\.dll$' -and
    $_ -ne 'images/icon.png' -and
    $_ -ne "$packageId.nuspec" -and
    $_ -ne '[Content_Types].xml' -and
    $_ -notmatch '^_rels/' -and
    $_ -notmatch '^package/services/metadata/'
}

if ($unexpected)
{
    Remove-Item -LiteralPath $packageFile
    Fail ("The package contains files outside lib\net48\Plugins (package deleted):`n  " + ($unexpected -join "`n  "))
}

foreach ($requiredDll in @("$packageId.dll", 'System.Resources.Extensions.dll'))
{
    if (-not ($entries -contains "lib/net48/Plugins/$requiredDll"))
    {
        Remove-Item -LiteralPath $packageFile
        Fail "The package does not contain lib\net48\Plugins\$requiredDll (package deleted)."
    }
}

Write-Host ''
Write-Host "Created $packageFile" -ForegroundColor Green
$entries | ForEach-Object { Write-Host "  $_" }
