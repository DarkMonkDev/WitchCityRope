/**
 * Navigation Workflow Tests
 *
 * Tests role-based navigation visibility and functionality across:
 * - Guest (unauthenticated)
 * - Member (unvetted)
 * - Vetted Member
 * - Admin
 * - Mobile hamburger menu
 *
 * STANDARDS:
 * - Uses AuthHelpers.loginAs() for authentication (NEVER manual login)
 * - Uses domcontentloaded NOT networkidle for wait strategy
 * - Tests FUNCTIONALITY not CSS classes
 * - Focuses on business rules, not UI design elements
 */

import { test, expect } from '@playwright/test';
import { AuthHelpers } from './test-utils/helpers/auth.helpers';

// Environment-aware URLs for container/host compatibility
const WEB_BASE_URL = process.env.PLAYWRIGHT_BASE_URL || 'http://localhost:5173';
const API_BASE_URL = process.env.API_URL || 'http://localhost:5655';


test.describe('Navigation Workflow - Role-Based Visibility', () => {
  test.beforeEach(async ({ page }) => {
    // Clear auth state before each test
    await AuthHelpers.clearAuthState(page);
  });

  test('Guest (unauthenticated) - sees public pages and login button', async ({ page }) => {
    // Navigate to homepage
    await page.goto('/');
    await page.waitForLoadState('domcontentloaded');

    // Verify logo is visible and clickable
    const logo = page.locator('.logo');
    await expect(logo).toBeVisible();
    await expect(logo).toHaveAttribute('href', '/');

    // Verify public navigation items are visible
    await expect(page.getByTestId('link-events')).toBeVisible();
    await expect(page.getByTestId('link-events')).toContainText('Events & Classes');

    // Verify "How to Join" is visible for unauthenticated users
    // (useMenuVisibility hook shows menu when not authenticated)
    const howToJoinLink = page.locator('a[href="/join"]').first();
    await expect(howToJoinLink).toBeVisible();
    await expect(howToJoinLink).toContainText('How to Join');

    // Verify Resources is visible
    const resourcesLink = page.locator('a[href="/resources"]').first();
    await expect(resourcesLink).toBeVisible();
    await expect(resourcesLink).toContainText('Resources');

    // Verify Login button is visible (NOT Dashboard)
    const loginButton = page.locator('a[href="/login"]').first();
    await expect(loginButton).toBeVisible();
    await expect(loginButton).toContainText('Login');

    // Verify Admin link is NOT visible
    await expect(page.getByTestId('link-admin')).not.toBeVisible();

    // Verify Dashboard link is NOT visible
    await expect(page.getByTestId('link-dashboard')).not.toBeVisible();
  });

  test('Member (unvetted) - sees dashboard and member features', async ({ page }) => {
    // Login as member
    await AuthHelpers.loginAs(page, 'member');

    // Navigate to homepage to check navigation
    await page.goto('/');
    await page.waitForLoadState('domcontentloaded');

    // Verify Dashboard button is visible
    await expect(page.getByTestId('link-dashboard')).toBeVisible();
    await expect(page.getByTestId('link-dashboard')).toContainText('Dashboard');

    // Verify public navigation items are still visible
    await expect(page.getByTestId('link-events')).toBeVisible();

    // Verify "How to Join" visibility depends on vetting status
    // Member account may or may not have application, so we just verify navigation works
    // (Not asserting visibility - depends on application status)

    // Verify Resources is visible
    const resourcesLink = page.locator('a[href="/resources"]').first();
    await expect(resourcesLink).toBeVisible();

    // Verify Admin link is NOT visible for regular member
    await expect(page.getByTestId('link-admin')).not.toBeVisible();

    // Verify Login button is NOT visible (replaced by Dashboard)
    const loginButton = page.locator('a[href="/login"]').first();
    await expect(loginButton).not.toBeVisible();

    // Verify Dashboard link is clickable and navigates correctly
    await page.getByTestId('link-dashboard').click();
    await page.waitForURL('**/dashboard');
    await page.waitForLoadState('domcontentloaded');
    await expect(page.url()).toContain('/dashboard');
  });

  test('Vetted Member - sees full member features', async ({ page }) => {
    // Login as vetted member
    await AuthHelpers.loginAs(page, 'vetted');

    // Navigate to homepage to check navigation
    await page.goto('/');
    await page.waitForLoadState('domcontentloaded');

    // Verify Dashboard button is visible
    await expect(page.getByTestId('link-dashboard')).toBeVisible();

    // Verify Events is visible
    await expect(page.getByTestId('link-events')).toBeVisible();

    // Verify "How to Join" should be HIDDEN for vetted member (approved status)
    // useMenuVisibility hook hides menu for Approved status
    const howToJoinLinks = page.locator('a[href="/join"]');
    const count = await howToJoinLinks.count();

    // If "How to Join" exists, it should not be in the main navigation
    // (It might exist in footer or other locations, so we check main nav specifically)
    const mainNavHowToJoin = page.locator('.nav a[href="/join"]');
    await expect(mainNavHowToJoin).not.toBeVisible();

    // Verify Resources is visible
    const resourcesLink = page.locator('a[href="/resources"]').first();
    await expect(resourcesLink).toBeVisible();

    // Verify Admin link is NOT visible for vetted member
    await expect(page.getByTestId('link-admin')).not.toBeVisible();

    // Verify Events navigation works
    await page.getByTestId('link-events').click();
    await page.waitForURL('**/events');
    await page.waitForLoadState('domcontentloaded');
    await expect(page.url()).toContain('/events');
  });

  test('Admin - sees admin menu and all features', async ({ page }) => {
    // Login as admin
    await AuthHelpers.loginAs(page, 'admin');

    // Navigate to homepage to check navigation
    await page.goto('/');
    await page.waitForLoadState('domcontentloaded');

    // Verify Admin link is visible
    await expect(page.getByTestId('link-admin')).toBeVisible();
    await expect(page.getByTestId('link-admin')).toContainText('Admin');

    // Verify Dashboard button is visible
    await expect(page.getByTestId('link-dashboard')).toBeVisible();

    // Verify Events is visible
    await expect(page.getByTestId('link-events')).toBeVisible();

    // Verify Resources is visible
    const resourcesLink = page.locator('a[href="/resources"]').first();
    await expect(resourcesLink).toBeVisible();

    // Verify Admin link is clickable and navigates correctly
    await page.getByTestId('link-admin').click();
    await page.waitForURL('**/admin');
    await page.waitForLoadState('domcontentloaded');
    await expect(page.url()).toContain('/admin');

    // Navigate back to verify other links work
    await page.goto('/');
    await page.waitForLoadState('domcontentloaded');

    // Verify Dashboard navigation
    await page.getByTestId('link-dashboard').click();
    await page.waitForURL('**/dashboard');
    await page.waitForLoadState('domcontentloaded');
    await expect(page.url()).toContain('/dashboard');
  });

  test('Mobile hamburger menu - opens and displays navigation items', async ({ page }) => {
    // Set mobile viewport
    await page.setViewportSize({ width: 375, height: 667 }); // iPhone SE size

    // Navigate to homepage as guest
    await page.goto('/');
    await page.waitForLoadState('domcontentloaded');

    // Verify hamburger menu button is visible
    const hamburgerButton = page.getByTestId('button-mobile-menu');
    await expect(hamburgerButton).toBeVisible();

    // Verify desktop navigation is hidden (CSS media query)
    // We just verify the hamburger is visible, indicating mobile mode

    // Click hamburger to open menu
    await hamburgerButton.click();

    // Wait for mobile menu to open
    const mobileMenu = page.locator('#mobile-menu');
    await expect(mobileMenu).toBeVisible();

    // Verify mobile menu has correct accessibility attributes
    await expect(mobileMenu).toHaveAttribute('role', 'navigation');
    await expect(mobileMenu).toHaveAttribute('aria-label', 'Mobile navigation menu');

    // Verify hamburger button aria-expanded is true
    await expect(hamburgerButton).toHaveAttribute('aria-expanded', 'true');

    // Verify mobile navigation items are visible
    await expect(page.getByTestId('mobile-link-events')).toBeVisible();
    await expect(page.getByTestId('mobile-link-events')).toContainText('Events & Classes');

    // Verify Private Lessons link (mobile-only)
    await expect(page.getByTestId('mobile-link-private-lessons')).toBeVisible();
    await expect(page.getByTestId('mobile-link-private-lessons')).toContainText('Private Lessons');

    // Verify Resources link
    await expect(page.getByTestId('mobile-link-resources')).toBeVisible();
    await expect(page.getByTestId('mobile-link-resources')).toContainText('Resources');

    // Verify Report Incident link (mobile-only)
    await expect(page.getByTestId('mobile-link-report-incident')).toBeVisible();
    await expect(page.getByTestId('mobile-link-report-incident')).toContainText('Report an Incident');

    // Verify Login button is visible (guest user)
    await expect(page.getByTestId('mobile-link-login')).toBeVisible();
    await expect(page.getByTestId('mobile-link-login')).toContainText('Login');

    // Verify overlay is visible
    const overlay = page.locator('.mobile-menu-overlay');
    await expect(overlay).toBeVisible();

    // Click overlay to close menu (force click since menu items might intercept)
    await overlay.click({ force: true });

    // Wait for CSS transition to complete (0.3s ease)
    await page.waitForTimeout(400);

    // Verify hamburger button aria-expanded is false
    await expect(hamburgerButton).toHaveAttribute('aria-expanded', 'false');

    // Verify overlay is no longer visible (removed from DOM when menu closed)
    await expect(overlay).not.toBeVisible();
  });

  test('Mobile menu - authenticated user sees dashboard and logout', async ({ page }) => {
    // Set mobile viewport
    await page.setViewportSize({ width: 375, height: 667 });

    // Login as member
    await AuthHelpers.loginAs(page, 'member');

    // Navigate to homepage
    await page.goto('/');
    await page.waitForLoadState('domcontentloaded');

    // Open mobile menu
    const hamburgerButton = page.getByTestId('button-mobile-menu');
    await hamburgerButton.click();

    // Wait for mobile menu to open
    const mobileMenu = page.locator('#mobile-menu');
    await expect(mobileMenu).toBeVisible();

    // Verify Dashboard button is visible (NOT Login)
    await expect(page.getByTestId('mobile-link-dashboard')).toBeVisible();
    await expect(page.getByTestId('mobile-link-dashboard')).toContainText('Dashboard');

    // Verify Login button is NOT visible
    await expect(page.getByTestId('mobile-link-login')).not.toBeVisible();

    // Verify Logout button is visible
    await expect(page.getByTestId('mobile-button-logout')).toBeVisible();
    await expect(page.getByTestId('mobile-button-logout')).toContainText('Logout');

    // Test navigation via mobile menu
    await page.getByTestId('mobile-link-events').click();

    // Wait for CSS transition to complete and navigation
    await page.waitForURL('**/events');
    await page.waitForLoadState('domcontentloaded');

    // Verify navigation worked
    await expect(page.url()).toContain('/events');

    // Verify hamburger button aria-expanded is false (menu closed after navigation)
    await expect(hamburgerButton).toHaveAttribute('aria-expanded', 'false');
  });

  test('Mobile menu - admin user sees admin link', async ({ page }) => {
    // Set mobile viewport
    await page.setViewportSize({ width: 375, height: 667 });

    // Login as admin
    await AuthHelpers.loginAs(page, 'admin');

    // Navigate to homepage
    await page.goto('/');
    await page.waitForLoadState('domcontentloaded');

    // Open mobile menu
    const hamburgerButton = page.getByTestId('button-mobile-menu');
    await hamburgerButton.click();

    // Wait for mobile menu to open
    const mobileMenu = page.locator('#mobile-menu');
    await expect(mobileMenu).toBeVisible();

    // Verify Admin link is visible
    await expect(page.getByTestId('mobile-link-admin')).toBeVisible();
    await expect(page.getByTestId('mobile-link-admin')).toContainText('Admin');

    // Verify Dashboard link is visible
    await expect(page.getByTestId('mobile-link-dashboard')).toBeVisible();

    // Verify Logout button is visible
    await expect(page.getByTestId('mobile-button-logout')).toBeVisible();

    // Test admin navigation via mobile menu
    await page.getByTestId('mobile-link-admin').click();

    // Wait for CSS transition to complete and navigation
    await page.waitForURL('**/admin');
    await page.waitForLoadState('domcontentloaded');

    // Verify navigation worked
    await expect(page.url()).toContain('/admin');

    // Verify hamburger button aria-expanded is false (menu closed after navigation)
    await expect(hamburgerButton).toHaveAttribute('aria-expanded', 'false');
  });

  test('Logo navigation - returns to homepage from any page', async ({ page }) => {
    // Login as member
    await AuthHelpers.loginAs(page, 'member');

    // Navigate to dashboard
    await page.goto('/dashboard');
    await page.waitForLoadState('domcontentloaded');

    // Click logo
    const logo = page.locator('.logo');
    await logo.click();

    // Verify navigation to homepage
    await page.waitForURL('/');
    await page.waitForLoadState('domcontentloaded');
    await expect(page.url()).toMatch(/\/$|\/index/);
  });

  test('Navigation persistence - items remain accessible after page navigation', async ({ page }) => {
    // Login as admin
    await AuthHelpers.loginAs(page, 'admin');

    // Start at homepage
    await page.goto('/');
    await page.waitForLoadState('domcontentloaded');

    // Verify Admin link is visible
    await expect(page.getByTestId('link-admin')).toBeVisible();

    // Navigate to Events
    await page.getByTestId('link-events').click();
    await page.waitForURL('**/events');
    await page.waitForLoadState('domcontentloaded');

    // Verify Admin link is STILL visible after navigation
    await expect(page.getByTestId('link-admin')).toBeVisible();

    // Verify Dashboard link is STILL visible
    await expect(page.getByTestId('link-dashboard')).toBeVisible();

    // Navigate to Dashboard
    await page.getByTestId('link-dashboard').click();
    await page.waitForURL('**/dashboard');
    await page.waitForLoadState('domcontentloaded');

    // Verify Admin link is STILL visible
    await expect(page.getByTestId('link-admin')).toBeVisible();

    // Navigate to Admin
    await page.getByTestId('link-admin').click();
    await page.waitForURL('**/admin');
    await page.waitForLoadState('domcontentloaded');

    // Verify Events link is STILL visible
    await expect(page.getByTestId('link-events')).toBeVisible();
  });

  test('Mobile menu logout - logs out user and closes menu', async ({ page }) => {
    // Set mobile viewport
    await page.setViewportSize({ width: 375, height: 667 });

    // Login as member
    await AuthHelpers.loginAs(page, 'member');

    // Navigate to homepage
    await page.goto('/');
    await page.waitForLoadState('domcontentloaded');

    // Open mobile menu
    const hamburgerButton = page.getByTestId('button-mobile-menu');
    await hamburgerButton.click();

    // Wait for mobile menu to open
    const mobileMenu = page.locator('#mobile-menu');
    await expect(mobileMenu).toBeVisible();

    // Click logout button
    await page.getByTestId('mobile-button-logout').click();

    // Wait for logout to complete and redirect to homepage (NOT login)
    // Logout mutation navigates to '/' (see mutations.ts line 230)
    await page.waitForURL('/', { timeout: 10000 });
    await page.waitForLoadState('domcontentloaded');

    // Verify we're on homepage
    const url = page.url();
    expect(url === `${WEB_BASE_URL}/` || url.endsWith('/')).toBeTruthy();

    // Verify hamburger button aria-expanded is false (menu closed after logout)
    await expect(hamburgerButton).toHaveAttribute('aria-expanded', 'false');

    // Verify we're logged out - Login button should exist in desktop nav
    // On mobile, the desktop nav is hidden via CSS, so we check for existence in DOM
    // The mobile login button would be in the hamburger menu (not tested here)
    const loginButton = page.locator('.nav a[href="/login"], a.btn[href="/login"]');
    await expect(loginButton.first()).toBeAttached({ timeout: 5000 });
  });
});
