import { test, expect } from '@playwright/test';
import { AuthHelpers } from './helpers/auth.helpers';

/**
 * Debug test for vetting navigation issue
 * Tests the actual navigation behavior when clicking a row
 */
test.describe('Vetting Navigation Debug', () => {
  test('should navigate to detail page when clicking row', async ({ page }) => {
    // Enable console logging
    page.on('console', msg => console.log('BROWSER:', msg.text()));

    // Arrange - Login as admin
    await AuthHelpers.loginAs(page, 'admin');

    // Navigate to vetting page
    await page.goto('/admin/vetting');
    await page.waitForLoadState('networkidle');

    // Take screenshot of list page
    await page.screenshot({ path: 'test-results/vetting-list-before-click.png', fullPage: true });

    // Get the current URL before click
    const urlBefore = page.url();
    console.log('URL before click:', urlBefore);

    // Wait for the table to load
    await page.waitForSelector('table tbody tr', { timeout: 10000 });

    // Get the first row
    const firstRow = page.locator('table tbody tr').first();
    await expect(firstRow).toBeVisible();

    // Click the row
    console.log('Clicking first row...');
    await firstRow.click();

    // Wait for URL to change - matches the actual route /admin/vetting/applications/:applicationId
    await page.waitForURL(/\/admin\/vetting\/applications\/[a-f0-9-]+$/, { timeout: 5000 });
    console.log('URL changed, waiting for page to render...');

    // Wait for network to be idle after navigation
    await page.waitForLoadState('networkidle');

    // Check the URL after click
    const urlAfter = page.url();
    console.log('URL after click:', urlAfter);

    // Take screenshot after click
    await page.screenshot({ path: 'test-results/vetting-after-click.png', fullPage: true });

    // Check if URL changed to detail page path
    expect(urlAfter).not.toBe(urlBefore);
    expect(urlAfter).toMatch(/\/admin\/vetting\/applications\/[a-f0-9-]+$/); // Should be /admin/vetting/applications/:applicationId

    // Check if the page actually changed
    // The detail page should NOT have the table anymore
    const tableStillExists = await page.locator('table').count();
    console.log('Table count after navigation:', tableStillExists);

    // The detail page should have different content
    const detailPageIndicators = [
      page.locator('text=/application.*detail/i'),
      page.locator('text=/back.*applications/i'),
      page.locator('[data-testid="application-detail"]')
    ];

    let foundDetailIndicator = false;
    for (const indicator of detailPageIndicators) {
      const count = await indicator.count();
      if (count > 0) {
        foundDetailIndicator = true;
        console.log('Found detail page indicator:', await indicator.first().textContent());
        break;
      }
    }

    // Wait a bit more and take another screenshot
    await page.waitForTimeout(2000);
    await page.screenshot({ path: 'test-results/vetting-after-wait.png', fullPage: true });

    console.log('Detail page loaded:', foundDetailIndicator);
    console.log('Table still exists:', tableStillExists > 0);

    // EXPECTATION: Detail page should be loaded (no table, detail content visible)
    // If this fails, it confirms the bug
    expect(foundDetailIndicator).toBe(true);
    expect(tableStillExists).toBe(0);
  });
});
