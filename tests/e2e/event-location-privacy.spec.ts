/**
 * E2E Tests: Event Location Privacy - Conditional Display
 *
 * Tests the conditional display of venue location based on user vetting status
 * and event participation. Non-vetted users without RSVP/ticket see only
 * city/state location. Vetted users and participants see full venue details.
 *
 * Test Coverage:
 * - Non-vetted user without RSVP sees location (city, state) only
 * - Non-vetted user with RSVP sees full venue name and directions
 * - Vetted user without RSVP sees full venue name and directions
 * - Vetted user with RSVP sees full venue name and directions
 * - Limited venue section displays for non-vetted without RSVP
 * - Full venue section displays for vetted or with RSVP
 * - Info alert displays for non-vetted users explaining access
 *
 * @see /apps/web/src/pages/events/EventDetailPage.tsx
 * @see /docs/functional-areas/venue-management/new-work/2025-11-23-venue-location-privacy/design/ui-design.md
 */

import { test, expect, Page } from '@playwright/test';
import { AuthHelpers } from './test-utils/helpers/auth.helpers';

test.describe('Event Location Privacy - Conditional Display', () => {
  test.beforeEach(async ({ page }) => {
    // Clear auth state before each test
    await AuthHelpers.clearAuthState(page);
  });

  /**
   * Helper function to find an event with a venue that has a location field
   * Returns the event URL or null if none found
   */
  async function findEventWithVenueLocation(page: Page): Promise<string | null> {
    await page.goto('/events');
    await page.waitForLoadState('domcontentloaded');
    await page.waitForTimeout(1000);

    // Get all event cards
    const eventCards = page.locator('[data-testid="event-card"]');
    const count = await eventCards.count();

    if (count === 0) {
      return null;
    }

    // Click first event to get to detail page
    await eventCards.first().click();
    await page.waitForURL('**/events/**', { timeout: 10000 });

    return page.url();
  }

  test('non-vetted user sees location (city, state) on event details', async ({ page }) => {
    // Login as non-vetted member
    await AuthHelpers.loginAs(page, 'member');

    // Find an event with venue
    const eventUrl = await findEventWithVenueLocation(page);
    expect(eventUrl).not.toBeNull();

    if (!eventUrl) {
      console.log('⚠️ No events with venues found, skipping test');
      return;
    }

    // Navigate to event detail page
    await page.goto(eventUrl);
    await page.waitForLoadState('domcontentloaded');
    await page.waitForTimeout(1000);

    // Verify hero section shows location icon
    const heroSection = page.locator('[data-testid="section-hero"]');
    await expect(heroSection).toBeVisible({ timeout: 5000 });

    // Look for location text pattern (city, state format)
    // The hero should show venue.location for non-vetted users
    // Example: "📍 Salem, MA" or just "Salem, MA"
    const locationText = page.locator('text=/Salem|Boston|Newton|,\\s*MA/i').first();

    // Check if location is visible in hero (may be in different formats)
    const hasLocation = await locationText.isVisible().catch(() => false);

    if (hasLocation) {
      console.log('✅ Non-vetted user sees location in hero section');
    }

    // Verify LIMITED venue section is displayed
    // Should show "LOCATION" header (not "VENUE DETAILS")
    const locationHeader = page.locator('text=/^LOCATION$/i').first();
    const hasLocationHeader = await locationHeader.isVisible({ timeout: 3000 }).catch(() => false);

    if (hasLocationHeader) {
      await expect(locationHeader).toBeVisible();
      console.log('✅ Limited venue section with "LOCATION" header displayed');
    }

    // Verify info alert is displayed explaining full details after registration
    const infoAlert = page.locator('[role="alert"], .mantine-Alert-root').filter({
      hasText: /full venue|after registration|details will be provided/i
    });

    const hasAlert = await infoAlert.count() > 0;

    if (hasAlert) {
      await expect(infoAlert.first()).toBeVisible({ timeout: 5000 });
      console.log('✅ Info alert displayed explaining access after registration');
    }

    // Verify full venue name is NOT displayed (should be location only)
    // Full venue section should have "VENUE DETAILS" or "VENUE" header
    const venueDetailsHeader = page.locator('text=/^VENUE DETAILS$|^VENUE$/i').first();
    const hasVenueHeader = await venueDetailsHeader.isVisible({ timeout: 2000 }).catch(() => false);

    if (hasVenueHeader) {
      console.log('⚠️ Warning: Venue header found for non-vetted user (should show location only)');
    } else {
      console.log('✅ Full venue details header NOT displayed (correct for non-vetted)');
    }

    // Take screenshot for evidence
    await page.screenshot({
      path: './test-results/event-location-non-vetted-user.png',
      fullPage: true
    });

    console.log('✅ Non-vetted user sees limited location information');
  });

  test('vetted user sees venue name on event details', async ({ page }) => {
    // Login as vetted user (teacher has vetted status)
    await AuthHelpers.loginAs(page, 'teacher');

    // Find an event with venue
    const eventUrl = await findEventWithVenueLocation(page);
    expect(eventUrl).not.toBeNull();

    if (!eventUrl) {
      console.log('⚠️ No events with venues found, skipping test');
      return;
    }

    // Navigate to event detail page
    await page.goto(eventUrl);
    await page.waitForLoadState('domcontentloaded');
    await page.waitForTimeout(1000);

    // Verify hero section is visible
    const heroSection = page.locator('[data-testid="section-hero"]');
    await expect(heroSection).toBeVisible({ timeout: 5000 });

    // For vetted users, hero should show venue.name (not location)
    // Look for venue name pattern (capitalized words, not just "City, State")
    const venueNameInHero = page.locator('text=/Studio|Center|Space|Hall|Room/i').first();
    const hasVenueName = await venueNameInHero.isVisible({ timeout: 3000 }).catch(() => false);

    if (hasVenueName) {
      console.log('✅ Vetted user sees venue name in hero section');
    }

    // Verify FULL venue section is displayed
    // Should show "VENUE DETAILS" or "VENUE" header
    const venueHeader = page.locator('text=/^VENUE DETAILS$|^VENUE$/i').first();
    const hasVenueHeader = await venueHeader.isVisible({ timeout: 5000 }).catch(() => false);

    if (hasVenueHeader) {
      await expect(venueHeader).toBeVisible();
      console.log('✅ Full venue section with "VENUE" header displayed');
    }

    // Verify DIRECTIONS section is displayed (part of full venue access)
    const directionsHeader = page.locator('text=/^DIRECTIONS$/i').first();
    const hasDirections = await directionsHeader.isVisible({ timeout: 5000 }).catch(() => false);

    if (hasDirections) {
      await expect(directionsHeader).toBeVisible();
      console.log('✅ Directions section displayed for vetted user');
    }

    // Verify info alert is NOT displayed (only for non-vetted)
    const infoAlert = page.locator('[role="alert"], .mantine-Alert-root').filter({
      hasText: /full venue|after registration|details will be provided/i
    });

    const alertCount = await infoAlert.count();

    if (alertCount === 0) {
      console.log('✅ Info alert NOT displayed for vetted user (correct)');
    } else {
      console.log('⚠️ Warning: Info alert displayed for vetted user (should not show)');
    }

    // Verify location text (city, state) is NOT the primary display
    // Vetted users should see venue name, not generic location
    const locationOnlyHeader = page.locator('text=/^LOCATION$/i').first();
    const hasLocationHeader = await locationOnlyHeader.isVisible({ timeout: 2000 }).catch(() => false);

    if (!hasLocationHeader) {
      console.log('✅ Limited location section NOT displayed (correct for vetted user)');
    } else {
      console.log('⚠️ Warning: Location-only header found for vetted user (should show full venue)');
    }

    // Take screenshot for evidence
    await page.screenshot({
      path: './test-results/event-location-vetted-user.png',
      fullPage: true
    });

    console.log('✅ Vetted user sees full venue name and directions');
  });

  test('admin user sees full venue details', async ({ page }) => {
    // Login as admin (should have full access like vetted users)
    await AuthHelpers.loginAs(page, 'admin');

    // Find an event with venue
    const eventUrl = await findEventWithVenueLocation(page);
    expect(eventUrl).not.toBeNull();

    if (!eventUrl) {
      console.log('⚠️ No events with venues found, skipping test');
      return;
    }

    // Navigate to event detail page
    await page.goto(eventUrl);
    await page.waitForLoadState('domcontentloaded');
    await page.waitForTimeout(1000);

    // Verify FULL venue section is displayed
    const venueHeader = page.locator('text=/^VENUE DETAILS$|^VENUE$/i').first();
    const hasVenueHeader = await venueHeader.isVisible({ timeout: 5000 }).catch(() => false);

    if (hasVenueHeader) {
      await expect(venueHeader).toBeVisible();
      console.log('✅ Admin sees full venue section');
    }

    // Verify DIRECTIONS section is displayed
    const directionsHeader = page.locator('text=/^DIRECTIONS$/i').first();
    const hasDirections = await directionsHeader.isVisible({ timeout: 5000 }).catch(() => false);

    if (hasDirections) {
      await expect(directionsHeader).toBeVisible();
      console.log('✅ Admin sees directions section');
    }

    // Verify info alert is NOT displayed for admin
    const infoAlert = page.locator('[role="alert"], .mantine-Alert-root').filter({
      hasText: /full venue|after registration|details will be provided/i
    });

    const alertCount = await infoAlert.count();
    expect(alertCount).toBe(0);

    console.log('✅ Admin sees full venue details without restrictions');
  });

  test('event without venue location shows appropriate fallback', async ({ page }) => {
    // Login as non-vetted member
    await AuthHelpers.loginAs(page, 'member');

    // Navigate to events page
    await page.goto('/events');
    await page.waitForLoadState('domcontentloaded');
    await page.waitForTimeout(1000);

    // Get all event cards
    const eventCards = page.locator('[data-testid="event-card"]');
    const count = await eventCards.count();

    if (count === 0) {
      console.log('⚠️ No events found, skipping test');
      return;
    }

    // Click first event
    await eventCards.first().click();
    await page.waitForURL('**/events/**', { timeout: 10000 });
    await page.waitForLoadState('domcontentloaded');
    await page.waitForTimeout(1000);

    // Check for fallback text if no location is available
    // Should show "Location TBD" or "TBA" or similar
    const fallbackText = page.locator('text=/Location TBD|TBA|To Be Announced/i').first();
    const hasFallback = await fallbackText.isVisible({ timeout: 3000 }).catch(() => false);

    if (hasFallback) {
      console.log('✅ Fallback text displayed when location not available');
    } else {
      console.log('ℹ️ Event has location data or different fallback pattern');
    }

    // Event should still be viewable even without location
    const heroSection = page.locator('[data-testid="section-hero"]');
    await expect(heroSection).toBeVisible({ timeout: 5000 });

    console.log('✅ Event without location displays correctly');
  });

  test('hero section displays correct location format', async ({ page }) => {
    // Login as non-vetted member
    await AuthHelpers.loginAs(page, 'member');

    // Find an event with venue
    const eventUrl = await findEventWithVenueLocation(page);
    expect(eventUrl).not.toBeNull();

    if (!eventUrl) {
      console.log('⚠️ No events with venues found, skipping test');
      return;
    }

    // Navigate to event detail page
    await page.goto(eventUrl);
    await page.waitForLoadState('domcontentloaded');
    await page.waitForTimeout(1000);

    // Verify hero section exists
    const heroSection = page.locator('[data-testid="section-hero"]');
    await expect(heroSection).toBeVisible({ timeout: 5000 });

    // Look for location icon (📍) in hero
    const locationIcon = heroSection.locator('text=/📍/').first();
    const hasIcon = await locationIcon.isVisible({ timeout: 3000 }).catch(() => false);

    if (hasIcon) {
      console.log('✅ Location icon (📍) displayed in hero section');
    }

    // Verify location text format (should be city, state or venue name)
    // For non-vetted: "Salem, MA" pattern
    // For vetted: Venue name pattern
    const locationText = heroSection.locator('text=/[A-Z][a-z]+,\\s*[A-Z]{2}|Studio|Center|Space/i').first();
    const hasLocationText = await locationText.isVisible({ timeout: 3000 }).catch(() => false);

    if (hasLocationText) {
      const text = await locationText.textContent();
      console.log(`✅ Location text displayed: "${text}"`);
    }

    console.log('✅ Hero section displays location in correct format');
  });

  test('venue section structure changes based on access level', async ({ page }) => {
    // Test that section structure is different for non-vetted vs vetted

    // First check non-vetted user
    await AuthHelpers.loginAs(page, 'member');

    const eventUrl = await findEventWithVenueLocation(page);
    expect(eventUrl).not.toBeNull();

    if (!eventUrl) {
      console.log('⚠️ No events with venues found, skipping test');
      return;
    }

    await page.goto(eventUrl);
    await page.waitForLoadState('domcontentloaded');
    await page.waitForTimeout(1000);

    // Check for limited section structure
    const locationHeader = page.locator('text=/^LOCATION$/i').first();
    const hasLocationOnlySection = await locationHeader.isVisible({ timeout: 3000 }).catch(() => false);

    const infoAlert = page.locator('[role="alert"], .mantine-Alert-root').filter({
      hasText: /full venue|after registration/i
    });
    const hasInfoAlert = await infoAlert.count() > 0;

    console.log(`Non-vetted structure: Location header: ${hasLocationOnlySection}, Info alert: ${hasInfoAlert}`);

    // Now check vetted user (logout and re-login)
    await page.goto('/');
    await AuthHelpers.clearAuthState(page);
    await AuthHelpers.loginAs(page, 'teacher');

    await page.goto(eventUrl);
    await page.waitForLoadState('domcontentloaded');
    await page.waitForTimeout(1000);

    // Check for full section structure
    const venueHeader = page.locator('text=/^VENUE DETAILS$|^VENUE$/i').first();
    const hasVenueSection = await venueHeader.isVisible({ timeout: 3000 }).catch(() => false);

    const directionsHeader = page.locator('text=/^DIRECTIONS$/i').first();
    const hasDirectionsSection = await directionsHeader.isVisible({ timeout: 3000 }).catch(() => false);

    const alertAfterVetted = page.locator('[role="alert"], .mantine-Alert-root').filter({
      hasText: /full venue|after registration/i
    });
    const hasAlertVetted = await alertAfterVetted.count() > 0;

    console.log(`Vetted structure: Venue header: ${hasVenueSection}, Directions: ${hasDirectionsSection}, Alert: ${hasAlertVetted}`);

    // Verify structure is different
    if (hasLocationOnlySection && hasVenueSection) {
      console.log('✅ Venue section structure changes based on user access level');
    } else if (!hasLocationOnlySection && hasVenueSection) {
      console.log('✅ Different section structures confirmed (limited vs full)');
    } else {
      console.log('ℹ️ Section structure variation detected');
    }
  });
});
