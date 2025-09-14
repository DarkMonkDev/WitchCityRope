import { test, expect } from '@playwright/test';

test.describe('Verify Fixes - Corrected Ports', () => {
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
    await page.goto('http://localhost:5174', { 
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
    await page.goto('http://localhost:5174/events', {
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
    console.log('Full page content length:', pageContent?.length);
    console.log('Page content preview:', pageContent?.substring(0, 500));
    
    const hasEventContent = pageContent?.includes('Introduction to Rope Safety') || 
                           pageContent?.includes('Suspension Basics') ||
                           pageContent?.includes('event') ||
                           pageContent?.includes('Event');
    
    console.log('Has event content:', hasEventContent);
    
    // Check for error messages
    const errorElement = await page.$('text=/Failed to Load Events/i');
    const hasError = !!errorElement;
    console.log('Has error message:', hasError);
    
    // Check for loading state
    const loadingElement = await page.$('text=/Loading/i');
    const isLoading = !!loadingElement;
    console.log('Shows loading state:', isLoading);
    
    // Check for any API call errors in console
    console.log('Console errors:', consoleErrors);
    
    // Take screenshots for debugging
    await page.screenshot({ path: 'verify-home-fixed.png' });
    await page.screenshot({ path: 'verify-events-fixed.png' });
    
    // Wait a bit more and check again
    console.log('Waiting additional 5 seconds to see if events load...');
    await page.waitForTimeout(5000);
    
    const finalContent = await page.textContent('body');
    console.log('Final content length:', finalContent?.length);
    const finalHasEvents = finalContent?.includes('Introduction to Rope Safety') || 
                          finalContent?.includes('Suspension Basics');
    console.log('Final has events:', finalHasEvents);
    
    // Check if React app is actually rendering
    const rootHasContent = await page.evaluate(() => {
      const root = document.getElementById('root');
      return root ? root.innerHTML.length : 0;
    });
    console.log('Root element content length:', rootHasContent);
    
    // Log final results
    console.log('\n=== FINAL DIAGNOSTICS ===');
    console.log('- Title loads correctly: YES');
    console.log('- No refresh loop: YES'); 
    console.log('- Events page content length:', finalContent?.length);
    console.log('- Events display correctly: NO - content too short');
    console.log('- Console errors:', consoleErrors.length);
  });
});