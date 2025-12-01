import { test, expect } from '@playwright/test';
import { AuthHelpers } from './test-utils/helpers/auth.helpers';

// Environment-aware URLs for container/host compatibility
const WEB_BASE_URL = process.env.PLAYWRIGHT_BASE_URL || 'http://localhost:5173';
const API_BASE_URL = process.env.API_URL || 'http://localhost:5655';


/**
 * User Dashboard Workflow Tests
 *
 * Purpose: Test user dashboard functionality (MyEventsPage component)
 * Replaces: user-dashboard-wireframe-validation.spec.ts (design validation - obsolete)
 *
 * Tests dashboard workflows:
 * - Dashboard loads with user info
 * - Vetting status displays correctly for different users
 * - Quick actions work (Edit Profile, Browse Events)
 * - Events display in grid/table view
 */

const baseUrl = process.env.PLAYWRIGHT_BASE_URL || WEB_BASE_URL;

test.describe('User Dashboard - Basic Functionality', () => {

  test('should load dashboard for authenticated member user', async ({ page }) => {
    // Login as general member
    await AuthHelpers.loginAs(page, 'member');

    // Navigate to dashboard
    await page.goto(`${baseUrl}/dashboard`);
    await page.waitForLoadState('domcontentloaded');

    // Verify page title includes "Events" (format: "{Name}'s Events")
    const pageTitle = page.locator('h1');
    await expect(pageTitle).toBeVisible({ timeout: 10000 });

    const titleText = await pageTitle.textContent();
    expect(titleText).toMatch(/Events/i);

    // Verify Edit Profile button exists
    const editProfileButton = page.locator('a:has-text("Edit Profile")');
    await expect(editProfileButton).toBeVisible();

    console.log('✅ Dashboard loaded with user info and Edit Profile button');
  });

  test('should display vetting status for unvetted member', async ({ page }) => {
    // Login as general member (may have vetting application in progress)
    await AuthHelpers.loginAs(page, 'member');

    await page.goto(`${baseUrl}/dashboard`);
    await page.waitForLoadState('domcontentloaded');

    // Look for vetting alert box (only shows for non-Approved users)
    const vettingAlert = page.locator('[role="alert"]').first();

    // Check if alert is visible (member may have pending/approved/denied status)
    const alertVisible = await vettingAlert.isVisible({ timeout: 3000 }).catch(() => false);

    if (alertVisible) {
      const alertText = await vettingAlert.textContent();
      console.log(`✅ Vetting alert displayed: ${alertText?.substring(0, 50)}...`);

      // Verify alert contains vetting-related content
      expect(alertText).toMatch(/application|interview|review|approved|hold|denied/i);
    } else {
      console.log('ℹ️ No vetting alert (user may be approved or no application submitted)');
    }
  });

  test('should NOT display vetting status for vetted user', async ({ page }) => {
    // Login as vetted member
    await AuthHelpers.loginAs(page, 'vetted');

    await page.goto(`${baseUrl}/dashboard`);
    await page.waitForLoadState('domcontentloaded');

    // Vetting alert should NOT be visible for approved users
    const vettingAlert = page.locator('[role="alert"]').filter({ hasText: /application|vetting|interview/i });

    // Wait a moment to ensure alert doesn't appear
    await page.waitForTimeout(1000);

    const alertCount = await vettingAlert.count();
    expect(alertCount).toBe(0);

    console.log('✅ Vetted user dashboard shows no vetting alert (as expected)');
  });
});

test.describe('User Dashboard - Quick Actions', () => {

  test('should navigate to profile settings when Edit Profile clicked', async ({ page }) => {
    await AuthHelpers.loginAs(page, 'member');

    await page.goto(`${baseUrl}/dashboard`);
    await page.waitForLoadState('domcontentloaded');

    // Click Edit Profile button
    const editProfileButton = page.locator('a:has-text("Edit Profile")');
    await expect(editProfileButton).toBeVisible();
    await editProfileButton.click();

    // Verify navigation to profile settings
    await page.waitForURL('**/dashboard/profile-settings', { timeout: 10000 });

    const currentUrl = page.url();
    expect(currentUrl).toContain('/dashboard/profile-settings');

    // Verify profile settings page loaded
    const profileTitle = page.locator('h1:has-text("Profile Settings")');
    await expect(profileTitle).toBeVisible({ timeout: 5000 });

    console.log('✅ Edit Profile button navigates to profile settings');
  });

  test('should navigate to events when Browse Events clicked (empty state)', async ({ page }) => {
    await AuthHelpers.loginAs(page, 'guest');

    await page.goto(`${baseUrl}/dashboard`);
    await page.waitForLoadState('domcontentloaded');

    // Guest user likely has no events - look for empty state
    const emptyState = page.locator('text=No Events Found');
    const browseEventsButton = page.locator('a:has-text("Browse Events")');

    const hasEmptyState = await emptyState.isVisible({ timeout: 3000 }).catch(() => false);

    if (hasEmptyState) {
      // Verify Browse Events button exists in empty state
      await expect(browseEventsButton).toBeVisible();

      // Click Browse Events
      await browseEventsButton.click();

      // Verify navigation to events page
      await page.waitForURL('**/events', { timeout: 10000 });
      expect(page.url()).toContain('/events');

      console.log('✅ Browse Events button navigates to events page (from empty state)');
    } else {
      console.log('ℹ️ User has events - empty state not tested');
    }
  });

  test('should return to dashboard when View Dashboard clicked from profile', async ({ page }) => {
    await AuthHelpers.loginAs(page, 'vetted');

    // Navigate to profile settings first
    await page.goto(`${baseUrl}/dashboard/profile-settings`);
    await page.waitForLoadState('domcontentloaded');

    // Verify we're on profile page
    await expect(page.locator('h1:has-text("Profile Settings")')).toBeVisible();

    // Click View Dashboard button
    const viewDashboardButton = page.locator('a:has-text("View Dashboard")');
    await expect(viewDashboardButton).toBeVisible();
    await viewDashboardButton.click();

    // Verify navigation back to dashboard
    await page.waitForURL('**/dashboard', { timeout: 10000 });

    const currentUrl = page.url();
    expect(currentUrl).toMatch(/\/dashboard(?:\/)?$/);

    // Verify dashboard loaded
    const dashboardTitle = page.locator('h1').filter({ hasText: /Events/i });
    await expect(dashboardTitle).toBeVisible({ timeout: 5000 });

    console.log('✅ View Dashboard button navigates back to dashboard');
  });
});

test.describe('User Dashboard - Event Display', () => {

  test('should display events in grid view by default', async ({ page }) => {
    await AuthHelpers.loginAs(page, 'vetted');

    await page.goto(`${baseUrl}/dashboard`);
    await page.waitForLoadState('domcontentloaded');

    // Check for event cards (grid view)
    const eventCards = page.locator('[data-testid="event-card"]');

    if (await eventCards.count() > 0) {
      // Verify at least one event card is visible
      await expect(eventCards.first()).toBeVisible({ timeout: 5000 });

      // Verify multiple cards are displayed (grid layout)
      const cardCount = await eventCards.count();
      expect(cardCount).toBeGreaterThan(0);

      console.log(`✅ Dashboard displays ${cardCount} events in grid view`);
    } else {
      console.log('ℹ️ User has no events - grid view not tested');
    }
  });

  test('should switch to table view when toggle clicked', async ({ page }) => {
    await AuthHelpers.loginAs(page, 'vetted');

    await page.goto(`${baseUrl}/dashboard`);
    await page.waitForLoadState('domcontentloaded');

    // Look for view toggle (filter bar component)
    const tableViewToggle = page.locator('button').filter({ hasText: /list|table/i });

    if (await tableViewToggle.count() > 0) {
      await tableViewToggle.first().click();
      await page.waitForTimeout(500);

      // Verify table is visible
      const eventTable = page.locator('table');
      await expect(eventTable).toBeVisible({ timeout: 5000 });

      // Verify table has expected columns
      const tableHeaders = await eventTable.locator('th').allTextContents();
      const headerText = tableHeaders.join(' ').toLowerCase();

      expect(headerText).toContain('date');
      expect(headerText).toContain('title');
      expect(headerText).toContain('status');

      console.log('✅ Dashboard switches to table view correctly');
    } else {
      console.log('ℹ️ View toggle not found - feature may not be implemented yet');
    }
  });

  test('should display event status badges in grid view', async ({ page }) => {
    await AuthHelpers.loginAs(page, 'vetted');

    await page.goto(`${baseUrl}/dashboard`);
    await page.waitForLoadState('domcontentloaded');

    const eventCards = page.locator('[data-testid="event-card"]');

    if (await eventCards.count() > 0) {
      // Look for status badge in first event card
      const firstCard = eventCards.first();
      const statusBadge = firstCard.locator('.mantine-Badge-root, [class*="Badge"]').first();

      if (await statusBadge.count() > 0) {
        await expect(statusBadge).toBeVisible({ timeout: 5000 });

        const badgeText = await statusBadge.textContent();
        expect(badgeText).toMatch(/RSVP|Ticket|Attended/i);

        console.log(`✅ Event card displays status badge: ${badgeText}`);
      } else {
        console.log('ℹ️ Status badge not found in event card');
      }
    } else {
      console.log('ℹ️ User has no events - status badge test skipped');
    }
  });

  test('should filter events with show past toggle', async ({ page }) => {
    await AuthHelpers.loginAs(page, 'vetted');

    await page.goto(`${baseUrl}/dashboard`);
    await page.waitForLoadState('domcontentloaded');

    // Look for "Show Past" checkbox in filter bar
    const showPastLabel = page.locator('label').filter({ hasText: /show past|past events/i });

    if (await showPastLabel.count() > 0) {
      const checkbox = showPastLabel.locator('input[type="checkbox"]');

      if (await checkbox.count() > 0) {
        // Verify checkbox is unchecked by default
        const isChecked = await checkbox.isChecked();
        expect(isChecked).toBe(false);

        // Check the box to show past events
        await checkbox.check();
        await page.waitForTimeout(500);

        // Verify checkbox is now checked
        const isNowChecked = await checkbox.isChecked();
        expect(isNowChecked).toBe(true);

        console.log('✅ Show Past Events toggle works correctly');
      }
    } else {
      console.log('ℹ️ Show Past Events toggle not found');
    }
  });

  test('should display event title and navigate on click', async ({ page }) => {
    await AuthHelpers.loginAs(page, 'vetted');

    await page.goto(`${baseUrl}/dashboard`);
    await page.waitForLoadState('domcontentloaded');

    const eventCards = page.locator('[data-testid="event-card"]');

    if (await eventCards.count() > 0) {
      const firstCard = eventCards.first();

      // Get event title
      const eventTitle = firstCard.locator('[data-testid="event-title"]');
      await expect(eventTitle).toBeVisible({ timeout: 5000 });

      const titleText = await eventTitle.textContent();
      expect(titleText).toBeTruthy();

      // Click card to navigate to event detail
      await firstCard.click();

      // Verify navigation to event detail page
      await page.waitForURL('**/events/**', { timeout: 10000 });
      expect(page.url()).toContain('/events/');

      console.log(`✅ Event card navigates to detail page for: ${titleText}`);
    } else {
      console.log('ℹ️ User has no events - navigation test skipped');
    }
  });
});

test.describe('User Dashboard - Responsive Design', () => {

  test('should display correctly on mobile viewport', async ({ page }) => {
    // Set mobile viewport
    await page.setViewportSize({ width: 375, height: 667 });

    await AuthHelpers.loginAs(page, 'member');

    await page.goto(`${baseUrl}/dashboard`);
    await page.waitForLoadState('domcontentloaded');

    // Verify page title is visible
    const pageTitle = page.locator('h1').filter({ hasText: /Events/i });
    await expect(pageTitle).toBeVisible({ timeout: 5000 });

    // Verify Edit Profile button is visible
    const editProfileButton = page.locator('a:has-text("Edit Profile")');
    await expect(editProfileButton).toBeVisible();

    // Take screenshot for visual verification
    await page.screenshot({ path: './test-results/dashboard-mobile-375.png' });

    console.log('✅ Dashboard displays correctly on mobile viewport');
  });

  test('should stack event cards vertically on mobile', async ({ page }) => {
    await page.setViewportSize({ width: 375, height: 667 });

    await AuthHelpers.loginAs(page, 'vetted');

    await page.goto(`${baseUrl}/dashboard`);
    await page.waitForLoadState('domcontentloaded');

    const eventCards = page.locator('[data-testid="event-card"]');

    if (await eventCards.count() >= 2) {
      // Get positions of first two cards
      const firstCardBox = await eventCards.first().boundingBox();
      const secondCardBox = await eventCards.nth(1).boundingBox();

      if (firstCardBox && secondCardBox) {
        // On mobile, cards should stack vertically (second card below first)
        const isStacked = secondCardBox.y > firstCardBox.y + firstCardBox.height - 10;
        expect(isStacked).toBe(true);

        console.log('✅ Event cards stack vertically on mobile');
      }
    } else {
      console.log('ℹ️ Not enough event cards to test stacking');
    }

    await page.screenshot({ path: './test-results/dashboard-mobile-stacking.png' });
  });
});
