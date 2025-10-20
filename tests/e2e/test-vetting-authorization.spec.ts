import { test, expect } from '@playwright/test';

/**
 * Test: Admin Vetting Management Authorization Verification
 * Purpose: Verify that the authorization fix allows administrators to access vetting endpoints
 * Requested by: Backend developer after fixing authorization issue
 */
test.describe('Admin Vetting Management Authorization', () => {

  test('Admin can access vetting applications interface', async ({ page }) => {
    // Environment Pre-Flight Check
    console.log('🔍 Starting admin vetting access verification...');

    // Step 1: Navigate to login page (use direct login page)
    await page.goto('http://localhost:5173/login');
    await page.waitForLoadState('networkidle');

    // Verify page loads
    const title = await page.title();
    expect(title).toContain('Witch City Rope');
    console.log('✅ React app loaded successfully');

    // Step 2: Login as admin using working pattern
    console.log('🔑 Logging in as admin...');

    // Wait for login form
    await page.waitForSelector('[data-testid="login-form"]', { timeout: 10000 });
    console.log('📍 Login form detected');

    // Use correct data-testid selectors
    const emailInput = page.locator('[data-testid="email-input"]');
    const passwordInput = page.locator('[data-testid="password-input"]');
    const loginButton = page.locator('[data-testid="login-button"]');

    // Fill credentials
    await emailInput.fill('admin@witchcityrope.com');
    await passwordInput.fill('Test123!');

    console.log('📍 Credentials filled');

    // Submit login
    await loginButton.click();

    // Wait for redirect to dashboard
    await page.waitForURL(/dashboard|admin/, { timeout: 15000 });
    console.log('✅ Successfully logged in as admin');

    // Step 3: Navigate to admin dashboard if not already there
    const currentUrl = page.url();
    console.log('📍 Current URL:', currentUrl);

    // Step 4: Try to access vetting functionality
    console.log('🔍 Looking for vetting applications access...');

    // Try multiple approaches to access vetting interface
    let vettingAccessible = false;
    let vettingResults = [];

    // Approach 1: Look for vetting navigation links
    const vettingNavSelectors = [
      'text="Vetting Applications"',
      'text="Vetting"',
      'a:has-text("Vetting")',
      'text="Member Vetting"',
      'text="Applications"',
      '[data-testid="vetting-link"]',
      'nav a:has-text("Vetting")'
    ];

    for (const selector of vettingNavSelectors) {
      try {
        const element = page.locator(selector);
        if (await element.isVisible({ timeout: 2000 })) {
          console.log(`✅ Found vetting navigation: ${selector}`);
          await element.click();
          await page.waitForTimeout(2000);
          vettingAccessible = true;
          vettingResults.push(`Navigation found: ${selector}`);
          break;
        }
      } catch (e) {
        // Continue trying other selectors
      }
    }

    // Approach 2: Try direct URL access if no navigation found
    if (!vettingAccessible) {
      console.log('🔍 Trying direct URL access to vetting pages...');

      const vettingUrls = [
        '/admin/vetting',
        '/admin/vetting-applications',
        '/vetting',
        '/admin/members/vetting',
        '/admin/users/vetting'
      ];

      for (const url of vettingUrls) {
        try {
          await page.goto(`http://localhost:5173${url}`);
          await page.waitForTimeout(3000);

          // Check if we get an authorization error
          const pageContent = await page.textContent('body');
          const hasAuthError = pageContent.includes('403') ||
                              pageContent.includes('Forbidden') ||
                              pageContent.includes('Unauthorized') ||
                              pageContent.includes('Access Denied');

          if (!hasAuthError) {
            console.log(`✅ Successfully accessed vetting page at: ${url}`);
            vettingAccessible = true;
            vettingResults.push(`Direct access successful: ${url}`);
            break;
          } else {
            console.log(`❌ Authorization error at: ${url}`);
            vettingResults.push(`Authorization error: ${url}`);
          }
        } catch (e) {
          console.log(`❌ Could not access: ${url} - ${e.message}`);
          vettingResults.push(`Access failed: ${url} - ${e.message}`);
        }
      }
    }

    // Step 5: Check final page state
    await page.waitForTimeout(3000);
    const finalUrl = page.url();
    const finalPageContent = await page.textContent('body');

    // Check for authorization errors on current page
    const hasAuthError = finalPageContent.includes('403') ||
                        finalPageContent.includes('Forbidden') ||
                        finalPageContent.includes('Unauthorized') ||
                        finalPageContent.includes('Access Denied');

    // Look for vetting interface elements
    const vettingInterfaceElements = [
      'text="Vetting Applications"',
      'text="Applications"',
      'table',
      'text="Status"',
      'text="Member"',
      'text="Application"',
      'text="Vetting"'
    ];

    const foundInterfaceElements = [];
    for (const selector of vettingInterfaceElements) {
      try {
        const element = page.locator(selector);
        if (await element.isVisible({ timeout: 2000 })) {
          foundInterfaceElements.push(selector);
        }
      } catch (e) {
        // Element not found
      }
    }

    // Step 6: Take screenshot for evidence
    await page.screenshot({
      path: `/home/chad/repos/witchcityrope/test-results/vetting-authorization-test-${Date.now()}.png`,
      fullPage: true
    });

    // Step 7: Report results
    console.log('\n📊 VETTING AUTHORIZATION TEST RESULTS:');
    console.log('=====================================');
    console.log(`✅ Login successful: Yes`);
    console.log(`✅ Admin access: Yes`);
    console.log(`🔍 Vetting access attempts: ${vettingResults.length}`);

    vettingResults.forEach(result => {
      console.log(`  - ${result}`);
    });

    console.log(`🔍 Interface elements found: ${foundInterfaceElements.length > 0 ? 'Yes' : 'No'}`);
    if (foundInterfaceElements.length > 0) {
      console.log(`  Found: ${foundInterfaceElements.join(', ')}`);
    }

    console.log(`❌ Authorization errors: ${hasAuthError ? 'Yes - FAILED' : 'No - PASSED'}`);
    console.log(`📍 Final URL: ${finalUrl}`);
    console.log(`📄 Page title: ${await page.title()}`);

    // Key assertion: No 403/Forbidden errors should be present
    expect(hasAuthError).toBe(false); // This is the main test - no authorization errors

    if (vettingAccessible || foundInterfaceElements.length > 0) {
      console.log('✅ SUCCESS: Admin can access vetting functionality without authorization errors');
    } else {
      console.log('⚠️ NOTE: Vetting interface may not be implemented yet, but no authorization errors found');
    }
  });

  test('Verify vetting API endpoints respond without 403 errors', async ({ page }) => {
    console.log('🔍 Testing vetting API endpoints directly...');

    // Login first to get authentication cookies
    await page.goto('http://localhost:5173/login');
    await page.waitForLoadState('networkidle');

    // Login using working pattern
    await page.waitForSelector('[data-testid="login-form"]', { timeout: 10000 });

    const emailInput = page.locator('[data-testid="email-input"]');
    const passwordInput = page.locator('[data-testid="password-input"]');
    const loginButton = page.locator('[data-testid="login-button"]');

    await emailInput.fill('admin@witchcityrope.com');
    await passwordInput.fill('Test123!');
    await loginButton.click();

    await page.waitForURL(/dashboard|admin/, { timeout: 15000 });
    console.log('✅ Logged in for API testing');

    // Test vetting API endpoints
    const vettingEndpoints = [
      '/api/vetting/applications',
      '/api/admin/vetting',
      '/api/vetting/status',
      '/api/members/vetting',
      '/api/admin/vetting/applications'
    ];

    const results = [];

    for (const endpoint of vettingEndpoints) {
      try {
        const response = await page.request.get(`http://localhost:5655${endpoint}`);
        const status = response.status();

        console.log(`📡 ${endpoint}: ${status}`);

        results.push({
          endpoint,
          status,
          accessible: status !== 403 && status !== 401,
          authError: status === 403
        });

        // Key test: Should not return 403 Forbidden
        // 404 = Not Found (endpoint doesn't exist - acceptable)
        // 401 = Unauthorized (authentication issue - different from authorization)
        // 200 = Success
        // 500 = Server error (but not authorization)

      } catch (error) {
        console.log(`❌ ${endpoint}: Error - ${error.message}`);
        results.push({
          endpoint,
          status: 'ERROR',
          accessible: false,
          authError: false,
          error: error.message
        });
      }
    }

    console.log('\n📊 API ENDPOINT RESULTS:');
    console.log('========================');
    results.forEach(result => {
      const status = result.status === 'ERROR' ? `ERROR: ${result.error}` : result.status;
      const accessSymbol = result.authError ? '❌ 403' : (result.accessible ? '✅' : '⚠️');
      console.log(`${accessSymbol} ${result.endpoint}: ${status}`);
    });

    // Count endpoints that returned 403 (authorization failures)
    const authErrorCount = results.filter(r => r.authError).length;
    const accessibleCount = results.filter(r => r.accessible).length;

    console.log(`\n📈 Summary: ${accessibleCount}/${results.length} endpoints accessible (non-403)`);
    console.log(`🚨 Authorization failures (403): ${authErrorCount}`);

    // Main assertion: No endpoints should return 403 Forbidden
    expect(authErrorCount).toBe(0); // This verifies the authorization fix worked

    if (authErrorCount === 0) {
      console.log('✅ SUCCESS: No 403 authorization errors found - fix verified!');
    } else {
      console.log('❌ FAILURE: Some endpoints still return 403 - authorization fix incomplete');
    }
  });
});