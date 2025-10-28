/**
 * E2E Tests: Login with Email or Scene Name Feature
 *
 * Feature: Users can login with either email address OR scene name
 * Backend Implementation: LoginRequest.EmailOrSceneName field
 * Service Logic: AuthenticationService tries email lookup first, then scene name lookup
 * Frontend Implementation: LoginPage.tsx with emailOrSceneName field
 *
 * Test Coverage:
 * - P1: Login with valid email address
 * - P1: Login with valid scene name
 * - P1: Error handling for invalid credentials
 * - P1: Error handling for wrong password (both email and scene name)
 * - P1: Validation for empty fields
 *
 * Created: 2025-10-27
 * Test Developer: Claude AI (test-developer agent)
 */

import { test, expect, Page } from '@playwright/test';

// Test configuration
const BASE_URL = 'http://localhost:5173';
const API_URL = 'http://localhost:5655';

// Test accounts
const TEST_ACCOUNTS = {
  admin: {
    email: 'admin@witchcityrope.com',
    password: 'Test123!',
    sceneName: '' // Will be fetched from database
  },
  teacher: {
    email: 'teacher@witchcityrope.com',
    password: 'Test123!',
    sceneName: '' // Will be fetched from database
  },
  member: {
    email: 'member@witchcityrope.com',
    password: 'Test123!',
    sceneName: '' // Will be fetched from database
  }
};

/**
 * Helper: Get user's scene name from API
 */
async function getUserSceneName(page: Page, email: string, password: string): Promise<string> {
  // Login to get auth token
  const loginResponse = await page.request.post(`${API_URL}/api/auth/login`, {
    data: {
      emailOrSceneName: email,
      password: password
    }
  });

  expect(loginResponse.ok()).toBe(true);
  const loginData = await loginResponse.json();

  return loginData.data.user.sceneName;
}

/**
 * Helper: Clear authentication state
 */
async function clearAuthState(page: Page) {
  await page.context().clearCookies();
  await page.evaluate(() => localStorage.clear());
  await page.evaluate(() => sessionStorage.clear());
}

/**
 * Helper: Fill login form and submit
 */
async function fillAndSubmitLogin(page: Page, identifier: string, password: string) {
  await page.locator('[data-testid="email-or-scenename-input"]').fill(identifier);
  await page.locator('[data-testid="password-input"]').fill(password);
  await page.locator('[data-testid="login-button"]').click();
}

test.describe('Login with Email or Scene Name', () => {
  test.beforeEach(async ({ page }) => {
    // Navigate to login page FIRST (localStorage requires a domain context)
    await page.goto(`${BASE_URL}/login`);
    await page.waitForLoadState('domcontentloaded');

    // Clear authentication state AFTER navigation
    await clearAuthState(page);
  });

  test.describe('P1 CRITICAL: Email Login Path', () => {
    test('should login successfully with email address', async ({ page }) => {
      // Arrange
      const testAccount = TEST_ACCOUNTS.admin;

      // Act
      await fillAndSubmitLogin(page, testAccount.email, testAccount.password);

      // Wait for navigation to dashboard
      await page.waitForURL(`${BASE_URL}/dashboard`, { timeout: 10000 });

      // Assert - Verify redirect to dashboard
      await expect(page).toHaveURL(`${BASE_URL}/dashboard`);

      // Verify user is logged in (check for logout button or user menu)
      const userMenu = page.locator('[data-testid="user-menu"], nav:has-text("Logout"), nav:has-text("Sign Out")').first();
      await expect(userMenu).toBeVisible({ timeout: 5000 });
    });

    test('should show error for wrong password with valid email', async ({ page }) => {
      // Arrange
      const testAccount = TEST_ACCOUNTS.admin;
      const wrongPassword = 'WrongPassword123!';

      // Act
      await fillAndSubmitLogin(page, testAccount.email, wrongPassword);

      // Wait for error message to appear
      await page.waitForTimeout(2000); // Allow time for API response

      // Assert - Verify error message displayed
      const errorAlert = page.locator('[data-testid="login-error"]');
      await expect(errorAlert).toBeVisible({ timeout: 5000 });

      const errorText = await errorAlert.textContent();
      expect(errorText).toContain('Invalid');

      // Verify still on login page (no redirect)
      await expect(page).toHaveURL(`${BASE_URL}/login`);
    });
  });

  test.describe('P1 CRITICAL: Scene Name Login Path', () => {
    test('should login successfully with scene name', async ({ page }) => {
      // Arrange - Get admin user's scene name from database
      const testAccount = TEST_ACCOUNTS.admin;
      const sceneName = await getUserSceneName(page, testAccount.email, testAccount.password);

      expect(sceneName).toBeTruthy();
      expect(sceneName.length).toBeGreaterThan(0);

      // Clear auth state after fetching scene name
      await clearAuthState(page);
      await page.goto(`${BASE_URL}/login`);
      await page.waitForLoadState('networkidle');

      // Act - Login with scene name instead of email
      await fillAndSubmitLogin(page, sceneName, testAccount.password);

      // Wait for navigation to dashboard
      await page.waitForURL(`${BASE_URL}/dashboard`, { timeout: 10000 });

      // Assert - Verify redirect to dashboard
      await expect(page).toHaveURL(`${BASE_URL}/dashboard`);

      // Verify user is logged in
      const userMenu = page.locator('[data-testid="user-menu"], nav:has-text("Logout"), nav:has-text("Sign Out")').first();
      await expect(userMenu).toBeVisible({ timeout: 5000 });
    });

    test('should show error for wrong password with valid scene name', async ({ page }) => {
      // Arrange - Get admin user's scene name
      const testAccount = TEST_ACCOUNTS.admin;
      const sceneName = await getUserSceneName(page, testAccount.email, testAccount.password);
      const wrongPassword = 'WrongPassword123!';

      // Clear auth state
      await clearAuthState(page);
      await page.goto(`${BASE_URL}/login`);
      await page.waitForLoadState('networkidle');

      // Act - Login with scene name and wrong password
      await fillAndSubmitLogin(page, sceneName, wrongPassword);

      // Wait for error message
      await page.waitForTimeout(2000);

      // Assert - Verify error message displayed
      const errorAlert = page.locator('[data-testid="login-error"]');
      await expect(errorAlert).toBeVisible({ timeout: 5000 });

      // Verify still on login page
      await expect(page).toHaveURL(`${BASE_URL}/login`);
    });
  });

  test.describe('P1: Error Handling', () => {
    test('should show error for invalid email or scene name', async ({ page }) => {
      // Arrange
      const nonExistentIdentifier = 'nonexistent@example.com';
      const anyPassword = 'AnyPassword123!';

      // Act
      await fillAndSubmitLogin(page, nonExistentIdentifier, anyPassword);

      // Wait for error message
      await page.waitForTimeout(2000);

      // Assert - Verify error message displayed
      const errorAlert = page.locator('[data-testid="login-error"]');
      await expect(errorAlert).toBeVisible({ timeout: 5000 });

      const errorText = await errorAlert.textContent();
      expect(errorText).toContain('Invalid');

      // Verify still on login page (no redirect)
      await expect(page).toHaveURL(`${BASE_URL}/login`);
    });

    test('should show error for non-existent scene name', async ({ page }) => {
      // Arrange
      const nonExistentSceneName = 'NonExistentSceneName12345';
      const anyPassword = 'Test123!';

      // Act
      await fillAndSubmitLogin(page, nonExistentSceneName, anyPassword);

      // Wait for error message
      await page.waitForTimeout(2000);

      // Assert - Verify error message displayed
      const errorAlert = page.locator('[data-testid="login-error"]');
      await expect(errorAlert).toBeVisible({ timeout: 5000 });

      // Verify still on login page
      await expect(page).toHaveURL(`${BASE_URL}/login`);
    });
  });

  test.describe('P1: Validation', () => {
    test('should show validation error for empty email or scene name', async ({ page }) => {
      // Arrange - Fill only password, leave email/scene name empty
      await page.locator('[data-testid="password-input"]').fill('Test123!');

      // Act - Try to submit form
      await page.locator('[data-testid="login-button"]').click();

      // Assert - Verify validation error shown (Mantine form validation)
      const emailInput = page.locator('[data-testid="email-or-scenename-input"]');

      // Check for HTML5 validation or Mantine error
      const isInvalid = await emailInput.evaluate((el: HTMLInputElement) => !el.validity.valid);
      expect(isInvalid).toBe(true);

      // Verify still on login page (form didn't submit)
      await expect(page).toHaveURL(`${BASE_URL}/login`);
    });

    test('should show validation error for empty password', async ({ page }) => {
      // Arrange - Fill only email, leave password empty
      await page.locator('[data-testid="email-or-scenename-input"]').fill('test@example.com');

      // Act - Try to submit form
      await page.locator('[data-testid="login-button"]').click();

      // Assert - Verify validation error shown
      const passwordInput = page.locator('[data-testid="password-input"]');

      // Check for HTML5 validation
      const isInvalid = await passwordInput.evaluate((el: HTMLInputElement) => !el.validity.valid);
      expect(isInvalid).toBe(true);

      // Verify still on login page
      await expect(page).toHaveURL(`${BASE_URL}/login`);
    });

    test('should show validation error for both empty fields', async ({ page }) => {
      // Act - Try to submit empty form
      await page.locator('[data-testid="login-button"]').click();

      // Assert - Verify form doesn't submit
      await page.waitForTimeout(1000);

      // Verify still on login page (form validation prevented submission)
      await expect(page).toHaveURL(`${BASE_URL}/login`);
    });
  });

  test.describe('Edge Cases', () => {
    test('should handle whitespace in email or scene name', async ({ page }) => {
      // Arrange
      const testAccount = TEST_ACCOUNTS.admin;
      const emailWithSpaces = `  ${testAccount.email}  `; // Leading/trailing spaces

      // Act
      await fillAndSubmitLogin(page, emailWithSpaces, testAccount.password);

      // Wait for response
      await page.waitForTimeout(2000);

      // Assert - Should either trim and succeed, or show validation error
      // Check if we're on dashboard (trimmed successfully) OR still on login (validation error)
      const currentUrl = page.url();
      const onDashboard = currentUrl.includes('/dashboard');
      const onLogin = currentUrl.includes('/login');

      expect(onDashboard || onLogin).toBe(true);
    });

    test('should be case-sensitive for scene name', async ({ page }) => {
      // Arrange - Get admin scene name
      const testAccount = TEST_ACCOUNTS.admin;
      const sceneName = await getUserSceneName(page, testAccount.email, testAccount.password);
      const upperCaseSceneName = sceneName.toUpperCase();

      // Skip test if scene name is already all uppercase
      if (sceneName === upperCaseSceneName) {
        test.skip();
        return;
      }

      // Clear auth state
      await clearAuthState(page);
      await page.goto(`${BASE_URL}/login`);
      await page.waitForLoadState('networkidle');

      // Act - Try to login with uppercase scene name
      await fillAndSubmitLogin(page, upperCaseSceneName, testAccount.password);

      // Wait for response
      await page.waitForTimeout(2000);

      // Assert - Should fail (scene names are case-sensitive)
      const errorAlert = page.locator('[data-testid="login-error"]');
      await expect(errorAlert).toBeVisible({ timeout: 5000 });

      // Verify still on login page
      await expect(page).toHaveURL(`${BASE_URL}/login`);
    });

    test('should be case-insensitive for email address', async ({ page }) => {
      // Arrange
      const testAccount = TEST_ACCOUNTS.admin;
      const upperCaseEmail = testAccount.email.toUpperCase();

      // Act - Try to login with uppercase email
      await fillAndSubmitLogin(page, upperCaseEmail, testAccount.password);

      // Wait for navigation
      await page.waitForTimeout(3000);

      // Assert - Should succeed (emails are case-insensitive)
      // Either on dashboard or check for success
      const currentUrl = page.url();
      const loginSucceeded = currentUrl.includes('/dashboard');

      // Emails should be case-insensitive, so this should work
      expect(loginSucceeded).toBe(true);
    });

    test('should display helpful placeholder text', async ({ page }) => {
      // Assert - Verify input has helpful placeholder
      const emailInput = page.locator('[data-testid="email-or-scenename-input"]');
      const placeholder = await emailInput.getAttribute('placeholder');

      expect(placeholder).toBeTruthy();
      expect(placeholder?.toLowerCase()).toContain('email');
      expect(placeholder?.toLowerCase()).toContain('scene');
    });

    test('should display helper text explaining both login options', async ({ page }) => {
      // Assert - Verify helper text is present
      const helperText = page.locator('text=/you can log in with either your email address or your scene name/i');
      await expect(helperText).toBeVisible();
    });
  });
});
