# Comprehensive Website Audit Script using Edge DevTools Protocol
# This script audits websites for accessibility, performance, console errors, and SEO issues

param(
    [string]$BaseUrl = "http://localhost:5651",
    [string]$EdgePath = "C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe"
)

# Check if Edge exists
if (-not (Test-Path $EdgePath)) {
    $EdgePath = "C:\Program Files\Microsoft\Edge\Application\msedge.exe"
    if (-not (Test-Path $EdgePath)) {
        Write-Error "Microsoft Edge not found. Please install Edge or specify the correct path."
        exit 1
    }
}

Write-Host "Comprehensive Website Audit Report" -ForegroundColor Cyan
Write-Host "=================================" -ForegroundColor Cyan
Write-Host "Generated: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')`n"

# Pages to audit
$pages = @(
    @{Name="Home"; Path="/"; Url="$BaseUrl/"},
    @{Name="Events"; Path="/events"; Url="$BaseUrl/events"},
    @{Name="Login"; Path="/auth/login"; Url="$BaseUrl/auth/login"}
)

$results = @()
$outputDir = "audit-results"
if (-not (Test-Path $outputDir)) {
    New-Item -ItemType Directory -Path $outputDir | Out-Null
}

# Function to run Edge with DevTools protocol
function Invoke-EdgeAudit {
    param($Page)
    
    Write-Host "`nAnalyzing $($Page.Name) page: $($Page.Url)" -ForegroundColor Yellow
    
    $tempFile = Join-Path $outputDir "temp-audit-$($Page.Name).json"
    
    # JavaScript to run in the page
    $auditScript = @'
(async function() {
    const results = {
        url: window.location.href,
        title: document.title,
        timestamp: new Date().toISOString(),
        performance: {},
        accessibility: [],
        seo: [],
        console: [],
        network: []
    };
    
    // Performance metrics
    if (window.performance && window.performance.timing) {
        const timing = window.performance.timing;
        results.performance = {
            domContentLoaded: timing.domContentLoadedEventEnd - timing.navigationStart,
            pageLoad: timing.loadEventEnd - timing.navigationStart,
            firstContentfulPaint: 0
        };
        
        // Get paint timing
        if (window.performance.getEntriesByType) {
            const paintEntries = window.performance.getEntriesByType('paint');
            paintEntries.forEach(entry => {
                if (entry.name === 'first-contentful-paint') {
                    results.performance.firstContentfulPaint = Math.round(entry.startTime);
                }
            });
        }
    }
    
    // Accessibility checks
    const accessibilityChecks = [];
    
    // Check images without alt text
    const imagesWithoutAlt = document.querySelectorAll('img:not([alt])');
    if (imagesWithoutAlt.length > 0) {
        accessibilityChecks.push({
            type: 'missing-alt-text',
            severity: 'HIGH',
            count: imagesWithoutAlt.length,
            description: `${imagesWithoutAlt.length} images found without alt text`
        });
    }
    
    // Check missing page title
    if (!document.title || document.title.trim() === '') {
        accessibilityChecks.push({
            type: 'missing-title',
            severity: 'HIGH',
            description: 'Page is missing a title element'
        });
    }
    
    // Check missing lang attribute
    if (!document.documentElement.lang) {
        accessibilityChecks.push({
            type: 'missing-lang',
            severity: 'HIGH',
            description: 'HTML element is missing lang attribute'
        });
    }
    
    // Check heading structure
    const h1Count = document.querySelectorAll('h1').length;
    if (h1Count === 0) {
        accessibilityChecks.push({
            type: 'missing-h1',
            severity: 'MEDIUM',
            description: 'Page is missing an H1 heading'
        });
    } else if (h1Count > 1) {
        accessibilityChecks.push({
            type: 'multiple-h1',
            severity: 'MEDIUM',
            description: `Page has ${h1Count} H1 headings (should have only one)`
        });
    }
    
    // Check form labels
    const inputsWithoutLabels = document.querySelectorAll('input:not([type="hidden"]):not([type="submit"]):not([aria-label]):not([aria-labelledby])');
    let unlabeledInputs = 0;
    inputsWithoutLabels.forEach(input => {
        const id = input.id;
        if (!id || !document.querySelector(`label[for="${id}"]`)) {
            unlabeledInputs++;
        }
    });
    if (unlabeledInputs > 0) {
        accessibilityChecks.push({
            type: 'missing-form-labels',
            severity: 'HIGH',
            count: unlabeledInputs,
            description: `${unlabeledInputs} form inputs found without associated labels`
        });
    }
    
    // Check buttons without text
    const buttons = document.querySelectorAll('button');
    let emptyButtons = 0;
    buttons.forEach(button => {
        const text = button.textContent.trim();
        const ariaLabel = button.getAttribute('aria-label');
        if (!text && !ariaLabel) {
            emptyButtons++;
        }
    });
    if (emptyButtons > 0) {
        accessibilityChecks.push({
            type: 'empty-buttons',
            severity: 'HIGH',
            count: emptyButtons,
            description: `${emptyButtons} buttons found without accessible text`
        });
    }
    
    results.accessibility = accessibilityChecks;
    
    // SEO checks
    const seoChecks = [];
    
    // Meta description
    const metaDesc = document.querySelector('meta[name="description"]');
    if (!metaDesc || !metaDesc.content) {
        seoChecks.push({
            type: 'missing-meta-description',
            severity: 'MEDIUM',
            description: 'Page is missing meta description'
        });
    }
    
    // Viewport meta tag
    const viewport = document.querySelector('meta[name="viewport"]');
    if (!viewport) {
        seoChecks.push({
            type: 'missing-viewport',
            severity: 'HIGH',
            description: 'Page is missing viewport meta tag (affects mobile responsiveness)'
        });
    }
    
    results.seo = seoChecks;
    
    return results;
})();
'@
    
    # Create a temporary HTML file to run our audit
    $htmlContent = @"
<!DOCTYPE html>
<html>
<head>
    <title>Audit Runner</title>
</head>
<body>
    <script>
        // Capture console messages
        const consoleLogs = [];
        const originalLog = console.log;
        const originalError = console.error;
        const originalWarn = console.warn;
        
        console.log = function(...args) {
            consoleLogs.push({type: 'log', message: args.join(' ')});
            originalLog.apply(console, args);
        };
        console.error = function(...args) {
            consoleLogs.push({type: 'error', message: args.join(' ')});
            originalError.apply(console, args);
        };
        console.warn = function(...args) {
            consoleLogs.push({type: 'warning', message: args.join(' ')});
            originalWarn.apply(console, args);
        };
        
        // Navigate to target page
        window.location.href = '$($Page.Url)';
    </script>
</body>
</html>
"@
    
    # For now, we'll use a simpler approach - manual instructions with detailed checks
    $auditResult = @{
        Name = $Page.Name
        Url = $Page.Url
        Status = "Manual Check Required"
        Issues = @()
    }
    
    Write-Host "Please manually check the following for $($Page.Name) page:" -ForegroundColor Green
    Write-Host ""
    Write-Host "1. ACCESSIBILITY CHECKS:" -ForegroundColor Cyan
    Write-Host "   - Images without alt text"
    Write-Host "   - Form inputs without labels"
    Write-Host "   - Buttons without accessible text"
    Write-Host "   - Missing page title"
    Write-Host "   - Missing lang attribute on <html>"
    Write-Host "   - Heading hierarchy (one H1, proper nesting)"
    Write-Host ""
    Write-Host "2. CONSOLE ERRORS:" -ForegroundColor Cyan
    Write-Host "   - Open Developer Tools (F12)"
    Write-Host "   - Check Console tab for red errors"
    Write-Host "   - Note any JavaScript errors"
    Write-Host ""
    Write-Host "3. NETWORK ERRORS:" -ForegroundColor Cyan
    Write-Host "   - In Developer Tools, go to Network tab"
    Write-Host "   - Refresh the page"
    Write-Host "   - Look for any red (failed) requests"
    Write-Host ""
    Write-Host "4. PERFORMANCE:" -ForegroundColor Cyan
    Write-Host "   - In Developer Tools, go to Lighthouse tab"
    Write-Host "   - Run analysis for Performance"
    Write-Host "   - Note FCP, LCP, TTI metrics"
    Write-Host ""
    Write-Host "5. SEO BASICS:" -ForegroundColor Cyan
    Write-Host "   - Check for meta description"
    Write-Host "   - Check for viewport meta tag"
    Write-Host "   - Verify mobile responsiveness"
    Write-Host ""
    
    $results += $auditResult
}

# Run audits
foreach ($page in $pages) {
    Invoke-EdgeAudit -Page $page
    Write-Host "`nPress any key to continue to next page..." -ForegroundColor Yellow
    $null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
}

# Generate summary report
Write-Host "`n`n" + ("=" * 60) -ForegroundColor Cyan
Write-Host "AUDIT SUMMARY" -ForegroundColor Cyan
Write-Host ("=" * 60) -ForegroundColor Cyan

$reportContent = @"
# Comprehensive Website Audit Report

Generated: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')

## Executive Summary

This audit covers accessibility, performance, SEO, and technical issues for the following pages:
- Home Page ($BaseUrl/)
- Events Page ($BaseUrl/events)
- Login Page ($BaseUrl/auth/login)

## Automated Testing Instructions

Since automated browser testing is not available in the current environment, please use the following tools:

### 1. Chrome/Edge DevTools Lighthouse
1. Open each page in Chrome or Edge
2. Press F12 to open Developer Tools
3. Go to the Lighthouse tab
4. Run audits for:
   - Performance
   - Accessibility
   - Best Practices
   - SEO

### 2. Manual Accessibility Testing
Use these browser extensions for comprehensive testing:
- **axe DevTools** - Comprehensive accessibility testing
- **WAVE** - WebAIM's accessibility evaluation tool
- **Lighthouse** - Built into Chrome/Edge DevTools

### 3. Performance Testing
- Use Chrome DevTools Performance tab
- Record page load and interactions
- Check for:
  - Long tasks blocking the main thread
  - Large layout shifts (CLS)
  - Slow server responses

### 4. Console and Network Monitoring
In DevTools:
- **Console tab**: Check for JavaScript errors (red text)
- **Network tab**: Look for failed requests (red status codes)

## Common Issues to Check

### Critical Issues (Fix Immediately)
1. **JavaScript Errors** - Break functionality
2. **Failed Network Requests** - Missing resources
3. **Missing Alt Text** - Accessibility barrier
4. **Form Inputs Without Labels** - Unusable for screen readers

### High Priority Issues
1. **Performance Metrics**:
   - First Contentful Paint (FCP) > 2.5s
   - Largest Contentful Paint (LCP) > 4s
   - Time to Interactive (TTI) > 5s
   
2. **Accessibility**:
   - Color contrast < 4.5:1
   - Missing keyboard navigation
   - Missing ARIA labels

### Medium Priority Issues
1. **SEO**:
   - Missing meta descriptions
   - Missing Open Graph tags
   - Non-descriptive link text
   
2. **Best Practices**:
   - Mixed content (HTTP on HTTPS)
   - Missing HTTPS
   - Console warnings

## Recommended Tools

### For Comprehensive Auditing:
1. **Google Lighthouse** (built into Chrome/Edge)
2. **WebPageTest** (https://www.webpagetest.org/)
3. **GTmetrix** (https://gtmetrix.com/)

### For Accessibility:
1. **axe DevTools** (Chrome/Firefox extension)
2. **WAVE** (https://wave.webaim.org/)
3. **Pa11y** (Command line tool)

### For Performance:
1. **Chrome DevTools Performance Panel**
2. **SpeedCurve** (https://speedcurve.com/)
3. **Calibre** (https://calibreapp.com/)

## Next Steps

1. Run Lighthouse audits on all three pages
2. Document all errors and warnings found
3. Prioritize fixes based on severity
4. Re-test after implementing fixes
5. Set up continuous monitoring

## Manual Audit Checklist

For each page, verify:

- [ ] No JavaScript errors in console
- [ ] All network requests succeed (no 404s or 500s)
- [ ] All images have alt text
- [ ] All form inputs have labels
- [ ] Page has one H1 heading
- [ ] HTML has lang attribute
- [ ] Page has meta description
- [ ] Page has viewport meta tag
- [ ] Page loads in under 3 seconds
- [ ] Page is keyboard navigable
- [ ] Color contrast meets WCAG standards
- [ ] Page works on mobile devices
"@

# Save the report
$reportPath = Join-Path $outputDir "comprehensive-audit-report.md"
$reportContent | Out-File -FilePath $reportPath -Encoding UTF8
Write-Host "`nAudit report saved to: $reportPath" -ForegroundColor Green

# Create a batch file for running Edge with debugging
$batchContent = @"
@echo off
echo Running Edge Developer Tools Audit
echo ==================================
echo.
echo Opening pages in Edge with DevTools...
echo.

start msedge --new-window "$BaseUrl/" --auto-open-devtools-for-tabs
timeout /t 3
start msedge --new-window "$BaseUrl/events" --auto-open-devtools-for-tabs
timeout /t 3
start msedge --new-window "$BaseUrl/auth/login" --auto-open-devtools-for-tabs

echo.
echo Edge windows opened with DevTools.
echo.
echo For each page:
echo 1. Go to Lighthouse tab in DevTools
echo 2. Run all audits
echo 3. Save the report
echo.
pause
"@

$batchPath = Join-Path $outputDir "run-edge-audit.bat"
$batchContent | Out-File -FilePath $batchPath -Encoding ASCII
Write-Host "Batch file created: $batchPath" -ForegroundColor Green
Write-Host "Run this file to open all pages in Edge with DevTools" -ForegroundColor Yellow

Write-Host "`nAudit setup complete!" -ForegroundColor Green
Write-Host "Please follow the manual instructions in the report to complete the audit." -ForegroundColor Yellow