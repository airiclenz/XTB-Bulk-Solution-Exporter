<#
    Uploads the package that "NuGet Pack.ps1" built for the current nuspec
    version to nuget.org, where the XrmToolBox Tool Library picks it up.

      .\NuGet Push.ps1             # push, then wait until nuget.org lists the version
      .\NuGet Push.ps1 -DryRun     # every check, no upload
      .\NuGet Push.ps1 -WaitMinutes 0

    The API key is read from the NUGET_API_KEY environment variable (the
    process first, then the user's saved variables, so a key saved with setx
    works without restarting the shell). The key is never printed.

    A version nuget.org already has is not pushed again: the script says so
    and succeeds, so a re-run after a partial failure is safe.

    Exit codes: 0 pushed (or already on nuget.org, or dry run passed), 1 refused or failed.
#>

param(
    [switch] $DryRun,
    [int] $WaitMinutes = 20
)

$ErrorActionPreference = 'Stop'

$projectDir = Split-Path -Parent $PSScriptRoot
$nuspecFile = Join-Path $projectDir 'BulkSolutionExporter.nuspec'
$outputDir  = Join-Path $projectDir 'Bulk Solution Exporter.nuget'

$pushSource        = 'https://api.nuget.org/v3/index.json'
$flatContainerUrl  = 'https://api.nuget.org/v3-flatcontainer/{0}/index.json'
$galleryUrl        = 'https://www.nuget.org/packages/{0}/{1}'
$apiKeyVariable    = 'NUGET_API_KEY'
$pollSeconds       = 30
$httpNotFound      = 404

function Fail([string] $message)
{
    Write-Host "ERROR: $message" -ForegroundColor Red
    exit 1
}


# ---------------------------------------------------------------------------
# Returns the versions nuget.org lists for the package (empty when it has none)
# ---------------------------------------------------------------------------

function Get-PublishedVersions([string] $id)
{
    $url = $flatContainerUrl -f $id.ToLowerInvariant()

    try
    {
        $response = Invoke-RestMethod -Uri $url -UseBasicParsing -Headers @{ 'Cache-Control' = 'no-cache' }
        return @($response.versions)
    }
    catch
    {
        $status = $null
        if ($_.Exception.Response)
        {
            $status = [int] $_.Exception.Response.StatusCode
        }

        if ($status -eq $httpNotFound)
        {
            return @()
        }

        throw
    }
}


# ---------------------------------------------------------------------------
# Package
# ---------------------------------------------------------------------------

[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

[xml] $nuspec = Get-Content -LiteralPath $nuspecFile -Raw -Encoding UTF8
$packageId      = $nuspec.package.metadata.id
$packageVersion = ([version] $nuspec.package.metadata.version).ToString()
$packageFile    = Join-Path $outputDir "$packageId.$packageVersion.nupkg"

if (-not (Test-Path -LiteralPath $packageFile))
{
    Fail "$packageFile does not exist. Run 'NuGet Pack.bat' first."
}

Write-Host "Package: $packageId $packageVersion" -ForegroundColor Cyan
Write-Host "File:    $packageFile"


# ---------------------------------------------------------------------------
# Already published?
# ---------------------------------------------------------------------------

try
{
    $published = Get-PublishedVersions $packageId
}
catch
{
    Fail "Could not ask nuget.org which versions it has: $($_.Exception.Message)"
}

if ($published -contains $packageVersion.ToLowerInvariant())
{
    Write-Host "nuget.org already lists $packageVersion - nothing to push." -ForegroundColor Green
    Write-Host ($galleryUrl -f $packageId, $packageVersion)
    exit 0
}

$latest = if ($published.Count -gt 0) { $published[-1] } else { 'none' }
Write-Host "nuget.org latest: $latest"


# ---------------------------------------------------------------------------
# Tools and key
# ---------------------------------------------------------------------------

$dotnet = (Get-Command 'dotnet' -ErrorAction SilentlyContinue).Source
if (-not $dotnet)
{
    Fail 'dotnet not found - is the .NET SDK installed?'
}

$apiKey = [Environment]::GetEnvironmentVariable($apiKeyVariable, 'Process')
if (-not $apiKey)
{
    $apiKey = [Environment]::GetEnvironmentVariable($apiKeyVariable, 'User')
}

if (-not $apiKey)
{
    Fail ("$apiKeyVariable is not set. Create a push key on https://www.nuget.org/account/apikeys " +
          "(glob pattern $packageId), then run in your own terminal: setx $apiKeyVariable <key>")
}

Write-Host "API key: found in $apiKeyVariable"

if ($DryRun)
{
    Write-Host "Dry run: would push $packageFile to $pushSource" -ForegroundColor Yellow
    exit 0
}


# ---------------------------------------------------------------------------
# Push
# ---------------------------------------------------------------------------

& $dotnet nuget push $packageFile --api-key $apiKey --source $pushSource
if ($LASTEXITCODE -ne 0)
{
    Fail 'dotnet nuget push failed.'
}

Write-Host "Pushed $packageId $packageVersion" -ForegroundColor Green


# ---------------------------------------------------------------------------
# Wait until nuget.org lists it (validation and indexing take a few minutes)
# ---------------------------------------------------------------------------

if ($WaitMinutes -le 0)
{
    exit 0
}

$deadline = (Get-Date).AddMinutes($WaitMinutes)
while ((Get-Date) -lt $deadline)
{
    Start-Sleep -Seconds $pollSeconds

    try
    {
        $published = Get-PublishedVersions $packageId
    }
    catch
    {
        continue
    }

    if ($published -contains $packageVersion.ToLowerInvariant())
    {
        Write-Host "nuget.org lists $packageVersion" -ForegroundColor Green
        Write-Host ($galleryUrl -f $packageId, $packageVersion)
        exit 0
    }
}

Write-Host "WARNING: pushed, but nuget.org did not list $packageVersion within $WaitMinutes minutes. It is usually still validating; check $($galleryUrl -f $packageId, $packageVersion)" -ForegroundColor Yellow
exit 0
