const puppeteer = require('puppeteer');
const fs = require('fs').promises;
const path = require('path');

const BASE_URL = 'http://localhost:5651';
const PAGES = [
    { name: 'Home', url: '/' },
    { name: 'Events', url: '/events' },
    { name: 'Login', url: '/auth/login' }
];

async function measurePageMetrics(page, pageInfo) {
    console.log(`\nMeasuring ${pageInfo.name} page...`);
    
    const metrics = {
        page: pageInfo.name,
        url: pageInfo.url,
        timestamp: new Date().toISOString()
    };
    
    // Start measuring
    const startTime = Date.now();
    
    // Navigate and wait for network idle
    await page.goto(BASE_URL + pageInfo.url, { 
        waitUntil: 'networkidle2',
        timeout: 30000 
    });
    
    // Calculate load time
    metrics.loadTime = Date.now() - startTime;
    
    // Get performance metrics
    const perfMetrics = await page.metrics();
    metrics.performance = {
        JSHeapUsedSize: Math.round(perfMetrics.JSHeapUsedSize / 1024 / 1024 * 100) / 100,
        Nodes: perfMetrics.Nodes,
        LayoutCount: perfMetrics.LayoutCount,
        RecalcStyleCount: perfMetrics.RecalcStyleCount,
        LayoutDuration: Math.round(perfMetrics.LayoutDuration * 1000) / 1000,
        RecalcStyleDuration: Math.round(perfMetrics.RecalcStyleDuration * 1000) / 1000,
        ScriptDuration: Math.round(perfMetrics.ScriptDuration * 1000) / 1000,
        TaskDuration: Math.round(perfMetrics.TaskDuration * 1000) / 1000
    };
    
    // Get navigation timing
    const navTiming = await page.evaluate(() => {
        const timing = performance.timing;
        return {
            DNS: timing.domainLookupEnd - timing.domainLookupStart,
            TCP: timing.connectEnd - timing.connectStart,
            Request: timing.responseStart - timing.requestStart,
            Response: timing.responseEnd - timing.responseStart,
            DOM: timing.domComplete - timing.domLoading,
            Load: timing.loadEventEnd - timing.navigationStart,
            DOMContentLoaded: timing.domContentLoadedEventEnd - timing.navigationStart,
            FirstPaint: performance.getEntriesByType('paint')[0]?.startTime || 0,
            FirstContentfulPaint: performance.getEntriesByType('paint')[1]?.startTime || 0
        };
    });
    
    metrics.navigationTiming = navTiming;
    
    // Get resource timing
    const resources = await page.evaluate(() => {
        return performance.getEntriesByType('resource').map(r => ({
            name: r.name.split('/').pop(),
            type: r.initiatorType,
            size: r.transferSize,
            duration: Math.round(r.duration)
        }));
    });
    
    // Categorize resources
    metrics.resources = {
        css: resources.filter(r => r.type === 'link' || r.name.endsWith('.css')),
        js: resources.filter(r => r.type === 'script' || r.name.endsWith('.js')),
        fonts: resources.filter(r => r.type === 'font' || r.name.includes('font')),
        other: resources.filter(r => !['link', 'script', 'font'].includes(r.type))
    };
    
    // Calculate totals
    metrics.resourceTotals = {
        cssSize: metrics.resources.css.reduce((sum, r) => sum + r.size, 0),
        jsSize: metrics.resources.js.reduce((sum, r) => sum + r.size, 0),
        fontSize: metrics.resources.fonts.reduce((sum, r) => sum + r.size, 0),
        totalSize: resources.reduce((sum, r) => sum + r.size, 0),
        totalRequests: resources.length
    };
    
    return metrics;
}

async function runPerformanceTests() {
    console.log('WitchCityRope.Web Performance Baseline Test');
    console.log('==========================================');
    
    const browser = await puppeteer.launch({
        headless: true,
        args: ['--no-sandbox', '--disable-setuid-sandbox']
    });
    
    const results = {
        testRun: new Date().toISOString(),
        baseUrl: BASE_URL,
        pages: []
    };
    
    try {
        for (const pageInfo of PAGES) {
            const page = await browser.newPage();
            
            // Set viewport
            await page.setViewport({ width: 1920, height: 1080 });
            
            // Enable request interception to block unnecessary resources
            await page.setRequestInterception(true);
            page.on('request', (req) => {
                // Allow all requests for baseline measurement
                req.continue();
            });
            
            // Measure metrics
            const metrics = await measurePageMetrics(page, pageInfo);
            results.pages.push(metrics);
            
            // Display summary
            console.log(`  Load Time: ${metrics.loadTime}ms`);
            console.log(`  DOM Content Loaded: ${metrics.navigationTiming.DOMContentLoaded}ms`);
            console.log(`  First Contentful Paint: ${Math.round(metrics.navigationTiming.FirstContentfulPaint)}ms`);
            console.log(`  Total Resources: ${metrics.resourceTotals.totalRequests}`);
            console.log(`  Total Size: ${Math.round(metrics.resourceTotals.totalSize / 1024)}KB`);
            console.log(`  - CSS: ${Math.round(metrics.resourceTotals.cssSize / 1024)}KB`);
            console.log(`  - JS: ${Math.round(metrics.resourceTotals.jsSize / 1024)}KB`);
            console.log(`  - Fonts: ${Math.round(metrics.resourceTotals.fontSize / 1024)}KB`);
            
            await page.close();
        }
        
        // Generate summary report
        const report = generateReport(results);
        
        // Save results
        const timestamp = new Date().toISOString().replace(/:/g, '-').substring(0, 19);
        await fs.writeFile(
            path.join(__dirname, `performance-reports/baseline-metrics-${timestamp}.json`),
            JSON.stringify(results, null, 2)
        );
        
        await fs.writeFile(
            path.join(__dirname, `performance-reports/baseline-report-${timestamp}.md`),
            report
        );
        
        console.log('\n' + report);
        
    } catch (error) {
        console.error('Error during performance testing:', error);
    } finally {
        await browser.close();
    }
}

function generateReport(results) {
    let report = `# Performance Baseline Report
Generated: ${new Date().toISOString()}

## Summary

`;
    
    // Calculate averages
    const avgLoadTime = Math.round(
        results.pages.reduce((sum, p) => sum + p.loadTime, 0) / results.pages.length
    );
    
    const avgFCP = Math.round(
        results.pages.reduce((sum, p) => sum + p.navigationTiming.FirstContentfulPaint, 0) / results.pages.length
    );
    
    const totalSize = Math.round(
        results.pages.reduce((sum, p) => sum + p.resourceTotals.totalSize, 0) / results.pages.length / 1024
    );
    
    report += `- **Average Load Time**: ${avgLoadTime}ms
- **Average First Contentful Paint**: ${avgFCP}ms
- **Average Total Size**: ${totalSize}KB

## Page Metrics

`;
    
    results.pages.forEach(page => {
        report += `### ${page.page}
- **Load Time**: ${page.loadTime}ms
- **DOM Content Loaded**: ${page.navigationTiming.DOMContentLoaded}ms
- **First Contentful Paint**: ${Math.round(page.navigationTiming.FirstContentfulPaint)}ms
- **Resources**: ${page.resourceTotals.totalRequests} requests, ${Math.round(page.resourceTotals.totalSize / 1024)}KB total
  - CSS: ${Math.round(page.resourceTotals.cssSize / 1024)}KB
  - JS: ${Math.round(page.resourceTotals.jsSize / 1024)}KB
  - Fonts: ${Math.round(page.resourceTotals.fontSize / 1024)}KB

`;
    });
    
    // Performance recommendations based on metrics
    report += `## Performance Assessment

### Current Status
`;
    
    if (avgFCP > 2500) {
        report += `- ⚠️ **First Contentful Paint is slow** (${avgFCP}ms > 2500ms target)
`;
    } else if (avgFCP > 1800) {
        report += `- ⚠️ **First Contentful Paint needs improvement** (${avgFCP}ms > 1800ms target)
`;
    } else {
        report += `- ✅ **First Contentful Paint is good** (${avgFCP}ms < 1800ms)
`;
    }
    
    if (totalSize > 1000) {
        report += `- ⚠️ **Total page size is large** (${totalSize}KB > 1000KB target)
`;
    } else if (totalSize > 500) {
        report += `- ⚠️ **Total page size could be reduced** (${totalSize}KB > 500KB target)
`;
    } else {
        report += `- ✅ **Total page size is reasonable** (${totalSize}KB < 500KB)
`;
    }
    
    report += `
### Key Recommendations
1. ${avgFCP > 1800 ? 'Optimize initial render path to improve First Contentful Paint' : 'Maintain current FCP performance'}
2. ${totalSize > 500 ? 'Reduce page weight through minification and compression' : 'Current page weight is acceptable'}
3. Enable caching headers for static resources
4. Consider lazy loading for non-critical resources
`;
    
    return report;
}

// Run the tests
runPerformanceTests().catch(console.error);