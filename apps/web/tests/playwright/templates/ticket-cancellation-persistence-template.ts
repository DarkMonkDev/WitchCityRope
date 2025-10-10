/**
 * Ticket Cancellation Persistence Test Template
 *
 * CRITICAL BUG THIS CATCHES: Ticket cancellation bug where:
 * - User clicks "Cancel Ticket"
 * - UI shows cancellation success
 * - Database is NOT updated (EventParticipation still shows as active)
 * - Page refresh shows ticket still active
 *
 * ROOT CAUSE: Frontend called wrong endpoint (/ticket instead of /participation)
 *
 * This template ensures ticket cancellations actually persist to database.
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
// TICKET CANCELLATION TEST CONFIG
// ============================================================================

export interface TicketCancellationTestConfig {
  /** User credentials for login */
  userEmail: string;
  userPassword: string;
  /** Event ID to cancel ticket for */
  eventId: string;
  /** Optional: Cancellation reason */
  cancellationReason?: string;
  /** Optional: Expected success message */
  successMessage?: string;
  /** Optional: Screenshot path */
  screenshotPath?: string;
}

/**
 * Create a complete ticket cancellation persistence test
 *
 * Test Pattern:
 * 1. Login as user
 * 2. Verify user has active ticket for event
 * 3. Navigate to event details
 * 4. Click "Cancel Ticket" button
 * 5. Confirm cancellation (if modal appears)
 * 6. Verify UI shows success
 * 7. Verify API returned 204 No Content or 200 OK
 * 8. Verify database shows status = 'Cancelled'
 * 9. Refresh page
 * 10. Verify "Cancel Ticket" button is gone (persistence)
 * 11. Verify database still shows 'Cancelled'
 */
export async function testTicketCancellationPersistence(
  page: Page,
  testConfig: TicketCancellationTestConfig
): Promise<void> {
  const {
    userEmail,
    userPassword,
    eventId,
    cancellationReason,
    successMessage,
    screenshotPath,
  } = testConfig;

  let userId: string;

  const config: TestConfig = {
    description: `Ticket cancellation persistence for ${userEmail} on event ${eventId}`,

    // STEP 1: Setup - Login, verify ticket exists, navigate to event
    setup: async (page: Page) => {
      // Login
      await page.goto('http://localhost:5173/login');
      await page.waitForLoadState('networkidle');

      await page.locator('[data-testid="email-input"]').fill(userEmail);
      await page.locator('[data-testid="password-input"]').fill(userPassword);
      await page.locator('[data-testid="login-button"]').click();

      // Wait for dashboard
      await page.waitForURL('**/dashboard', { timeout: 10000 });
      await page.waitForLoadState('networkidle');

      // Get user ID for database verification
      userId = await DatabaseHelpers.getUserIdFromEmail(userEmail);
      console.log(`✅ Logged in as ${userEmail} (ID: ${userId})`);

      // Verify user has active ticket/participation for this event
      const participation = await DatabaseHelpers.verifyEventParticipation(
        userId,
        eventId,
        1 // 1 = Active
      );
      console.log(`✅ User has active ${participation.participationType} for event`);

      // Navigate to event details
      await page.goto(`http://localhost:5173/events/${eventId}`);
      await page.waitForLoadState('networkidle');
      console.log('✅ Navigated to event details');

      // Verify Cancel Ticket button is visible
      const cancelButton = page.locator(
        '[data-testid="cancel-ticket-button"], button:has-text("Cancel Ticket"), button:has-text("Cancel Registration")'
      );

      await expect(cancelButton).toBeVisible({ timeout: 5000 });
      console.log('✅ Cancel Ticket button is visible');
    },

    // STEP 2: Action - Click cancel ticket and confirm
    action: async (page: Page) => {
      // Click "Cancel Ticket" button
      const cancelButton = page.locator(
        '[data-testid="cancel-ticket-button"], button:has-text("Cancel Ticket"), button:has-text("Cancel Registration")'
      ).first();

      await cancelButton.click();
      console.log('✅ Clicked Cancel Ticket button');

      // Wait for confirmation modal (if it exists)
      await page.waitForTimeout(500);

      // Check for confirmation modal
      const confirmModal = page.locator(
        '[role="dialog"], .modal, [data-testid*="confirm"]'
      );

      if (await confirmModal.count() > 0 && await confirmModal.isVisible()) {
        console.log('✅ Confirmation modal appeared');

        // Fill in cancellation reason if field exists and reason provided
        if (cancellationReason) {
          const reasonInput = page.locator(
            '[data-testid="cancellation-reason"], textarea[name="reason"], input[name="reason"]'
          );

          if (await reasonInput.count() > 0) {
            await reasonInput.fill(cancellationReason);
            console.log(`✅ Filled cancellation reason: ${cancellationReason}`);
          }
        }

        // Click confirm button in modal
        const confirmButton = page.locator(
          'button:has-text("Confirm"), button:has-text("Yes"), button:has-text("Cancel Ticket")'
        ).last(); // Use last() in case there are multiple

        await confirmButton.click();
        console.log('✅ Clicked confirmation button');
      }
    },

    // STEP 3: Verify UI shows success
    verifyUiSuccess: async (page: Page) => {
      // Verify success message
      await verifySuccessMessage(page, successMessage);
      await verifyNoErrorMessage(page);

      // Verify Cancel Ticket button is gone or disabled
      const cancelButton = page.locator(
        '[data-testid="cancel-ticket-button"], button:has-text("Cancel Ticket")'
      );

      const buttonCount = await cancelButton.count();

      if (buttonCount > 0) {
        // Button might still exist but be disabled
        const isDisabled = await cancelButton.getAttribute('disabled');
        if (isDisabled === null) {
          console.warn('⚠️  Cancel Ticket button still visible and enabled');
        }
      } else {
        console.log('✅ Cancel Ticket button removed from UI');
      }

      // Optional: Verify "Purchase Ticket" or "Register" button appears
      // (indicates user can re-register)
      const purchaseButton = page.locator(
        'button:has-text("Purchase Ticket"), button:has-text("Register"), button:has-text("RSVP")'
      );

      if (await purchaseButton.count() > 0) {
        console.log('✅ Purchase/Register button now visible');
      }
    },

    // STEP 4: Verify API response
    verifyApiResponse: async (response) => {
      await verifyApiSuccess(response);

      // Verify status is 204 No Content or 200 OK
      const status = response.status();
      if (status !== 200 && status !== 204) {
        throw new Error(`Expected 200 or 204 but got ${status}`);
      }

      console.log(`✅ API returned ${status} (success)`);
    },

    // STEP 5: Verify database shows cancellation (CRITICAL)
    verifyDatabaseState: async (page: Page) => {
      // Verify participation status is 'Cancelled'
      const participation = await DatabaseHelpers.verifyEventParticipation(
        userId,
        eventId,
        2 // 2 = Cancelled
      );

      console.log('✅ Database shows participation status: Cancelled');
      console.log(`   Participation ID: ${participation.id}`);
      console.log(`   Updated at: ${participation.updatedAt}`);

      // Optional: Verify audit log entry exists
      const auditLogExists = await DatabaseHelpers.verifyAuditLogExists(
        'ParticipationHistory',
        participation.id,
        'Cancelled'
      );

      if (auditLogExists) {
        console.log('✅ Audit log entry created for cancellation');
      }
    },

    // STEP 6: Verify persistence after refresh (CRITICAL)
    verifyPersistence: async (page: Page) => {
      // After refresh, Cancel Ticket button should NOT reappear
      const cancelButton = page.locator(
        '[data-testid="cancel-ticket-button"], button:has-text("Cancel Ticket")'
      );

      const buttonCount = await cancelButton.count();

      if (buttonCount > 0) {
        // Take screenshot for debugging
        await page.screenshot({ path: `${screenshotPath}/BUG-cancel-button-reappeared.png` });

        throw new Error(
          'BUG DETECTED: Cancel Ticket button reappeared after refresh! ' +
          'This means the cancellation did NOT persist to database.'
        );
      }

      console.log('✅ Cancel Ticket button still gone after refresh');

      // Verify Purchase/Register button is visible (user can re-register)
      const purchaseButton = page.locator(
        'button:has-text("Purchase Ticket"), button:has-text("Register"), button:has-text("RSVP")'
      );

      await expect(purchaseButton).toBeVisible({ timeout: 5000 });
      console.log('✅ Purchase/Register button visible (user can re-register)');
    },
  };

  const options: PersistenceTestOptions = {
    screenshotPath: screenshotPath || '/tmp/ticket-cancellation-test',
    apiEndpoint: /\/api\/events\/.*\/(participation|ticket|rsvp)/i,
    apiMethod: 'DELETE',
    waitAfterAction: 1500, // Wait for API call to complete
  };

  await PersistenceTestTemplate.runTest(page, config, options);
}

// ============================================================================
// TICKET LIFECYCLE TESTS
// ============================================================================

/**
 * Test complete ticket lifecycle: Purchase → Cancel → Re-purchase
 */
export async function testTicketLifecycle(
  page: Page,
  userEmail: string,
  userPassword: string,
  eventId: string
): Promise<void> {
  let userId: string;

  console.log('\n=== TICKET LIFECYCLE TEST ===\n');

  // Step 1: Login
  console.log('Step 1: Login...');
  await page.goto('http://localhost:5173/login');
  await page.waitForLoadState('networkidle');

  await page.locator('[data-testid="email-input"]').fill(userEmail);
  await page.locator('[data-testid="password-input"]').fill(userPassword);
  await page.locator('[data-testid="login-button"]').click();

  await page.waitForURL('**/dashboard', { timeout: 10000 });
  await page.waitForLoadState('networkidle');

  userId = await DatabaseHelpers.getUserIdFromEmail(userEmail);

  // Step 2: Purchase ticket
  console.log('Step 2: Purchase ticket...');
  await page.goto(`http://localhost:5173/events/${eventId}`);
  await page.waitForLoadState('networkidle');

  const purchaseButton = page.locator(
    'button:has-text("Purchase Ticket"), button:has-text("Register")'
  ).first();

  if (await purchaseButton.count() > 0) {
    await purchaseButton.click();
    await page.waitForLoadState('networkidle');

    // Verify ticket in database
    await DatabaseHelpers.verifyEventParticipation(userId, eventId, 1); // 1 = Active
    console.log('✅ Ticket purchased and persisted to database');
  } else {
    console.log('⚠️  User already has ticket, skipping purchase');
  }

  // Step 3: Cancel ticket (using template)
  console.log('Step 3: Cancel ticket...');
  await testTicketCancellationPersistence(page, {
    userEmail,
    userPassword,
    eventId,
    cancellationReason: 'Lifecycle test cancellation',
  });

  // Step 4: Re-purchase ticket
  console.log('Step 4: Re-purchase ticket...');
  await page.goto(`http://localhost:5173/events/${eventId}`);
  await page.waitForLoadState('networkidle');

  const repurchaseButton = page.locator(
    'button:has-text("Purchase Ticket"), button:has-text("Register")'
  ).first();

  await expect(repurchaseButton).toBeVisible({ timeout: 5000 });
  await repurchaseButton.click();
  await page.waitForLoadState('networkidle');

  // Verify new ticket in database
  const newParticipation = await DatabaseHelpers.verifyEventParticipation(
    userId,
    eventId,
    1 // 1 = Active
  );

  console.log('✅ Ticket re-purchased successfully');
  console.log(`   New participation ID: ${newParticipation.id}`);

  console.log('\n✅ TICKET LIFECYCLE TEST PASSED\n');
}

// Export all functions
export default {
  testTicketCancellationPersistence,
  testTicketLifecycle,
};
