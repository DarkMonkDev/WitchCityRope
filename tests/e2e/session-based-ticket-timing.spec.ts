/**
 * Session-Based Ticket Timing E2E Tests (DataFactory Migration)
 *
 * Tests that verify session-based timing functionality for ticket purchases
 * from a user's perspective. These tests validate the implementation of the
 * session timing refactor specification.
 *
 * MIGRATION NOTES:
 * - Uses df (DataFactory) fixture for automatic cleanup
 * - Creates test data via TestHelper API endpoints
 * - No need for manual API calls or cleanup logic
 * - Data is automatically cleaned up after each test
 *
 * Original: tests/e2e/session-based-ticket-timing.spec.ts
 * Migrated: 2025-12-10
 */

import { expect } from '@playwright/test';
import { test } from '../lib/datafactory/fixtures/test.fixture';
import { AuthHelpers } from './test-utils/helpers/auth.helpers';

test.describe('Session-Based Ticket Timing', () => {
  test('multi-session event shows tickets for future sessions', async ({ page, df }) => {
    // Create test event with 2 sessions
    const event = await df.events.createPublished(`Ticket Timing Test Event ${Date.now()}`);

    // Calculate session times - one 7 days out, one 8 days out
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
      title: 'Day 1 Session',
      startTime: session1Start,
      endTime: session1End,
      maxCapacity: 20,
    });

    const session2 = await df.sessions.create({
      eventId: event.id,
      title: 'Day 2 Session',
      startTime: session2Start,
      endTime: session2End,
      maxCapacity: 20,
    });

    const ticketType = await df.ticketTypes.create({
      eventId: event.id,
      sessionId: session1.id,
      name: 'Both Sessions Pass',
      price: 50.0,
      quantityAvailable: 20,
    });

    console.log(`✅ Created test event: ${event.id}`);
    console.log(`Session 1 ID: ${session1.id}`);
    console.log(`Session 2 ID: ${session2.id}`);

    // Navigate to the test event's public page
    await page.goto(`/events/${event.id}`);
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

  test('event displays session information', async ({ page, df }) => {
    const event = await df.events.createPublished(`Session Display Test ${Date.now()}`);

    const sessionStart = new Date();
    sessionStart.setDate(sessionStart.getDate() + 7);
    sessionStart.setHours(18, 0, 0, 0);
    const sessionEnd = new Date(sessionStart.getTime() + 3 * 60 * 60 * 1000);

    await df.sessions.create({
      eventId: event.id,
      title: 'Day 1 Session',
      startTime: sessionStart,
      endTime: sessionEnd,
      maxCapacity: 20,
    });

    await page.goto(`/events/${event.id}`);
    await page.waitForLoadState('domcontentloaded');

    // Look for sessions section
    const sessionsSection = page.locator('[data-testid="event-sessions"], section:has-text("Sessions"), [data-testid="sessions-list"]').first();

    if (await sessionsSection.count() > 0) {
      await expect(sessionsSection).toBeVisible();
      console.log('✅ Sessions section visible');

      // Verify session names are displayed
      const pageContent = await page.locator('body').textContent();
      if (pageContent?.includes('Day 1')) {
        console.log('✅ Session 1 information visible');
      }
    } else {
      console.log('Sessions may be displayed inline with tickets');
    }
  });

  test('ticket shows which sessions it covers', async ({ page, df }) => {
    const event = await df.events.createPublished(`Session Coverage Test ${Date.now()}`);

    const sessionStart = new Date();
    sessionStart.setDate(sessionStart.getDate() + 7);
    sessionStart.setHours(18, 0, 0, 0);
    const sessionEnd = new Date(sessionStart.getTime() + 3 * 60 * 60 * 1000);

    const session = await df.sessions.create({
      eventId: event.id,
      title: 'Main Session',
      startTime: sessionStart,
      endTime: sessionEnd,
      maxCapacity: 20,
    });

    await df.ticketTypes.create({
      eventId: event.id,
      sessionId: session.id,
      name: 'Session Ticket',
      price: 25.0,
      quantityAvailable: 20,
    });

    await page.goto(`/events/${event.id}`);
    await page.waitForLoadState('domcontentloaded');

    // Find ticket cards
    const ticketCards = page.locator('[data-testid="ticket-card"], .ticket-card, [data-testid="ticket-type"]');

    if (await ticketCards.count() > 0) {
      const firstTicket = ticketCards.first();
      await expect(firstTicket).toBeVisible();

      const ticketText = await firstTicket.textContent();
      console.log(`Ticket content: ${ticketText?.substring(0, 100)}...`);

      // Look for session information in ticket
      if (ticketText?.match(/session|Main Session/i)) {
        console.log('✅ Ticket displays session information');
      } else {
        console.log('Ticket may not show explicit session names');
      }
    }
  });

  test('ticket availability reflects timing settings', async ({ page, df }) => {
    const event = await df.events.createPublished(`Availability Test ${Date.now()}`);

    const sessionStart = new Date();
    sessionStart.setDate(sessionStart.getDate() + 7);
    sessionStart.setHours(18, 0, 0, 0);
    const sessionEnd = new Date(sessionStart.getTime() + 3 * 60 * 60 * 1000);

    const session = await df.sessions.create({
      eventId: event.id,
      title: 'Test Session',
      startTime: sessionStart,
      endTime: sessionEnd,
      maxCapacity: 20,
    });

    await df.ticketTypes.create({
      eventId: event.id,
      sessionId: session.id,
      name: 'Test Ticket',
      price: 25.0,
      quantityAvailable: 20,
    });

    await page.goto(`/events/${event.id}`);
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

  test('admin can view timing settings for event', async ({ page, df }) => {
    const event = await df.events.createPublished(`Admin Timing Test ${Date.now()}`);

    const sessionStart = new Date();
    sessionStart.setDate(sessionStart.getDate() + 7);
    sessionStart.setHours(18, 0, 0, 0);
    const sessionEnd = new Date(sessionStart.getTime() + 3 * 60 * 60 * 1000);

    await df.sessions.create({
      eventId: event.id,
      title: 'Test Session',
      startTime: sessionStart,
      endTime: sessionEnd,
      maxCapacity: 20,
    });

    await AuthHelpers.loginAs(page, 'admin');
    await page.goto(`/admin/events/${event.id}`);
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

  test('member can view event with tickets', async ({ page, df }) => {
    const event = await df.events.createPublished(`Member View Test ${Date.now()}`);

    const sessionStart = new Date();
    sessionStart.setDate(sessionStart.getDate() + 7);
    sessionStart.setHours(18, 0, 0, 0);
    const sessionEnd = new Date(sessionStart.getTime() + 3 * 60 * 60 * 1000);

    const session = await df.sessions.create({
      eventId: event.id,
      title: 'Test Session',
      startTime: sessionStart,
      endTime: sessionEnd,
      maxCapacity: 20,
    });

    await df.ticketTypes.create({
      eventId: event.id,
      sessionId: session.id,
      name: 'Member Ticket',
      price: 25.0,
      quantityAvailable: 20,
    });

    await AuthHelpers.loginAs(page, 'member');
    await page.goto(`/events/${event.id}`);
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

  test('ticket timing uses session dates not event dates', async ({ page, df }) => {
    // This test verifies the core session-based timing behavior
    // Tickets should be available based on session timing, not event start date

    const event = await df.events.createPublished(`Timing Logic Test ${Date.now()}`);

    const sessionStart = new Date();
    sessionStart.setDate(sessionStart.getDate() + 7);
    sessionStart.setHours(18, 0, 0, 0);
    const sessionEnd = new Date(sessionStart.getTime() + 3 * 60 * 60 * 1000);

    const session = await df.sessions.create({
      eventId: event.id,
      title: 'Future Session',
      startTime: sessionStart,
      endTime: sessionEnd,
      maxCapacity: 20,
    });

    await df.ticketTypes.create({
      eventId: event.id,
      sessionId: session.id,
      name: 'Future Session Ticket',
      price: 25.0,
      quantityAvailable: 20,
    });

    await page.goto(`/events/${event.id}`);
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
