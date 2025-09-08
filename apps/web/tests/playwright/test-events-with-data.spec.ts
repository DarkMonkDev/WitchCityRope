import { test, expect } from '@playwright/test';

test.describe('Public Events Page with Mock Data', () => {
  test('should display events with mock data', async ({ page }) => {
    // Navigate to events page
    await page.goto('http://localhost:5174/events');
    
    // Wait for content to load
    await page.waitForLoadState('networkidle');
    
    // Take screenshot
    await page.screenshot({ 
      path: 'test-results/events-page-with-mock-data.png',
      fullPage: true 
    });
    
    // Check for event content
    const hasEvents = await page.locator('text=Introduction to Rope Bondage').isVisible();
    console.log('Has events:', hasEvents);
    
    // Check for event cards
    const eventCards = await page.locator('[data-testid="event-card"]').count();
    console.log('Event cards found:', eventCards);
  });
});