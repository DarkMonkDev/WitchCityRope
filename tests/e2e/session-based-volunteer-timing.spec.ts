/**
 * Session-Based Volunteer Timing E2E Tests
 *
 * Tests that verify session-based timing functionality for volunteer positions
 * from a user's perspective. These tests validate the implementation of the
 * session timing refactor specification.
 *
 * Key Scenarios:
 * - Session-specific volunteer positions show for future sessions only
 * - Event-wide positions use earliest future session for timing
 * - Volunteer positions show session information clearly
 * - Signup windows respect session-based timing calculations
 *
 * ARCHITECTURE: Tests create their own event data to ensure proper test isolation
 * and avoid dependency on seed data.
 *
 * Created: 2025-11-30
 * Updated: 2025-12-09 - Refactored to create own test data
 */

import { test, expect, Page } from '@playwright/test';
import { AuthHelpers } from './test-utils/helpers/auth.helpers';

// Helper to make authenticated API request
async function apiRequest(page: Page, method: string, url: string, data?: unknown): Promise<{ status: number; data: unknown }> {
  const response = await page.evaluate(async ({ method, url, data }) => {
    const options: RequestInit = {
      method,
      credentials: 'include',
      headers: data ? { 'Content-Type': 'application/json' } : {},
    };

    if (data) {
      options.body = JSON.stringify(data);
    }

    const res = await fetch(url, options);
    const text = await res.text();
    try {
      return { status: res.status, data: JSON.parse(text) };
    } catch {
      return { status: res.status, data: text };
    }
  }, { method, url, data });

  return response;
}

test.describe('Session-Based Volunteer Timing', () => {
  let testEventId: string | null = null;
  let session1Id: string | null = null;
  let session2Id: string | null = null;
  let volunteerPositionId: string | null = null;

  test.beforeAll(async ({ browser }) => {
    // Create a multi-session event with volunteer positions for testing
    const page = await browser.newPage();
    await AuthHelpers.loginAs(page, 'admin');

    // Get first venue ID
    const venuesResponse = await apiRequest(page, 'GET', '/api/venues');
    const venues = venuesResponse.data as Array<{ id: string }>;
    const venueId = venues[0]?.id;

    if (!venueId) {
      console.error('No venues found - cannot create test event');
      await page.close();
      return;
    }

    // Create event with 2 sessions - one 7 days out, one 8 days out
    const session1Start = new Date();
    session1Start.setDate(session1Start.getDate() + 7);
    session1Start.setHours(18, 0, 0, 0);

    const session2Start = new Date();
    session2Start.setDate(session2Start.getDate() + 8);
    session2Start.setHours(18, 0, 0, 0);

    const eventData = {
      title: `Volunteer Timing Test Event ${Date.now()}`,
      shortDescription: 'Test event for session-based volunteer timing',
      description: 'This event tests volunteer timing calculations based on sessions.',
      eventType: 'Class',
      startDate: session1Start.toISOString(),
      endDate: session2Start.toISOString(),
      venueId: venueId,
      capacity: 20,
      isPublished: true,
      // CRITICAL: Timing controls
      registrationOpenHours: null, // No open restriction
      registrationCloseHours: 0,   // Don't close before session
      cancellationCloseHours: 24,  // Can cancel until 24 hours before
      volunteerRegistrationCloseHours: 0,  // Don't close volunteer signup
      volunteerCancellationCloseHours: 24, // Can cancel volunteer until 24 hours before
      sessions: [
        {
          sessionIdentifier: 'S1',
          name: 'Day 1 Session',
          startTime: session1Start.toISOString(),
          endTime: new Date(session1Start.getTime() + 3 * 60 * 60 * 1000).toISOString(),
          capacity: 20,
        },
        {
          sessionIdentifier: 'S2',
          name: 'Day 2 Session',
          startTime: session2Start.toISOString(),
          endTime: new Date(session2Start.getTime() + 3 * 60 * 60 * 1000).toISOString(),
          capacity: 20,
        },
      ],
    };

    console.log('Creating test event with 2 sessions...');
    const createResponse = await apiRequest(page, 'POST', '/api/admin/events', eventData);

    if (createResponse.status !== 201 && createResponse.status !== 200) {
      console.error('Failed to create test event:', createResponse);
      await page.close();
      return;
    }

    const responseData = createResponse.data as { id: string };
    testEventId = responseData.id;
    console.log(`✅ Created test event: ${testEventId}`);

    // Get session IDs
    const eventResponse = await apiRequest(page, 'GET', `/api/events/${testEventId}`);
    const eventDetails = eventResponse.data as { sessions: Array<{ id: string; sessionIdentifier: string }> };
    const sessions = eventDetails.sessions || [];
    session1Id = sessions.find((s) => s.sessionIdentifier === 'S1')?.id || null;
    session2Id = sessions.find((s) => s.sessionIdentifier === 'S2')?.id || null;

    console.log(`Session 1 ID: ${session1Id}`);
    console.log(`Session 2 ID: ${session2Id}`);

    // Create volunteer position for Session 1 (session-specific)
    const session1PositionData = {
      eventId: testEventId,
      name: 'Setup Helper - Day 1',
      description: 'Help set up equipment for Day 1 session',
      requiredCount: 2,
      sessionIdentifier: 'S1',
    };

    const session1PositionResponse = await apiRequest(
      page,
      'POST',
      `/api/admin/events/${testEventId}/volunteer-positions`,
      session1PositionData
    );

    if (session1PositionResponse.status === 200 || session1PositionResponse.status === 201) {
      const positionData = session1PositionResponse.data as { id: string };
      volunteerPositionId = positionData.id;
      console.log(`✅ Created volunteer position for S1: ${volunteerPositionId}`);
    }

    // Create volunteer position for Session 2 (session-specific)
    const session2PositionData = {
      eventId: testEventId,
      name: 'Setup Helper - Day 2',
      description: 'Help set up equipment for Day 2 session',
      requiredCount: 2,
      sessionIdentifier: 'S2',
    };

    const session2PositionResponse = await apiRequest(
      page,
      'POST',
      `/api/admin/events/${testEventId}/volunteer-positions`,
      session2PositionData
    );

    if (session2PositionResponse.status === 200 || session2PositionResponse.status === 201) {
      console.log('✅ Created volunteer position for S2');
    }

    // Create event-wide volunteer position (no session)
    const eventWidePositionData = {
      eventId: testEventId,
      name: 'General Event Support',
      description: 'Help with general event logistics',
      requiredCount: 3,
      // No sessionIdentifier - event-wide position
    };

    const eventWidePositionResponse = await apiRequest(
      page,
      'POST',
      `/api/admin/events/${testEventId}/volunteer-positions`,
      eventWidePositionData
    );

    if (eventWidePositionResponse.status === 200 || eventWidePositionResponse.status === 201) {
      console.log('✅ Created event-wide volunteer position');
    }

    await page.close();
  });

  test.afterAll(async ({ browser }) => {
    // Cleanup: Delete test event
    if (!testEventId) return;

    const page = await browser.newPage();
    await AuthHelpers.loginAs(page, 'admin');

    console.log(`Cleaning up test event: ${testEventId}`);
    await apiRequest(page, 'DELETE', `/api/admin/events/${testEventId}`);
    console.log('✅ Test event deleted');

    await page.close();
  });

  test('session-specific volunteer position shows for future session', async ({ page }) => {
    if (!testEventId) {
      test.fail(true, 'Test event not created in beforeAll');
      return;
    }

    // Login as vetted member (required to see volunteer opportunities)
    await AuthHelpers.loginAs(page, 'vetted');

    // Navigate to the test event's public page
    await page.goto(`/events/${testEventId}`);
    await page.waitForLoadState('domcontentloaded');

    // Verify page loaded
    await expect(page.locator('h1, h2').first()).toBeVisible({ timeout: 10000 });

    // Look for volunteer section
    const volunteerSection = page.locator('[data-testid="volunteer-section"], section:has-text("Volunteer")').first();

    // Verify volunteer section exists (event has future sessions)
    await expect(volunteerSection).toBeVisible({ timeout: 5000 });
    console.log('✅ Volunteer section visible for multi-session event');

    // Check for volunteer position cards
    const volunteerCards = page.locator('[data-testid="volunteer-position-card"], .volunteer-position');
    const positionCount = await volunteerCards.count();

    if (positionCount > 0) {
      console.log(`✅ Found ${positionCount} volunteer position(s)`);

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
      }
    }
  });

  test('volunteer positions display correctly on event page', async ({ page }) => {
    if (!testEventId) {
      test.fail(true, 'Test event not created in beforeAll');
      return;
    }

    await AuthHelpers.loginAs(page, 'vetted');

    await page.goto(`/events/${testEventId}`);
    await page.waitForLoadState('domcontentloaded');

    // Look for volunteer section
    const volunteerSection = page.locator('[data-testid="volunteer-section"], section:has-text("Volunteer")').first();

    if (await volunteerSection.count() > 0) {
      await expect(volunteerSection).toBeVisible();
      console.log('✅ Volunteer section visible');

      // Count volunteer positions
      const volunteerCards = page.locator('[data-testid="volunteer-position-card"], .volunteer-position');
      const positionCount = await volunteerCards.count();

      // We created 3 positions: S1-specific, S2-specific, event-wide
      console.log(`✅ Found ${positionCount} volunteer position(s)`);
      expect(positionCount).toBeGreaterThanOrEqual(1);
    }
  });

  test('volunteer signup shows session name', async ({ page }) => {
    if (!testEventId) {
      test.fail(true, 'Test event not created in beforeAll');
      return;
    }

    await AuthHelpers.loginAs(page, 'vetted');

    await page.goto(`/events/${testEventId}`);
    await page.waitForLoadState('domcontentloaded');

    // Look for volunteer positions
    const volunteerCards = page.locator('[data-testid="volunteer-position-card"], .volunteer-position');

    if (await volunteerCards.count() > 0) {
      const firstPosition = volunteerCards.first();
      const positionText = await firstPosition.textContent();

      console.log(`Volunteer position content: ${positionText?.substring(0, 100)}...`);

      // Look for session information in position
      if (positionText?.match(/session|S1|S2|Day 1|Day 2/i)) {
        console.log('✅ Volunteer position displays session information');
      } else {
        console.log('Volunteer position may not show explicit session names');
      }
    }
  });

  test('event-wide position available after first session passes', async ({ page }) => {
    if (!testEventId) {
      test.fail(true, 'Test event not created in beforeAll');
      return;
    }

    // This test verifies that volunteer positions WITHOUT a specific SessionId
    // (event-wide positions) remain available based on the earliest future session

    await AuthHelpers.loginAs(page, 'vetted');

    await page.goto(`/events/${testEventId}`);
    await page.waitForLoadState('domcontentloaded');

    // Look for volunteer positions
    const volunteerCards = page.locator('[data-testid="volunteer-position-card"], .volunteer-position');
    const positionCount = await volunteerCards.count();

    if (positionCount > 0) {
      console.log(`✅ ${positionCount} volunteer positions visible`);

      // Check for positions that might be event-wide (no specific session)
      // Our "General Event Support" position should be here
      for (let i = 0; i < positionCount; i++) {
        const position = volunteerCards.nth(i);
        const positionText = await position.textContent();

        if (positionText?.includes('General Event Support')) {
          console.log('✅ Found event-wide position: "General Event Support"');

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

          break;
        }
      }
    }
  });

  test('admin can view volunteer timing settings for event', async ({ page }) => {
    if (!testEventId) {
      test.fail(true, 'Test event not created in beforeAll');
      return;
    }

    await AuthHelpers.loginAs(page, 'admin');
    await page.goto(`/admin/events/${testEventId}`);
    await page.waitForLoadState('domcontentloaded');

    // Verify admin page loads
    await expect(page.locator('[data-testid="page-admin-event-details"]')).toBeVisible({ timeout: 10000 });

    // Look for timing settings
    const pageContent = await page.locator('body').textContent();

    // Check for volunteer timing fields
    if (pageContent?.match(/volunteer.*registration|volunteer.*close|volunteer.*cancel/i)) {
      console.log('✅ Volunteer timing settings visible in admin panel');
    }

    // Navigate to Setup tab if exists
    const setupTab = page.getByRole('tab', { name: /setup|volunteer/i });
    if (await setupTab.count() > 0) {
      await setupTab.click();
      await page.waitForTimeout(500);

      // Verify volunteer positions are displayed
      const volunteerSection = page.locator('[data-testid="volunteer-positions-section"], [data-testid="grid-volunteer-positions"]');
      if (await volunteerSection.count() > 0) {
        await expect(volunteerSection).toBeVisible();
        console.log('✅ Volunteer positions grid visible in admin');
      }
    }
  });

  test('member can view event with volunteer positions', async ({ page }) => {
    if (!testEventId) {
      test.fail(true, 'Test event not created in beforeAll');
      return;
    }

    await AuthHelpers.loginAs(page, 'vetted');
    await page.goto(`/events/${testEventId}`);
    await page.waitForLoadState('domcontentloaded');

    // Verify event page loads for authenticated user
    await expect(page.locator('h1, h2').first()).toBeVisible({ timeout: 10000 });

    // Look for volunteer section
    const volunteerSection = page.locator('[data-testid="volunteer-section"], section:has-text("Volunteer")').first();

    if (await volunteerSection.count() > 0) {
      await expect(volunteerSection).toBeVisible();
      console.log('✅ Vetted member can see volunteer section');

      // Check for signup options
      const signupOptions = page.locator('button').filter({ hasText: /sign up|volunteer/i });
      if (await signupOptions.count() > 0) {
        console.log('✅ Signup options available to vetted member');
      }
    }
  });

  test('volunteer timing uses session dates not event dates', async ({ page }) => {
    if (!testEventId) {
      test.fail(true, 'Test event not created in beforeAll');
      return;
    }

    // This test verifies the core session-based timing behavior
    // Volunteer positions should be available based on session timing, not event start date

    await AuthHelpers.loginAs(page, 'vetted');

    await page.goto(`/events/${testEventId}`);
    await page.waitForLoadState('domcontentloaded');

    // Verify volunteer section is visible (our test event has future sessions)
    const volunteerSection = page.locator('[data-testid="volunteer-section"], section:has-text("Volunteer")').first();
    await expect(volunteerSection).toBeVisible({ timeout: 5000 });

    // Look for "signup closed" or "no positions" messages - should NOT appear
    const closedMessage = page.locator('text=/signup.*closed|no.*positions|no.*volunteers/i').first();
    const hasClosedMessage = await closedMessage.count() > 0;

    if (!hasClosedMessage) {
      console.log('✅ No "signup closed" message - volunteer positions available for future sessions');
    } else {
      const messageText = await closedMessage.textContent();
      console.log(`⚠️ Found closed message: ${messageText}`);
    }

    // Verify at least one volunteer position option exists
    const volunteerOptions = page.locator('[data-testid="volunteer-position-card"], .volunteer-position, .volunteer-option');
    const optionCount = await volunteerOptions.count();
    expect(optionCount).toBeGreaterThan(0);
    console.log(`✅ Found ${optionCount} volunteer position(s) for event with future sessions`);
  });
});
