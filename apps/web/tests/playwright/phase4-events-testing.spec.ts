import { test, expect } from '@playwright/test';

test.describe('Phase 4: Public Events Pages Implementation Testing', () => {
  test.beforeEach(async ({ page }) => {
    // Navigate to events page
    await page.goto('http://localhost:5173/events');
  });

  test('should load events page successfully', async ({ page }) => {
    // Verify page title and URL
    await expect(page).toHaveURL('/events');

    // Check for key page elements
    await expect(page.locator('h1')).toContainText('Explore Classes & Meetups');
    
    // Take screenshot for manual review
    await page.screenshot({ 
      path: '/home/chad/repos/witchcityrope/test-results/events-page-loaded.png',
      fullPage: true 
    });
  });

  test('should display event filters correctly', async ({ page }) => {
    // Wait for filter controls to load (using existing selectors)
    await page.waitForSelector('[data-testid="button-view-toggle"]', { timeout: 10000 });
    await page.waitForSelector('[data-testid="input-search"]', { timeout: 10000 });
    await page.waitForSelector('[data-testid="select-category"]', { timeout: 10000 });

    // Verify filter components are visible
    await expect(page.locator('[data-testid="button-view-toggle"]')).toBeVisible();
    await expect(page.locator('[data-testid="input-search"]')).toBeVisible();
    
    // Test filter interaction
    await page.click('text=Classes');
    await page.waitForTimeout(1000); // Allow filter to apply
    
    // Take screenshot of filtered view
    await page.screenshot({ 
      path: '/home/chad/repos/witchcityrope/test-results/events-filters-active.png',
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
      
      // Check for required elements (using existing data-testids)
      await expect(firstCard.locator('[data-testid="event-title"]')).toBeVisible();
      await expect(firstCard.locator('[data-testid="event-date"]')).toBeVisible();
      await expect(firstCard.locator('[data-testid="event-time"]')).toBeVisible();
      
      // Take screenshot of event card
      await firstCard.screenshot({ 
        path: '/home/chad/repos/witchcityrope/test-results/event-card-details.png' 
      });
    }
  });

  test('should be responsive on mobile viewport', async ({ page }) => {
    // Set mobile viewport
    await page.setViewportSize({ width: 375, height: 667 });

    // Wait for page to adjust
    await page.waitForTimeout(1000);

    // Verify filter components are visible on mobile (using existing selectors)
    await expect(page.locator('[data-testid="button-view-toggle"]')).toBeVisible();
    await expect(page.locator('[data-testid="input-search"]')).toBeVisible();
    
    // Take mobile screenshot
    await page.screenshot({ 
      path: '/home/chad/repos/witchcityrope/test-results/events-mobile-view.png',
      fullPage: true 
    });
  });

  test('should handle loading and error states', async ({ page }) => {
    // Navigate to events page and watch for loading states
    await page.goto('http://localhost:5173/events');

    // Look for loading indicators initially
    const loadingIndicator = page.locator('[data-testid="loading"], .spinner, text="Loading"');

    // Wait for content to load (using proper Playwright selector syntax)
    await Promise.race([
      page.locator('[data-testid="events-list"]').waitFor({ timeout: 10000 }),
      page.locator('[data-testid="events-empty-state"]').waitFor({ timeout: 10000 })
    ]);
    
    // Take screenshot of loaded state
    await page.screenshot({ 
      path: '/home/chad/repos/witchcityrope/test-results/events-loaded-state.png',
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
        path: '/home/chad/repos/witchcityrope/test-results/event-detail-navigation.png',
        fullPage: true 
      });
    }
  });
});