import { test, expect } from '@playwright/test';

test.describe('Debug Join Route', () => {
  test('Check what loads on /join route', async ({ page }) => {
    console.log('🔍 Debugging /join route...');

    // Navigate to the join route
    await page.goto('http://localhost:5173/join');
    await page.waitForLoadState('networkidle');

    // Take screenshot
    await page.screenshot({ path: 'test-results/join-route-debug.png', fullPage: true });

    // Get page title
    const title = await page.title();
    console.log(`📖 Page title: ${title}`);

    // Get page content
    const bodyText = await page.textContent('body');
    console.log(`📝 Page content length: ${bodyText?.length} characters`);
    console.log(`📝 Page content preview: ${bodyText?.substring(0, 500)}...`);

    // Check for form elements
    const forms = await page.locator('form').count();
    console.log(`📋 Number of forms found: ${forms}`);

    // Check for input elements
    const inputs = await page.locator('input').count();
    console.log(`📝 Number of inputs found: ${inputs}`);

    // Check for any error messages
    const errorElements = await page.locator('.error, .alert-danger, [class*="error"]').count();
    console.log(`❌ Number of error elements: ${errorElements}`);

    // Check for specific content
    const hasJoinContent = bodyText?.toLowerCase().includes('join') || false;
    const hasFormContent = bodyText?.toLowerCase().includes('form') || false;
    const hasVettingContent = bodyText?.toLowerCase().includes('vetting') || false;

    console.log(`📋 Content analysis:`);
    console.log(`   - Contains "join": ${hasJoinContent}`);
    console.log(`   - Contains "form": ${hasFormContent}`);
    console.log(`   - Contains "vetting": ${hasVettingContent}`);

    // Check URL after navigation
    const currentUrl = page.url();
    console.log(`📍 Current URL: ${currentUrl}`);

    // Check if redirected
    if (!currentUrl.includes('/join')) {
      console.log(`⚠️ Redirected from /join to ${currentUrl}`);
    }

    // Check console errors
    page.on('console', msg => {
      if (msg.type() === 'error') {
        console.log(`💥 Console error: ${msg.text()}`);
      }
    });

    // Check for React rendering
    const rootElement = await page.locator('#root').innerHTML();
    console.log(`⚛️ React root content length: ${rootElement.length} characters`);

    if (rootElement.length === 0) {
      console.log('❌ React app not rendering - empty root element');
    }
  });
});