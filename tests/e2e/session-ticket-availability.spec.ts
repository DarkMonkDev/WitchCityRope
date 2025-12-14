/**
 * Session-Based Ticket Availability E2E Tests
 *
 * Tests the session-based timing logic for ticket purchases.
 * The key calculation: hoursUntilSession < registrationCloseHours → NOT purchasable
 *
 * TIMING LOGIC:
 * - hoursUntilSession = (session.StartTime - now).TotalHours
 * - Positive closeHours = must buy X hours BEFORE session
 * - Negative closeHours = can buy up to X hours AFTER session start
 * - NULL closeHours = no restriction (always purchasable)
 *
 * MULTI-SESSION TICKETS:
 * - Use IsAnySessionPurchasable() - purchasable if ANY session is still in window
 *
 * TEST SCENARIOS:
 * 1. Session 48 hours past, closeHours=4 → NOT purchasable (way past)
 * 2. Session 2 hours past, closeHours=4 → NOT purchasable (still past)
 * 3. Session 2 hours future, closeHours=4 → NOT purchasable (within close window)
 * 4. Session 24 hours future, closeHours=4 → PURCHASABLE (outside close window)
 * 5. Session 2 hours past, closeHours=-4 → PURCHASABLE (within post-start window)
 * 6. Multi-session: S1 past, S2 future → PURCHASABLE (S2 is still in window)
 *
 * @see /apps/api/Features/Events/Services/TimeZoneService.cs - IsActionAllowedForSession
 * @see /docs/functional-areas/events/session-timing-refactor/SPECIFICATION.md
 */

import { expect } from '@playwright/test';
import { test } from '../lib/datafactory/fixtures/test.fixture';
import { AuthHelpers } from './test-utils/helpers/auth.helpers';

/**
 * Helper to create a date X hours from now
 * Positive hours = future, Negative hours = past
 */
function hoursFromNow(hours: number): Date {
  return new Date(Date.now() + hours * 60 * 60 * 1000);
}

test.describe('Session-Based Ticket Availability - Timing Tests', () => {
  test('session 48 hours past with closeHours=4 should NOT be purchasable', async ({ page, df }) => {
    /**
     * Scenario: Session was 48 hours ago, registration closes 4 hours before
     * hoursUntilSession = -48
     * -48 < 4 → TRUE → canPurchase = FALSE
     */
    const event = await df.events.create({
      title: `Past Session Test ${Date.now()}`,
      startDate: hoursFromNow(-48),
      endDate: hoursFromNow(-45),
      status: 'Published',
      isPublic: true,
      registrationCloseHours: 4, // Must buy 4+ hours before session
    });

    const session = await df.sessions.create({
      eventId: event.id,
      title: 'Past Session',
      sessionIdentifier: 'S1',
      startTime: hoursFromNow(-48),
      endTime: hoursFromNow(-45),
      maxCapacity: 20,
    });

    await df.ticketTypes.create({
      eventId: event.id,
      sessionId: session.id,
      name: 'Past Session Ticket',
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

    const ticket = response.data.ticketTypes?.find((t: any) => t.name === 'Past Session Ticket');

    console.log('\n=== 48 Hours Past, closeHours=4 ===');
    console.log(`registrationCloseHours: ${response.data.registrationCloseHours}`);
    console.log(`canPurchase: ${ticket?.canPurchase}`);
    console.log(`availabilityMessage: ${ticket?.availabilityMessage}`);

    expect(ticket?.canPurchase).toBe(false);
    console.log('✅ Session 48 hours past correctly NOT purchasable');
  });

  test('session 2 hours past with closeHours=4 should NOT be purchasable', async ({ page, df }) => {
    /**
     * Scenario: Session was 2 hours ago, registration closes 4 hours before
     * hoursUntilSession = -2
     * -2 < 4 → TRUE → canPurchase = FALSE
     */
    const event = await df.events.create({
      title: `Recently Past Session Test ${Date.now()}`,
      startDate: hoursFromNow(-2),
      endDate: hoursFromNow(1),
      status: 'Published',
      isPublic: true,
      registrationCloseHours: 4,
    });

    const session = await df.sessions.create({
      eventId: event.id,
      title: 'Recently Past Session',
      sessionIdentifier: 'S1',
      startTime: hoursFromNow(-2),
      endTime: hoursFromNow(1),
      maxCapacity: 20,
    });

    await df.ticketTypes.create({
      eventId: event.id,
      sessionId: session.id,
      name: 'Recently Past Ticket',
      price: 25,
      quantityAvailable: 20,
    });

    await AuthHelpers.loginAs(page, 'admin');
    await page.goto('/admin/events');
    await page.waitForLoadState('networkidle');

    const response = await page.evaluate(async (eventId: string) => {
      const res = await fetch(`/api/events/${eventId}`, { credentials: 'include' });
      return { status: res.status, data: await res.json() };
    }, event.id);

    const ticket = response.data.ticketTypes?.find((t: any) => t.name === 'Recently Past Ticket');

    console.log('\n=== 2 Hours Past, closeHours=4 ===');
    console.log(`canPurchase: ${ticket?.canPurchase}`);

    expect(ticket?.canPurchase).toBe(false);
    console.log('✅ Session 2 hours past correctly NOT purchasable');
  });

  test('session 2 hours future with closeHours=4 should NOT be purchasable (within close window)', async ({ page, df }) => {
    /**
     * Scenario: Session is 2 hours from now, registration closes 4 hours before
     * hoursUntilSession = 2
     * 2 < 4 → TRUE → canPurchase = FALSE (too close to session!)
     */
    const event = await df.events.create({
      title: `Close Future Session Test ${Date.now()}`,
      startDate: hoursFromNow(2),
      endDate: hoursFromNow(5),
      status: 'Published',
      isPublic: true,
      registrationCloseHours: 4,
    });

    const session = await df.sessions.create({
      eventId: event.id,
      title: 'Close Future Session',
      sessionIdentifier: 'S1',
      startTime: hoursFromNow(2),
      endTime: hoursFromNow(5),
      maxCapacity: 20,
    });

    await df.ticketTypes.create({
      eventId: event.id,
      sessionId: session.id,
      name: 'Close Future Ticket',
      price: 25,
      quantityAvailable: 20,
    });

    await AuthHelpers.loginAs(page, 'admin');
    await page.goto('/admin/events');
    await page.waitForLoadState('networkidle');

    const response = await page.evaluate(async (eventId: string) => {
      const res = await fetch(`/api/events/${eventId}`, { credentials: 'include' });
      return { status: res.status, data: await res.json() };
    }, event.id);

    const ticket = response.data.ticketTypes?.find((t: any) => t.name === 'Close Future Ticket');

    console.log('\n=== 2 Hours Future, closeHours=4 ===');
    console.log(`canPurchase: ${ticket?.canPurchase}`);
    console.log(`(Registration window closed because we are within 4 hours of session)`);

    expect(ticket?.canPurchase).toBe(false);
    console.log('✅ Session 2 hours future correctly NOT purchasable (within close window)');
  });

  test('session 24 hours future with closeHours=4 should BE purchasable', async ({ page, df }) => {
    /**
     * Scenario: Session is 24 hours from now, registration closes 4 hours before
     * hoursUntilSession = 24
     * 24 < 4 → FALSE → canPurchase = TRUE
     */
    const event = await df.events.create({
      title: `Far Future Session Test ${Date.now()}`,
      startDate: hoursFromNow(24),
      endDate: hoursFromNow(27),
      status: 'Published',
      isPublic: true,
      registrationCloseHours: 4,
    });

    const session = await df.sessions.create({
      eventId: event.id,
      title: 'Far Future Session',
      sessionIdentifier: 'S1',
      startTime: hoursFromNow(24),
      endTime: hoursFromNow(27),
      maxCapacity: 20,
    });

    await df.ticketTypes.create({
      eventId: event.id,
      sessionId: session.id,
      name: 'Far Future Ticket',
      price: 25,
      quantityAvailable: 20,
    });

    await AuthHelpers.loginAs(page, 'admin');
    await page.goto('/admin/events');
    await page.waitForLoadState('networkidle');

    const response = await page.evaluate(async (eventId: string) => {
      const res = await fetch(`/api/events/${eventId}`, { credentials: 'include' });
      return { status: res.status, data: await res.json() };
    }, event.id);

    const ticket = response.data.ticketTypes?.find((t: any) => t.name === 'Far Future Ticket');

    console.log('\n=== 24 Hours Future, closeHours=4 ===');
    console.log(`canPurchase: ${ticket?.canPurchase}`);

    expect(ticket?.canPurchase).toBe(true);
    console.log('✅ Session 24 hours future correctly PURCHASABLE');
  });

  test('session 2 hours past with closeHours=-4 should BE purchasable (post-start window)', async ({ page, df }) => {
    /**
     * Scenario: Session was 2 hours ago, but registration allows up to 4 hours AFTER start
     * hoursUntilSession = -2
     * closeHours = -4 (negative = hours after session start)
     * -2 < -4 → FALSE (since -2 > -4) → canPurchase = TRUE
     */
    const event = await df.events.create({
      title: `Post-Start Window Test ${Date.now()}`,
      startDate: hoursFromNow(-2),
      endDate: hoursFromNow(1),
      status: 'Published',
      isPublic: true,
      registrationCloseHours: -4, // Can buy up to 4 hours AFTER session starts
    });

    const session = await df.sessions.create({
      eventId: event.id,
      title: 'Post-Start Session',
      sessionIdentifier: 'S1',
      startTime: hoursFromNow(-2),
      endTime: hoursFromNow(1),
      maxCapacity: 20,
    });

    await df.ticketTypes.create({
      eventId: event.id,
      sessionId: session.id,
      name: 'Post-Start Ticket',
      price: 25,
      quantityAvailable: 20,
    });

    await AuthHelpers.loginAs(page, 'admin');
    await page.goto('/admin/events');
    await page.waitForLoadState('networkidle');

    const response = await page.evaluate(async (eventId: string) => {
      const res = await fetch(`/api/events/${eventId}`, { credentials: 'include' });
      return { status: res.status, data: await res.json() };
    }, event.id);

    const ticket = response.data.ticketTypes?.find((t: any) => t.name === 'Post-Start Ticket');

    console.log('\n=== 2 Hours Past, closeHours=-4 (allows post-start) ===');
    console.log(`registrationCloseHours: ${response.data.registrationCloseHours}`);
    console.log(`canPurchase: ${ticket?.canPurchase}`);
    console.log(`(Registration allows purchase up to 4 hours after session start)`);

    expect(ticket?.canPurchase).toBe(true);
    console.log('✅ Session 2 hours past with -4 closeHours correctly PURCHASABLE');
  });

  test('multi-session ticket with S1 past and S2 future should BE purchasable', async ({ page, df }) => {
    /**
     * Multi-session ticket behavior:
     * Uses IsAnySessionPurchasable() - returns TRUE if ANY session is still in window
     * S1: 48 hours past → NOT purchasable
     * S2: 24 hours future → PURCHASABLE
     * Result: Ticket IS purchasable because S2 is still in window
     */
    const event = await df.events.create({
      title: `Multi-Session Test ${Date.now()}`,
      startDate: hoursFromNow(-48),
      endDate: hoursFromNow(27),
      status: 'Published',
      isPublic: true,
      registrationCloseHours: 4,
    });

    // S1 - Past session (48 hours ago)
    const s1 = await df.sessions.create({
      eventId: event.id,
      title: 'S1 - Past Session',
      sessionIdentifier: 'S1',
      startTime: hoursFromNow(-48),
      endTime: hoursFromNow(-45),
      maxCapacity: 20,
    });

    // S2 - Future session (24 hours from now)
    const s2 = await df.sessions.create({
      eventId: event.id,
      title: 'S2 - Future Session',
      sessionIdentifier: 'S2',
      startTime: hoursFromNow(24),
      endTime: hoursFromNow(27),
      maxCapacity: 20,
    });

    // Create S1 Only ticket (should NOT be purchasable)
    await df.ticketTypes.create({
      eventId: event.id,
      sessionId: s1.id,
      name: 'S1 Only Ticket',
      price: 25,
      quantityAvailable: 20,
    });

    // Create S2 Only ticket (should BE purchasable)
    await df.ticketTypes.create({
      eventId: event.id,
      sessionId: s2.id,
      name: 'S2 Only Ticket',
      price: 25,
      quantityAvailable: 20,
    });

    // Create Both Sessions ticket (should BE purchasable because S2 is still in window)
    await df.ticketTypes.create({
      eventId: event.id,
      sessionIds: [s1.id, s2.id],
      name: 'Both Sessions Ticket',
      price: 40,
      quantityAvailable: 20,
    });

    await AuthHelpers.loginAs(page, 'admin');
    await page.goto('/admin/events');
    await page.waitForLoadState('networkidle');

    const response = await page.evaluate(async (eventId: string) => {
      const res = await fetch(`/api/events/${eventId}`, { credentials: 'include' });
      return { status: res.status, data: await res.json() };
    }, event.id);

    const s1Ticket = response.data.ticketTypes?.find((t: any) => t.name === 'S1 Only Ticket');
    const s2Ticket = response.data.ticketTypes?.find((t: any) => t.name === 'S2 Only Ticket');
    const bothTicket = response.data.ticketTypes?.find((t: any) => t.name === 'Both Sessions Ticket');

    console.log('\n=== Multi-Session Test ===');
    console.log(`S1 Only (48h past): canPurchase = ${s1Ticket?.canPurchase}`);
    console.log(`S2 Only (24h future): canPurchase = ${s2Ticket?.canPurchase}`);
    console.log(`Both Sessions: canPurchase = ${bothTicket?.canPurchase}`);

    // S1 Only should NOT be purchasable (session is past)
    expect(s1Ticket?.canPurchase).toBe(false);

    // S2 Only SHOULD be purchasable (session is future, outside close window)
    expect(s2Ticket?.canPurchase).toBe(true);

    // Both Sessions SHOULD be purchasable (S2 is still in window)
    expect(bothTicket?.canPurchase).toBe(true);

    console.log('✅ Multi-session ticket correctly PURCHASABLE (S2 is still in window)');
  });

  test('no timing restrictions (null closeHours) should always be purchasable', async ({ page, df }) => {
    /**
     * When registrationCloseHours is NULL, there are no timing restrictions
     * Any ticket should be purchasable regardless of session timing
     */
    const event = await df.events.create({
      title: `No Restrictions Test ${Date.now()}`,
      startDate: hoursFromNow(-48), // Session 48 hours past
      endDate: hoursFromNow(-45),
      status: 'Published',
      isPublic: true,
      // registrationCloseHours not set = null = no restriction
    });

    const session = await df.sessions.create({
      eventId: event.id,
      title: 'Past Session No Restrictions',
      sessionIdentifier: 'S1',
      startTime: hoursFromNow(-48),
      endTime: hoursFromNow(-45),
      maxCapacity: 20,
    });

    await df.ticketTypes.create({
      eventId: event.id,
      sessionId: session.id,
      name: 'No Restrictions Ticket',
      price: 25,
      quantityAvailable: 20,
    });

    await AuthHelpers.loginAs(page, 'admin');
    await page.goto('/admin/events');
    await page.waitForLoadState('networkidle');

    const response = await page.evaluate(async (eventId: string) => {
      const res = await fetch(`/api/events/${eventId}`, { credentials: 'include' });
      return { status: res.status, data: await res.json() };
    }, event.id);

    const ticket = response.data.ticketTypes?.find((t: any) => t.name === 'No Restrictions Ticket');

    console.log('\n=== No Timing Restrictions (null closeHours) ===');
    console.log(`registrationCloseHours: ${response.data.registrationCloseHours ?? 'null'}`);
    console.log(`canPurchase: ${ticket?.canPurchase}`);

    expect(ticket?.canPurchase).toBe(true);
    console.log('✅ No timing restrictions correctly allows purchase of past session ticket');
  });
});
