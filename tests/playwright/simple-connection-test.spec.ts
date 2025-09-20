import { test, expect } from '@playwright/test';

test.describe('Simple Webapp-API Connection Test', () => {
  test('React app loads and API is reachable', async ({ page }) => {
    console.log('🔍 Testing basic webapp-API connectivity...');

    // Navigate to the React app
    await page.goto('http://localhost:5173');

    // Wait for the page to load
    await page.waitForTimeout(3000);

    // Check if the page title is correct
    const title = await page.title();
    console.log(`📄 Page title: ${title}`);
    expect(title).toContain('Witch City Rope');

    // Check if the React app root element has content
    const rootContent = await page.locator('#root').innerHTML();
    console.log(`🎯 Root content length: ${rootContent.length} characters`);

    // Test API directly from the browser context
    const apiResponse = await page.request.get('http://localhost:5655/health');
    console.log(`🌐 API health status: ${apiResponse.status()}`);
    expect(apiResponse.status()).toBe(200);

    const healthData = await apiResponse.json();
    console.log(`💚 API health response: ${JSON.stringify(healthData)}`);

    // Test events API
    const eventsResponse = await page.request.get('http://localhost:5655/api/events');
    console.log(`📅 Events API status: ${eventsResponse.status()}`);

    // Take a screenshot
    await page.screenshot({ path: 'test-results/webapp-state.png', fullPage: true });

    console.log('✅ Connection test completed');
  });
});