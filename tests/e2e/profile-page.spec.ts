import { test, expect } from '@playwright/test';
import { AuthHelpers } from './test-utils/helpers/auth.helpers';

/**
 * ProfilePage E2E Tests
 *
 * Tests converted from ProfilePage.test.tsx unit tests that were failing due to
 * TanStack Query integration complexity. These tests verify actual API integration
 * and error handling in a real browser environment.
 *
 * Original unit tests: /apps/web/src/pages/dashboard/__tests__/ProfilePage.test.tsx
 * Converted: 2025-10-24
 */
test.describe('ProfilePage - E2E Tests', () => {

  test('should handle user loading error', async ({ page }) => {
    // Set up route mock BEFORE login to intercept all user requests
    let requestCount = 0;
    const routeHandler = async (route: import('@playwright/test').Route) => {
      requestCount++;

      // Let the first request through (for login)
      if (requestCount === 1) {
        await route.continue();
      } else {
        // Fail subsequent requests (for profile page)
        await route.fulfill({
          status: 500,
          contentType: 'application/json',
          body: JSON.stringify({ error: 'Server error' })
        });
      }
    };

    await page.route('**/api/auth/user', routeHandler);

    // Login as admin (first request will succeed)
    await AuthHelpers.loginAs(page, 'admin');

    // Navigate to profile page - this will trigger the error (second request)
    await page.goto('/dashboard/profile-settings');
    await page.waitForLoadState('domcontentloaded');

    // Verify error message is displayed (using flexible selector for error alert)
    const errorAlert = page.locator('[role="alert"]').filter({ hasText: /error|failed/i }).first();
    const errorText = page.locator('text=/Error Loading Profile|Failed to load|error/i').first();

    // Try both selectors
    const errorVisible = await errorAlert.isVisible({ timeout: 5000 }).catch(() => false) ||
                         await errorText.isVisible({ timeout: 5000 }).catch(() => false);

    expect(errorVisible).toBeTruthy();

    console.log('✅ Profile page correctly displays error when API returns 500');

    // Cleanup: Remove route mock for subsequent tests
    await page.unroute('**/api/auth/user', routeHandler);
  });

  test('should display user account information correctly', async ({ page }) => {
    // CRITICAL: Fully reset browser state to avoid TanStack Query cached errors from test 1
    await page.context().clearCookies();
    await page.goto('/login'); // Navigate to clear React Query cache
    await page.evaluate(() => {
      localStorage.clear();
      sessionStorage.clear();
    });

    // Login as admin using standard flow (no mocking - use real API)
    await AuthHelpers.loginAs(page, 'admin');

    // Capture the API response for debugging
    let capturedResponse = null;
    page.on('response', async (response) => {
      if (response.url().includes('/api/auth/user')) {
        capturedResponse = {
          status: response.status(),
          statusText: response.statusText(),
          url: response.url(),
          body: await response.text().catch(() => 'Failed to read body')
        };
        console.log('📡 Captured /api/auth/user response:', JSON.stringify(capturedResponse, null, 2));
      }
    });

    await page.goto('/dashboard/profile-settings');
    await page.waitForLoadState('domcontentloaded');

    // Wait for profile page to load
    await expect(page.locator('h1:has-text("Profile Settings")')).toBeVisible({ timeout: 10000 });

    // Check if error message is displayed (API might be failing)
    const errorMessage = page.locator('text=Failed to load your profile');
    if (await errorMessage.isVisible({ timeout: 2000 }).catch(() => false)) {
      console.log('⚠️ Page shows error despite API response');

      // Take screenshot for debugging
      await page.screenshot({ path: './test-results/profile-api-error.png', fullPage: true });

      // Fail with helpful message including actual response body
      throw new Error(
        `ProfilePage shows error despite API response.\n` +
        `API Response: ${JSON.stringify(capturedResponse, null, 2)}\n` +
        `This suggests the API might be returning an error structure even with 200 status, ` +
        `or there's a client-side error parsing the response.`
      );
    }

    // Verify profile tabs exist (current implementation uses Tabs component)
    const personalTab = page.locator('button[role="tab"]').filter({ hasText: /personal/i }).first();
    const securityTab = page.locator('button[role="tab"]').filter({ hasText: /password|security/i }).first();
    const vettingTab = page.locator('button[role="tab"]').filter({ hasText: /vetting/i }).first();

    await expect(personalTab).toBeVisible({ timeout: 5000 });
    await expect(securityTab).toBeVisible({ timeout: 5000 });
    await expect(vettingTab).toBeVisible({ timeout: 5000 });

    // Verify personal info fields exist (using data-testid attributes from ProfileSettingsPage.tsx)
    await expect(page.getByTestId('scene-name-input')).toBeVisible();
    await expect(page.getByTestId('first-name-input')).toBeVisible();
    await expect(page.getByTestId('last-name-input')).toBeVisible();
    await expect(page.getByTestId('email-input')).toBeVisible();

    console.log('✅ Profile page displays account information correctly');
  });

});
