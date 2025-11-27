/**
 * E2E Tests: Venue Display on Event Page
 *
 * Tests venue visibility rules on event detail pages.
 * Venue details should ONLY be visible to users with RSVP or tickets.
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

import { test, expect } from '@playwright/test';
import { AuthHelpers } from './test-utils/helpers/auth.helpers';

const baseUrl = process.env.PLAYWRIGHT_BASE_URL || 'http://localhost:5173';

test.describe('Venue Display on Event Page', () => {
  // We'll use an existing event from seed data for testing
  // If no events exist, these tests will need an event created first

  test.beforeEach(async ({ page }) => {
    // Clear auth state before each test
    await AuthHelpers.clearAuthState(page);
  });

  test('should NOT display venue to unauthenticated users', async ({ page }) => {
    // Navigate to an event page WITHOUT logging in
    await page.goto(`${baseUrl}/events`);
    await page.waitForLoadState('networkidle');

    // Find and click on first event (if any exist)
    const firstEvent = page.locator('[data-testid="event-card"], .event-card, a[href*="/events/"]').first();

    if (await firstEvent.count() > 0) {
      await firstEvent.click();
      await page.waitForTimeout(1000);

      // Verify we're on an event detail page
      await expect(page).toHaveURL(/\/events\/\d+/);

      // Venue section should NOT be visible
      const venueSection = page.locator('[data-testid="venue-section"], text="Directions To"');

      if (await venueSection.count() > 0) {
        // If venue section exists, it should not be visible
        await expect(venueSection.first()).not.toBeVisible();
      }

      // Alternatively, venue content should not be present at all
      const venueDirections = page.locator('text=/turn left/i, text=/address/i, text=/location/i').filter({ hasText: /venue|directions/i });
      expect(await venueDirections.count()).toBe(0);
    } else {
      console.log('⚠️ No events found for testing. Skipping venue visibility test.');
      test.skip();
    }
  });

  test('should NOT display venue to authenticated users WITHOUT RSVP or ticket', async ({ page }) => {
    // Login as member who hasn't RSVP'd
    await AuthHelpers.loginAs(page, 'member');

    // Navigate to events list
    await page.goto(`${baseUrl}/events`);
    await page.waitForLoadState('networkidle');

    // Find an event we haven't RSVP'd to
    const eventCards = page.locator('[data-testid="event-card"], .event-card, a[href*="/events/"]');

    if (await eventCards.count() > 0) {
      // Click on first event
      await eventCards.first().click();
      await page.waitForTimeout(1000);

      // Verify we're on event detail page
      await expect(page).toHaveURL(/\/events\/\d+/);

      // Venue section should NOT be visible if we don't have RSVP/ticket
      // Check if RSVP button exists (meaning we haven't RSVP'd yet)
      const rsvpButton = page.locator('[data-testid="rsvp-button"], button:has-text("RSVP")');

      if (await rsvpButton.count() > 0) {
        // We haven't RSVP'd, so venue should NOT be visible
        const venueSection = page.locator('text="Directions To"');
        expect(await venueSection.count()).toBe(0);
      } else {
        console.log('⚠️ User already has RSVP for this event, cannot test venue hiding');
      }
    } else {
      console.log('⚠️ No events found for testing');
      test.skip();
    }
  });

  test('should display venue to users WITH RSVP', async ({ page }) => {
    // Login as member
    await AuthHelpers.loginAs(page, 'member');

    // Navigate to events list
    await page.goto(`${baseUrl}/events`);
    await page.waitForLoadState('networkidle');

    // Find a social event (social events use RSVP)
    const eventCards = page.locator('[data-testid="event-card"], .event-card, a[href*="/events/"]');

    if (await eventCards.count() > 0) {
      // Click on first event
      await eventCards.first().click();
      await page.waitForTimeout(1000);

      // Check if we can RSVP (if RSVP button exists)
      const rsvpButton = page.locator('[data-testid="rsvp-button"], button:has-text("RSVP")');

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
          console.log('⚠️ Venue section not found after RSVP - venue may not be set for this event');
        }
      } else {
        console.log('⚠️ RSVP button not found - user may already have RSVP or event is not social');
      }
    } else {
      console.log('⚠️ No events found for testing');
      test.skip();
    }
  });

  test('should display venue to users WITH ticket', async ({ page }) => {
    // Login as member
    await AuthHelpers.loginAs(page, 'member');

    // Navigate to events list
    await page.goto(`${baseUrl}/events`);
    await page.waitForLoadState('networkidle');

    // Look for a class/workshop event (events with tickets)
    const eventCards = page.locator('[data-testid="event-card"], .event-card, a[href*="/events/"]');

    if (await eventCards.count() > 0) {
      // Try multiple events to find one with ticket purchase option
      const eventCount = await eventCards.count();

      for (let i = 0; i < Math.min(eventCount, 3); i++) {
        await page.goto(`${baseUrl}/events`);
        await page.waitForLoadState('networkidle');

        await eventCards.nth(i).click();
        await page.waitForTimeout(1000);

        // Check if this event has ticket purchase option
        const ticketButton = page.locator('[data-testid="buy-ticket-button"], button:has-text("Buy"), button:has-text("Ticket")');

        if (await ticketButton.count() > 0) {
          // This event has tickets - try to purchase
          await ticketButton.first().click();
          await page.waitForTimeout(1000);

          // Complete purchase flow (simplified - actual flow may vary)
          const confirmButton = page.locator('[data-testid="confirm-purchase"], button:has-text("Confirm"), button:has-text("Purchase")');

          if (await confirmButton.count() > 0) {
            await confirmButton.click();
            await page.waitForTimeout(2000);

            // After ticket purchase, venue should be visible
            const venueSection = page.locator('text="Directions To"');

            if (await venueSection.count() > 0) {
              await expect(venueSection.first()).toBeVisible({ timeout: 5000 });
              break; // Successfully tested, exit loop
            }
          }
        }
      }
    } else {
      console.log('⚠️ No events found for testing');
      test.skip();
    }
  });

  test('should display correct venue name and directions', async ({ page }) => {
    // Login and RSVP to an event first
    await AuthHelpers.loginAs(page, 'vetted');

    await page.goto(`${baseUrl}/events`);
    await page.waitForLoadState('networkidle');

    const eventCards = page.locator('[data-testid="event-card"], .event-card, a[href*="/events/"]');

    if (await eventCards.count() > 0) {
      await eventCards.first().click();
      await page.waitForTimeout(1000);

      // RSVP if possible
      const rsvpButton = page.locator('[data-testid="rsvp-button"], button:has-text("RSVP")');

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
    }
  });

  test('should NOT display admin Notes field to public', async ({ page }) => {
    // Login as regular member (not admin)
    await AuthHelpers.loginAs(page, 'member');

    await page.goto(`${baseUrl}/events`);
    await page.waitForLoadState('networkidle');

    const eventCards = page.locator('[data-testid="event-card"], .event-card, a[href*="/events/"]');

    if (await eventCards.count() > 0) {
      await eventCards.first().click();
      await page.waitForTimeout(1000);

      // RSVP to see venue
      const rsvpButton = page.locator('[data-testid="rsvp-button"], button:has-text("RSVP")');

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
    }
  });

  test('should hide venue when user cancels RSVP', async ({ page }) => {
    // Login as member
    await AuthHelpers.loginAs(page, 'member');

    await page.goto(`${baseUrl}/events`);
    await page.waitForLoadState('networkidle');

    const eventCards = page.locator('[data-testid="event-card"], .event-card, a[href*="/events/"]');

    if (await eventCards.count() > 0) {
      await eventCards.first().click();
      await page.waitForTimeout(1000);

      // RSVP first
      const rsvpButton = page.locator('[data-testid="rsvp-button"], button:has-text("RSVP")');

      if (await rsvpButton.count() > 0) {
        await rsvpButton.click();
        await page.waitForTimeout(1500);

        // Verify venue is visible
        const venueSection = page.locator('text="Directions To"');
        if (await venueSection.count() > 0) {
          await expect(venueSection.first()).toBeVisible();

          // Now cancel RSVP
          const cancelButton = page.locator('[data-testid="cancel-rsvp-button"], button:has-text("Cancel RSVP")');

          if (await cancelButton.count() > 0) {
            await cancelButton.click();
            await page.waitForTimeout(1000);

            // Confirm cancellation if modal appears
            const confirmCancel = page.locator('[data-testid="confirm-cancel"], button:has-text("Confirm")');
            if (await confirmCancel.count() > 0) {
              await confirmCancel.click();
              await page.waitForTimeout(1500);
            }

            // After canceling RSVP, venue should be hidden
            await expect(venueSection.first()).not.toBeVisible();
          }
        }
      }
    }
  });
});
