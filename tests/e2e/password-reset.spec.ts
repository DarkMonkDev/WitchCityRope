/**
 * E2E Tests: Password Reset Feature
 *
 * Feature: Users can reset their password via email link
 * Backend Implementation: Phase 3 - Password Reset
 * Service Logic: AuthenticationService.ForgotPasswordAsync() and ResetPasswordAsync()
 * Frontend Implementation: ForgotPasswordPage.tsx and ResetPasswordPage.tsx
 *
 * Test Coverage:
 * - P1: Forgot password form submission and success message
 * - P1: Reset password form with valid token
 * - P1: Password validation (min length, matching passwords)
 * - P1: Invalid/expired token handling
 * - P2: Email enumeration protection (always shows success)
 * - P2: Navigation flow (forgot -> reset -> login)
 *
 * Created: 2025-11-17
 * Test Developer: Claude AI
 */

import { test, expect, Page } from '@playwright/test';

// Test configuration - Use relative URLs with Playwright baseURL

// Test accounts
const TEST_ACCOUNTS = {
  member: {
    email: 'member@witchcityrope.com',
    password: 'Test123!',
    sceneName: 'Learning'
  }
};

/**
 * Helper: Clear authentication state
 */
async function clearAuthState(page: Page) {
  await page.context().clearCookies();
  await page.evaluate(() => localStorage.clear());
  await page.evaluate(() => sessionStorage.clear());
}

test.describe('Password Reset Feature', () => {
  test.beforeEach(async ({ page }) => {
    // Navigate to page first to establish domain context
    await page.goto('/');
    await page.waitForLoadState('domcontentloaded');

    // Clear auth state after navigation
    await clearAuthState(page);
  });

  test.describe('P1 CRITICAL: Forgot Password Flow', () => {
    test('should display forgot password form with correct elements', async ({ page }) => {
      // Act
      await page.goto('/forgot-password');
      await page.waitForLoadState('domcontentloaded');

      // Assert - Page elements
      await expect(page.locator('[data-testid="page-forgot-password"]')).toBeVisible();
      await expect(page.locator('text=Reset Password')).toBeVisible();
      await expect(page.locator('[data-testid="email-input"]')).toBeVisible();
      await expect(page.locator('[data-testid="submit-button"]')).toBeVisible();
      await expect(page.locator('[data-testid="link-back-to-login"]').first()).toBeVisible();
    });

    test('should validate email field is required', async ({ page }) => {
      // Arrange
      await page.goto('/forgot-password');
      await page.waitForLoadState('domcontentloaded');

      // Act - Try to click submit button (HTML5 required will prevent submission)
      await page.locator('[data-testid="submit-button"]').click();

      // Wait a moment for any validation
      await page.waitForTimeout(500);

      // Assert - Form is still visible (not submitted) because field is required
      await expect(page.locator('[data-testid="forgot-password-form"]')).toBeVisible();
      await expect(page.locator('[data-testid="success-message"]')).not.toBeVisible();
    });

    test('should validate email format', async ({ page }) => {
      // Arrange
      await page.goto('/forgot-password');
      await page.waitForLoadState('domcontentloaded');

      // Act - Submit invalid email
      await page.locator('[data-testid="email-input"]').fill('not-an-email');
      await page.locator('[data-testid="submit-button"]').click();

      // Assert - Validation error appears
      const emailInput = page.locator('[data-testid="email-input"]');
      await expect(emailInput).toHaveAttribute('data-error', 'true');
    });

    test('should show success message for existing account', async ({ page }) => {
      // Arrange
      await page.goto('/forgot-password');
      await page.waitForLoadState('domcontentloaded');

      // Act - Submit valid email
      await page.locator('[data-testid="email-input"]').fill(TEST_ACCOUNTS.member.email);
      await page.locator('[data-testid="submit-button"]').click();

      // Wait for API response
      await page.waitForTimeout(1000);

      // Assert - Success message appears (generic for security)
      await expect(page.locator('[data-testid="success-message"]')).toBeVisible();
      await expect(page.locator('text=If an account exists with this email')).toBeVisible();

      // Assert - Form is replaced with success message
      await expect(page.locator('[data-testid="forgot-password-form"]')).not.toBeVisible();
    });

    test('should show success message for non-existent account (email enumeration protection)', async ({ page }) => {
      // Arrange
      await page.goto('/forgot-password');
      await page.waitForLoadState('domcontentloaded');

      // Act - Submit non-existent email
      await page.locator('[data-testid="email-input"]').fill('nonexistent@example.com');
      await page.locator('[data-testid="submit-button"]').click();

      // Wait for API response
      await page.waitForTimeout(1000);

      // Assert - Same success message appears (security: no email enumeration)
      await expect(page.locator('[data-testid="success-message"]')).toBeVisible();
      await expect(page.locator('text=If an account exists with this email')).toBeVisible();
    });

    test('should navigate back to login from success page', async ({ page }) => {
      // Arrange
      await page.goto('/forgot-password');
      await page.waitForLoadState('domcontentloaded');

      // Submit form to reach success page
      await page.locator('[data-testid="email-input"]').fill(TEST_ACCOUNTS.member.email);
      await page.locator('[data-testid="submit-button"]').click();
      await page.waitForTimeout(1000);

      // Act - Click back to login link
      await page.locator('[data-testid="link-back-to-login"]').click();

      // Assert - Redirected to login page
      await page.waitForURL('/login');
      expect(page.url()).toContain('/login');
    });
  });

  test.describe('P1 CRITICAL: Reset Password Flow', () => {
    test('should show error for missing userId or token parameters', async ({ page }) => {
      // Act - Navigate without parameters
      await page.goto('/reset-password');
      await page.waitForLoadState('domcontentloaded');

      // Assert - Error message shown
      await expect(page.locator('text=Invalid Reset Link')).toBeVisible();
      await expect(page.locator('text=invalid or incomplete')).toBeVisible();
      await expect(page.locator('[data-testid="link-forgot-password"]')).toBeVisible();
    });

    test('should show error for missing userId parameter', async ({ page }) => {
      // Act - Navigate with only token
      await page.goto('/reset-password?token=abc123');
      await page.waitForLoadState('domcontentloaded');

      // Assert - Error message shown
      await expect(page.locator('text=Invalid Reset Link')).toBeVisible();
    });

    test('should show error for missing token parameter', async ({ page }) => {
      // Act - Navigate with only userId
      const mockUserId = '123e4567-e89b-12d3-a456-426614174000';
      await page.goto(`/reset-password?userId=${mockUserId}`);
      await page.waitForLoadState('domcontentloaded');

      // Assert - Error message shown
      await expect(page.locator('text=Invalid Reset Link')).toBeVisible();
    });

    test('should display reset password form with valid parameters', async ({ page }) => {
      // Arrange - Mock valid parameters
      const mockUserId = '123e4567-e89b-12d3-a456-426614174000';
      const mockToken = 'CfDJ8MockTokenString123';

      // Act
      await page.goto(`/reset-password?userId=${mockUserId}&token=${encodeURIComponent(mockToken)}`);
      await page.waitForLoadState('domcontentloaded');

      // Assert - Form elements present
      await expect(page.locator('[data-testid="page-reset-password"]')).toBeVisible();
      await expect(page.locator('text=Set New Password')).toBeVisible();
      await expect(page.locator('[data-testid="new-password-input"]')).toBeVisible();
      await expect(page.locator('[data-testid="confirm-password-input"]')).toBeVisible();
      await expect(page.locator('[data-testid="submit-button"]')).toBeVisible();
      await expect(page.locator('[data-testid="link-back-to-login"]')).toBeVisible();
    });

    test('should validate password minimum length', async ({ page }) => {
      // Arrange
      const mockUserId = '123e4567-e89b-12d3-a456-426614174000';
      const mockToken = 'CfDJ8MockTokenString123';
      await page.goto(`${BASE_URL}/reset-password?userId=${mockUserId}&token=${encodeURIComponent(mockToken)}`);
      await page.waitForLoadState('domcontentloaded');

      // Act - Enter short password
      await page.locator('[data-testid="new-password-input"]').fill('Test1!');
      await page.locator('[data-testid="confirm-password-input"]').fill('Test1!');
      await page.locator('[data-testid="submit-button"]').click();

      // Assert - Validation error
      const passwordInput = page.locator('[data-testid="new-password-input"]');
      await expect(passwordInput).toHaveAttribute('data-invalid', 'true');
    });

    test('should validate passwords match', async ({ page }) => {
      // Arrange
      const mockUserId = '123e4567-e89b-12d3-a456-426614174000';
      const mockToken = 'CfDJ8MockTokenString123';
      await page.goto(`${BASE_URL}/reset-password?userId=${mockUserId}&token=${encodeURIComponent(mockToken)}`);
      await page.waitForLoadState('domcontentloaded');

      // Act - Enter non-matching passwords
      await page.locator('[data-testid="new-password-input"]').fill('NewPassword123!');
      await page.locator('[data-testid="confirm-password-input"]').fill('DifferentPassword123!');
      await page.locator('[data-testid="submit-button"]').click();

      // Assert - Validation error on confirm password
      const confirmInput = page.locator('[data-testid="confirm-password-input"]');
      await expect(confirmInput).toHaveAttribute('data-invalid', 'true');
    });

    test('should show error for invalid/expired token', async ({ page }) => {
      // Arrange - Use invalid token
      const mockUserId = '123e4567-e89b-12d3-a456-426614174000';
      const invalidToken = 'invalid-token';
      await page.goto(`${BASE_URL}/reset-password?userId=${mockUserId}&token=${encodeURIComponent(invalidToken)}`);
      await page.waitForLoadState('domcontentloaded');

      // Act - Submit form with valid passwords
      await page.locator('[data-testid="new-password-input"]').fill('NewPassword123!');
      await page.locator('[data-testid="confirm-password-input"]').fill('NewPassword123!');
      await page.locator('[data-testid="submit-button"]').click();

      // Wait for API response
      await page.waitForTimeout(2000);

      // Assert - Error message appears
      await expect(page.locator('[data-testid="error-message"]')).toBeVisible();
    });

    test('should navigate back to login from form', async ({ page }) => {
      // Arrange
      const mockUserId = '123e4567-e89b-12d3-a456-426614174000';
      const mockToken = 'CfDJ8MockTokenString123';
      await page.goto(`${BASE_URL}/reset-password?userId=${mockUserId}&token=${encodeURIComponent(mockToken)}`);
      await page.waitForLoadState('domcontentloaded');

      // Act - Click back to login
      await page.locator('[data-testid="link-back-to-login"]').click();

      // Assert - Redirected to login
      await page.waitForURL(`${BASE_URL}/login`);
      expect(page.url()).toBe(`${BASE_URL}/login`);
    });
  });

  test.describe('P2: Navigation Flow', () => {
    test('should have reset password link on login page', async ({ page }) => {
      // Act
      await page.goto('/login');
      await page.waitForLoadState('domcontentloaded');

      // Assert - Reset password link exists
      const resetLink = page.locator('[data-testid="link-forgot-password"]');
      await expect(resetLink).toBeVisible();
      await expect(resetLink).toHaveText('Reset Password');

      // Act - Click link
      await resetLink.click();

      // Assert - Redirected to forgot password page
      await page.waitForURL('/forgot-password');
      expect(page.url()).toContain('/forgot-password');
    });

    test('should navigate from forgot password back to login', async ({ page }) => {
      // Arrange
      await page.goto('/forgot-password');
      await page.waitForLoadState('domcontentloaded');

      // Act - Click back to login
      await page.locator('[data-testid="link-back-to-login"]').first().click();

      // Assert - Redirected to login
      await page.waitForURL(`${BASE_URL}/login`);
      expect(page.url()).toBe(`${BASE_URL}/login`);
    });

    test('should navigate from invalid reset link to forgot password', async ({ page }) => {
      // Arrange - Navigate without parameters
      await page.goto(`${BASE_URL}/reset-password`);
      await page.waitForLoadState('domcontentloaded');

      // Act - Click request new link
      await page.locator('[data-testid="link-forgot-password"]').click();

      // Assert - Redirected to forgot password
      await page.waitForURL(`${BASE_URL}/forgot-password`);
      expect(page.url()).toBe(`${BASE_URL}/forgot-password`);
    });
  });
});
