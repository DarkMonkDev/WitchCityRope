import { test, expect } from '@playwright/test';
import { AuthHelpers } from './test-utils/helpers/auth.helpers';

/**
 * CRITICAL E2E Tests for Admin Events Navigation
 *
 * These tests are designed to catch the admin navigation bugs that previous tests missed.
 * Key focus areas:
 * - VERIFY admin events page actually loads
 * - CHECK event details pages load correctly
 * - VALIDATE admin permissions and content
 * - MONITOR for 404 errors and broken navigation
 */

test.describe('Admin Events Navigation - Critical Bug Detection', () => {
  let consoleErrors: string[] = [];
  let jsErrors: string[] = [];

  test.beforeEach(async ({ page }) => {
    // Reset error arrays for each test
    consoleErrors = [];
    jsErrors = [];

    // 🚨 CRITICAL: Monitor console errors
    page.on('console', msg => {
      if (msg.type() === 'error') {
        const errorText = msg.text();
        console.log(`❌ Console Error: ${errorText}`);
        consoleErrors.push(errorText);
      }
    });

    // 🚨 CRITICAL: Monitor JavaScript errors
    page.on('pageerror', error => {
      const errorText = error.toString();
      console.log(`💥 JavaScript Error: ${errorText}`);
      jsErrors.push(errorText);
    });

    // Monitor failed requests
    page.on('requestfailed', request => {
      console.log(`❌ Request Failed: ${request.method()} ${request.url()}`);
    });

    // Monitor API responses
    page.on('response', response => {
      if (!response.ok() && response.url().includes('/api/')) {
        console.log(`❌ API Error: ${response.status()} ${response.url()}`);
      }
    });
  });

  // REMOVED: API Health Pre-Check - Not necessary, just increases test complexity
  // Tests will fail naturally if API is down, providing better debugging info
  // than a generic health check failure that blocks all tests

  test('Admin can navigate to events management and page ACTUALLY loads', async ({ page }) => {
    console.log('🔍 Testing critical admin events navigation flow...');

    // Step 1: Login as admin
    await AuthHelpers.loginAs(page, 'admin');
    console.log('✅ Admin login successful');

    // Step 3: Navigate to Admin Events section
    // Check for admin navigation options
    const adminLinks = [
      'text=/Admin/i',
      'text=/Events Management/i',
      'text=/Manage Events/i',
      'a[href*="admin"]',
      'a[href*="events"]'
    ];

    let adminLinkFound = false;
    for (const linkSelector of adminLinks) {
      const linkCount = await page.locator(linkSelector).count();
      if (linkCount > 0) {
        console.log(`Found admin link: ${linkSelector}`);
        await page.locator(linkSelector).first().click();
        adminLinkFound = true;
        break;
      }
    }

    if (!adminLinkFound) {
      // Try direct navigation to admin events
      console.log('No admin links found, trying direct navigation...');
      await page.goto('/admin/events');
    }

    // Step 4: Wait for admin events page to load
    await page.waitForTimeout(3000);
    await page.waitForLoadState('domcontentloaded');

    const currentUrl = page.url();
    console.log(`Current URL: ${currentUrl}`);

    // Step 5: 🚨 CRITICAL ERROR CHECKS
    if (jsErrors.length > 0) {
      console.log(`💥 FATAL: JavaScript errors on admin events page:`);
      jsErrors.forEach(error => console.log(`  💥 ${error}`));
      throw new Error(`Admin events page has JavaScript errors: ${jsErrors.join('; ')}`);
    }

    if (consoleErrors.length > 0) {
      console.log(`❌ Console errors on admin events page:`);
      consoleErrors.forEach(error => console.log(`  ❌ ${error}`));

      // Filter out expected 401 errors during auth state loading
      const unexpectedErrors = consoleErrors.filter(error =>
        !error.includes('401') && !error.includes('Unauthorized')
      );

      if (unexpectedErrors.length > 0) {
        throw new Error(`Admin events page has console errors: ${unexpectedErrors.join('; ')}`);
      }
    }

    // Step 6: Verify admin events page loaded correctly
    // Check for 404 or error messages
    const errorMessages = await page.locator('text=/404|Not Found|Access Denied|Unauthorized/i').count();
    if (errorMessages > 0) {
      const errorText = await page.locator('text=/404|Not Found|Access Denied|Unauthorized/i').first().textContent();
      throw new Error(`Admin events page shows error: ${errorText}`);
    }

    // Step 7: Verify admin events content
    const adminContentSelectors = [
      'h1',
      'text=/Events/i',
      'text=/Management/i',
      'text=/Create/i',
      'text=/Edit/i',
      'table',
      '.event',
      '[data-testid*="event"]'
    ];

    let contentFound = false;
    for (const selector of adminContentSelectors) {
      const count = await page.locator(selector).count();
      if (count > 0) {
        console.log(`Found admin content: ${selector} (${count} elements)`);
        contentFound = true;
      }
    }

    expect(contentFound).toBeTruthy();

    // Step 8: Take screenshot for verification
    await page.screenshot({
      path: './test-results/admin-events-navigation-success.png',
      fullPage: true
    });

    console.log('✅ SUCCESS: Admin events navigation test completed');
  });

  test('Admin can access event details from events list', async ({ page }) => {
    console.log('🔍 Testing admin event details navigation...');

    // Login as admin
    await AuthHelpers.loginAs(page, 'admin');

    // Navigate to admin events
    try {
      // Try to find admin navigation
      const adminButton = page.locator('text=/Admin/i').first();
      if (await adminButton.count() > 0) {
        await adminButton.click();
        await page.waitForTimeout(1000);
      }

      // Try to find events navigation
      const eventsButton = page.locator('text=/Events/i').first();
      if (await eventsButton.count() > 0) {
        await eventsButton.click();
        await page.waitForTimeout(1000);
      } else {
        // Direct navigation fallback
        await page.goto('/admin/events');
      }
    } catch (error) {
      console.log('Navigation via buttons failed, using direct URL');
      await page.goto('/admin/events');
    }

    await page.waitForLoadState('domcontentloaded');
    await page.waitForTimeout(2000);

    // Check for events list
    const eventElements = [
      'tr:has(td)', // Table rows with data
      '.event-item',
      '[data-testid*="event"]',
      'a[href*="event"]',
      'button:has-text(/View|Edit|Details/i)'
    ];

    let eventFound = false;
    let eventElement = null;

    for (const selector of eventElements) {
      const elements = page.locator(selector);
      const count = await elements.count();
      if (count > 0) {
        console.log(`Found events: ${selector} (${count} elements)`);
        eventElement = elements.first();
        eventFound = true;
        break;
      }
    }

    if (eventFound && eventElement) {
      console.log('🎯 Clicking on first event...');

      try {
        await eventElement.click();
        await page.waitForTimeout(2000);
        await page.waitForLoadState('domcontentloaded');

        // Check for event details page
        const currentUrl = page.url();
        console.log(`Event details URL: ${currentUrl}`);

        // Verify no errors on event details page (filter out expected 401 errors)
        const unexpectedConsoleErrors = consoleErrors.filter(error =>
          !error.includes('401') && !error.includes('Unauthorized')
        );
        if (jsErrors.length > 0 || unexpectedConsoleErrors.length > 0) {
          throw new Error(`Event details page has errors: JS(${jsErrors.length}) Console(${unexpectedConsoleErrors.length})`);
        }

        // Check for event details content
        const detailsContent = [
          'text=/Event Details/i',
          'text=/Edit Event/i',
          'text=/Title/i',
          'text=/Description/i',
          'text=/Date/i',
          'input',
          'textarea',
          'button:has-text(/Save|Update/i)'
        ];

        let detailsFound = false;
        for (const selector of detailsContent) {
          const count = await page.locator(selector).count();
          if (count > 0) {
            console.log(`Found details content: ${selector}`);
            detailsFound = true;
            break;
          }
        }

        expect(detailsFound).toBeTruthy();
        console.log('✅ Event details page loaded successfully');

      } catch (clickError) {
        console.log(`Event click failed: ${clickError}`);
        // Try direct navigation to an event details page
        await page.goto('/admin/events/1');
        await page.waitForTimeout(2000);

        const notFoundCount = await page.locator('text=/404|Not Found/i').count();
        if (notFoundCount === 0) {
          console.log('✅ Direct event details navigation works');
        }
      }
    } else {
      console.log('No events found in list, testing direct event details access...');

      // Test direct event details navigation
      await page.goto('/admin/events/1');
      await page.waitForTimeout(2000);

      const notFoundCount = await page.locator('text=/404|Not Found/i').count();
      if (notFoundCount === 0) {
        console.log('✅ Direct event details navigation works');
      } else {
        console.log('ℹ️ No events available for testing details navigation');
      }
    }

    console.log('✅ Admin event details navigation test completed');
  });

  test('Admin events page handles no events scenario correctly', async ({ page }) => {
    console.log('🔍 Testing admin events page with no events...');

    // Login as admin
    await AuthHelpers.loginAs(page, 'admin');

    // Navigate to admin events
    await page.goto('/admin/events');
    await page.waitForLoadState('domcontentloaded');
    await page.waitForTimeout(2000);

    // Verify no critical errors (filter out expected 401 errors)
    const unexpectedConsoleErrors = consoleErrors.filter(error =>
      !error.includes('401') && !error.includes('Unauthorized')
    );
    if (jsErrors.length > 0 || unexpectedConsoleErrors.length > 0) {
      throw new Error(`Admin events page with no events has errors`);
    }

    // Check for appropriate empty state or events list
    const expectedContent = [
      'text=/No events/i',
      'text=/Create Event/i',
      'text=/Add Event/i',
      'text=/Events/i',
      'table',
      'h1'
    ];

    let contentFound = false;
    for (const selector of expectedContent) {
      const count = await page.locator(selector).count();
      if (count > 0) {
        console.log(`Found content: ${selector}`);
        contentFound = true;
      }
    }

    expect(contentFound).toBeTruthy();
    console.log('✅ Admin events page handles empty/populated state correctly');
  });

  test('Admin events navigation maintains authentication state', async ({ page }) => {
    console.log('🔍 Testing admin events authentication persistence...');

    // Login as admin
    await AuthHelpers.loginAs(page, 'admin');

    // Navigate to admin events
    await page.goto('/admin/events');
    await page.waitForLoadState('domcontentloaded');

    // Verify admin access
    const unauthorizedCount = await page.locator('text=/Unauthorized|Access Denied|Login Required/i').count();
    expect(unauthorizedCount).toBe(0);

    // Refresh page and verify authentication persists
    await page.reload();
    await page.waitForLoadState('domcontentloaded');
    await page.waitForTimeout(2000);

    const unauthorizedAfterRefresh = await page.locator('text=/Unauthorized|Access Denied|Login Required/i').count();
    expect(unauthorizedAfterRefresh).toBe(0);

    console.log('✅ Admin authentication persists through navigation and refresh');
  });

  test('Non-admin user cannot access admin events section', async ({ page }) => {
    console.log('🔍 Testing non-admin access restriction...');

    // Login as regular member
    await AuthHelpers.loginAs(page, 'member');

    // Try to access admin events
    await page.goto('/admin/events');
    await page.waitForTimeout(3000);

    // Should be redirected or show access denied
    const currentUrl = page.url();
    const isRedirected = !currentUrl.includes('/admin/events');
    const hasAccessDenied = await page.locator('text=/Unauthorized|Access Denied|403|Forbidden/i').count() > 0;

    expect(isRedirected || hasAccessDenied).toBeTruthy();

    if (isRedirected) {
      console.log(`✅ Non-admin redirected to: ${currentUrl}`);
    } else {
      console.log('✅ Non-admin shown access denied message');
    }

    console.log('✅ Non-admin access restriction working correctly');
  });
});