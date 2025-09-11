import { test, expect } from '@playwright/test';

test.describe('Wireframe Comparison', () => {
  test('capture original wireframe', async ({ page }) => {
    // Navigate to the original wireframe
    await page.goto('http://localhost:8080/docs/functional-areas/events/admin-events-management/event-creation.html');
    
    // Wait for the page to load
    await page.waitForLoadState('networkidle');
    
    // Take a screenshot
    await page.screenshot({ 
      path: 'test-results/original-wireframe.png',
      fullPage: true 
    });
  });

  test('capture current implementation', async ({ page }) => {
    // Navigate to the current implementation
    await page.goto('http://localhost:5173/admin/event-session-matrix-demo');
    
    // Wait for the page to load
    await page.waitForLoadState('networkidle');
    
    // Take a screenshot
    await page.screenshot({ 
      path: 'test-results/current-implementation.png',
      fullPage: true 
    });
  });
});