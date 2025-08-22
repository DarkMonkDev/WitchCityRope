# PowerShell script to capture screenshots using Edge/Chrome DevTools Protocol
# This works without needing Puppeteer MCP

param(
    [string]$BaseUrl = "http://localhost:5651"
)

Write-Host "Screenshot Capture Script for WitchCityRope" -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan

# Pages to capture
$pages = @(
    @{Name="home"; Path="/"; Title="Home Page"},
    @{Name="events"; Path="/events"; Title="Events List"},
    @{Name="login"; Path="/auth/login"; Title="Login Page"},
    @{Name="dashboard"; Path="/dashboard"; Title="Member Dashboard"},
    @{Name="profile"; Path="/profile"; Title="Member Profile"},
    @{Name="admin-events"; Path="/admin/events"; Title="Admin Events"},
    @{Name="admin-vetting"; Path="/admin/vetting"; Title="Admin Vetting"}
)

Write-Host "`nManual Screenshot Instructions:" -ForegroundColor Yellow
Write-Host "Since Puppeteer MCP is not available, please follow these steps:`n"

foreach ($page in $pages) {
    $url = "$BaseUrl$($page.Path)"
    Write-Host "[$($page.Name)]" -ForegroundColor Green
    Write-Host "  1. Open browser and navigate to: $url"
    Write-Host "  2. Press F12 to open Developer Tools"
    Write-Host "  3. Press Ctrl+Shift+P and type 'screenshot'"
    Write-Host "  4. Select 'Capture full size screenshot'"
    Write-Host "  5. Save as: screenshot-$($page.Name).png"
    Write-Host ""
}

Write-Host "Alternative: Browser Extensions" -ForegroundColor Yellow
Write-Host "You can also use browser extensions like:"
Write-Host "- GoFullPage (Chrome/Edge)"
Write-Host "- Fireshot (Chrome/Firefox)"
Write-Host "- Awesome Screenshot (Chrome/Edge)"

Write-Host "`nOr use PowerShell with Edge (if available):" -ForegroundColor Yellow
Write-Host @'
# Example using Edge in headless mode (requires Edge installed)
$pages | ForEach-Object {
    $url = "$BaseUrl$($_.Path)"
    $output = "screenshot-$($_.Name).png"
    
    # This requires msedge executable in PATH
    & msedge --headless --disable-gpu --screenshot="$output" "$url"
}
'@