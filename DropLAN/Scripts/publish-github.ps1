param(
    [Parameter(Mandatory = $true)]
    [string]$RepoUrl,

    [Parameter(Mandatory = $true)]
    [string]$Version,

    [Parameter(Mandatory = $true)]
    [string]$GitHubToken
)

$ErrorActionPreference = "Stop"

$ProjectRoot = Split-Path -Parent $PSScriptRoot
$ReleaseDir = Join-Path $ProjectRoot "Releases"

if (-not (Test-Path $ReleaseDir)) {
    throw "Brak katalogu Releases. Najpierw uruchom build-release.ps1."
}

vpk upload github `
    --outputDir $ReleaseDir `
    --repoUrl $RepoUrl `
    --token $GitHubToken `
    --publish `
    --releaseName "DropLAN $Version" `
    --tag "v$Version"

if ($LASTEXITCODE -ne 0) {
    throw "Publikacja na GitHubie zakończyła się błędem."
}

Write-Host "Release v$Version opublikowany." -ForegroundColor Green
