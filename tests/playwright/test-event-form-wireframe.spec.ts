import { test, expect } from '@playwright/test';

test.describe('Event Form Wireframe Verification', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('http://localhost:5173');
  });

  test('should screenshot emails tab to verify wireframe match', async ({ page }) => {
    // Navigate to the event form test page
    await page.goto('http://localhost:5173/event-form-test');
    
    // Wait for the form to load
    await page.waitForSelector('button[data-tabs-value="emails"]', { timeout: 10000 });
    
    // Click on the Emails tab
    await page.click('button[data-tabs-value="emails"]');
    
    // Wait a moment for the tab content to load
    await page.waitForTimeout(500);
    
    // Take screenshot of the emails tab
    await page.screenshot({ 
      path: 'test-results/event-form-emails-tab.png',
      fullPage: true 
    });
    
    // Click on the Volunteers/Staff tab
    await page.click('button[data-tabs-value="volunteers-staff"]');
    
    // Wait a moment for the tab content to load
    await page.waitForTimeout(500);
    
    // Take screenshot of the volunteers/staff tab
    await page.screenshot({ 
      path: 'test-results/event-form-volunteers-staff-tab.png',
      fullPage: true 
    });
    
    console.log('Screenshots saved: event-form-emails-tab.png and event-form-volunteers-staff-tab.png');
  });
});