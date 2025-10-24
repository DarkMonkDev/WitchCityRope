import { test, expect } from '@playwright/test';
import { AuthHelpers } from './helpers/auth.helpers';

test.describe('Public Event Pages Comparison', () => {
  test('capture dashboard events page', async ({ page }) => {
    // Login as member before accessing dashboard
    await AuthHelpers.loginAs(page, 'member');

    // Navigate to the dashboard events page (currently requires auth)
    await page.goto('http://localhost:5173/dashboard/events');

    // Wait for the page to load
    await page.waitForLoadState('networkidle');

    // Take a screenshot
    await page.screenshot({
      path: 'test-results/dashboard-events-page.png',
      fullPage: true
    });
  });

  // TODO: Skip - wireframe files don't exist in test environment
  // These tests are for design validation, not functional testing
  test.skip('capture public events wireframe', async ({ page }) => {
    // Navigate to the public events list wireframe
    await page.goto('http://localhost:8080/docs/functional-areas/events/public-events/event-list.html');

    // Wait for the page to load
    await page.waitForLoadState('networkidle');

    // Take a screenshot
    await page.screenshot({
      path: 'test-results/public-events-wireframe.png',
      fullPage: true
    });
  });

  // TODO: Skip - wireframe files don't exist in test environment
  // These tests are for design validation, not functional testing
  test.skip('capture event detail wireframe', async ({ page }) => {
    // Navigate to the event detail wireframe
    await page.goto('http://localhost:8080/docs/functional-areas/events/public-events/event-detail.html');

    // Wait for the page to load
    await page.waitForLoadState('networkidle');

    // Take a screenshot
    await page.screenshot({
      path: 'test-results/event-detail-wireframe.png',
      fullPage: true
    });
  });
});
