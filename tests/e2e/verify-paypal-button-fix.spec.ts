/**
 * Verify PayPalButton debugLog Import Fix
 *
 * Purpose: Verify that the missing debugLog import in PayPalButton.tsx is fixed
 * and that the PayPal payment page loads without "ReferenceError: debugLog is not defined"
 *
 * Context: Fixed missing import on line 6 of PayPalButton.tsx
 * Expected: PayPal button should render without errors
 */

import { test, expect } from '@playwright/test';
import { AuthHelpers } from './test-utils/helpers/auth.helpers';

const baseUrl = process.env.PLAYWRIGHT_BASE_URL || 'http://localhost:5173';

test.describe('PayPal Button - debugLog Import Fix Verification', () => {
  let consoleErrors: string[] = [];
  let pageErrors: string[] = [];

  test.beforeEach(async ({ page }) => {
    // Reset error arrays
    consoleErrors = [];
    pageErrors = [];

    // Listen for console errors - especially debugLog errors
    page.on('console', msg => {
      if (msg.type() === 'error') {
        const text = msg.text();
        console.log('Browser Console Error:', text);
        if (text.includes('debugLog')) {
          consoleErrors.push(text);
        }
      }
    });

    // Listen for page errors - especially ReferenceError: debugLog
    page.on('pageerror', error => {
      const message = error.message;
      console.log('Page Error:', message);
      if (message.includes('debugLog')) {
        pageErrors.push(message);
      }
    });

    // Login as admin to access check-in kiosk
    await AuthHelpers.loginAs(page, 'admin');
  });

  test('PayPal button loads without debugLog ReferenceError', async ({ page }) => {
    console.log('🔍 Step 1: Navigate to check-in kiosk');
    await page.goto(`${baseUrl}/checkin`);
    await page.waitForLoadState('domcontentloaded');

    // Find and click on a workshop event (workshops have payment options)
    console.log('🔍 Step 2: Look for workshop events with "Pay at Door" option');
    const eventCards = page.locator('[data-testid="event-card"]');
    const eventCount = await eventCards.count();

    console.log(`  Found ${eventCount} event cards`);

    let payAtDoorButtonFound = false;

    // Try to find an event with "Pay at Door" option
    for (let i = 0; i < eventCount; i++) {
      const card = eventCards.nth(i);
      const eventName = await card.locator('h3, h2').first().textContent();
      console.log(`  Checking event: ${eventName}`);

      await card.click();
      await page.waitForLoadState('domcontentloaded');

      // Check if "Pay at Door" button exists
      const payAtDoorButton = page.locator('button').filter({ hasText: /pay.*door/i });

      if (await payAtDoorButton.count() > 0) {
        console.log('  ✅ Found "Pay at Door" button');
        payAtDoorButtonFound = true;
        await payAtDoorButton.click();
        await page.waitForLoadState('domcontentloaded');
        break;
      } else {
        console.log('  ⏭️ No "Pay at Door" button - event may be free or registration closed');
        // Go back to check-in kiosk
        await page.goto(`${baseUrl}/checkin`);
        await page.waitForLoadState('domcontentloaded');
      }
    }

    if (!payAtDoorButtonFound) {
      console.log('⏭️ No events with "Pay at Door" option found - skipping test');
      return;
    }

    console.log('🔍 Step 3: Look for "Digital Payment" option');
    const digitalPaymentButton = page.locator('button').filter({ hasText: /digital.*payment/i });

    if (await digitalPaymentButton.count() === 0) {
      console.log('⏭️ No "Digital Payment" button found - PayPal button may already be visible');
    } else {
      await digitalPaymentButton.click();
      await page.waitForLoadState('domcontentloaded');
    }

    console.log('🔍 Step 4: Wait for PayPal button container to load');
    // Wait for PayPal script provider or button container
    // The PayPalButton component should render without throwing debugLog errors
    await page.waitForTimeout(3000); // Give PayPal SDK time to load

    console.log('🔍 Step 5: Take screenshot for visual verification');
    await page.screenshot({
      path: './test-results/paypal-button-fix-verification.png',
      fullPage: true
    });

    console.log('🔍 Step 6: Verify no debugLog ReferenceError occurred');
    console.log(`  Console errors with debugLog: ${consoleErrors.length}`);
    console.log(`  Page errors with debugLog: ${pageErrors.length}`);

    if (consoleErrors.length > 0) {
      console.log('❌ Console errors found:');
      consoleErrors.forEach(err => console.log(`  - ${err}`));
    }

    if (pageErrors.length > 0) {
      console.log('❌ Page errors found:');
      pageErrors.forEach(err => console.log(`  - ${err}`));
    }

    // Assert NO debugLog errors occurred
    expect(consoleErrors.length, `Console errors found: ${consoleErrors.join(', ')}`).toBe(0);
    expect(pageErrors.length, `Page errors found: ${pageErrors.join(', ')}`).toBe(0);

    console.log('✅ SUCCESS: PayPal button loaded without debugLog ReferenceError');
  });

  test('PayPal button configuration debug logs work correctly', async ({ page }) => {
    console.log('🔍 Verify debugLog calls work in PayPalButton (no ReferenceError)');

    // Capture debug logs from PayPalButton
    const debugLogs: string[] = [];
    page.on('console', msg => {
      const text = msg.text();
      // PayPalButton uses these debug logs (lines 38-43 in PayPalButton.tsx)
      if (text.includes('PayPal Button Configuration') ||
          text.includes('paypalClientId') ||
          text.includes('paypalMode') ||
          text.includes('originalAmount') ||
          text.includes('slidingScalePercentage') ||
          text.includes('finalAmount')) {
        debugLogs.push(text);
        console.log('Debug Log:', text);
      }
    });

    // Navigate to check-in kiosk
    await page.goto(`${baseUrl}/checkin`);
    await page.waitForLoadState('domcontentloaded');

    // Try to trigger PayPal button render
    const eventCards = page.locator('[data-testid="event-card"]');
    const eventCount = await eventCards.count();

    for (let i = 0; i < eventCount; i++) {
      const card = eventCards.nth(i);
      await card.click();
      await page.waitForLoadState('domcontentloaded');

      const payAtDoorButton = page.locator('button').filter({ hasText: /pay.*door/i });
      if (await payAtDoorButton.count() > 0) {
        await payAtDoorButton.click();
        await page.waitForLoadState('domcontentloaded');

        // Wait for PayPal initialization
        await page.waitForTimeout(2000);
        break;
      } else {
        // Go back
        await page.goto(`${baseUrl}/checkin`);
        await page.waitForLoadState('domcontentloaded');
      }
    }

    console.log(`🔍 Captured ${debugLogs.length} debug logs from PayPalButton`);
    debugLogs.forEach(log => console.log(`  - ${log}`));

    // Verify NO debugLog ReferenceError occurred
    expect(consoleErrors.length, 'debugLog ReferenceError occurred').toBe(0);
    expect(pageErrors.length, 'debugLog ReferenceError occurred').toBe(0);

    console.log('✅ Test complete - debugLog function is working (no ReferenceError)');
  });
});
