import { test, expect } from '@playwright/test';

/**
 * IMPROVED Test: Dashboard Page State Check
 *
 * This test now includes comprehensive error monitoring and actual functionality validation
 * instead of just taking screenshots. This demonstrates how tests should verify navigation works.
 */

test.describe('Dashboard Page State Validation', () => {
  let consoleErrors: string[] = [];
  let jsErrors: string[] = [];

  test.beforeEach(async ({ page }) => {
    // Reset error arrays
    consoleErrors = [];
    jsErrors = [];

    // Monitor console errors
    page.on('console', msg => {
      if (msg.type() === 'error') {
        const errorText = msg.text();
        console.log(`❌ Console Error: ${errorText}`);
        consoleErrors.push(errorText);
      }
    });

    // Monitor JavaScript errors
    page.on('pageerror', error => {
      const errorText = error.toString();
      console.log(`💥 JavaScript Error: ${errorText}`);
      jsErrors.push(errorText);
    });
  });

  // API Health Pre-Check
  test.beforeAll(async ({ request }) => {
    const response = await request.get('http://localhost:5655/health');
    expect(response.ok()).toBeTruthy();
    const health = await response.json();
    expect(health.status).toBe('Healthy');
  });

  test('verify dashboard page actually loads and functions correctly', async ({ page }) => {
    console.log('🔗 Navigating to React app...');
    await page.goto('http://localhost:5173');

    // Verify home page loads without errors
    await page.waitForLoadState('networkidle');
    if (jsErrors.length > 0 || consoleErrors.length > 0) {
      throw new Error(`Home page has errors: JS(${jsErrors.length}) Console(${consoleErrors.length})`);
    }

    console.log('📸 Taking home page screenshot...');
    await page.screenshot({ path: '/home/chad/repos/witchcityrope-react/test-results/home-page-2025-09-18.png', fullPage: true });

    console.log('🔗 Navigating to login page...');
    await page.goto('http://localhost:5173/login');

    // Verify login page loads and has required elements
    await page.waitForLoadState('networkidle');
    await expect(page.locator('[data-testid="login-form"]')).toBeVisible({ timeout: 5000 });
    await expect(page.locator('[data-testid="email-input"]')).toBeVisible();
    await expect(page.locator('[data-testid="password-input"]')).toBeVisible();
    await expect(page.locator('[data-testid="login-button"]')).toBeVisible();

    console.log('📸 Taking login page screenshot...');
    await page.screenshot({ path: '/home/chad/repos/witchcityrope-react/test-results/login-page-2025-09-18.png', fullPage: true });

    // Test actual login functionality
    console.log('🔐 Testing login functionality...');
    await page.locator('[data-testid="email-input"]').fill('member@witchcityrope.com');
    await page.locator('[data-testid="password-input"]').fill('Test123!');
    await page.locator('[data-testid="login-button"]').click();

    // Verify successful navigation to dashboard
    await page.waitForURL('**/dashboard', { timeout: 15000 });
    await page.waitForLoadState('networkidle');
    await page.waitForTimeout(2000);

    // CRITICAL: Check for errors that crash the dashboard
    if (jsErrors.length > 0) {
      console.log(`💥 CRITICAL: Dashboard has JavaScript errors:`);
      jsErrors.forEach(error => console.log(`  💥 ${error}`));
      throw new Error(`Dashboard login navigation failed with JavaScript errors: ${jsErrors.join('; ')}`);
    }

    if (consoleErrors.length > 0) {
      console.log(`❌ CRITICAL: Dashboard has console errors:`);
      consoleErrors.forEach(error => console.log(`  ❌ ${error}`));

      // Check for critical date/time errors that crash components
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

    // Verify dashboard content loads
    await expect(page.locator('h1')).toContainText(/Welcome|Dashboard/i);

    // Check for connection problems
    const connectionErrors = await page.locator('text=/Connection Problem|Failed to load|Error loading/i').count();
    if (connectionErrors > 0) {
      const errorText = await page.locator('text=/Connection Problem|Failed to load|Error loading/i').first().textContent();
      throw new Error(`Dashboard shows connection error: ${errorText}`);
    }

    console.log('📸 Taking dashboard screenshot...');
    await page.screenshot({ path: '/home/chad/repos/witchcityrope-react/test-results/dashboard-page-2025-09-18.png', fullPage: true });

    console.log('✅ Dashboard page state validation completed successfully');
    console.log('🎯 This test now ACTUALLY verifies navigation works, not just that pages exist');
  });
});
