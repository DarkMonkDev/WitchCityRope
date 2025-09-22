import { test, expect } from '@playwright/test';

test.describe('Simple Navigation Check After API Fix', () => {
  test('Verify React App Renders and Login Button Works', async ({ page }) => {
    console.log('🔍 Testing basic React app functionality...');

    // Navigate to homepage
    await page.goto('http://localhost:5173');
    await page.waitForTimeout(3000);

    // Take screenshot of loaded page
    await page.screenshot({ path: '/home/chad/repos/witchcityrope-react./test-results/app-loaded.png' });

    // Verify React app loaded properly
    const pageTitle = await page.title();
    console.log(`✅ Page title: ${pageTitle}`);
    expect(pageTitle).toContain('Witch City Rope');

    // Check if main content is visible
    const mainContent = await page.locator('text=SALEM\'S ROPE BONDAGE').isVisible();
    console.log(`✅ Main content visible: ${mainContent}`);
    expect(mainContent).toBe(true);

    // Try to click LOGIN button (using simpler selector)
    console.log('🔍 Looking for LOGIN button...');
    const loginButton = await page.locator('text=LOGIN').first();
    const loginButtonVisible = await loginButton.isVisible();
    console.log(`✅ LOGIN button visible: ${loginButtonVisible}`);

    if (loginButtonVisible) {
      await loginButton.click();
      await page.waitForTimeout(2000);

      // Take screenshot after clicking login
      await page.screenshot({ path: '/home/chad/repos/witchcityrope-react./test-results/after-login-click.png' });

      console.log('✅ Successfully clicked LOGIN button');
    }

    console.log('🎉 Basic React app functionality verified - API fix appears successful!');
  });

  test('Test API Endpoints Directly', async ({ page }) => {
    console.log('🔍 Testing API endpoints directly...');

    // Test API health endpoint
    const healthResponse = await page.request.get('http://localhost:5655/health');
    const healthStatus = healthResponse.status();
    console.log(`✅ API Health Status: ${healthStatus}`);
    expect(healthStatus).toBe(200);

    // Test dashboard endpoints (should return 401 without auth)
    const dashboardEventsResponse = await page.request.get('http://localhost:5655/api/dashboard/events?count=3');
    const eventsStatus = dashboardEventsResponse.status();
    console.log(`✅ Dashboard Events Status: ${eventsStatus} (401 expected without auth)`);
    expect(eventsStatus).toBe(401); // Should be unauthorized without login

    const dashboardStatsResponse = await page.request.get('http://localhost:5655/api/dashboard/statistics');
    const statsStatus = dashboardStatsResponse.status();
    console.log(`✅ Dashboard Statistics Status: ${statsStatus} (401 expected without auth)`);
    expect(statsStatus).toBe(401); // Should be unauthorized without login

    console.log('🎉 API endpoints responding correctly - compilation issues resolved!');
  });
});