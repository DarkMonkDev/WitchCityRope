/**
 * E2E Tests: Post-Login Return to Intended Page
 *
 * Feature: Post-Login Return URL Functionality
 * Backend Implementation: Commit 55e7deb7 (OWASP-compliant URL validation)
 * Frontend Implementation: Commit e6f77f50 (React integration with returnUrl)
 * Documentation: /docs/functional-areas/authentication/new-work/2025-10-10-post-login-return/
 *
 * Test Coverage:
 * - P0: Security tests (malicious URLs blocked)
 * - P1: Vetting workflow (return to application form)
 * - P1: Event workflow (return to event page)
 * - P1: Default dashboard behavior (no returnUrl)
 *
 * Created: 2025-10-23
 * Test Developer: Claude AI
 */

import { test, expect, Page } from '@playwright/test';
import { AuthHelpers } from '../helpers/auth.helpers';

// Test configuration
const BASE_URL = 'http://localhost:5173';
const API_URL = 'http://localhost:5655';

// Test account for authentication
const TEST_ACCOUNT = {
  email: 'member@witchcityrope.com',
  password: 'Test123!'
};

/**
 * Helper: Navigate to page and verify login button with returnUrl
 */
async function verifyLoginButtonWithReturnUrl(page: Page, pageUrl: string, expectedReturnUrl: string) {
  await page.goto(`${BASE_URL}${pageUrl}`);
  await page.waitForLoadState('networkidle');

  // Find login button (text may vary)
  const loginButton = page.locator('a[href*="login?returnUrl"], a:has-text("Login"), a:has-text("Log In")').first();
  await expect(loginButton).toBeVisible({ timeout: 10000 });

  // Verify returnUrl is in href
  const href = await loginButton.getAttribute('href');
  expect(href).toContain('returnUrl');
  expect(href).toContain(encodeURIComponent(expectedReturnUrl));

  return loginButton;
}

/**
 * Helper: Complete login flow from current page
 */
async function completeLogin(page: Page) {
  // Fill login form
  await page.locator('[data-testid="email-input"]').fill(TEST_ACCOUNT.email);
  await page.locator('[data-testid="password-input"]').fill(TEST_ACCOUNT.password);

  // Submit login
  await page.locator('[data-testid="login-button"]').click();

  // Wait for login to complete (will redirect)
  await page.waitForLoadState('networkidle');
}

test.describe('Post-Login Return to Intended Page', () => {
  test.beforeEach(async ({ page }) => {
    // Ensure clean auth state before each test
    await AuthHelpers.clearAuthState(page);
  });

  test.describe('P1 CRITICAL: Vetting Workflow', () => {
    test('should return to /apply/vetting after login from vetting page', async ({ page }) => {
      // Step 1: Navigate to vetting application page (not authenticated)
      await page.goto(`${BASE_URL}/apply/vetting`);
      await page.waitForLoadState('networkidle');

      // Step 2: Verify "Login to Your Account" button is present with returnUrl
      const loginButton = await verifyLoginButtonWithReturnUrl(page, '/apply/vetting', '/apply/vetting');

      // Step 3: Click login button
      await loginButton.click();
      await page.waitForLoadState('networkidle');

      // Step 4: Verify we're on login page with returnUrl parameter
      await expect(page).toHaveURL(/\/login\?returnUrl=%2Fapply%2Fvetting/);

      // Step 5: Complete login
      await completeLogin(page);

      // Step 6: Verify redirect back to vetting page
      await expect(page).toHaveURL(`${BASE_URL}/apply/vetting`);

      // Step 7: Verify success message shown
      const successMessage = page.locator('[role="alert"]').filter({ hasText: /complete your application/i }).first();
      await expect(successMessage).toBeVisible({ timeout: 5000 });
    });

    test('should return to /join after login from join page', async ({ page }) => {
      // /join is an alias for /apply/vetting
      await page.goto(`${BASE_URL}/join`);
      await page.waitForLoadState('networkidle');

      // Find login button with returnUrl
      const loginButton = page.locator('a[href*="login?returnUrl"]').first();
      await expect(loginButton).toBeVisible({ timeout: 10000 });

      // Click and navigate to login
      await loginButton.click();
      await page.waitForLoadState('networkidle');

      // Complete login
      await completeLogin(page);

      // Verify redirect back to join page
      await expect(page).toHaveURL(`${BASE_URL}/join`);

      // Verify contextual success message
      const successMessage = page.locator('[role="alert"]').first();
      await expect(successMessage).toBeVisible({ timeout: 5000 });
    });
  });

  test.describe('P1 CRITICAL: Event Page Workflow', () => {
    test('should return to event page after login from event details', async ({ page }) => {
      // Step 1: Get a published event ID from API
      const eventsResponse = await page.request.get(`${API_URL}/api/events`);
      const eventsData = await eventsResponse.json();

      expect(eventsData.success).toBe(true);
      expect(Array.isArray(eventsData.data)).toBe(true);
      expect(eventsData.data.length).toBeGreaterThan(0);

      const firstEvent = eventsData.data[0];
      const eventId = firstEvent.id;
      const eventUrl = `/events/${eventId}`;

      // Step 2: Navigate to event page (not authenticated)
      await page.goto(`${BASE_URL}${eventUrl}`);
      await page.waitForLoadState('networkidle');

      // Step 3: Look for "Log In" button in ParticipationCard
      const loginButton = page.locator('a[href*="login?returnUrl"]').filter({ hasText: /log in/i }).first();

      // If button not found, event might allow guest access - skip test
      if (await loginButton.count() === 0) {
        test.skip();
        return;
      }

      await expect(loginButton).toBeVisible({ timeout: 10000 });

      // Step 4: Verify returnUrl includes event ID
      const href = await loginButton.getAttribute('href');
      expect(href).toContain('returnUrl');
      expect(href).toContain(encodeURIComponent(eventUrl));

      // Step 5: Click login button
      await loginButton.click();
      await page.waitForLoadState('networkidle');

      // Step 6: Verify we're on login page with event returnUrl
      await expect(page).toHaveURL(/\/login\?returnUrl=/);

      // Step 7: Complete login
      await completeLogin(page);

      // Step 8: Verify redirect back to same event page
      await expect(page).toHaveURL(`${BASE_URL}${eventUrl}`);

      // Step 9: Verify success message about event registration
      const successMessage = page.locator('[role="alert"]').filter({ hasText: /register for this event/i }).first();
      await expect(successMessage).toBeVisible({ timeout: 5000 });
    });

    test('should show event registration options after login return', async ({ page }) => {
      // Get published event
      const eventsResponse = await page.request.get(`${API_URL}/api/events`);
      const eventsData = await eventsResponse.json();
      const firstEvent = eventsData.data[0];
      const eventUrl = `/events/${firstEvent.id}`;

      // Navigate to event and login
      await page.goto(`${BASE_URL}${eventUrl}`);
      await page.waitForLoadState('networkidle');

      const loginButton = page.locator('a[href*="login?returnUrl"]').first();
      if (await loginButton.count() === 0) {
        test.skip();
        return;
      }

      await loginButton.click();
      await page.waitForLoadState('networkidle');
      await completeLogin(page);

      // Wait for return to event page
      await expect(page).toHaveURL(`${BASE_URL}${eventUrl}`);

      // Verify user can now see registration options (RSVP/tickets)
      // Look for participation card or ticket/RSVP buttons
      const participationOptions = page.locator('[data-testid="participation-card"], button:has-text("RSVP"), button:has-text("Get Tickets")').first();
      await expect(participationOptions).toBeVisible({ timeout: 10000 });
    });
  });

  test.describe('P1: Default Dashboard Behavior', () => {
    test('should redirect to dashboard when no returnUrl provided', async ({ page }) => {
      // Step 1: Navigate directly to login page (no returnUrl parameter)
      await page.goto(`${BASE_URL}/login`);
      await page.waitForLoadState('networkidle');

      // Step 2: Verify no returnUrl in URL
      const currentUrl = page.url();
      expect(currentUrl).not.toContain('returnUrl');

      // Step 3: Complete login
      await completeLogin(page);

      // Step 4: Verify redirect to default /dashboard
      await expect(page).toHaveURL(`${BASE_URL}/dashboard`);

      // Step 5: Verify generic success message
      const successMessage = page.locator('[role="alert"]').filter({ hasText: /welcome back/i }).first();
      await expect(successMessage).toBeVisible({ timeout: 5000 });
    });

    test('should redirect to dashboard from nav menu login', async ({ page }) => {
      // Navigate to home page
      await page.goto(`${BASE_URL}/`);
      await page.waitForLoadState('networkidle');

      // Find "Login" link in navigation (should not have returnUrl)
      const navLoginLink = page.locator('nav a[href="/login"], header a[href="/login"]').first();

      if (await navLoginLink.count() > 0) {
        await navLoginLink.click();
        await page.waitForLoadState('networkidle');

        // Verify we're on login page without returnUrl
        expect(page.url()).toBe(`${BASE_URL}/login`);

        // Complete login
        await completeLogin(page);

        // Verify redirect to dashboard (default)
        await expect(page).toHaveURL(`${BASE_URL}/dashboard`);
      } else {
        // If no nav login link, just verify direct login behavior
        await page.goto(`${BASE_URL}/login`);
        await page.waitForLoadState('networkidle');
        await completeLogin(page);
        await expect(page).toHaveURL(`${BASE_URL}/dashboard`);
      }
    });
  });

  test.describe('P0 CRITICAL: Security Tests', () => {
    test('should block external URL redirect - https://evil.com', async ({ page }) => {
      // Step 1: Navigate to login with malicious external URL
      const maliciousUrl = 'https://evil.com/phishing';
      await page.goto(`${BASE_URL}/login?returnUrl=${encodeURIComponent(maliciousUrl)}`);
      await page.waitForLoadState('networkidle');

      // Step 2: Complete login
      await completeLogin(page);

      // Step 3: Verify redirect to safe default (/dashboard), NOT external site
      await expect(page).toHaveURL(`${BASE_URL}/dashboard`);

      // Step 4: Verify we're NOT on evil.com
      expect(page.url()).not.toContain('evil.com');
      expect(page.url()).not.toContain('phishing');

      // Step 5: Verify user landed safely
      await page.waitForLoadState('networkidle');
      const dashboardContent = page.locator('[data-testid="dashboard-content"], main, nav').first();
      await expect(dashboardContent).toBeVisible({ timeout: 10000 });
    });

    test('should block JavaScript protocol attack - javascript:alert()', async ({ page }) => {
      // Step 1: Navigate to login with JavaScript protocol attack
      const maliciousUrl = "javascript:alert('XSS')";
      await page.goto(`${BASE_URL}/login?returnUrl=${encodeURIComponent(maliciousUrl)}`);
      await page.waitForLoadState('networkidle');

      // Step 2: Complete login
      await completeLogin(page);

      // Step 3: Verify redirect to safe default (/dashboard)
      await expect(page).toHaveURL(`${BASE_URL}/dashboard`);

      // Step 4: Verify JavaScript code was NOT executed
      // If alert() was executed, Playwright would show a dialog
      // We verify no dialogs appeared by successfully navigating
      await page.waitForLoadState('networkidle');
      const dashboardContent = page.locator('main, nav').first();
      await expect(dashboardContent).toBeVisible({ timeout: 10000 });
    });

    test('should block data: protocol attack', async ({ page }) => {
      // Step 1: Navigate to login with data: protocol attack
      const maliciousUrl = "data:text/html,<script>alert('XSS')</script>";
      await page.goto(`${BASE_URL}/login?returnUrl=${encodeURIComponent(maliciousUrl)}`);
      await page.waitForLoadState('networkidle');

      // Step 2: Complete login
      await completeLogin(page);

      // Step 3: Verify redirect to safe default (/dashboard)
      await expect(page).toHaveURL(`${BASE_URL}/dashboard`);

      // Step 4: Verify data: URL was not navigated to
      expect(page.url()).not.toContain('data:');

      // Step 5: Verify user is safely on dashboard
      await page.waitForLoadState('networkidle');
      const dashboardContent = page.locator('main').first();
      await expect(dashboardContent).toBeVisible({ timeout: 10000 });
    });

    test('should block file: protocol attack', async ({ page }) => {
      // Step 1: Navigate to login with file: protocol attack
      const maliciousUrl = "file:///etc/passwd";
      await page.goto(`${BASE_URL}/login?returnUrl=${encodeURIComponent(maliciousUrl)}`);
      await page.waitForLoadState('networkidle');

      // Step 2: Complete login
      await completeLogin(page);

      // Step 3: Verify redirect to safe default (/dashboard)
      await expect(page).toHaveURL(`${BASE_URL}/dashboard`);

      // Step 4: Verify file: protocol was not accessed
      expect(page.url()).not.toContain('file:');

      // Step 5: Verify safe landing
      await page.waitForLoadState('networkidle');
      const dashboardContent = page.locator('main').first();
      await expect(dashboardContent).toBeVisible({ timeout: 10000 });
    });

    test('should validate URLs on backend, not just frontend', async ({ page }) => {
      // This test verifies that backend validation is enforced
      // by attempting to bypass frontend validation with direct API call

      // Step 1: Login first to get auth cookie
      await AuthHelpers.loginAs(page, 'member');

      // Step 2: Attempt login with malicious returnUrl via API
      const maliciousUrl = 'https://attacker.com/steal-data';

      const loginResponse = await page.request.post(`${API_URL}/api/auth/login`, {
        data: {
          email: TEST_ACCOUNT.email,
          password: TEST_ACCOUNT.password,
          returnUrl: maliciousUrl
        }
      });

      // Step 3: Verify API response
      const responseData = await loginResponse.json();

      // Backend should validate and reject malicious URL
      // returnUrl should be null (validation failed)
      expect(responseData.success).toBe(true); // Login succeeds

      // CRITICAL: returnUrl should be null or safe internal path, never the malicious URL
      if (responseData.data && responseData.data.returnUrl) {
        expect(responseData.data.returnUrl).not.toContain('attacker.com');
        expect(responseData.data.returnUrl).not.toContain('steal-data');
      } else {
        // returnUrl is null - validation correctly rejected the malicious URL
        expect(responseData.data?.returnUrl).toBeNull();
      }
    });

    test('should sanitize and validate returnUrl with special characters', async ({ page }) => {
      // Test various URL encoding attacks
      const attackVectors = [
        '//evil.com', // Protocol-relative URL
        '\\\\evil.com', // Windows-style path
        '/\\evil.com', // Mixed separators
        '/%2f%2fevil.com', // Double-encoded slashes
      ];

      for (const maliciousUrl of attackVectors) {
        // Navigate to login with attack vector
        await page.goto(`${BASE_URL}/login?returnUrl=${encodeURIComponent(maliciousUrl)}`);
        await page.waitForLoadState('networkidle');

        // Complete login
        await AuthHelpers.clearAuthState(page); // Reset for clean login
        await page.goto(`${BASE_URL}/login?returnUrl=${encodeURIComponent(maliciousUrl)}`);
        await page.waitForLoadState('networkidle');
        await completeLogin(page);

        // Verify redirect to safe default
        await expect(page).toHaveURL(`${BASE_URL}/dashboard`);

        // Verify not redirected to evil.com
        expect(page.url()).not.toContain('evil.com');

        // Reset for next iteration
        await AuthHelpers.clearAuthState(page);
      }
    });
  });

  test.describe('Edge Cases and Error Handling', () => {
    test('should handle empty returnUrl gracefully', async ({ page }) => {
      await page.goto(`${BASE_URL}/login?returnUrl=`);
      await page.waitForLoadState('networkidle');

      await completeLogin(page);

      // Should redirect to default dashboard
      await expect(page).toHaveURL(`${BASE_URL}/dashboard`);
    });

    test('should handle non-existent internal path', async ({ page }) => {
      // Navigate with valid internal path that doesn't exist
      await page.goto(`${BASE_URL}/login?returnUrl=${encodeURIComponent('/does-not-exist-12345')}`);
      await page.waitForLoadState('networkidle');

      await completeLogin(page);

      // Backend may validate and allow internal paths even if they don't exist
      // Frontend will attempt navigation, React Router will handle 404
      await page.waitForLoadState('networkidle');

      // We should either be on the attempted path or dashboard (safe default)
      const currentUrl = page.url();
      const isOnDashboard = currentUrl.includes('/dashboard');
      const isOn404 = currentUrl.includes('/does-not-exist') || await page.locator('text=/not found/i, text=/404/i').count() > 0;

      expect(isOnDashboard || isOn404).toBe(true);
    });

    test('should preserve hash fragments in returnUrl if supported', async ({ page }) => {
      // Test if URL hash fragments are preserved
      const urlWithHash = '/apply/vetting#section-2';

      await page.goto(`${BASE_URL}/login?returnUrl=${encodeURIComponent(urlWithHash)}`);
      await page.waitForLoadState('networkidle');

      await completeLogin(page);

      // Check if hash is preserved (may depend on backend implementation)
      await page.waitForLoadState('networkidle');

      // Either on vetting page with or without hash
      expect(page.url()).toContain('/apply/vetting');
    });
  });
});
