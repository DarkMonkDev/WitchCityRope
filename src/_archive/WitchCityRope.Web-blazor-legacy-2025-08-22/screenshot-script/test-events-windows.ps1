# PowerShell script to run the comprehensive events test from Windows side
param(
    [string]$NodePath = "node"
)

Write-Host "Running Events Page Comprehensive Test from Windows..." -ForegroundColor Cyan
Write-Host "====================================================" -ForegroundColor Cyan

# Change to the script directory
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $scriptDir

# Check if Node.js is available
try {
    $nodeVersion = & $NodePath --version
    Write-Host "Node.js version: $nodeVersion" -ForegroundColor Green
} catch {
    Write-Host "Node.js not found. Please ensure Node.js is installed and in PATH." -ForegroundColor Red
    exit 1
}

# Check if the test script exists
if (Test-Path "test-events-comprehensive.js") {
    Write-Host "Found test script. Running tests..." -ForegroundColor Yellow
    
    # Run the test script
    & $NodePath test-events-comprehensive.js
    
    # Check if the test completed successfully
    if ($LASTEXITCODE -eq 0) {
        Write-Host "`nTests completed successfully!" -ForegroundColor Green
        
        # Open the HTML report if it exists
        $timestamp = Get-Date -Format "yyyy-MM-ddTHH-mm-ss"
        $reportPath = Join-Path "screenshots\events-test" "test-report-*.html"
        $latestReport = Get-ChildItem $reportPath | Sort-Object LastWriteTime -Descending | Select-Object -First 1
        
        if ($latestReport) {
            Write-Host "Opening test report: $($latestReport.FullName)" -ForegroundColor Green
            Start-Process $latestReport.FullName
        }
    } else {
        Write-Host "`nTests failed with exit code: $LASTEXITCODE" -ForegroundColor Red
    }
} else {
    Write-Host "Test script not found: test-events-comprehensive.js" -ForegroundColor Red
    exit 1
}

Write-Host "`nPress any key to continue..."
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")