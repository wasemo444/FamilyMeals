<#
.SYNOPSIS
    Publishes WASM and deploys to Cloudflare (Workers static assets, SPA mode).

.EXAMPLE
    ./scripts/deploy-cloudflare.ps1 -ApiBaseUrl "https://familymeals-dyrq.onrender.com"
#>
param(
    [string]$ApiBaseUrl = "https://familymeals-dyrq.onrender.com"
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot

& (Join-Path $PSScriptRoot "publish-static-web.ps1") -ApiBaseUrl $ApiBaseUrl

$outputPath = Join-Path $repoRoot "publish/static-web"
$redirectsPath = Join-Path $outputPath "_redirects"
# Workers SPA mode is in wrangler.toml; _redirects conflicts and causes deploy error 100324.
if (Test-Path $redirectsPath) {
    Remove-Item $redirectsPath -Force
    Write-Host "Removed _redirects (Workers uses wrangler.toml SPA mode)."
}

Push-Location $repoRoot
try {
    npx wrangler deploy
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}
finally {
    Pop-Location
}

Write-Host ""
Write-Host "Deployed successfully."
Write-Host "Your URL is shown above (often https://linknestapplication.<account>.workers.dev)."
Write-Host "Set Render Cors__AllowedOrigins and Auth__WebBaseUrl to that exact URL."
