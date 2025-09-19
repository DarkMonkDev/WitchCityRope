import { test, expect } from '@playwright/test';

/**
 * Quick validation test for Admin Event Editing comprehensive tests
 *
 * This is a simplified version to verify the test infrastructure works
 * before running the full comprehensive test suite.
 */

test.describe('Admin Event Editing - Quick Validation', () => {

  test('should load admin login page and navigate to events', async ({ page }) => {
    console.log('🧪 Starting quick admin event editing validation...');

    // Navigate to login page
    await page.goto('/login');
    await page.waitForLoadState('networkidle');

    // Verify login page loaded correctly
    await expect(page.locator('[data-testid="page-login"]')).toBeVisible();
    await expect(page.locator('h1')).toContainText('Welcome Back');

    // Login as admin
    await page.fill('[data-testid="email-input"]', 'admin@witchcityrope.com');
    await page.fill('[data-testid="password-input"]', 'Test123!');
    await page.click('[data-testid="login-button"]');

    // Wait for navigation to dashboard
    await page.waitForURL('**/dashboard', { timeout: 15000 });
    await page.waitForLoadState('networkidle');

    console.log('✅ Successfully logged in as admin');

    // Navigate to admin events page
    await page.goto('/admin/events');
    await page.waitForLoadState('networkidle');

    // Verify we're on admin events page
    await expect(page.locator('h1')).toContainText('Events Dashboard');
    console.log('✅ Successfully navigated to admin events dashboard');

    // Check if there are events available
    const eventRows = page.locator('[data-testid="event-row"]');
    const rowCount = await eventRows.count();
    console.log(`🔍 Found ${rowCount} events in the dashboard`);

    if (rowCount > 0) {
      // Click on first event to test navigation to details
      await eventRows.first().click();
      await page.waitForLoadState('networkidle');

      // Verify we're on event details page
      const eventDetailsPage = page.locator('[data-testid="page-admin-event-details"]');
      await expect(eventDetailsPage).toBeVisible();
      console.log('✅ Successfully navigated to event details page');

      // Check if basic tabs are present
      const basicInfoTab = page.locator('[data-testid="tab-basic-info"]');
      const setupTab = page.locator('[data-testid="setup-tab"]');

      if (await basicInfoTab.count() > 0) {
        console.log('✅ Basic Info tab found');
      }

      if (await setupTab.count() > 0) {
        console.log('✅ Setup tab found');
      }
    } else {
      console.log('⚠️ No events found in dashboard - cannot test event editing workflow');
    }

    console.log('🎉 Quick validation test completed successfully');
  });
});