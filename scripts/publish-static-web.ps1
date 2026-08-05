<#
.SYNOPSIS
    Publishes LinkNest.Web.Client as standalone Blazor WASM for Cloudflare Pages upload.

.PARAMETER ApiBaseUrl
    Render (or local) Api URL baked into wwwroot/appsettings.json at publish time.
    Example: https://linknest-api.onrender.com

.PARAMETER OutputPath
    Directory for the Cloudflare-ready site root (default: ./publish/static-web).
    Contains index.html at the top level — upload this folder's contents to Pages.

.EXAMPLE
    ./scripts/publish-static-web.ps1 -ApiBaseUrl "https://familymeals-dyrq.onrender.com"
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

$stagingPath = Join-Path ([System.IO.Path]::GetTempPath()) ("linknest-static-web-" + [Guid]::NewGuid().ToString("n"))

function Publish-Client {
    param([string]$PublishOutput)

    $publishArgs = @(
        "publish", $clientProject,
        "-c", "Release",
        "-o", $PublishOutput
    )

    & dotnet @publishArgs
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}

function Copy-SiteRoot {
    param(
        [string]$SourceWwwroot,
        [string]$Destination
    )

    if (-not (Test-Path (Join-Path $SourceWwwroot "index.html"))) {
        throw "Publish did not produce wwwroot/index.html at $SourceWwwroot"
    }

    if (Test-Path $Destination) {
        Remove-Item $Destination -Recurse -Force
    }

    New-Item -ItemType Directory -Path $Destination -Force | Out-Null
    Copy-Item -Path (Join-Path $SourceWwwroot "*") -Destination $Destination -Recurse -Force
}

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
        Publish-Client -PublishOutput $stagingPath
        Copy-SiteRoot -SourceWwwroot (Join-Path $stagingPath "wwwroot") -Destination $OutputPath
    }
    finally {
        if ($null -ne $appsettingsBackup) {
            Set-Content $appsettingsPath $appsettingsBackup -Encoding utf8 -NoNewline
        }
        if (Test-Path $stagingPath) {
            Remove-Item $stagingPath -Recurse -Force -ErrorAction SilentlyContinue
        }
    }
}
else {
    try {
        Publish-Client -PublishOutput $stagingPath
        Copy-SiteRoot -SourceWwwroot (Join-Path $stagingPath "wwwroot") -Destination $OutputPath
    }
    finally {
        if (Test-Path $stagingPath) {
            Remove-Item $stagingPath -Recurse -Force -ErrorAction SilentlyContinue
        }
    }
}

Write-Host ""
Write-Host "Static web published to: $OutputPath"
Write-Host "Cloudflare Pages: upload the contents of that folder (index.html at the root)."
Write-Host "Git-connected Pages build output directory: publish/static-web"
if (-not [string]::IsNullOrWhiteSpace($ApiBaseUrl)) {
    Write-Host "ApiBaseUrl: $ApiBaseUrl"
}
