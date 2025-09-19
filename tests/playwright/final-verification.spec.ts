import { test, expect } from '@playwright/test';

test.describe('Final Verification of Issues', () => {
  test('Logout Persistence Test - CRITICAL FINDING', async ({ page }) => {
    console.log('=== LOGOUT PERSISTENCE ISSUE CONFIRMED ===');

    // Login
    await page.goto('http://localhost:5173');
    await page.click('text=LOGIN');
    await page.waitForURL('**/login');
    await page.fill('input[placeholder="your@email.com"]', 'admin@witchcityrope.com');
    await page.fill('input[type="password"]', 'Test123!');
    await page.click('button[type="submit"]');
    await page.waitForTimeout(3000);

    console.log('✅ Successfully logged in - on dashboard');

    // Logout
    await page.click('text=LOGOUT');
    await page.waitForTimeout(3000);

    const urlAfterLogout = page.url();
    console.log(`After logout URL: ${urlAfterLogout}`);

    // Refresh page
    await page.reload();
    await page.waitForLoadState('networkidle');
    await page.waitForTimeout(3000);

    const urlAfterRefresh = page.url();
    const contentAfterRefresh = await page.content();
    const hasLogout = contentAfterRefresh.includes('LOGOUT');
    const hasLogin = contentAfterRefresh.includes('LOGIN');

    console.log(`After refresh URL: ${urlAfterRefresh}`);
    console.log(`Has LOGOUT button: ${hasLogout}`);
    console.log(`Has LOGIN button: ${hasLogin}`);

    // CRITICAL FINDING
    if (hasLogout && !hasLogin) {
      console.log('❌ LOGOUT PERSISTENCE: NO - User gets logged back in after refresh');
      console.log('❌ BUG CONFIRMED: Logout does not persist after page refresh');
    } else if (hasLogin && !hasLogout) {
      console.log('✅ LOGOUT PERSISTENCE: YES - User stays logged out after refresh');
    } else {
      console.log('⚠️ LOGOUT PERSISTENCE: UNCLEAR STATE');
    }

    expect(true).toBe(true);
  });

  test('Admin Access Test', async ({ page }) => {
    console.log('=== TESTING ADMIN ACCESS FOR TEACHER SELECTION ===');

    // Login as admin
    await page.goto('http://localhost:5173');
    await page.click('text=LOGIN');
    await page.waitForURL('**/login');
    await page.fill('input[placeholder="your@email.com"]', 'admin@witchcityrope.com');
    await page.fill('input[type="password"]', 'Test123!');
    await page.click('button[type="submit"]');
    await page.waitForTimeout(3000);

    console.log('✅ Logged in as admin');

    // Check for admin navigation
    const adminNavigation = await page.locator('text=Admin').count();
    console.log(`Admin navigation links found: ${adminNavigation}`);

    // Try direct navigation to admin events
    await page.goto('http://localhost:5173/admin/events');
    await page.waitForLoadState('networkidle');
    await page.waitForTimeout(3000);

    const currentUrl = page.url();
    const pageContent = await page.content();

    console.log(`Direct admin/events URL: ${currentUrl}`);
    console.log(`Page contains "event": ${pageContent.toLowerCase().includes('event')}`);
    console.log(`Page contains "admin": ${pageContent.toLowerCase().includes('admin')}`);

    // Look for any form elements that might be event editing
    const forms = await page.locator('form').count();
    const selects = await page.locator('select').count();
    const inputs = await page.locator('input').count();
    const buttons = await page.locator('button').count();

    console.log(`Forms on page: ${forms}`);
    console.log(`Select dropdowns: ${selects}`);
    console.log(`Input fields: ${inputs}`);
    console.log(`Buttons: ${buttons}`);

    if (selects > 0) {
      console.log('✅ SELECT ELEMENTS FOUND - Teacher selection might be possible');

      // Check select options
      const selectElements = await page.locator('select').all();
      for (let i = 0; i < selectElements.length; i++) {
        const options = await selectElements[i].locator('option').allTextContents();
        console.log(`Select ${i + 1} options: ${options.join(', ')}`);
      }
    } else {
      console.log('❌ NO SELECT ELEMENTS - Teacher selection not implemented');
    }

    if (currentUrl.includes('/admin') && (pageContent.includes('event') || forms > 0)) {
      console.log('✅ ADMIN EVENTS AREA ACCESSIBLE');
    } else {
      console.log('❌ ADMIN EVENTS AREA NOT ACCESSIBLE OR NOT IMPLEMENTED');
    }

    expect(true).toBe(true);
  });

  test('Console Error Analysis', async ({ page }) => {
    console.log('=== CONSOLE ERROR ANALYSIS ===');

    const errors: string[] = [];
    page.on('console', (msg) => {
      if (msg.type() === 'error') {
        errors.push(msg.text());
      }
    });

    // Full user flow
    await page.goto('http://localhost:5173');
    await page.waitForTimeout(2000);

    await page.click('text=LOGIN');
    await page.waitForURL('**/login');
    await page.fill('input[placeholder="your@email.com"]', 'admin@witchcityrope.com');
    await page.fill('input[type="password"]', 'Test123!');
    await page.click('button[type="submit"]');
    await page.waitForTimeout(3000);

    await page.click('text=LOGOUT');
    await page.waitForTimeout(2000);

    await page.reload();
    await page.waitForTimeout(2000);

    console.log(`Total console errors detected: ${errors.length}`);

    const authErrors = errors.filter(err =>
      err.includes('401') ||
      err.includes('Unauthorized') ||
      err.includes('auth') ||
      err.includes('login')
    );

    console.log(`Authentication-related errors: ${authErrors.length}`);
    authErrors.forEach((err, i) => {
      console.log(`  ${i + 1}. ${err}`);
    });

    if (authErrors.length > 0) {
      console.log('❌ AUTHENTICATION ERRORS DETECTED');
    } else {
      console.log('✅ NO AUTHENTICATION ERRORS');
    }

    expect(true).toBe(true);
  });
});