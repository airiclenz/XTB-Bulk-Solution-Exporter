<#
    Builds the plugin in Release and packs it into a NuGet package that the
    XrmToolBox Tool Library accepts.

    Checks before packing:
      - nuspec <version> == AssemblyVersion == AssemblyFileVersion
      - the package for that version does not exist yet
      - the nuspec XrmToolBox dependency == the XrmToolBoxPackage version the plugin is built against
    Checks after packing:
      - the package contains only the plugin DLL under lib\net48\Plugins and the icon

    The package is written to the "Bulk Solution Exporter.nuget" folder.
#>

$ErrorActionPreference = 'Stop'

$projectDir   = Split-Path -Parent $PSScriptRoot
$solutionFile = Join-Path (Split-Path -Parent $projectDir) 'Bulk Solution Exporter.sln'
$nuspecFile   = Join-Path $projectDir 'BulkSolutionExporter.nuspec'
$assemblyInfo = Join-Path $projectDir 'Properties\AssemblyInfo.cs'
$packagesFile = Join-Path $projectDir 'packages.config'
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

[xml] $packages = Get-Content -LiteralPath $packagesFile -Raw
$builtAgainst = ($packages.packages.package | Where-Object { $_.id -eq 'XrmToolBoxPackage' }).version
$declared     = ($nuspec.package.metadata.dependencies.SelectNodes('.//*[local-name()="dependency"]') | Where-Object { $_.id -eq 'XrmToolBox' }).version

if (-not $declared)
{
    Fail "The nuspec does not declare a dependency on XrmToolBox."
}

if ($declared -ne $builtAgainst)
{
    Fail "The nuspec declares XrmToolBox $declared but the plugin is built against XrmToolBoxPackage $builtAgainst (packages.config)."
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

$vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
if (-not (Test-Path -LiteralPath $vswhere))
{
    Fail 'vswhere.exe not found - is Visual Studio / Build Tools installed?'
}

$msbuild = & $vswhere -latest -requires Microsoft.Component.MSBuild -find 'MSBuild\**\Bin\MSBuild.exe' | Select-Object -First 1
if (-not $msbuild)
{
    Fail 'MSBuild not found.'
}


# ---------------------------------------------------------------------------
# Build
# ---------------------------------------------------------------------------

& $nuget restore $solutionFile -NonInteractive
if ($LASTEXITCODE -ne 0) { Fail 'NuGet restore failed.' }

& $msbuild $solutionFile /t:Rebuild /p:Configuration=Release /v:minimal /nologo
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

if (-not ($entries -contains "lib/net48/Plugins/$packageId.dll"))
{
    Remove-Item -LiteralPath $packageFile
    Fail "The package does not contain lib\net48\Plugins\$packageId.dll (package deleted)."
}

Write-Host ''
Write-Host "Created $packageFile" -ForegroundColor Green
$entries | ForEach-Object { Write-Host "  $_" }
