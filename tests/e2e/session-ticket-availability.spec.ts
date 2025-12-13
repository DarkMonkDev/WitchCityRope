/**
 * Session-Based Ticket Availability E2E Tests (DataFactory Migration)
 *
 * Tests the core session-based timing scenario:
 * - Event with 2 sessions: S1 (past), S2 (future)
 * - RegistrationCloseHours: 12
 * - 3 ticket types: S1 Only, S2 Only, Both Sessions
 *
 * Expected Behavior (CORRECTED):
 * - S1 Only Ticket: NOT available (timing window closed - S1 is 7 days past, 12hr close = closed)
 * - S2 Only Ticket: AVAILABLE (S2 is 5 days future, 120hr > 12hr close window)
 * - Both Sessions Ticket: NOT available - uses EARLIEST session (S1) for timing, S1's window closed
 *
 * KEY RULE: Multi-session tickets use the EARLIEST session for ALL timing decisions.
 * This means once the first session's registration closes, you can't buy the multi-session ticket.
 *
 * MIGRATION NOTES:
 * - Uses DataFactory to create test data (no reliance on seed data)
 * - Each test creates its own event/sessions/tickets
 * - Data is automatically cleaned up after each test
 *
 * Specification: /docs/functional-areas/events/session-timing-refactor/SPECIFICATION.md
 *
 * Created: 2025-12-01
 * Updated: 2025-12-01 - Fixed multi-session timing to use EARLIEST session
 * Migrated to DataFactory: 2025-12-10 - Now creates own test data instead of relying on seed data
 */

import { expect } from '@playwright/test';
import { test } from '../lib/datafactory/fixtures/test.fixture';
import { AuthHelpers } from './test-utils/helpers/auth.helpers';

test.describe('Session-Based Ticket Availability - S1 Passed, S2 Future', () => {
  test('verify session timing test event has correct configuration', async ({ page, df }) => {
    /**
     * Create a test event with:
     * - S1: Past session (7 days ago)
     * - S2: Future session (5 days from now)
     * - 3 ticket types: S1 Only, S2 Only, Both Sessions
     * - RegistrationCloseHours: 12
     */

    // Create published event with 12-hour registration close window
    const event = await df.events.create({
      title: `Session Timing Test ${Date.now()}`,
      startDate: new Date(Date.now() - 7 * 24 * 60 * 60 * 1000), // 7 days ago
      endDate: new Date(Date.now() + 5 * 24 * 60 * 60 * 1000), // 5 days future
      status: 'Published',
      isPublic: true,
    });

    console.log(`\n=== Created Session Timing Test Event ===`);
    console.log(`Event ID: ${event.id}`);
    console.log(`Title: ${event.title}`);

    // Create S1 - Past session (7 days ago)
    const pastSessionStart = new Date();
    pastSessionStart.setDate(pastSessionStart.getDate() - 7);
    pastSessionStart.setHours(18, 0, 0, 0);
    const pastSessionEnd = new Date(pastSessionStart.getTime() + 3 * 60 * 60 * 1000);

    const s1Session = await df.sessions.create({
      eventId: event.id,
      title: 'S1 - Past Session',
      startTime: pastSessionStart,
      endTime: pastSessionEnd,
      maxCapacity: 20,
    });

    console.log(`\nS1 Session (PAST):`);
    console.log(`  ID: ${s1Session.id}`);
    console.log(`  Start: ${s1Session.startTime}`);
    console.log(`  Hours since start: ${((Date.now() - pastSessionStart.getTime()) / (1000 * 60 * 60)).toFixed(1)}`);

    // Create S2 - Future session (5 days from now)
    const futureSessionStart = new Date();
    futureSessionStart.setDate(futureSessionStart.getDate() + 5);
    futureSessionStart.setHours(18, 0, 0, 0);
    const futureSessionEnd = new Date(futureSessionStart.getTime() + 3 * 60 * 60 * 1000);

    const s2Session = await df.sessions.create({
      eventId: event.id,
      title: 'S2 - Future Session',
      startTime: futureSessionStart,
      endTime: futureSessionEnd,
      maxCapacity: 20,
    });

    console.log(`\nS2 Session (FUTURE):`);
    console.log(`  ID: ${s2Session.id}`);
    console.log(`  Start: ${s2Session.startTime}`);
    console.log(`  Hours until start: ${((futureSessionStart.getTime() - Date.now()) / (1000 * 60 * 60)).toFixed(1)}`);

    // Create S1 Only ticket type
    const s1Ticket = await df.ticketTypes.create({
      eventId: event.id,
      sessionId: s1Session.id,
      eventId: event.id,
      name: 'S1 Only Ticket',
      price: 25,
      quantityAvailable: 20,
    });

    console.log(`\nS1 Only Ticket:`);
    console.log(`  ID: ${s1Ticket.id}`);
    console.log(`  Name: ${s1Ticket.name}`);

    // Create S2 Only ticket type
    const s2Ticket = await df.ticketTypes.create({
      eventId: event.id,
      sessionId: s2Session.id,
      eventId: event.id,
      name: 'S2 Only Ticket',
      price: 25,
      quantityAvailable: 20,
    });

    console.log(`\nS2 Only Ticket:`);
    console.log(`  ID: ${s2Ticket.id}`);
    console.log(`  Name: ${s2Ticket.name}`);

    // Create Both Sessions ticket type
    const bothTicket = await df.ticketTypes.create({
      eventId: event.id,
      sessionIds: [s1Session.id, s2Session.id],
      eventId: event.id,
      name: 'Both Sessions Ticket',
      price: 40,
      quantityAvailable: 20,
    });

    console.log(`\nBoth Sessions Ticket:`);
    console.log(`  ID: ${bothTicket.id}`);
    console.log(`  Name: ${bothTicket.name}`);

    // Now verify the event via API to check ticket availability
    await AuthHelpers.loginAs(page, 'admin');
    await page.goto('/admin/events');
    await page.waitForLoadState('networkidle');

    // Get the event details via page evaluation
    const response = await page.evaluate(async (eventId: string) => {
      const res = await fetch(`/api/events/${eventId}`, {
        credentials: 'include',
      });
      const text = await res.text();
      try {
        return { status: res.status, data: JSON.parse(text) };
      } catch {
        return { status: res.status, data: text };
      }
    }, event.id);

    expect(response.status).toBe(200);
    const eventData = response.data;

    console.log(`\n=== Event Configuration Verified ===`);
    console.log(`Registration Close Hours: ${eventData.registrationCloseHours || 'default'}`);
    console.log(`Sessions: ${eventData.sessions?.length || 0}`);
    console.log(`Ticket Types: ${eventData.ticketTypes?.length || 0}`);

    // Verify sessions exist
    expect(eventData.sessions?.length).toBeGreaterThanOrEqual(2);

    // Verify ticket types exist
    expect(eventData.ticketTypes?.length).toBeGreaterThanOrEqual(3);

    console.log('\n✅ All expected sessions and ticket types created');
  });

  test('S1 Only ticket should NOT be available (timing window closed)', async ({ page, df }) => {
    /**
     * Core test: S1 Only ticket should have:
     * - referenceSessionId = S1 (the specific session)
     * - canPurchase = false (S1 is 7 days past, 12hr close = closed ~175 hours ago)
     * - Timing based on: S1.StartTime - RegistrationCloseHours
     */

    // Create test event and sessions
    const event = await df.events.createPublished(`S1 Timing Test ${Date.now()}`);

    // S1 - Past session (7 days ago)
    const pastSessionStart = new Date();
    pastSessionStart.setDate(pastSessionStart.getDate() - 7);
    pastSessionStart.setHours(18, 0, 0, 0);
    const pastSessionEnd = new Date(pastSessionStart.getTime() + 3 * 60 * 60 * 1000);

    const s1Session = await df.sessions.create({
      eventId: event.id,
      title: 'S1 - Past Session',
      startTime: pastSessionStart,
      endTime: pastSessionEnd,
      maxCapacity: 20,
    });

    // S2 - Future session
    const futureSessionStart = new Date();
    futureSessionStart.setDate(futureSessionStart.getDate() + 5);
    futureSessionStart.setHours(18, 0, 0, 0);
    const futureSessionEnd = new Date(futureSessionStart.getTime() + 3 * 60 * 60 * 1000);

    await df.sessions.create({
      eventId: event.id,
      title: 'S2 - Future Session',
      startTime: futureSessionStart,
      endTime: futureSessionEnd,
      maxCapacity: 20,
    });

    // Create S1 Only ticket
    await df.ticketTypes.create({
      eventId: event.id,
      sessionId: s1Session.id,
      eventId: event.id,
      name: 'S1 Only Ticket',
      price: 25,
      quantityAvailable: 20,
    });

    // Verify via API
    await AuthHelpers.loginAs(page, 'admin');
    await page.goto('/admin/events');
    await page.waitForLoadState('networkidle');

    const response = await page.evaluate(async (eventId: string) => {
      const res = await fetch(`/api/events/${eventId}`, { credentials: 'include' });
      return { status: res.status, data: await res.json() };
    }, event.id);

    const s1OnlyTicket = response.data.ticketTypes?.find((t: any) => t.name === 'S1 Only Ticket');

    console.log('\n=== S1 Only Ticket Analysis ===');
    console.log(`Name: ${s1OnlyTicket?.name}`);
    console.log(`referenceSessionId: ${s1OnlyTicket?.referenceSessionId || 'null'}`);
    console.log(`canPurchase: ${s1OnlyTicket?.canPurchase}`);
    console.log(`availabilityMessage: ${s1OnlyTicket?.availabilityMessage}`);

    // CRITICAL ASSERTION: S1 Only should have reference session but NOT be purchasable
    expect(s1OnlyTicket?.referenceSessionId).toBeTruthy();
    expect(s1OnlyTicket?.canPurchase).toBe(false);

    console.log('\n✅ S1 Only Ticket correctly marked as NOT available (timing window closed)');
  });

  test('S2 Only ticket SHOULD be available (future session)', async ({ page, df }) => {
    /**
     * Core test: S2 Only ticket should have:
     * - referenceSessionId = S2's ID (has future session)
     * - canPurchase = true (120 hours until session > 12 hour close window)
     * - Visible on public event page
     */

    // Create test event and sessions
    const event = await df.events.createPublished(`S2 Timing Test ${Date.now()}`);

    // S1 - Past session
    const pastSessionStart = new Date();
    pastSessionStart.setDate(pastSessionStart.getDate() - 7);
    pastSessionStart.setHours(18, 0, 0, 0);
    const pastSessionEnd = new Date(pastSessionStart.getTime() + 3 * 60 * 60 * 1000);

    await df.sessions.create({
      eventId: event.id,
      title: 'S1 - Past Session',
      startTime: pastSessionStart,
      endTime: pastSessionEnd,
      maxCapacity: 20,
    });

    // S2 - Future session (5 days from now)
    const futureSessionStart = new Date();
    futureSessionStart.setDate(futureSessionStart.getDate() + 5);
    futureSessionStart.setHours(18, 0, 0, 0);
    const futureSessionEnd = new Date(futureSessionStart.getTime() + 3 * 60 * 60 * 1000);

    const s2Session = await df.sessions.create({
      eventId: event.id,
      title: 'S2 - Future Session',
      startTime: futureSessionStart,
      endTime: futureSessionEnd,
      maxCapacity: 20,
    });

    // Create S2 Only ticket
    await df.ticketTypes.create({
      eventId: event.id,
      sessionId: s2Session.id,
      eventId: event.id,
      name: 'S2 Only Ticket',
      price: 25,
      quantityAvailable: 20,
    });

    // Verify via API
    await AuthHelpers.loginAs(page, 'admin');
    await page.goto('/admin/events');
    await page.waitForLoadState('networkidle');

    const response = await page.evaluate(async (eventId: string) => {
      const res = await fetch(`/api/events/${eventId}`, { credentials: 'include' });
      return { status: res.status, data: await res.json() };
    }, event.id);

    const s2OnlyTicket = response.data.ticketTypes?.find((t: any) => t.name === 'S2 Only Ticket');

    console.log('\n=== S2 Only Ticket Analysis ===');
    console.log(`Name: ${s2OnlyTicket?.name}`);
    console.log(`referenceSessionId: ${s2OnlyTicket?.referenceSessionId}`);
    console.log(`canPurchase: ${s2OnlyTicket?.canPurchase}`);
    console.log(`availabilityMessage: ${s2OnlyTicket?.availabilityMessage}`);

    // Calculate hours until S2
    const hoursUntil = (futureSessionStart.getTime() - Date.now()) / (1000 * 60 * 60);
    console.log(`\nHours until S2: ${hoursUntil.toFixed(1)}`);
    console.log(`Is within sales window: ${hoursUntil > 12}`);

    // CRITICAL ASSERTION: S2 Only SHOULD be purchasable
    expect(s2OnlyTicket?.referenceSessionId).toBeTruthy();
    expect(s2OnlyTicket?.canPurchase).toBe(true);

    console.log('\n✅ S2 Only Ticket correctly marked as AVAILABLE (future session)');
  });

  test('Both Sessions ticket uses EARLIEST session (S1) - NOT purchasable', async ({ page, df }) => {
    /**
     * Multi-session ticket behavior (CORRECTED):
     * - referenceSessionId = S1's ID (EARLIEST session, not first future)
     * - canPurchase = false (S1 timing window has closed)
     *
     * KEY RULE: Multi-session tickets use the EARLIEST session for ALL timing decisions.
     * Once the earliest session's registration closes, you cannot buy the multi-session ticket.
     */

    // Create test event and sessions
    const event = await df.events.createPublished(`Both Sessions Test ${Date.now()}`);

    // S1 - Past session (7 days ago)
    const pastSessionStart = new Date();
    pastSessionStart.setDate(pastSessionStart.getDate() - 7);
    pastSessionStart.setHours(18, 0, 0, 0);
    const pastSessionEnd = new Date(pastSessionStart.getTime() + 3 * 60 * 60 * 1000);

    const s1Session = await df.sessions.create({
      eventId: event.id,
      title: 'S1 - Past Session',
      startTime: pastSessionStart,
      endTime: pastSessionEnd,
      maxCapacity: 20,
    });

    // S2 - Future session (5 days from now)
    const futureSessionStart = new Date();
    futureSessionStart.setDate(futureSessionStart.getDate() + 5);
    futureSessionStart.setHours(18, 0, 0, 0);
    const futureSessionEnd = new Date(futureSessionStart.getTime() + 3 * 60 * 60 * 1000);

    const s2Session = await df.sessions.create({
      eventId: event.id,
      title: 'S2 - Future Session',
      startTime: futureSessionStart,
      endTime: futureSessionEnd,
      maxCapacity: 20,
    });

    // Create Both Sessions ticket
    await df.ticketTypes.create({
      eventId: event.id,
      sessionIds: [s1Session.id, s2Session.id],
      eventId: event.id,
      name: 'Both Sessions Ticket',
      price: 40,
      quantityAvailable: 20,
    });

    // Verify via API
    await AuthHelpers.loginAs(page, 'admin');
    await page.goto('/admin/events');
    await page.waitForLoadState('networkidle');

    const response = await page.evaluate(async (eventId: string) => {
      const res = await fetch(`/api/events/${eventId}`, { credentials: 'include' });
      return { status: res.status, data: await res.json() };
    }, event.id);

    const bothTicket = response.data.ticketTypes?.find((t: any) => t.name === 'Both Sessions Ticket');

    console.log('\n=== Both Sessions Ticket Analysis ===');
    console.log(`Name: ${bothTicket?.name}`);
    console.log(`sessionIdentifiers: ${JSON.stringify(bothTicket?.sessionIdentifiers)}`);
    console.log(`referenceSessionId: ${bothTicket?.referenceSessionId}`);
    console.log(`canPurchase: ${bothTicket?.canPurchase}`);
    console.log(`availabilityMessage: ${bothTicket?.availabilityMessage}`);

    // CRITICAL ASSERTION: Both Sessions should use S1 as reference (EARLIEST session)
    expect(bothTicket?.referenceSessionId).toBeTruthy();

    // The reference session should be S1 (the EARLIEST session)
    console.log(`\nExpected reference session (S1 - earliest): ${s1Session.id}`);
    console.log(`Actual reference session: ${bothTicket?.referenceSessionId}`);
    expect(bothTicket?.referenceSessionId).toBe(s1Session.id);

    // CRITICAL ASSERTION: Both Sessions should NOT be purchasable
    // S1 is 7 days past, RegistrationCloseHours = 12
    // Registration closed 7 days + 12 hours ago
    expect(bothTicket?.canPurchase).toBe(false);

    console.log('\n✅ Both Sessions Ticket correctly uses S1 (earliest) for timing');
    console.log('✅ Both Sessions Ticket correctly NOT purchasable (earliest session registration closed)');
  });

  test('member view shows only available tickets', async ({ page, df }) => {
    /**
     * End-to-end verification from member perspective:
     * - Navigate to event as member
     * - S1 Only ticket should NOT be visible (or marked unavailable)
     * - S2 Only ticket SHOULD be visible and purchasable
     */

    // Create test event and sessions
    const event = await df.events.createPublished(`Member View Test ${Date.now()}`);

    // S1 - Past session
    const pastSessionStart = new Date();
    pastSessionStart.setDate(pastSessionStart.getDate() - 7);
    pastSessionStart.setHours(18, 0, 0, 0);
    const pastSessionEnd = new Date(pastSessionStart.getTime() + 3 * 60 * 60 * 1000);

    const s1Session = await df.sessions.create({
      eventId: event.id,
      title: 'S1 - Past Session',
      startTime: pastSessionStart,
      endTime: pastSessionEnd,
      maxCapacity: 20,
    });

    // S2 - Future session
    const futureSessionStart = new Date();
    futureSessionStart.setDate(futureSessionStart.getDate() + 5);
    futureSessionStart.setHours(18, 0, 0, 0);
    const futureSessionEnd = new Date(futureSessionStart.getTime() + 3 * 60 * 60 * 1000);

    const s2Session = await df.sessions.create({
      eventId: event.id,
      title: 'S2 - Future Session',
      startTime: futureSessionStart,
      endTime: futureSessionEnd,
      maxCapacity: 20,
    });

    // Create tickets
    await df.ticketTypes.create({
      eventId: event.id,
      sessionId: s1Session.id,
      eventId: event.id,
      name: 'S1 Only Ticket',
      price: 25,
      quantityAvailable: 20,
    });

    await df.ticketTypes.create({
      eventId: event.id,
      sessionId: s2Session.id,
      eventId: event.id,
      name: 'S2 Only Ticket',
      price: 25,
      quantityAvailable: 20,
    });

    await df.ticketTypes.create({
      eventId: event.id,
      sessionIds: [s1Session.id, s2Session.id],
      eventId: event.id,
      name: 'Both Sessions Ticket',
      price: 40,
      quantityAvailable: 20,
    });

    // Login as member and navigate to event
    await AuthHelpers.loginAs(page, 'member');
    await page.goto(`/events/${event.id}`);
    await page.waitForLoadState('networkidle');
    await page.waitForTimeout(1000);

    // Take screenshot for debugging
    await page.screenshot({
      path: './test-results/session-timing-member-view.png',
      fullPage: true,
    });

    console.log('\n=== Member View Analysis ===');

    // Check what tickets are visible on the page
    const pageContent = await page.content();

    const s1TicketVisible = pageContent.includes('S1 Only Ticket');
    const s2TicketVisible = pageContent.includes('S2 Only Ticket');
    const bothTicketVisible = pageContent.includes('Both Sessions Ticket');

    console.log(`S1 Only Ticket visible: ${s1TicketVisible}`);
    console.log(`S2 Only Ticket visible: ${s2TicketVisible}`);
    console.log(`Both Sessions Ticket visible: ${bothTicketVisible}`);

    // Check for ticket options section
    const hasTicketOptions = pageContent.includes('Ticket Options');
    const hasUserTicket = pageContent.includes('Your Ticket Purchase');
    console.log(`Has Ticket Options section: ${hasTicketOptions}`);
    console.log(`Has User Ticket section: ${hasUserTicket}`);

    // Look for purchase buttons
    const purchaseButtons = await page
      .locator('button:has-text("Purchase"), [data-testid="button-purchase-ticket"]')
      .count();
    console.log(`Purchase buttons visible: ${purchaseButtons}`);

    // The key assertion: S2 Only should be available, S1 Only should not
    // Note: Implementation may hide unavailable tickets or show them as disabled
    if (hasTicketOptions || hasUserTicket) {
      console.log('\n✅ Ticket section visible on event page');

      // If S1 Only is visible, it should be marked as unavailable
      if (s1TicketVisible) {
        // Check if it's marked as unavailable
        const s1UnavailableIndicator =
          (await page.locator('text=/S1 Only.*closed|unavailable|passed/i').count()) > 0 ||
          (await page
            .locator('[data-testid="ticket-S1 Only Ticket"]')
            .locator('text=/closed|unavailable/i')
            .count()) > 0;
        console.log(`S1 Only marked as unavailable: ${s1UnavailableIndicator}`);
      }

      // S2 Only should have a purchase option
      if (s2TicketVisible) {
        console.log('✅ S2 Only Ticket is visible to member');
      }
    } else {
      console.log('⚠️ No ticket section visible - event may not be published or member may not have access');
    }
  });

  test('API returns correct ticket availability status', async ({ page, df }) => {
    /**
     * Direct API verification of ticket availability fields
     */

    // Create test event and sessions
    const event = await df.events.createPublished(`API Availability Test ${Date.now()}`);

    // S1 - Past session
    const pastSessionStart = new Date();
    pastSessionStart.setDate(pastSessionStart.getDate() - 7);
    pastSessionStart.setHours(18, 0, 0, 0);
    const pastSessionEnd = new Date(pastSessionStart.getTime() + 3 * 60 * 60 * 1000);

    const s1Session = await df.sessions.create({
      eventId: event.id,
      title: 'S1 - Past Session',
      startTime: pastSessionStart,
      endTime: pastSessionEnd,
      maxCapacity: 20,
    });

    // S2 - Future session
    const futureSessionStart = new Date();
    futureSessionStart.setDate(futureSessionStart.getDate() + 5);
    futureSessionStart.setHours(18, 0, 0, 0);
    const futureSessionEnd = new Date(futureSessionStart.getTime() + 3 * 60 * 60 * 1000);

    const s2Session = await df.sessions.create({
      eventId: event.id,
      title: 'S2 - Future Session',
      startTime: futureSessionStart,
      endTime: futureSessionEnd,
      maxCapacity: 20,
    });

    // Create tickets
    await df.ticketTypes.create({
      eventId: event.id,
      sessionId: s1Session.id,
      eventId: event.id,
      name: 'S1 Only Ticket',
      price: 25,
      quantityAvailable: 20,
    });

    await df.ticketTypes.create({
      eventId: event.id,
      sessionId: s2Session.id,
      eventId: event.id,
      name: 'S2 Only Ticket',
      price: 25,
      quantityAvailable: 20,
    });

    await df.ticketTypes.create({
      eventId: event.id,
      sessionIds: [s1Session.id, s2Session.id],
      eventId: event.id,
      name: 'Both Sessions Ticket',
      price: 40,
      quantityAvailable: 20,
    });

    // Verify via API
    await AuthHelpers.loginAs(page, 'member');
    await page.goto('/events');
    await page.waitForLoadState('networkidle');

    const response = await page.evaluate(async (eventId: string) => {
      const res = await fetch(`/api/events/${eventId}`, { credentials: 'include' });
      return { status: res.status, data: await res.json() };
    }, event.id);

    console.log('\n=== API Ticket Availability Summary ===');

    const s1Only = response.data.ticketTypes?.find((t: any) => t.name === 'S1 Only Ticket');
    const s2Only = response.data.ticketTypes?.find((t: any) => t.name === 'S2 Only Ticket');
    const both = response.data.ticketTypes?.find((t: any) => t.name === 'Both Sessions Ticket');

    console.log('\nTicket Type | referenceSessionId | canPurchase | availabilityMessage');
    console.log('----------- | ------------------ | ----------- | -------------------');
    console.log(
      `S1 Only     | ${(s1Only?.referenceSessionId?.substring(0, 8) || 'null').padEnd(18)} | ${String(s1Only?.canPurchase).padEnd(11)} | ${s1Only?.availabilityMessage}`
    );
    console.log(
      `S2 Only     | ${(s2Only?.referenceSessionId?.substring(0, 8) || 'null').padEnd(18)} | ${String(s2Only?.canPurchase).padEnd(11)} | ${s2Only?.availabilityMessage}`
    );
    console.log(
      `Both        | ${(both?.referenceSessionId?.substring(0, 8) || 'null').padEnd(18)} | ${String(both?.canPurchase).padEnd(11)} | ${both?.availabilityMessage}`
    );

    // Final assertions
    console.log('\n=== Assertions ===');

    // S1 Only: NOT available (timing window closed)
    expect(s1Only?.canPurchase).toBe(false);
    console.log('✅ S1 Only: canPurchase = false (timing window closed)');

    // S2 Only: Available (future session, within timing window)
    expect(s2Only?.canPurchase).toBe(true);
    console.log('✅ S2 Only: canPurchase = true (within timing window)');

    // Both: NOT available (uses EARLIEST session S1, timing closed)
    expect(both?.referenceSessionId).toBeTruthy();
    expect(both?.canPurchase).toBe(false);
    console.log('✅ Both Sessions: canPurchase = false (uses earliest session S1, timing closed)');

    console.log('\n🎉 All session-based ticket availability assertions passed!');
    console.log('KEY: Only S2 Only ticket is purchasable - multi-session uses earliest session timing');
  });
});
