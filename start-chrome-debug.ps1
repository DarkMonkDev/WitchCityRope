Write-Host "Starting Chrome in debug mode for Stagehand MCP..." -ForegroundColor Green
$chromePath = "C:\Program Files\Google\Chrome\Application\chrome.exe"
if (-not (Test-Path $chromePath)) {
    $chromePath = "C:\Program Files (x86)\Google\Chrome\Application\chrome.exe"
}
Start-Process $chromePath -ArgumentList "--remote-debugging-port=9222", "--user-data-dir=$env:TEMP\chrome-debug"
Write-Host "Chrome started with remote debugging on port 9222" -ForegroundColor Green
Write-Host "You can now use Stagehand in Claude Desktop" -ForegroundColor Yellow
Read-Host "Press Enter to continue..."