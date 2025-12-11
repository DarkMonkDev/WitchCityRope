/**
 * Ticket Purchase E2E Tests (DataFactory Migration)
 *
 * This is a migrated version of ticket-purchase-e2e.spec.ts that uses the
 * DataFactory pattern for test data creation and automatic cleanup.
 *
 * MIGRATION NOTES:
 * - Uses df (DataFactory) fixture for automatic cleanup
 * - Creates test data via TestHelper API endpoints
 * - No need for manual API calls or cleanup logic
 * - Data is automatically cleaned up after each test
 *
 * Original: tests/e2e/ticket-purchase-e2e.spec.ts
 */

import { expect } from '@playwright/test';
import { test } from '../lib/datafactory/fixtures/test.fixture';
import { AuthHelpers } from './test-utils/helpers/auth.helpers';

test.describe('Ticket Purchase - DataFactory Migration', () => {
  // Run checkout tests serially to avoid conflicts
  test.describe.configure({ mode: 'serial' });

  test('Complete ticket purchase with credit card', async ({ page, df }) => {
    // Step 1: Create test data using DataFactory
    // This replaces 60+ lines of manual event creation in beforeAll
    const event = await df.events.createPublished(`Ticket Purchase Test ${Date.now()}`);

    // Calculate session times (3 hours from event start)
    const sessionStart = new Date();
    sessionStart.setDate(sessionStart.getDate() + 1); // Tomorrow
    sessionStart.setHours(18, 0, 0, 0);
    const sessionEnd = new Date(sessionStart.getTime() + 3 * 60 * 60 * 1000);

    const session = await df.sessions.create({
      eventId: event.id,
      title: 'Main Session',
      startTime: sessionStart,
      endTime: sessionEnd,
      maxCapacity: 20,
    });

    const paidTicket = await df.ticketTypes.create({
      sessionId: session.id,
      name: 'Paid Ticket',
      price: 25.0,
      quantityAvailable: 20,
    });

    console.log(`✅ Created test event: ${event.id}`);
    console.log(`✅ Created session: ${session.id}`);
    console.log(`✅ Created paid ticket: ${paidTicket.id}`);

    // Step 2: Login as member
    await AuthHelpers.loginAs(page, 'member');

    // Step 3: Navigate to checkout page
    await page.goto(`/checkout/${event.id}`, { waitUntil: 'domcontentloaded' });
    await page.waitForLoadState('networkidle');

    // Verify we're on the checkout page
    await expect(page).not.toHaveURL(/login/);
    await page.screenshot({ path: './test-results/df-checkout-step1.png' });

    // Step 4: Select the paid ticket
    const ticketCheckbox = page
      .locator(`input[type="checkbox"][value="${paidTicket.id}"]`)
      .last();
    if (await ticketCheckbox.isVisible({ timeout: 5000 }).catch(() => false)) {
      const isChecked = await ticketCheckbox.isChecked();
      if (!isChecked) {
        await ticketCheckbox.check();
      }
    } else {
      const ticketLabel = page.locator('text=Paid Ticket').last();
      if (await ticketLabel.isVisible({ timeout: 3000 }).catch(() => false)) {
        await ticketLabel.click();
      }
    }

    // Step 5: Click Continue
    const continueButton = page
      .locator('button')
      .filter({ hasText: /continue|next|proceed/i })
      .first();
    if (await continueButton.isVisible({ timeout: 5000 }).catch(() => false)) {
      await continueButton.click();
      await page.waitForTimeout(1000);
    }

    await page.screenshot({ path: './test-results/df-checkout-step2.png' });

    // Step 6: Accept terms
    const termsCheckbox = page
      .locator('#terms-checkbox, #terms-checkbox-mobile, [id*="terms"]')
      .first();
    if (await termsCheckbox.isVisible({ timeout: 3000 }).catch(() => false)) {
      await termsCheckbox.check();
    } else {
      const termsLabel = page
        .locator('label')
        .filter({ hasText: /agree|waiver/i })
        .first();
      if (await termsLabel.isVisible({ timeout: 2000 }).catch(() => false)) {
        await termsLabel.click();
      }
    }

    // Step 7: Click Pay
    const payButton = page
      .locator('button')
      .filter({ hasText: /pay with credit card/i })
      .first();
    await expect(payButton).toBeVisible({ timeout: 10000 });
    await expect(payButton).toBeEnabled({ timeout: 5000 });

    await page.screenshot({ path: './test-results/df-checkout-step3.png' });
    await payButton.click();

    // Step 8: Verify confirmation
    await page.waitForTimeout(3000);
    await page.screenshot({ path: './test-results/df-checkout-step4.png' });

    const confirmationIndicators = [
      page.locator('text=/confirmation|success|thank you|complete/i').first(),
      page.locator('[class*="Confirmation"]').first(),
      page.locator('text=/your ticket|registration confirmed/i').first(),
    ];

    let confirmationFound = false;
    for (const indicator of confirmationIndicators) {
      if (await indicator.isVisible({ timeout: 5000 }).catch(() => false)) {
        confirmationFound = true;
        break;
      }
    }

    await page.screenshot({ path: './test-results/df-checkout-confirmation.png' });
    expect(confirmationFound).toBeTruthy();

    // NOTE: df.cleanupAll() is called automatically after test via fixture
  });

  test('Free RSVP ticket purchase completes successfully', async ({ page, df }) => {
    // Create test data - much simpler than original 60+ lines
    const event = await df.events.createPublished(`Free RSVP Test ${Date.now()}`);

    // Calculate session times
    const sessionStart = new Date();
    sessionStart.setDate(sessionStart.getDate() + 1);
    sessionStart.setHours(18, 0, 0, 0);
    const sessionEnd = new Date(sessionStart.getTime() + 3 * 60 * 60 * 1000);

    const session = await df.sessions.create({
      eventId: event.id,
      title: 'Free Session',
      startTime: sessionStart,
      endTime: sessionEnd,
      maxCapacity: 20,
    });

    const freeTicket = await df.ticketTypes.create({
      sessionId: session.id,
      name: 'Free RSVP',
      price: 0,
      quantityAvailable: 20,
    });

    console.log(`✅ Created free event: ${event.id}`);
    console.log(`✅ Free ticket ID: ${freeTicket.id}`);

    // Login and navigate
    await AuthHelpers.loginAs(page, 'member');
    await page.goto(`/checkout/${event.id}?ticketTypeId=${freeTicket.id}`, {
      waitUntil: 'domcontentloaded',
    });
    await page.waitForLoadState('networkidle');

    await expect(page).not.toHaveURL(/login/);

    // Verify free ticket shown
    const freeIndicator = page.locator('text=/\\$0\\.00/').first();
    await expect(freeIndicator).toBeVisible({ timeout: 10000 });

    await page.screenshot({ path: './test-results/df-free-step1.png' });

    // Continue to payment
    const continueButton = page
      .locator('button')
      .filter({ hasText: /continue to payment/i })
      .first();
    await expect(continueButton).toBeVisible({ timeout: 5000 });
    await continueButton.click();

    await page.waitForTimeout(1000);
    await page.screenshot({ path: './test-results/df-free-step2.png' });

    // Accept terms
    const termsCheckbox = page.locator('#terms-checkbox, #terms-checkbox-mobile').first();
    await expect(termsCheckbox).toBeVisible({ timeout: 5000 });
    await termsCheckbox.check();

    // Complete purchase
    const payButton = page
      .locator('button')
      .filter({ hasText: /pay with credit card/i })
      .first();
    await expect(payButton).toBeVisible({ timeout: 5000 });
    await expect(payButton).toBeEnabled();

    await page.screenshot({ path: './test-results/df-free-step3.png' });
    await payButton.click();

    await page.waitForTimeout(4000);
    await page.screenshot({ path: './test-results/df-free-step4.png' });

    // Verify confirmation
    const confirmationContent = page
      .locator('text=/view my registrations|thank you for|registration confirmed|your ticket/i')
      .first();
    const viewDashboardButton = page
      .locator('button')
      .filter({ hasText: /view.*registration|go to dashboard|done/i })
      .first();

    let confirmationFound = false;
    if (await confirmationContent.isVisible({ timeout: 10000 }).catch(() => false)) {
      confirmationFound = true;
    }
    if (
      !confirmationFound &&
      (await viewDashboardButton.isVisible({ timeout: 3000 }).catch(() => false))
    ) {
      confirmationFound = true;
    }

    await page.screenshot({ path: './test-results/df-free-confirmation.png' });
    expect(confirmationFound).toBeTruthy();
    console.log('FREE RSVP purchase completed successfully!');
  });

  test('Checkout requires authentication', async ({ page, context, df }) => {
    // Create minimal test data
    const event = await df.events.createDefault(`Auth Test Event ${Date.now()}`);

    // Clear authentication
    await context.clearCookies();

    // Try to access checkout without auth
    await page.goto(`/checkout/${event.id}`, { waitUntil: 'domcontentloaded' });
    await page.waitForLoadState('networkidle');

    // Should redirect to login
    await expect(page).toHaveURL(/login/);

    const loginForm = page
      .locator('[data-testid="login-button"], button:has-text("Sign In")')
      .first();
    await expect(loginForm).toBeVisible({ timeout: 5000 });

    console.log('Correctly redirected to login when not authenticated');
  });
});
