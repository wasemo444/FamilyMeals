<#
.SYNOPSIS
    Applies EF Core migrations to Neon (or any PostgreSQL) for Render Api.

.EXAMPLE
    ./scripts/apply-neon-migrations.ps1
    ./scripts/apply-neon-migrations.ps1 -ConnectionString "Host=...;Database=...;Username=...;Password=..."
#>
param(
    [string]$ConnectionString = ""
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot
$apiProject = Join-Path $repoRoot "src/LinkNest.Api/LinkNest.Api.csproj"

if ([string]::IsNullOrWhiteSpace($ConnectionString)) {
    $ConnectionString = Read-Host "Paste Neon connection string (same as Render ConnectionStrings__DefaultConnection)"
}

if ([string]::IsNullOrWhiteSpace($ConnectionString)) {
    throw "Connection string is required."
}

$env:ConnectionStrings__DefaultConnection = $ConnectionString

Write-Host "Applying migrations..."
dotnet ef database update --project $apiProject
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host ""
Write-Host "Done. Redeploy or restart Render, then confirm logs no longer show DataProtectionKeys missing."
