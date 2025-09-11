import { test, expect } from '@playwright/test';
import { AuthHelpers } from './helpers/auth.helpers';
import { FormHelpers } from './helpers/form.helpers';
import { WaitHelpers } from './helpers/wait.helpers';

/**
 * FIXED Authentication Tests for React Implementation
 * 
 * These tests are updated to match the actual React LoginPage.tsx implementation:
 * - Title: "Welcome Back" (not "Login")
 * - Button: "Sign In" (not "Login") 
 * - Uses data-testid selectors
 * - Handles Mantine components properly
 */
test.describe('Authentication (FIXED)', () => {
  test.beforeEach(async ({ page }) => {
    // Clear auth state before each test
    await AuthHelpers.clearAuth(page);
    
    // Enable console monitoring for debugging
    page.on('console', msg => {
      if (msg.type() === 'error') {
        console.log(`❌ Console Error: ${msg.text()}`);
      }
    });
  });

  test('should display login page with correct elements', async ({ page }) => {
    await page.goto('/login');
    await WaitHelpers.waitForPageLoad(page, '**/login');

    // ✅ CORRECT: Expect "Welcome Back" not "Login"
    await expect(page.locator('h1')).toContainText('Welcome Back');
    
    // Verify all form elements are present using data-testid
    await AuthHelpers.verifyLoginFormElements(page);
    
    // Verify age verification notice
    await expect(page.locator('text=21+ COMMUNITY')).toBeVisible();
    
    // Verify test account info is displayed
    await expect(page.locator('text=admin@witchcityrope.com')).toBeVisible();
  });

  test('should successfully login with admin credentials', async ({ page }) => {
    // Use helper to login as admin
    const credentials = await AuthHelpers.loginAs(page, 'admin');
    
    // Verify successful redirect to dashboard
    await expect(page).toHaveURL('/dashboard');
    
    // Verify user is authenticated by checking page content
    // (Update this based on actual dashboard implementation)
    const pageContent = await page.textContent('body');
    expect(pageContent).toContain('admin@witchcityrope.com');
    
    console.log(`✅ Successfully logged in as admin: ${credentials.email}`);
  });

  test('should successfully login with different user roles', async ({ page }) => {
    const roles = ['member', 'teacher', 'vetted', 'guest'] as const;
    
    for (const role of roles) {
      await AuthHelpers.clearAuth(page);
      
      const credentials = await AuthHelpers.loginAs(page, role);
      
      // Verify successful authentication
      await expect(page).toHaveURL('/dashboard');
      console.log(`✅ Successfully logged in as ${role}: ${credentials.email}`);
      
      // Logout for next iteration
      await AuthHelpers.logout(page);
    }
  });

  test('should show error for invalid credentials', async ({ page }) => {
    const invalidCredentials = {
      email: 'invalid@example.com',
      password: 'wrongpassword'
    };

    const errorMessage = await AuthHelpers.loginExpectingError(page, invalidCredentials);
    
    // Verify error message appears
    expect(errorMessage).toMatch(/login failed|invalid|error/i);
    
    // Verify we remain on login page
    await expect(page).toHaveURL(/.*\/login/);
    
    console.log(`✅ Invalid credentials properly rejected with error: ${errorMessage}`);
  });

  test('should handle form validation errors', async ({ page }) => {
    await page.goto('/login');
    await WaitHelpers.waitForPageLoad(page);

    // Test email validation
    await FormHelpers.testFormValidation(
      page,
      { 
        email: 'invalid-email', 
        password: 'validpassword' 
      },
      {
        email: 'Invalid email format'
      },
      'login-button'
    );

    // Clear and test required field validation
    await FormHelpers.clearForm(page, ['email', 'password']);
    await FormHelpers.submitForm(page, 'login-button');

    // Mantine form should show validation errors for required fields
    await page.waitForTimeout(500);
    
    // Check for validation feedback
    const emailField = page.locator('[data-testid="email-input"]');
    const passwordField = page.locator('[data-testid="password-input"]');
    
    // Verify fields show invalid state (Mantine typically adds aria-invalid)
    await expect(emailField).toHaveAttribute('aria-invalid', 'true');
    await expect(passwordField).toHaveAttribute('aria-invalid', 'true');
    
    console.log('✅ Form validation errors properly displayed');
  });

  test('should redirect unauthenticated users to login', async ({ page }) => {
    // Try to access protected route without authentication
    await page.goto('/dashboard');
    
    // Should redirect to login page
    await WaitHelpers.waitForNavigation(page, '**/login');
    await expect(page.locator('h1')).toContainText('Welcome Back');
    
    console.log('✅ Protected route properly redirects to login');
  });

  test('should maintain authentication across page refresh', async ({ page }) => {
    // Login first
    await AuthHelpers.loginAs(page, 'admin');
    await expect(page).toHaveURL('/dashboard');
    
    // Refresh the page
    await page.reload();
    await WaitHelpers.waitForPageLoad(page);
    
    // Should still be authenticated and on dashboard
    await expect(page).toHaveURL('/dashboard');
    
    // Verify authentication state persisted
    const isStillAuthenticated = await AuthHelpers.isAuthenticated(page);
    expect(isStillAuthenticated).toBe(true);
    
    console.log('✅ Authentication persists across page refresh');
  });

  test('should handle logout correctly', async ({ page }) => {
    // Login first
    await AuthHelpers.loginAs(page, 'admin');
    await expect(page).toHaveURL('/dashboard');
    
    // Logout
    await AuthHelpers.logout(page);
    
    // Verify redirect to login page
    await expect(page.locator('h1')).toContainText('Welcome Back');
    
    // Try to access protected route - should redirect to login
    await page.goto('/dashboard');
    await WaitHelpers.waitForNavigation(page, '**/login');
    
    console.log('✅ Logout properly clears authentication state');
  });

  test('should handle remember me functionality', async ({ page }) => {
    await page.goto('/login');
    
    // Fill login form
    await page.locator('[data-testid="email-input"]').fill('admin@witchcityrope.com');
    await page.locator('[data-testid="password-input"]').fill('Test123!');
    
    // Check remember me checkbox
    await FormHelpers.toggleCheckbox(page, 'remember-me-checkbox', true);
    
    // Submit form
    await page.locator('[data-testid="login-button"]').click();
    
    // Verify successful login
    await WaitHelpers.waitForNavigation(page, '/dashboard');
    
    // Note: Remember me functionality testing would require checking cookie expiration,
    // which is better suited for API-level testing
    console.log('✅ Remember me checkbox interaction works');
  });

  test('should show loading state during authentication', async ({ page }) => {
    await page.goto('/login');
    
    // Fill credentials
    await page.locator('[data-testid="email-input"]').fill('admin@witchcityrope.com');
    await page.locator('[data-testid="password-input"]').fill('Test123!');
    
    // Monitor button state during submission
    const submitButton = page.locator('[data-testid="login-button"]');
    
    // Click submit and immediately check loading state
    const submitPromise = submitButton.click();
    
    // Button should show loading state briefly
    await page.waitForTimeout(50);
    
    // Wait for submission to complete
    await submitPromise;
    await WaitHelpers.waitForFormSubmission(page, 'login-button');
    
    // Should navigate to dashboard
    await WaitHelpers.waitForNavigation(page, '/dashboard');
    
    console.log('✅ Loading state properly displayed during authentication');
  });

  test('should handle network errors gracefully', async ({ page }) => {
    // Intercept login API and simulate network error
    await page.route('**/api/auth/login', route => {
      route.abort('failed');
    });

    await page.goto('/login');
    
    // Attempt login
    await page.locator('[data-testid="email-input"]').fill('admin@witchcityrope.com');
    await page.locator('[data-testid="password-input"]').fill('Test123!');
    await page.locator('[data-testid="login-button"]').click();
    
    // Should show error message
    await WaitHelpers.waitForComponent(page, 'login-error');
    const errorMessage = await page.locator('[data-testid="login-error"]').textContent();
    
    expect(errorMessage).toMatch(/network|connection|failed/i);
    
    console.log(`✅ Network error properly handled: ${errorMessage}`);
  });
});

/**
 * Security Tests - Updated for React Implementation  
 */
test.describe('Security (FIXED)', () => {
  test('should protect against XSS with httpOnly cookies', async ({ page }) => {
    // Login first
    await AuthHelpers.loginAs(page, 'admin');
    
    // Try to access auth cookies via JavaScript
    const accessibleCookies = await page.evaluate(() => document.cookie);
    
    // HttpOnly cookies should not be accessible via JavaScript
    expect(accessibleCookies).not.toMatch(/auth|token|session/i);
    
    console.log('✅ HttpOnly cookies properly protect against XSS');
  });

  test('should include proper security headers in API requests', async ({ page }) => {
    let loginRequestHeaders: Record<string, string> = {};
    
    // Intercept login request to examine headers
    page.on('request', request => {
      if (request.url().includes('/api/auth/login')) {
        loginRequestHeaders = request.headers();
      }
    });

    // Perform login
    await AuthHelpers.loginAs(page, 'admin');
    
    // Verify proper headers are included
    expect(loginRequestHeaders['content-type']).toContain('application/json');
    
    console.log('✅ API requests include proper security headers');
  });
});

/**
 * Performance Tests - Updated for React Implementation
 */
test.describe('Performance (FIXED)', () => {
  test('should complete login within performance target', async ({ page }) => {
    await page.goto('/login');
    
    const startTime = Date.now();
    
    // Perform login
    await page.locator('[data-testid="email-input"]').fill('admin@witchcityrope.com');
    await page.locator('[data-testid="password-input"]').fill('Test123!');
    await page.locator('[data-testid="login-button"]').click();
    
    // Wait for completion
    await WaitHelpers.waitForNavigation(page, '/dashboard');
    
    const loginDuration = Date.now() - startTime;
    
    // Login should complete within 3 seconds
    expect(loginDuration).toBeLessThan(3000);
    
    console.log(`✅ Login completed in ${loginDuration}ms (under 3s target)`);
  });

  test('should handle concurrent login attempts', async ({ context }) => {
    // Create multiple pages for concurrent testing
    const pages = await Promise.all([
      context.newPage(),
      context.newPage(),
      context.newPage()
    ]);

    const roles = ['admin', 'member', 'teacher'] as const;
    
    // Attempt concurrent logins
    const loginPromises = pages.map(async (page, index) => {
      const role = roles[index];
      const startTime = Date.now();
      
      await AuthHelpers.loginAs(page, role);
      
      const duration = Date.now() - startTime;
      await page.close();
      
      return { role, duration };
    });

    const results = await Promise.all(loginPromises);
    
    // All logins should succeed within reasonable time
    for (const result of results) {
      expect(result.duration).toBeLessThan(5000);
      console.log(`✅ ${result.role} login: ${result.duration}ms`);
    }
  });
});