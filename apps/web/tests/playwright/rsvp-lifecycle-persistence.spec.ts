/**
 * RSVP Lifecycle Persistence E2E Test
 *
 * Verifies RSVP operations persist correctly to database.
 * Similar pattern to ticket cancellation but for free events.
 *
 * Test Pattern:
 * - RSVP → Verify persistence
 * - Cancel RSVP → Verify persistence
 * - Re-RSVP → Verify persistence
 */

import { test, expect } from '@playwright/test';
import {
  testRsvpPersistence,
  testCancelRsvpPersistence,
  testRsvpLifecycle,
} from './templates/rsvp-persistence-template';
import { DatabaseHelpers } from './utils/database-helpers';
import { globalCleanup } from './templates/persistence-test-template';
import { AuthHelpers } from './helpers/auth.helpers';

// Test event IDs for RSVP events (should be Social events)
let RSVP_EVENT_ID: string;

test.describe.serial('RSVP Lifecycle Persistence Tests', () => {
  test.beforeAll(async ({ browser }) => {
    // Find a test event to use
    console.log('🔍 Looking for test event with free/RSVP tickets...');

    try {
      const rsvpEvent = await DatabaseHelpers.getFirstRsvpEvent();

      if (!rsvpEvent) {
        throw new Error(
          'No RSVP events found in database.\n' +
          '\n' +
          'These tests require at least one published event with free tickets (Price = 0).\n' +
          '\n' +
          'To fix this:\n' +
          '1. Ensure Docker containers are running: ./dev.sh\n' +
          '2. Check if database has events: curl http://localhost:5655/api/events\n' +
          '3. If no events exist, seed the database with test data\n' +
          '4. Verify event has "free" ticket type (Price = 0) in TicketTypes table\n'
        );
      }

      RSVP_EVENT_ID = rsvpEvent.id;
      console.log(`✅ Found RSVP event: "${rsvpEvent.title}" (ID: ${RSVP_EVENT_ID})`);
      console.log(`   Event Type: ${rsvpEvent.eventType}`);
      console.log(`   Start Date: ${rsvpEvent.startDate}`);
      console.log(`   Capacity: ${rsvpEvent.capacity}`);
    } catch (error) {
      console.error('❌ Failed to find RSVP event for testing:', error);
      throw error;
    }
  });

  test.afterAll(async () => {
    await globalCleanup();
  });

  test('should persist RSVP to database', async ({ page }) => {
    // RSVP_EVENT_ID is guaranteed to exist from beforeAll hook

    const userId = await DatabaseHelpers.getUserIdFromEmail(AuthHelpers.accounts.vetted.email);

    // Ensure user does NOT have RSVP yet (cancel if exists)
    try {
      const participation = await DatabaseHelpers.verifyEventParticipation(
        userId,
        RSVP_EVENT_ID,
        1  // 1 = Active (ParticipationStatus enum)
      );

      // Cancel existing RSVP
      console.log('Cancelling existing RSVP for clean test...');
      await testCancelRsvpPersistence(page, {
        userEmail: AuthHelpers.accounts.vetted.email,
        userPassword: AuthHelpers.accounts.vetted.password,
        eventId: RSVP_EVENT_ID,
      });
    } catch {
      console.log('✅ No existing RSVP to clean up');
    }

    // Now test RSVP persistence
    await testRsvpPersistence(page, {
      userEmail: AuthHelpers.accounts.vetted.email,
      userPassword: AuthHelpers.accounts.vetted.password,
      eventId: RSVP_EVENT_ID,
      successMessage: 'RSVP successful',
      screenshotPath: '/tmp/rsvp-test',
    });

    console.log('✅ RSVP persisted correctly to database');
  });

  test('should persist RSVP cancellation to database', async ({ page }) => {
    // RSVP_EVENT_ID is guaranteed to exist from beforeAll hook

    const userId = await DatabaseHelpers.getUserIdFromEmail(AuthHelpers.accounts.member.email);

    // Ensure user HAS an RSVP to cancel
    try {
      await DatabaseHelpers.verifyEventParticipation(userId, RSVP_EVENT_ID, 1);  // 1 = Active
      console.log('✅ User has active RSVP');
    } catch {
      // RSVP first
      console.log('Creating RSVP for cancellation test...');
      await testRsvpPersistence(page, {
        userEmail: AuthHelpers.accounts.member.email,
        userPassword: AuthHelpers.accounts.member.password,
        eventId: RSVP_EVENT_ID,
      });
    }

    // Now test cancellation persistence
    await testCancelRsvpPersistence(page, {
      userEmail: AuthHelpers.accounts.member.email,
      userPassword: AuthHelpers.accounts.member.password,
      eventId: RSVP_EVENT_ID,
      successMessage: 'RSVP cancelled',
      screenshotPath: '/tmp/cancel-rsvp-test',
    });

    console.log('✅ RSVP cancellation persisted correctly to database');
  });

  test('should handle complete RSVP lifecycle', async ({ page }) => {
    // RSVP_EVENT_ID is guaranteed to exist from beforeAll hook

    // Test: RSVP → Cancel → Re-RSVP
    // Each step verifies database persistence
    await testRsvpLifecycle(
      page,
      AuthHelpers.accounts.guest.email,
      AuthHelpers.accounts.guest.password,
      RSVP_EVENT_ID
    );

    console.log('✅ Complete RSVP lifecycle verified');
  });

  test('should verify RSVP type in database is correct', async ({ page }) => {
    // RSVP_EVENT_ID is guaranteed to exist from beforeAll hook

    const userId = await DatabaseHelpers.getUserIdFromEmail(AuthHelpers.accounts.vetted.email);

    // Ensure user has RSVP
    try {
      await DatabaseHelpers.verifyEventParticipation(userId, RSVP_EVENT_ID, 1);  // 1 = Active
    } catch {
      // Create RSVP
      await testRsvpPersistence(page, {
        userEmail: AuthHelpers.accounts.vetted.email,
        userPassword: AuthHelpers.accounts.vetted.password,
        eventId: RSVP_EVENT_ID,
      });
    }

    // Get participation record
    const participation = await DatabaseHelpers.verifyEventParticipation(
      userId,
      RSVP_EVENT_ID,
      1  // 1 = Active
    );

    // Verify participation type is RSVP (not Ticket)
    expect(participation.participationType).toBe('RSVP');
    console.log('✅ Participation type correctly set to RSVP');
  });

  test('should create audit log entry for RSVP', async ({ page }) => {
    // RSVP_EVENT_ID is guaranteed to exist from beforeAll hook

    const userId = await DatabaseHelpers.getUserIdFromEmail(AuthHelpers.accounts.member.email);

    // Cancel existing RSVP if any
    try {
      await testCancelRsvpPersistence(page, {
        userEmail: AuthHelpers.accounts.member.email,
        userPassword: AuthHelpers.accounts.member.password,
        eventId: RSVP_EVENT_ID,
      });
    } catch {
      // No RSVP to cancel
    }

    // Create new RSVP
    await testRsvpPersistence(page, {
      userEmail: AuthHelpers.accounts.member.email,
      userPassword: AuthHelpers.accounts.member.password,
      eventId: RSVP_EVENT_ID,
    });

    // Verify audit log
    const participation = await DatabaseHelpers.verifyEventParticipation(
      userId,
      RSVP_EVENT_ID,
      1  // 1 = Active
    );

    const auditLogExists = await DatabaseHelpers.verifyAuditLogExists(
      'AttendanceHistory',
      participation.id,
      'Registered'
    );

    expect(auditLogExists).toBeTruthy();
    console.log('✅ RSVP audit log entry created');
  });

  test('should create audit log entry for RSVP cancellation', async ({ page }) => {
    // RSVP_EVENT_ID is guaranteed to exist from beforeAll hook

    const userId = await DatabaseHelpers.getUserIdFromEmail(AuthHelpers.accounts.guest.email);

    // Ensure user has RSVP
    try {
      await DatabaseHelpers.verifyEventParticipation(userId, RSVP_EVENT_ID, 1);  // 1 = Active
    } catch {
      await testRsvpPersistence(page, {
        userEmail: AuthHelpers.accounts.guest.email,
        userPassword: AuthHelpers.accounts.guest.password,
        eventId: RSVP_EVENT_ID,
      });
    }

    // Cancel RSVP
    await testCancelRsvpPersistence(page, {
      userEmail: AuthHelpers.accounts.guest.email,
      userPassword: AuthHelpers.accounts.guest.password,
      eventId: RSVP_EVENT_ID,
    });

    // Verify cancellation audit log
    const participation = await DatabaseHelpers.verifyEventParticipation(
      userId,
      RSVP_EVENT_ID,
      2  // 2 = Cancelled
    );

    const auditLogExists = await DatabaseHelpers.verifyAuditLogExists(
      'AttendanceHistory',
      participation.id,
      'Cancelled'
    );

    expect(auditLogExists).toBeTruthy();
    console.log('✅ RSVP cancellation audit log entry created');
  });

  test('should prevent duplicate RSVPs', async ({ page }) => {
    // RSVP_EVENT_ID is guaranteed to exist from beforeAll hook

    const userId = await DatabaseHelpers.getUserIdFromEmail(AuthHelpers.accounts.member.email);

    // Ensure user has RSVP
    try {
      await DatabaseHelpers.verifyEventParticipation(userId, RSVP_EVENT_ID, 1);  // 1 = Active
    } catch {
      await testRsvpPersistence(page, {
        userEmail: AuthHelpers.accounts.member.email,
        userPassword: AuthHelpers.accounts.member.password,
        eventId: RSVP_EVENT_ID,
      });
    }

    // Navigate to event page
    await AuthHelpers.loginAs(page, 'member');

    await page.goto(`http://localhost:5173/events/${RSVP_EVENT_ID}`);
    await page.waitForLoadState('networkidle');

    // RSVP button should NOT be visible (user already has RSVP)
    const rsvpButton = page.locator('button:has-text("RSVP")').first();
    const isVisible = await rsvpButton.isVisible().catch(() => false);

    if (isVisible) {
      // Button might be disabled instead of hidden
      const isDisabled = await rsvpButton.getAttribute('disabled');
      expect(isDisabled).toBeTruthy();
      console.log('✅ RSVP button is disabled (user already has RSVP)');
    } else {
      console.log('✅ RSVP button not visible (user already has RSVP)');
    }

    // Cancel RSVP button SHOULD be visible
    const cancelButton = page.locator('button:has-text("Cancel RSVP"), button:has-text("Withdraw")');
    await expect(cancelButton).toBeVisible({ timeout: 5000 });
    console.log('✅ Cancel RSVP button visible instead');
  });
});

test.describe('RSVP Persistence Edge Cases', () => {
  test.afterAll(async () => {
    await globalCleanup();
  });

  test('should handle rapid RSVP/cancel cycles', async ({ page }) => {
    // RSVP_EVENT_ID is guaranteed to exist from beforeAll hook

    // Test multiple rapid RSVP/cancel cycles
    for (let i = 0; i < 3; i++) {
      console.log(`Cycle ${i + 1}: RSVP → Cancel`);

      await testRsvpPersistence(page, {
        userEmail: AuthHelpers.accounts.vetted.email,
        userPassword: AuthHelpers.accounts.vetted.password,
        eventId: RSVP_EVENT_ID,
      });

      await testCancelRsvpPersistence(page, {
        userEmail: AuthHelpers.accounts.vetted.email,
        userPassword: AuthHelpers.accounts.vetted.password,
        eventId: RSVP_EVENT_ID,
      });
    }

    console.log('✅ Rapid RSVP/cancel cycles handled correctly');
  });

  test('should maintain separate RSVP state per user', async ({ page }) => {
    // RSVP_EVENT_ID is guaranteed to exist from beforeAll hook

    // User 1: RSVP
    await testRsvpPersistence(page, {
      userEmail: AuthHelpers.accounts.vetted.email,
      userPassword: AuthHelpers.accounts.vetted.password,
      eventId: RSVP_EVENT_ID,
    });

    // User 2: Should be able to RSVP independently
    await testRsvpPersistence(page, {
      userEmail: AuthHelpers.accounts.member.email,
      userPassword: AuthHelpers.accounts.member.password,
      eventId: RSVP_EVENT_ID,
    });

    // Verify both users have separate RSVP records
    const userId1 = await DatabaseHelpers.getUserIdFromEmail(AuthHelpers.accounts.vetted.email);
    const userId2 = await DatabaseHelpers.getUserIdFromEmail(AuthHelpers.accounts.member.email);

    const rsvp1 = await DatabaseHelpers.verifyEventParticipation(
      userId1,
      RSVP_EVENT_ID,
      1  // 1 = Active
    );

    const rsvp2 = await DatabaseHelpers.verifyEventParticipation(
      userId2,
      RSVP_EVENT_ID,
      1  // 1 = Active
    );

    expect(rsvp1.id).not.toBe(rsvp2.id);
    console.log('✅ Users have separate RSVP records');
  });
});
