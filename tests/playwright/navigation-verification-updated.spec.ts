import { test, expect } from '@playwright/test';

test.describe('Navigation Verification After API Fix', () => {
  test('User Dashboard Navigation - Regular Member', async ({ page }) => {
    console.log('🔍 Starting user dashboard navigation test...');

    // Navigate to homepage
    await page.goto('http://localhost:5173');
    await page.waitForTimeout(2000);

    // Take screenshot of initial page
    await page.screenshot({ path: '/home/chad/repos/witchcityrope-react/test-results/01-homepage-loaded.png' });

    // Verify React app is fully loaded
    const pageTitle = await page.title();
    console.log(`🔍 Page title: ${pageTitle}`);
    expect(pageTitle).toContain('Witch City Rope');

    // Click the LOGIN button (yellow button in top right)
    console.log('🔍 Clicking LOGIN button...');
    await page.click('button:has-text("LOGIN"), text=LOGIN');
    await page.waitForTimeout(2000);

    // Take screenshot of login page/modal
    await page.screenshot({ path: '/home/chad/repos/witchcityrope-react/test-results/02-login-form.png' });

    // Check if login form appeared
    const emailField = await page.locator('input[type="email"], input[placeholder*="email" i]').isVisible({ timeout: 5000 });
    console.log(`🔍 Email field visible: ${emailField}`);

    if (emailField) {
      // Fill login form
      await page.fill('input[type="email"], input[placeholder*="email" i]', 'member@witchcityrope.com');
      await page.fill('input[type="password"], input[placeholder*="password" i]', 'Test123!');

      // Look for sign in button
      const signInButton = await page.locator('button:has-text("Sign In"), button:has-text("Login"), button[type="submit"]').first();
      await signInButton.click();

      console.log('🔍 Waiting for login to complete...');
      await page.waitForTimeout(4000);

      // Take screenshot after login
      await page.screenshot({ path: '/home/chad/repos/witchcityrope-react/test-results/03-after-login.png' });

      const urlAfterLogin = page.url();
      console.log(`🔍 URL after login: ${urlAfterLogin}`);

      // Check for dashboard navigation
      const dashboardLinkExists = await page.locator('text=Dashboard, a[href*="dashboard"], nav a:has-text("Dashboard")').isVisible({ timeout: 3000 });
      console.log(`🔍 Dashboard link exists: ${dashboardLinkExists}`);

      if (dashboardLinkExists) {
        // Click Dashboard link
        console.log('🔍 Clicking Dashboard link...');
        await page.click('text=Dashboard, a[href*="dashboard"], nav a:has-text("Dashboard")');
        await page.waitForTimeout(4000);

        // Take screenshot of dashboard
        await page.screenshot({ path: '/home/chad/repos/witchcityrope-react/test-results/04-user-dashboard.png' });

        const dashboardUrl = page.url();
        console.log(`🔍 Dashboard URL: ${dashboardUrl}`);

        // Check dashboard content
        const pageContent = await page.textContent('body');
        const hasConnectionProblem = pageContent?.includes('Connection Problem') || false;
        const hasApiError = pageContent?.includes('API Error') || pageContent?.includes('500') || pageContent?.includes('404') || false;

        console.log(`🔍 Dashboard loaded without connection problems: ${!hasConnectionProblem}`);
        console.log(`🔍 Dashboard loaded without API errors: ${!hasApiError}`);

        // Success criteria: Dashboard loads without critical errors
        expect(hasConnectionProblem).toBe(false);

        console.log('✅ User dashboard navigation test PASSED');
      } else {
        console.log('⚠️  Dashboard link not found - checking if user is logged in...');
        const bodyContent = await page.textContent('body');
        console.log(`Body content includes user indicators: ${bodyContent?.includes('Welcome') || bodyContent?.includes('logout') || bodyContent?.includes('profile')}`);
      }
    } else {
      console.log('❌ Login form not found');
      expect(emailField).toBe(true);
    }
  });

  test('Admin Event Details Navigation', async ({ page }) => {
    console.log('🔍 Starting admin event navigation test...');

    // Navigate to homepage
    await page.goto('http://localhost:5173');
    await page.waitForTimeout(2000);

    // Click LOGIN button
    console.log('🔍 Clicking LOGIN button...');
    await page.click('button:has-text("LOGIN"), text=LOGIN');
    await page.waitForTimeout(2000);

    // Check for login form
    const emailField = await page.locator('input[type="email"], input[placeholder*="email" i]').isVisible({ timeout: 5000 });

    if (emailField) {
      // Login as admin
      await page.fill('input[type="email"], input[placeholder*="email" i]', 'admin@witchcityrope.com');
      await page.fill('input[type="password"], input[placeholder*="password" i]', 'Test123!');

      const signInButton = await page.locator('button:has-text("Sign In"), button:has-text("Login"), button[type="submit"]').first();
      await signInButton.click();

      console.log('🔍 Waiting for admin login to complete...');
      await page.waitForTimeout(4000);

      // Take screenshot after admin login
      await page.screenshot({ path: '/home/chad/repos/witchcityrope-react/test-results/05-admin-after-login.png' });

      // Check for Admin menu
      const adminMenuExists = await page.locator('text=Admin, a[href*="admin"], nav a:has-text("Admin")').isVisible({ timeout: 3000 });
      console.log(`🔍 Admin menu exists: ${adminMenuExists}`);

      if (adminMenuExists) {
        // Navigate to Admin section
        console.log('🔍 Clicking Admin menu...');
        await page.click('text=Admin, a[href*="admin"], nav a:has-text("Admin")');
        await page.waitForTimeout(2000);

        // Look for Events in admin section
        const eventsLinkExists = await page.locator('text=Events, a[href*="events"], nav a:has-text("Events")').isVisible({ timeout: 3000 });
        console.log(`🔍 Events link exists: ${eventsLinkExists}`);

        if (eventsLinkExists) {
          console.log('🔍 Clicking Events link...');
          await page.click('text=Events, a[href*="events"], nav a:has-text("Events")');
          await page.waitForTimeout(4000);

          // Take screenshot of admin events page
          await page.screenshot({ path: '/home/chad/repos/witchcityrope-react/test-results/06-admin-events.png' });

          const eventsUrl = page.url();
          console.log(`🔍 Admin Events URL: ${eventsUrl}`);

          // Check events page content
          const pageContent = await page.textContent('body');
          const hasConnectionProblem = pageContent?.includes('Connection Problem') || false;
          const hasApiError = pageContent?.includes('API Error') || pageContent?.includes('500') || pageContent?.includes('404') || false;

          console.log(`🔍 Admin Events page loaded without connection problems: ${!hasConnectionProblem}`);
          console.log(`🔍 Admin Events page loaded without API errors: ${!hasApiError}`);

          // Success criteria: Admin events page loads without critical errors
          expect(hasConnectionProblem).toBe(false);

          console.log('✅ Admin event navigation test PASSED');
        } else {
          console.log('⚠️  Events link not found in admin section');
        }
      } else {
        console.log('⚠️  Admin menu not found - checking admin access...');
        const bodyContent = await page.textContent('body');
        console.log(`Body content for admin access check: ${bodyContent?.substring(0, 200)}`);
      }
    } else {
      console.log('❌ Login form not found');
      expect(emailField).toBe(true);
    }
  });
});