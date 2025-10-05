import { Page, expect } from '@playwright/test';
import { WaitHelpers, TIMEOUTS } from './wait.helpers';

export interface TestCredentials {
  email: string;
  password: string;
}

/**
 * Authentication helpers for Playwright tests
 * Uses actual UI selectors and text from React implementation
 */
export class AuthHelpers {
  // Test account credentials matching seeded data
  static readonly accounts = {
    admin: { email: 'admin@witchcityrope.com', password: 'Test123!' },
    teacher: { email: 'teacher@witchcityrope.com', password: 'Test123!' },
    vetted: { email: 'vetted@witchcityrope.com', password: 'Test123!' },
    member: { email: 'member@witchcityrope.com', password: 'Test123!' },
    guest: { email: 'guest@witchcityrope.com', password: 'Test123!' }
  } as const;

  /**
   * Login with specific user role using data-testid selectors
   * Matches current React LoginPage.tsx implementation
   * CRITICAL: Uses ABSOLUTE URLs to ensure cookies persist properly
   */
  static async loginAs(page: Page, role: keyof typeof AuthHelpers.accounts) {
    const credentials = this.accounts[role];

    // Clear auth state safely first
    await this.clearAuthState(page);

    // Navigate to login page using ABSOLUTE URL (required for cookie persistence)
    await page.goto('http://localhost:5173/login');
    await page.waitForLoadState('networkidle');

    // Fill form using data-testid selectors (lines 171, 214 in LoginPage.tsx)
    await page.locator('[data-testid="email-input"]').fill(credentials.email);
    await page.locator('[data-testid="password-input"]').fill(credentials.password);

    // Click login button using data-testid (line 270 in LoginPage.tsx)
    await page.locator('[data-testid="login-button"]').click();

    // Wait for successful authentication redirect with networkidle
    await page.waitForURL('**/dashboard', { timeout: 10000 });
    await page.waitForLoadState('networkidle');

    return credentials;
  }

  /**
   * Login with custom credentials
   * CRITICAL: Uses ABSOLUTE URLs to ensure cookies persist properly
   */
  static async loginWith(page: Page, credentials: TestCredentials) {
    // Navigate to login page using ABSOLUTE URL (required for cookie persistence)
    await page.goto('http://localhost:5173/login');
    await page.waitForLoadState('networkidle');

    // Fill form using data-testid selectors
    await page.locator('[data-testid="email-input"]').fill(credentials.email);
    await page.locator('[data-testid="password-input"]').fill(credentials.password);

    // Click login button
    await page.locator('[data-testid="login-button"]').click();

    // Wait for successful authentication redirect with networkidle
    await page.waitForURL('**/dashboard', { timeout: 10000 });
    await page.waitForLoadState('networkidle');
  }

  /**
   * Attempt login and expect failure
   * CRITICAL: Uses ABSOLUTE URLs to ensure cookies persist properly
   */
  static async loginExpectingError(page: Page, credentials: TestCredentials, expectedError?: string) {
    // Navigate to login page using ABSOLUTE URL (required for cookie persistence)
    await page.goto('http://localhost:5173/login');
    await page.waitForLoadState('networkidle');

    // Fill form using data-testid selectors
    await page.locator('[data-testid="email-input"]').fill(credentials.email);
    await page.locator('[data-testid="password-input"]').fill(credentials.password);

    // Click login button
    await page.locator('[data-testid="login-button"]').click();

    // Wait for error alert to appear (data-testid from line 146 in LoginPage.tsx)
    await expect(page.locator('[data-testid="login-error"]')).toBeVisible({ timeout: TIMEOUTS.MEDIUM });

    if (expectedError) {
      await expect(page.locator('[data-testid="login-error"]')).toContainText(expectedError, { timeout: TIMEOUTS.SHORT });
    }

    return page.locator('[data-testid="login-error"]').textContent();
  }

  /**
   * Logout user and verify redirect to login page
   * Enhanced with better timeout handling and API monitoring
   */
  static async logout(page: Page) {
    // Look for logout button in navigation or user menu
    const logoutButton = page.locator('[data-testid="logout-button"], button:has-text("Logout"), button:has-text("Sign Out")');
    
    if (await logoutButton.count() > 0) {
      // Monitor logout API call
      const logoutResponsePromise = WaitHelpers.waitForApiResponse(page, '/api/auth/logout', {
        method: 'POST',
        timeout: TIMEOUTS.MEDIUM
      }).catch(() => {
        // Logout might not always call an API, so don't fail on this
        console.log('No logout API call detected, proceeding with navigation check');
      });
      
      await logoutButton.click();
      
      // Wait for logout response (if any)
      await logoutResponsePromise;
      
      // Wait for navigation with network idle
      await WaitHelpers.waitForNavigation(page, '/login', TIMEOUTS.MEDIUM);
    } else {
      // If no logout button visible, clear auth state manually
      await this.clearAuth(page);
    }
    
    // Verify we're on login page with timeout
    await expect(page.locator('h1')).toContainText('Welcome Back', { timeout: TIMEOUTS.MEDIUM });
  }

  /**
   * Check if user is currently authenticated
   * Enhanced with better timeout handling
   */
  static async isAuthenticated(page: Page): Promise<boolean> {
    try {
      // Try to access a protected route
      await page.goto('/dashboard');
      await page.waitForURL('/dashboard', { timeout: TIMEOUTS.MEDIUM });
      
      // Wait for page to load to ensure we're not just seeing a redirect
      await WaitHelpers.waitForPageLoad(page, '/dashboard', TIMEOUTS.SHORT);
      
      return true;
    } catch {
      // If redirected to login or timeout, user is not authenticated
      const currentUrl = page.url();
      return !currentUrl.includes('/login');
    }
  }

  /**
   * Clear all authentication state safely for test setup
   * Use this in beforeEach hooks to ensure clean state
   * CRITICAL: Uses ABSOLUTE URL to ensure cookies persist properly
   */
  static async clearAuthState(page: Page) {
    // Use Playwright's storage state API - most reliable method
    await page.context().clearCookies();
    await page.context().clearPermissions();

    try {
      // Navigate to login page using ABSOLUTE URL first to establish context
      await page.goto('http://localhost:5173/login');
      await page.waitForLoadState('networkidle');

      // Clear storage after page is loaded
      await page.evaluate(() => {
        if (typeof localStorage !== 'undefined') localStorage.clear();
        if (typeof sessionStorage !== 'undefined') sessionStorage.clear();
      });
    } catch (error) {
      // If storage clearing fails, at least cookies are cleared
      console.warn('Storage clearing failed, but cookies cleared:', error);
    }
  }

  /**
   * Clear all authentication state safely
   * @deprecated Use clearAuthState() instead for better reliability
   */
  static async clearAuth(page: Page) {
    return this.clearAuthState(page);
  }

  /**
   * Wait for login button to be enabled (handles loading states)
   * Enhanced with better timeout handling
   */
  static async waitForLoginReady(page: Page) {
    // Wait for all form elements to be visible and ready
    await page.locator('[data-testid="email-input"]').waitFor({ 
      state: 'visible', 
      timeout: TIMEOUTS.MEDIUM 
    });
    await page.locator('[data-testid="password-input"]').waitFor({ 
      state: 'visible', 
      timeout: TIMEOUTS.MEDIUM 
    });
    await page.locator('[data-testid="login-button"]').waitFor({ 
      state: 'visible', 
      timeout: TIMEOUTS.MEDIUM 
    });
    
    // Wait for login button to be enabled
    await expect(page.locator('[data-testid="login-button"]')).not.toHaveAttribute('disabled', {
      timeout: TIMEOUTS.MEDIUM
    });
    
    // Additional wait for React hydration
    await page.waitForTimeout(200);
  }

  /**
   * Verify login form elements are present and functional
   */
  static async verifyLoginFormElements(page: Page) {
    // Check all required form elements exist
    await expect(page.locator('[data-testid="email-input"]')).toBeVisible();
    await expect(page.locator('[data-testid="password-input"]')).toBeVisible();
    await expect(page.locator('[data-testid="login-button"]')).toBeVisible();
    
    // Verify form can be interacted with
    await page.locator('[data-testid="email-input"]').fill('test@test.com');
    await expect(page.locator('[data-testid="email-input"]')).toHaveValue('test@test.com');
    
    await page.locator('[data-testid="password-input"]').fill('testpassword');
    await expect(page.locator('[data-testid="password-input"]')).toHaveValue('testpassword');
    
    // Clear for clean state
    await page.locator('[data-testid="email-input"]').clear();
    await page.locator('[data-testid="password-input"]').clear();
  }

  /**
   * Handle registration flow with unique credentials
   */
  static async registerNewUser(page: Page, baseSceneName: string = 'TestUser') {
    const uniqueId = Date.now();
    const credentials = {
      email: `test-${uniqueId}@example.com`,
      password: 'StrongPass123!',
      sceneName: `${baseSceneName}${uniqueId}`
    };

    await page.goto('/register');
    
    // Fill registration form (assuming similar data-testid pattern)
    await page.locator('[data-testid="email-input"]').fill(credentials.email);
    await page.locator('[data-testid="scene-name-input"]').fill(credentials.sceneName);
    await page.locator('[data-testid="password-input"]').fill(credentials.password);
    
    // Submit registration
    await page.locator('[data-testid="register-button"], button[type="submit"]').click();
    
    return credentials;
  }

  /**
   * Retry authentication with exponential backoff
   * Useful for handling flaky authentication flows
   */
  static async retryAuthenticationWith<T>(
    operation: () => Promise<T>,
    maxRetries: number = 3,
    baseDelay: number = 1000
  ): Promise<T> {
    for (let attempt = 1; attempt <= maxRetries; attempt++) {
      try {
        return await operation();
      } catch (error) {
        if (attempt === maxRetries) {
          throw error;
        }
        
        // Exponential backoff: 1s, 2s, 4s, etc.
        const delay = baseDelay * Math.pow(2, attempt - 1);
        console.log(`Authentication attempt ${attempt} failed, retrying in ${delay}ms...`);
        await new Promise(resolve => setTimeout(resolve, delay));
      }
    }
    
    throw new Error('This should never be reached');
  }

  /**
   * Login with retry mechanism for flaky authentication
   */
  static async loginAsWithRetry(page: Page, role: keyof typeof AuthHelpers.accounts, maxRetries: number = 2) {
    return this.retryAuthenticationWith(
      () => this.loginAs(page, role),
      maxRetries
    );
  }

  /**
   * Wait for dashboard to be fully loaded and interactive
   * Enhanced validation for authentication success
   */
  static async waitForDashboardReady(page: Page) {
    // Wait for URL to be dashboard
    await page.waitForURL('/dashboard', { 
      timeout: TIMEOUTS.AUTHENTICATION,
      waitUntil: 'networkidle' 
    });

    // Wait for dashboard page to load completely
    await WaitHelpers.waitForPageLoad(page, '/dashboard', TIMEOUTS.MEDIUM);

    // Look for dashboard-specific elements that indicate successful load
    const dashboardIndicators = [
      '[data-testid="dashboard-content"]',
      '[data-testid="user-welcome"]',
      '[data-testid="dashboard-navigation"]',
      'nav', // Fallback for navigation element
      'main' // Fallback for main content
    ];

    // Wait for at least one dashboard indicator to be present
    let indicatorFound = false;
    for (const indicator of dashboardIndicators) {
      const element = page.locator(indicator);
      if (await element.count() > 0) {
        await expect(element).toBeVisible({ timeout: TIMEOUTS.MEDIUM });
        indicatorFound = true;
        break;
      }
    }

    if (!indicatorFound) {
      console.warn('No dashboard indicators found, but URL is correct');
    }

    // Final verification: ensure we're not in a loading state
    await page.waitForTimeout(500);
    
    // Verify no loading spinners are present
    const loadingElements = page.locator('[data-testid*="loading"], .loading, .spinner');
    if (await loadingElements.count() > 0) {
      await expect(loadingElements).not.toBeVisible({ timeout: TIMEOUTS.MEDIUM });
    }
  }
}