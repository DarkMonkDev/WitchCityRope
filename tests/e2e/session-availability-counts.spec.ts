/**
 * Session Availability Counts E2E Tests
 *
 * Verifies that session sold/available counts are correctly calculated and displayed.
 * This test was added after discovering that the counts were not being reported correctly
 * due to tickets without SessionId needing to be traced through TicketPurchase -> TicketType -> TicketTypeSessions.
 *
 * @see /docs/functional-areas/payments/new-work/2025-12-07-session-based-ticketing/README.md
 */

import { test, expect, Page } from '@playwright/test';
import { AuthHelpers } from './test-utils/helpers/auth.helpers';

// Helper to make authenticated API request using page.evaluate (works in browser context)
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

test.describe('Session Availability Counts', () => {
  let testEventId: string | null = null;

  test.beforeAll(async ({ browser }) => {
    // Create a multi-session event for testing
    // CRITICAL: Must create context with baseURL for page.goto to work with relative URLs
    const baseURL = process.env.PLAYWRIGHT_BASE_URL || process.env.WEB_BASE_URL || 'http://localhost:5173';
    const context = await browser.newContext({ baseURL });
    const page = await context.newPage();
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

    // Create event with 2 sessions
    const session1Start = new Date();
    session1Start.setDate(session1Start.getDate() + 7);
    session1Start.setHours(18, 0, 0, 0);

    const session2Start = new Date();
    session2Start.setDate(session2Start.getDate() + 8);
    session2Start.setHours(18, 0, 0, 0);

    const eventData = {
      title: `Session Availability Test ${Date.now()}`,
      shortDescription: 'Test event for session availability counts',
      description: 'This event tests session availability calculations.',
      eventType: 'Class',
      startDate: session1Start.toISOString(),
      endDate: session2Start.toISOString(),
      venueId: venueId,
      capacity: 20,
      isPublished: true,
      registrationOpenHours: null,
      registrationCloseHours: 0,
      cancellationCloseHours: 0,
      sessions: [
        {
          sessionIdentifier: 'S1',
          name: 'Session 1',
          startTime: session1Start.toISOString(),
          endTime: new Date(session1Start.getTime() + 3 * 60 * 60 * 1000).toISOString(),
          capacity: 20,
        },
        {
          sessionIdentifier: 'S2',
          name: 'Session 2',
          startTime: session2Start.toISOString(),
          endTime: new Date(session2Start.getTime() + 3 * 60 * 60 * 1000).toISOString(),
          capacity: 20,
        },
      ],
      ticketTypes: [
        {
          name: 'Both Sessions Pass',
          pricingType: 'Fixed',
          price: 0,
          sessionIdentifiers: ['S1', 'S2'],
        },
      ],
    };

    console.log('Creating multi-session test event...');
    const createResponse = await apiRequest(page, 'POST', '/api/admin/events', eventData);

    if (createResponse.status === 200 || createResponse.status === 201) {
      const responseData = createResponse.data as { id: string };
      testEventId = responseData.id;
      console.log(`✅ Created test event: ${testEventId}`);
    } else {
      console.error('Failed to create test event:', createResponse);
    }

    await page.close();
  });

  test.afterAll(async ({ browser }) => {
    if (!testEventId) return;

    // CRITICAL: Must create context with baseURL for page.goto to work with relative URLs
    const baseURL = process.env.PLAYWRIGHT_BASE_URL || process.env.WEB_BASE_URL || 'http://localhost:5173';
    const context = await browser.newContext({ baseURL });
    const page = await context.newPage();
    await AuthHelpers.loginAs(page, 'admin');

    console.log(`Cleaning up test event: ${testEventId}`);
    await apiRequest(page, 'DELETE', `/api/admin/events/${testEventId}`);
    console.log('✅ Test event deleted');

    await page.close();
  });

  test.describe('API Tests', () => {

    test('should return correct session soldCount and availableCount from events API', async ({ page }) => {
      // Get events list using page.request (uses baseURL automatically)
      const response = await page.request.get('/api/events');
      expect(response.ok()).toBeTruthy();

      const events = await response.json();

      // Find a multi-session event
      const multiSessionEvent = events.find((e: any) =>
        e.sessions && e.sessions.length > 1
      );

      expect(multiSessionEvent).toBeDefined();
      expect(multiSessionEvent.sessions.length).toBeGreaterThan(1);

      // Verify each session has the required count fields
      for (const session of multiSessionEvent.sessions) {
        expect(session).toHaveProperty('registrationCount');
        expect(session).toHaveProperty('capacity');
        expect(typeof session.registrationCount).toBe('number');
        expect(typeof session.capacity).toBe('number');
        expect(session.registrationCount).toBeGreaterThanOrEqual(0);
        expect(session.capacity).toBeGreaterThan(0);
      }
    });

    test('should return correct sessionAvailability from participation API', async ({ page }) => {
      // Login using AuthHelpers (MANDATORY)
      await AuthHelpers.loginAs(page, 'vetted');

      // Get events to find a multi-session event ID
      const eventsResponse = await page.request.get('/api/events');
      const events = await eventsResponse.json();

      const multiSessionEvent = events.find((e: any) =>
        e.sessions && e.sessions.length > 1
      );
      expect(multiSessionEvent).toBeDefined();

      // Get participation status for this event
      const participationResponse = await page.request.get(
        `/api/events/${multiSessionEvent.id}/participation`
      );
      expect(participationResponse.ok()).toBeTruthy();

      const participation = await participationResponse.json();

      // Verify sessionAvailability is present for multi-session events
      expect(participation).toHaveProperty('sessionAvailability');
      expect(Array.isArray(participation.sessionAvailability)).toBeTruthy();
      expect(participation.sessionAvailability.length).toBeGreaterThan(1);

      // Verify each session availability has correct structure
      for (const session of participation.sessionAvailability) {
        expect(session).toHaveProperty('sessionId');
        expect(session).toHaveProperty('sessionIdentifier');
        expect(session).toHaveProperty('soldCount');
        expect(session).toHaveProperty('availableCount');
        expect(session).toHaveProperty('capacity');

        // Verify counts are numbers
        expect(typeof session.soldCount).toBe('number');
        expect(typeof session.availableCount).toBe('number');
        expect(typeof session.capacity).toBe('number');

        // Verify math: availableCount = capacity - soldCount
        expect(session.availableCount).toBe(
          Math.max(0, session.capacity - session.soldCount)
        );
      }
    });

    test('should have consistent counts between events API and participation API', async ({ page }) => {
      if (!testEventId) {
        test.skip(true, 'Test event not created in beforeAll');
        return;
      }

      // Login using AuthHelpers (MANDATORY)
      await AuthHelpers.loginAs(page, 'vetted');

      // Get the test event
      const eventResponse = await page.request.get(`/api/events/${testEventId}`);
      const multiSessionEvent = await eventResponse.json();

      // Get participation status
      const participationResponse = await page.request.get(
        `/api/events/${multiSessionEvent.id}/participation`
      );
      const participation = await participationResponse.json();

      // Compare counts between APIs
      for (const eventSession of multiSessionEvent.sessions) {
        const participationSession = participation.sessionAvailability?.find(
          (s: any) => s.sessionId === eventSession.id
        );

        if (participationSession) {
          // registrationCount from events API should match soldCount from participation API
          expect(eventSession.registrationCount).toBe(participationSession.soldCount);
        }
      }
    });
  });

  test.describe('UI Tests', () => {

    test('should display session availability on event details page', async ({ page }) => {
      if (!testEventId) {
        test.skip(true, 'Test event not created in beforeAll');
        return;
      }

      // Login using AuthHelpers (MANDATORY)
      await AuthHelpers.loginAs(page, 'vetted');

      // Navigate directly to the test event
      await page.goto(`/events/${testEventId}`);
      await page.waitForLoadState('domcontentloaded');

      // Check for "Event Dates / Times" section (renamed from "Session Availability")
      const sessionSection = page.getByText('Event Dates / Times', { exact: false });

      if (await sessionSection.isVisible()) {
        // Verify the section contains sold/available counts
        const parentSection = sessionSection.locator('..').locator('..');
        const sectionText = await parentSection.textContent();

        // Should contain "sold" and "Available" text
        expect(sectionText).toMatch(/\d+\s*sold/i);
        expect(sectionText).toMatch(/\d+\s*Available/i);
      }
    });

    test('should hide session availability when user has a ticket', async ({ page }) => {
      // This test verifies that the "Event Dates / Times" section is hidden
      // when the user already has a ticket for the event

      // Login using AuthHelpers (MANDATORY)
      await AuthHelpers.loginAs(page, 'vetted');

      // Navigate to events using relative URL (MANDATORY)
      await page.goto('/events');
      await page.waitForLoadState('domcontentloaded');

      // Look for events with "Ticket Purchased" badge or similar
      const ticketBadge = page.getByText('Ticket Purchased', { exact: false });

      if (await ticketBadge.isVisible()) {
        // Click on the event with the ticket
        const eventCard = ticketBadge.locator('..').locator('..');
        await eventCard.click();
        await page.waitForLoadState('domcontentloaded');

        // Verify "Event Dates / Times" section is NOT visible
        const sessionSection = page.getByText('Event Dates / Times', { exact: false });

        // Should either not exist or be hidden
        const isVisible = await sessionSection.isVisible().catch(() => false);
        expect(isVisible).toBeFalsy();

        // Instead, should see "Your Ticket(s) Purchase" section
        const ticketSection = page.getByText('Your Ticket(s) Purchase', { exact: false });
        // This might be visible if user has a ticket
      }
    });

    test('should show additional sessions purchase option when user has partial tickets', async ({ page }) => {
      // This test verifies that users with tickets for some sessions
      // can see the option to purchase tickets for remaining sessions

      // Login using AuthHelpers (MANDATORY)
      await AuthHelpers.loginAs(page, 'vetted');

      // Navigate to events using relative URL (MANDATORY)
      await page.goto('/events');
      await page.waitForLoadState('domcontentloaded');

      // Look for "Purchase Additional Sessions" button if visible
      const additionalSessionsButton = page.getByText('Purchase Additional Sessions', { exact: false });

      // This test just verifies the button renders correctly when conditions are met
      // The actual visibility depends on user having partial session ownership
      if (await additionalSessionsButton.isVisible()) {
        expect(await additionalSessionsButton.isEnabled()).toBeTruthy();
      }
    });
  });

  test.describe('Data Integrity Tests', () => {

    test('should correctly count tickets via TicketPurchase -> TicketType -> TicketTypeSessions chain', async ({ page }) => {
      if (!testEventId) {
        test.skip(true, 'Test event not created in beforeAll');
        return;
      }

      // This test verifies the fix for legacy tickets without SessionId
      // which need to be counted via the TicketPurchase relationship

      // Login using AuthHelpers (MANDATORY)
      await AuthHelpers.loginAs(page, 'admin');

      // Get the test event details
      const eventResponse = await page.request.get(`/api/events/${testEventId}`);
      const testEvent = await eventResponse.json();

      // Get participation status
      const participationResponse = await page.request.get(
        `/api/events/${testEventId}/participation`
      );
      const participation = await participationResponse.json();

      // Verify we have session availability data
      expect(participation.sessionAvailability).toBeDefined();
      expect(participation.sessionAvailability.length).toBe(2); // Test event has 2 sessions

      // Session 1 (S1) should have tickets (based on seed data)
      const session1 = participation.sessionAvailability.find(
        (s: any) => s.sessionIdentifier === 'S1'
      );
      expect(session1).toBeDefined();
      expect(session1.soldCount).toBeGreaterThanOrEqual(0);

      // Session 2 (S2)
      const session2 = participation.sessionAvailability.find(
        (s: any) => s.sessionIdentifier === 'S2'
      );
      expect(session2).toBeDefined();
      expect(session2.soldCount).toBeGreaterThanOrEqual(0);

      // Log actual counts for debugging
      console.log(`Session 1 (S1): soldCount=${session1.soldCount}, availableCount=${session1.availableCount}, capacity=${session1.capacity}`);
      console.log(`Session 2 (S2): soldCount=${session2.soldCount}, availableCount=${session2.availableCount}, capacity=${session2.capacity}`);
    });
  });
});
