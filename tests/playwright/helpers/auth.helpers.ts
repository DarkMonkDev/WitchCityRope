/**
 * Authentication helper utilities for WitchCityRope E2E tests
 * Manages authentication state, session reuse, and auth-related operations
 */

import { Page, Browser, BrowserContext } from '@playwright/test';
import { testConfig } from './test.config';
import { BlazorHelpers } from './blazor.helpers';
import path from 'path';
import fs from 'fs/promises';

/**
 * Authentication state interface
 */
export interface AuthState {
  email: string;
  isAuthenticated: boolean;
  roles: string[];
  userId?: string;
  displayName?: string;
  storageStatePath?: string;
}

/**
 * Login credentials interface
 */
export interface LoginCredentials {
  email: string;
  password: string;
  remember?: boolean;
}

/**
 * Authentication helper class
 */
export class AuthHelpers {
  private static authStates: Map<string, AuthState> = new Map();
  private static readonly AUTH_STORAGE_DIR = path.join(process.cwd(), 'tests', 'playwright', '.auth');

  /**
   * Initialize authentication storage directory
   */
  static async initializeAuthStorage(): Promise<void> {
    try {
      await fs.mkdir(this.AUTH_STORAGE_DIR, { recursive: true });
    } catch (error) {
      console.error('Failed to create auth storage directory:', error);
    }
  }

  /**
   * Login a user and save authentication state
   * Supports both credentials object and individual parameters
   */
  static async login(page: Page, emailOrCredentials: string | LoginCredentials, password?: string): Promise<AuthState> {
    // Handle both parameter patterns
    let credentials: LoginCredentials;
    if (typeof emailOrCredentials === 'string') {
      credentials = {
        email: emailOrCredentials,
        password: password!,
        remember: false
      };
    } else {
      credentials = emailOrCredentials;
    }

    // Navigate to login page - Playwright will prepend baseURL from config
    await page.goto(testConfig.urls.login);
    // No need to wait for Blazor - Identity pages are Razor Pages, not Blazor components

    // Fill login form using Identity page selectors
    await page.locator('input[name="Input.EmailOrUsername"]').fill(credentials.email);
    await page.locator('input[name="Input.Password"]').fill(credentials.password);

    // Check remember me if specified
    if (credentials.remember) {
      await page.locator('input[name="Input.RememberMe"]').check();
    }

    // Submit form using button selector
    await page.locator('button[type="submit"]').click();

    // Wait for successful login (redirect with ReturnUrl or away from login form)
    await page.waitForTimeout(3000); // Give time for redirect
    const currentUrl = page.url();
    
    // Check for successful login indicators
    if (!currentUrl.includes('ReturnUrl=') && currentUrl.includes('/login')) {
      throw new Error(`Login failed - still on login page: ${currentUrl}`);
    }

    // Extract user information
    const authState = await this.extractAuthState(page);
    authState.email = credentials.email;

    // Save authentication state
    const statePath = await this.saveAuthState(page, credentials.email);
    authState.storageStatePath = statePath;

    // Cache the auth state
    this.authStates.set(credentials.email, authState);

    return authState;
  }

  /**
   * Extract current authentication state from the page
   */
  static async extractAuthState(page: Page): Promise<AuthState> {
    // Wait for page to be fully loaded
    await page.waitForLoadState('networkidle');

    const authInfo = await page.evaluate(() => {
      // Try to extract auth info from various sources
      // This may need adjustment based on your actual implementation
      const authElement = document.querySelector('[data-testid="auth-state"]');
      if (authElement) {
        return JSON.parse(authElement.getAttribute('data-auth') || '{}');
      }

      // Alternative: check for user menu or profile elements
      const userMenu = document.querySelector('[data-testid="user-menu"]');
      const isAuthenticated = !!userMenu;

      // Extract display name if available
      const displayNameElement = document.querySelector('[data-testid="user-display-name"]');
      const displayName = displayNameElement?.textContent || '';

      // Extract roles from data attributes or class names
      const roles: string[] = [];
      if (document.querySelector('.admin-menu')) roles.push('Admin');
      if (document.querySelector('.teacher-menu')) roles.push('Teacher');
      if (document.querySelector('.member-menu')) roles.push('Member');

      return {
        isAuthenticated,
        displayName,
        roles
      };
    });

    return {
      email: '',
      isAuthenticated: authInfo.isAuthenticated || false,
      roles: authInfo.roles || [],
      displayName: authInfo.displayName || '',
      userId: authInfo.userId
    };
  }

  /**
   * Save authentication state to file
   */
  static async saveAuthState(page: Page, identifier: string): Promise<string> {
    await this.initializeAuthStorage();
    
    const filename = `${identifier.replace(/[^a-zA-Z0-9]/g, '_')}.json`;
    const statePath = path.join(this.AUTH_STORAGE_DIR, filename);
    
    await page.context().storageState({ path: statePath });
    
    return statePath;
  }

  /**
   * Load authentication state from file
   */
  static async loadAuthState(browser: Browser, identifier: string): Promise<BrowserContext | null> {
    const filename = `${identifier.replace(/[^a-zA-Z0-9]/g, '_')}.json`;
    const statePath = path.join(this.AUTH_STORAGE_DIR, filename);

    try {
      await fs.access(statePath);
      const context = await browser.newContext({ storageState: statePath });
      return context;
    } catch (error) {
      console.log(`No saved auth state found for ${identifier}`);
      return null;
    }
  }

  /**
   * Get or create authenticated context for a user
   */
  static async getAuthenticatedContext(
    browser: Browser, 
    credentials: LoginCredentials
  ): Promise<{ context: BrowserContext; authState: AuthState }> {
    // Try to load existing auth state
    let context = await this.loadAuthState(browser, credentials.email);
    let authState = this.authStates.get(credentials.email);

    if (context && authState) {
      // Verify the auth state is still valid
      const page = await context.newPage();
      await page.goto(testConfig.urls.home);
      // Only wait for Blazor if we're on a Blazor page
      if (!page.url().includes('/Identity/')) {
        await BlazorHelpers.waitForBlazorReady(page);
      }
      
      const currentAuthState = await this.extractAuthState(page);
      
      if (currentAuthState.isAuthenticated) {
        await page.close();
        return { context, authState };
      }
      
      // Auth state is invalid, close and re-authenticate
      await page.close();
      await context.close();
    }

    // Create new context and authenticate
    context = await browser.newContext();
    const page = await context.newPage();
    
    authState = await this.login(page, credentials);
    await page.close();

    return { context, authState };
  }

  /**
   * Get authenticated page for a specific user type
   */
  static async getAuthenticatedPage(
    browser: Browser,
    userType: keyof typeof testConfig.accounts
  ): Promise<{ page: Page; authState: AuthState }> {
    const credentials = testConfig.accounts[userType];
    const { context, authState } = await this.getAuthenticatedContext(browser, credentials);
    const page = await context.newPage();

    return { page, authState };
  }

  /**
   * Logout the current user
   */
  static async logout(page: Page): Promise<void> {
    // Only wait for Blazor if we're not on an Identity page
    if (!page.url().includes('/Identity/')) {
      await BlazorHelpers.waitForBlazorReady(page);
    }

    // Try direct navigation to logout URL first
    try {
      await page.goto(testConfig.urls.logout);
      // Wait for redirect after logout
      await page.waitForURL((url) => 
        url.pathname === '/' || url.pathname.includes('/Identity/Account/Login'),
        { timeout: 10000 }
      );
      return;
    } catch (error) {
      console.log('Direct logout navigation failed, trying UI method');
    }

    // Fallback to UI method
    // Click user menu - try multiple selectors as fallback
    const userMenuSelectors = [
      '[data-testid="user-menu-toggle"]',
      '.user-dropdown-toggle',
      '.user-menu-toggle',
      'button:has-text("Account")',
      '.navbar-user-dropdown'
    ];
    
    let menuClicked = false;
    for (const selector of userMenuSelectors) {
      const element = page.locator(selector).first();
      if (await element.isVisible()) {
        await element.click();
        menuClicked = true;
        break;
      }
    }
    
    if (!menuClicked) {
      // If no dropdown found, look for direct logout button
      const logoutButton = page.locator('button:has-text("Logout"), a:has-text("Logout"), button:has-text("Sign out"), a:has-text("Sign out")').first();
      await logoutButton.click();
    } else {
      // Wait for dropdown to open and click logout
      await page.waitForTimeout(500);
      const logoutOption = page.locator('.dropdown-item:has-text("Logout"), .dropdown-item:has-text("Sign out"), [data-testid="logout-button"]').first();
      await logoutOption.click();
    }

    // Wait for redirect to home or login page
    await page.waitForURL((url) => 
      url.pathname === '/' || url.pathname.includes('/Identity/Account/Login'),
      { timeout: 10000 }
    );
  }

  /**
   * Clear all saved authentication states
   */
  static async clearAllAuthStates(): Promise<void> {
    try {
      const files = await fs.readdir(this.AUTH_STORAGE_DIR);
      await Promise.all(
        files.map(file => fs.unlink(path.join(this.AUTH_STORAGE_DIR, file)))
      );
      this.authStates.clear();
    } catch (error) {
      console.error('Failed to clear auth states:', error);
    }
  }

  /**
   * Check if a user has a specific role
   */
  static hasRole(authState: AuthState, role: string): boolean {
    return authState.roles.includes(role);
  }

  /**
   * Check if a user is authenticated
   */
  static isAuthenticated(authState: AuthState): boolean {
    return authState.isAuthenticated;
  }

  /**
   * Handle two-factor authentication if required
   */
  static async handleTwoFactorAuth(page: Page, code: string): Promise<void> {
    // Wait for 2FA form - Identity pages don't use Blazor components
    await page.locator('[data-testid="two-factor-form"]').waitFor({ state: 'visible' });

    // Enter 2FA code
    await page.locator('[data-testid="2fa-code-input"]').fill(code);

    // Submit
    await page.locator('[data-testid="2fa-submit-button"]').click();

    // Wait for successful authentication
    await page.waitForURL((url) => !url.pathname.includes('/login'), { timeout: 10000 });
  }

  /**
   * Impersonate another user (admin feature)
   */
  static async impersonateUser(page: Page, userEmail: string): Promise<void> {
    // Navigate to admin user management
    await page.goto(testConfig.urls.adminUsers);
    // Admin pages are Blazor components
    await BlazorHelpers.waitForBlazorReady(page);

    // Search for user
    await page.locator('[data-testid="user-search"]').fill(userEmail);
    await page.keyboard.press('Enter');

    // Wait for search results
    await BlazorHelpers.waitForComponent(page, 'user-search-results');

    // Click impersonate button
    await page.locator(`[data-testid="impersonate-${userEmail}"]`).click();

    // Wait for impersonation banner
    await BlazorHelpers.waitForComponent(page, 'impersonation-banner');
  }

  /**
   * Stop impersonation and return to admin account
   */
  static async stopImpersonation(page: Page): Promise<void> {
    await page.locator('[data-testid="stop-impersonation"]').click();
    await page.waitForSelector('[data-testid="impersonation-banner"]', { state: 'hidden' });
  }

  /**
   * Get a fresh authenticated session (useful for parallel tests)
   */
  static async getFreshAuthenticatedSession(
    browser: Browser,
    userType: keyof typeof testConfig.accounts
  ): Promise<{ page: Page; context: BrowserContext; authState: AuthState }> {
    const context = await browser.newContext();
    const page = await context.newPage();
    
    const credentials = testConfig.accounts[userType];
    const authState = await this.login(page, credentials);

    return { page, context, authState };
  }

  /**
   * Verify user has access to a protected route
   */
  static async verifyRouteAccess(page: Page, route: string, expectedAccess: boolean): Promise<boolean> {
    await page.goto(route);
    // Only wait for Blazor if not on an Identity page
    if (!route.includes('/Identity/') && !page.url().includes('/Identity/')) {
      await BlazorHelpers.waitForBlazorReady(page);
    }

    const currentUrl = page.url();
    const isOnRoute = currentUrl.includes(route);
    const isOnLogin = currentUrl.includes('/login');
    const isOnAccessDenied = currentUrl.includes('/access-denied') || currentUrl.includes('/403');

    if (expectedAccess) {
      return isOnRoute && !isOnLogin && !isOnAccessDenied;
    } else {
      return isOnLogin || isOnAccessDenied || !isOnRoute;
    }
  }
}

/**
 * Convenience function export for login
 */
export const login = AuthHelpers.login.bind(AuthHelpers);