# Stops Manage Family Meals dev server (port 5299)
$connections = Get-NetTCPConnection -LocalPort 5299 -State Listen -ErrorAction SilentlyContinue
if ($connections) {
    $connections | ForEach-Object {
        $processId = $_.OwningProcess
        Write-Host "Stopping process $processId on port 5299..."
        Stop-Process -Id $processId -Force -ErrorAction SilentlyContinue
    }
    Start-Sleep -Seconds 2
}

Get-Process -Name "ManageFamilyMeals.Web" -ErrorAction SilentlyContinue | ForEach-Object {
    Write-Host "Stopping $($_.ProcessName) ($($_.Id))..."
    Stop-Process -Id $_.Id -Force -ErrorAction SilentlyContinue
}

if (Get-NetTCPConnection -LocalPort 5299 -State Listen -ErrorAction SilentlyContinue) {
    Write-Host "Port 5299 is still in use." -ForegroundColor Red
    exit 1
}

Write-Host "Port 5299 is free. You can run dotnet run now." -ForegroundColor Green
