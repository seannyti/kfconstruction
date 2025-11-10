Write-Host "KfConstruction Balanced Launcher" -ForegroundColor Green
Write-Host ""

Write-Host "Starting API with hot reload..." -ForegroundColor Cyan
Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd '$PSScriptRoot'; dotnet watch --project KfConstructionAPI\KfConstructionAPI.csproj --launch-profile https"

Write-Host "Waiting for API to initialize..." -ForegroundColor Yellow
Start-Sleep -Seconds 4

Write-Host "Starting Web with hot reload..." -ForegroundColor Cyan
Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd '$PSScriptRoot'; dotnet watch --project KfConstructionWeb\KfConstructionWeb.csproj --launch-profile https"

Write-Host "Waiting for Web to initialize..." -ForegroundColor Yellow
Start-Sleep -Seconds 4

Write-Host "Opening main website..." -ForegroundColor Green
Start-Process "https://localhost:7085"

Write-Host ""
Write-Host "Applications started successfully!" -ForegroundColor Green
Write-Host "Website: https://localhost:7085" -ForegroundColor White
Write-Host "API: https://localhost:7136/swagger (running but not opened)" -ForegroundColor Gray
Write-Host ""
Write-Host "Hot reload enabled - edit files to see instant changes" -ForegroundColor Yellow
Write-Host "You will see 2 terminal windows - this is normal" -ForegroundColor Gray
Write-Host "Close the terminal windows to stop the applications" -ForegroundColor Gray
Write-Host ""
Write-Host "Press any key to exit launcher..." -ForegroundColor Yellow
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")