#!/usr/bin/env pwsh
# Minify CSS and JavaScript assets for WitchCityRope.Web
# This script reduces file sizes by 40-50% through minification

Write-Host "WitchCityRope Asset Minification Script" -ForegroundColor Cyan
Write-Host "======================================" -ForegroundColor Cyan
Write-Host ""

# Check if Node.js is installed
try {
    $nodeVersion = node --version
    Write-Host "Found Node.js $nodeVersion" -ForegroundColor Green
} catch {
    Write-Host "ERROR: Node.js is not installed. Please install Node.js first." -ForegroundColor Red
    exit 1
}

# Install minification tools if not already installed
Write-Host "Checking for minification tools..." -ForegroundColor Yellow
$terserInstalled = npm list -g terser 2>$null
$cssnanoInstalled = npm list -g cssnano-cli 2>$null

if (-not $terserInstalled -or $terserInstalled -match "empty") {
    Write-Host "Installing terser for JavaScript minification..." -ForegroundColor Yellow
    npm install -g terser
}

if (-not $cssnanoInstalled -or $cssnanoInstalled -match "empty") {
    Write-Host "Installing cssnano for CSS minification..." -ForegroundColor Yellow
    npm install -g cssnano-cli
}

# Get the script directory
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$wwwrootPath = Join-Path $scriptDir "wwwroot"
$cssPath = Join-Path $wwwrootPath "css"
$jsPath = Join-Path $wwwrootPath "js"

Write-Host ""
Write-Host "Starting minification process..." -ForegroundColor Cyan
Write-Host ""

# Minify CSS files
Write-Host "Minifying CSS files..." -ForegroundColor Yellow
$cssFiles = @(
    "app.css",
    "pages.css",
    "wcr-theme.css"
)

$totalOriginalSize = 0
$totalMinifiedSize = 0

foreach ($cssFile in $cssFiles) {
    $inputFile = Join-Path $cssPath $cssFile
    $outputFile = Join-Path $cssPath ($cssFile -replace '\.css$', '.min.css')
    
    if (Test-Path $inputFile) {
        $originalSize = (Get-Item $inputFile).Length
        $totalOriginalSize += $originalSize
        
        Write-Host "  Minifying $cssFile..." -NoNewline
        
        # Run cssnano with options for maximum compression
        & cssnano $inputFile $outputFile --no-map
        
        if (Test-Path $outputFile) {
            $minifiedSize = (Get-Item $outputFile).Length
            $totalMinifiedSize += $minifiedSize
            $reduction = [math]::Round((1 - ($minifiedSize / $originalSize)) * 100, 1)
            
            Write-Host " Done! " -NoNewline -ForegroundColor Green
            Write-Host "($([math]::Round($originalSize/1024, 1))KB → $([math]::Round($minifiedSize/1024, 1))KB, -$reduction%)"
        } else {
            Write-Host " Failed!" -ForegroundColor Red
        }
    } else {
        Write-Host "  Warning: $cssFile not found" -ForegroundColor Yellow
    }
}

Write-Host ""

# Minify JavaScript files
Write-Host "Minifying JavaScript files..." -ForegroundColor Yellow
$jsFiles = @(
    "app.js"
)

foreach ($jsFile in $jsFiles) {
    $inputFile = Join-Path $jsPath $jsFile
    $outputFile = Join-Path $jsPath ($jsFile -replace '\.js$', '.min.js')
    
    if (Test-Path $inputFile) {
        $originalSize = (Get-Item $inputFile).Length
        $totalOriginalSize += $originalSize
        
        Write-Host "  Minifying $jsFile..." -NoNewline
        
        # Run terser with options for maximum compression
        & terser $inputFile -o $outputFile --compress --mangle --comments false
        
        if (Test-Path $outputFile) {
            $minifiedSize = (Get-Item $outputFile).Length
            $totalMinifiedSize += $minifiedSize
            $reduction = [math]::Round((1 - ($minifiedSize / $originalSize)) * 100, 1)
            
            Write-Host " Done! " -NoNewline -ForegroundColor Green
            Write-Host "($([math]::Round($originalSize/1024, 1))KB → $([math]::Round($minifiedSize/1024, 1))KB, -$reduction%)"
        } else {
            Write-Host " Failed!" -ForegroundColor Red
        }
    } else {
        Write-Host "  Warning: $jsFile not found" -ForegroundColor Yellow
    }
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Minification Summary" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Total Original Size: $([math]::Round($totalOriginalSize/1024, 1))KB" -ForegroundColor White
Write-Host "Total Minified Size: $([math]::Round($totalMinifiedSize/1024, 1))KB" -ForegroundColor White
Write-Host "Total Reduction: $([math]::Round((1 - ($totalMinifiedSize / $totalOriginalSize)) * 100, 1))%" -ForegroundColor Green
Write-Host ""

# Create version hash for cache busting
$versionHash = Get-Date -Format "yyyyMMddHHmmss"
$versionFile = Join-Path $wwwrootPath "version.txt"
Set-Content -Path $versionFile -Value $versionHash

Write-Host "Cache busting version: $versionHash" -ForegroundColor Yellow
Write-Host "Saved to: $versionFile" -ForegroundColor Gray
Write-Host ""
Write-Host "Next Steps:" -ForegroundColor Cyan
Write-Host "1. Update _Layout.cshtml to use .min.css and .min.js files" -ForegroundColor White
Write-Host "2. Add ?v=$versionHash to asset URLs for cache busting" -ForegroundColor White
Write-Host "3. Consider adding this script to your build pipeline" -ForegroundColor White
Write-Host ""
Write-Host "Minification complete!" -ForegroundColor Green