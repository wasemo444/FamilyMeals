# Run this script as Administrator (right-click PowerShell -> Run as administrator)
# Fixes phone access when Wi-Fi is on the Public firewall profile.

$ruleName = "ManageFamilyMeals Dev 5299"

Write-Host "Adding firewall rule for Private, Public, and Domain profiles..."
netsh advfirewall firewall delete rule name="$ruleName" 2>$null
netsh advfirewall firewall add rule name="$ruleName" dir=in action=allow protocol=TCP localport=5299 profile=private,public,domain

Write-Host "Setting Wi-Fi network to Private (home network)..."
try {
    Set-NetConnectionProfile -InterfaceAlias "Wi-Fi" -NetworkCategory Private
    Write-Host "Wi-Fi set to Private." -ForegroundColor Green
}
catch {
    Write-Host "Could not change network category (Group Policy may block this). Firewall rule above should still help." -ForegroundColor Yellow
}

Write-Host ""
Write-Host "Done. Start the app with:" -ForegroundColor Cyan
Write-Host '  cd src\ManageFamilyMeals.Web\ManageFamilyMeals.Web'
Write-Host '  dotnet run --urls "http://0.0.0.0:5299"'
Write-Host ""
Write-Host "On your phone (same Wi-Fi): http://192.168.178.21:5299"
