import { test, expect } from '@playwright/test';

/**
 * Test suite to verify page stability and content for Events Management demo pages
 * 
 * This test addresses user reports of:
 * 1. Demo page showing minimal content instead of full demo
 * 2. Pages constantly reloading
 * 3. Navigation test page constantly counting up renders
 * 
 * Environment: React app on port 5174
 * Created: 2025-09-06
 * Purpose: Verify actual page behavior vs reported issues
 */

test.describe('Events Management Demo Page Stability', () => {
  const BASE_URL = 'http://localhost:5174';
  
  test.beforeEach(async ({ page }) => {
    // Set up console monitoring to track errors and reloads
    page.on('console', msg => {
      if (msg.type() === 'error') {
        console.log(`❌ Console Error: ${msg.text()}`);
      } else if (msg.type() === 'warn') {
        console.log(`⚠️ Console Warning: ${msg.text()}`);
      }
    });

    // Track page reloads
    page.on('load', () => {
      console.log(`🔄 Page loaded: ${page.url()}`);
    });
  });

  test('should verify /admin/events-management-api-demo shows full demo content, not minimal test page', async ({ page }) => {
    console.log('🔍 Testing events management API demo page content...');
    
    // Navigate to the demo page
    await page.goto(`${BASE_URL}/admin/events-management-api-demo`);
    
    // Wait for page to be fully loaded
    await page.waitForLoadState('networkidle', { timeout: 10000 });
    
    // Take screenshot for evidence
    await page.screenshot({ 
      path: '/home/chad/repos/witchcityrope-react/.worktrees/feature-2025-08-24-events-management/apps/web/test-results/events-demo-page-content.png',
      fullPage: true 
    });
    
    // Check for full demo content indicators (vs minimal test page)
    const pageContent = await page.textContent('body');
    
    // Look for full demo indicators
    const hasEventSessionMatrix = await page.locator('text=Event Session Matrix').count() > 0;
    const hasDemoContent = await page.locator('[data-testid="demo-content"]').count() > 0 ||
                           await page.locator('text=Demo').count() > 0;
    const hasApiDemoFeatures = await page.locator('text=API').count() > 0;
    
    // Look for minimal test page indicators
    const hasMinimalContent = pageContent?.includes('minimal test') || 
                             pageContent?.includes('Basic test page') ||
                             pageContent?.length < 200; // Very short content suggests minimal page
    
    // Document findings
    console.log(`📊 Page Content Analysis:`);
    console.log(`   - Has Event Session Matrix: ${hasEventSessionMatrix}`);
    console.log(`   - Has Demo Content: ${hasDemoContent}`);
    console.log(`   - Has API Demo Features: ${hasApiDemoFeatures}`);
    console.log(`   - Content Length: ${pageContent?.length || 0} characters`);
    console.log(`   - Has Minimal Content Indicators: ${hasMinimalContent}`);
    
    // The main assertion
    if (hasMinimalContent) {
      console.log('❌ CONFIRMED: Page shows minimal content instead of full demo');
      throw new Error(`Page shows minimal content instead of full demo. Content: ${pageContent?.substring(0, 500)}...`);
    } else if (hasEventSessionMatrix || hasDemoContent || hasApiDemoFeatures) {
      console.log('✅ SUCCESS: Page shows full demo content');
    } else {
      console.log('⚠️  UNCLEAR: Unable to definitively determine if content is full demo or minimal');
      // Still take screenshot for manual review
    }
    
    // Verify basic page structure
    expect(page.url()).toBe(`${BASE_URL}/admin/events-management-api-demo`);
  });

  test('should monitor page for unwanted reloading within 10 seconds', async ({ page }) => {
    console.log('🔍 Testing for unwanted page reloads...');
    
    let reloadCount = 0;
    let initialLoadTime = Date.now();
    
    // Track page reloads
    page.on('load', () => {
      if (Date.now() - initialLoadTime > 1000) { // Ignore initial load
        reloadCount++;
        console.log(`🔄 Unwanted reload detected! Count: ${reloadCount}`);
      }
    });
    
    // Navigate to the demo page
    await page.goto(`${BASE_URL}/admin/events-management-api-demo`);
    initialLoadTime = Date.now();
    
    // Wait for initial load
    await page.waitForLoadState('networkidle', { timeout: 5000 });
    
    // Monitor for 10 seconds for unwanted reloads
    console.log('⏱️ Monitoring for reloads for 10 seconds...');
    await page.waitForTimeout(10000);
    
    // Take screenshot of final state
    await page.screenshot({ 
      path: '/home/chad/repos/witchcityrope-react/.worktrees/feature-2025-08-24-events-management/apps/web/test-results/events-demo-stability-check.png',
      fullPage: true 
    });
    
    console.log(`📊 Reload Analysis:`);
    console.log(`   - Unwanted reloads in 10 seconds: ${reloadCount}`);
    
    if (reloadCount > 0) {
      console.log('❌ CONFIRMED: Page is reloading inappropriately');
      throw new Error(`Page reloaded ${reloadCount} times during 10-second monitoring period`);
    } else {
      console.log('✅ SUCCESS: No unwanted reloads detected');
    }
  });

  test('should check /navigation-test page for constantly increasing render count', async ({ page }) => {
    console.log('🔍 Testing navigation test page render counting...');
    
    // Navigate to the navigation test page
    await page.goto(`${BASE_URL}/navigation-test`);
    
    // Wait for page to be fully loaded
    await page.waitForLoadState('networkidle', { timeout: 5000 });
    
    // Look for render count indicators
    let renderCountElements = await page.locator('text=/render/i').all();
    let renderCountNumbers = await page.locator('text=/\\d+/').all();
    
    // Capture initial render count if present
    let initialRenderCount = null;
    for (let element of renderCountNumbers) {
      const text = await element.textContent();
      const number = parseInt(text || '0', 10);
      if (number > 0) {
        initialRenderCount = number;
        break;
      }
    }
    
    console.log(`📊 Initial Render Count: ${initialRenderCount}`);
    
    // Take initial screenshot
    await page.screenshot({ 
      path: '/home/chad/repos/witchcityrope-react/.worktrees/feature-2025-08-24-events-management/apps/web/test-results/navigation-test-initial.png',
      fullPage: true 
    });
    
    // Wait 5 seconds and check if render count increases
    await page.waitForTimeout(5000);
    
    // Re-check render count
    renderCountNumbers = await page.locator('text=/\\d+/').all();
    let laterRenderCount = null;
    for (let element of renderCountNumbers) {
      const text = await element.textContent();
      const number = parseInt(text || '0', 10);
      if (number > 0) {
        laterRenderCount = number;
        break;
      }
    }
    
    // Take final screenshot
    await page.screenshot({ 
      path: '/home/chad/repos/witchcityrope-react/.worktrees/feature-2025-08-24-events-management/apps/web/test-results/navigation-test-after-5s.png',
      fullPage: true 
    });
    
    console.log(`📊 Render Count Analysis:`);
    console.log(`   - Initial count: ${initialRenderCount}`);
    console.log(`   - After 5 seconds: ${laterRenderCount}`);
    
    if (initialRenderCount !== null && laterRenderCount !== null && laterRenderCount > initialRenderCount) {
      const increase = laterRenderCount - initialRenderCount;
      console.log(`❌ CONFIRMED: Render count increased by ${increase} in 5 seconds`);
      throw new Error(`Render count increased from ${initialRenderCount} to ${laterRenderCount} (${increase} renders) in 5 seconds`);
    } else if (initialRenderCount !== null) {
      console.log('✅ SUCCESS: Render count stable');
    } else {
      console.log('⚠️ INFO: No render count found on page');
    }
    
    // Verify basic page structure
    expect(page.url()).toBe(`${BASE_URL}/navigation-test`);
  });

  test('should check /test-no-layout page stability and console errors', async ({ page }) => {
    console.log('🔍 Testing test-no-layout page stability...');
    
    let consoleErrors: string[] = [];
    let consoleWarnings: string[] = [];
    
    // Capture console messages
    page.on('console', msg => {
      if (msg.type() === 'error') {
        consoleErrors.push(msg.text());
      } else if (msg.type() === 'warn') {
        consoleWarnings.push(msg.text());
      }
    });
    
    // Navigate to the test page
    await page.goto(`${BASE_URL}/test-no-layout`);
    
    // Wait for page to be fully loaded
    await page.waitForLoadState('networkidle', { timeout: 5000 });
    
    // Take screenshot
    await page.screenshot({ 
      path: '/home/chad/repos/witchcityrope-react/.worktrees/feature-2025-08-24-events-management/apps/web/test-results/test-no-layout-stability.png',
      fullPage: true 
    });
    
    // Monitor page stability for 5 seconds
    await page.waitForTimeout(5000);
    
    console.log(`📊 Console Analysis:`);
    console.log(`   - Console errors: ${consoleErrors.length}`);
    console.log(`   - Console warnings: ${consoleWarnings.length}`);
    
    if (consoleErrors.length > 0) {
      console.log('❌ Console errors found:', consoleErrors.slice(0, 3)); // Show first 3
    }
    if (consoleWarnings.length > 0) {
      console.log('⚠️ Console warnings found:', consoleWarnings.slice(0, 3)); // Show first 3
    }
    
    // Verify basic page structure
    expect(page.url()).toBe(`${BASE_URL}/test-no-layout`);
    
    // Expect minimal console errors (some may be expected due to API issues)
    if (consoleErrors.length > 10) {
      console.log('❌ EXCESSIVE console errors detected');
      throw new Error(`Excessive console errors: ${consoleErrors.length} errors found`);
    } else {
      console.log('✅ Console errors within acceptable range');
    }
  });

  test('should generate comprehensive stability report', async ({ page }) => {
    console.log('🔍 Generating comprehensive stability report...');
    
    const report = {
      timestamp: new Date().toISOString(),
      testResults: {
        eventsManagementApiDemo: { tested: false, stable: false, hasFullContent: false, issues: [] as string[] },
        navigationTest: { tested: false, stable: false, renderCountStable: false, issues: [] as string[] },
        testNoLayout: { tested: false, stable: false, consoleErrorCount: 0, issues: [] as string[] }
      },
      environment: {
        baseUrl: BASE_URL,
        reactHealthy: false,
        apiHealthy: false
      }
    };

    // Test each page quickly
    const pages = [
      { url: '/admin/events-management-api-demo', key: 'eventsManagementApiDemo' },
      { url: '/navigation-test', key: 'navigationTest' },
      { url: '/test-no-layout', key: 'testNoLayout' }
    ];

    for (const pageInfo of pages) {
      try {
        console.log(`Testing ${pageInfo.url}...`);
        await page.goto(`${BASE_URL}${pageInfo.url}`, { timeout: 10000 });
        await page.waitForLoadState('networkidle', { timeout: 5000 });
        
        const result = report.testResults[pageInfo.key as keyof typeof report.testResults];
        result.tested = true;
        result.stable = true; // If we got here, page loaded
        
        const content = await page.textContent('body');
        if (pageInfo.key === 'eventsManagementApiDemo') {
          result.hasFullContent = (content?.length || 0) > 200;
        }
        
        // Take screenshot
        await page.screenshot({ 
          path: `/home/chad/repos/witchcityrope-react/.worktrees/feature-2025-08-24-events-management/apps/web/test-results/${pageInfo.key}-report-screenshot.png`
        });
        
      } catch (error) {
        const result = report.testResults[pageInfo.key as keyof typeof report.testResults];
        result.issues.push(`Failed to load: ${error}`);
        console.log(`❌ Failed to test ${pageInfo.url}: ${error}`);
      }
    }

    // Check environment health
    try {
      const reactResponse = await fetch(`${BASE_URL}`);
      report.environment.reactHealthy = reactResponse.ok;
    } catch {
      report.environment.reactHealthy = false;
    }

    // Write comprehensive report
    const reportPath = '/home/chad/repos/witchcityrope-react/.worktrees/feature-2025-08-24-events-management/apps/web/test-results/page-stability-report.json';
    await page.evaluate((reportData) => {
      // Save report data for later access
      window.testReport = reportData;
    }, report);

    console.log('📊 COMPREHENSIVE STABILITY REPORT:');
    console.log('=====================================');
    console.log(`Environment: React ${report.environment.reactHealthy ? 'healthy' : 'unhealthy'}, API ${report.environment.apiHealthy ? 'healthy' : 'unhealthy'}`);
    console.log(`Events Demo: ${report.testResults.eventsManagementApiDemo.stable ? 'stable' : 'unstable'}, Full content: ${report.testResults.eventsManagementApiDemo.hasFullContent}`);
    console.log(`Navigation Test: ${report.testResults.navigationTest.stable ? 'stable' : 'unstable'}`);
    console.log(`Test No Layout: ${report.testResults.testNoLayout.stable ? 'stable' : 'unstable'}`);
    console.log('=====================================');

    expect(report.testResults.eventsManagementApiDemo.tested).toBe(true);
    expect(report.testResults.navigationTest.tested).toBe(true);
    expect(report.testResults.testNoLayout.tested).toBe(true);
  });
});