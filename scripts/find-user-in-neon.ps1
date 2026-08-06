<#
.SYNOPSIS
    Lists Identity users in Neon using the same connection string as Render.

.EXAMPLE
    ./scripts/find-user-in-neon.ps1 -Email "mwasim.alkurdi@gmail.com"
#>
param(
    [string]$Email = "",
    [string]$ConnectionString = ""
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot

if ([string]::IsNullOrWhiteSpace($ConnectionString)) {
    $ConnectionString = Read-Host "Paste Render ConnectionStrings__DefaultConnection (Neon)"
}

if ([string]::IsNullOrWhiteSpace($ConnectionString)) {
    throw "Connection string is required."
}

$search = if ([string]::IsNullOrWhiteSpace($Email)) { "%" } else { "%$($Email.Trim())%" }
$normalized = if ([string]::IsNullOrWhiteSpace($Email)) { "%" } else { $Email.Trim().ToUpperInvariant() }

$sql = @"
SELECT COUNT(*) AS total_users FROM "AspNetUsers";

SELECT "Id", "UserName", "Email", "NormalizedEmail", "EmailConfirmed", "IsActive", "CreatedAtUtc"
FROM "AspNetUsers"
WHERE "Email" ILIKE '$search'
   OR "NormalizedEmail" = '$normalized'
   OR "UserName" ILIKE '$search'
ORDER BY "CreatedAtUtc" DESC;
"@

Write-Host "Checking database from connection string..."
Write-Host ""

$env:ConnectionStrings__DefaultConnection = $ConnectionString
dotnet ef dbcontext info --project (Join-Path $repoRoot "src/LinkNest.Api/LinkNest.Api.csproj") 2>&1 | Select-String "Provider|Database"

Write-Host ""
Write-Host "Run this SQL in Neon SQL Editor (same connection string as Render):"
Write-Host $sql
Write-Host ""
Write-Host "Also compare with Render API:"
Write-Host "  curl.exe -s https://familymeals-dyrq.onrender.com/health/db"
Write-Host ""
Write-Host "If userCount > 0 but Neon shows 0 rows, you are querying a different Neon branch/database than Render."
