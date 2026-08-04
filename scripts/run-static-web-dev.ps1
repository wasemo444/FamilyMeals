<#
.SYNOPSIS
    Serves the standalone Blazor WASM client locally (JWT / Cloudflare Pages profile).

.DESCRIPTION
    Builds LinkNest.Web.Client and serves the publish output with a simple static file host.
    Use this when `dotnet run` on the client project shows a blank page, or for quick local
    testing against the Render Api configured in wwwroot/appsettings.json.

.EXAMPLE
    ./scripts/run-static-web-dev.ps1
    ./scripts/run-static-web-dev.ps1 -Port 5186
#>
param(
    [int]$Port = 5186
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot
$clientProject = Join-Path $repoRoot "src/LinkNest.Web/LinkNest.Web.Client/LinkNest.Web.Client.csproj"
$outputPath = Join-Path $repoRoot "publish/static-web-dev"
$wwwroot = Join-Path $outputPath "wwwroot"

Write-Host "Building static WASM client..."
dotnet publish $clientProject -c Debug -o $outputPath
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

if (-not (Test-Path (Join-Path $wwwroot "index.html"))) {
    throw "Publish did not produce wwwroot/index.html at $wwwroot"
}

Write-Host ""
Write-Host "Static WASM ready at: http://localhost:$Port/"
Write-Host "Press Ctrl+C to stop."
Write-Host ""

if (Get-Command python -ErrorAction SilentlyContinue) {
    python -m http.server $Port --directory $wwwroot
}
elseif (Get-Command py -ErrorAction SilentlyContinue) {
    py -m http.server $Port --directory $wwwroot
}
else {
    throw "Python is required to serve static files. Install Python or use: dotnet run --project `"$clientProject`""
}
