<#
.SYNOPSIS
    Publishes LinkNest.Web.Client as standalone Blazor WASM for Cloudflare Pages upload.

.PARAMETER ApiBaseUrl
    Render (or local) Api URL baked into wwwroot/appsettings.json at publish time.
    Example: https://linknest-api.onrender.com

.PARAMETER OutputPath
    Directory for publish output (default: ./publish/static-web).

.EXAMPLE
    ./scripts/publish-static-web.ps1 -ApiBaseUrl "https://linknest-api.onrender.com"
#>
param(
    [string]$ApiBaseUrl = "",
    [string]$OutputPath = ""
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot
$clientProject = Join-Path $repoRoot "src/LinkNest.Web/LinkNest.Web.Client/LinkNest.Web.Client.csproj"
$appsettingsPath = Join-Path $repoRoot "src/LinkNest.Web/LinkNest.Web.Client/wwwroot/appsettings.json"

if ([string]::IsNullOrWhiteSpace($OutputPath)) {
    $OutputPath = Join-Path $repoRoot "publish/static-web"
}

$publishArgs = @(
    "publish", $clientProject,
    "-c", "Release",
    "-o", $OutputPath
)

if (-not [string]::IsNullOrWhiteSpace($ApiBaseUrl)) {
    $normalized = $ApiBaseUrl.TrimEnd('/')
    $appsettingsBackup = $null
    if (Test-Path $appsettingsPath) {
        $appsettingsBackup = Get-Content $appsettingsPath -Raw
        $json = Get-Content $appsettingsPath -Raw | ConvertFrom-Json
        $json.ApiBaseUrl = $normalized
        $json | ConvertTo-Json -Depth 4 | Set-Content $appsettingsPath -Encoding utf8
    }

    try {
        & dotnet @publishArgs
        if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    }
    finally {
        if ($null -ne $appsettingsBackup) {
            Set-Content $appsettingsPath $appsettingsBackup -Encoding utf8 -NoNewline
        }
    }
}
else {
    & dotnet @publishArgs
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}

Write-Host ""
Write-Host "Static web published to: $OutputPath"
Write-Host "Upload the contents of wwwroot (or publish folder) to Cloudflare Pages."
if (-not [string]::IsNullOrWhiteSpace($ApiBaseUrl)) {
    Write-Host "ApiBaseUrl: $ApiBaseUrl"
}
