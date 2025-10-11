/**
 * RSVP Persistence Test Template
 *
 * Ensures RSVP actions persist correctly to database.
 * Similar to ticket cancellation but for free events (RSVPs vs paid tickets).
 *
 * Test Pattern:
 * - RSVP to event → Verify persistence
 * - Cancel RSVP → Verify persistence
 */

import { Page, expect } from '@playwright/test';
import {
  PersistenceTestTemplate,
  TestConfig,
  PersistenceTestOptions,
  verifySuccessMessage,
  verifyNoErrorMessage,
  verifyApiSuccess,
} from './persistence-test-template';
import { DatabaseHelpers } from '../utils/database-helpers';

// ============================================================================
// RSVP TEST CONFIG
// ============================================================================

export interface RsvpTestConfig {
  /** User credentials for login */
  userEmail: string;
  userPassword: string;
  /** Event ID to RSVP to */
  eventId: string;
  /** Optional: Expected success message */
  successMessage?: string;
  /** Optional: Screenshot path */
  screenshotPath?: string;
}

/**
 * Test RSVP to event persists to database
 */
export async function testRsvpPersistence(
  page: Page,
  testConfig: RsvpTestConfig
): Promise<void> {
  const { userEmail, userPassword, eventId, successMessage, screenshotPath } = testConfig;

  let userId: string;

  const config: TestConfig = {
    description: `RSVP persistence for ${userEmail} on event ${eventId}`,

    // Setup: Login and navigate to event
    setup: async (page: Page) => {
      await page.goto('http://localhost:5173/login');
      await page.waitForLoadState('networkidle');

      await page.locator('[data-testid="email-input"]').fill(userEmail);
      await page.locator('[data-testid="password-input"]').fill(userPassword);
      await page.locator('[data-testid="login-button"]').click();

      await page.waitForURL('**/dashboard', { timeout: 10000 });
      await page.waitForLoadState('networkidle');

      userId = await DatabaseHelpers.getUserIdFromEmail(userEmail);
      console.log(`✅ Logged in as ${userEmail} (ID: ${userId})`);

      // Navigate to event
      await page.goto(`http://localhost:5173/events/${eventId}`);
      await page.waitForLoadState('networkidle');
      console.log('✅ Navigated to event details');
    },

    // Action: Click RSVP button
    action: async (page: Page) => {
      const rsvpButton = page.locator(
        '[data-testid="button-rsvp"], button:has-text("RSVP Now"), button:has-text("RSVP")'
      ).first();

      await expect(rsvpButton).toBeVisible({ timeout: 5000 });
      await rsvpButton.click();
      console.log('✅ Clicked RSVP button');

      // NO CONFIRMATION MODAL for RSVP
      // RSVP happens immediately (see ParticipationCard.tsx line 293-296)
      // Wait for UI to update
      await page.waitForTimeout(1000);
    },

    // Verify UI success
    verifyUiSuccess: async (page: Page) => {
      // NO SUCCESS NOTIFICATION SHOWN
      // RSVP mutation runs silently (see useParticipation.ts lines 40-70)
      // Success is indicated by UI state change only

      await verifyNoErrorMessage(page);

      // RSVP button should change to "Cancel RSVP"
      // Wait for UI to update via React Query cache
      // Use .first() to handle multiple "Cancel RSVP" buttons (status box + potential modal)
      const cancelRsvpButton = page.locator('button:has-text("Cancel RSVP"), button:has-text("Withdraw")').first();
      await expect(cancelRsvpButton).toBeVisible({ timeout: 5000 });
      console.log('✅ RSVP button changed to Cancel RSVP');
    },

    // Verify API response
    verifyApiResponse: async (response) => {
      await verifyApiSuccess(response);
    },

    // Verify database (CRITICAL)
    verifyDatabaseState: async (page: Page) => {
      const participation = await DatabaseHelpers.verifyEventParticipation(
        userId,
        eventId,
        1  // 1 = Active (ParticipationStatus enum)
      );

      // Verify participation type is RSVP
      if (participation.participationType !== 'RSVP') {
        throw new Error(
          `Expected RSVP but got ${participation.participationType}`
        );
      }

      console.log('✅ Database shows RSVP registered');
    },

    // Verify persistence after refresh (CRITICAL)
    verifyPersistence: async (page: Page) => {
      // After refresh, Cancel RSVP button should be visible
      const cancelRsvpButton = page.locator(
        'button:has-text("Cancel RSVP"), button:has-text("Withdraw")'
      );

      await expect(cancelRsvpButton).toBeVisible({ timeout: 5000 });
      console.log('✅ Cancel RSVP button still visible after refresh');

      // RSVP button should NOT be visible
      const rsvpButton = page.locator('button:has-text("RSVP")').first();
      const rsvpCount = await rsvpButton.count();

      if (rsvpCount > 0 && await rsvpButton.isVisible()) {
        throw new Error('BUG: RSVP button reappeared after refresh!');
      }
    },
  };

  const options: PersistenceTestOptions = {
    screenshotPath: screenshotPath || '/tmp/rsvp-test',
    apiEndpoint: /\/api\/events\/.*\/(participation|rsvp)/i,
    apiMethod: 'POST',
    waitAfterAction: 1500,
  };

  await PersistenceTestTemplate.runTest(page, config, options);
}

/**
 * Test cancelling RSVP persists to database
 */
export async function testCancelRsvpPersistence(
  page: Page,
  testConfig: RsvpTestConfig
): Promise<void> {
  const { userEmail, userPassword, eventId, successMessage, screenshotPath } = testConfig;

  let userId: string;

  const config: TestConfig = {
    description: `Cancel RSVP persistence for ${userEmail} on event ${eventId}`,

    // Setup: Login, verify RSVP exists, navigate to event
    setup: async (page: Page) => {
      await page.goto('http://localhost:5173/login');
      await page.waitForLoadState('networkidle');

      await page.locator('[data-testid="email-input"]').fill(userEmail);
      await page.locator('[data-testid="password-input"]').fill(userPassword);
      await page.locator('[data-testid="login-button"]').click();

      await page.waitForURL('**/dashboard', { timeout: 10000 });
      await page.waitForLoadState('networkidle');

      userId = await DatabaseHelpers.getUserIdFromEmail(userEmail);

      // Verify user has active RSVP
      const participation = await DatabaseHelpers.verifyEventParticipation(
        userId,
        eventId,
        1  // 1 = Active (ParticipationStatus enum)
      );
      console.log(`✅ User has active ${participation.participationType}`);

      // Navigate to event
      await page.goto(`http://localhost:5173/events/${eventId}`);
      await page.waitForLoadState('networkidle');
    },

    // Action: Click Cancel RSVP button
    action: async (page: Page) => {
      const cancelButton = page.locator(
        'button:has-text("Cancel RSVP"), button:has-text("Withdraw")'
      ).first();

      await expect(cancelButton).toBeVisible({ timeout: 5000 });
      await cancelButton.click();
      console.log('✅ Clicked Cancel RSVP button');

      // CANCELLATION HAS CONFIRMATION MODAL (see ParticipationCard.tsx line 298-307)
      // Wait for modal to appear
      await page.waitForTimeout(500);

      const confirmModal = page.locator('[role="dialog"]');
      await expect(confirmModal).toBeVisible({ timeout: 5000 });

      // Click the red "Cancel RSVP" button in modal (not "Keep RSVP")
      const confirmButton = page.locator('button:has-text("Cancel RSVP")').last();
      await confirmButton.click();
      console.log('✅ Confirmed cancellation in modal');
    },

    // Verify UI success
    verifyUiSuccess: async (page: Page) => {
      // NO SUCCESS NOTIFICATION SHOWN
      // Cancel RSVP mutation runs silently (see useParticipation.ts lines 72-120)
      // Success is indicated by UI state change only

      await verifyNoErrorMessage(page);

      // RSVP button should reappear after cancellation
      const rsvpButton = page.locator('[data-testid="button-rsvp"], button:has-text("RSVP Now"), button:has-text("RSVP")');
      await expect(rsvpButton).toBeVisible({ timeout: 5000 });
      console.log('✅ RSVP button reappeared');
    },

    // Verify API response
    verifyApiResponse: async (response) => {
      await verifyApiSuccess(response);
    },

    // Verify database (CRITICAL)
    verifyDatabaseState: async (page: Page) => {
      await DatabaseHelpers.verifyEventParticipation(
        userId,
        eventId,
        2  // 2 = Cancelled (ParticipationStatus enum)
      );
      console.log('✅ Database shows RSVP cancelled');
    },

    // Verify persistence after refresh (CRITICAL)
    verifyPersistence: async (page: Page) => {
      // After refresh, RSVP button should be visible again
      const rsvpButton = page.locator('button:has-text("RSVP"), button:has-text("Register")');
      await expect(rsvpButton).toBeVisible({ timeout: 5000 });
      console.log('✅ RSVP button still visible after refresh');

      // Cancel RSVP button should NOT be visible
      const cancelButton = page.locator('button:has-text("Cancel RSVP")');
      const cancelCount = await cancelButton.count();

      if (cancelCount > 0 && await cancelButton.isVisible()) {
        throw new Error('BUG: Cancel RSVP button reappeared after refresh!');
      }
    },
  };

  const options: PersistenceTestOptions = {
    screenshotPath: screenshotPath || '/tmp/cancel-rsvp-test',
    apiEndpoint: /\/api\/events\/.*\/(participation|rsvp)/i,
    apiMethod: 'DELETE',
    waitAfterAction: 1500,
  };

  await PersistenceTestTemplate.runTest(page, config, options);
}

/**
 * Test complete RSVP lifecycle: RSVP → Cancel → Re-RSVP
 */
export async function testRsvpLifecycle(
  page: Page,
  userEmail: string,
  userPassword: string,
  eventId: string
): Promise<void> {
  console.log('\n=== RSVP LIFECYCLE TEST ===\n');

  // Step 1: RSVP to event
  console.log('Step 1: RSVP to event...');
  await testRsvpPersistence(page, { userEmail, userPassword, eventId });

  // Step 2: Cancel RSVP
  console.log('Step 2: Cancel RSVP...');
  await testCancelRsvpPersistence(page, { userEmail, userPassword, eventId });

  // Step 3: Re-RSVP
  console.log('Step 3: Re-RSVP...');
  await testRsvpPersistence(page, { userEmail, userPassword, eventId });

  console.log('\n✅ RSVP LIFECYCLE TEST PASSED\n');
}

// Export all functions
export default {
  testRsvpPersistence,
  testCancelRsvpPersistence,
  testRsvpLifecycle,
};
