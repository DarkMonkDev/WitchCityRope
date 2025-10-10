/**
 * Ticket Lifecycle Persistence E2E Test
 *
 * CRITICAL BUG THIS CATCHES: Ticket cancellation bug where UI showed success
 * but database wasn't updated because frontend called wrong endpoint.
 *
 * ROOT CAUSE: Frontend called DELETE /api/events/{id}/ticket (doesn't exist)
 * instead of DELETE /api/events/{id}/participation (correct endpoint)
 *
 * This test verifies complete ticket lifecycle:
 * - Purchase → Verify persistence
 * - Cancel → Verify persistence (CRITICAL BUG CHECK)
 * - Re-purchase → Verify persistence
 */

import { test, expect } from '@playwright/test';
import {
  testTicketCancellationPersistence,
  testTicketLifecycle,
} from './templates/ticket-cancellation-persistence-template';
import { DatabaseHelpers } from './utils/database-helpers';
import { globalCleanup } from './templates/persistence-test-template';
import { AuthHelpers } from './helpers/auth.helpers';

// Test event IDs - These should exist in seeded data
// In production, we'd create test events via API
let TEST_EVENT_ID: string;

test.describe.serial('Ticket Lifecycle Persistence Tests', () => {
  test.beforeAll(async ({ browser }) => {
    // Find a test event to use
    // In production, we'd create a test event via API
    // For now, we'll use a seeded event
    console.log('⚠️  Note: These tests require an active event in the database');
    console.log('   If tests fail, ensure test events exist via database seeding');
  });

  test.afterAll(async () => {
    await globalCleanup();
  });

  test('should find a test event for ticket lifecycle tests', async ({ page }) => {
    console.log('🔍 Finding suitable test event...');

    // STRATEGY 1: Try to get event directly from database (faster, more reliable)
    try {
      const ticketEvent = await DatabaseHelpers.getFirstTicketEvent();

      if (ticketEvent) {
        TEST_EVENT_ID = ticketEvent.id;
        console.log(`✅ Found ticket event via database: ${TEST_EVENT_ID}`);
        console.log(`   Event: "${ticketEvent.title}" (${ticketEvent.eventType})`);
        expect(TEST_EVENT_ID).toBeTruthy();
        return;
      }
    } catch (error) {
      console.log('⚠️  Could not query database directly, falling back to UI discovery');
    }

    // STRATEGY 2: Fallback to UI-based discovery with improved waiting
    console.log('📍 Using UI-based event discovery...');

    await AuthHelpers.loginAs(page, 'vetted');

    // Navigate to events page
    await page.goto('http://localhost:5173/events');
    await page.waitForLoadState('networkidle');

    // ✅ NEW: Wait for event cards to render with explicit timeout
    console.log('⏳ Waiting for event cards to render...');

    const eventCards = page.locator('[data-testid="event-card"]');

    try {
      // Wait up to 10 seconds for at least one event card
      await eventCards.first().waitFor({
        state: 'visible',
        timeout: 10000
      });

      const cardCount = await eventCards.count();
      console.log(`✅ Found ${cardCount} event cards`);

      // Find first event link within event cards
      const firstEventCard = eventCards.first();
      const eventLink = firstEventCard.locator('a[href*="/events/"]');

      const href = await eventLink.getAttribute('href');

      if (!href) {
        throw new Error('Event card found but has no link');
      }

      TEST_EVENT_ID = href.split('/events/')[1];

      console.log(`✅ Found test event ID via UI: ${TEST_EVENT_ID}`);
      expect(TEST_EVENT_ID).toBeTruthy();

    } catch (error) {
      // Provide detailed diagnostic information
      const currentUrl = page.url();
      const pageTitle = await page.title();

      console.error('❌ Failed to find events on page');
      console.error(`   Current URL: ${currentUrl}`);
      console.error(`   Page title: ${pageTitle}`);

      // Check if page shows error or empty state
      const errorAlert = page.locator('[role="alert"]');
      const hasError = await errorAlert.count() > 0;

      if (hasError) {
        const errorText = await errorAlert.textContent();
        console.error(`   Error on page: ${errorText}`);
      }

      const emptyState = page.locator('[data-testid="events-empty-state"]');
      const hasEmptyState = await emptyState.count() > 0;

      if (hasEmptyState) {
        console.error('   Page shows empty state (no events available)');
      }

      throw new Error(
        'No events found on events page.\n' +
        '\n' +
        'Troubleshooting steps:\n' +
        '1. Check if database has events: curl http://localhost:5655/api/events\n' +
        '2. Verify user is authenticated and can view events\n' +
        '3. Check browser console for JavaScript errors\n' +
        '4. Ensure events page component is rendering correctly\n' +
        '\n' +
        'To seed events: npm run db:seed'
      );
    }
  });

  test('CRITICAL: should persist ticket cancellation to database', async ({ page }) => {
    test.skip(!TEST_EVENT_ID, 'No test event available');

    // This tests the exact bug that was found:
    // - User cancels ticket
    // - UI shows success
    // - Frontend calls /api/events/{id}/ticket (WRONG - doesn't exist)
    // - Backend returns 404 (not found)
    // - Frontend ignores error, shows success anyway
    // - Database NOT updated
    // - Page refresh shows ticket still active

    const userId = await DatabaseHelpers.getUserIdFromEmail(AuthHelpers.accounts.vetted.email);

    // First, ensure user has a ticket to cancel
    console.log('Setting up: Ensuring user has active ticket...');

    // Check if user already has ticket
    try {
      await DatabaseHelpers.verifyEventParticipation(userId, TEST_EVENT_ID, 'Registered');
      console.log('✅ User already has active ticket');
    } catch {
      // User doesn't have ticket, purchase one
      console.log('Purchasing ticket for test...');
      await page.goto(`http://localhost:5173/events/${TEST_EVENT_ID}`);
      await page.waitForLoadState('networkidle');

      const purchaseButton = page.locator(
        'button:has-text("Purchase Ticket"), button:has-text("Register")'
      ).first();

      if (await purchaseButton.count() > 0) {
        await purchaseButton.click();
        await page.waitForLoadState('networkidle');
        console.log('✅ Purchased ticket for test');
      }
    }

    // Now test cancellation persistence
    try {
      await testTicketCancellationPersistence(page, {
        userEmail: AuthHelpers.accounts.vetted.email,
        userPassword: AuthHelpers.accounts.vetted.password,
        eventId: TEST_EVENT_ID,
        cancellationReason: 'E2E persistence test',
        successMessage: 'Ticket cancelled successfully',
        screenshotPath: '/tmp/ticket-cancel-critical',
      });

      console.log('✅ PERSISTENCE VERIFIED: Ticket cancellation correctly persists to database');
    } catch (error) {
      console.error('❌ BUG DETECTED: Ticket cancellation did NOT persist to database!');
      console.error('This is the exact bug that was found in production:');
      console.error('- UI showed success message');
      console.error('- Frontend called wrong endpoint (/ticket instead of /participation)');
      console.error('- Backend returned 404 (endpoint not found)');
      console.error('- Database was NOT updated');
      console.error('- Page refresh showed ticket still active');
      throw error;
    }
  });

  test('should handle complete ticket lifecycle', async ({ page }) => {
    test.skip(!TEST_EVENT_ID, 'No test event available');

    // Test: Purchase → Cancel → Re-purchase
    // Each step verifies database persistence
    await testTicketLifecycle(
      page,
      AuthHelpers.accounts.member.email,
      AuthHelpers.accounts.member.password,
      TEST_EVENT_ID
    );
  });

  test('should persist cancellation reason to database', async ({ page }) => {
    test.skip(!TEST_EVENT_ID, 'No test event available');

    const userId = await DatabaseHelpers.getUserIdFromEmail(AuthHelpers.accounts.member.email);

    // Ensure user has ticket
    try {
      await DatabaseHelpers.verifyEventParticipation(userId, TEST_EVENT_ID, 'Registered');
    } catch {
      // Purchase ticket if needed
      await page.goto(`http://localhost:5173/events/${TEST_EVENT_ID}`);
      await page.waitForLoadState('networkidle');

      const purchaseButton = page.locator('button:has-text("Purchase Ticket"), button:has-text("Register")').first();
      if (await purchaseButton.count() > 0) {
        await purchaseButton.click();
        await page.waitForLoadState('networkidle');
      }
    }

    const cancellationReason = `Cancellation reason test ${Date.now()}`;

    await testTicketCancellationPersistence(page, {
      userEmail: AuthHelpers.accounts.member.email,
      userPassword: AuthHelpers.accounts.member.password,
      eventId: TEST_EVENT_ID,
      cancellationReason,
    });

    // Verify cancellation reason in participation history
    const participation = await DatabaseHelpers.verifyEventParticipation(
      userId,
      TEST_EVENT_ID,
      'Cancelled'
    );

    const historyExists = await DatabaseHelpers.verifyAuditLogExists(
      'ParticipationHistory',
      participation.id,
      'Cancelled'
    );

    expect(historyExists).toBeTruthy();
    console.log('✅ Cancellation audit log created');
  });

  test('should prevent duplicate cancellations', async ({ page }) => {
    test.skip(!TEST_EVENT_ID, 'No test event available');

    const userId = await DatabaseHelpers.getUserIdFromEmail(AuthHelpers.accounts.vetted.email);

    // Ensure ticket is already cancelled
    try {
      await DatabaseHelpers.verifyEventParticipation(userId, TEST_EVENT_ID, 'Cancelled');
      console.log('✅ Ticket already cancelled');
    } catch {
      // Cancel ticket first
      await testTicketCancellationPersistence(page, {
        userEmail: AuthHelpers.accounts.vetted.email,
        userPassword: AuthHelpers.accounts.vetted.password,
        eventId: TEST_EVENT_ID,
      });
    }

    // Navigate to event page
    await page.goto(`http://localhost:5173/events/${TEST_EVENT_ID}`);
    await page.waitForLoadState('networkidle');

    // Cancel button should NOT be visible
    const cancelButton = page.locator('button:has-text("Cancel Ticket")');
    const buttonCount = await cancelButton.count();

    expect(buttonCount).toBe(0);
    console.log('✅ Cancel button not visible for already-cancelled ticket');
  });
});

test.describe('Ticket Persistence Edge Cases', () => {
  test.afterAll(async () => {
    await globalCleanup();
  });

  test('should handle network errors gracefully during cancellation', async ({ page }) => {
    // This would test offline/network error scenarios
    // For now, we'll skip as it requires network mocking
    test.skip();
  });

  test('should handle concurrent cancellation attempts', async ({ page }) => {
    // This would test race conditions
    // For now, we'll skip as it requires multiple browser contexts
    test.skip();
  });

  test('should verify endpoint called is correct', async ({ page }) => {
    test.skip(!TEST_EVENT_ID, 'No test event available');

    const userId = await DatabaseHelpers.getUserIdFromEmail(AuthHelpers.accounts.member.email);

    // Ensure user has ticket
    try {
      await DatabaseHelpers.verifyEventParticipation(userId, TEST_EVENT_ID, 'Registered');
    } catch {
      await page.goto(`http://localhost:5173/events/${TEST_EVENT_ID}`);
      await page.waitForLoadState('networkidle');

      const purchaseButton = page.locator('button:has-text("Purchase Ticket")').first();
      if (await purchaseButton.count() > 0) {
        await purchaseButton.click();
        await page.waitForLoadState('networkidle');
      }
    }

    // Monitor network requests
    const requests: string[] = [];
    page.on('request', request => {
      if (request.method() === 'DELETE') {
        requests.push(request.url());
      }
    });

    // Navigate and cancel
    await page.goto(`http://localhost:5173/events/${TEST_EVENT_ID}`);
    await page.waitForLoadState('networkidle');

    const cancelButton = page.locator('button:has-text("Cancel Ticket")').first();
    if (await cancelButton.count() > 0) {
      await cancelButton.click();

      // Wait for confirmation modal
      await page.waitForTimeout(500);
      const confirmButton = page.locator('button:has-text("Confirm")').last();
      if (await confirmButton.count() > 0) {
        await confirmButton.click();
      }

      await page.waitForTimeout(1000);

      // Verify correct endpoint was called
      const participationEndpoint = requests.find(url =>
        url.includes('/participation') && !url.includes('/ticket')
      );

      const wrongEndpoint = requests.find(url =>
        url.includes('/ticket') && !url.includes('/participation')
      );

      if (wrongEndpoint) {
        console.error('❌ BUG: Frontend called WRONG endpoint:', wrongEndpoint);
        console.error('Expected: /api/events/{id}/participation');
        throw new Error('Frontend calling wrong endpoint for ticket cancellation');
      }

      if (participationEndpoint) {
        console.log('✅ Frontend called CORRECT endpoint:', participationEndpoint);
      }

      expect(participationEndpoint).toBeTruthy();
    }
  });
});
