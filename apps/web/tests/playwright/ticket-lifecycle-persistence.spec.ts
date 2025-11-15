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
    console.log('🔍 Looking for test event with paid tickets...');

    try {
      const ticketEvent = await DatabaseHelpers.getFirstTicketEvent();

      if (!ticketEvent) {
        throw new Error(
          'No ticket events found in database.\n' +
          '\n' +
          'These tests require at least one published event with paid tickets.\n' +
          '\n' +
          'To fix this:\n' +
          '1. Ensure Docker containers are running: ./dev.sh\n' +
          '2. Check if database has events: curl http://localhost:5655/api/events\n' +
          '3. If no events exist, seed the database with test data\n' +
          '4. Verify event has "paid" ticket type in EventTicketTypes table\n'
        );
      }

      TEST_EVENT_ID = ticketEvent.id;
      console.log(`✅ Found ticket event: "${ticketEvent.title}" (ID: ${TEST_EVENT_ID})`);
      console.log(`   Event Type: ${ticketEvent.eventType}`);
      console.log(`   Start Date: ${ticketEvent.startDate}`);
      console.log(`   Capacity: ${ticketEvent.capacity}`);
    } catch (error) {
      console.error('❌ Failed to find ticket event for testing:', error);
      throw error;
    }
  });

  test.afterAll(async () => {
    await globalCleanup();
  });

  test('CRITICAL: should persist ticket cancellation to database', async ({ page }) => {
    // TEST_EVENT_ID is guaranteed to exist from beforeAll hook

    // This tests the exact bug that was found:
    // - User cancels ticket
    // - UI shows success
    // - Frontend calls /api/events/{id}/ticket (WRONG - doesn't exist)
    // - Backend returns 404 (not found)
    // - Frontend ignores error, shows success anyway
    // - Database NOT updated
    // - Page refresh shows ticket still active

    const userId = await DatabaseHelpers.getUserIdFromEmail(AuthHelpers.accounts.vetted.email);

    // First, login and navigate to event page
    await AuthHelpers.loginAs(page, 'vetted');
    await page.goto(`http://localhost:5173/events/${TEST_EVENT_ID}`);
    await page.waitForLoadState('networkidle');

    // Wait for loading overlay to disappear
    const loadingOverlay = page.locator('.mantine-LoadingOverlay-overlay');
    if (await loadingOverlay.count() > 0) {
      await loadingOverlay.waitFor({ state: 'hidden', timeout: 10000 }).catch(() => {
        console.log('⚠️  Loading overlay did not disappear, continuing anyway');
      });
    }

    console.log('Verifying user has active ticket for cancellation test...');

    // Verify user has active ticket in database
    // If not, test setup is incomplete (ticket should be created beforehand)
    try {
      await DatabaseHelpers.verifyEventParticipation(userId, TEST_EVENT_ID, 1); // 1 = Active
      console.log('✅ User has active ticket - ready for cancellation test');
    } catch (error) {
      throw new Error(
        `Test setup incomplete: User does not have active ticket for event ${TEST_EVENT_ID}.\n` +
        'To fix: Ensure test event has tickets created beforehand via database seeding or manual setup.\n' +
        `Error: ${error}`
      );
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
    // TEST_EVENT_ID is guaranteed to exist from beforeAll hook

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
    // TEST_EVENT_ID is guaranteed to exist from beforeAll hook

    const userId = await DatabaseHelpers.getUserIdFromEmail(AuthHelpers.accounts.member.email);

    // Ensure user has ticket
    try {
      await DatabaseHelpers.verifyEventParticipation(userId, TEST_EVENT_ID, 1); // 1 = Active
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
      2 // 2 = Cancelled
    );

    const historyExists = await DatabaseHelpers.verifyAuditLogExists(
      'AttendanceHistory',
      participation.id,
      'Cancelled'
    );

    expect(historyExists).toBeTruthy();
    console.log('✅ Cancellation audit log created');
  });

  test('should prevent duplicate cancellations', async ({ page }) => {
    // TEST_EVENT_ID is guaranteed to exist from beforeAll hook

    const userId = await DatabaseHelpers.getUserIdFromEmail(AuthHelpers.accounts.vetted.email);

    // Ensure ticket is already cancelled
    try {
      await DatabaseHelpers.verifyEventParticipation(userId, TEST_EVENT_ID, 2); // 2 = Cancelled
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
    // TEST_EVENT_ID is guaranteed to exist from beforeAll hook

    const userId = await DatabaseHelpers.getUserIdFromEmail(AuthHelpers.accounts.member.email);

    // Ensure user has ticket
    try {
      await DatabaseHelpers.verifyEventParticipation(userId, TEST_EVENT_ID, 1); // 1 = Active
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
