import { test, expect } from '@playwright/test';

test.describe('Debug Events Page', () => {
  test('debug why events are not showing', async ({ page }) => {
    // Listen for console messages
    page.on('console', msg => {
      if (msg.type() === 'error') {
        console.log('Browser console error:', msg.text());
      }
    });
    
    // Navigate to events page
    await page.goto('http://localhost:5173/events');
    
    // Wait longer for React to render
    await page.waitForTimeout(3000);
    
    // Check page title
    const title = await page.textContent('h1');
    console.log('Page title:', title);
    
    // Check for loading indicator
    const hasLoadingText = await page.locator('text=Loading events').isVisible();
    console.log('Has loading text:', hasLoadingText);
    
    // Check for any event text
    const pageContent = await page.textContent('body');
    const hasIntroEvent = pageContent.includes('Introduction to Rope');
    console.log('Has Introduction event:', hasIntroEvent);
    
    // Take screenshot
    await page.screenshot({ 
      path: 'test-results/debug-events-page.png',
      fullPage: true 
    });
    
    // Try to get any data from React DevTools
    await page.evaluate(() => {
      console.log('Window location:', window.location.href);
    });
  });
});