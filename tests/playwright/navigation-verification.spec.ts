import { test, expect } from '@playwright/test';

test.describe('Navigation Verification After API Fix', () => {
  test('User Dashboard Navigation - Regular Member', async ({ page }) => {
    console.log('🔍 Starting user dashboard navigation test...');

    // Navigate to login page
    await page.goto('http://localhost:5173');
    await page.waitForTimeout(2000);

    // Take screenshot of initial page
    await page.screenshot({ path: '/home/chad/repos/witchcityrope-react/test-results/01-initial-page.png' });

    // Click Login
    await page.click('text=Login');
    await page.waitForTimeout(1000);

    // Take screenshot of login page
    await page.screenshot({ path: '/home/chad/repos/witchcityrope-react/test-results/02-login-page.png' });

    // Login as regular user
    await page.fill('input[type="email"]', 'member@witchcityrope.com');
    await page.fill('input[type="password"]', 'Test123!');
    await page.click('button:has-text("Sign In")');

    console.log('🔍 Waiting for login to complete...');
    await page.waitForTimeout(3000);

    // Take screenshot after login
    await page.screenshot({ path: '/home/chad/repos/witchcityrope-react/test-results/03-after-login.png' });

    const urlAfterLogin = page.url();
    console.log(`🔍 URL after login: ${urlAfterLogin}`);

    // Check if dashboard link is available
    const dashboardLinkExists = await page.locator('text=Dashboard').isVisible();
    console.log(`🔍 Dashboard link exists: ${dashboardLinkExists}`);

    if (dashboardLinkExists) {
      // Click Dashboard link
      console.log('🔍 Clicking Dashboard link...');
      await page.click('text=Dashboard');
      await page.waitForTimeout(3000);

      // Take screenshot of dashboard
      await page.screenshot({ path: '/home/chad/repos/witchcityrope-react/test-results/04-user-dashboard.png' });

      const dashboardUrl = page.url();
      console.log(`🔍 Dashboard URL: ${dashboardUrl}`);

      // Check for content or error messages
      const pageContent = await page.textContent('body');
      const hasConnectionProblem = pageContent?.includes('Connection Problem') || false;
      const hasError = pageContent?.includes('Error') || false;

      console.log(`🔍 Dashboard loaded successfully: ${!hasConnectionProblem && !hasError}`);
      console.log(`🔍 Has connection problem: ${hasConnectionProblem}`);
      console.log(`🔍 Has error: ${hasError}`);

      // Log network errors from console
      const consoleErrors = [];
      page.on('console', msg => {
        if (msg.type() === 'error') {
          consoleErrors.push(msg.text());
        }
      });

      // Wait for any async operations
      await page.waitForTimeout(2000);

      if (consoleErrors.length > 0) {
        console.log('🚨 Console errors found:');
        consoleErrors.forEach(error => console.log(`   - ${error}`));
      }

      // The test should pass if dashboard loads without major errors
      expect(hasConnectionProblem).toBe(false);
    } else {
      console.log('❌ Dashboard link not found - may indicate login failed');
      expect(dashboardLinkExists).toBe(true);
    }
  });

  test('Admin Event Details Navigation', async ({ page }) => {
    console.log('🔍 Starting admin event navigation test...');

    // Navigate to login page
    await page.goto('http://localhost:5173');
    await page.waitForTimeout(2000);

    // Login as admin
    await page.click('text=Login');
    await page.waitForTimeout(1000);

    await page.fill('input[type="email"]', 'admin@witchcityrope.com');
    await page.fill('input[type="password"]', 'Test123!');
    await page.click('button:has-text("Sign In")');

    console.log('🔍 Waiting for admin login to complete...');
    await page.waitForTimeout(3000);

    // Take screenshot after admin login
    await page.screenshot({ path: '/home/chad/repos/witchcityrope-react/test-results/05-admin-after-login.png' });

    // Check if Admin menu is available
    const adminMenuExists = await page.locator('text=Admin').isVisible();
    console.log(`🔍 Admin menu exists: ${adminMenuExists}`);

    if (adminMenuExists) {
      // Navigate to Admin → Events
      console.log('🔍 Clicking Admin menu...');
      await page.click('text=Admin');
      await page.waitForTimeout(1000);

      console.log('🔍 Clicking Events in admin menu...');
      await page.click('text=Events');
      await page.waitForTimeout(3000);

      // Take screenshot of admin events page
      await page.screenshot({ path: '/home/chad/repos/witchcityrope-react/test-results/06-admin-events.png' });

      const eventsUrl = page.url();
      console.log(`🔍 Admin Events URL: ${eventsUrl}`);

      // Check if events list loads
      const pageContent = await page.textContent('body');
      const hasConnectionProblem = pageContent?.includes('Connection Problem') || false;
      const hasError = pageContent?.includes('Error') || false;

      console.log(`🔍 Admin Events page loaded successfully: ${!hasConnectionProblem && !hasError}`);

      // Try to find and click on an event (if any exist)
      const eventRowExists = await page.locator('.event-row, [data-event], tr:has(td)').first().isVisible({ timeout: 5000 }).catch(() => false);
      console.log(`🔍 Event rows found: ${eventRowExists}`);

      if (eventRowExists) {
        console.log('🔍 Clicking on first event...');
        await page.locator('.event-row, [data-event], tr:has(td)').first().click();
        await page.waitForTimeout(3000);

        // Take screenshot of event details
        await page.screenshot({ path: '/home/chad/repos/witchcityrope-react/test-results/07-event-details.png' });

        const detailsUrl = page.url();
        console.log(`🔍 Event Details URL: ${detailsUrl}`);

        const detailsContent = await page.textContent('body');
        const detailsHasError = detailsContent?.includes('Connection Problem') || detailsContent?.includes('Error') || false;

        console.log(`🔍 Event details loaded successfully: ${!detailsHasError}`);
      } else {
        console.log('⚠️  No events found to test event details navigation');
      }

      // The test should pass if admin events page loads
      expect(hasConnectionProblem).toBe(false);
    } else {
      console.log('❌ Admin menu not found - may indicate admin privileges not working');
      expect(adminMenuExists).toBe(true);
    }
  });
});