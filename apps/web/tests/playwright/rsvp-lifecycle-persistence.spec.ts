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

// Test accounts
const VETTED_USER = {
  email: 'vetted@witchcityrope.com',
  password: 'Test123!',
};

const MEMBER_USER = {
  email: 'member@witchcityrope.com',
  password: 'Test123!',
};

const GUEST_USER = {
  email: 'guest@witchcityrope.com',
  password: 'Test123!',
};

// Test event IDs for RSVP events (should be Social events)
let RSVP_EVENT_ID: string;

test.describe.serial('RSVP Lifecycle Persistence Tests', () => {
  test.beforeAll(async ({ browser }) => {
    console.log('⚠️  Note: These tests require Social/Free events in the database');
    console.log('   If tests fail, ensure Social events exist via database seeding');
  });

  test.afterAll(async () => {
    await globalCleanup();
  });

  test('should find a Social event for RSVP tests', async ({ page }) => {
    console.log('🔍 Finding suitable RSVP event...');

    // STRATEGY 1: Try to get RSVP event directly from database (faster, more reliable)
    try {
      const rsvpEvent = await DatabaseHelpers.getFirstRsvpEvent();

      if (rsvpEvent) {
        RSVP_EVENT_ID = rsvpEvent.id;
        console.log(`✅ Found RSVP event via database: ${RSVP_EVENT_ID}`);
        console.log(`   Event: "${rsvpEvent.title}" (${rsvpEvent.eventType})`);
        expect(RSVP_EVENT_ID).toBeTruthy();
        return;
      }
    } catch (error) {
      console.log('⚠️  Could not query database directly, falling back to UI discovery');
    }

    // STRATEGY 2: Fallback to UI-based discovery with improved waiting
    console.log('📍 Using UI-based event discovery...');

    // Login and navigate to events page
    await page.goto('http://localhost:5173/login');
    await page.waitForLoadState('networkidle');

    await page.locator('[data-testid="email-input"]').fill(GUEST_USER.email);
    await page.locator('[data-testid="password-input"]').fill(GUEST_USER.password);
    await page.locator('[data-testid="login-button"]').click();

    await page.waitForURL('**/dashboard', { timeout: 10000 });
    await page.waitForLoadState('networkidle');

    // Navigate to events page
    await page.goto('http://localhost:5173/events');
    await page.waitForLoadState('networkidle');

    // Wait for event cards to render
    console.log('⏳ Waiting for event cards to render...');
    const eventCards = page.locator('[data-testid="event-card"]');

    try {
      await eventCards.first().waitFor({
        state: 'visible',
        timeout: 10000
      });

      const cardCount = await eventCards.count();
      console.log(`✅ Found ${cardCount} event cards`);

      // Find a Social event (free RSVP events)
      // Look for event with RSVP button or Social event type
      const socialEventCard = eventCards.filter({ hasText: 'Social' }).first();

      if (await socialEventCard.count() > 0) {
        const socialLink = socialEventCard.locator('a[href*="/events/"]');
        const href = await socialLink.getAttribute('href');
        RSVP_EVENT_ID = href?.split('/events/')[1] || '';
        console.log(`✅ Found Social event ID: ${RSVP_EVENT_ID}`);
      } else {
        // Fallback: Use first event (may not be Social)
        const firstEventCard = eventCards.first();
        const firstLink = firstEventCard.locator('a[href*="/events/"]');
        const href = await firstLink.getAttribute('href');
        RSVP_EVENT_ID = href?.split('/events/')[1] || '';
        console.log(`✅ Found event ID: ${RSVP_EVENT_ID} (may not be Social)`);
      }

      expect(RSVP_EVENT_ID).toBeTruthy();

    } catch (error) {
      throw new Error(
        'No events found on events page.\n' +
        'Check database has events: curl http://localhost:5655/api/events\n' +
        'To seed events: npm run db:seed'
      );
    }
  });

  test('should persist RSVP to database', async ({ page }) => {
    test.skip(!RSVP_EVENT_ID, 'No test event available');

    const userId = await DatabaseHelpers.getUserIdFromEmail(VETTED_USER.email);

    // Ensure user does NOT have RSVP yet (cancel if exists)
    try {
      const participation = await DatabaseHelpers.verifyEventParticipation(
        userId,
        RSVP_EVENT_ID,
        'Registered'
      );

      // Cancel existing RSVP
      console.log('Cancelling existing RSVP for clean test...');
      await testCancelRsvpPersistence(page, {
        userEmail: VETTED_USER.email,
        userPassword: VETTED_USER.password,
        eventId: RSVP_EVENT_ID,
      });
    } catch {
      console.log('✅ No existing RSVP to clean up');
    }

    // Now test RSVP persistence
    await testRsvpPersistence(page, {
      userEmail: VETTED_USER.email,
      userPassword: VETTED_USER.password,
      eventId: RSVP_EVENT_ID,
      successMessage: 'RSVP successful',
      screenshotPath: '/tmp/rsvp-test',
    });

    console.log('✅ RSVP persisted correctly to database');
  });

  test('should persist RSVP cancellation to database', async ({ page }) => {
    test.skip(!RSVP_EVENT_ID, 'No test event available');

    const userId = await DatabaseHelpers.getUserIdFromEmail(MEMBER_USER.email);

    // Ensure user HAS an RSVP to cancel
    try {
      await DatabaseHelpers.verifyEventParticipation(userId, RSVP_EVENT_ID, 'Registered');
      console.log('✅ User has active RSVP');
    } catch {
      // RSVP first
      console.log('Creating RSVP for cancellation test...');
      await testRsvpPersistence(page, {
        userEmail: MEMBER_USER.email,
        userPassword: MEMBER_USER.password,
        eventId: RSVP_EVENT_ID,
      });
    }

    // Now test cancellation persistence
    await testCancelRsvpPersistence(page, {
      userEmail: MEMBER_USER.email,
      userPassword: MEMBER_USER.password,
      eventId: RSVP_EVENT_ID,
      successMessage: 'RSVP cancelled',
      screenshotPath: '/tmp/cancel-rsvp-test',
    });

    console.log('✅ RSVP cancellation persisted correctly to database');
  });

  test('should handle complete RSVP lifecycle', async ({ page }) => {
    test.skip(!RSVP_EVENT_ID, 'No test event available');

    // Test: RSVP → Cancel → Re-RSVP
    // Each step verifies database persistence
    await testRsvpLifecycle(
      page,
      GUEST_USER.email,
      GUEST_USER.password,
      RSVP_EVENT_ID
    );

    console.log('✅ Complete RSVP lifecycle verified');
  });

  test('should verify RSVP type in database is correct', async ({ page }) => {
    test.skip(!RSVP_EVENT_ID, 'No test event available');

    const userId = await DatabaseHelpers.getUserIdFromEmail(VETTED_USER.email);

    // Ensure user has RSVP
    try {
      await DatabaseHelpers.verifyEventParticipation(userId, RSVP_EVENT_ID, 'Registered');
    } catch {
      // Create RSVP
      await testRsvpPersistence(page, {
        userEmail: VETTED_USER.email,
        userPassword: VETTED_USER.password,
        eventId: RSVP_EVENT_ID,
      });
    }

    // Get participation record
    const participation = await DatabaseHelpers.verifyEventParticipation(
      userId,
      RSVP_EVENT_ID,
      'Registered'
    );

    // Verify participation type is RSVP (not Ticket)
    expect(participation.participationType).toBe('RSVP');
    console.log('✅ Participation type correctly set to RSVP');
  });

  test('should create audit log entry for RSVP', async ({ page }) => {
    test.skip(!RSVP_EVENT_ID, 'No test event available');

    const userId = await DatabaseHelpers.getUserIdFromEmail(MEMBER_USER.email);

    // Cancel existing RSVP if any
    try {
      await testCancelRsvpPersistence(page, {
        userEmail: MEMBER_USER.email,
        userPassword: MEMBER_USER.password,
        eventId: RSVP_EVENT_ID,
      });
    } catch {
      // No RSVP to cancel
    }

    // Create new RSVP
    await testRsvpPersistence(page, {
      userEmail: MEMBER_USER.email,
      userPassword: MEMBER_USER.password,
      eventId: RSVP_EVENT_ID,
    });

    // Verify audit log
    const participation = await DatabaseHelpers.verifyEventParticipation(
      userId,
      RSVP_EVENT_ID,
      'Registered'
    );

    const auditLogExists = await DatabaseHelpers.verifyAuditLogExists(
      'ParticipationHistory',
      participation.id,
      'Registered'
    );

    expect(auditLogExists).toBeTruthy();
    console.log('✅ RSVP audit log entry created');
  });

  test('should create audit log entry for RSVP cancellation', async ({ page }) => {
    test.skip(!RSVP_EVENT_ID, 'No test event available');

    const userId = await DatabaseHelpers.getUserIdFromEmail(GUEST_USER.email);

    // Ensure user has RSVP
    try {
      await DatabaseHelpers.verifyEventParticipation(userId, RSVP_EVENT_ID, 'Registered');
    } catch {
      await testRsvpPersistence(page, {
        userEmail: GUEST_USER.email,
        userPassword: GUEST_USER.password,
        eventId: RSVP_EVENT_ID,
      });
    }

    // Cancel RSVP
    await testCancelRsvpPersistence(page, {
      userEmail: GUEST_USER.email,
      userPassword: GUEST_USER.password,
      eventId: RSVP_EVENT_ID,
    });

    // Verify cancellation audit log
    const participation = await DatabaseHelpers.verifyEventParticipation(
      userId,
      RSVP_EVENT_ID,
      'Cancelled'
    );

    const auditLogExists = await DatabaseHelpers.verifyAuditLogExists(
      'ParticipationHistory',
      participation.id,
      'Cancelled'
    );

    expect(auditLogExists).toBeTruthy();
    console.log('✅ RSVP cancellation audit log entry created');
  });

  test('should prevent duplicate RSVPs', async ({ page }) => {
    test.skip(!RSVP_EVENT_ID, 'No test event available');

    const userId = await DatabaseHelpers.getUserIdFromEmail(MEMBER_USER.email);

    // Ensure user has RSVP
    try {
      await DatabaseHelpers.verifyEventParticipation(userId, RSVP_EVENT_ID, 'Registered');
    } catch {
      await testRsvpPersistence(page, {
        userEmail: MEMBER_USER.email,
        userPassword: MEMBER_USER.password,
        eventId: RSVP_EVENT_ID,
      });
    }

    // Navigate to event page
    await page.goto('http://localhost:5173/login');
    await page.waitForLoadState('networkidle');

    await page.locator('[data-testid="email-input"]').fill(MEMBER_USER.email);
    await page.locator('[data-testid="password-input"]').fill(MEMBER_USER.password);
    await page.locator('[data-testid="login-button"]').click();

    await page.waitForURL('**/dashboard', { timeout: 10000 });
    await page.waitForLoadState('networkidle');

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
    test.skip(!RSVP_EVENT_ID, 'No test event available');

    // Test multiple rapid RSVP/cancel cycles
    for (let i = 0; i < 3; i++) {
      console.log(`Cycle ${i + 1}: RSVP → Cancel`);

      await testRsvpPersistence(page, {
        userEmail: VETTED_USER.email,
        userPassword: VETTED_USER.password,
        eventId: RSVP_EVENT_ID,
      });

      await testCancelRsvpPersistence(page, {
        userEmail: VETTED_USER.email,
        userPassword: VETTED_USER.password,
        eventId: RSVP_EVENT_ID,
      });
    }

    console.log('✅ Rapid RSVP/cancel cycles handled correctly');
  });

  test('should maintain separate RSVP state per user', async ({ page }) => {
    test.skip(!RSVP_EVENT_ID, 'No test event available');

    // User 1: RSVP
    await testRsvpPersistence(page, {
      userEmail: VETTED_USER.email,
      userPassword: VETTED_USER.password,
      eventId: RSVP_EVENT_ID,
    });

    // User 2: Should be able to RSVP independently
    await testRsvpPersistence(page, {
      userEmail: MEMBER_USER.email,
      userPassword: MEMBER_USER.password,
      eventId: RSVP_EVENT_ID,
    });

    // Verify both users have separate RSVP records
    const userId1 = await DatabaseHelpers.getUserIdFromEmail(VETTED_USER.email);
    const userId2 = await DatabaseHelpers.getUserIdFromEmail(MEMBER_USER.email);

    const rsvp1 = await DatabaseHelpers.verifyEventParticipation(
      userId1,
      RSVP_EVENT_ID,
      'Registered'
    );

    const rsvp2 = await DatabaseHelpers.verifyEventParticipation(
      userId2,
      RSVP_EVENT_ID,
      'Registered'
    );

    expect(rsvp1.id).not.toBe(rsvp2.id);
    console.log('✅ Users have separate RSVP records');
  });
});
