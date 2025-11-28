import { test, expect } from '@playwright/test';
import { AuthHelpers } from './test-utils/helpers/auth.helpers';

/**
 * CRITICAL E2E Tests for Dashboard Navigation
 *
 * These tests are designed to catch the navigation bugs that previous tests missed.
 * Key focus areas:
 * - VERIFY pages actually load (not just that buttons exist)
 * - CHECK for "Connection Problem" errors
 * - VALIDATE expected content appears
 * - MONITOR JavaScript and console errors
 */

test.describe('Dashboard Navigation - Critical Bug Detection', () => {
  let consoleErrors: string[] = [];
  let jsErrors: string[] = [];

  test.beforeEach(async ({ page }) => {
    // Reset error arrays for each test
    consoleErrors = [];
    jsErrors = [];

    // 🚨 CRITICAL: Monitor console errors - catches JavaScript crashes
    page.on('console', msg => {
      if (msg.type() === 'error') {
        const errorText = msg.text();
        console.log(`❌ Console Error: ${errorText}`);
        consoleErrors.push(errorText);

        // Specifically catch date/time errors that crash components
        if (errorText.includes('RangeError') || errorText.includes('Invalid time value')) {
          console.log(`🚨 CRITICAL: Date/Time error detected: ${errorText}`);
        }
      }
    });

    // 🚨 CRITICAL: Monitor JavaScript errors - catches page crashes
    page.on('pageerror', error => {
      const errorText = error.toString();
      console.log(`💥 JavaScript Error: ${errorText}`);
      jsErrors.push(errorText);
    });

    // Monitor API responses for context
    page.on('response', response => {
      if (!response.ok() && response.url().includes('/api/')) {
        console.log(`❌ API Error: ${response.status()} ${response.url()}`);
      }
    });
  });

  // API Health Pre-Check - MANDATORY before all tests
  test.beforeAll(async ({ request }) => {
    console.log('🔍 Pre-flight API health check...');
    const response = await request.get('/health');
    expect(response.ok()).toBeTruthy();
    const health = await response.json();
    expect(health.status).toBe('Healthy');
    console.log('✅ API health check passed');
  });

  test('User can navigate to dashboard after login and dashboard ACTUALLY loads', async ({ page }) => {
    console.log('🔍 Testing critical dashboard navigation flow...');

    // Step 1: Login using AuthHelpers
    await AuthHelpers.loginAs(page, 'member');
    console.log('✅ Logged in successfully using AuthHelpers');

    // Step 2: Wait for page to fully load and components to render
    await page.waitForLoadState('networkidle', { timeout: 10000 });
    await page.waitForTimeout(2000); // Allow React components to render

    // Step 3: 🚨 CRITICAL ERROR CHECKS - These catch the bugs previous tests missed
    if (jsErrors.length > 0) {
      console.log(`💥 FATAL: JavaScript errors detected on dashboard:`);
      jsErrors.forEach(error => console.log(`  💥 ${error}`));
      throw new Error(`Dashboard has JavaScript errors that crash the page: ${jsErrors.join('; ')}`);
    }

    if (consoleErrors.length > 0) {
      console.log(`❌ Console errors detected on dashboard:`);
      consoleErrors.forEach(error => console.log(`  ❌ ${error}`));

      // Filter out expected 401 errors during initial auth state loading
      const unexpectedErrors = consoleErrors.filter(error =>
        !error.includes('401') &&
        !error.includes('Unauthorized')
      );

      // Check for critical date/time errors
      const criticalErrors = unexpectedErrors.filter(error =>
        error.includes('RangeError') ||
        error.includes('Invalid time value') ||
        error.includes('Invalid Date')
      );

      if (criticalErrors.length > 0) {
        throw new Error(`CRITICAL: Dashboard has date/time errors that crash the page: ${criticalErrors.join('; ')}`);
      }

      if (unexpectedErrors.length > 0) {
        throw new Error(`Dashboard has console errors: ${unexpectedErrors.join('; ')}`);
      }
    }

    // Step 4: Verify dashboard content is actually displayed (not just that page loads)
    // Dashboard shows "Learning's Events" as the main heading
    await expect(page.locator('h1')).toContainText(/Welcome|Dashboard|Events/i, { timeout: 5000 });
    console.log('✅ Dashboard header verified');

    // Step 5: Check for "Connection Problem" error messages
    const connectionErrors = await page.locator('text=/Connection Problem|Failed to load|Error loading/i').count();
    if (connectionErrors > 0) {
      const errorText = await page.locator('text=/Connection Problem|Failed to load|Error loading/i').first().textContent();
      throw new Error(`Dashboard shows connection error: ${errorText}`);
    }

    // Step 6: Verify some dashboard content is displayed
    const dashboardContent = await page.locator('text=/Your Events|Quick Actions|Welcome|Browse All Events/i').count();
    expect(dashboardContent).toBeGreaterThan(0);
    console.log('✅ Dashboard content verified');

    // Step 7: Take screenshot for verification
    await page.screenshot({
      path: './test-results/dashboard-navigation-success.png',
      fullPage: true
    });

    console.log('✅ SUCCESS: User dashboard navigation test completed without errors');
  });

  test('User navigation to dashboard via direct URL works correctly', async ({ page }) => {
    console.log('🔍 Testing direct dashboard URL navigation...');

    // First login to establish session using AuthHelpers
    await AuthHelpers.loginAs(page, 'member');
    console.log('✅ Logged in successfully using AuthHelpers');

    // Now test direct URL navigation
    await page.goto('/dashboard');
    await page.waitForLoadState('networkidle');
    await page.waitForTimeout(2000);

    // Verify no errors (filter out expected 401 errors during auth state loading)
    const unexpectedConsoleErrors = consoleErrors.filter(error =>
      !error.includes('401') && !error.includes('Unauthorized')
    );
    if (jsErrors.length > 0 || unexpectedConsoleErrors.length > 0) {
      throw new Error(`Direct dashboard URL navigation failed with errors: JS(${jsErrors.length}) Console(${unexpectedConsoleErrors.length})`);
    }

    // Verify content loads - Dashboard shows "Learning's Events" or similar
    await expect(page.locator('h1')).toContainText(/Welcome|Dashboard|Events/i);

    console.log('✅ Direct dashboard URL navigation works');
  });

  test('Dashboard navigation persists through page refresh', async ({ page }) => {
    console.log('🔍 Testing dashboard persistence through page refresh...');

    // Login and navigate to dashboard using AuthHelpers
    await AuthHelpers.loginAs(page, 'member');
    console.log('✅ Logged in successfully using AuthHelpers');

    // Verify initial load - Dashboard shows "Learning's Events" or similar
    await page.waitForLoadState('networkidle');
    await expect(page.locator('h1')).toContainText(/Welcome|Dashboard|Events/i);

    // Refresh the page
    await page.reload();
    await page.waitForLoadState('networkidle');
    await page.waitForTimeout(2000);

    // Verify no errors after refresh (filter out expected 401 errors)
    const unexpectedConsoleErrors = consoleErrors.filter(error =>
      !error.includes('401') && !error.includes('Unauthorized')
    );
    if (jsErrors.length > 0 || unexpectedConsoleErrors.length > 0) {
      throw new Error(`Dashboard refresh failed with errors: JS(${jsErrors.length}) Console(${unexpectedConsoleErrors.length})`);
    }

    // Verify content still loads - Dashboard shows "Learning's Events" or similar
    await expect(page.locator('h1')).toContainText(/Welcome|Dashboard|Events/i);

    console.log('✅ Dashboard persists through page refresh');
  });

  test('Dashboard shows appropriate content for authenticated user', async ({ page }) => {
    console.log('🔍 Testing dashboard content verification...');

    // Login as member using AuthHelpers
    await AuthHelpers.loginAs(page, 'member');
    console.log('✅ Logged in successfully using AuthHelpers');

    await page.waitForLoadState('networkidle');
    await page.waitForTimeout(2000);

    // Verify no critical errors (filter out expected 401 errors)
    const unexpectedConsoleErrors = consoleErrors.filter(error =>
      !error.includes('401') && !error.includes('Unauthorized')
    );
    if (jsErrors.length > 0 || unexpectedConsoleErrors.length > 0) {
      throw new Error(`Dashboard content loading failed with errors`);
    }

    // Verify user-specific dashboard content - Dashboard shows "Learning's Events" or similar
    await expect(page.locator('h1')).toContainText(/Welcome|Dashboard|Events/i);

    // Check for typical dashboard elements (may vary based on implementation)
    const expectedElements = [
      'text=/Your Events|Upcoming Events|Event/i',
      'text=/Profile|Settings|Account/i',
      'text=/Browse|Events|Activities/i'
    ];

    let foundElements = 0;
    for (const selector of expectedElements) {
      const count = await page.locator(selector).count();
      if (count > 0) foundElements++;
    }

    expect(foundElements).toBeGreaterThan(0);
    console.log(`✅ Found ${foundElements} expected dashboard elements`);

    console.log('✅ Dashboard content verification completed');
  });

  test('Dashboard handles unauthenticated access correctly', async ({ page }) => {
    console.log('🔍 Testing unauthenticated dashboard access...');

    // Clear any existing session
    await page.context().clearCookies();

    // Try to access dashboard without authentication
    await page.goto('/dashboard');

    // Should redirect to login or show login prompt
    await page.waitForTimeout(3000);

    const currentUrl = page.url();
    console.log(`Current URL after unauthenticated access: ${currentUrl}`);

    // Should either be on login page or show login form
    const isOnLoginPage = currentUrl.includes('/login');
    const hasLoginForm = await page.locator('[data-testid="login-form"]').count() > 0;

    expect(isOnLoginPage || hasLoginForm).toBeTruthy();

    console.log('✅ Unauthenticated dashboard access handled correctly');
  });
});