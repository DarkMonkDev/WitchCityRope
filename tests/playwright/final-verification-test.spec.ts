import { test, expect } from '@playwright/test';

test.describe('Final Verification - Login and Dashboard Integration', () => {
  test('Complete login to dashboard flow should work without errors', async ({ page }) => {
    const results = {
      loginWorks: false,
      dashboardLoads: false,
      noConnectionProblem: true,
      adminMenuVisible: false,
      dashboardShowsData: false,
      consoleLogs: [],
      networkErrors: [],
      screenshots: {}
    };

    // Monitor console logs and network errors
    page.on('console', msg => {
      results.consoleLogs.push({ type: msg.type(), text: msg.text() });
    });

    page.on('response', response => {
      if (!response.ok() && response.url().includes('/api/')) {
        results.networkErrors.push({
          url: response.url(),
          status: response.status(),
          statusText: response.statusText()
        });
      }
    });

    console.log('🔍 Starting final verification test...');

    // Step 1: Navigate to React app
    console.log('📍 Step 1: Loading React app');
    await page.goto('http://localhost:5173');
    await page.waitForTimeout(2000);

    // Take initial screenshot
    await page.screenshot({ path: '/home/chad/repos/witchcityrope-react./test-results/01-initial-load.png', fullPage: true });
    results.screenshots.initialLoad = '/home/chad/repos/witchcityrope-react./test-results/01-initial-load.png';

    // Step 2: Check if login link is available
    const loginButton = page.locator('text=Login');
    if (await loginButton.isVisible()) {
      console.log('✅ Login button found');
      await loginButton.click();
      await page.waitForTimeout(1000);

      // Take login page screenshot
      await page.screenshot({ path: '/home/chad/repos/witchcityrope-react./test-results/02-login-page.png', fullPage: true });
      results.screenshots.loginPage = '/home/chad/repos/witchcityrope-react./test-results/02-login-page.png';
    } else {
      console.log('❌ Login button not found');
      await page.screenshot({ path: '/home/chad/repos/witchcityrope-react./test-results/02-no-login-button.png', fullPage: true });
      return results;
    }

    // Step 3: Attempt login as admin
    console.log('📍 Step 3: Attempting admin login');

    // Fill login form
    const emailInput = page.locator('input[type="email"]');
    const passwordInput = page.locator('input[type="password"]');
    const signInButton = page.locator('button:has-text("Sign In")');

    if (await emailInput.isVisible() && await passwordInput.isVisible()) {
      await emailInput.fill('admin@witchcityrope.com');
      await passwordInput.fill('Test123!');

      // Take filled form screenshot
      await page.screenshot({ path: '/home/chad/repos/witchcityrope-react./test-results/03-filled-form.png', fullPage: true });
      results.screenshots.filledForm = '/home/chad/repos/witchcityrope-react./test-results/03-filled-form.png';

      await signInButton.click();
      console.log('✅ Login form submitted');
      results.loginWorks = true;
    } else {
      console.log('❌ Login form not found');
      await page.screenshot({ path: '/home/chad/repos/witchcityrope-react./test-results/03-no-login-form.png', fullPage: true });
      return results;
    }

    // Step 4: Wait for navigation and check dashboard
    console.log('📍 Step 4: Waiting for dashboard...');
    await page.waitForTimeout(5000); // Give time for navigation and API calls

    const currentUrl = page.url();
    const bodyText = await page.textContent('body');

    console.log(`Current URL: ${currentUrl}`);
    console.log(`Body contains: ${bodyText?.substring(0, 200)}...`);

    // Check for success indicators
    results.noConnectionProblem = !bodyText?.includes('Connection Problem');
    results.adminMenuVisible = bodyText?.includes('Admin') || false;
    results.dashboardLoads = bodyText?.includes('Dashboard') || bodyText?.includes('Welcome') || currentUrl.includes('dashboard') || false;
    results.dashboardShowsData = bodyText?.includes('events') || bodyText?.includes('statistics') || bodyText?.includes('members') || false;

    // Take final dashboard screenshot
    await page.screenshot({ path: '/home/chad/repos/witchcityrope-react./test-results/04-dashboard-final.png', fullPage: true });
    results.screenshots.dashboardFinal = '/home/chad/repos/witchcityrope-react./test-results/04-dashboard-final.png';

    // Output detailed results
    console.log('\n📊 FINAL VERIFICATION RESULTS:');
    console.log(`✅/❌ Login works: ${results.loginWorks ? '✅' : '❌'}`);
    console.log(`✅/❌ Dashboard loads: ${results.dashboardLoads ? '✅' : '❌'}`);
    console.log(`✅/❌ No "Connection Problem": ${results.noConnectionProblem ? '✅' : '❌'}`);
    console.log(`✅/❌ Admin menu visible: ${results.adminMenuVisible ? '✅' : '❌'}`);
    console.log(`✅/❌ Dashboard shows data: ${results.dashboardShowsData ? '✅' : '❌'}`);

    console.log(`\n🔍 Console Logs (${results.consoleLogs.length}):`);
    results.consoleLogs.forEach(log => console.log(`  ${log.type}: ${log.text}`));

    console.log(`\n🚨 Network Errors (${results.networkErrors.length}):`);
    results.networkErrors.forEach(error => console.log(`  ${error.status} ${error.url}`));

    // Determine overall success
    const overallSuccess = results.loginWorks &&
                          results.dashboardLoads &&
                          results.noConnectionProblem &&
                          results.adminMenuVisible;

    expect(overallSuccess,
      `Final verification failed:
       Login works: ${results.loginWorks}
       Dashboard loads: ${results.dashboardLoads}
       No connection problem: ${results.noConnectionProblem}
       Admin menu visible: ${results.adminMenuVisible}
       Network errors: ${results.networkErrors.length}
      `).toBe(true);

    // Store results for reporting
    await page.evaluate((data) => {
      localStorage.setItem('finalVerificationResults', JSON.stringify(data));
    }, results);
  });

  test('Test different user roles', async ({ page }) => {
    const userTests = [
      { email: 'teacher@witchcityrope.com', role: 'Teacher' },
      { email: 'member@witchcityrope.com', role: 'Member' }
    ];

    for (const user of userTests) {
      console.log(`🔍 Testing login for ${user.email}`);

      await page.goto('http://localhost:5173');
      await page.waitForTimeout(1000);

      const loginButton = page.locator('text=Login');
      if (await loginButton.isVisible()) {
        await loginButton.click();
        await page.waitForTimeout(500);

        await page.fill('input[type="email"]', user.email);
        await page.fill('input[type="password"]', 'Test123!');
        await page.click('button:has-text("Sign In")');

        await page.waitForTimeout(3000);

        const bodyText = await page.textContent('body');
        const hasConnectionProblem = bodyText?.includes('Connection Problem');

        console.log(`${user.role} login - Connection Problem: ${hasConnectionProblem ? 'YES' : 'NO'}`);

        await page.screenshot({
          path: `/home/chad/repos/witchcityrope-react./test-results/user-${user.role.toLowerCase()}-result.png`,
          fullPage: true
        });
      }
    }
  });
});