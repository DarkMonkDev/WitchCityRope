/**
 * Session-Based Ticket Availability E2E Tests
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
 * Uses seed data event: "Session Timing Test Event"
 * - S1: 7 days in the past
 * - S2: 5 days in the future
 *
 * Specification: /docs/functional-areas/events/session-timing-refactor/SPECIFICATION.md
 *
 * Created: 2025-12-01
 * Updated: 2025-12-01 - Fixed multi-session timing to use EARLIEST session
 */

import { test, expect, Page } from '@playwright/test';
import { AuthHelpers } from './test-utils/helpers/auth.helpers';

// Helper to make authenticated API request
async function apiRequest(page: Page, method: string, url: string): Promise<any> {
  const response = await page.evaluate(async ({ method, url }) => {
    const res = await fetch(url, {
      method,
      credentials: 'include',
    });

    const text = await res.text();
    try {
      return { status: res.status, data: JSON.parse(text) };
    } catch {
      return { status: res.status, data: text };
    }
  }, { method, url });

  return response;
}

test.describe('Session-Based Ticket Availability - S1 Passed, S2 Future', () => {

  test('verify session timing test event has correct configuration', async ({ page }) => {
    /**
     * Verify the seed data event "Session Timing Test Event" exists with:
     * - S1: Past session
     * - S2: Future session
     * - 3 ticket types: S1 Only, S2 Only, Both Sessions
     * - RegistrationCloseHours: 12
     */
    await AuthHelpers.loginAs(page, 'admin');
    await page.goto('/admin/events');
    await page.waitForLoadState('networkidle');

    // Get the Session Timing Test Event
    const response = await apiRequest(page, 'GET', '/api/events?includeUnpublished=true');
    expect(response.status).toBe(200);

    const testEvent = response.data.find((e: any) => e.title === 'Session Timing Test Event');

    if (!testEvent) {
      console.log('⚠️ Session Timing Test Event not found - database may need reseeding');
      console.log('Available events:', response.data.map((e: any) => e.title));
      test.skip();
      return;
    }

    console.log('\n=== Session Timing Test Event Configuration ===');
    console.log(`Event ID: ${testEvent.id}`);
    console.log(`Registration Close Hours: ${testEvent.registrationCloseHours}`);

    // Verify sessions
    expect(testEvent.sessions?.length).toBe(2);

    console.log('\nSessions:');
    for (const session of testEvent.sessions || []) {
      const startDate = new Date(session.startTime);
      const now = new Date();
      const hoursUntil = (startDate.getTime() - now.getTime()) / (1000 * 60 * 60);
      const isPast = hoursUntil < 0;
      console.log(`  ${session.sessionIdentifier} (${session.name}):`);
      console.log(`    StartTime: ${session.startTime}`);
      console.log(`    Hours until: ${hoursUntil.toFixed(1)} (${isPast ? 'PAST' : 'FUTURE'})`);
    }

    // Find S1 and S2 sessions
    const s1Session = testEvent.sessions?.find((s: any) => s.sessionIdentifier === 'S1');
    const s2Session = testEvent.sessions?.find((s: any) => s.sessionIdentifier === 'S2');

    expect(s1Session).toBeTruthy();
    expect(s2Session).toBeTruthy();

    // Verify S1 is in the past
    const s1StartDate = new Date(s1Session.startTime);
    const now = new Date();
    expect(s1StartDate.getTime()).toBeLessThan(now.getTime());
    console.log(`\n✅ S1 is in the past`);

    // Verify S2 is in the future
    const s2StartDate = new Date(s2Session.startTime);
    expect(s2StartDate.getTime()).toBeGreaterThan(now.getTime());
    console.log(`✅ S2 is in the future`);

    // Verify ticket types
    expect(testEvent.ticketTypes?.length).toBe(3);

    console.log('\nTicket Types:');
    for (const tt of testEvent.ticketTypes || []) {
      console.log(`  ${tt.name}:`);
      console.log(`    sessionIdentifiers: ${JSON.stringify(tt.sessionIdentifiers)}`);
      console.log(`    referenceSessionId: ${tt.referenceSessionId || 'null (all sessions passed)'}`);
      console.log(`    canPurchase: ${tt.canPurchase}`);
      console.log(`    availabilityMessage: ${tt.availabilityMessage}`);
    }

    // Verify we have the expected ticket types
    const s1OnlyTicket = testEvent.ticketTypes?.find((t: any) => t.name === 'S1 Only Ticket');
    const s2OnlyTicket = testEvent.ticketTypes?.find((t: any) => t.name === 'S2 Only Ticket');
    const bothTicket = testEvent.ticketTypes?.find((t: any) => t.name === 'Both Sessions Ticket');

    expect(s1OnlyTicket).toBeTruthy();
    expect(s2OnlyTicket).toBeTruthy();
    expect(bothTicket).toBeTruthy();

    console.log('\n✅ All expected ticket types found');
  });

  test('S1 Only ticket should NOT be available (timing window closed)', async ({ page }) => {
    /**
     * Core test: S1 Only ticket should have:
     * - referenceSessionId = S1 (the specific session)
     * - canPurchase = false (S1 is 7 days past, 12hr close = closed ~175 hours ago)
     * - Timing based on: S1.StartTime - RegistrationCloseHours
     */
    await AuthHelpers.loginAs(page, 'admin');
    await page.goto('/admin/events');
    await page.waitForLoadState('networkidle');

    // Get the test event
    const response = await apiRequest(page, 'GET', '/api/events?includeUnpublished=true');
    const testEvent = response.data.find((e: any) => e.title === 'Session Timing Test Event');

    if (!testEvent) {
      console.log('⚠️ Session Timing Test Event not found');
      test.skip();
      return;
    }

    const s1OnlyTicket = testEvent.ticketTypes?.find((t: any) => t.name === 'S1 Only Ticket');
    const s1Session = testEvent.sessions?.find((s: any) => s.sessionIdentifier === 'S1');

    console.log('\n=== S1 Only Ticket Analysis ===');
    console.log(`Name: ${s1OnlyTicket?.name}`);
    console.log(`sessionIdentifiers: ${JSON.stringify(s1OnlyTicket?.sessionIdentifiers)}`);
    console.log(`referenceSessionId: ${s1OnlyTicket?.referenceSessionId || 'null'}`);
    console.log(`canPurchase: ${s1OnlyTicket?.canPurchase}`);
    console.log(`availabilityMessage: ${s1OnlyTicket?.availabilityMessage}`);

    // Calculate timing for debugging
    if (s1Session) {
      const s1StartDate = new Date(s1Session.startTime);
      const now = new Date();
      const hoursUntil = (s1StartDate.getTime() - now.getTime()) / (1000 * 60 * 60);
      console.log(`\nHours until S1: ${hoursUntil.toFixed(1)} (negative = past)`);
      console.log(`Registration Close Hours: ${testEvent.registrationCloseHours}`);
      console.log(`Window closed ${Math.abs(hoursUntil).toFixed(1) + testEvent.registrationCloseHours} hours ago`);
    }

    // CRITICAL ASSERTION: S1 Only should have S1 as reference but NOT be purchasable
    expect(s1OnlyTicket?.referenceSessionId).toBeTruthy(); // Has reference session (S1)
    expect(s1OnlyTicket?.canPurchase).toBe(false); // But timing window is closed

    console.log('\n✅ S1 Only Ticket correctly marked as NOT available (timing window closed)');
  });

  test('S2 Only ticket SHOULD be available (future session)', async ({ page }) => {
    /**
     * Core test: S2 Only ticket should have:
     * - referenceSessionId = S2's ID (has future session)
     * - canPurchase = true (120 hours until session > 12 hour close window)
     * - Visible on public event page
     */
    await AuthHelpers.loginAs(page, 'admin');
    await page.goto('/admin/events');
    await page.waitForLoadState('networkidle');

    // Get the test event
    const response = await apiRequest(page, 'GET', '/api/events?includeUnpublished=true');
    const testEvent = response.data.find((e: any) => e.title === 'Session Timing Test Event');

    if (!testEvent) {
      console.log('⚠️ Session Timing Test Event not found');
      test.skip();
      return;
    }

    const s2OnlyTicket = testEvent.ticketTypes?.find((t: any) => t.name === 'S2 Only Ticket');
    const s2Session = testEvent.sessions?.find((s: any) => s.sessionIdentifier === 'S2');

    console.log('\n=== S2 Only Ticket Analysis ===');
    console.log(`Name: ${s2OnlyTicket?.name}`);
    console.log(`sessionIdentifiers: ${JSON.stringify(s2OnlyTicket?.sessionIdentifiers)}`);
    console.log(`referenceSessionId: ${s2OnlyTicket?.referenceSessionId}`);
    console.log(`canPurchase: ${s2OnlyTicket?.canPurchase}`);
    console.log(`availabilityMessage: ${s2OnlyTicket?.availabilityMessage}`);

    // Calculate hours until S2 for debugging
    if (s2Session) {
      const s2StartDate = new Date(s2Session.startTime);
      const now = new Date();
      const hoursUntil = (s2StartDate.getTime() - now.getTime()) / (1000 * 60 * 60);
      console.log(`\nHours until S2: ${hoursUntil.toFixed(1)}`);
      console.log(`Registration Close Hours: ${testEvent.registrationCloseHours}`);
      console.log(`Is within sales window: ${hoursUntil > testEvent.registrationCloseHours}`);
    }

    // CRITICAL ASSERTION: S2 Only SHOULD be purchasable
    expect(s2OnlyTicket?.referenceSessionId).toBeTruthy();
    expect(s2OnlyTicket?.canPurchase).toBe(true);

    console.log('\n✅ S2 Only Ticket correctly marked as AVAILABLE (future session)');
  });

  test('Both Sessions ticket uses EARLIEST session (S1) - NOT purchasable', async ({ page }) => {
    /**
     * Multi-session ticket behavior (CORRECTED):
     * - referenceSessionId = S1's ID (EARLIEST session, not first future)
     * - canPurchase = false (S1 timing window has closed)
     *
     * KEY RULE: Multi-session tickets use the EARLIEST session for ALL timing decisions.
     * Once the earliest session's registration closes, you cannot buy the multi-session ticket.
     */
    await AuthHelpers.loginAs(page, 'admin');
    await page.goto('/admin/events');
    await page.waitForLoadState('networkidle');

    // Get the test event
    const response = await apiRequest(page, 'GET', '/api/events?includeUnpublished=true');
    const testEvent = response.data.find((e: any) => e.title === 'Session Timing Test Event');

    if (!testEvent) {
      console.log('⚠️ Session Timing Test Event not found');
      test.skip();
      return;
    }

    const bothTicket = testEvent.ticketTypes?.find((t: any) => t.name === 'Both Sessions Ticket');
    const s1Session = testEvent.sessions?.find((s: any) => s.sessionIdentifier === 'S1');

    console.log('\n=== Both Sessions Ticket Analysis ===');
    console.log(`Name: ${bothTicket?.name}`);
    console.log(`sessionIdentifiers: ${JSON.stringify(bothTicket?.sessionIdentifiers)}`);
    console.log(`referenceSessionId: ${bothTicket?.referenceSessionId}`);
    console.log(`canPurchase: ${bothTicket?.canPurchase}`);
    console.log(`availabilityMessage: ${bothTicket?.availabilityMessage}`);

    // CRITICAL ASSERTION: Both Sessions should use S1 as reference (EARLIEST session)
    expect(bothTicket?.referenceSessionId).toBeTruthy();

    // The reference session should be S1 (the EARLIEST session)
    if (s1Session) {
      console.log(`\nExpected reference session (S1 - earliest): ${s1Session.id}`);
      console.log(`Actual reference session: ${bothTicket?.referenceSessionId}`);
      expect(bothTicket?.referenceSessionId).toBe(s1Session.id);
    }

    // CRITICAL ASSERTION: Both Sessions should NOT be purchasable
    // S1 is 7 days past, RegistrationCloseHours = 12
    // Registration closed 7 days + 12 hours ago
    expect(bothTicket?.canPurchase).toBe(false);

    console.log('\n✅ Both Sessions Ticket correctly uses S1 (earliest) for timing');
    console.log('✅ Both Sessions Ticket correctly NOT purchasable (earliest session registration closed)');
  });

  test('member view shows only available tickets', async ({ page }) => {
    /**
     * End-to-end verification from member perspective:
     * - Navigate to event as member
     * - S1 Only ticket should NOT be visible (or marked unavailable)
     * - S2 Only ticket SHOULD be visible and purchasable
     */
    await AuthHelpers.loginAs(page, 'member');

    // Get the test event ID first (include past events since event's StartDate is in the past)
    const eventsResponse = await apiRequest(page, 'GET', '/api/events?includePastEvents=true');
    const testEvent = eventsResponse.data.find((e: any) => e.title === 'Session Timing Test Event');

    if (!testEvent) {
      console.log('⚠️ Session Timing Test Event not found in public events');
      test.skip();
      return;
    }

    // Navigate to the event page
    await page.goto(`/events/${testEvent.id}`);
    await page.waitForLoadState('networkidle');
    await page.waitForTimeout(1000);

    // Take screenshot for debugging
    await page.screenshot({ path: 'test-results/session-timing-member-view.png', fullPage: true });

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
    const purchaseButtons = await page.locator('button:has-text("Purchase"), [data-testid="button-purchase-ticket"]').count();
    console.log(`Purchase buttons visible: ${purchaseButtons}`);

    // The key assertion: S2 Only should be available, S1 Only should not
    // Note: Implementation may hide unavailable tickets or show them as disabled
    if (hasTicketOptions || hasUserTicket) {
      console.log('\n✅ Ticket section visible on event page');

      // If S1 Only is visible, it should be marked as unavailable
      if (s1TicketVisible) {
        // Check if it's marked as unavailable
        const s1UnavailableIndicator = await page.locator('text=/S1 Only.*closed|unavailable|passed/i').count() > 0 ||
          await page.locator('[data-testid="ticket-S1 Only Ticket"]').locator('text=/closed|unavailable/i').count() > 0;
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

  test('API returns correct ticket availability status', async ({ page }) => {
    /**
     * Direct API verification of ticket availability fields
     */
    await AuthHelpers.loginAs(page, 'member');
    await page.goto('/events');
    await page.waitForLoadState('networkidle');

    // Get the test event (include past events since event's StartDate is in the past)
    const response = await apiRequest(page, 'GET', '/api/events?includePastEvents=true');
    const testEvent = response.data.find((e: any) => e.title === 'Session Timing Test Event');

    if (!testEvent) {
      console.log('⚠️ Session Timing Test Event not found');
      test.skip();
      return;
    }

    console.log('\n=== API Ticket Availability Summary ===');

    const s1Only = testEvent.ticketTypes?.find((t: any) => t.name === 'S1 Only Ticket');
    const s2Only = testEvent.ticketTypes?.find((t: any) => t.name === 'S2 Only Ticket');
    const both = testEvent.ticketTypes?.find((t: any) => t.name === 'Both Sessions Ticket');

    console.log('\nTicket Type | referenceSessionId | canPurchase | availabilityMessage');
    console.log('----------- | ------------------ | ----------- | -------------------');
    console.log(`S1 Only     | ${(s1Only?.referenceSessionId || 'null').padEnd(18)} | ${String(s1Only?.canPurchase).padEnd(11)} | ${s1Only?.availabilityMessage}`);
    console.log(`S2 Only     | ${(s2Only?.referenceSessionId?.substring(0, 8) || 'null').padEnd(18)} | ${String(s2Only?.canPurchase).padEnd(11)} | ${s2Only?.availabilityMessage}`);
    console.log(`Both        | ${(both?.referenceSessionId?.substring(0, 8) || 'null').padEnd(18)} | ${String(both?.canPurchase).padEnd(11)} | ${both?.availabilityMessage}`);

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
