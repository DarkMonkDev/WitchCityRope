import { test, expect } from '@playwright/test';

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
    const response = await request.get('http://localhost:5655/health');
    expect(response.ok()).toBeTruthy();
    const health = await response.json();
    expect(health.status).toBe('Healthy');
    console.log('✅ API health check passed');
  });

  test('User can navigate to dashboard after login and dashboard ACTUALLY loads', async ({ page }) => {
    console.log('🔍 Testing critical dashboard navigation flow...');

    // Step 1: Navigate to login page
    await page.goto('http://localhost:5173/login');
    console.log('📍 Navigated to login page');

    // Step 2: Verify login form is available
    await expect(page.locator('[data-testid="login-form"]')).toBeVisible({ timeout: 10000 });
    await expect(page.locator('[data-testid="email-input"]')).toBeVisible();
    await expect(page.locator('[data-testid="password-input"]')).toBeVisible();
    await expect(page.locator('[data-testid="login-button"]')).toBeVisible();

    // Step 3: Login with test credentials
    await page.locator('[data-testid="email-input"]').fill('member@witchcityrope.com');
    await page.locator('[data-testid="password-input"]').fill('Test123!');

    console.log('🔐 Filled login credentials');

    // Step 4: Submit login form
    await page.locator('[data-testid="login-button"]').click();
    console.log('🎯 Clicked login button');

    // Step 5: Wait for navigation to dashboard with proper timeout
    try {
      await page.waitForURL('**/dashboard', { timeout: 15000 });
      console.log('✅ Navigation to dashboard completed');
    } catch (error) {
      console.log('❌ Navigation timeout - checking current URL');
      console.log(`Current URL: ${page.url()}`);
      throw new Error(`Failed to navigate to dashboard: ${error}`);
    }

    // Step 6: Wait for page to fully load and components to render
    await page.waitForLoadState('networkidle', { timeout: 10000 });
    await page.waitForTimeout(2000); // Allow React components to render

    // Step 7: 🚨 CRITICAL ERROR CHECKS - These catch the bugs previous tests missed
    if (jsErrors.length > 0) {
      console.log(`💥 FATAL: JavaScript errors detected on dashboard:`);
      jsErrors.forEach(error => console.log(`  💥 ${error}`));
      throw new Error(`Dashboard has JavaScript errors that crash the page: ${jsErrors.join('; ')}`);
    }

    if (consoleErrors.length > 0) {
      console.log(`❌ Console errors detected on dashboard:`);
      consoleErrors.forEach(error => console.log(`  ❌ ${error}`));

      // Check for critical date/time errors
      const criticalErrors = consoleErrors.filter(error =>
        error.includes('RangeError') ||
        error.includes('Invalid time value') ||
        error.includes('Invalid Date')
      );

      if (criticalErrors.length > 0) {
        throw new Error(`CRITICAL: Dashboard has date/time errors that crash the page: ${criticalErrors.join('; ')}`);
      }

      throw new Error(`Dashboard has console errors: ${consoleErrors.join('; ')}`);
    }

    // Step 8: Verify dashboard content is actually displayed (not just that page loads)
    await expect(page.locator('h1')).toContainText(/Welcome|Dashboard/i, { timeout: 5000 });
    console.log('✅ Dashboard header verified');

    // Step 9: Check for "Connection Problem" error messages
    const connectionErrors = await page.locator('text=/Connection Problem|Failed to load|Error loading/i').count();
    if (connectionErrors > 0) {
      const errorText = await page.locator('text=/Connection Problem|Failed to load|Error loading/i').first().textContent();
      throw new Error(`Dashboard shows connection error: ${errorText}`);
    }

    // Step 10: Verify some dashboard content is displayed
    const dashboardContent = await page.locator('text=/Your Events|Quick Actions|Welcome|Browse All Events/i').count();
    expect(dashboardContent).toBeGreaterThan(0);
    console.log('✅ Dashboard content verified');

    // Step 11: Take screenshot for verification
    await page.screenshot({
      path: '/home/chad/repos/witchcityrope-react/test-results/dashboard-navigation-success.png',
      fullPage: true
    });

    console.log('✅ SUCCESS: User dashboard navigation test completed without errors');
  });

  test('User navigation to dashboard via direct URL works correctly', async ({ page }) => {
    console.log('🔍 Testing direct dashboard URL navigation...');

    // First login to establish session
    await page.goto('http://localhost:5173/login');
    await page.locator('[data-testid="email-input"]').fill('member@witchcityrope.com');
    await page.locator('[data-testid="password-input"]').fill('Test123!');
    await page.locator('[data-testid="login-button"]').click();
    await page.waitForURL('**/dashboard', { timeout: 15000 });

    // Now test direct URL navigation
    await page.goto('http://localhost:5173/dashboard');
    await page.waitForLoadState('networkidle');
    await page.waitForTimeout(2000);

    // Verify no errors
    if (jsErrors.length > 0 || consoleErrors.length > 0) {
      throw new Error(`Direct dashboard URL navigation failed with errors: JS(${jsErrors.length}) Console(${consoleErrors.length})`);
    }

    // Verify content loads
    await expect(page.locator('h1')).toContainText(/Welcome|Dashboard/i);

    console.log('✅ Direct dashboard URL navigation works');
  });

  test('Dashboard navigation persists through page refresh', async ({ page }) => {
    console.log('🔍 Testing dashboard persistence through page refresh...');

    // Login and navigate to dashboard
    await page.goto('http://localhost:5173/login');
    await page.locator('[data-testid="email-input"]').fill('member@witchcityrope.com');
    await page.locator('[data-testid="password-input"]').fill('Test123!');
    await page.locator('[data-testid="login-button"]').click();
    await page.waitForURL('**/dashboard', { timeout: 15000 });

    // Verify initial load
    await page.waitForLoadState('networkidle');
    await expect(page.locator('h1')).toContainText(/Welcome|Dashboard/i);

    // Refresh the page
    await page.reload();
    await page.waitForLoadState('networkidle');
    await page.waitForTimeout(2000);

    // Verify no errors after refresh
    if (jsErrors.length > 0 || consoleErrors.length > 0) {
      throw new Error(`Dashboard refresh failed with errors: JS(${jsErrors.length}) Console(${consoleErrors.length})`);
    }

    // Verify content still loads
    await expect(page.locator('h1')).toContainText(/Welcome|Dashboard/i);

    console.log('✅ Dashboard persists through page refresh');
  });

  test('Dashboard shows appropriate content for authenticated user', async ({ page }) => {
    console.log('🔍 Testing dashboard content verification...');

    // Login as member
    await page.goto('http://localhost:5173/login');
    await page.locator('[data-testid="email-input"]').fill('member@witchcityrope.com');
    await page.locator('[data-testid="password-input"]').fill('Test123!');
    await page.locator('[data-testid="login-button"]').click();
    await page.waitForURL('**/dashboard', { timeout: 15000 });
    await page.waitForLoadState('networkidle');
    await page.waitForTimeout(2000);

    // Verify no critical errors
    if (jsErrors.length > 0 || consoleErrors.length > 0) {
      throw new Error(`Dashboard content loading failed with errors`);
    }

    // Verify user-specific dashboard content
    await expect(page.locator('h1')).toContainText(/Welcome|Dashboard/i);

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
    await page.goto('http://localhost:5173/dashboard');

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