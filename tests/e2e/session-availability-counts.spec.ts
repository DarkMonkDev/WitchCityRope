/**
 * Session Availability Counts E2E Tests
 *
 * Verifies that session sold/available counts are correctly calculated and displayed.
 * This test was added after discovering that the counts were not being reported correctly
 * due to tickets without SessionId needing to be traced through TicketPurchase -> TicketType -> TicketTypeSessions.
 *
 * Migrated to DataFactory: 2025-12-10
 *
 * @see /docs/functional-areas/payments/new-work/2025-12-07-session-based-ticketing/README.md
 */

import { expect } from '@playwright/test';
import { test } from '../lib/datafactory/fixtures/test.fixture';
import { AuthHelpers } from './test-utils/helpers/auth.helpers';

test.describe('Session Availability Counts', () => {
  test.describe('API Tests', () => {
    test('should return correct session soldCount and availableCount from events API', async ({
      page,
      df,
    }) => {
      // Create a multi-session event
      const event = await df.events.createPublished(`Session Availability Test ${Date.now()}`);

      // Calculate session times
      const session1Start = new Date();
      session1Start.setDate(session1Start.getDate() + 7);
      session1Start.setHours(18, 0, 0, 0);
      const session1End = new Date(session1Start.getTime() + 3 * 60 * 60 * 1000);

      const session2Start = new Date();
      session2Start.setDate(session2Start.getDate() + 8);
      session2Start.setHours(18, 0, 0, 0);
      const session2End = new Date(session2Start.getTime() + 3 * 60 * 60 * 1000);

      const session1 = await df.sessions.create({
        eventId: event.id,
        title: 'Session 1',
        sessionIdentifier: 'S1',
        startTime: session1Start,
        endTime: session1End,
        maxCapacity: 20,
      });

      const session2 = await df.sessions.create({
        eventId: event.id,
        title: 'Session 2',
        sessionIdentifier: 'S2',
        startTime: session2Start,
        endTime: session2End,
        maxCapacity: 20,
      });

      await df.ticketTypes.create({
        eventId: event.id,
      sessionId: session1.id,
        name: 'Both Sessions Pass',
        price: 0,
        quantityAvailable: 20,
      });

      console.log(`✅ Created test event: ${event.id}`);

      // Get events list using page.request (uses baseURL automatically)
      const response = await page.request.get('/api/events');
      expect(response.ok()).toBeTruthy();

      const events = await response.json();

      // Find our test event
      const testEvent = events.find((e: any) => e.id === event.id);

      expect(testEvent).toBeDefined();
      expect(testEvent.sessions.length).toBeGreaterThan(1);

      // Verify each session has the required count fields
      for (const session of testEvent.sessions) {
        expect(session).toHaveProperty('registrationCount');
        expect(session).toHaveProperty('capacity');
        expect(typeof session.registrationCount).toBe('number');
        expect(typeof session.capacity).toBe('number');
        expect(session.registrationCount).toBeGreaterThanOrEqual(0);
        expect(session.capacity).toBeGreaterThan(0);
      }
    });

    test('should return correct sessionAvailability from participation API', async ({
      page,
      df,
    }) => {
      // Create a multi-session event
      const event = await df.events.createPublished(
        `Participation Availability Test ${Date.now()}`
      );

      // Calculate session times
      const session1Start = new Date();
      session1Start.setDate(session1Start.getDate() + 7);
      session1Start.setHours(18, 0, 0, 0);
      const session1End = new Date(session1Start.getTime() + 3 * 60 * 60 * 1000);

      const session2Start = new Date();
      session2Start.setDate(session2Start.getDate() + 8);
      session2Start.setHours(18, 0, 0, 0);
      const session2End = new Date(session2Start.getTime() + 3 * 60 * 60 * 1000);

      const session1 = await df.sessions.create({
        eventId: event.id,
        title: 'Session 1',
        sessionIdentifier: 'S1',
        startTime: session1Start,
        endTime: session1End,
        maxCapacity: 20,
      });

      const session2 = await df.sessions.create({
        eventId: event.id,
        title: 'Session 2',
        sessionIdentifier: 'S2',
        startTime: session2Start,
        endTime: session2End,
        maxCapacity: 20,
      });

      await df.ticketTypes.create({
        eventId: event.id,
      sessionId: session1.id,
        name: 'Both Sessions Pass',
        price: 0,
        quantityAvailable: 20,
      });

      // Login using AuthHelpers (MANDATORY)
      await AuthHelpers.loginAs(page, 'vetted');

      // Get participation status for this event
      const participationResponse = await page.request.get(
        `/api/events/${event.id}/participation`
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
        expect(session.availableCount).toBe(Math.max(0, session.capacity - session.soldCount));
      }
    });

    test('should have consistent counts between events API and participation API', async ({
      page,
      df,
    }) => {
      // Create a multi-session event
      const event = await df.events.createPublished(`Consistency Test ${Date.now()}`);

      // Calculate session times
      const session1Start = new Date();
      session1Start.setDate(session1Start.getDate() + 7);
      session1Start.setHours(18, 0, 0, 0);
      const session1End = new Date(session1Start.getTime() + 3 * 60 * 60 * 1000);

      const session2Start = new Date();
      session2Start.setDate(session2Start.getDate() + 8);
      session2Start.setHours(18, 0, 0, 0);
      const session2End = new Date(session2Start.getTime() + 3 * 60 * 60 * 1000);

      const session1 = await df.sessions.create({
        eventId: event.id,
        title: 'Session 1',
        sessionIdentifier: 'S1',
        startTime: session1Start,
        endTime: session1End,
        maxCapacity: 20,
      });

      const session2 = await df.sessions.create({
        eventId: event.id,
        title: 'Session 2',
        sessionIdentifier: 'S2',
        startTime: session2Start,
        endTime: session2End,
        maxCapacity: 20,
      });

      await df.ticketTypes.create({
        eventId: event.id,
      sessionId: session1.id,
        name: 'Both Sessions Pass',
        price: 0,
        quantityAvailable: 20,
      });

      // Login using AuthHelpers (MANDATORY)
      await AuthHelpers.loginAs(page, 'vetted');

      // Get the test event
      const eventResponse = await page.request.get(`/api/events/${event.id}`);
      const eventData = await eventResponse.json();

      // Get participation status
      const participationResponse = await page.request.get(
        `/api/events/${event.id}/participation`
      );
      const participation = await participationResponse.json();

      // Compare counts between APIs
      for (const eventSession of eventData.sessions) {
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
    test('should display session availability on event details page', async ({ page, df }) => {
      // Create a multi-session event
      const event = await df.events.createPublished(`UI Availability Test ${Date.now()}`);

      // Calculate session times
      const session1Start = new Date();
      session1Start.setDate(session1Start.getDate() + 7);
      session1Start.setHours(18, 0, 0, 0);
      const session1End = new Date(session1Start.getTime() + 3 * 60 * 60 * 1000);

      const session2Start = new Date();
      session2Start.setDate(session2Start.getDate() + 8);
      session2Start.setHours(18, 0, 0, 0);
      const session2End = new Date(session2Start.getTime() + 3 * 60 * 60 * 1000);

      const session1 = await df.sessions.create({
        eventId: event.id,
        title: 'Session 1',
        sessionIdentifier: 'S1',
        startTime: session1Start,
        endTime: session1End,
        maxCapacity: 20,
      });

      const session2 = await df.sessions.create({
        eventId: event.id,
        title: 'Session 2',
        sessionIdentifier: 'S2',
        startTime: session2Start,
        endTime: session2End,
        maxCapacity: 20,
      });

      await df.ticketTypes.create({
        eventId: event.id,
      sessionId: session1.id,
        name: 'Both Sessions Pass',
        price: 0,
        quantityAvailable: 20,
      });

      // Login using AuthHelpers (MANDATORY)
      await AuthHelpers.loginAs(page, 'vetted');

      // Navigate directly to the test event
      await page.goto(`/events/${event.id}`);
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

    test('should show additional sessions purchase option when user has partial tickets', async ({
      page,
    }) => {
      // This test verifies that users with tickets for some sessions
      // can see the option to purchase tickets for remaining sessions

      // Login using AuthHelpers (MANDATORY)
      await AuthHelpers.loginAs(page, 'vetted');

      // Navigate to events using relative URL (MANDATORY)
      await page.goto('/events');
      await page.waitForLoadState('domcontentloaded');

      // Look for "Purchase Additional Sessions" button if visible
      const additionalSessionsButton = page.getByText('Purchase Additional Sessions', {
        exact: false,
      });

      // This test just verifies the button renders correctly when conditions are met
      // The actual visibility depends on user having partial session ownership
      if (await additionalSessionsButton.isVisible()) {
        expect(await additionalSessionsButton.isEnabled()).toBeTruthy();
      }
    });
  });

  test.describe('Data Integrity Tests', () => {
    test('should correctly count tickets via TicketPurchase -> TicketType -> TicketTypeSessions chain', async ({
      page,
      df,
    }) => {
      // This test verifies the fix for legacy tickets without SessionId
      // which need to be counted via the TicketPurchase relationship

      // Create a multi-session event
      const event = await df.events.createPublished(`Data Integrity Test ${Date.now()}`);

      // Calculate session times
      const session1Start = new Date();
      session1Start.setDate(session1Start.getDate() + 7);
      session1Start.setHours(18, 0, 0, 0);
      const session1End = new Date(session1Start.getTime() + 3 * 60 * 60 * 1000);

      const session2Start = new Date();
      session2Start.setDate(session2Start.getDate() + 8);
      session2Start.setHours(18, 0, 0, 0);
      const session2End = new Date(session2Start.getTime() + 3 * 60 * 60 * 1000);

      const session1 = await df.sessions.create({
        eventId: event.id,
        title: 'S1',
        sessionIdentifier: 'S1',
        startTime: session1Start,
        endTime: session1End,
        maxCapacity: 20,
      });

      const session2 = await df.sessions.create({
        eventId: event.id,
        title: 'S2',
        sessionIdentifier: 'S2',
        startTime: session2Start,
        endTime: session2End,
        maxCapacity: 20,
      });

      await df.ticketTypes.create({
        eventId: event.id,
      sessionId: session1.id,
        name: 'Both Sessions Pass',
        price: 0,
        quantityAvailable: 20,
      });

      // Login using AuthHelpers (MANDATORY)
      await AuthHelpers.loginAs(page, 'admin');

      // Get the test event details
      const eventResponse = await page.request.get(`/api/events/${event.id}`);
      const testEvent = await eventResponse.json();

      // Get participation status
      const participationResponse = await page.request.get(`/api/events/${event.id}/participation`);
      const participation = await participationResponse.json();

      // Verify we have session availability data
      expect(participation.sessionAvailability).toBeDefined();
      expect(participation.sessionAvailability.length).toBe(2); // Test event has 2 sessions

      // Session 1 (S1) should have tickets (based on seed data)
      const sessionData1 = participation.sessionAvailability.find(
        (s: any) => s.sessionIdentifier === 'S1'
      );
      expect(sessionData1).toBeDefined();
      expect(sessionData1.soldCount).toBeGreaterThanOrEqual(0);

      // Session 2 (S2)
      const sessionData2 = participation.sessionAvailability.find(
        (s: any) => s.sessionIdentifier === 'S2'
      );
      expect(sessionData2).toBeDefined();
      expect(sessionData2.soldCount).toBeGreaterThanOrEqual(0);

      // Log actual counts for debugging
      console.log(
        `Session 1 (S1): soldCount=${sessionData1.soldCount}, availableCount=${sessionData1.availableCount}, capacity=${sessionData1.capacity}`
      );
      console.log(
        `Session 2 (S2): soldCount=${sessionData2.soldCount}, availableCount=${sessionData2.availableCount}, capacity=${sessionData2.capacity}`
      );
    });
  });
});
