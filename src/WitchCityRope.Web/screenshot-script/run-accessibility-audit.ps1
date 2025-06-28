# PowerShell script to run comprehensive accessibility audits
# This script orchestrates the accessibility testing process

param(
    [Parameter(Mandatory=$false)]
    [string]$Environment = "local",
    
    [Parameter(Mandatory=$false)]
    [string]$BaseUrl = "http://localhost:5651",
    
    [Parameter(Mandatory=$false)]
    [switch]$SkipInstall = $false,
    
    [Parameter(Mandatory=$false)]
    [switch]$OpenReport = $false
)

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Witch City Rope Accessibility Audit Tool" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Set the working directory
$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $scriptPath

# Check if Node.js is installed
Write-Host "Checking prerequisites..." -ForegroundColor Yellow
try {
    $nodeVersion = node --version
    Write-Host "Node.js version: $nodeVersion" -ForegroundColor Green
} catch {
    Write-Host "ERROR: Node.js is not installed or not in PATH" -ForegroundColor Red
    Write-Host "Please install Node.js from https://nodejs.org/" -ForegroundColor Yellow
    exit 1
}

# Install dependencies if needed
if (-not $SkipInstall) {
    Write-Host ""
    Write-Host "Installing/updating dependencies..." -ForegroundColor Yellow
    
    # Check if axe-core is installed (for additional accessibility testing)
    if (-not (Test-Path "node_modules\axe-core")) {
        Write-Host "Installing axe-core for enhanced accessibility testing..." -ForegroundColor Yellow
        npm install axe-core --save
    }
    
    # Check if pa11y is installed (for additional accessibility testing)
    if (-not (Test-Path "node_modules\pa11y")) {
        Write-Host "Installing pa11y for WCAG testing..." -ForegroundColor Yellow
        npm install pa11y --save
    }
    
    # Ensure all other dependencies are installed
    npm install
} else {
    Write-Host "Skipping dependency installation (--SkipInstall flag set)" -ForegroundColor Gray
}

# Check if the application is running
Write-Host ""
Write-Host "Checking if application is running at $BaseUrl..." -ForegroundColor Yellow
try {
    $response = Invoke-WebRequest -Uri $BaseUrl -UseBasicParsing -TimeoutSec 5 -ErrorAction Stop
    Write-Host "Application is running (Status: $($response.StatusCode))" -ForegroundColor Green
} catch {
    Write-Host "WARNING: Application is not responding at $BaseUrl" -ForegroundColor Red
    Write-Host "Please ensure the application is running before proceeding." -ForegroundColor Yellow
    
    $continue = Read-Host "Do you want to continue anyway? (y/n)"
    if ($continue -ne 'y') {
        exit 1
    }
}

# Create output directory
$timestamp = Get-Date -Format "yyyy-MM-dd_HH-mm-ss"
$outputDir = "accessibility-reports\$timestamp"
New-Item -ItemType Directory -Force -Path $outputDir | Out-Null
Write-Host ""
Write-Host "Output directory created: $outputDir" -ForegroundColor Green

# Run main accessibility audit
Write-Host ""
Write-Host "Running comprehensive accessibility audit..." -ForegroundColor Cyan
Write-Host "This may take several minutes..." -ForegroundColor Gray

$auditStartTime = Get-Date
try {
    # Run the main accessibility audit
    node accessibility-audit.js
    
    # Move reports to timestamped directory
    if (Test-Path "accessibility-audit-results.json") {
        Move-Item -Force "accessibility-audit-results.json" "$outputDir\accessibility-audit-results.json"
    }
    if (Test-Path "accessibility-audit-report.md") {
        Move-Item -Force "accessibility-audit-report.md" "$outputDir\accessibility-audit-report.md"
    }
    if (Test-Path "accessibility-fixes.md") {
        Move-Item -Force "accessibility-fixes.md" "$outputDir\accessibility-fixes.md"
    }
    
    Write-Host "Main accessibility audit completed successfully!" -ForegroundColor Green
} catch {
    Write-Host "ERROR: Accessibility audit failed" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    exit 1
}

# Run additional color contrast analysis
Write-Host ""
Write-Host "Running detailed color contrast analysis..." -ForegroundColor Cyan
try {
    node color-contrast-analyzer.js
    if (Test-Path "color-contrast-report.json") {
        Move-Item -Force "color-contrast-report.json" "$outputDir\color-contrast-report.json"
    }
    Write-Host "Color contrast analysis completed!" -ForegroundColor Green
} catch {
    Write-Host "WARNING: Color contrast analysis failed" -ForegroundColor Yellow
}

# Run keyboard navigation testing
Write-Host ""
Write-Host "Running keyboard navigation tests..." -ForegroundColor Cyan
try {
    node keyboard-navigation-test.js
    if (Test-Path "keyboard-navigation-report.json") {
        Move-Item -Force "keyboard-navigation-report.json" "$outputDir\keyboard-navigation-report.json"
    }
    Write-Host "Keyboard navigation tests completed!" -ForegroundColor Green
} catch {
    Write-Host "WARNING: Keyboard navigation tests failed" -ForegroundColor Yellow
}

# Run screen reader compatibility tests
Write-Host ""
Write-Host "Running screen reader compatibility tests..." -ForegroundColor Cyan
try {
    node screen-reader-test.js
    if (Test-Path "screen-reader-report.json") {
        Move-Item -Force "screen-reader-report.json" "$outputDir\screen-reader-report.json"
    }
    Write-Host "Screen reader tests completed!" -ForegroundColor Green
} catch {
    Write-Host "WARNING: Screen reader tests failed" -ForegroundColor Yellow
}

# Calculate total time
$auditEndTime = Get-Date
$totalTime = $auditEndTime - $auditStartTime
Write-Host ""
Write-Host "Total audit time: $($totalTime.TotalMinutes.ToString('F2')) minutes" -ForegroundColor Cyan

# Generate summary report
Write-Host ""
Write-Host "Generating summary report..." -ForegroundColor Yellow

$summaryContent = @"
# Accessibility Audit Summary

**Generated**: $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")
**Environment**: $Environment
**Base URL**: $BaseUrl
**Total Time**: $($totalTime.TotalMinutes.ToString('F2')) minutes

## Reports Generated

1. **Main Accessibility Report**: accessibility-audit-report.md
   - WCAG 2.1 compliance analysis
   - Lighthouse accessibility scores
   - Detailed issue breakdown
   
2. **Implementation Fixes**: accessibility-fixes.md
   - Prioritized list of fixes
   - Code examples and solutions
   - Testing guidelines

3. **Raw Data**: accessibility-audit-results.json
   - Complete audit data in JSON format
   - Suitable for automated processing

## Quick Stats

"@

# Try to extract key metrics from the JSON report
if (Test-Path "$outputDir\accessibility-audit-results.json") {
    try {
        $jsonData = Get-Content "$outputDir\accessibility-audit-results.json" | ConvertFrom-Json
        
        $summaryContent += @"
- **Overall Score**: $($jsonData.summary.overallScore)/100
- **WCAG Compliance**: $($jsonData.summary.wcagCompliance.level)
- **Total Issues**: $($jsonData.summary.totalIssues)
- **Critical Issues**: $($jsonData.summary.criticalIssues)

## Page Scores

| Page | Score |
|------|-------|
"@
        
        foreach ($page in $jsonData.summary.pageScores) {
            $summaryContent += "| $($page.page) | $($page.score)/100 |`n"
        }
        
    } catch {
        Write-Host "Could not parse JSON report for summary" -ForegroundColor Yellow
    }
}

$summaryContent += @"

## Next Steps

1. Review the detailed report in `accessibility-audit-report.md`
2. Implement fixes from `accessibility-fixes.md` starting with critical issues
3. Re-run this audit after implementing fixes to track progress
4. Consider manual testing with actual screen readers and keyboard navigation

## Testing Commands

To re-run the full audit:
``````powershell
.\run-accessibility-audit.ps1
``````

To run specific tests:
``````bash
# Color contrast only
node color-contrast-analyzer.js

# Keyboard navigation only
node keyboard-navigation-test.js

# Screen reader compatibility only
node screen-reader-test.js
``````
"@

# Save summary
$summaryContent | Out-File -FilePath "$outputDir\AUDIT-SUMMARY.md" -Encoding UTF8
Write-Host "Summary report saved to: $outputDir\AUDIT-SUMMARY.md" -ForegroundColor Green

# Display results location
Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Audit Complete!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Reports saved to: $outputDir" -ForegroundColor Yellow
Write-Host ""
Write-Host "Key files:" -ForegroundColor Yellow
Write-Host "  - AUDIT-SUMMARY.md           : Quick overview and stats" -ForegroundColor Gray
Write-Host "  - accessibility-audit-report.md : Detailed findings" -ForegroundColor Gray
Write-Host "  - accessibility-fixes.md        : Implementation guide" -ForegroundColor Gray
Write-Host ""

# Open report if requested
if ($OpenReport) {
    Write-Host "Opening report in default editor..." -ForegroundColor Yellow
    Start-Process "$outputDir\accessibility-audit-report.md"
}

# Provide actionable next steps based on results
if (Test-Path "$outputDir\accessibility-audit-results.json") {
    try {
        $jsonData = Get-Content "$outputDir\accessibility-audit-results.json" | ConvertFrom-Json
        
        if ($jsonData.summary.criticalIssues -gt 0) {
            Write-Host "ACTION REQUIRED: $($jsonData.summary.criticalIssues) critical accessibility issues found!" -ForegroundColor Red
            Write-Host "Please review accessibility-fixes.md for immediate fixes." -ForegroundColor Yellow
        } elseif ($jsonData.summary.totalIssues -gt 0) {
            Write-Host "ATTENTION: $($jsonData.summary.totalIssues) accessibility issues found." -ForegroundColor Yellow
            Write-Host "Review the reports to improve accessibility compliance." -ForegroundColor Yellow
        } else {
            Write-Host "EXCELLENT: No accessibility issues found!" -ForegroundColor Green
            Write-Host "Your application appears to be WCAG 2.1 AA compliant." -ForegroundColor Green
        }
    } catch {
        # Silent fail - already displayed reports location
    }
}

Write-Host ""
Write-Host "Run with -OpenReport flag to automatically open the report." -ForegroundColor Gray
Write-Host "Example: .\run-accessibility-audit.ps1 -OpenReport" -ForegroundColor Gray
Write-Host ""