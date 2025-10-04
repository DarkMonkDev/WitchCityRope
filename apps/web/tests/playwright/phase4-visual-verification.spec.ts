import { test, expect } from '@playwright/test';

test.describe('Phase 4: Public Events Pages - Visual Verification', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('http://localhost:5173/events');
    // Wait for page to fully load
    await page.waitForTimeout(2000);
  });

  test('should display events page with correct branding and layout', async ({ page }) => {
    // Verify URL is correct
    expect(page.url()).toBe('http://localhost:5173/events');
    
    // Check WitchCityRope branding in header
    await expect(page.locator('text="WITCH CITY ROPE"')).toBeVisible();
    
    // Check main title
    await expect(page.locator('h1:has-text("Events & Classes")')).toBeVisible();
    
    // Check subtitle
    await expect(page.locator('text="Discover workshops, classes, and social events"')).toBeVisible();
    
    // Take full page screenshot for design verification
    await page.screenshot({ 
      path: '/home/chad/repos/witchcityrope-react/test-results/phase4-events-page-desktop.png',
      fullPage: true 
    });
  });

  test('should display filter section with correct options', async ({ page }) => {
    // Check filter section exists
    await expect(page.locator('text="Filter Events"')).toBeVisible();
    
    // Check filter chips are present
    await expect(page.locator('text="All Events"')).toBeVisible();
    await expect(page.locator('text="Classes Only"')).toBeVisible();
    await expect(page.locator('text="Social Events"')).toBeVisible();
    await expect(page.locator('text="Member Events Only"')).toBeVisible();
    
    // Check instructor dropdown
    await expect(page.locator('input[placeholder="Filter by instructor"]')).toBeVisible();
    
    // Check "View Past Events" link
    await expect(page.locator('text="View Past Events"')).toBeVisible();
    
    // Take screenshot of filters section
    const filtersSection = page.locator('text="Filter Events"').locator('..');
    await filtersSection.screenshot({ 
      path: '/home/chad/repos/witchcityrope-react/test-results/phase4-filters-section.png' 
    });
  });

  test('should show empty state when no events available', async ({ page }) => {
    // Check for empty state content
    await expect(page.locator('text="No Events Found"')).toBeVisible();
    await expect(page.locator('text="There are no upcoming events scheduled"')).toBeVisible();
    
    // Take screenshot of empty state
    await page.screenshot({ 
      path: '/home/chad/repos/witchcityrope-react/test-results/phase4-empty-state.png',
      fullPage: true 
    });
  });

  test('should be responsive on mobile viewport', async ({ page }) => {
    // Set mobile viewport (iPhone SE)
    await page.setViewportSize({ width: 375, height: 667 });
    await page.waitForTimeout(1000);
    
    // Verify page still loads correctly
    await expect(page.locator('h1:has-text("Events & Classes")')).toBeVisible();
    await expect(page.locator('text="Filter Events"')).toBeVisible();
    
    // Take mobile screenshot
    await page.screenshot({ 
      path: '/home/chad/repos/witchcityrope-react/test-results/phase4-events-mobile-375px.png',
      fullPage: true 
    });
  });

  test('should be responsive on tablet viewport', async ({ page }) => {
    // Set tablet viewport (iPad)
    await page.setViewportSize({ width: 768, height: 1024 });
    await page.waitForTimeout(1000);
    
    // Verify layout adapts correctly
    await expect(page.locator('h1:has-text("Events & Classes")')).toBeVisible();
    await expect(page.locator('text="Filter Events"')).toBeVisible();
    
    // Take tablet screenshot
    await page.screenshot({ 
      path: '/home/chad/repos/witchcityrope-react/test-results/phase4-events-tablet-768px.png',
      fullPage: true 
    });
  });

  test('should handle filter interactions', async ({ page }) => {
    // Click on Classes Only filter
    const classesFilter = page.locator('text="Classes Only"');
    await classesFilter.click();
    await page.waitForTimeout(500);
    
    // Verify filter is active (should be visually different)
    // Note: Without data-testid, we can't easily verify state changes
    
    // Click on Social Events filter
    const socialFilter = page.locator('text="Social Events"');
    await socialFilter.click();
    await page.waitForTimeout(500);
    
    // Take screenshot of filter interaction
    await page.screenshot({ 
      path: '/home/chad/repos/witchcityrope-react/test-results/phase4-filter-interactions.png',
      fullPage: true 
    });
  });

  test('should display correct navigation elements', async ({ page }) => {
    // Check main navigation
    await expect(page.locator('text="EVENTS & CLASSES"')).toBeVisible();
    await expect(page.locator('text="HOW TO JOIN"')).toBeVisible();
    await expect(page.locator('text="RESOURCES"')).toBeVisible();
    await expect(page.locator('text="LOGIN"')).toBeVisible();
    
    // Check utility bar
    await expect(page.locator('text="REPORT AN INCIDENT"')).toBeVisible();
    await expect(page.locator('text="PRIVATE LESSONS"')).toBeVisible();
    await expect(page.locator('text="CONTACT"')).toBeVisible();
    
    // Take screenshot focused on navigation
    await page.screenshot({ 
      path: '/home/chad/repos/witchcityrope-react/test-results/phase4-navigation-elements.png',
      clip: { x: 0, y: 0, width: 1280, height: 200 }
    });
  });

  test('should match design specifications visually', async ({ page }) => {
    // Set consistent viewport for design comparison
    await page.setViewportSize({ width: 1200, height: 800 });
    await page.waitForTimeout(1000);
    
    // Take screenshot for design comparison
    await page.screenshot({ 
      path: '/home/chad/repos/witchcityrope-react/test-results/phase4-design-comparison.png',
      fullPage: true 
    });
    
    // Check that WitchCityRope brand colors are present
    // (Visual verification - burgundy/wine colors should be visible)
    
    // Check typography - titles should be clearly visible and readable
    const mainTitle = page.locator('h1:has-text("Events & Classes")');
    await expect(mainTitle).toBeVisible();
    
    // Check that filter section has proper styling
    const filterSection = page.locator('text="Filter Events"').locator('..');
    await expect(filterSection).toBeVisible();
  });

  test('should load within performance targets', async ({ page }) => {
    const startTime = Date.now();
    
    // Navigate to events page
    await page.goto('http://localhost:5173/events');
    
    // Wait for main content to be visible
    await expect(page.locator('h1:has-text("Events & Classes")')).toBeVisible();
    
    const loadTime = Date.now() - startTime;
    console.log(`Page load time: ${loadTime}ms`);
    
    // Verify load time is under 2 seconds (as specified in handoff)
    expect(loadTime).toBeLessThan(2000);
  });
});