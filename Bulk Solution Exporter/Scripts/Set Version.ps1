<#
    Sets the plugin version in the three places XrmToolBox needs to agree:
    the nuspec <version>, AssemblyVersion and AssemblyFileVersion.

    The version format is YYYY.M.D.N: the release date and a running build
    number that goes up by one with every release.

      .\Set Version.ps1                      # today's date, build number + 1
      .\Set Version.ps1 -Version 2026.11.19.30
      .\Set Version.ps1 -WhatIf              # print the change, write nothing

    Refuses a version that is not above the current one.
#>

[CmdletBinding(SupportsShouldProcess = $true)]
param(
    [string] $Version
)

$ErrorActionPreference = 'Stop'

$projectDir   = Split-Path -Parent $PSScriptRoot
$nuspecFile   = Join-Path $projectDir 'BulkSolutionExporter.nuspec'
$assemblyInfo = Join-Path $projectDir 'Properties\AssemblyInfo.cs'

$versionPattern         = '^\d{4}\.\d{1,2}\.\d{1,2}\.\d+$'
$nuspecVersionPattern   = '(<version>)([^<]+)(</version>)'
$assemblyVersionPattern = '(?m)^(\s*\[assembly:\s*Assembly(?:File)?Version\(")([^"]+)("\)\])'

function Fail([string] $message)
{
    Write-Host "ERROR: $message" -ForegroundColor Red
    exit 1
}


# ---------------------------------------------------------------------------
# Reads and writes a text file, keeping its encoding (BOM or not) and line endings
# ---------------------------------------------------------------------------

function Read-TextFile([string] $path)
{
    return [IO.File]::ReadAllText($path)
}

function Write-TextFile([string] $path, [string] $text)
{
    $bytes  = [IO.File]::ReadAllBytes($path)
    $hasBom = ($bytes.Length -ge 3) -and ($bytes[0] -eq 0xEF) -and ($bytes[1] -eq 0xBB) -and ($bytes[2] -eq 0xBF)
    [IO.File]::WriteAllText($path, $text, (New-Object Text.UTF8Encoding($hasBom)))
}


# ---------------------------------------------------------------------------
# Current version
# ---------------------------------------------------------------------------

$nuspecText   = Read-TextFile $nuspecFile
$assemblyText = Read-TextFile $assemblyInfo

$nuspecMatch = [regex]::Match($nuspecText, $nuspecVersionPattern)
if (-not $nuspecMatch.Success)
{
    Fail "Could not read <version> from $nuspecFile"
}

$assemblyMatches = [regex]::Matches($assemblyText, $assemblyVersionPattern)
if ($assemblyMatches.Count -ne 2)
{
    Fail "Expected AssemblyVersion and AssemblyFileVersion in $assemblyInfo, found $($assemblyMatches.Count) attribute(s)."
}

$current = $nuspecMatch.Groups[2].Value
foreach ($match in $assemblyMatches)
{
    if ($match.Groups[2].Value -ne $current)
    {
        Fail "Versions disagree before the change - nuspec: $current, AssemblyInfo: $($match.Groups[2].Value). Fix them by hand first."
    }
}


# ---------------------------------------------------------------------------
# New version
# ---------------------------------------------------------------------------

if (-not $Version)
{
    $build   = ([version] $current).Revision + 1
    $today   = Get-Date
    $Version = '{0}.{1}.{2}.{3}' -f $today.Year, $today.Month, $today.Day, $build
}

if ($Version -notmatch $versionPattern)
{
    Fail "$Version is not in the format YYYY.M.D.N (no leading zeros needed, e.g. 2026.11.19.30)."
}

$Version = ([version] $Version).ToString()

if ([version] $Version -le [version] $current)
{
    Fail "$Version is not above the current version $current."
}

Write-Host "Version: $current -> $Version" -ForegroundColor Cyan

if (-not $PSCmdlet.ShouldProcess("$nuspecFile and $assemblyInfo", "set version $Version"))
{
    exit 0
}


# ---------------------------------------------------------------------------
# Write
# ---------------------------------------------------------------------------

$nuspecText   = ([regex] $nuspecVersionPattern).Replace($nuspecText, ('${1}' + $Version + '${3}'), 1)
$assemblyText = [regex]::Replace($assemblyText, $assemblyVersionPattern, ('${1}' + $Version + '${3}'))

Write-TextFile $nuspecFile $nuspecText
Write-TextFile $assemblyInfo $assemblyText

Write-Host "Set $Version in BulkSolutionExporter.nuspec and Properties\AssemblyInfo.cs" -ForegroundColor Green
