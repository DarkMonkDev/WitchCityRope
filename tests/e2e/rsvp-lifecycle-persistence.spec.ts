/**
 * RSVP Lifecycle Persistence E2E Test (DataFactory Migration)
 *
 * Verifies RSVP operations persist correctly to database.
 * Similar pattern to ticket cancellation but for free events.
 *
 * Test Pattern:
 * - RSVP → Verify persistence
 * - Cancel RSVP → Verify persistence
 * - Re-RSVP → Verify persistence
 *
 * MIGRATION NOTES:
 * - Uses df (DataFactory) fixture for automatic cleanup
 * - Creates test data via TestHelper API endpoints
 * - No need for manual API calls or cleanup logic
 * - Data is automatically cleaned up after each test
 */

import { expect } from '@playwright/test';
import { test } from '../lib/datafactory/fixtures/test.fixture';
import {
  testRsvpPersistence,
  testCancelRsvpPersistence,
  testRsvpLifecycle,
} from './templates/rsvp-persistence-template';
import { DatabaseHelpers } from './utils/database-helpers';
import { AuthHelpers } from './test-utils/helpers/auth.helpers';

test.describe.serial('RSVP Lifecycle Persistence Tests', () => {
  test('should persist RSVP to database', async ({ page, df }) => {
    // Create test event with free RSVP using DataFactory
    const event = await df.events.createPublished(`RSVP Test ${Date.now()}`);

    const sessionStart = new Date();
    sessionStart.setDate(sessionStart.getDate() + 7);
    sessionStart.setHours(18, 0, 0, 0);
    const sessionEnd = new Date(sessionStart.getTime() + 3 * 60 * 60 * 1000);

    const session = await df.sessions.create({
      eventId: event.id,
      title: 'Free Session',
      startTime: sessionStart,
      endTime: sessionEnd,
      maxCapacity: 20,
    });

    const ticketType = await df.ticketTypes.create({
      sessionId: session.id,
      name: 'Free RSVP',
      price: 0, // Free
      quantityAvailable: 20,
    });

    await testRsvpPersistence(page, {
      userEmail: AuthHelpers.accounts.vetted.email,
      userPassword: AuthHelpers.accounts.vetted.password,
      eventId: event.id,
      successMessage: 'RSVP successful',
      screenshotPath: '/tmp/rsvp-test',
    });

    console.log('✅ RSVP persisted correctly to database');
  });

  test('should persist RSVP cancellation to database', async ({ page, df }) => {
    // Create test event with free RSVP using DataFactory
    const event = await df.events.createPublished(`Cancel RSVP Test ${Date.now()}`);

    const sessionStart = new Date();
    sessionStart.setDate(sessionStart.getDate() + 7);
    sessionStart.setHours(18, 0, 0, 0);
    const sessionEnd = new Date(sessionStart.getTime() + 3 * 60 * 60 * 1000);

    const session = await df.sessions.create({
      eventId: event.id,
      title: 'Free Session',
      startTime: sessionStart,
      endTime: sessionEnd,
      maxCapacity: 20,
    });

    const ticketType = await df.ticketTypes.create({
      sessionId: session.id,
      name: 'Free RSVP',
      price: 0,
      quantityAvailable: 20,
    });

    const userId = await DatabaseHelpers.getUserIdFromEmail(AuthHelpers.accounts.teacher.email);

    // Ensure user HAS an RSVP to cancel
    try {
      await DatabaseHelpers.verifyEventParticipation(userId, event.id, 1);  // 1 = Active
      console.log('✅ User has active RSVP');
    } catch {
      // RSVP first
      console.log('Creating RSVP for cancellation test...');
      await testRsvpPersistence(page, {
        userEmail: AuthHelpers.accounts.teacher.email,
        userPassword: AuthHelpers.accounts.teacher.password,
        eventId: event.id,
      });
    }

    // Now test cancellation persistence
    await testCancelRsvpPersistence(page, {
      userEmail: AuthHelpers.accounts.teacher.email,
      userPassword: AuthHelpers.accounts.teacher.password,
      eventId: event.id,
      successMessage: 'RSVP cancelled',
      screenshotPath: '/tmp/cancel-rsvp-test',
    });

    console.log('✅ RSVP cancellation persisted correctly to database');
  });

  test('should handle complete RSVP lifecycle', async ({ page, df }) => {
    // Create test event with free RSVP using DataFactory
    const event = await df.events.createPublished(`Lifecycle Test ${Date.now()}`);

    const sessionStart = new Date();
    sessionStart.setDate(sessionStart.getDate() + 7);
    sessionStart.setHours(18, 0, 0, 0);
    const sessionEnd = new Date(sessionStart.getTime() + 3 * 60 * 60 * 1000);

    const session = await df.sessions.create({
      eventId: event.id,
      title: 'Free Session',
      startTime: sessionStart,
      endTime: sessionEnd,
      maxCapacity: 20,
    });

    const ticketType = await df.ticketTypes.create({
      sessionId: session.id,
      name: 'Free RSVP',
      price: 0,
      quantityAvailable: 20,
    });

    // Test: RSVP → Cancel → Re-RSVP
    // Each step verifies database persistence
    await testRsvpLifecycle(
      page,
      AuthHelpers.accounts.guest.email,
      AuthHelpers.accounts.guest.password,
      event.id
    );

    console.log('✅ Complete RSVP lifecycle verified');
  });

  test('should verify RSVP type in database is correct', async ({ page, df }) => {
    // Create test event with free RSVP using DataFactory
    const event = await df.events.createPublished(`Type Verify Test ${Date.now()}`);

    const sessionStart = new Date();
    sessionStart.setDate(sessionStart.getDate() + 7);
    sessionStart.setHours(18, 0, 0, 0);
    const sessionEnd = new Date(sessionStart.getTime() + 3 * 60 * 60 * 1000);

    const session = await df.sessions.create({
      eventId: event.id,
      title: 'Free Session',
      startTime: sessionStart,
      endTime: sessionEnd,
      maxCapacity: 20,
    });

    const ticketType = await df.ticketTypes.create({
      sessionId: session.id,
      name: 'Free RSVP',
      price: 0,
      quantityAvailable: 20,
    });

    const userId = await DatabaseHelpers.getUserIdFromEmail(AuthHelpers.accounts.vetted.email);

    // Ensure user has RSVP
    try {
      await DatabaseHelpers.verifyEventParticipation(userId, event.id, 1);  // 1 = Active
    } catch {
      // Create RSVP
      await testRsvpPersistence(page, {
        userEmail: AuthHelpers.accounts.vetted.email,
        userPassword: AuthHelpers.accounts.vetted.password,
        eventId: event.id,
      });
    }

    // Get participation record
    const participation = await DatabaseHelpers.verifyEventParticipation(
      userId,
      event.id,
      1  // 1 = Active
    );

    // Verify participation type is RSVP (not Ticket)
    expect(participation).not.toBeNull();
    expect(participation!.participationType).toBe('RSVP');
    console.log('✅ Participation type correctly set to RSVP');
  });

  test('should create audit log entry for RSVP', async ({ page, df }) => {
    // Create test event
    const event = await df.events.createPublished(`Audit Log Test ${Date.now()}`);

    const sessionStart = new Date();
    sessionStart.setDate(sessionStart.getDate() + 7);
    sessionStart.setHours(18, 0, 0, 0);
    const sessionEnd = new Date(sessionStart.getTime() + 3 * 60 * 60 * 1000);

    const session = await df.sessions.create({
      eventId: event.id,
      title: 'Free Session',
      startTime: sessionStart,
      endTime: sessionEnd,
      maxCapacity: 20,
    });

    const ticketType = await df.ticketTypes.create({
      sessionId: session.id,
      name: 'Free RSVP',
      price: 0,
      quantityAvailable: 20,
    });

    const userId = await DatabaseHelpers.getUserIdFromEmail(AuthHelpers.accounts.teacher.email);

    // Create new RSVP
    await testRsvpPersistence(page, {
      userEmail: AuthHelpers.accounts.teacher.email,
      userPassword: AuthHelpers.accounts.teacher.password,
      eventId: event.id,
    });

    // Verify audit log
    const participation = await DatabaseHelpers.verifyEventParticipation(
      userId,
      event.id,
      1  // 1 = Active
    );

    expect(participation).not.toBeNull();
    const auditLogExists = await DatabaseHelpers.verifyAuditLogExists(
      'AttendanceHistory',
      participation!.id,
      'Registered'
    );

    expect(auditLogExists).toBeTruthy();
    console.log('✅ RSVP audit log entry created');
  });

  test('should create audit log entry for RSVP cancellation', async ({ page, df }) => {
    // Create test event
    const event = await df.events.createPublished(`Cancel Audit Test ${Date.now()}`);

    const sessionStart = new Date();
    sessionStart.setDate(sessionStart.getDate() + 7);
    sessionStart.setHours(18, 0, 0, 0);
    const sessionEnd = new Date(sessionStart.getTime() + 3 * 60 * 60 * 1000);

    const session = await df.sessions.create({
      eventId: event.id,
      title: 'Free Session',
      startTime: sessionStart,
      endTime: sessionEnd,
      maxCapacity: 20,
    });

    const ticketType = await df.ticketTypes.create({
      sessionId: session.id,
      name: 'Free RSVP',
      price: 0,
      quantityAvailable: 20,
    });

    const userId = await DatabaseHelpers.getUserIdFromEmail(AuthHelpers.accounts.guest.email);

    // Ensure user has RSVP
    try {
      await DatabaseHelpers.verifyEventParticipation(userId, event.id, 1);  // 1 = Active
    } catch {
      await testRsvpPersistence(page, {
        userEmail: AuthHelpers.accounts.guest.email,
        userPassword: AuthHelpers.accounts.guest.password,
        eventId: event.id,
      });
    }

    // Cancel RSVP
    await testCancelRsvpPersistence(page, {
      userEmail: AuthHelpers.accounts.guest.email,
      userPassword: AuthHelpers.accounts.guest.password,
      eventId: event.id,
    });

    // Verify cancellation audit log
    const participation = await DatabaseHelpers.verifyEventParticipation(
      userId,
      event.id,
      2  // 2 = Cancelled
    );

    expect(participation).not.toBeNull();
    const auditLogExists = await DatabaseHelpers.verifyAuditLogExists(
      'AttendanceHistory',
      participation!.id,
      'Cancelled'
    );

    expect(auditLogExists).toBeTruthy();
    console.log('✅ RSVP cancellation audit log entry created');
  });

  test('should prevent duplicate RSVPs', async ({ page, df }) => {
    // Create test event
    const event = await df.events.createPublished(`Duplicate Test ${Date.now()}`);

    const sessionStart = new Date();
    sessionStart.setDate(sessionStart.getDate() + 7);
    sessionStart.setHours(18, 0, 0, 0);
    const sessionEnd = new Date(sessionStart.getTime() + 3 * 60 * 60 * 1000);

    const session = await df.sessions.create({
      eventId: event.id,
      title: 'Free Session',
      startTime: sessionStart,
      endTime: sessionEnd,
      maxCapacity: 20,
    });

    const ticketType = await df.ticketTypes.create({
      sessionId: session.id,
      name: 'Free RSVP',
      price: 0,
      quantityAvailable: 20,
    });

    const userId = await DatabaseHelpers.getUserIdFromEmail(AuthHelpers.accounts.teacher.email);

    // Ensure user has RSVP
    try {
      await DatabaseHelpers.verifyEventParticipation(userId, event.id, 1);  // 1 = Active
    } catch {
      await testRsvpPersistence(page, {
        userEmail: AuthHelpers.accounts.teacher.email,
        userPassword: AuthHelpers.accounts.teacher.password,
        eventId: event.id,
      });
    }

    // Navigate to event page
    await AuthHelpers.loginAs(page, 'teacher');

    await page.goto(`/events/${event.id}`, { waitUntil: 'domcontentloaded' });
    await page.waitForLoadState('domcontentloaded');

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

test.describe.serial('RSVP Persistence Edge Cases', () => {
  test('should handle rapid RSVP/cancel cycles', async ({ page, df }) => {
    // Create test event
    const event = await df.events.createPublished(`Rapid Cycle Test ${Date.now()}`);

    const sessionStart = new Date();
    sessionStart.setDate(sessionStart.getDate() + 7);
    sessionStart.setHours(18, 0, 0, 0);
    const sessionEnd = new Date(sessionStart.getTime() + 3 * 60 * 60 * 1000);

    const session = await df.sessions.create({
      eventId: event.id,
      title: 'Free Session',
      startTime: sessionStart,
      endTime: sessionEnd,
      maxCapacity: 20,
    });

    const ticketType = await df.ticketTypes.create({
      sessionId: session.id,
      name: 'Free RSVP',
      price: 0,
      quantityAvailable: 20,
    });

    // Test multiple rapid RSVP/cancel cycles (2 cycles to stay within 90s timeout)
    for (let i = 0; i < 2; i++) {
      console.log(`Cycle ${i + 1}: RSVP → Cancel`);

      await testRsvpPersistence(page, {
        userEmail: AuthHelpers.accounts.teacher.email,
        userPassword: AuthHelpers.accounts.teacher.password,
        eventId: event.id,
      });

      await testCancelRsvpPersistence(page, {
        userEmail: AuthHelpers.accounts.teacher.email,
        userPassword: AuthHelpers.accounts.teacher.password,
        eventId: event.id,
      });
    }

    console.log('✅ Rapid RSVP/cancel cycles handled correctly');
  });

  test('should maintain separate RSVP state per user', async ({ page, df }) => {
    // Create test event
    const event = await df.events.createPublished(`Multi User Test ${Date.now()}`);

    const sessionStart = new Date();
    sessionStart.setDate(sessionStart.getDate() + 7);
    sessionStart.setHours(18, 0, 0, 0);
    const sessionEnd = new Date(sessionStart.getTime() + 3 * 60 * 60 * 1000);

    const session = await df.sessions.create({
      eventId: event.id,
      title: 'Free Session',
      startTime: sessionStart,
      endTime: sessionEnd,
      maxCapacity: 20,
    });

    const ticketType = await df.ticketTypes.create({
      sessionId: session.id,
      name: 'Free RSVP',
      price: 0,
      quantityAvailable: 20,
    });

    // User 1: RSVP
    await testRsvpPersistence(page, {
      userEmail: AuthHelpers.accounts.admin.email,
      userPassword: AuthHelpers.accounts.admin.password,
      eventId: event.id,
    });

    // User 2: Should be able to RSVP independently
    await testRsvpPersistence(page, {
      userEmail: AuthHelpers.accounts.teacher.email,
      userPassword: AuthHelpers.accounts.teacher.password,
      eventId: event.id,
    });

    // Verify both users have separate RSVP records
    const userId1 = await DatabaseHelpers.getUserIdFromEmail(AuthHelpers.accounts.admin.email);
    const userId2 = await DatabaseHelpers.getUserIdFromEmail(AuthHelpers.accounts.teacher.email);

    const rsvp1 = await DatabaseHelpers.verifyEventParticipation(
      userId1,
      event.id,
      1  // 1 = Active
    );

    const rsvp2 = await DatabaseHelpers.verifyEventParticipation(
      userId2,
      event.id,
      1  // 1 = Active
    );

    expect(rsvp1).not.toBeNull();
    expect(rsvp2).not.toBeNull();
    expect(rsvp1!.id).not.toBe(rsvp2!.id);
    console.log('✅ Users have separate RSVP records');
  });
});
