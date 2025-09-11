import { test, expect } from '@playwright/test';

test.describe('Public Event Pages Comparison', () => {
  test('capture dashboard events page', async ({ page }) => {
    // Navigate to the dashboard events page (currently requires auth)
    await page.goto('http://localhost:5173/dashboard/events');
    
    // Wait for the page to load
    await page.waitForLoadState('networkidle');
    
    // Take a screenshot
    await page.screenshot({ 
      path: 'test-results/dashboard-events-page.png',
      fullPage: true 
    });
  });

  test('capture public events wireframe', async ({ page }) => {
    // Navigate to the public events list wireframe
    await page.goto('http://localhost:8080/docs/functional-areas/events/public-events/event-list.html');
    
    // Wait for the page to load
    await page.waitForLoadState('networkidle');
    
    // Take a screenshot
    await page.screenshot({ 
      path: 'test-results/public-events-wireframe.png',
      fullPage: true 
    });
  });

  test('capture event detail wireframe', async ({ page }) => {
    // Navigate to the event detail wireframe
    await page.goto('http://localhost:8080/docs/functional-areas/events/public-events/event-detail.html');
    
    // Wait for the page to load
    await page.waitForLoadState('networkidle');
    
    // Take a screenshot
    await page.screenshot({ 
      path: 'test-results/event-detail-wireframe.png',
      fullPage: true 
    });
  });
});