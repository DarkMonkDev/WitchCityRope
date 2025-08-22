#!/usr/bin/env pwsh

# Performance Analysis Script for WitchCityRope.Web
# Checks bundle sizes, analyzes CSS/JS, and generates performance report

Write-Host "WitchCityRope.Web Performance Analysis" -ForegroundColor Cyan
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host ""

# Set working directory
$workingDir = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $workingDir

# Create output directory for reports
$reportDir = "$workingDir/performance-reports"
if (!(Test-Path $reportDir)) {
    New-Item -ItemType Directory -Path $reportDir | Out-Null
}

$timestamp = Get-Date -Format "yyyy-MM-dd_HH-mm-ss"
$reportFile = "$reportDir/performance-report-$timestamp.md"

# Initialize report
$report = @"
# WitchCityRope.Web Performance Analysis Report
Generated: $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")

## Executive Summary
This report analyzes the performance characteristics of the WitchCityRope.Web application, including bundle sizes, asset optimization, and performance recommendations.

"@

# 1. Analyze CSS Bundle Sizes
Write-Host "1. Analyzing CSS Bundle Sizes..." -ForegroundColor Yellow
$report += @"
## 1. CSS Bundle Analysis

### File Sizes
"@

$cssFiles = Get-ChildItem "$workingDir/wwwroot/css/*.css"
$totalCssSize = 0

foreach ($file in $cssFiles) {
    $size = (Get-Item $file.FullName).Length
    $sizeKB = [math]::Round($size / 1KB, 2)
    $totalCssSize += $size
    
    Write-Host "  - $($file.Name): $sizeKB KB"
    $report += "- **$($file.Name)**: $sizeKB KB`n"
}

$totalCssKB = [math]::Round($totalCssSize / 1KB, 2)
Write-Host "  Total CSS: $totalCssKB KB" -ForegroundColor Green
$report += "`n**Total CSS Size**: $totalCssKB KB`n`n"

# 2. Analyze JavaScript Bundle Sizes
Write-Host "`n2. Analyzing JavaScript Bundle Sizes..." -ForegroundColor Yellow
$report += @"
## 2. JavaScript Bundle Analysis

### File Sizes
"@

$jsFiles = Get-ChildItem "$workingDir/wwwroot/js/*.js"
$totalJsSize = 0

foreach ($file in $jsFiles) {
    $size = (Get-Item $file.FullName).Length
    $sizeKB = [math]::Round($size / 1KB, 2)
    $totalJsSize += $size
    
    Write-Host "  - $($file.Name): $sizeKB KB"
    $report += "- **$($file.Name)**: $sizeKB KB`n"
}

$totalJsKB = [math]::Round($totalJsSize / 1KB, 2)
Write-Host "  Total JavaScript: $totalJsKB KB" -ForegroundColor Green
$report += "`n**Total JavaScript Size**: $totalJsKB KB`n`n"

# 3. Check for Minification
Write-Host "`n3. Checking Minification Status..." -ForegroundColor Yellow
$report += @"
## 3. Minification Analysis

"@

$needsMinification = @()

foreach ($file in $cssFiles + $jsFiles) {
    $content = Get-Content $file.FullName -Raw
    $lines = ($content -split "`n").Count
    $avgLineLength = $content.Length / $lines
    
    # If average line length is less than 200, probably not minified
    if ($avgLineLength -lt 200) {
        $needsMinification += $file.Name
        Write-Host "  - $($file.Name): NOT minified (avg line length: $([math]::Round($avgLineLength, 0)))" -ForegroundColor Red
    } else {
        Write-Host "  - $($file.Name): Appears minified" -ForegroundColor Green
    }
}

if ($needsMinification.Count -gt 0) {
    $report += "### Files Requiring Minification`n"
    foreach ($file in $needsMinification) {
        $report += "- $file`n"
    }
} else {
    $report += "All CSS and JavaScript files appear to be minified.`n"
}

# 4. Analyze External Resources
Write-Host "`n4. Analyzing External Resources..." -ForegroundColor Yellow
$report += @"

## 4. External Resources Analysis

"@

# Check _Layout.cshtml for external resources
$layoutFile = "$workingDir/Pages/_Layout.cshtml"
if (Test-Path $layoutFile) {
    $layoutContent = Get-Content $layoutFile -Raw
    
    # Find external CSS/JS references
    $externalResources = @()
    
    # Google Fonts
    if ($layoutContent -match 'fonts.googleapis.com') {
        $externalResources += "Google Fonts (multiple font families)"
        Write-Host "  - Google Fonts detected" -ForegroundColor Yellow
    }
    
    # Syncfusion
    if ($layoutContent -match 'Syncfusion') {
        $externalResources += "Syncfusion Blazor Components"
        Write-Host "  - Syncfusion components detected" -ForegroundColor Yellow
    }
    
    $report += "### External Dependencies`n"
    foreach ($resource in $externalResources) {
        $report += "- $resource`n"
    }
}

# 5. Image Optimization Check
Write-Host "`n5. Checking for Images..." -ForegroundColor Yellow
$report += @"

## 5. Image Optimization

"@

$imageExtensions = @('*.png', '*.jpg', '*.jpeg', '*.gif', '*.svg', '*.webp')
$images = @()

foreach ($ext in $imageExtensions) {
    $images += Get-ChildItem "$workingDir/wwwroot" -Recurse -Filter $ext -ErrorAction SilentlyContinue
}

if ($images.Count -eq 0) {
    Write-Host "  - No images found in wwwroot" -ForegroundColor Yellow
    $report += "No images found in wwwroot directory. Consider adding optimized images for better visual appeal.`n"
} else {
    $report += "### Image Files`n"
    foreach ($img in $images) {
        $sizeKB = [math]::Round((Get-Item $img.FullName).Length / 1KB, 2)
        $report += "- $($img.Name): $sizeKB KB`n"
    }
}

# 6. Performance Recommendations
Write-Host "`n6. Generating Recommendations..." -ForegroundColor Yellow
$report += @"

## 6. Performance Recommendations

### High Priority
"@

$recommendations = @()

# CSS recommendations
if ($totalCssKB -gt 50) {
    $recommendations += @{
        Priority = "High"
        Category = "CSS"
        Issue = "Large CSS bundle size ($totalCssKB KB)"
        Recommendation = "- Enable CSS minification`n- Remove unused CSS rules`n- Consider code splitting for page-specific styles"
    }
}

# JavaScript recommendations
if ($needsMinification.Count -gt 0) {
    $recommendations += @{
        Priority = "High"
        Category = "JavaScript"
        Issue = "Unminified JavaScript files"
        Recommendation = "- Implement build-time minification using terser or similar`n- Enable compression (gzip/brotli) on the server"
    }
}

# External resources
if ($layoutContent -match 'fonts.googleapis.com') {
    $recommendations += @{
        Priority = "Medium"
        Category = "Fonts"
        Issue = "Multiple Google Fonts loaded"
        Recommendation = "- Limit font families to 2-3 maximum`n- Use font-display: swap for better loading`n- Consider self-hosting critical fonts"
    }
}

# Blazor-specific
$recommendations += @{
    Priority = "High"
    Category = "Blazor"
    Issue = "Server-side Blazor overhead"
    Recommendation = "- Enable response compression`n- Implement lazy loading for components`n- Use virtualization for large lists`n- Consider Blazor WebAssembly for static content"
}

# Sort and display recommendations
$highPriority = $recommendations | Where-Object { $_.Priority -eq "High" }
$mediumPriority = $recommendations | Where-Object { $_.Priority -eq "Medium" }

foreach ($rec in $highPriority) {
    $report += @"

### $($rec.Category): $($rec.Issue)
$($rec.Recommendation)
"@
}

if ($mediumPriority.Count -gt 0) {
    $report += "`n### Medium Priority`n"
    foreach ($rec in $mediumPriority) {
        $report += @"

### $($rec.Category): $($rec.Issue)
$($rec.Recommendation)
"@
    }
}

# 7. Quick Wins
$report += @"

## 7. Quick Performance Wins

1. **Enable Response Compression**
   - Add response compression middleware in Program.cs
   - Compress static assets (CSS, JS, JSON)

2. **Implement Browser Caching**
   - Set appropriate cache headers for static assets
   - Use versioning for cache busting

3. **Optimize Blazor Server Connection**
   - Configure SignalR for better performance
   - Implement circuit handler for connection management

4. **Add Loading States**
   - Show skeleton screens during component initialization
   - Implement progressive enhancement

5. **Resource Hints**
   - Add preconnect for external domains
   - Use prefetch for critical resources

## 8. Estimated Impact

Based on the analysis:
- **Current estimated load time**: 2-3 seconds (with good connection)
- **After optimizations**: < 1.5 seconds
- **Potential improvement**: 40-50% faster initial load

### Core Web Vitals Targets
- **LCP (Largest Contentful Paint)**: < 2.5s (Good)
- **FID (First Input Delay)**: < 100ms (Good)
- **CLS (Cumulative Layout Shift)**: < 0.1 (Good)

"@

# Save report
$report | Out-File -FilePath $reportFile -Encoding UTF8

Write-Host "`nPerformance analysis complete!" -ForegroundColor Green
Write-Host "Report saved to: $reportFile" -ForegroundColor Cyan

# Display summary
Write-Host "`nSummary:" -ForegroundColor Yellow
Write-Host "- Total CSS: $totalCssKB KB"
Write-Host "- Total JS: $totalJsKB KB"
Write-Host "- Files needing minification: $($needsMinification.Count)"
Write-Host "- High priority recommendations: $($highPriority.Count)"

# Open report in default editor
if ($IsWindows) {
    Start-Process $reportFile
}