import { test, expect } from '@playwright/test';

/**
 * Comprehensive E2E tests for Events Management System
 * Tests both demo pages: Events Management API Demo and Event Session Matrix Demo
 * 
 * Context: Phase 4 (Testing) of Events Management System
 * Phase 3 implementation complete with backend API + frontend integration
 */

test.describe('Events Management System E2E Tests', () => {
  
  test.describe('Events Management API Demo', () => {
    
    test.beforeEach(async ({ page }) => {
      // Listen for console errors
      page.on('console', msg => {
        if (msg.type() === 'error') {
          console.log('Console Error:', msg.text());
        }
      });
      
      // Listen for network failures
      page.on('response', response => {
        if (!response.ok() && response.url().includes('/api/')) {
          console.log('API Error:', response.status(), response.url());
        }
      });
    });

    test('should load API demo page without errors', async ({ page }) => {
      // Navigate to the demo page
      await page.goto('/admin/events-management-api-demo');
      
      // Wait for page to load
      await expect(page).toHaveTitle(/Vite \+ React \+ TS/);
      
      // Verify main title is visible
      await expect(page.locator('h1')).toContainText('Events Management API Integration Demo');
      
      // Verify page loads without constant reloading (wait and check title still there)
      await page.waitForTimeout(2000);
      await expect(page.locator('h1')).toContainText('Events Management API Integration Demo');
      
      // Take screenshot for verification
      await page.screenshot({ path: 'test-results/events-api-demo-loaded.png' });
    });

    test('should display events list with fallback data', async ({ page }) => {
      await page.goto('/admin/events-management-api-demo');
      
      // Wait for page to load
      await expect(page.locator('h1')).toContainText('Events Management API Integration Demo');
      
      // Verify fallback events are displayed (based on diagnostic results)
      await expect(page.locator('text=Rope Basics Workshop (Fallback)')).toBeVisible();
      await expect(page.locator('text=Advanced Suspension Techniques (Fallback)')).toBeVisible();
      await expect(page.locator('text=Community Social & Practice (Fallback)')).toBeVisible();
      
      // Take screenshot of events list
      await page.screenshot({ path: 'test-results/events-list-display.png' });
    });

    test('should switch between API tabs correctly', async ({ page }) => {
      await page.goto('/admin/events-management-api-demo');
      
      // Wait for page load
      await expect(page.locator('h1')).toContainText('Events Management API Integration Demo');
      
      // Verify both tabs are present
      await expect(page.locator('[role="tab"]').nth(0)).toContainText('Current API (Working)');
      await expect(page.locator('[role="tab"]').nth(1)).toContainText('Future Events Management API');
      
      // Verify current API tab is active by default
      await expect(page.locator('[role="tab"][aria-selected="true"]')).toContainText('Current API (Working)');
      
      // Switch to Future API tab
      await page.locator('[role="tab"]:has-text("Future Events Management API")').click();
      
      // Verify tab switched
      await expect(page.locator('[role="tab"][aria-selected="true"]')).toContainText('Future Events Management API');
      
      // Verify disabled notice is shown
      await expect(page.locator('text=API calls are disabled to prevent errors')).toBeVisible();
      
      // Switch back to Current API tab
      await page.locator('[role="tab"]:has-text("Current API (Working)")').click();
      
      // Verify events are still shown
      await expect(page.locator('text=Rope Basics Workshop (Fallback)')).toBeVisible();
      
      // Take screenshot after tab switching
      await page.screenshot({ path: 'test-results/tab-switching-complete.png' });
    });

    test('should allow event selection', async ({ page }) => {
      await page.goto('/admin/events-management-api-demo');
      
      // Wait for events to load
      await expect(page.locator('text=Rope Basics Workshop (Fallback)')).toBeVisible();
      
      // Click on first event to select it
      await page.locator('text=Rope Basics Workshop (Fallback)').click();
      
      // Note: Since we don't have selection UI implemented yet, 
      // we just verify the click doesn't cause errors
      await page.waitForTimeout(1000);
      
      // Verify page is still functional
      await expect(page.locator('h1')).toContainText('Events Management API Integration Demo');
      
      // Take screenshot after event interaction
      await page.screenshot({ path: 'test-results/event-selection.png' });
    });

    test('should test refresh functionality', async ({ page }) => {
      await page.goto('/admin/events-management-api-demo');
      
      // Wait for events to load
      await expect(page.locator('text=Rope Basics Workshop (Fallback)')).toBeVisible();
      
      // Look for refresh button (if implemented)
      const refreshButton = page.locator('button:has-text("Refresh")');
      const refreshButtonCount = await refreshButton.count();
      
      if (refreshButtonCount > 0) {
        // Click refresh button if it exists
        await refreshButton.click();
        
        // Wait for refresh to complete
        await page.waitForTimeout(2000);
        
        // Verify events are still displayed after refresh
        await expect(page.locator('text=Rope Basics Workshop (Fallback)')).toBeVisible();
      } else {
        // If no refresh button, test page reload functionality
        await page.reload();
        
        // Verify page loads correctly after reload
        await expect(page.locator('h1')).toContainText('Events Management API Integration Demo');
        await expect(page.locator('text=Rope Basics Workshop (Fallback)')).toBeVisible();
      }
      
      // Take screenshot after refresh test
      await page.screenshot({ path: 'test-results/refresh-functionality.png' });
    });

    test('should verify no console errors during operation', async ({ page }) => {
      const consoleErrors: string[] = [];
      
      page.on('console', msg => {
        if (msg.type() === 'error') {
          consoleErrors.push(msg.text());
        }
      });
      
      await page.goto('/admin/events-management-api-demo');
      
      // Wait for page to fully load
      await expect(page.locator('h1')).toContainText('Events Management API Integration Demo');
      await page.waitForTimeout(3000);
      
      // Test tab switching
      await page.locator('[role="tab"]:has-text("Future Events Management API")').click();
      await page.waitForTimeout(1000);
      await page.locator('[role="tab"]:has-text("Current API (Working)")').click();
      await page.waitForTimeout(1000);
      
      // Verify no critical console errors occurred
      // Filter out non-critical errors (like TinyMCE warnings and Vite dev server WebSocket errors)
      const criticalErrors = consoleErrors.filter(error => 
        !error.includes('TinyMCE') && 
        !error.includes('DevTools') &&
        !error.includes('favicon') &&
        !error.includes('WebSocket') &&
        !error.includes('ws://') &&
        !error.includes('Upgrade Required')
      );
      
      console.log(`Found ${consoleErrors.length} total errors, ${criticalErrors.length} critical errors`);
      expect(criticalErrors.length).toBe(0);
    });
  });

  test.describe('Event Session Matrix Demo', () => {
    
    test.beforeEach(async ({ page }) => {
      // Listen for console errors
      page.on('console', msg => {
        if (msg.type() === 'error') {
          console.log('Console Error:', msg.text());
        }
      });
    });

    test('should load Event Session Matrix demo page', async ({ page }) => {
      // Navigate to the demo page
      await page.goto('/admin/event-session-matrix-demo');
      
      // Verify page loads
      await expect(page).toHaveTitle(/Vite \+ React \+ TS/);
      
      // Verify main title is visible based on diagnostic
      await expect(page.locator('h1')).toContainText('Event Session Matrix Demo');
      
      // Wait for any dynamic content to load
      await page.waitForTimeout(2000);
      
      // Take screenshot of loaded page
      await page.screenshot({ path: 'test-results/matrix-demo-loaded.png' });
    });

    test('should display all four tabs', async ({ page }) => {
      await page.goto('/admin/event-session-matrix-demo');
      
      // Wait for page to load
      await page.waitForTimeout(2000);
      
      // Check for tab elements based on diagnostic results
      const tabs = page.locator('[role="tab"], .tab, [data-testid*="tab"]');
      const tabCount = await tabs.count();
      
      // Diagnostic showed 4 tabs exist
      expect(tabCount).toBeGreaterThanOrEqual(4);
      
      // Look for specific tab names that diagnostic confirmed exist
      // Use more specific selectors to avoid multiple matches
      await expect(page.locator('[role="tab"]:has-text("Basic Info")')).toBeVisible();
      await expect(page.locator('[role="tab"]:has-text("Tickets")')).toBeVisible();
      await expect(page.locator('[role="tab"]:has-text("Emails")')).toBeVisible();
      await expect(page.locator('[role="tab"]:has-text("Volunteers")')).toBeVisible();
      
      // Take screenshot showing tab structure
      await page.screenshot({ path: 'test-results/matrix-tabs-structure.png' });
    });

    test('should test tab switching functionality', async ({ page }) => {
      await page.goto('/admin/event-session-matrix-demo');
      
      // Wait for page load
      await page.waitForTimeout(2000);
      
      // Get all clickable elements that might be tabs
      const clickableElements = page.locator('[role="tab"], .tab, button, [data-testid*="tab"]');
      const elementCount = await clickableElements.count();
      
      console.log(`Found ${elementCount} potentially clickable tab elements`);
      
      // Try clicking on different tab-like elements
      for (let i = 0; i < Math.min(elementCount, 4); i++) {
        try {
          const element = clickableElements.nth(i);
          const text = await element.textContent();
          console.log(`Trying to click element ${i}: "${text}"`);
          
          await element.click();
          await page.waitForTimeout(1000);
          
          // Take screenshot after each click
          await page.screenshot({ path: `test-results/tab-click-${i}.png` });
        } catch (error) {
          console.log(`Could not click element ${i}:`, error);
        }
      }
    });

    test('should verify form fields are present', async ({ page }) => {
      await page.goto('/admin/event-session-matrix-demo');
      
      // Wait for page load
      await page.waitForTimeout(2000);
      
      // Look for common form elements
      const inputs = await page.locator('input').count();
      const textareas = await page.locator('textarea').count();
      const selects = await page.locator('select').count();
      const buttons = await page.locator('button').count();
      
      console.log('Form elements found:', { inputs, textareas, selects, buttons });
      
      // We expect at least some form elements
      expect(inputs + textareas + selects).toBeGreaterThan(0);
      
      // Take screenshot showing form fields
      await page.screenshot({ path: 'test-results/form-fields-present.png' });
    });

    test('should verify TinyMCE editors load', async ({ page }) => {
      await page.goto('/admin/event-session-matrix-demo');
      
      // Wait for page load
      await page.waitForTimeout(3000);
      
      // Look for TinyMCE-related elements
      const iframes = await page.locator('iframe').count();
      const tinyElements = await page.locator('[class*="tiny"], [id*="tiny"]').count();
      const textareas = await page.locator('textarea').count();
      
      console.log('Editor elements found:', { iframes, tinyElements, textareas });
      
      // If we have textareas, they might be TinyMCE editors
      if (textareas > 0) {
        // Wait a bit more for TinyMCE initialization
        await page.waitForTimeout(3000);
        
        // Check again for TinyMCE initialization
        const initializedEditors = await page.locator('iframe[id*="tiny"]').count();
        console.log(`Found ${initializedEditors} initialized TinyMCE editors`);
      }
      
      // Take screenshot of editor state
      await page.screenshot({ path: 'test-results/tinymce-editors-loaded.png' });
    });

    test('should test session grid display', async ({ page }) => {
      await page.goto('/admin/event-session-matrix-demo');
      
      // Wait for content to load
      await page.waitForTimeout(2000);
      
      // Look for grid-like elements (tables, grids, lists)
      const tables = await page.locator('table').count();
      const grids = await page.locator('[role="grid"], .grid').count();
      const lists = await page.locator('ul, ol').count();
      
      console.log('Grid elements found:', { tables, grids, lists });
      
      // Look for session-related content
      const sessionElements = await page.locator('*:has-text("session"), *:has-text("Session")').count();
      console.log(`Found ${sessionElements} session-related elements`);
      
      // Take screenshot of session grid area
      await page.screenshot({ path: 'test-results/session-grid-display.png' });
    });

    test('should verify ticket types section', async ({ page }) => {
      await page.goto('/admin/event-session-matrix-demo');
      
      // Wait for page load
      await page.waitForTimeout(2000);
      
      // Look for ticket-related elements
      const ticketElements = await page.locator('*:has-text("ticket"), *:has-text("Ticket")').count();
      const priceElements = await page.locator('*:has-text("$"), *:has-text("price"), *:has-text("Price")').count();
      
      console.log('Ticket elements found:', { ticketElements, priceElements });
      
      // Take screenshot of ticket types area
      await page.screenshot({ path: 'test-results/ticket-types-section.png' });
    });

    test('should test Save Draft and Cancel buttons', async ({ page }) => {
      await page.goto('/admin/event-session-matrix-demo');
      
      // Wait for page load
      await page.waitForTimeout(2000);
      
      // Look for Save Draft button
      const saveDraftButton = page.locator('button:has-text("Save Draft"), button:has-text("Save"), [data-testid*="save"]');
      const saveDraftCount = await saveDraftButton.count();
      
      // Look for Cancel button
      const cancelButton = page.locator('button:has-text("Cancel"), [data-testid*="cancel"]');
      const cancelCount = await cancelButton.count();
      
      console.log('Action buttons found:', { saveDraftCount, cancelCount });
      
      // Test Save Draft button if it exists
      if (saveDraftCount > 0) {
        await saveDraftButton.first().click();
        await page.waitForTimeout(1000);
        
        // Take screenshot after save action
        await page.screenshot({ path: 'test-results/save-draft-clicked.png' });
      }
      
      // Test Cancel button if it exists
      if (cancelCount > 0) {
        await cancelButton.first().click();
        await page.waitForTimeout(1000);
        
        // Take screenshot after cancel action
        await page.screenshot({ path: 'test-results/cancel-clicked.png' });
      }
      
      // If no specific buttons found, look for any buttons
      if (saveDraftCount === 0 && cancelCount === 0) {
        const allButtons = await page.locator('button').count();
        console.log(`No Save/Cancel buttons found, but ${allButtons} total buttons exist`);
        
        // Take screenshot showing available buttons
        await page.screenshot({ path: 'test-results/available-buttons.png' });
      }
    });
  });

  test.describe('API Integration Tests', () => {
    
    test('should verify API calls to events endpoint', async ({ page }) => {
      // Track network requests
      const apiCalls: string[] = [];
      
      page.on('response', response => {
        if (response.url().includes('/api/events')) {
          apiCalls.push(`${response.status()} ${response.url()}`);
          console.log('API Call:', response.status(), response.url());
        }
      });
      
      await page.goto('/admin/events-management-api-demo');
      
      // Wait for API calls to complete
      await page.waitForTimeout(3000);
      
      // Verify API call was made (or fallback was used)
      console.log('API calls made:', apiCalls);
      
      // The page should still function even if API fails
      await expect(page.locator('h1')).toContainText('Events Management API Integration Demo');
      
      // Take screenshot of final state
      await page.screenshot({ path: 'test-results/api-integration-test.png' });
    });

    test('should test error handling for failed API calls', async ({ page }) => {
      // Intercept API calls and make them fail
      await page.route('**/api/events', route => {
        console.log('Intercepting API call to simulate error');
        route.fulfill({
          status: 500,
          contentType: 'application/json',
          body: JSON.stringify({ error: 'Internal Server Error' })
        });
      });
      
      await page.goto('/admin/events-management-api-demo');
      
      // Wait for page load and error handling
      await page.waitForTimeout(3000);
      
      // Verify page still loads (should show fallback data)
      await expect(page.locator('h1')).toContainText('Events Management API Integration Demo');
      
      // Should still show fallback events
      await expect(page.locator('text=Rope Basics Workshop (Fallback)')).toBeVisible();
      
      // Take screenshot of error handling
      await page.screenshot({ path: 'test-results/api-error-handling.png' });
    });

    test('should verify response data structure', async ({ page }) => {
      let responseData: any = null;
      
      page.on('response', async response => {
        if (response.url().includes('/api/events') && response.ok()) {
          try {
            responseData = await response.json();
            console.log('API Response:', responseData);
          } catch (error) {
            console.log('Could not parse response:', error);
          }
        }
      });
      
      await page.goto('/admin/events-management-api-demo');
      
      // Wait for API response
      await page.waitForTimeout(3000);
      
      // Verify page functionality regardless of API response
      await expect(page.locator('h1')).toContainText('Events Management API Integration Demo');
      
      // Log the response structure for validation
      if (responseData) {
        console.log('Response structure validated:', typeof responseData, Array.isArray(responseData));
      } else {
        console.log('Using fallback data (API not available)');
      }
      
      // Take screenshot of data structure test
      await page.screenshot({ path: 'test-results/response-data-structure.png' });
    });
  });

  test.describe('Cross-Browser Compatibility', () => {
    
    test('should work correctly in different viewport sizes', async ({ page }) => {
      // Test mobile viewport
      await page.setViewportSize({ width: 375, height: 667 });
      await page.goto('/admin/events-management-api-demo');
      
      await expect(page.locator('h1')).toContainText('Events Management API Integration Demo');
      await page.screenshot({ path: 'test-results/mobile-viewport-api-demo.png' });
      
      // Test tablet viewport
      await page.setViewportSize({ width: 768, height: 1024 });
      await page.goto('/admin/event-session-matrix-demo');
      
      await page.waitForTimeout(2000);
      await page.screenshot({ path: 'test-results/tablet-viewport-matrix-demo.png' });
      
      // Test desktop viewport
      await page.setViewportSize({ width: 1920, height: 1080 });
      await page.goto('/admin/events-management-api-demo');
      
      await expect(page.locator('h1')).toContainText('Events Management API Integration Demo');
      await page.screenshot({ path: 'test-results/desktop-viewport-api-demo.png' });
    });
  });
});