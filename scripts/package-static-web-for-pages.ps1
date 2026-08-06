<#
.SYNOPSIS
    Publishes the static WASM client and creates a Cloudflare-ready zip.

.EXAMPLE
    ./scripts/package-static-web-for-pages.ps1 -ApiBaseUrl "https://familymeals-dyrq.onrender.com"
#>
param(
    [string]$ApiBaseUrl = "https://familymeals-dyrq.onrender.com",
    [string]$OutputPath = "",
    [string]$ZipPath = ""
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot

if ([string]::IsNullOrWhiteSpace($OutputPath)) {
    $OutputPath = Join-Path $repoRoot "publish/static-web"
}

if ([string]::IsNullOrWhiteSpace($ZipPath)) {
    $ZipPath = Join-Path $repoRoot "publish/linknest-pages.zip"
}

& (Join-Path $PSScriptRoot "publish-static-web.ps1") -ApiBaseUrl $ApiBaseUrl -OutputPath $OutputPath

if (-not (Test-Path (Join-Path $OutputPath "index.html"))) {
    throw "Expected index.html at $OutputPath"
}

if (Test-Path $ZipPath) {
    Remove-Item $ZipPath -Force
}

$zipTemp = Join-Path $env:TEMP ("linknest-pages-" + [Guid]::NewGuid().ToString("n"))
New-Item -ItemType Directory -Path $zipTemp -Force | Out-Null
Copy-Item -Path (Join-Path $OutputPath "*") -Destination $zipTemp -Recurse -Force
Compress-Archive -Path (Join-Path $zipTemp "*") -DestinationPath $ZipPath -Force
Remove-Item $zipTemp -Recurse -Force

Write-Host ""
Write-Host "Upload this zip in Cloudflare:"
Write-Host "  Dashboard -> Workers & Pages -> Create -> Pages tab -> Upload assets"
Write-Host ""
Write-Host "Zip: $ZipPath"
Write-Host "(index.html is at the root of the zip)"
