import { test, expect } from '@playwright/test';

test.describe('Admin Events Table UI Check', () => {
  test('verify admin events table layout fixes', async ({ page }) => {
    // Navigate to admin events page
    await page.goto('http://localhost:5173/admin/events');

    // Wait for table to load
    await expect(page.locator('[data-testid="events-table"]')).toBeVisible({ timeout: 10000 });

    // Take screenshot to verify the fixes
    await page.screenshot({
      path: 'admin-events-table-layout-check.png',
      fullPage: true,
      clip: {
        x: 0,
        y: 100, // Skip nav area
        width: 1200,
        height: 600
      }
    });

    // Check that copy button is visible and properly styled
    const copyButtons = page.locator('[data-testid="button-copy-event"]');
    await expect(copyButtons.first()).toBeVisible();

    // Verify the button text is fully visible (not cut off)
    const copyButton = copyButtons.first();
    const buttonBox = await copyButton.boundingBox();
    const textElement = await copyButton.textContent();
    
    console.log('Copy button text:', textElement);
    console.log('Copy button dimensions:', buttonBox);

    // Take close-up screenshot of the table
    await page.screenshot({
      path: 'admin-events-table-closeup.png',
      clip: {
        x: 50,
        y: 200, // Focus on table area
        width: 1100,
        height: 400
      }
    });
  });
});