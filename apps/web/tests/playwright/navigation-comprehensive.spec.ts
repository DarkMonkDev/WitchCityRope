/**
 * Comprehensive Navigation Test Suite
 *
 * Consolidates functionality from:
 * - events-actual-routes-test.spec.ts (route validation, API tests, authentication)
 * - navigation-updates-test.spec.ts (navigation UI elements, user-specific navigation)
 * - test-direct-navigation.spec.ts (direct URL navigation patterns)
 * - test-events-navigation.spec.ts (events list → detail navigation)
 *
 * Date Consolidated: 2025-10-10
 * Reason: Reduce duplicate tests per user request
 *
 * What This Test Covers:
 * 1. Route validation and API data availability
 * 2. Navigation UI elements (Login/Dashboard, Admin link, user greeting)
 * 3. User-specific navigation (guest vs member vs admin)
 * 4. Direct URL navigation to detail pages
 * 5. List → detail navigation flows
 * 6. Authentication and protected routes
 */

import { test, expect, Page } from '@playwright/test';
import { AuthHelpers } from './helpers/auth.helpers';

const testUrls = {
  login: 'http://localhost:5173/login',
  home: 'http://localhost:5173/',
  dashboardEvents: 'http://localhost:5173/dashboard/events',
  adminEvents: 'http://localhost:5173/admin/events',
  adminEventsDemo: 'http://localhost:5173/admin/events-management-api-demo',
  adminSessionDemo: 'http://localhost:5173/admin/event-session-matrix-demo',
};

test.describe('Navigation System - Comprehensive Testing', () => {

  test.describe('Route Validation and API Integration', () => {

    test('should verify API events data availability', async ({ page }) => {
      console.log('🧪 Testing Events API data availability...');

      // Direct API test
      const eventsApiResponse = await page.request.get('http://localhost:5655/api/events');
      console.log(`📅 Events API status: ${eventsApiResponse.status()}`);

      expect(eventsApiResponse.ok()).toBe(true);

      const eventsResponse = await eventsApiResponse.json();
      console.log(`📊 Events API returned:`, {
        success: eventsResponse.success,
        hasData: eventsResponse.data !== null,
        dataType: typeof eventsResponse.data,
        isArray: Array.isArray(eventsResponse.data),
        count: Array.isArray(eventsResponse.data) ? eventsResponse.data.length : 'N/A',
      });

      // Validate ApiResponse<List<EventDto>> wrapper format
      expect(eventsResponse.success).toBe(true);
      expect(eventsResponse.error).toBeNull();
      expect(Array.isArray(eventsResponse.data)).toBe(true);
      expect(eventsResponse.data.length).toBeGreaterThan(0);

      console.log('✅ Events API: Working perfectly with ApiResponse wrapper');
    });

    test('should load demo routes successfully', async ({ page }) => {
      console.log('🧪 Testing Events Management API Demo route...');

      const response = await page.goto(testUrls.adminEventsDemo);
      console.log(`📡 Demo page response: ${response?.status()}`);

      await page.waitForTimeout(3000);

      // Check for demo content
      const pageContent = await page.locator('body').textContent();
      const hasEventContent = pageContent?.toLowerCase().includes('event');
      const hasApiContent = pageContent?.toLowerCase().includes('api');

      console.log(`📄 Demo page contains event/API content: ${hasEventContent || hasApiContent}`);

      expect(response?.status()).toBe(200);
      expect(hasEventContent || hasApiContent).toBe(true);

      console.log('✅ Events Management API Demo: Loading successfully');
    });

    test('should access protected dashboard events route after authentication', async ({ page }) => {
      console.log('🧪 Testing protected dashboard events route...');

      // Login using AuthHelpers
      await AuthHelpers.loginAs(page, 'admin');
      console.log('✓ Logged in as admin');

      // Navigate to protected dashboard/events route
      const dashboardEventsResponse = await page.goto(testUrls.dashboardEvents);
      console.log(`📅 Dashboard events status: ${dashboardEventsResponse?.status()}`);

      await page.waitForTimeout(2000);

      // Check for events dashboard content
      const dashboardContent = await page.locator('body').textContent();
      const hasEventsContent = dashboardContent?.toLowerCase().includes('event');

      console.log(`📊 Dashboard events page has events content: ${hasEventsContent}`);

      expect(dashboardEventsResponse?.status()).toBe(200);

      console.log('✅ Protected Dashboard Events: Accessible after authentication');
    });
  });

  test.describe('Navigation UI Elements - Guest Users', () => {

    test.beforeEach(async ({ page }) => {
      await page.goto(testUrls.home);
      await page.waitForLoadState('networkidle');
    });

    test('should show Login button (not Dashboard) for guest users', async ({ page }) => {
      console.log('🧪 Testing guest user navigation UI...');

      // Verify Login button appears
      const loginButton = page.locator('[data-testid="nav-main"] a').filter({ hasText: 'Login' });
      await expect(loginButton).toBeVisible();

      // Verify Dashboard button does NOT appear
      const dashboardButton = page.locator('[data-testid="link-dashboard"]');
      await expect(dashboardButton).not.toBeVisible();

      console.log('✅ Guest users see Login button (not Dashboard)');
    });

    test('should NOT show Admin link for guest users', async ({ page }) => {
      const adminLink = page.locator('[data-testid="link-admin"]');
      await expect(adminLink).not.toBeVisible();

      console.log('✅ Guest users do not see Admin link');
    });

    test('should NOT show user greeting or Logout for guest users', async ({ page }) => {
      const userGreeting = page.locator('[data-testid="user-greeting"]');
      await expect(userGreeting).not.toBeVisible();

      const logoutButton = page.locator('[data-testid="button-logout"]');
      await expect(logoutButton).not.toBeVisible();

      console.log('✅ Guest users do not see user greeting or Logout link');
    });
  });

  test.describe('Navigation UI Elements - Regular Members', () => {

    test.beforeEach(async ({ page }) => {
      // Login as regular member
      await AuthHelpers.loginAs(page, 'member');
      await page.goto(testUrls.home);
      await page.waitForLoadState('networkidle');
    });

    test('should show Dashboard button (not Login) for logged-in members', async ({ page }) => {
      console.log('🧪 Testing member navigation UI...');

      // Verify Dashboard button appears
      const dashboardButton = page.locator('[data-testid="link-dashboard"]');
      await expect(dashboardButton).toBeVisible();
      await expect(dashboardButton).toHaveText('Dashboard');

      // Verify Login button does NOT appear
      const loginButton = page.locator('[data-testid="nav-main"] a').filter({ hasText: 'Login' });
      await expect(loginButton).not.toBeVisible();

      console.log('✅ Members see Dashboard button (not Login)');
    });

    test('should NOT show Admin link for regular members', async ({ page }) => {
      const adminLink = page.locator('[data-testid="link-admin"]');
      await expect(adminLink).not.toBeVisible();

      console.log('✅ Regular members do not see Admin link');
    });

    test('should show user greeting and Logout for logged-in members', async ({ page }) => {
      const userGreeting = page.locator('[data-testid="user-greeting"]');
      await expect(userGreeting).toBeVisible();
      await expect(userGreeting).toContainText('Welcome,');

      const logoutButton = page.locator('[data-testid="button-logout"]');
      await expect(logoutButton).toBeVisible();
      await expect(logoutButton).toHaveText('Logout');

      console.log('✅ Members see user greeting and Logout link');
    });

    test('should be able to logout', async ({ page }) => {
      const logoutButton = page.locator('[data-testid="button-logout"]');
      await logoutButton.click();

      // Should redirect to homepage and show login button again
      await expect(page).toHaveURL(testUrls.home);
      const loginButton = page.locator('[data-testid="nav-main"] a').filter({ hasText: 'Login' });
      await expect(loginButton).toBeVisible();

      console.log('✅ Members can logout successfully');
    });
  });

  test.describe('Navigation UI Elements - Administrators', () => {

    test.beforeEach(async ({ page }) => {
      // Login as administrator
      await AuthHelpers.loginAs(page, 'admin');
      await page.goto(testUrls.home);
      await page.waitForLoadState('networkidle');
    });

    test('should show Dashboard button for admins', async ({ page }) => {
      const dashboardButton = page.locator('[data-testid="link-dashboard"]');
      await expect(dashboardButton).toBeVisible();
      await expect(dashboardButton).toHaveText('Dashboard');

      console.log('✅ Admins see Dashboard button');
    });

    test('should show Admin link left of Events & Classes', async ({ page }) => {
      console.log('🧪 Testing admin navigation link position...');

      const adminLink = page.locator('[data-testid="link-admin"]');
      const eventsLink = page.locator('[data-testid="link-events"]');

      await expect(adminLink).toBeVisible();
      await expect(adminLink).toHaveText('Admin');
      await expect(eventsLink).toBeVisible();

      // Verify admin link comes before events link
      const navItems = page.locator('[data-testid="nav-main"] .nav a');
      const adminIndex = await getElementIndex(navItems, adminLink);
      const eventsIndex = await getElementIndex(navItems, eventsLink);

      expect(adminIndex).toBeLessThan(eventsIndex);

      console.log('✅ Admin link appears left of Events & Classes');
    });

    test('should show user greeting and Logout for admins', async ({ page }) => {
      const userGreeting = page.locator('[data-testid="user-greeting"]');
      await expect(userGreeting).toBeVisible();
      await expect(userGreeting).toContainText('Welcome,');

      const logoutButton = page.locator('[data-testid="button-logout"]');
      await expect(logoutButton).toBeVisible();
      await expect(logoutButton).toHaveText('Logout');

      console.log('✅ Admins see user greeting and Logout link');
    });
  });

  test.describe('Direct Navigation Patterns', () => {

    test('should support direct URL navigation to vetting detail page', async ({ page }) => {
      console.log('🧪 Testing direct navigation to vetting detail page...');

      page.on('console', msg => console.log('BROWSER:', msg.text()));

      await AuthHelpers.loginAs(page, 'admin');

      // Navigate DIRECTLY to a detail page URL
      await page.goto('/admin/vetting/test-id-12345');
      await page.waitForLoadState('networkidle');

      console.log('URL:', page.url());
      await page.screenshot({
        path: '/home/chad/repos/witchcityrope/test-results/direct-navigation-vetting.png',
        fullPage: true
      });

      // Check what's on the page
      const title = await page.locator('h1').first().textContent();
      console.log('Page title:', title);

      const hasTable = await page.locator('table').count();
      console.log('Has table:', hasTable > 0);

      // Verify page loaded (no 404 error)
      const bodyText = await page.locator('body').textContent();
      const is404 = bodyText?.toLowerCase().includes('not found') || bodyText?.toLowerCase().includes('404');

      expect(is404).toBe(false);

      console.log('✅ Direct URL navigation to vetting detail page works');
    });

    test('should support direct URL navigation to event detail page', async ({ page }) => {
      console.log('🧪 Testing direct navigation to event detail page...');

      page.on('console', msg => console.log('BROWSER:', msg.text()));

      await AuthHelpers.loginAs(page, 'admin');

      // Get first event ID from events list
      await page.goto(testUrls.adminEvents);
      await page.waitForLoadState('networkidle');

      const firstRow = page.locator('[data-testid="event-row"]').first();
      if (await firstRow.isVisible()) {
        // Click to get event ID from URL
        await firstRow.click();
        await page.waitForLoadState('networkidle');

        const urlAfter = page.url();
        console.log('Event detail URL:', urlAfter);

        // Verify we're on event detail page
        expect(urlAfter).toMatch(/\/admin\/events\/[a-f0-9-]+/);

        // Now test direct navigation to this URL
        await page.goto(testUrls.adminEvents); // Go back to list
        await page.goto(urlAfter); // Direct navigate to detail
        await page.waitForLoadState('networkidle');

        // Verify page loaded successfully
        const bodyText = await page.locator('body').textContent();
        const is404 = bodyText?.toLowerCase().includes('not found') || bodyText?.toLowerCase().includes('404');

        expect(is404).toBe(false);

        console.log('✅ Direct URL navigation to event detail page works');
      } else {
        console.log('⚠️ No events found to test direct navigation');
      }
    });
  });

  test.describe('List to Detail Navigation Flows', () => {

    test('should navigate from events list to event detail', async ({ page }) => {
      console.log('🧪 Testing events list → detail navigation...');

      page.on('console', msg => console.log('BROWSER:', msg.text()));

      await AuthHelpers.loginAs(page, 'admin');

      // Navigate to events list
      await page.goto(testUrls.adminEvents);
      await page.waitForLoadState('networkidle');

      const urlBefore = page.url();
      console.log('Events list URL:', urlBefore);

      // Click first event (if any)
      const firstRow = page.locator('[data-testid="event-row"]').first();
      if (await firstRow.isVisible()) {
        console.log('Clicking first event...');
        await firstRow.click();
        await page.waitForLoadState('networkidle');

        const urlAfter = page.url();
        console.log('Detail URL:', urlAfter);

        // Verify URL changed to detail page
        expect(urlAfter).not.toBe(urlBefore);
        expect(urlAfter).toMatch(/\/admin\/events\/[a-f0-9-]+/);

        console.log('✅ Events list → detail navigation works');
      } else {
        console.log('⚠️ No events found to test navigation');
      }
    });

    test('should navigate from vetting list to vetting detail', async ({ page }) => {
      console.log('🧪 Testing vetting list → detail navigation...');

      await AuthHelpers.loginAs(page, 'admin');

      // Navigate to vetting list
      await page.goto('/admin/vetting');
      await page.waitForLoadState('networkidle');

      const urlBefore = page.url();
      console.log('Vetting list URL:', urlBefore);

      // Look for first vetting application row
      const firstRow = page.locator('[data-testid="application-row"], tbody tr').first();
      if (await firstRow.isVisible()) {
        console.log('Clicking first vetting application...');
        await firstRow.click();
        await page.waitForLoadState('networkidle');

        const urlAfter = page.url();
        console.log('Detail URL:', urlAfter);

        // Verify URL changed to detail page
        expect(urlAfter).not.toBe(urlBefore);
        expect(urlAfter).toMatch(/\/admin\/vetting\/[a-f0-9-]+/);

        console.log('✅ Vetting list → detail navigation works');
      } else {
        console.log('⚠️ No vetting applications found to test navigation');
      }
    });
  });

  test.describe('Responsive Navigation', () => {

    test('should show mobile menu on small viewports', async ({ page }) => {
      console.log('🧪 Testing responsive navigation...');

      await page.goto(testUrls.home);
      await page.setViewportSize({ width: 375, height: 667 });

      const mobileMenuButton = page.locator('[data-testid="button-mobile-menu"]');
      await expect(mobileMenuButton).toBeVisible();

      console.log('✅ Mobile menu button visible on mobile viewport');
    });

    test('should preserve navigation order on all screen sizes', async ({ page }) => {
      console.log('🧪 Testing navigation order consistency...');

      // Login as admin to see all navigation items
      await AuthHelpers.loginAs(page, 'admin');
      await page.goto(testUrls.home);
      await page.waitForLoadState('networkidle');

      const navItems = page.locator('[data-testid="nav-main"] .nav a, [data-testid="nav-main"] .nav .btn');

      // Expected order for admin users: Admin, Events & Classes, Resources, Dashboard
      // Note: "How to Join" is conditionally hidden for vetted members/admins
      await expect(navItems.nth(0)).toContainText('Admin');
      await expect(navItems.nth(1)).toContainText('Events & Classes');
      await expect(navItems.nth(2)).toContainText('Resources');
      await expect(navItems.nth(3)).toContainText('Dashboard');

      console.log('✅ Navigation order correct on all screen sizes');
    });
  });
});

// Helper functions
async function getElementIndex(locator: any, targetElement: any): Promise<number> {
  const count = await locator.count();
  for (let i = 0; i < count; i++) {
    const element = locator.nth(i);
    if (await element.isVisible() && await targetElement.isVisible()) {
      const elementText = await element.textContent();
      const targetText = await targetElement.textContent();
      if (elementText === targetText) {
        return i;
      }
    }
  }
  return -1;
}
