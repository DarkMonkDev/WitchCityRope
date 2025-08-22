# PowerShell script to take screenshots using Chrome in headless mode
$url = "http://localhost:5651/auth/login"
$timestamp = Get-Date -Format "yyyy-MM-dd-HH-mm-ss"
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$screenshotPath = Join-Path $scriptDir "login-page-screenshot-$timestamp.png"
$viewportPath = Join-Path $scriptDir "login-page-viewport-$timestamp.png"

# Chrome executable path
$chromePath = "C:\Program Files\Google\Chrome\Application\chrome.exe"

Write-Host "Taking screenshot of $url..."

# Take a full page screenshot
& "$chromePath" --headless --disable-gpu --screenshot="$screenshotPath" --window-size=1920,1080 --virtual-time-budget=5000 "$url"

Write-Host "Screenshot saved to: $screenshotPath"

# Also take a viewport screenshot with different dimensions
& "$chromePath" --headless --disable-gpu --screenshot="$viewportPath" --window-size=1366,768 --virtual-time-budget=5000 "$url"

Write-Host "Viewport screenshot saved to: $viewportPath"

# Check if files were created
if (Test-Path $screenshotPath) {
    $fileInfo = Get-Item $screenshotPath
    Write-Host "Full page screenshot size: $($fileInfo.Length) bytes"
}

if (Test-Path $viewportPath) {
    $fileInfo = Get-Item $viewportPath
    Write-Host "Viewport screenshot size: $($fileInfo.Length) bytes"
}