Write-Host "========================================" -ForegroundColor Cyan
Write-Host "RandomWheel Manual Test Guide" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

$appDataPath = "$env:APPDATA\RandomWheel"
$listsFile = "$appDataPath\lists.json"
$auditLogFile = "$appDataPath\audit.log"

Write-Host "`nWAIT: Give the app 3 seconds to initialize..." -ForegroundColor Yellow
Start-Sleep -Seconds 3

Write-Host "`n[TEST 1] Initial app data creation" -ForegroundColor Magenta
if (Test-Path $appDataPath) {
    Write-Host "OK - App data folder created" -ForegroundColor Green
} else {
    Write-Host "FAIL - App data folder NOT found" -ForegroundColor Red
}

Write-Host "`n[INSTRUCTIONS] Interact with the app:" -ForegroundColor Yellow
Write-Host "1. Add 3 items: Alice, Bob, Charlie" -ForegroundColor Cyan
Write-Host "2. Click SPIN and observe wheel rotation" -ForegroundColor Cyan
Write-Host "3. Click YES to mark the winner" -ForegroundColor Cyan
Write-Host "4. Create a new list" -ForegroundColor Cyan
Write-Host "5. Bulk add: John,Jane,Jack,Jill,Jim" -ForegroundColor Cyan
Write-Host "6. Toggle mark on John" -ForegroundColor Cyan
Write-Host "7. Delete Jack" -ForegroundColor Cyan
Write-Host "8. View Log button to see audit log" -ForegroundColor Cyan
Write-Host "9. Close app when done" -ForegroundColor Cyan

Write-Host "`nPress ENTER when finished with manual tests..." -ForegroundColor Yellow
Read-Host

Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "Validating Results" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

Write-Host "`n[CHECK] lists.json persistence" -ForegroundColor Magenta
if (Test-Path $listsFile) {
    Write-Host "OK - lists.json exists" -ForegroundColor Green
    $json = Get-Content $listsFile -Raw
    Write-Host "Content:" -ForegroundColor Cyan
    Write-Host $json -ForegroundColor Gray
} else {
    Write-Host "FAIL - lists.json NOT found" -ForegroundColor Red
}

Write-Host "`n[CHECK] audit.log entries" -ForegroundColor Magenta
if (Test-Path $auditLogFile) {
    Write-Host "OK - audit.log exists" -ForegroundColor Green
    $lines = Get-Content $auditLogFile
    Write-Host "Entry count: $($lines.Count)" -ForegroundColor Cyan
    Write-Host "Sample entries:" -ForegroundColor Cyan
    $lines | Select-Object -Last 5 | ForEach-Object { Write-Host "  $_" -ForegroundColor Gray }
} else {
    Write-Host "FAIL - audit.log NOT found" -ForegroundColor Red
}

Write-Host "`n========================================" -ForegroundColor Green
Write-Host "Testing Complete" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
