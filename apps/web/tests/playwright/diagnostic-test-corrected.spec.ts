import { test, expect } from '@playwright/test';

test.describe('Diagnostic Test - Corrected Ports', () => {
  test('check page loading and refreshing issue', async ({ page }) => {
    // Set up console monitoring
    const consoleLogs: string[] = [];
    const consoleErrors: string[] = [];
    
    page.on('console', msg => {
      if (msg.type() === 'error') {
        consoleErrors.push(msg.text());
      } else {
        consoleLogs.push(`[${msg.type()}] ${msg.text()}`);
      }
    });

    // Monitor navigation events
    const navigations: string[] = [];
    page.on('framenavigated', () => {
      navigations.push(`Navigation at ${new Date().toISOString()}`);
    });

    console.log('Navigating to http://localhost:5174');
    
    // Go to the page with timeout
    try {
      await page.goto('http://localhost:5174', { 
        waitUntil: 'networkidle',
        timeout: 10000 
      });
    } catch (error) {
      console.log('Error during navigation:', error);
    }

    // Wait a bit to see if refreshing happens
    await page.waitForTimeout(3000);

    // Take a screenshot
    await page.screenshot({ path: 'diagnostic-home.png' });

    // Check for basic page structure
    const title = await page.title();
    console.log('Page title:', title);

    // Check if root element exists
    const rootElement = await page.$('#root');
    console.log('Root element exists:', !!rootElement);

    // Check current URL
    const currentUrl = page.url();
    console.log('Current URL:', currentUrl);

    // Print console logs
    console.log('\n=== Console Logs ===');
    consoleLogs.forEach(log => console.log(log));

    // Print console errors
    if (consoleErrors.length > 0) {
      console.log('\n=== Console Errors ===');
      consoleErrors.forEach(err => console.log(err));
    }

    // Print navigations
    console.log('\n=== Navigation Events ===');
    console.log('Total navigations:', navigations.length);
    navigations.forEach(nav => console.log(nav));

    // Try to go to events page
    console.log('\nNavigating to /events');
    try {
      await page.goto('http://localhost:5174/events', { 
        waitUntil: 'networkidle',
        timeout: 10000 
      });
      
      await page.waitForTimeout(2000);
      
      // Take screenshot of events page
      await page.screenshot({ path: 'diagnostic-events.png' });
      
      // Check for events content
      const eventsContent = await page.textContent('body');
      console.log('Events page loaded, content length:', eventsContent?.length);
      
      // Check for error messages
      const errorElement = await page.$('text=/Failed to Load Events/i');
      console.log('Error message present:', !!errorElement);
      
      if (errorElement) {
        const errorText = await errorElement.textContent();
        console.log('Error text:', errorText);
      }
      
      // Look for specific event names we saw in API
      const hasEventTitles = eventsContent?.includes('Introduction to Rope Safety') ||
                           eventsContent?.includes('Suspension Basics') ||
                           eventsContent?.includes('Community Rope Jam');
      console.log('Has expected event titles:', hasEventTitles);
      
    } catch (error) {
      console.log('Error navigating to events:', error);
    }

    // Final console state
    console.log('\n=== Final Console State ===');
    console.log('Total console logs:', consoleLogs.length);
    console.log('Total console errors:', consoleErrors.length);
    console.log('Total navigations:', navigations.length);
  });
});