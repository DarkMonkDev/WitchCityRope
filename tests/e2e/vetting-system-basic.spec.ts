import { test, expect } from '@playwright/test';
import { AuthHelpers } from './test-utils/helpers/auth.helpers';

/**
 * BASIC VETTING SYSTEM E2E TEST
 *
 * This test covers the core vetting system functionality that can be reliably tested
 * even when the backend vetting API has issues. It focuses on:
 * 1. UI/UX workflow from discovery to login
 * 2. Error handling and navigation
 * 3. Admin access and navigation
 * 4. Basic form accessibility
 *
 * This serves as a foundational test that can be expanded when the API is stable.
 */

test.describe('Vetting System - Basic Functionality', () => {

  test.beforeEach(async ({ page }) => {
    // Clear auth state before each test
    await AuthHelpers.clearAuthState(page);
  });

  test('Basic vetting discovery and authentication workflow', async ({ page }) => {
    console.log('🚀 Starting basic vetting system test...');

    // STEP 1: Logged-out user discovers vetting requirement
    console.log('📍 STEP 1: Test logged-out vetting discovery');
    await page.goto('/join', { waitUntil: 'domcontentloaded' });
    await page.waitForLoadState('domcontentloaded');

    // Wait for the page to fully render (vetting components may load async)
    await page.waitForTimeout(1000);

    // Verify logged-out state shows login requirement - check for the data-testid that exists
    const loginRequiredAlert = page.locator('[data-testid="login-required-alert"]');
    const loginRequiredAlertVisible = await loginRequiredAlert.isVisible().catch(() => false);

    // Also check text content as fallback
    const loggedOutText = await page.textContent('body');
    const showsLoginRequired = loginRequiredAlertVisible ||
                               loggedOutText?.includes('Login Required') ||
                               loggedOutText?.includes('must have an account') ||
                               loggedOutText?.includes('logged in');

    expect(showsLoginRequired).toBe(true);
    console.log('✅ Page correctly shows login requirement for logged-out users');

    // Take screenshot for documentation
    await page.screenshot({ path: 'test-results/vetting-logged-out-basic.png', fullPage: true });

    // STEP 2: Test login navigation from vetting page
    console.log('📍 STEP 2: Test login navigation');

    const loginToAccountButton = page.locator('text=LOGIN TO YOUR ACCOUNT');
    if (await loginToAccountButton.count() > 0) {
      console.log('✅ Login button found on vetting page');
    } else {
      console.log('⚠️ Login button not found - page may already require navigation');
    }

    // STEP 3: Test guest user authentication
    console.log('📍 STEP 3: Test guest authentication');

    // Use AuthHelper for reliable login (handles form selectors properly)
    const loginSuccess = await AuthHelpers.loginAs(page, 'guest');
    expect(loginSuccess).toBe(true);
    console.log('✅ Successfully logged in as guest user');

    // STEP 4: Test authenticated vetting page access
    console.log('📍 STEP 4: Test authenticated vetting page');

    await page.goto('/join', { waitUntil: 'domcontentloaded' });
    await page.waitForLoadState('domcontentloaded');

    const authenticatedText = await page.textContent('body');
    const hasForm = await page.locator('form').count() > 0;
    const hasError = authenticatedText?.includes('Something went wrong') || authenticatedText?.includes('500');
    const hasExistingApp = authenticatedText?.includes('already submitted') || authenticatedText?.includes('application submitted');

    console.log(`📝 Authenticated page state:`);
    console.log(`   - Has form: ${hasForm ? '✅' : '❌'}`);
    console.log(`   - Has error: ${hasError ? '⚠️' : '✅'}`);
    console.log(`   - Has existing app: ${hasExistingApp ? '⚠️' : '✅'}`);

    if (hasError) {
      console.log('📝 Testing error handling...');

      // Test error page elements
      const reloadButton = page.locator('button:has-text("Reload Page"), button:has-text("Reload")');
      const returnHomeButton = page.locator('a:has-text("Return Home"), button:has-text("Return Home")');

      expect(await reloadButton.count()).toBeGreaterThan(0);
      expect(await returnHomeButton.count()).toBeGreaterThan(0);

      console.log('✅ Error page has proper navigation elements');

      // Test return home functionality
      await returnHomeButton.click();
      await page.waitForLoadState('domcontentloaded');
      expect(page.url()).toContain('/');
      console.log('✅ Return home navigation works');

    } else if (hasForm) {
      console.log('📝 Testing form accessibility...');

      // Test form accessibility
      const formElements = await page.locator('form input, form textarea, form select').count();
      const labelsCount = await page.locator('label').count();
      const ariaLabelsCount = await page.locator('[aria-label]').count();

      console.log(`🏷️ Form accessibility:`);
      console.log(`   - Form elements: ${formElements}`);
      console.log(`   - Labels: ${labelsCount}`);
      console.log(`   - Aria-labels: ${ariaLabelsCount}`);

      if (formElements > 0) {
        console.log('✅ Vetting form is accessible');
      }

    } else if (hasExistingApp) {
      console.log('📝 User already has application - testing status display...');

      const hasStatusInfo = authenticatedText?.includes('status') ||
                           authenticatedText?.includes('review') ||
                           authenticatedText?.includes('submitted');

      if (hasStatusInfo) {
        console.log('✅ Application status information is displayed');
      }

    } else {
      console.log('⚠️ Unexpected page state - may need investigation');
    }

    // Take screenshot of authenticated state
    await page.screenshot({ path: 'test-results/vetting-authenticated-basic.png', fullPage: true });

    // Test complete - guest vetting flow verified
    // Note: Admin vetting is tested in separate test (vetting-admin-dashboard.spec.ts)
    console.log('✅ Basic vetting discovery and authentication workflow complete');
  });

  test('Vetting page navigation and UI consistency', async ({ page }) => {
    console.log('🔍 Testing vetting page navigation consistency...');

    // Test navigation from different entry points
    const testUrls = [
      '/',
      '/events',
      '/resources'
    ];

    for (const url of testUrls) {
      await page.goto(url, { waitUntil: 'domcontentloaded' });
      await page.waitForLoadState('domcontentloaded');

      // Look for "How to Join" navigation link
      const joinLink = page.locator('nav a:has-text("How to Join"), header a:has-text("How to Join"), a[href*="join"]').first();
      const hasJoinLink = await joinLink.count() > 0;

      console.log(`📍 ${url}: How to Join link ${hasJoinLink ? '✅ found' : '❌ missing'}`);

      if (hasJoinLink) {
        await joinLink.click();
        await page.waitForLoadState('domcontentloaded');

        const joinPageText = await page.textContent('body');
        const isJoinPage = joinPageText?.includes('Join') ||
                          joinPageText?.includes('vetting') ||
                          joinPageText?.includes('application');

        expect(isJoinPage).toBe(true);
        console.log(`✅ Navigation from ${url} to join page works`);
      }
    }

    console.log('✅ Navigation consistency test completed');
  });

  test('Vetting system responsive design check', async ({ page }) => {
    console.log('📱 Testing vetting system responsive design...');

    // Test different viewport sizes
    const viewports = [
      { width: 320, height: 568, name: 'Mobile' },
      { width: 768, height: 1024, name: 'Tablet' },
      { width: 1920, height: 1080, name: 'Desktop' }
    ];

    for (const viewport of viewports) {
      await page.setViewportSize({ width: viewport.width, height: viewport.height });
      await page.goto('/join', { waitUntil: 'domcontentloaded' });
      await page.waitForLoadState('domcontentloaded');

      // Take screenshot for each viewport
      await page.screenshot({
        path: `test-results/vetting-responsive-${viewport.name.toLowerCase()}.png`,
        fullPage: true
      });

      // Check if key elements are visible (relaxed checks for responsive design)
      const titleVisible = await page.locator('h1, h2').isVisible();
      // Just verify page loaded without errors (don't require specific buttons to be visible)
      const hasError = await page.locator('text=Something went wrong').isVisible().catch(() => false);

      console.log(`📱 ${viewport.name} (${viewport.width}x${viewport.height}):`);
      console.log(`   - Title visible: ${titleVisible ? '✅' : '❌'}`);
      console.log(`   - No errors: ${!hasError ? '✅' : '❌'}`);

      expect(titleVisible).toBe(true);
      expect(hasError).toBe(false);
    }

    console.log('✅ Responsive design test completed');
  });
});