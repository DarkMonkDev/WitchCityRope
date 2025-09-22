import { test, expect } from '@playwright/test';

test.describe('Vetting Form Communication Diagnostic', () => {
  test('Check React app loads and vetting form is accessible', async ({ page }) => {
    console.log('🔍 Testing React app and vetting form communication...');

    // Navigate to React app
    await page.goto('http://localhost:5173');

    // Check if React app loads
    const title = await page.title();
    console.log(`📍 Page title: ${title}`);
    expect(title).toContain('Witch City Rope');

    // Take screenshot of homepage
    await page.screenshot({ path: '/tmp/homepage.png' });

    // Try to navigate to join page
    try {
      await page.goto('http://localhost:5173/join');
      console.log('✅ Join page navigation successful');

      // Wait for any React content to load
      await page.waitForTimeout(2000);

      // Take screenshot of join page
      await page.screenshot({ path: '/tmp/join-page.png' });

      // Check for form elements
      const formElements = await page.locator('form, input, textarea, button').count();
      console.log(`📍 Found ${formElements} form elements on join page`);

      // Check for any error messages
      const errorMessages = await page.locator('[class*="error"], .error, [data-testid*="error"]').count();
      console.log(`⚠️ Found ${errorMessages} error elements on page`);

      // Check console for errors
      const consoleErrors = [];
      page.on('console', (msg) => {
        if (msg.type() === 'error') {
          consoleErrors.push(msg.text());
        }
      });

      // Check network requests
      const failedRequests = [];
      page.on('response', (response) => {
        if (!response.ok() && response.url().includes('api')) {
          failedRequests.push(`${response.status()} ${response.url()}`);
        }
      });

      await page.waitForTimeout(1000);

      console.log(`🚨 Console errors: ${consoleErrors.length}`);
      consoleErrors.forEach(error => console.log(`   - ${error}`));

      console.log(`🌐 Failed API requests: ${failedRequests.length}`);
      failedRequests.forEach(request => console.log(`   - ${request}`));

    } catch (error) {
      console.log(`❌ Error navigating to join page: ${error.message}`);
      throw error;
    }
  });

  test('Test API communication directly', async ({ request }) => {
    console.log('🔍 Testing API endpoints directly...');

    // Test API health
    const healthResponse = await request.get('http://localhost:5655/health');
    console.log(`📍 API health: ${healthResponse.status()}`);
    expect(healthResponse.status()).toBe(200);

    // Test vetting health
    const vettingHealthResponse = await request.get('http://localhost:5655/api/vetting/health');
    console.log(`📍 Vetting API health: ${vettingHealthResponse.status()}`);
    expect(vettingHealthResponse.status()).toBe(200);

    // Test vetting endpoint with empty data (should get validation errors)
    const vettingResponse = await request.post('http://localhost:5655/api/vetting/applications', {
      data: {}
    });
    console.log(`📍 Vetting endpoint: ${vettingResponse.status()}`);

    if (vettingResponse.status() === 400) {
      const errorBody = await vettingResponse.json();
      console.log(`✅ Validation errors returned correctly`);
      console.log(`📍 Required fields: ${Object.keys(errorBody.errors || {}).length}`);
    }
  });
});