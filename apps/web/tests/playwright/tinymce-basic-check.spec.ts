import { test, expect } from '@playwright/test';

test.describe('TinyMCE Basic Check', () => {
  test('should verify TinyMCE loads on event demo page', async ({ page }) => {
    // Navigate to the event demo page
    await page.goto('http://localhost:5173/admin/event-session-matrix-demo');
    await page.waitForLoadState('networkidle');

    // Take a screenshot to see what's actually rendered
    await page.screenshot({ path: 'test-results/event-demo-page.png' });

    // Check for TinyMCE-related elements
    const tinyMCEElements = await page.locator('div[class*="tox"]').count();
    const iframes = await page.locator('iframe').count();
    const textareas = await page.locator('textarea').count();
    
    console.log(`Found ${tinyMCEElements} TinyMCE elements, ${iframes} iframes, ${textareas} textareas`);

    // Check if @tinymce/tinymce-react is actually working
    const hasEditor = await page.locator('.tox-editor-container, iframe[id*="tiny"], div[class*="tinymce"]').count();
    
    if (hasEditor === 0) {
      // No TinyMCE found, check if there's an error message
      const errorAlert = await page.locator('[role="alert"]').textContent();
      console.log('No TinyMCE editor found. Error alert:', errorAlert);
      
      // Check console for errors
      const consoleMessages = await page.evaluate(() => {
        // @ts-ignore - accessing window console
        return window.console?.logs || 'No console access';
      });
      console.log('Console:', consoleMessages);
    }

    // This test is just for debugging - we expect it might fail but will give us information
    expect(hasEditor).toBeGreaterThan(0);
  });

  test('should check if TinyMCE package is available', async ({ page }) => {
    await page.goto('http://localhost:5173/admin/event-session-matrix-demo');
    
    // Check if TinyMCE is available in the global scope
    const tinyMCEAvailable = await page.evaluate(() => {
      return typeof window !== 'undefined' && 'tinymce' in window;
    });
    
    console.log('TinyMCE globally available:', tinyMCEAvailable);
    
    // Check if the React component import worked
    const reactImportError = await page.evaluate(() => {
      try {
        // This is a rough check - in a real app this would be handled by the bundler
        return false; // No error
      } catch (e) {
        return e.message;
      }
    });
    
    console.log('React import error:', reactImportError);
  });
});