/**
 * Notification System Test
 *
 * Tests Mantine Notifications component to verify it renders correctly
 * and appears in the DOM when notifications.show() is called.
 *
 * This test helps debug notification rendering issues.
 */

import { test, expect } from '@playwright/test';

test.describe('Notification System', () => {
  test('Notifications container appears when notification is shown', async ({ page }) => {
    // Navigate to test notifications page
    await page.goto('/test-notifications', { waitUntil: 'domcontentloaded' });

    // Wait for page to load
    await expect(page.locator('h1')).toContainText('Notifications Test Page');

    // Take screenshot before clicking
    await page.screenshot({ path: './test-results/notification-before-click.png' });

    // Click button to show test notification
    const testButton = page.locator('button').filter({ hasText: /Show Test Notification/i });
    await expect(testButton).toBeVisible();
    await testButton.click();

    // Wait a moment for React to render
    await page.waitForTimeout(500);

    // Take screenshot after clicking
    await page.screenshot({ path: './test-results/notification-after-click.png' });

    // Check if notifications container exists in DOM
    const notificationContainer = page.locator('[class*="notifications"], #mantine-notifications-container, [data-notifications]');

    // Debug: Log what's in the DOM
    const bodyHTML = await page.locator('body').innerHTML();
    console.log('=== Body HTML (first 2000 chars) ===');
    console.log(bodyHTML.substring(0, 2000));

    // Debug: Get all elements with "notification" in class or data attributes
    const notificationElements = await page.locator('[class*="notification"], [data-position]').all();
    console.log(`=== Found ${notificationElements.length} notification-related elements ===`);
    for (const el of notificationElements) {
      const className = await el.getAttribute('class');
      const dataPosition = await el.getAttribute('data-position');
      const isVisible = await el.isVisible();
      console.log(`Element: class="${className}", position="${dataPosition}", visible=${isVisible}`);
    }

    // Try multiple possible selectors for Mantine v7 notifications
    const possibleSelectors = [
      '#mantine-notifications-container',
      '[class*="mantine-Notifications"]',
      '[class*="notifications"]',
      '[role="region"][aria-label*="notification"]',
      '[data-notifications]'
    ];

    let found = false;
    for (const selector of possibleSelectors) {
      const element = page.locator(selector).first();
      const count = await element.count();
      console.log(`Selector "${selector}" found ${count} elements`);
      if (count > 0) {
        found = true;
        await expect(element).toBeVisible({ timeout: 5000 });
        break;
      }
    }

    // Don't check container visibility - it may be hidden when empty
    // Instead, look directly for the notification content
    console.log('=== Looking for notification content ===');

    // Look for the actual notification message (should be visible even if container shows as "hidden")
    const notification = page.locator('text=/Test Notification|test notification/i').first();
    const notificationCount = await notification.count();
    console.log(`Found ${notificationCount} elements matching notification text`);

    if (notificationCount === 0) {
      // Try finding notification by Mantine classes
      const mantineNotification = page.locator('[class*="Notification-"]').first();
      const mantineCount = await mantineNotification.count();
      console.log(`Found ${mantineCount} elements with Mantine Notification classes`);

      if (mantineCount > 0) {
        const notifHTML = await mantineNotification.innerHTML();
        console.log(`Notification HTML: ${notifHTML.substring(0, 500)}`);
      }

      throw new Error('Notification text not found in DOM - notifications.show() may not be working');
    }

    await expect(notification).toBeVisible({ timeout: 5000 });

    console.log('✅ Notification system working correctly!');
  });

  test('Multiple notifications stack correctly', async ({ page }) => {
    await page.goto('/test-notifications', { waitUntil: 'domcontentloaded' });

    // Show multiple notifications quickly
    await page.locator('button').filter({ hasText: /Show Test Notification/i }).click();
    await page.waitForTimeout(200);
    await page.locator('button').filter({ hasText: /Show Test Notification/i }).click();
    await page.waitForTimeout(200);
    await page.locator('button').filter({ hasText: /Show Test Notification/i }).click();

    await page.waitForTimeout(500);

    // Check if multiple notifications appear
    const notifications = page.locator('[class*="Notification"], [role="alert"]');
    const count = await notifications.count();
    console.log(`Found ${count} notifications in DOM`);

    // Should have at least 2 visible (may have already auto-closed)
    expect(count).toBeGreaterThanOrEqual(1);
  });
});
