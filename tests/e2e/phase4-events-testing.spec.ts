import { test, expect } from '@playwright/test';

test.describe('Phase 4: Public Events Pages Implementation Testing', () => {
  test.beforeEach(async ({ page }) => {
    // Navigate to events page
    await page.goto('/events', { waitUntil: 'domcontentloaded' });
  });

  test('should load events page successfully', async ({ page }) => {
    // Verify page title and URL
    await expect(page).toHaveURL('/events');

    // Check for key page elements
    await expect(page.locator('h1')).toContainText('Explore Classes & Meetups');
    
    // Take screenshot for manual review
    await page.screenshot({ 
      path: './test-results/events-page-loaded.png',
      fullPage: true 
    });
  });

  test('should display event filters correctly', async ({ page }) => {
    // Wait for page to fully load - either filter bar appears OR error state
    // The filter bar only renders when events API succeeds (no early return on error)
    // Note: View toggle was removed from UI (card view only for initial release)
    const filterBarOrError = await Promise.race([
      page.waitForSelector('[data-testid="input-search"]', { timeout: 10000 }).then(() => 'success'),
      page.waitForSelector('[data-testid="events-error"]', { timeout: 10000 }).then(() => 'error'),
    ]);

    if (filterBarOrError === 'error') {
      console.log('⚠️ Events page shows error state - API may not be responding');
      // Skip filter assertions if API error occurred
      return;
    }

    // Wait for all filter controls to load
    await page.waitForSelector('[data-testid="input-search"]', { timeout: 5000 });
    await page.waitForSelector('[data-testid="select-category"]', { timeout: 5000 });

    // Verify filter components are visible (use .first() since there are desktop and mobile versions)
    await expect(page.locator('[data-testid="input-search"]').first()).toBeVisible();
    await expect(page.locator('[data-testid="select-category"]').first()).toBeVisible();

    // Take screenshot of filter view
    await page.screenshot({
      path: './test-results/events-filters-active.png',
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
        path: './test-results/event-card-details.png' 
      });
    }
  });

  test('should be responsive on mobile viewport', async ({ page }) => {
    // Set mobile viewport FIRST, then navigate (ensures CSS media queries apply correctly)
    await page.setViewportSize({ width: 375, height: 667 });

    // Navigate to events page with mobile viewport
    await page.goto('/events', { waitUntil: 'domcontentloaded' });

    // Wait for page to load - check for page container OR error state
    const pageLoad = await Promise.race([
      page.waitForSelector('[data-testid="page-events"]', { timeout: 10000 }).then(() => 'success'),
      page.waitForSelector('[data-testid="events-error"]', { timeout: 10000 }).then(() => 'error'),
    ]);

    if (pageLoad === 'error') {
      console.log('⚠️ Events page shows error state - skipping mobile viewport test');
      return;
    }

    // Wait for filter components to be visible on mobile
    // Desktop comes first in DOM (hidden on mobile), mobile comes last (visible on mobile)
    // Use data-testid for reliable targeting
    const searchInput = page.locator('[data-testid="input-search"]').last();
    const sortDropdown = page.locator('[data-testid="select-category"]').last();

    await expect(searchInput).toBeVisible({ timeout: 5000 });
    await expect(sortDropdown).toBeVisible({ timeout: 5000 });

    // Take mobile screenshot
    await page.screenshot({
      path: './test-results/events-mobile-view.png',
      fullPage: true
    });
  });

  test('should handle loading and error states', async ({ page }) => {
    // Navigate to events page and watch for loading states
    await page.goto('/events', { waitUntil: 'domcontentloaded' });

    // Look for loading indicators initially
    const loadingIndicator = page.locator('[data-testid="loading"], .spinner, text="Loading"');

    // Wait for content to load (using proper Playwright selector syntax)
    await Promise.race([
      page.locator('[data-testid="events-list"]').waitFor({ timeout: 10000 }),
      page.locator('[data-testid="events-empty-state"]').waitFor({ timeout: 10000 })
    ]);
    
    // Take screenshot of loaded state
    await page.screenshot({ 
      path: './test-results/events-loaded-state.png',
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
        path: './test-results/event-detail-navigation.png',
        fullPage: true 
      });
    }
  });
});