import { test, expect } from '@playwright/test';

test.describe('Events Management API Demo', () => {
  test('should load without constant reloading', async ({ page }) => {
    // Navigate to the demo page
    await page.goto('/admin/events-management-api-demo');
    
    // Wait for the page to load
    await expect(page).toHaveTitle(/Vite \+ React \+ TS/);
    
    // Verify the main title is visible
    await expect(page.locator('h1')).toContainText('Events Management API Integration Demo');
    
    // Verify both API tabs are present
    await expect(page.locator('[role="tab"]')).toHaveText(['Current API (Working)', 'Future Events Management API']);
    
    // Verify the current API tab is active by default
    await expect(page.locator('[role="tab"][aria-selected="true"]')).toContainText('Current API (Working)');
    
    // Wait a few seconds and verify the page doesn't reload
    await page.waitForTimeout(3000);
    
    // The title should still be there (page didn't reload)
    await expect(page.locator('h1')).toContainText('Events Management API Integration Demo');
    
    // Check that fallback data is loaded
    await expect(page.locator('text=Rope Basics Workshop (Fallback)')).toBeVisible();
  });
  
  test('should switch between API tabs without reloading', async ({ page }) => {
    await page.goto('/admin/events-management-api-demo');
    
    // Wait for page to load
    await expect(page.locator('h1')).toContainText('Events Management API Integration Demo');
    
    // Click on Future API tab (use the tab element directly)
    await page.locator('[role="tab"]:has-text("Future Events Management API")').click();
    
    // Verify the tab switched
    await expect(page.locator('[role="tab"][aria-selected="true"]')).toContainText('Future Events Management API');
    
    // Verify the disabled notice is shown
    await expect(page.locator('text=API calls are disabled to prevent errors')).toBeVisible();
    
    // Switch back to Current API tab
    await page.locator('[role="tab"]:has-text("Current API (Working)")').click();
    
    // Verify events are still shown
    await expect(page.locator('text=Rope Basics Workshop (Fallback)')).toBeVisible();
  });
});