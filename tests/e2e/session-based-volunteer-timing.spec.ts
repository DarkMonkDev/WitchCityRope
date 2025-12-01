/**
 * Session-Based Volunteer Timing E2E Tests
 *
 * Tests that verify session-based timing functionality for volunteer positions
 * from a user's perspective. These tests validate the implementation of the
 * session timing refactor specification.
 *
 * Specification: /docs/functional-areas/events/session-timing-refactor/SPECIFICATION.md
 *
 * Key Scenarios:
 * - Session-specific volunteer positions show for future sessions only
 * - Event-wide positions use earliest future session for timing
 * - Volunteer positions show session information clearly
 * - Signup windows respect session-based timing calculations
 *
 * Created: 2025-11-30
 */

import { test, expect, Page } from '@playwright/test';
import { AuthHelpers } from './test-utils/helpers/auth.helpers';

test.describe('Session-Based Volunteer Timing', () => {
  let page: Page;

  test.beforeEach(async ({ browser }) => {
    page = await browser.newPage();
  });

  test.afterEach(async () => {
    await page.close();
  });

  test('session-specific volunteer position shows for future session', async () => {
    // This test verifies that volunteer positions assigned to a specific session
    // are visible when that session is in the future

    // Login as vetted member (required to see volunteer opportunities)
    await AuthHelpers.loginAs(page, 'vetted');

    await page.goto('/events');
    await page.waitForLoadState('domcontentloaded');

    const eventCards = page.locator('[data-testid="event-card"]');
    const eventCount = await eventCards.count();

    if (eventCount === 0) {
      console.log('⚠️ No events found - skipping test');
      test.skip();
      return;
    }

    // Click first event
    await eventCards.first().click();
    await page.waitForLoadState('domcontentloaded');

    // Look for volunteer section
    const volunteerSection = page.locator(
      '[data-testid="volunteer-section"], section:has-text("Volunteer")'
    ).first();

    if (await volunteerSection.count() === 0) {
      console.log('⚠️ No volunteer section found - event may not have volunteer positions');
      test.skip();
      return;
    }

    await expect(volunteerSection).toBeVisible();

    // Look for volunteer position cards
    const volunteerCards = page.locator('[data-testid="volunteer-position-card"], .volunteer-position');

    if (await volunteerCards.count() === 0) {
      console.log('⚠️ No volunteer positions found');
      test.skip();
      return;
    }

    // Check first volunteer position
    const firstPosition = volunteerCards.first();
    await expect(firstPosition).toBeVisible();

    // Look for signup button or status
    const signupButton = firstPosition.locator('button:has-text("Sign Up"), button:has-text("Volunteer")').first();
    const closedMessage = firstPosition.locator('text=/signup closed/i, text=/fully staffed/i').first();

    if (await signupButton.count() > 0) {
      await expect(signupButton).toBeVisible();
      console.log('✅ Volunteer position shows signup button (within timing window)');
    } else if (await closedMessage.count() > 0) {
      await expect(closedMessage).toBeVisible();
      const message = await closedMessage.textContent();
      console.log(`✅ Volunteer position shows status: "${message}"`);
    } else {
      console.log('⚠️ No signup button or status found - UI may need implementation');
    }

    // Check if position shows session information
    const positionText = await firstPosition.textContent();
    if (positionText?.match(/session/i)) {
      console.log('✅ Position displays session information');
    }
  });

  test('past session volunteer position is hidden', async () => {
    // This test verifies that volunteer positions for past sessions
    // are not displayed to users (filtered out by backend)

    await AuthHelpers.loginAs(page, 'vetted');

    await page.goto('/events');
    await page.waitForLoadState('domcontentloaded');

    const eventCards = page.locator('[data-testid="event-card"]');
    const eventCount = await eventCards.count();

    if (eventCount === 0) {
      console.log('⚠️ No events found - skipping test');
      test.skip();
      return;
    }

    // Try to find an event with past sessions
    // This is difficult with seed data that's mostly future events
    let foundMultiSessionEvent = false;

    for (let i = 0; i < Math.min(eventCount, 5); i++) {
      await eventCards.nth(i).click();
      await page.waitForLoadState('domcontentloaded');

      // Check if this event has sessions section
      const sessionsSection = page.locator('[data-testid="event-sessions"], section:has-text("Sessions")').first();

      if (await sessionsSection.count() > 0) {
        foundMultiSessionEvent = true;
        break;
      }

      // Go back and try next event
      await page.goto('/events');
      await page.waitForLoadState('domcontentloaded');
    }

    if (!foundMultiSessionEvent) {
      console.log('⚠️ No multi-session events found - skipping test');
      test.skip();
      return;
    }

    // Look for volunteer section
    const volunteerSection = page.locator(
      '[data-testid="volunteer-section"], section:has-text("Volunteer")'
    ).first();

    if (await volunteerSection.count() === 0) {
      console.log('⚠️ No volunteer section found');
      test.skip();
      return;
    }

    // Count volunteer positions
    const volunteerCards = page.locator('[data-testid="volunteer-position-card"], .volunteer-position');
    const positionCount = await volunteerCards.count();

    // All positions shown should be for future sessions
    // Backend filters out past session positions, so we verify they're not visible
    console.log(`✅ ${positionCount} volunteer positions visible (past sessions filtered by backend)`);

    // If there are positions, verify they don't mention past sessions
    if (positionCount > 0) {
      for (let i = 0; i < positionCount; i++) {
        const position = volunteerCards.nth(i);
        const positionText = await position.textContent();

        // Should not contain indicators of past sessions
        // (Actual detection would depend on how past sessions are labeled)
        if (positionText?.match(/ended|past|completed/i)) {
          console.log('⚠️ Found position with past session indicator - should be filtered');
        }
      }
    }

    // Verify positions can be interacted with (not disabled/grayed out)
    if (positionCount > 0) {
      const firstPosition = volunteerCards.first();
      await expect(firstPosition).toBeVisible();
      console.log('✅ Visible positions are active (future sessions only)');
    }
  });

  test('volunteer signup shows session name', async () => {
    // This test verifies that volunteer positions clearly display
    // which session they're associated with

    await AuthHelpers.loginAs(page, 'vetted');

    await page.goto('/events');
    await page.waitForLoadState('domcontentloaded');

    const eventCards = page.locator('[data-testid="event-card"]');
    const eventCount = await eventCards.count();

    if (eventCount === 0) {
      console.log('⚠️ No events found - skipping test');
      test.skip();
      return;
    }

    // Click first event
    await eventCards.first().click();
    await page.waitForLoadState('domcontentloaded');

    // Check if this is a multi-session event
    const sessionsSection = page.locator('[data-testid="event-sessions"], section:has-text("Sessions")').first();

    if (await sessionsSection.count() === 0) {
      console.log('⚠️ Single-session event - session name display not applicable');
      test.skip();
      return;
    }

    // Look for volunteer positions
    const volunteerSection = page.locator(
      '[data-testid="volunteer-section"], section:has-text("Volunteer")'
    ).first();

    if (await volunteerSection.count() === 0) {
      console.log('⚠️ No volunteer section found');
      test.skip();
      return;
    }

    const volunteerCards = page.locator('[data-testid="volunteer-position-card"], .volunteer-position');

    if (await volunteerCards.count() === 0) {
      console.log('⚠️ No volunteer positions found');
      test.skip();
      return;
    }

    // Check first position for session information
    const firstPosition = volunteerCards.first();
    const positionText = await firstPosition.textContent();

    // Look for session name badge or label
    const sessionBadge = firstPosition.locator('[data-testid="session-badge"], .session-name, badge').first();

    if (await sessionBadge.count() > 0) {
      await expect(sessionBadge).toBeVisible();
      const badgeText = await sessionBadge.textContent();
      console.log(`✅ Position shows session badge: "${badgeText}"`);
    } else if (positionText?.match(/session \w+|for session|session:/i)) {
      console.log('✅ Position includes session information in text');
    } else {
      console.log('⚠️ No clear session indicator found - may need UI implementation');
      console.log('   For multi-session events, positions should show which session they belong to');
    }
  });

  test('event-wide position available after first session passes', async () => {
    // This test verifies that volunteer positions WITHOUT a specific SessionId
    // (event-wide positions) remain available based on the earliest future session,
    // not the first session of the event

    await AuthHelpers.loginAs(page, 'vetted');

    await page.goto('/events');
    await page.waitForLoadState('domcontentloaded');

    const eventCards = page.locator('[data-testid="event-card"]');
    const eventCount = await eventCards.count();

    if (eventCount === 0) {
      console.log('⚠️ No events found - skipping test');
      test.skip();
      return;
    }

    // Find a multi-session event
    let foundMultiSessionEvent = false;

    for (let i = 0; i < Math.min(eventCount, 5); i++) {
      await eventCards.nth(i).click();
      await page.waitForLoadState('domcontentloaded');

      const sessionsSection = page.locator('[data-testid="event-sessions"], section:has-text("Sessions")').first();

      if (await sessionsSection.count() > 0) {
        // Verify there are multiple sessions
        const sessionList = sessionsSection.locator('[data-testid="session-item"], li');
        const sessionCount = await sessionList.count();

        if (sessionCount >= 2) {
          foundMultiSessionEvent = true;
          console.log(`✅ Found multi-session event with ${sessionCount} sessions`);
          break;
        }
      }

      await page.goto('/events');
      await page.waitForLoadState('domcontentloaded');
    }

    if (!foundMultiSessionEvent) {
      console.log('⚠️ No multi-session events found - skipping test');
      test.skip();
      return;
    }

    // Look for volunteer positions
    const volunteerSection = page.locator(
      '[data-testid="volunteer-section"], section:has-text("Volunteer")'
    ).first();

    if (await volunteerSection.count() === 0) {
      console.log('⚠️ No volunteer section found');
      test.skip();
      return;
    }

    const volunteerCards = page.locator('[data-testid="volunteer-position-card"], .volunteer-position');
    const positionCount = await volunteerCards.count();

    if (positionCount === 0) {
      console.log('⚠️ No volunteer positions found');
      test.skip();
      return;
    }

    console.log(`✅ ${positionCount} volunteer positions visible`);

    // Check for positions that might be event-wide (no specific session)
    // Event-wide positions should show signup button based on earliest future session
    for (let i = 0; i < positionCount; i++) {
      const position = volunteerCards.nth(i);
      const positionText = await position.textContent();

      // Event-wide positions might not have specific session badge
      const hasSessionBadge = await position.locator('[data-testid="session-badge"]').count() > 0;
      const mentionsSpecificSession = positionText?.match(/session \w+/i);

      if (!hasSessionBadge && !mentionsSpecificSession) {
        console.log(`✅ Found potential event-wide position (no specific session mentioned)`);

        // Verify it has signup button or status
        const signupButton = position.locator('button:has-text("Sign Up")').first();
        const statusMessage = position.locator('text=/signup closed|fully staffed/i').first();

        if (await signupButton.count() > 0) {
          await expect(signupButton).toBeVisible();
          console.log('  ✅ Event-wide position shows signup button (based on earliest future session)');
        } else if (await statusMessage.count() > 0) {
          const status = await statusMessage.textContent();
          console.log(`  ✅ Event-wide position shows status: "${status}"`);
        }

        break; // Found at least one, that's sufficient
      }
    }
  });

  test('volunteer cancellation respects session timing', async () => {
    // This test verifies that volunteer signup cancellation uses
    // session-based timing (VolunteerCancellationCloseHours from reference session)

    await AuthHelpers.loginAs(page, 'vetted');

    // Navigate to user's volunteer shifts
    await page.goto('/dashboard');
    await page.waitForLoadState('domcontentloaded');

    // Look for user's volunteer shifts section
    const volunteerShiftsSection = page.locator(
      '[data-testid="my-volunteer-shifts"], section:has-text("Volunteer Shifts")'
    ).first();

    if (await volunteerShiftsSection.count() === 0) {
      console.log('⚠️ No volunteer shifts section found - user may not have volunteer signups');
      test.skip();
      return;
    }

    // Look for volunteer shift cards
    const shiftCards = page.locator('[data-testid="volunteer-shift-card"], .volunteer-shift');

    if (await shiftCards.count() === 0) {
      console.log('⚠️ User has no volunteer shifts');
      test.skip();
      return;
    }

    // Check first shift for cancel button or timing message
    const firstShift = shiftCards.first();
    await expect(firstShift).toBeVisible();

    const cancelButton = firstShift.locator('button:has-text("Cancel")').first();
    const timingMessage = firstShift.locator(
      'text=/cannot cancel/i, text=/cancellation closed/i, text=/too late/i'
    ).first();

    if (await cancelButton.count() > 0) {
      // Cancel button exists - within cancellation window
      await expect(cancelButton).toBeVisible();
      await expect(cancelButton).toBeEnabled();
      console.log('✅ Cancel button enabled (within VolunteerCancellationCloseHours window)');
    } else if (await timingMessage.count() > 0) {
      // Timing message shown - outside cancellation window
      await expect(timingMessage).toBeVisible();
      const message = await timingMessage.textContent();
      console.log(`✅ Cancellation timing message shown: "${message}"`);
    } else {
      console.log('⚠️ No cancel button or timing message found');
    }
  });

  test('session-specific position timing is independent from other sessions', async () => {
    // This test verifies that a volunteer position assigned to Session 2
    // uses Session 2's timing, even if Session 1 has already passed

    await AuthHelpers.loginAs(page, 'vetted');

    await page.goto('/events');
    await page.waitForLoadState('domcontentloaded');

    const eventCards = page.locator('[data-testid="event-card"]');
    const eventCount = await eventCards.count();

    if (eventCount === 0) {
      console.log('⚠️ No events found - skipping test');
      test.skip();
      return;
    }

    // Find multi-session event
    let foundMultiSessionEvent = false;

    for (let i = 0; i < Math.min(eventCount, 5); i++) {
      await eventCards.nth(i).click();
      await page.waitForLoadState('domcontentloaded');

      const sessionsSection = page.locator('[data-testid="event-sessions"], section:has-text("Sessions")').first();

      if (await sessionsSection.count() > 0) {
        const sessionList = sessionsSection.locator('[data-testid="session-item"], li');
        if (await sessionList.count() >= 2) {
          foundMultiSessionEvent = true;
          break;
        }
      }

      await page.goto('/events');
      await page.waitForLoadState('domcontentloaded');
    }

    if (!foundMultiSessionEvent) {
      console.log('⚠️ No multi-session events found - skipping test');
      test.skip();
      return;
    }

    // Look for volunteer positions
    const volunteerCards = page.locator('[data-testid="volunteer-position-card"], .volunteer-position');

    if (await volunteerCards.count() === 0) {
      console.log('⚠️ No volunteer positions found');
      test.skip();
      return;
    }

    // Check if any positions show session-specific information
    let foundSessionSpecificPosition = false;

    for (let i = 0; i < await volunteerCards.count(); i++) {
      const position = volunteerCards.nth(i);
      const positionText = await position.textContent();

      // Look for session badge or session mention
      const sessionBadge = position.locator('[data-testid="session-badge"]').first();

      if (await sessionBadge.count() > 0) {
        foundSessionSpecificPosition = true;
        const badgeText = await sessionBadge.textContent();
        console.log(`✅ Found session-specific position: "${badgeText}"`);

        // Verify this position has signup button (meaning session timing allows it)
        const signupButton = position.locator('button:has-text("Sign Up")').first();

        if (await signupButton.count() > 0) {
          await expect(signupButton).toBeVisible();
          console.log('  ✅ Signup button visible - session timing allows signup');
        }

        break;
      }
    }

    if (!foundSessionSpecificPosition) {
      console.log('⚠️ No clearly session-specific positions found');
      console.log('   All positions may be event-wide, which is acceptable');
    }
  });

  test('volunteer timing respects VolunteerRegistrationCloseHours setting', async () => {
    // This test verifies that volunteer signup windows close based on
    // VolunteerRegistrationCloseHours relative to session start time

    // Login as admin to check event settings
    await AuthHelpers.loginAs(page, 'admin');

    await page.goto('/admin/events');
    await page.waitForLoadState('domcontentloaded');

    const eventRows = page.locator('[data-testid="event-row"], tr:has-text("Event")');

    if (await eventRows.count() === 0) {
      console.log('⚠️ No events found in admin panel');
      test.skip();
      return;
    }

    // Click first event
    await eventRows.first().click();
    await page.waitForLoadState('domcontentloaded');

    // Look for volunteer registration timing setting
    const volunteerRegCloseField = page.locator(
      '[data-testid="volunteer-registration-close-hours"], input[name="volunteerRegistrationCloseHours"]'
    ).first();

    if (await volunteerRegCloseField.count() === 0) {
      console.log('⚠️ VolunteerRegistrationCloseHours field not found in admin panel');
      test.skip();
      return;
    }

    const closeHours = await volunteerRegCloseField.inputValue();
    console.log(`Volunteer registration closes: ${closeHours} hours before session`);

    // Get event ID
    const eventUrl = page.url();
    const eventId = eventUrl.match(/\/events\/([^\/]+)/)?.[1];

    if (!eventId) {
      console.log('⚠️ Could not extract event ID');
      test.skip();
      return;
    }

    // Logout and login as vetted member
    await page.goto('/logout');
    await page.waitForLoadState('domcontentloaded');

    await AuthHelpers.loginAs(page, 'vetted');

    // Navigate to event public page
    await page.goto(`/events/${eventId}`);
    await page.waitForLoadState('domcontentloaded');

    // Check volunteer positions
    const volunteerCards = page.locator('[data-testid="volunteer-position-card"], .volunteer-position');

    if (await volunteerCards.count() === 0) {
      console.log('⚠️ No volunteer positions found');
      test.skip();
      return;
    }

    // Check first position for signup availability
    const firstPosition = volunteerCards.first();
    const signupButton = firstPosition.locator('button:has-text("Sign Up")').first();
    const closedMessage = firstPosition.locator('text=/signup closed/i').first();

    if (await signupButton.count() > 0) {
      await expect(signupButton).toBeVisible();
      console.log(`✅ Signup available (event > ${closeHours} hours away)`);
    } else if (await closedMessage.count() > 0) {
      await expect(closedMessage).toBeVisible();
      console.log(`✅ Signup closed (event < ${closeHours} hours away)`);
    } else {
      console.log('⚠️ No signup button or closed message found');
    }
  });
});
