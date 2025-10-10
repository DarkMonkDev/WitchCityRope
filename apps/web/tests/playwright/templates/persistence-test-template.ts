/**
 * Base Persistence Test Template
 *
 * Reusable pattern for testing that UI actions persist to database.
 * This template catches the category of bugs where:
 * - UI shows success message
 * - Database is NOT updated
 * - Page refresh reveals the failure
 *
 * CRITICAL: Both profile update and ticket cancellation bugs
 * would have been caught by following this pattern.
 *
 * Pattern: Action → Verify UI → Verify API → Verify Database → Refresh → Verify Persistence
 */

import { Page, expect, Response } from '@playwright/test';
import { DatabaseHelpers, closeDatabaseConnections } from '../utils/database-helpers';

// ============================================================================
// TYPE DEFINITIONS
// ============================================================================

export interface TestConfig {
  /** Test description for logging */
  description: string;
  /** Setup function to run before the test action */
  setup: (page: Page) => Promise<void>;
  /** The action to test (e.g., click Save button) */
  action: (page: Page) => Promise<void>;
  /** Verify UI shows success after action */
  verifyUiSuccess: (page: Page) => Promise<void>;
  /** Verify API returned success (optional) */
  verifyApiResponse?: (response: Response) => Promise<void>;
  /** Verify database was updated correctly */
  verifyDatabaseState: (page: Page) => Promise<void>;
  /** Verify UI still shows success after refresh */
  verifyPersistence: (page: Page) => Promise<void>;
  /** Cleanup after test (optional) */
  cleanup?: (page: Page) => Promise<void>;
}

export interface PersistenceTestOptions {
  /** Take screenshots at each step for debugging */
  screenshotPath?: string;
  /** Wait time after action before verification (ms) */
  waitAfterAction?: number;
  /** Expected API endpoint to monitor (optional) */
  apiEndpoint?: string | RegExp;
  /** Expected API method (optional) */
  apiMethod?: 'GET' | 'POST' | 'PUT' | 'PATCH' | 'DELETE';
}

// ============================================================================
// BASE PERSISTENCE TEST TEMPLATE
// ============================================================================

export class PersistenceTestTemplate {
  /**
   * Run a complete persistence test following the standard pattern
   *
   * Steps:
   * 1. Setup: Login, navigate, prepare state
   * 2. Monitor: Start monitoring network/API calls
   * 3. Action: Perform the operation (update profile, cancel ticket, etc.)
   * 4. Verify UI: Check success message, button states, visual feedback
   * 5. Verify API: Check network response status and data
   * 6. Verify Database: Query database directly to confirm persistence
   * 7. Refresh: Reload the page to clear any client-side state
   * 8. Verify Persistence: Check UI and database still show changes
   * 9. Cleanup: Remove test data if needed
   */
  static async runTest(
    page: Page,
    config: TestConfig,
    options: PersistenceTestOptions = {}
  ): Promise<void> {
    const {
      screenshotPath = '/tmp',
      waitAfterAction = 1000,
      apiEndpoint,
      apiMethod,
    } = options;

    console.log(`\n=== Persistence Test: ${config.description} ===\n`);

    // Track API responses if endpoint specified
    let apiResponse: Response | undefined;
    if (apiEndpoint) {
      page.on('response', (response) => {
        const url = response.url();
        const method = response.request().method();

        const urlMatches =
          typeof apiEndpoint === 'string'
            ? url.includes(apiEndpoint)
            : apiEndpoint.test(url);

        if (urlMatches && (!apiMethod || method === apiMethod)) {
          apiResponse = response;
          console.log(`✅ Captured API response: ${method} ${url} (${response.status()})`);
        }
      });
    }

    try {
      // STEP 1: Setup
      console.log('Step 1: Running setup...');
      await config.setup(page);
      await page.screenshot({ path: `${screenshotPath}/01-setup-complete.png`, fullPage: true });

      // STEP 2: Perform Action
      console.log('Step 2: Performing action...');
      await config.action(page);

      // Wait for any async operations
      if (waitAfterAction > 0) {
        await page.waitForTimeout(waitAfterAction);
      }
      await page.screenshot({ path: `${screenshotPath}/02-action-complete.png`, fullPage: true });

      // STEP 3: Verify UI Success
      console.log('Step 3: Verifying UI shows success...');
      await config.verifyUiSuccess(page);
      await page.screenshot({ path: `${screenshotPath}/03-ui-success-verified.png`, fullPage: true });
      console.log('✅ UI shows success');

      // STEP 4: Verify API Response
      if (config.verifyApiResponse && apiResponse) {
        console.log('Step 4: Verifying API response...');
        await config.verifyApiResponse(apiResponse);
        console.log(`✅ API returned success (${apiResponse.status()})`);
      } else if (apiEndpoint && !apiResponse) {
        console.warn('⚠️  Expected API call not detected - possible bug');
      }

      // STEP 5: Verify Database State (CRITICAL)
      console.log('Step 5: Verifying database was updated...');
      await config.verifyDatabaseState(page);
      console.log('✅ Database updated correctly');

      // STEP 6: Refresh Page (Clear Client State)
      console.log('Step 6: Refreshing page to verify persistence...');
      await page.reload({ waitUntil: 'networkidle' });
      await page.screenshot({ path: `${screenshotPath}/04-after-refresh.png`, fullPage: true });

      // STEP 7: Verify Persistence After Refresh (CRITICAL)
      console.log('Step 7: Verifying changes persisted after refresh...');
      await config.verifyPersistence(page);
      console.log('✅ Changes persisted after page refresh');

      // STEP 8: Verify Database Again (Double Check)
      console.log('Step 8: Verifying database still correct after refresh...');
      await config.verifyDatabaseState(page);
      await page.screenshot({ path: `${screenshotPath}/05-persistence-verified.png`, fullPage: true });
      console.log('✅ Database still correct after refresh');

      console.log(`\n✅ PERSISTENCE TEST PASSED: ${config.description}\n`);
    } catch (error) {
      console.error(`\n❌ PERSISTENCE TEST FAILED: ${config.description}`);
      console.error(`Error: ${error}\n`);

      // Take failure screenshot for debugging
      await page.screenshot({
        path: `${screenshotPath}/FAILURE-${Date.now()}.png`,
        fullPage: true,
      });

      throw error;
    } finally {
      // Cleanup
      if (config.cleanup) {
        console.log('Running cleanup...');
        await config.cleanup(page);
      }
    }
  }
}

// ============================================================================
// AUTHENTICATED TEST HELPER
// ============================================================================

/**
 * Helper for tests requiring authentication
 */
export async function withAuthentication(
  page: Page,
  userEmail: string,
  password: string,
  testFn: () => Promise<void>
): Promise<void> {
  // Login
  await page.goto('http://localhost:5173/login');
  await page.waitForLoadState('networkidle');

  await page.locator('[data-testid="email-input"]').fill(userEmail);
  await page.locator('[data-testid="password-input"]').fill(password);
  await page.locator('[data-testid="login-button"]').click();

  // Wait for redirect to dashboard
  await page.waitForURL('**/dashboard', { timeout: 10000 });
  await page.waitForLoadState('networkidle');

  // Run the test
  await testFn();

  // Logout
  const logoutButton = page.locator('[data-testid="logout-button"], button:has-text("Logout")');
  if (await logoutButton.count() > 0) {
    await logoutButton.click();
  }
}

// ============================================================================
// COMMON VERIFICATION PATTERNS
// ============================================================================

/**
 * Verify success message appears in UI
 */
export async function verifySuccessMessage(
  page: Page,
  messageText?: string
): Promise<void> {
  const successAlert = page.locator(
    '[role="alert"], .alert-success, .success, [data-testid*="success"]'
  ).first();

  await expect(successAlert).toBeVisible({ timeout: 5000 });

  if (messageText) {
    await expect(successAlert).toContainText(messageText, { timeout: 2000 });
  }
}

/**
 * Verify error message does NOT appear
 */
export async function verifyNoErrorMessage(page: Page): Promise<void> {
  const errorAlert = page.locator(
    '[role="alert"].error, .alert-danger, .error, [data-testid*="error"]'
  );

  const errorCount = await errorAlert.count();
  if (errorCount > 0) {
    const errorText = await errorAlert.textContent();
    throw new Error(`Unexpected error message: ${errorText}`);
  }
}

/**
 * Verify API response is successful
 */
export async function verifyApiSuccess(response: Response): Promise<void> {
  const status = response.status();

  if (status < 200 || status >= 300) {
    const body = await response.text().catch(() => 'Unable to read response body');
    throw new Error(
      `API returned error status ${status}: ${body}`
    );
  }

  console.log(`✅ API response successful: ${status}`);
}

/**
 * Verify API response contains expected data
 */
export async function verifyApiResponseData(
  response: Response,
  validator: (data: any) => void
): Promise<void> {
  await verifyApiSuccess(response);

  const data = await response.json();
  validator(data);
}

// ============================================================================
// CLEANUP HELPERS
// ============================================================================

/**
 * Global cleanup function for test teardown
 */
export async function globalCleanup(): Promise<void> {
  await closeDatabaseConnections();
}

// Export everything
export default PersistenceTestTemplate;
