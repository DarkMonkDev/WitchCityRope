# PowerShell script to test events page using Edge browser automation
# This uses Edge's built-in screenshot capabilities

param(
    [string]$BaseUrl = "http://localhost:5651",
    [string]$OutputDir = ".\screenshots\events-test"
)

Write-Host "Events Page Browser Testing Script" -ForegroundColor Cyan
Write-Host "==================================" -ForegroundColor Cyan

# Create output directory
if (!(Test-Path $OutputDir)) {
    New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null
}

$timestamp = Get-Date -Format "yyyy-MM-dd-HH-mm-ss"

# Function to take screenshot using Edge
function Take-EdgeScreenshot {
    param(
        [string]$Url,
        [string]$OutputFile,
        [string]$ViewportSize = "1920,1080"
    )
    
    $edgePath = "msedge"
    
    # Check if Edge is available
    try {
        $edgeVersion = & $edgePath --version 2>$null
        Write-Host "Using Edge: $edgeVersion" -ForegroundColor Gray
    } catch {
        Write-Host "Microsoft Edge not found in PATH" -ForegroundColor Red
        return $false
    }
    
    # Take screenshot using Edge headless mode
    $args = @(
        "--headless",
        "--disable-gpu",
        "--screenshot=`"$OutputFile`"",
        "--window-size=$ViewportSize",
        "--hide-scrollbars",
        "--force-device-scale-factor=1",
        "`"$Url`""
    )
    
    Write-Host "Taking screenshot: $OutputFile" -ForegroundColor Yellow
    Start-Process -FilePath $edgePath -ArgumentList $args -Wait -NoNewWindow
    
    if (Test-Path $OutputFile) {
        Write-Host "Screenshot saved successfully" -ForegroundColor Green
        return $true
    } else {
        Write-Host "Failed to save screenshot" -ForegroundColor Red
        return $false
    }
}

# Test scenarios
Write-Host "`nTest 1: Initial Page Load (Desktop View)" -ForegroundColor Yellow
$screenshotPath = Join-Path $OutputDir "1-initial-load-$timestamp.png"
Take-EdgeScreenshot -Url "$BaseUrl/events" -OutputFile $screenshotPath -ViewportSize "1920,1080"

Start-Sleep -Seconds 2

Write-Host "`nTest 2: Mobile View (375x667)" -ForegroundColor Yellow
$screenshotPath = Join-Path $OutputDir "2-mobile-view-$timestamp.png"
Take-EdgeScreenshot -Url "$BaseUrl/events" -OutputFile $screenshotPath -ViewportSize "375,667"

Start-Sleep -Seconds 2

Write-Host "`nTest 3: Tablet View (768x1024)" -ForegroundColor Yellow
$screenshotPath = Join-Path $OutputDir "3-tablet-view-$timestamp.png"
Take-EdgeScreenshot -Url "$BaseUrl/events" -OutputFile $screenshotPath -ViewportSize "768,1024"

# Generate test report
$reportContent = @"
# Events Page Test Report
Generated: $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")

## Test Results

### Test 1: Desktop View (1920x1080)
- Screenshot: 1-initial-load-$timestamp.png
- Status: Check manually for:
  - At least 4 event cards visible
  - Search functionality present
  - Proper layout and styling

### Test 2: Mobile View (375x667)
- Screenshot: 2-mobile-view-$timestamp.png
- Status: Check manually for:
  - Cards stack vertically
  - No horizontal scrolling
  - Touch-friendly interface

### Test 3: Tablet View (768x1024)
- Screenshot: 3-tablet-view-$timestamp.png
- Status: Check manually for:
  - Appropriate layout for medium screens
  - Readable text and proper spacing

## Manual Testing Required

Since we cannot interact with the page programmatically, please perform these tests manually:

1. **Search Functionality**
   - Navigate to $BaseUrl/events
   - Type "rope" in the search box
   - Verify events are filtered
   - Clear search and verify all events return

2. **Performance Check**
   - Open Developer Tools (F12)
   - Go to Performance tab
   - Record page load
   - Check for:
     - Load time < 3 seconds
     - No console errors
     - All resources load successfully

3. **Show Past Classes**
   - Click "Show Past Classes" button
   - Verify past events appear
   - Note: If no future events exist, this may be why the page appears empty initially

## Known Issues (from previous analysis)

- Default filter shows only future events
- If all mock data is in the past, page will appear empty
- Solution: Click "Show Past Classes" to see events

## Next Steps

1. Review screenshots in: $OutputDir
2. Perform manual interaction tests
3. Check browser console for errors
4. Test on different browsers if needed
"@

$reportPath = Join-Path $OutputDir "test-report-$timestamp.md"
$reportContent | Out-File -FilePath $reportPath -Encoding UTF8

Write-Host "`nTest Report saved to: $reportPath" -ForegroundColor Green
Write-Host "Screenshots saved to: $OutputDir" -ForegroundColor Green

# Open the output directory
Write-Host "`nOpening results directory..." -ForegroundColor Yellow
Start-Process explorer.exe $OutputDir

Write-Host "`nAutomated testing complete!" -ForegroundColor Green
Write-Host "Please perform manual interaction tests as described in the report." -ForegroundColor Yellow