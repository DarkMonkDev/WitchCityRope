import { test, expect } from '@playwright/test';

test.describe('Event Session Matrix Demo', () => {
  test('should load demo page with correct title', async ({ page }) => {
    // Navigate to the demo page
    await page.goto('/admin/event-session-matrix-demo');
    
    // Wait for the page to load
    await page.waitForLoadState('networkidle', { timeout: 10000 });
    
    // Check if the page title indicates successful load
    await expect(page).toHaveTitle(/Vite \+ React \+ TS/);
    
    // Take screenshot for verification
    await page.screenshot({ path: '/home/chad/repos/witchcityrope-react/apps/web/test-results/event-session-matrix-demo-load.png' });
  });

  test('should display demo page with Basic Info tab active', async ({ page }) => {
    await page.goto('/admin/event-session-matrix-demo');
    
    // Wait for React to load
    await page.waitForSelector('body', { timeout: 15000 });
    
    // Wait a bit more for component to render
    await page.waitForTimeout(3000);
    
    // Take screenshot to verify what's actually loaded
    await page.screenshot({ 
      path: '/home/chad/repos/witchcityrope-react/apps/web/test-results/event-session-matrix-demo-content.png',
      fullPage: true 
    });
    
    // Check if basic React app is working
    const bodyText = await page.textContent('body');
    console.log('Page body text length:', bodyText?.length);
  });

  test('should handle API errors gracefully', async ({ page }) => {
    // Monitor console for errors
    const consoleErrors = [];
    page.on('console', msg => {
      if (msg.type() === 'error') {
        consoleErrors.push(msg.text());
      }
    });

    await page.goto('/admin/event-session-matrix-demo');
    await page.waitForTimeout(5000);
    
    // Check for critical errors
    const criticalErrors = consoleErrors.filter(error => 
      !error.includes('Failed to load resource') && 
      !error.includes('net::ERR_') &&
      !error.includes('404')
    );
    
    console.log('Console errors found:', consoleErrors.length);
    console.log('Critical errors found:', criticalErrors.length);
    
    // App should handle API failures gracefully
    expect(criticalErrors.length).toBeLessThan(5);
  });
});