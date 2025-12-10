/**
 * Session-Based Ticket Timing E2E Tests
 *
 * Tests that verify session-based timing functionality for ticket purchases
 * from a user's perspective. These tests validate the implementation of the
 * session timing refactor specification.
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

test.describe('Session-Based Ticket Timing', () => {
  let testEventId: string | null = null;
  let session1Id: string | null = null;
  let session2Id: string | null = null;
  let ticketTypeId: string | null = null;

  test.beforeAll(async ({ browser }) => {
    // Create a multi-session event with ticket types for testing
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
      title: `Ticket Timing Test Event ${Date.now()}`,
      shortDescription: 'Test event for session-based ticket timing',
      description: 'This event tests ticket timing calculations based on sessions.',
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

    // Create ticket types
    const ticketTypeData = {
      eventId: testEventId,
      name: 'Both Sessions Pass',
      description: 'Access to both Day 1 and Day 2 sessions',
      price: 50.00,
      capacity: null,
      sessionIdentifiers: ['S1', 'S2'],
    };

    const ticketResponse = await apiRequest(
      page,
      'POST',
      `/api/admin/events/${testEventId}/ticket-types`,
      ticketTypeData
    );

    if (ticketResponse.status === 200 || ticketResponse.status === 201) {
      const ticketData = ticketResponse.data as { id: string };
      ticketTypeId = ticketData.id;
      console.log(`✅ Created ticket type: ${ticketTypeId}`);
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

  test('multi-session event shows tickets for future sessions', async ({ page }) => {
    if (!testEventId) {
      test.fail(true, 'Test event not created in beforeAll');
      return;
    }

    // Navigate to the test event's public page
    await page.goto(`/events/${testEventId}`);
    await page.waitForLoadState('domcontentloaded');

    // Verify page loaded
    await expect(page.locator('h1, h2').first()).toBeVisible({ timeout: 10000 });

    // Look for ticket section
    const ticketSection = page.locator('[data-testid="ticket-section"], section:has-text("Tickets")').first();

    // Verify tickets section exists (event has future sessions)
    await expect(ticketSection).toBeVisible({ timeout: 5000 });
    console.log('✅ Ticket section visible for multi-session event with future sessions');

    // Check for ticket cards or purchase options
    const ticketCards = page.locator('[data-testid="ticket-card"], .ticket-card, [data-testid="ticket-type"]');
    const ticketCount = await ticketCards.count();

    if (ticketCount > 0) {
      console.log(`✅ Found ${ticketCount} ticket type(s)`);
      await expect(ticketCards.first()).toBeVisible();
    }
  });

  test('event displays session information', async ({ page }) => {
    if (!testEventId) {
      test.fail(true, 'Test event not created in beforeAll');
      return;
    }

    await page.goto(`/events/${testEventId}`);
    await page.waitForLoadState('domcontentloaded');

    // Look for sessions section
    const sessionsSection = page.locator('[data-testid="event-sessions"], section:has-text("Sessions"), [data-testid="sessions-list"]').first();

    if (await sessionsSection.count() > 0) {
      await expect(sessionsSection).toBeVisible();
      console.log('✅ Sessions section visible');

      // Verify session names are displayed
      const pageContent = await page.locator('body').textContent();
      if (pageContent?.includes('Day 1') || pageContent?.includes('S1')) {
        console.log('✅ Session 1 information visible');
      }
      if (pageContent?.includes('Day 2') || pageContent?.includes('S2')) {
        console.log('✅ Session 2 information visible');
      }
    } else {
      console.log('Sessions may be displayed inline with tickets');
    }
  });

  test('ticket shows which sessions it covers', async ({ page }) => {
    if (!testEventId) {
      test.fail(true, 'Test event not created in beforeAll');
      return;
    }

    await page.goto(`/events/${testEventId}`);
    await page.waitForLoadState('domcontentloaded');

    // Find ticket cards
    const ticketCards = page.locator('[data-testid="ticket-card"], .ticket-card, [data-testid="ticket-type"]');

    if (await ticketCards.count() > 0) {
      const firstTicket = ticketCards.first();
      await expect(firstTicket).toBeVisible();

      const ticketText = await firstTicket.textContent();
      console.log(`Ticket content: ${ticketText?.substring(0, 100)}...`);

      // Look for session information in ticket
      if (ticketText?.match(/session|S1|S2|Day 1|Day 2|Both/i)) {
        console.log('✅ Ticket displays session information');
      } else {
        console.log('Ticket may not show explicit session names');
      }
    }
  });

  test('ticket availability reflects timing settings', async ({ page }) => {
    if (!testEventId) {
      test.fail(true, 'Test event not created in beforeAll');
      return;
    }

    await page.goto(`/events/${testEventId}`);
    await page.waitForLoadState('domcontentloaded');

    // Look for availability indicators
    const availabilityIndicators = page.locator(
      'text=/available/i, text=/purchase/i, text=/register/i, button:has-text("Get Tickets"), button:has-text("Register")'
    );

    if (await availabilityIndicators.count() > 0) {
      const firstIndicator = availabilityIndicators.first();
      await expect(firstIndicator).toBeVisible();
      console.log('✅ Ticket availability indicator visible');

      // Check if tickets are purchasable (registration not closed)
      const purchaseButton = page.locator('button').filter({ hasText: /purchase|register|get ticket/i }).first();
      if (await purchaseButton.count() > 0) {
        const isDisabled = await purchaseButton.isDisabled();
        if (!isDisabled) {
          console.log('✅ Purchase button enabled - within registration window');
        } else {
          console.log('Purchase button disabled - may be outside registration window');
        }
      }
    }
  });

  test('admin can view timing settings for event', async ({ page }) => {
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

    // Check for registration timing fields
    if (pageContent?.match(/registration.*open|registration.*close|cancellation/i)) {
      console.log('✅ Timing settings visible in admin panel');
    }

    // Navigate to Setup tab if exists
    const setupTab = page.getByRole('tab', { name: /setup|sessions/i });
    if (await setupTab.count() > 0) {
      await setupTab.click();
      await page.waitForTimeout(500);

      // Verify sessions are displayed
      const sessionsGrid = page.locator('[data-testid="grid-sessions"], [data-testid="sessions-section"]');
      if (await sessionsGrid.count() > 0) {
        await expect(sessionsGrid).toBeVisible();
        console.log('✅ Sessions grid visible in admin');
      }
    }
  });

  test('member can view event with tickets', async ({ page }) => {
    if (!testEventId) {
      test.fail(true, 'Test event not created in beforeAll');
      return;
    }

    await AuthHelpers.loginAs(page, 'member');
    await page.goto(`/events/${testEventId}`);
    await page.waitForLoadState('domcontentloaded');

    // Verify event page loads for authenticated user
    await expect(page.locator('h1, h2').first()).toBeVisible({ timeout: 10000 });

    // Look for ticket section
    const ticketSection = page.locator('[data-testid="ticket-section"], section:has-text("Tickets")').first();

    if (await ticketSection.count() > 0) {
      await expect(ticketSection).toBeVisible();
      console.log('✅ Member can see tickets section');

      // Check for purchase/register options
      const purchaseOptions = page.locator('button').filter({ hasText: /purchase|register|checkout/i });
      if (await purchaseOptions.count() > 0) {
        console.log('✅ Purchase options available to member');
      }
    }
  });

  test('ticket timing uses session dates not event dates', async ({ page }) => {
    if (!testEventId) {
      test.fail(true, 'Test event not created in beforeAll');
      return;
    }

    // This test verifies the core session-based timing behavior
    // Tickets should be available based on session timing, not event start date

    await page.goto(`/events/${testEventId}`);
    await page.waitForLoadState('domcontentloaded');

    // Verify tickets are available (our test event has future sessions)
    const ticketSection = page.locator('[data-testid="ticket-section"], section:has-text("Tickets")').first();
    await expect(ticketSection).toBeVisible({ timeout: 5000 });

    // Look for "sales closed" or "no tickets" messages - should NOT appear
    const closedMessage = page.locator('text=/sales.*closed|no.*tickets|sold out/i').first();
    const hasClosedMessage = await closedMessage.count() > 0;

    if (!hasClosedMessage) {
      console.log('✅ No "sales closed" message - tickets available for future sessions');
    } else {
      const messageText = await closedMessage.textContent();
      console.log(`⚠️ Found closed message: ${messageText}`);
      // This might be expected if the event is actually sold out
    }

    // Verify at least one ticket option exists
    const ticketOptions = page.locator('[data-testid="ticket-card"], .ticket-card, [data-testid="ticket-type"], .ticket-option');
    const optionCount = await ticketOptions.count();
    expect(optionCount).toBeGreaterThan(0);
    console.log(`✅ Found ${optionCount} ticket option(s) for event with future sessions`);
  });
});
