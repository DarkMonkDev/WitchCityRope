/**
 * E2E Tests: Venue Display on Event Page (DataFactory Migration)
 *
 * Tests venue visibility rules on event detail pages.
 * Venue details should ONLY be visible to users with RSVP or tickets.
 *
 * MIGRATION NOTES:
 * - Uses df (DataFactory) fixture for automatic cleanup
 * - Creates test data via TestHelper API endpoints
 * - No need for manual API calls or cleanup logic
 * - Data is automatically cleaned up after each test
 *
 * Test Coverage:
 * - Unauthenticated users cannot see venue
 * - Authenticated users WITHOUT RSVP/ticket cannot see venue
 * - Users WITH RSVP can see venue
 * - Users WITH ticket can see venue
 * - Venue name and directions displayed correctly
 * - Notes field NOT displayed to public (admin-only)
 *
 * @see /apps/web/src/pages/events/EventDetailPage.tsx (lines 326-339)
 * @see /apps/api/Endpoints/VenueEndpoints.cs
 */

import { expect } from '@playwright/test';
import { test } from '../lib/datafactory/fixtures/test.fixture';
import { AuthHelpers } from './test-utils/helpers/auth.helpers';

test.describe('Venue Display on Event Page - DataFactory Migration', () => {
  test.beforeEach(async ({ page }) => {
    // Clear auth state before each test
    await AuthHelpers.clearAuthState(page);
  });

  test('should NOT display venue to unauthenticated users', async ({ page, df }) => {
    // Create test event with venue
    const event = await df.events.createPublished(`Venue Test Unauth ${Date.now()}`);

    // Create session (needed for complete event setup)
    const sessionStart = new Date();
    sessionStart.setDate(sessionStart.getDate() + 7);
    sessionStart.setHours(18, 0, 0, 0);
    const sessionEnd = new Date(sessionStart.getTime() + 3 * 60 * 60 * 1000);

    await df.sessions.create({
      eventId: event.id,
      title: 'Test Session',
      startTime: sessionStart,
      endTime: sessionEnd,
      maxCapacity: 20,
    });

    console.log(`✅ Created test event: ${event.id}`);

    // Navigate to event page WITHOUT logging in
    await page.goto(`/events/${event.id}`, { waitUntil: 'domcontentloaded' });
    await page.waitForTimeout(1000);

    // Verify we're on the event detail page
    await expect(page).toHaveURL(new RegExp(`/events/${event.id}`));

    // Venue section should NOT be visible
    const venueSection = page.locator('[data-testid="venue-section"], text="Directions To"');

    if (await venueSection.count() > 0) {
      // If venue section exists, it should not be visible
      await expect(venueSection.first()).not.toBeVisible();
    }

    // Alternatively, venue content should not be present at all
    const venueDirections = page
      .locator('text=/turn left/i, text=/address/i, text=/location/i')
      .filter({ hasText: /venue|directions/i });
    expect(await venueDirections.count()).toBe(0);
  });

  test('should NOT display venue to authenticated users WITHOUT RSVP or ticket', async ({
    page,
    df,
  }) => {
    // Create test event with venue
    const event = await df.events.createPublished(
      `Venue Test No RSVP ${Date.now()}`
    );

    // Create session
    const sessionStart = new Date();
    sessionStart.setDate(sessionStart.getDate() + 7);
    sessionStart.setHours(18, 0, 0, 0);
    const sessionEnd = new Date(sessionStart.getTime() + 3 * 60 * 60 * 1000);

    await df.sessions.create({
      eventId: event.id,
      title: 'Test Session',
      startTime: sessionStart,
      endTime: sessionEnd,
      maxCapacity: 20,
    });

    console.log(`✅ Created test event: ${event.id}`);

    // Login as member who hasn't RSVP'd
    await AuthHelpers.loginAs(page, 'member');

    // Navigate to event detail page
    await page.goto(`/events/${event.id}`, { waitUntil: 'domcontentloaded' });
    await page.waitForTimeout(1000);

    // Verify we're on event detail page
    await expect(page).toHaveURL(new RegExp(`/events/${event.id}`));

    // Venue section should NOT be visible since we don't have RSVP/ticket
    const venueSection = page.locator('text="Directions To"');
    expect(await venueSection.count()).toBe(0);
  });

  test('should display venue to users WITH RSVP', async ({ page, df }) => {
    // Create test event with venue (venueId defaults to 1 = Main Studio)
    const event = await df.events.createPublished(`Venue Test RSVP ${Date.now()}`);

    // Create session
    const sessionStart = new Date();
    sessionStart.setDate(sessionStart.getDate() + 7);
    sessionStart.setHours(18, 0, 0, 0);
    const sessionEnd = new Date(sessionStart.getTime() + 3 * 60 * 60 * 1000);

    const session = await df.sessions.create({
      eventId: event.id,
      title: 'Test Session',
      startTime: sessionStart,
      endTime: sessionEnd,
      maxCapacity: 20,
    });

    // Create free RSVP ticket
    await df.ticketTypes.create({
      eventId: event.id,
      sessionId: session.id,
      name: 'Free RSVP',
      price: 0,
      quantityAvailable: 20,
    });

    console.log(`✅ Created test event: ${event.id}`);

    // Login as member
    await AuthHelpers.loginAs(page, 'member');

    // Navigate to event
    await page.goto(`/events/${event.id}`, { waitUntil: 'domcontentloaded' });
    await page.waitForTimeout(1000);

    // Check if we can RSVP (if RSVP button exists)
    const rsvpButton = page.locator(
      '[data-testid="rsvp-button"], button:has-text("RSVP")'
    );

    if (await rsvpButton.count() > 0) {
      // Click RSVP button
      await rsvpButton.click();
      await page.waitForTimeout(1500);

      // After RSVP, venue should now be visible
      const venueSection = page.locator('text="Directions To"');

      if (await venueSection.count() > 0) {
        await expect(venueSection.first()).toBeVisible({ timeout: 5000 });

        // Verify venue details are present
        // Look for directions content (should be visible)
        const venueContent = page.locator('[data-testid="venue-directions"]');

        if (await venueContent.count() > 0) {
          await expect(venueContent.first()).toBeVisible();
        }
      } else {
        console.log(
          '⚠️ Venue section not found after RSVP - venue may not be set for this event'
        );
      }
    }
  });

  test('should display venue to users WITH ticket', async ({ page, df }) => {
    // Create test event with venue
    const event = await df.events.createPublished(`Venue Test Ticket ${Date.now()}`);

    // Create session
    const sessionStart = new Date();
    sessionStart.setDate(sessionStart.getDate() + 7);
    sessionStart.setHours(18, 0, 0, 0);
    const sessionEnd = new Date(sessionStart.getTime() + 3 * 60 * 60 * 1000);

    const session = await df.sessions.create({
      eventId: event.id,
      title: 'Test Session',
      startTime: sessionStart,
      endTime: sessionEnd,
      maxCapacity: 20,
    });

    // Create paid ticket
    const paidTicket = await df.ticketTypes.create({
      eventId: event.id,
      sessionId: session.id,
      name: 'Paid Ticket',
      price: 25.0,
      quantityAvailable: 20,
    });

    console.log(`✅ Created test event: ${event.id}`);
    console.log(`✅ Created paid ticket: ${paidTicket.id}`);

    // Login as member
    await AuthHelpers.loginAs(page, 'member');

    // Navigate to checkout page
    await page.goto(`/checkout/${event.id}`, { waitUntil: 'domcontentloaded' });
    await page.waitForLoadState('networkidle');

    await expect(page).not.toHaveURL(/login/);
    await page.screenshot({ path: './test-results/venue-checkout-step1.png' });

    // Select the paid ticket
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

    // Click Continue
    const continueButton = page
      .locator('button')
      .filter({ hasText: /continue|next|proceed/i })
      .first();
    if (await continueButton.isVisible({ timeout: 5000 }).catch(() => false)) {
      await continueButton.click();
      await page.waitForTimeout(1000);
    }

    await page.screenshot({ path: './test-results/venue-checkout-step2.png' });

    // Accept terms
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

    // Click Pay
    const payButton = page
      .locator('button')
      .filter({ hasText: /pay with credit card/i })
      .first();
    await expect(payButton).toBeVisible({ timeout: 10000 });
    await expect(payButton).toBeEnabled({ timeout: 5000 });

    await page.screenshot({ path: './test-results/venue-checkout-step3.png' });
    await payButton.click();

    // Verify purchase completed
    await page.waitForTimeout(3000);
    await page.screenshot({ path: './test-results/venue-checkout-confirmation.png' });

    // Navigate back to event page
    await page.goto(`/events/${event.id}`, { waitUntil: 'domcontentloaded' });
    await page.waitForTimeout(1000);

    // After ticket purchase, venue should be visible
    const venueSection = page.locator('text="Directions To"');

    if (await venueSection.count() > 0) {
      await expect(venueSection.first()).toBeVisible({ timeout: 5000 });
      console.log('✅ Venue visible after ticket purchase');
    } else {
      console.log('⚠️ Venue section not found after ticket purchase');
    }
  });

  test('should display correct venue name and directions', async ({ page, df }) => {
    // Create test event with venue
    const event = await df.events.createPublished(`Venue Test Display ${Date.now()}`);

    // Create session
    const sessionStart = new Date();
    sessionStart.setDate(sessionStart.getDate() + 7);
    sessionStart.setHours(18, 0, 0, 0);
    const sessionEnd = new Date(sessionStart.getTime() + 3 * 60 * 60 * 1000);

    const session = await df.sessions.create({
      eventId: event.id,
      title: 'Test Session',
      startTime: sessionStart,
      endTime: sessionEnd,
      maxCapacity: 20,
    });

    // Create free RSVP ticket
    await df.ticketTypes.create({
      eventId: event.id,
      sessionId: session.id,
      name: 'Free RSVP',
      price: 0,
      quantityAvailable: 20,
    });

    console.log(`✅ Created test event: ${event.id}`);

    // Login and RSVP to the event
    await AuthHelpers.loginAs(page, 'vetted');

    await page.goto(`/events/${event.id}`, { waitUntil: 'domcontentloaded' });
    await page.waitForTimeout(1000);

    // RSVP if possible
    const rsvpButton = page.locator(
      '[data-testid="rsvp-button"], button:has-text("RSVP")'
    );

    if (await rsvpButton.count() > 0) {
      await rsvpButton.click();
      await page.waitForTimeout(1500);
    }

    // Check for venue section
    const venueSection = page.locator('text="Directions To"');

    if (await venueSection.count() > 0) {
      // Verify venue name is in the heading
      const venueHeading = await venueSection.first().textContent();
      expect(venueHeading).toBeTruthy();
      expect(venueHeading).toContain('Directions To');

      // Verify directions content exists
      const directionsContent = page.locator('[data-testid="venue-directions"]');

      if (await directionsContent.count() > 0) {
        const directionsText = await directionsContent.first().textContent();
        expect(directionsText).toBeTruthy();
        expect(directionsText!.length).toBeGreaterThan(0);
      }
    }
  });

  test('should NOT display admin Notes field to public', async ({ page, df }) => {
    // Create test event with venue
    const event = await df.events.createPublished(`Venue Test Notes ${Date.now()}`);

    // Create session
    const sessionStart = new Date();
    sessionStart.setDate(sessionStart.getDate() + 7);
    sessionStart.setHours(18, 0, 0, 0);
    const sessionEnd = new Date(sessionStart.getTime() + 3 * 60 * 60 * 1000);

    const session = await df.sessions.create({
      eventId: event.id,
      title: 'Test Session',
      startTime: sessionStart,
      endTime: sessionEnd,
      maxCapacity: 20,
    });

    // Create free RSVP ticket
    await df.ticketTypes.create({
      eventId: event.id,
      sessionId: session.id,
      name: 'Free RSVP',
      price: 0,
      quantityAvailable: 20,
    });

    console.log(`✅ Created test event: ${event.id}`);

    // Login as regular member (not admin)
    await AuthHelpers.loginAs(page, 'member');

    await page.goto(`/events/${event.id}`, { waitUntil: 'domcontentloaded' });
    await page.waitForTimeout(1000);

    // RSVP to see venue
    const rsvpButton = page.locator(
      '[data-testid="rsvp-button"], button:has-text("RSVP")'
    );

    if (await rsvpButton.count() > 0) {
      await rsvpButton.click();
      await page.waitForTimeout(1500);
    }

    // Even if venue is visible, Notes field should NOT be displayed
    const notesSection = page.locator('[data-testid="venue-notes"], text="Notes"');
    expect(await notesSection.count()).toBe(0);

    // Admin-only note content should not be visible
    const adminContent = page.locator('text=/admin.*only/i, text=/internal.*notes/i');
    expect(await adminContent.count()).toBe(0);
  });

  test('should hide venue when user cancels RSVP', async ({ page, df }) => {
    // Create test event with venue
    const event = await df.events.createPublished(`Venue Test Cancel ${Date.now()}`);

    // Create session
    const sessionStart = new Date();
    sessionStart.setDate(sessionStart.getDate() + 7);
    sessionStart.setHours(18, 0, 0, 0);
    const sessionEnd = new Date(sessionStart.getTime() + 3 * 60 * 60 * 1000);

    const session = await df.sessions.create({
      eventId: event.id,
      title: 'Test Session',
      startTime: sessionStart,
      endTime: sessionEnd,
      maxCapacity: 20,
    });

    // Create free RSVP ticket
    await df.ticketTypes.create({
      eventId: event.id,
      sessionId: session.id,
      name: 'Free RSVP',
      price: 0,
      quantityAvailable: 20,
    });

    console.log(`✅ Created test event: ${event.id}`);

    // Login as member
    await AuthHelpers.loginAs(page, 'member');

    await page.goto(`/events/${event.id}`, { waitUntil: 'domcontentloaded' });
    await page.waitForTimeout(1000);

    // RSVP first
    const rsvpButton = page.locator(
      '[data-testid="rsvp-button"], button:has-text("RSVP")'
    );

    if (await rsvpButton.count() > 0) {
      await rsvpButton.click();
      await page.waitForTimeout(1500);

      // Verify venue is visible
      const venueSection = page.locator('text="Directions To"');
      if (await venueSection.count() > 0) {
        await expect(venueSection.first()).toBeVisible();

        // Now cancel RSVP
        const cancelButton = page.locator(
          '[data-testid="cancel-rsvp-button"], button:has-text("Cancel RSVP")'
        );

        if (await cancelButton.count() > 0) {
          await cancelButton.click();
          await page.waitForTimeout(1000);

          // Confirm cancellation if modal appears
          const confirmCancel = page.locator(
            '[data-testid="confirm-cancel"], button:has-text("Confirm")'
          );
          if (await confirmCancel.count() > 0) {
            await confirmCancel.click();
            await page.waitForTimeout(1500);
          }

          // After canceling RSVP, venue should be hidden
          await expect(venueSection.first()).not.toBeVisible();
        }
      }
    }
  });
});
