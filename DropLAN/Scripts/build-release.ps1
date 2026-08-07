param(
    [Parameter(Mandatory = $false)]
    [string]$Version = "0.4.0"
)

$ErrorActionPreference = "Stop"

$ProjectRoot = Split-Path -Parent $PSScriptRoot
$PublishDir = Join-Path $ProjectRoot "publish"
$ReleaseDir = Join-Path $ProjectRoot "Releases"

Write-Host "== DropLAN $Version ==" -ForegroundColor Cyan

if (-not (Get-Command vpk -ErrorAction SilentlyContinue)) {
    Write-Host "Instaluję narzędzie Velopack vpk..."
    dotnet tool install -g vpk
}

if (Test-Path $PublishDir) {
    Remove-Item $PublishDir -Recurse -Force
}

dotnet publish "$ProjectRoot\DropLAN.csproj" `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -o $PublishDir

if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish zakończył się błędem."
}

vpk pack `
    --packId DropLAN `
    --packVersion $Version `
    --packDir $PublishDir `
    --mainExe DropLAN.exe `
    --outputDir $ReleaseDir

if ($LASTEXITCODE -ne 0) {
    throw "vpk pack zakończył się błędem."
}

Write-Host ""
Write-Host "Gotowe. Instalator i paczki są w:" -ForegroundColor Green
Write-Host $ReleaseDir
