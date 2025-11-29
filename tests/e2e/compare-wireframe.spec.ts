import { test, expect } from '@playwright/test';
import { AuthHelpers } from './test-utils/helpers/auth.helpers';

const baseUrl = process.env.PLAYWRIGHT_BASE_URL || 'http://localhost:5173';
const wireframeUrl = process.env.WIREFRAME_BASE_URL || 'http://localhost:8080';

test.describe('Wireframe Comparison', () => {
  test('capture original wireframe', async ({ page }) => {
    // Navigate to the original wireframe
    await page.goto(`${wireframeUrl}/docs/functional-areas/events/admin-events-management/event-creation.html`);
    
    // Wait for the page to load
    await page.waitForLoadState('domcontentloaded');
    
    // Take a screenshot
    await page.screenshot({ 
      path: 'test-results/original-wireframe.png',
      fullPage: true 
    });
  });

  test('capture current implementation', async ({ page }) => {
    // Login as admin before accessing admin pages
    await AuthHelpers.loginAs(page, 'admin');

    // Navigate to the current implementation
    await page.goto(`${baseUrl}/admin/event-session-matrix-demo`);
    
    // Wait for the page to load
    await page.waitForLoadState('domcontentloaded');
    
    // Take a screenshot
    await page.screenshot({ 
      path: 'test-results/current-implementation.png',
      fullPage: true 
    });
  });
});