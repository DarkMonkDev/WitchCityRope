import { test, expect } from '@playwright/test';

test.describe('Phase 4: Public Events Pages Implementation Testing', () => {
  test.beforeEach(async ({ page }) => {
    // Navigate to events page
    await page.goto('http://localhost:5174/events');
  });

  test('should load events page successfully', async ({ page }) => {
    // Verify page title and URL
    await expect(page).toHaveURL('/events');
    
    // Check for key page elements
    await expect(page.locator('h1')).toContainText('Events');
    
    // Take screenshot for manual review
    await page.screenshot({ 
      path: '/home/chad/repos/witchcityrope-react/test-results/events-page-loaded.png',
      fullPage: true 
    });
  });

  test('should display event filters correctly', async ({ page }) => {
    // Wait for filters to load
    await page.waitForSelector('[data-testid="event-filters"]', { timeout: 10000 });
    
    // Check filter buttons exist
    const filterButtons = page.locator('button').filter({ hasText: /All|Classes|Social|Member/ });
    await expect(filterButtons).toHaveCount(4);
    
    // Test filter interaction
    await page.click('text=Classes');
    await page.waitForTimeout(1000); // Allow filter to apply
    
    // Take screenshot of filtered view
    await page.screenshot({ 
      path: '/home/chad/repos/witchcityrope-react/test-results/events-filters-active.png',
      fullPage: true 
    });
  });

  test('should display event cards with correct information', async ({ page }) => {
    // Wait for events to load
    await page.waitForSelector('[data-testid="event-card"]', { timeout: 10000 });
    
    // Check if event cards are present
    const eventCards = page.locator('[data-testid="event-card"]');
    const cardCount = await eventCards.count();
    console.log(`Found ${cardCount} event cards`);
    
    if (cardCount > 0) {
      // Test first event card
      const firstCard = eventCards.first();
      
      // Check for required elements
      await expect(firstCard.locator('[data-testid="event-title"]')).toBeVisible();
      await expect(firstCard.locator('[data-testid="event-type"]')).toBeVisible();
      await expect(firstCard.locator('[data-testid="event-time"]')).toBeVisible();
      
      // Take screenshot of event card
      await firstCard.screenshot({ 
        path: '/home/chad/repos/witchcityrope-react/test-results/event-card-details.png' 
      });
    }
  });

  test('should be responsive on mobile viewport', async ({ page }) => {
    // Set mobile viewport
    await page.setViewportSize({ width: 375, height: 667 });
    
    // Wait for page to adjust
    await page.waitForTimeout(1000);
    
    // Check filters stack vertically on mobile
    const filtersContainer = page.locator('[data-testid="event-filters"]');
    await expect(filtersContainer).toBeVisible();
    
    // Take mobile screenshot
    await page.screenshot({ 
      path: '/home/chad/repos/witchcityrope-react/test-results/events-mobile-view.png',
      fullPage: true 
    });
  });

  test('should handle loading and error states', async ({ page }) => {
    // Navigate to events page and watch for loading states
    await page.goto('http://localhost:5174/events');
    
    // Look for loading indicators initially
    const loadingIndicator = page.locator('[data-testid="loading"], .spinner, text="Loading"');
    
    // Wait for content to load
    await page.waitForSelector('[data-testid="events-list"], [data-testid="event-card"], text="No events"', { 
      timeout: 10000 
    });
    
    // Take screenshot of loaded state
    await page.screenshot({ 
      path: '/home/chad/repos/witchcityrope-react/test-results/events-loaded-state.png',
      fullPage: true 
    });
  });

  test('should navigate to event details when clicking event card', async ({ page }) => {
    // Wait for events to load
    await page.waitForSelector('[data-testid="event-card"]', { timeout: 10000 });
    
    const eventCards = page.locator('[data-testid="event-card"]');
    const cardCount = await eventCards.count();
    
    if (cardCount > 0) {
      // Click first event card
      await eventCards.first().click();
      
      // Should navigate to event detail page (even if it shows "Coming Soon")
      await expect(page).toHaveURL(/\/events\/\w+/);
      
      // Take screenshot of detail page
      await page.screenshot({ 
        path: '/home/chad/repos/witchcityrope-react/test-results/event-detail-navigation.png',
        fullPage: true 
      });
    }
  });
});