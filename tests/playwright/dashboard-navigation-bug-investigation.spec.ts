import { test, expect } from '@playwright/test';

test.describe('Dashboard Navigation Bug Investigation', () => {

  test('User Dashboard Navigation Bug Investigation', async ({ page }) => {
    console.log('🔍 Starting User Dashboard Bug Investigation...');

    // Navigate to the app
    await page.goto('http://localhost:5173');
    console.log('✅ Navigated to app');

    // Take initial screenshot
    await page.screenshot({ path: 'user-dashboard-bug-initial.png', fullPage: true });

    // Check if the page loaded at all
    const title = await page.title();
    console.log(`📄 Page title: ${title}`);

    // Check if React app mounted
    const rootContent = await page.locator('#root').innerHTML();
    console.log(`🔍 Root element content length: ${rootContent.length} characters`);

    if (rootContent.length === 0) {
      console.log('❌ React app not mounted - root element is empty');

      // Check for console errors
      const errors: string[] = [];
      page.on('console', msg => {
        if (msg.type() === 'error') {
          errors.push(`Console Error: ${msg.text()}`);
        }
      });

      // Check for page errors
      const pageErrors: string[] = [];
      page.on('pageerror', err => {
        pageErrors.push(`Page Error: ${err.message}`);
      });

      // Wait a bit for errors to surface
      await page.waitForTimeout(5000);

      console.log(`❌ Console errors found: ${errors.length}`);
      errors.forEach(error => console.log(error));

      console.log(`❌ Page errors found: ${pageErrors.length}`);
      pageErrors.forEach(error => console.log(error));

      return; // Can't test navigation if app doesn't load
    }

    console.log('✅ React app appears to be mounted');

    // Check if login link exists
    const loginLink = page.locator('text=Login').first();
    const loginExists = await loginLink.isVisible();
    console.log(`🔍 Login link visible: ${loginExists}`);

    if (!loginExists) {
      console.log('❌ Login link not found - UI may not be rendering');
      await page.screenshot({ path: 'user-dashboard-bug-no-login.png', fullPage: true });
      return;
    }

    // Try to login as regular user
    console.log('🔑 Attempting login as regular user...');
    await loginLink.click();

    // Wait for navigation
    await page.waitForTimeout(2000);

    // Fill login form
    const emailInput = page.locator('input[type="email"]');
    const passwordInput = page.locator('input[type="password"]');
    const submitButton = page.locator('button:has-text("Sign In")');

    if (await emailInput.isVisible()) {
      await emailInput.fill('member@witchcityrope.com');
      await passwordInput.fill('Test123!');

      console.log('🔑 Submitting login form...');
      await submitButton.click();

      // Wait for login to process
      await page.waitForTimeout(3000);

      // Check for API errors during login
      const responses: string[] = [];
      page.on('response', response => {
        if (response.status() >= 400) {
          responses.push(`❌ API Error: ${response.status()} ${response.url()}`);
        }
      });

      await page.waitForTimeout(2000);

      // Check if we're logged in (look for user-specific elements)
      const welcomeText = page.locator('text=Welcome').first();
      const dashboardLink = page.locator('text=Dashboard').first();

      const isLoggedIn = await welcomeText.isVisible() || await dashboardLink.isVisible();
      console.log(`🔍 Login appears successful: ${isLoggedIn}`);

      if (!isLoggedIn) {
        console.log('❌ Login failed - cannot test dashboard navigation');
        await page.screenshot({ path: 'user-dashboard-bug-login-failed.png', fullPage: true });
        responses.forEach(response => console.log(response));
        return;
      }

      // Now test dashboard navigation
      console.log('📊 Testing Dashboard navigation...');

      const dashboardLinkExists = await dashboardLink.isVisible();
      console.log(`🔍 Dashboard link visible: ${dashboardLinkExists}`);

      if (dashboardLinkExists) {
        console.log('👆 Clicking Dashboard link...');
        await dashboardLink.click();

        // Wait for navigation
        await page.waitForTimeout(5000);

        // Check current URL
        const currentUrl = page.url();
        console.log(`🔍 Current URL after Dashboard click: ${currentUrl}`);

        // Check if dashboard content loaded
        const dashboardContent = await page.locator('[data-testid="dashboard"]').isVisible().catch(() => false);
        const hasDashboardHeading = await page.locator('h1:has-text("Dashboard")').isVisible().catch(() => false);
        const hasUserStats = await page.locator('[data-testid="user-stats"]').isVisible().catch(() => false);

        console.log(`🔍 Dashboard content visible: ${dashboardContent}`);
        console.log(`🔍 Dashboard heading visible: ${hasDashboardHeading}`);
        console.log(`🔍 User stats visible: ${hasUserStats}`);

        // Take screenshot of dashboard attempt
        await page.screenshot({ path: 'user-dashboard-bug-navigation-result.png', fullPage: true });

        // Log any API failures
        responses.forEach(response => console.log(response));

        if (!dashboardContent && !hasDashboardHeading && !hasUserStats) {
          console.log('❌ Dashboard page failed to load properly');
        } else {
          console.log('✅ Dashboard page appears to have loaded');
        }
      } else {
        console.log('❌ Dashboard link not found after login');
      }
    } else {
      console.log('❌ Login form not found');
    }
  });

  test('Admin Event Details Navigation Bug Investigation', async ({ page }) => {
    console.log('🔍 Starting Admin Event Details Bug Investigation...');

    // Navigate to the app
    await page.goto('http://localhost:5173');
    console.log('✅ Navigated to app');

    // Quick check if app loads
    const rootContent = await page.locator('#root').innerHTML();
    if (rootContent.length === 0) {
      console.log('❌ React app not mounted - cannot test admin navigation');
      return;
    }

    // Try to login as admin
    console.log('🔑 Attempting login as admin...');
    const loginLink = page.locator('text=Login').first();

    if (await loginLink.isVisible()) {
      await loginLink.click();
      await page.waitForTimeout(2000);

      // Fill admin credentials
      await page.locator('input[type="email"]').fill('admin@witchcityrope.com');
      await page.locator('input[type="password"]').fill('Test123!');
      await page.locator('button:has-text("Sign In")').click();

      // Wait for login
      await page.waitForTimeout(3000);

      // Check for admin menu
      const adminLink = page.locator('text=Admin').first();
      const adminExists = await adminLink.isVisible();
      console.log(`🔍 Admin link visible: ${adminExists}`);

      if (adminExists) {
        console.log('👆 Clicking Admin link...');
        await adminLink.click();
        await page.waitForTimeout(2000);

        // Look for Events link in admin menu
        const eventsLink = page.locator('text=Events').first();
        const eventsExists = await eventsLink.isVisible();
        console.log(`🔍 Events link visible: ${eventsExists}`);

        if (eventsExists) {
          console.log('👆 Clicking Events link...');
          await eventsLink.click();
          await page.waitForTimeout(3000);

          // Check current URL
          const currentUrl = page.url();
          console.log(`🔍 Current URL after Events click: ${currentUrl}`);

          // Look for event list
          const eventItems = await page.locator('[data-testid="event-item"]').count();
          const hasEventsList = await page.locator('h1:has-text("Events")').isVisible().catch(() => false);

          console.log(`🔍 Event items found: ${eventItems}`);
          console.log(`🔍 Events list heading visible: ${hasEventsList}`);

          if (eventItems > 0) {
            console.log('👆 Clicking first event...');
            await page.locator('[data-testid="event-item"]').first().click();
            await page.waitForTimeout(3000);

            // Check event details page
            const eventDetailsUrl = page.url();
            const hasEventTitle = await page.locator('h1').isVisible().catch(() => false);
            const hasEventDetails = await page.locator('[data-testid="event-details"]').isVisible().catch(() => false);

            console.log(`🔍 Event details URL: ${eventDetailsUrl}`);
            console.log(`🔍 Event title visible: ${hasEventTitle}`);
            console.log(`🔍 Event details visible: ${hasEventDetails}`);

            // Take screenshot
            await page.screenshot({ path: 'admin-event-details-bug-result.png', fullPage: true });

            if (!hasEventTitle && !hasEventDetails) {
              console.log('❌ Event details page failed to load');
            } else {
              console.log('✅ Event details page appears to have loaded');
            }
          } else {
            console.log('❌ No events found in events list');
            await page.screenshot({ path: 'admin-event-details-no-events.png', fullPage: true });
          }
        } else {
          console.log('❌ Events link not found in admin menu');
        }
      } else {
        console.log('❌ Admin link not found - user may not have admin permissions');
      }
    } else {
      console.log('❌ Login link not found');
    }
  });
});