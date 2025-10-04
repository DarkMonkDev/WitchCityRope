import { test, expect } from '@playwright/test';

test.describe('EventForm Screenshots for Verification', () => {
  test('Take EventForm screenshots for wireframe comparison', async ({ page }) => {
    // Navigate to the event form test page
    await page.goto('http://localhost:5173/event-form-test');
    
    // Wait for the page to load
    await page.waitForLoadState('networkidle');
    
    // Wait for TinyMCE to be ready
    await page.waitForSelector('.tox-tinymce', { timeout: 10000 });
    
    // Take screenshot of Basic Info tab (default)
    await page.screenshot({ path: 'test-results/event-form-basic-info.png', fullPage: true });
    
    // Click on Emails tab
    await page.locator('.mantine-Tabs-tab:has-text("Emails")').click();
    await page.waitForTimeout(1000); // Wait for tab switch
    
    // Take screenshot of Emails tab
    await page.screenshot({ path: 'test-results/event-form-emails.png', fullPage: true });
    
    // Click on Volunteers tab
    await page.locator('.mantine-Tabs-tab:has-text("Volunteers")').click();
    await page.waitForTimeout(1000); // Wait for tab switch
    
    // Take screenshot of Volunteers tab
    await page.screenshot({ path: 'test-results/event-form-volunteers.png', fullPage: true });
    
    // Verify the form is present
    await expect(page.locator('[data-testid="event-form"]')).toBeVisible();
    
    console.log('Screenshots saved to test-results/ directory');
  });
});