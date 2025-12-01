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
import { getParticipationTypeName } from '../utils/database-helpers';

// Environment-aware URLs for container/host compatibility
const WEB_BASE_URL = process.env.PLAYWRIGHT_BASE_URL || 'http://localhost:5173';
const API_BASE_URL = process.env.API_URL || 'http://localhost:5655';


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
      // Map email to role for AuthHelpers.loginAs()
      const roleMap: Record<string, keyof typeof import('../test-utils/helpers/auth.helpers').AuthHelpers.accounts> = {
        'admin@witchcityrope.com': 'admin',
        'teacher@witchcityrope.com': 'teacher',
        'vetted@witchcityrope.com': 'vetted',
        'member@witchcityrope.com': 'member',
        'guest@witchcityrope.com': 'guest'
      };

      const role = roleMap[userEmail];
      if (!role) {
        throw new Error(`Unknown user email: ${userEmail}. Expected one of: ${Object.keys(roleMap).join(', ')}`);
      }

      // Use AuthHelpers for robust login (clears auth state, handles waits properly)
      const { AuthHelpers } = await import('../test-utils/helpers/auth.helpers');
      await AuthHelpers.loginAs(page, role);

      userId = await DatabaseHelpers.getUserIdFromEmail(userEmail);
      console.log(`✅ Logged in as ${userEmail} (ID: ${userId})`);

      // DEFENSIVE DATABASE-FIRST CLEANUP: Check database state FIRST, then fix UI
      // This makes tests idempotent and race-condition resistant
      console.log('🔍 Checking database for existing participation...');

      try {
        // Check if user has ANY participation (active or cancelled) in database
        const existingActive = await DatabaseHelpers.verifyEventParticipation(userId, eventId, 1).catch(() => null);
        const existingCancelled = await DatabaseHelpers.verifyEventParticipation(userId, eventId, 2).catch(() => null);

        if (existingActive) {
          console.log(`⚠️  Database shows ACTIVE ${getParticipationTypeName(existingActive.participationType)} - cancelling via UI`);

          // Navigate and cancel through UI
          await page.goto(`${WEB_BASE_URL}/events/${eventId}`);
          await page.waitForLoadState('networkidle');
          await page.waitForTimeout(2000);

          const cancelButton = page.locator(
            'button:has-text("Cancel Ticket"), button:has-text("Cancel RSVP"), button:has-text("Withdraw")'
          ).last();

          if (await cancelButton.isVisible({ timeout: 5000 }).catch(() => false)) {
            await cancelButton.click();
            await page.waitForTimeout(500);

            const confirmModal = page.locator('[role="dialog"]');
            await confirmModal.waitFor({ state: 'visible', timeout: 5000 });

            const confirmButton = confirmModal.locator('button').filter({
              hasText: /cancel ticket|cancel rsvp|confirm|yes/i
            }).last();

            await confirmButton.click();
            await page.waitForTimeout(2000);

            // Verify cancellation in database
            await DatabaseHelpers.verifyEventParticipation(userId, eventId, 2); // 2 = Cancelled
            console.log('✅ Cancelled existing active participation');

            // CRITICAL: After cancellation, wait extra time for UI to fully reset
            console.log('⏳ Waiting for UI to reset after cancellation...');
            await page.waitForTimeout(3000);
          }
        } else if (existingCancelled) {
          console.log(`⚠️  Database shows CANCELLED ${getParticipationTypeName(existingCancelled.participationType)} - already clean`);
        } else {
          console.log('✅ No existing participation in database - clean slate');
        }
      } catch (error) {
        console.log('⚠️  Database check failed, will verify UI state:', error);
      }

      // ALWAYS navigate to ensure fresh page state
      await page.goto(`${WEB_BASE_URL}/events/${eventId}`);
      await page.waitForLoadState('networkidle');
      await page.waitForTimeout(3000); // Increased from 2000ms to allow UI to fully settle
      console.log('✅ Navigated to event details page');
    },

    // Action: Check waiver checkbox, then click RSVP button
    action: async (page: Page) => {
      // STEP 1: Wait for ALL loading states to complete
      console.log('Waiting for page to fully load (no loading states)...');

      // Wait for any loading overlays to disappear
      const loadingOverlay = page.locator('.mantine-LoadingOverlay-overlay');
      if (await loadingOverlay.count() > 0) {
        await loadingOverlay.waitFor({ state: 'hidden', timeout: 10000 }).catch(() => {
          console.log('⚠️  Loading overlay did not disappear, continuing anyway');
        });
      }

      // Wait for page to stabilize (increased to allow UI to fully settle after navigation)
      await page.waitForTimeout(3000);
      console.log('✅ Page fully loaded');

      // STEP 2: Check the event waiver checkbox
      console.log('Attempting to check waiver checkbox...');

      // Mantine hides the checkbox so completely that clicking the label doesn't work
      // Find the visible Mantine checkbox wrapper (the styled div with the checkmark icon)
      // This is what a real user would click
      const mantineCheckbox = page.locator('input[data-testid="rsvp-terms-checkbox"]')
        .locator('..')  // Get parent element (the Mantine checkbox wrapper)
        .last();  // Use .last() to avoid React strict mode duplicate

      await expect(mantineCheckbox).toBeVisible({ timeout: 5000 });
      await mantineCheckbox.click();
      console.log('✅ Clicked Mantine checkbox wrapper');

      // Wait for React to process the change
      await page.waitForTimeout(2000);

      // STEP 3: Now RSVP button should be enabled
      const rsvpButton = page.getByRole('button', {
        name: /rsvp now|rsvp/i
      }).first();

      // Wait for button to be ENABLED (waiver checkbox enables it)
      await expect(rsvpButton).toBeVisible({ timeout: 5000 });
      await expect(rsvpButton).toBeEnabled({ timeout: 10000 });
      console.log('✅ RSVP button is enabled after waiver checked');

      await rsvpButton.click();
      console.log('✅ Clicked RSVP button');

      // RSVP happens immediately (see ParticipationCard.tsx)
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
      // Use .last() to avoid hidden React strict mode duplicates (same as checkbox fix)
      const cancelRsvpButton = page.locator('button:has-text("Cancel RSVP"), button:has-text("Withdraw")').last();
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
        1,  // 1 = Active (ParticipationStatus enum)
        1   // 1 = RSVP (AttendanceType enum) - filters to ensure we get the RSVP record, not a Ticket
      );

      // Verify participation type is RSVP
      // Database stores AttendanceType as integer: 1=RSVP, 2=Ticket
      if (!participation) {
        throw new Error('No participation record found in database');
      }
      const typeName = getParticipationTypeName(participation.participationType);
      if (typeName !== 'RSVP') {
        throw new Error(
          `Expected RSVP but got ${typeName} (raw: ${participation.participationType})`
        );
      }

      console.log('✅ Database shows RSVP registered');
    },

    // Verify persistence after refresh (CRITICAL)
    verifyPersistence: async (page: Page) => {
      // After refresh, Cancel RSVP button should be visible
      // Use .last() to avoid React strict mode duplicates (same as line 210)
      const cancelRsvpButton = page.locator(
        'button:has-text("Cancel RSVP"), button:has-text("Withdraw")'
      ).last();

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
      // Map email to role for AuthHelpers.loginAs()
      const roleMap: Record<string, keyof typeof import('../test-utils/helpers/auth.helpers').AuthHelpers.accounts> = {
        'admin@witchcityrope.com': 'admin',
        'teacher@witchcityrope.com': 'teacher',
        'vetted@witchcityrope.com': 'vetted',
        'member@witchcityrope.com': 'member',
        'guest@witchcityrope.com': 'guest'
      };

      const role = roleMap[userEmail];
      if (!role) {
        throw new Error(`Unknown user email: ${userEmail}. Expected one of: ${Object.keys(roleMap).join(', ')}`);
      }

      // Use AuthHelpers for robust login (clears auth state, handles waits properly)
      const { AuthHelpers } = await import('../test-utils/helpers/auth.helpers');
      await AuthHelpers.loginAs(page, role);

      userId = await DatabaseHelpers.getUserIdFromEmail(userEmail);

      // DEFENSIVE: Ensure user HAS an active RSVP (another test might have cancelled it)
      console.log('🔍 Checking for active RSVP in database...');

      try {
        const participation = await DatabaseHelpers.verifyEventParticipation(
          userId,
          eventId,
          1  // 1 = Active (ParticipationStatus enum)
        );
        if (!participation) {
          throw new Error('No participation found');
        }
        const typeName = getParticipationTypeName(participation.participationType);
        console.log(`✅ User has active ${typeName} - ready to cancel`);
      } catch (error) {
        console.log('⚠️  No active RSVP found - creating one first');

        // Navigate and create RSVP via UI
        await page.goto(`${WEB_BASE_URL}/events/${eventId}`);
        await page.waitForLoadState('networkidle');
        await page.waitForTimeout(2000);

        // Check and click waiver checkbox
        const mantineCheckbox = page.locator('input[data-testid="rsvp-terms-checkbox"]')
          .locator('..')
          .last();

        if (await mantineCheckbox.isVisible({ timeout: 5000 }).catch(() => false)) {
          await mantineCheckbox.click();
          await page.waitForTimeout(1000);
        }

        // Click RSVP button
        const rsvpButton = page.getByRole('button', {
          name: /rsvp now|rsvp/i
        }).first();

        await rsvpButton.waitFor({ state: 'visible', timeout: 5000 });
        await rsvpButton.click();
        await page.waitForTimeout(2000);

        // Verify RSVP was created
        await DatabaseHelpers.verifyEventParticipation(userId, eventId, 1); // 1 = Active
        console.log('✅ Created RSVP for cancellation test');
      }

      // Navigate to event
      await page.goto(`${WEB_BASE_URL}/events/${eventId}`);
      await page.waitForLoadState('networkidle');
    },

    // Action: Click Cancel RSVP button
    action: async (page: Page) => {
      // STEP 1: Wait for ALL loading states to complete (same as RSVP create)
      console.log('Waiting for page to fully load (no loading states)...');

      // Wait for any loading overlays to disappear
      const loadingOverlay = page.locator('.mantine-LoadingOverlay-overlay');
      if (await loadingOverlay.count() > 0) {
        await loadingOverlay.waitFor({ state: 'hidden', timeout: 10000 }).catch(() => {
          console.log('⚠️  Loading overlay did not disappear, continuing anyway');
        });
      }

      // Wait for page to stabilize
      await page.waitForTimeout(3000);

      // CRITICAL: Wait for actual page content to render before looking for buttons
      // The blank white page screenshot showed React hadn't rendered yet
      // Wait for event details or participant card (something that WILL exist regardless of button state)
      const pageContent = page.locator('h1, [class*="ParticipationCard"], [class*="EventDetails"]').first();
      await expect(pageContent).toBeVisible({ timeout: 10000 }).catch(() => {
        console.log('⚠️  Page content not visible, continuing anyway');
      });
      console.log('✅ Page fully loaded and content rendered');

      // STEP 2: Find and click Cancel RSVP button
      // Use .last() for React Strict Mode - duplicates exist, visible one is last
      const cancelButton = page.locator(
        'button:has-text("Cancel RSVP"), button:has-text("Withdraw")'
      ).last();

      // Wait for button to be visible and enabled before clicking
      await expect(cancelButton).toBeVisible({ timeout: 10000 });
      await expect(cancelButton).toBeEnabled({ timeout: 10000 });
      console.log('✅ Cancel RSVP button is enabled');

      await cancelButton.click();
      console.log('✅ Clicked Cancel RSVP button');

      // CANCELLATION HAS CONFIRMATION MODAL (see ParticipationCard.tsx line 298-307)
      // Wait for modal to appear
      await page.waitForTimeout(500);

      const confirmModal = page.locator('[role="dialog"]');
      await expect(confirmModal).toBeVisible({ timeout: 5000 });

      // Click the red "Cancel RSVP" button in modal (not "Keep RSVP")
      const confirmButton = page.locator('button:has-text("Cancel RSVP")').last();
      await expect(confirmButton).toBeEnabled({ timeout: 5000 });
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
      const rsvpButton = page.locator('[data-testid="button-rsvp"], button:has-text("RSVP Now"), button:has-text("RSVP")').last();
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
      // Use .last() to handle React Strict Mode duplicates
      const rsvpButton = page.locator('button:has-text("RSVP"), button:has-text("Register")').last();
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
