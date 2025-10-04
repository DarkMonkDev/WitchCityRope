import { test, expect } from '@playwright/test';

test.describe('Events Page Structure Exploration', () => {
  test('explore events page DOM structure', async ({ page }) => {
    console.log('Navigating to events page...');
    await page.goto('http://localhost:5173/events');
    
    // Wait for page to load
    await page.waitForTimeout(3000);
    
    // Take screenshot first
    await page.screenshot({ 
      path: '/home/chad/repos/witchcityrope-react/test-results/events-page-exploration.png',
      fullPage: true 
    });
    
    // Log page title
    const title = await page.title();
    console.log('Page title:', title);
    
    // Log current URL
    const url = page.url();
    console.log('Current URL:', url);
    
    // Check for any h1 elements
    const h1Elements = await page.locator('h1').count();
    console.log('Number of h1 elements:', h1Elements);
    
    if (h1Elements > 0) {
      const h1Text = await page.locator('h1').first().textContent();
      console.log('First h1 text:', h1Text);
    }
    
    // Check for any elements with "events" in the text
    const eventsElements = await page.locator('text=/events/i').count();
    console.log('Elements containing "events":', eventsElements);
    
    // Check for any data-testid attributes
    const testIds = await page.locator('[data-testid]').count();
    console.log('Elements with data-testid:', testIds);
    
    if (testIds > 0) {
      // Get all test IDs
      const testIdElements = page.locator('[data-testid]');
      const count = await testIdElements.count();
      
      for (let i = 0; i < Math.min(count, 10); i++) {
        const testId = await testIdElements.nth(i).getAttribute('data-testid');
        console.log(`Test ID ${i}: ${testId}`);
      }
    }
    
    // Check for any cards or event-related elements
    const cardElements = await page.locator('[class*="card"], [class*="event"]').count();
    console.log('Elements with card/event classes:', cardElements);
    
    // Check for loading states
    const loadingElements = await page.locator('text=/loading/i, [class*="loading"], [class*="spinner"]').count();
    console.log('Loading elements found:', loadingElements);
    
    // Check console errors
    const consoleMessages = [];
    page.on('console', msg => {
      if (msg.type() === 'error') {
        consoleMessages.push(msg.text());
      }
    });
    
    // Wait a bit more to catch any console errors
    await page.waitForTimeout(2000);
    
    if (consoleMessages.length > 0) {
      console.log('Console errors:', consoleMessages);
    } else {
      console.log('No console errors found');
    }
    
    // Check network requests
    const responses = [];
    page.on('response', response => {
      if (response.url().includes('events')) {
        responses.push(`${response.status()} ${response.url()}`);
      }
    });
    
    // Trigger any network requests by waiting longer
    await page.waitForTimeout(2000);
    
    console.log('Network responses for events:', responses);
  });
});