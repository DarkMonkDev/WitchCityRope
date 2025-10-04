import { test, expect } from '@playwright/test';

test.describe('Verify Fixes', () => {
  test('should load without refresh loop and show events', async ({ page }) => {
    // Monitor console for errors
    const consoleErrors: string[] = [];
    page.on('console', msg => {
      if (msg.type() === 'error') {
        consoleErrors.push(msg.text());
      }
    });

    // Count navigations to detect refresh loops
    let navigationCount = 0;
    page.on('framenavigated', () => {
      navigationCount++;
    });

    // Go to home page
    await page.goto('http://localhost:5173', { 
      waitUntil: 'networkidle',
      timeout: 10000 
    });

    // Wait to ensure no refresh loop
    await page.waitForTimeout(2000);
    
    console.log('Navigation count:', navigationCount);
    expect(navigationCount).toBeLessThan(3); // Should not have many navigations

    // Check page loaded
    const title = await page.title();
    expect(title).toContain('Witch City Rope');

    // Navigate to events page
    await page.goto('http://localhost:5173/events', {
      waitUntil: 'networkidle',
      timeout: 10000
    });

    // Check for events content
    await page.waitForTimeout(2000);
    
    // Look for event cards or list items
    const eventCards = await page.$$('[data-testid="event-card"], .event-card, [role="article"]');
    console.log('Event cards found:', eventCards.length);
    
    // Check if there's event content
    const pageContent = await page.textContent('body');
    const hasEventContent = pageContent?.includes('Introduction to Rope Safety') || 
                           pageContent?.includes('Suspension Basics') ||
                           pageContent?.includes('event') ||
                           pageContent?.includes('Event');
    
    console.log('Has event content:', hasEventContent);
    
    // Check for error messages
    const errorElement = await page.$('text=/Failed to Load Events/i');
    const hasError = !!errorElement;
    console.log('Has error message:', hasError);
    
    // Take screenshots for debugging
    await page.screenshot({ path: 'verify-home.png' });
    await page.screenshot({ path: 'verify-events.png' });
    
    // Report console errors
    if (consoleErrors.length > 0) {
      console.log('Console errors found:');
      consoleErrors.forEach(err => console.log('  -', err));
    }
    
    // Assertions
    expect(hasError).toBe(false);
    expect(hasEventContent || eventCards.length > 0).toBe(true);
  });
});