import { test, expect } from '@playwright/test';

test.describe('Admin Events Page - Data Loading Diagnostic', () => {
  test('should load events data in admin events page', async ({ page }) => {
    // Navigate to admin events page
    await page.goto('http://localhost:5173/admin/events');
    
    // Wait for page to load
    await page.waitForLoadState('networkidle');
    
    // Check if page title loads
    const title = await page.locator('h1').first();
    await expect(title).toBeVisible();
    console.log('Page title:', await title.textContent());
    
    // Check for loading state initially
    const loader = page.locator('[data-testid="loader"], .mantine-Loader-root');
    
    // Wait for either events to load or error to show
    await page.waitForFunction(() => {
      const eventsTable = document.querySelector('[data-testid="events-table"], table');
      const errorAlert = document.querySelector('[data-testid="error-alert"], .mantine-Alert-root');
      const noEventsMessage = document.querySelector(':text("No events")');
      return eventsTable || errorAlert || noEventsMessage;
    }, { timeout: 10000 });
    
    // Check network requests
    const apiRequests = [];
    page.on('request', request => {
      if (request.url().includes('/api/events')) {
        apiRequests.push({
          url: request.url(),
          method: request.method()
        });
        console.log('API Request:', request.method(), request.url());
      }
    });
    
    page.on('response', response => {
      if (response.url().includes('/api/events')) {
        console.log('API Response:', response.status(), response.url());
      }
    });
    
    // Check for events table or error state
    const eventsTable = page.locator('table, [data-testid="events-table"]');
    const errorAlert = page.locator('[data-testid="error-alert"], .mantine-Alert-root');
    const noEventsText = page.locator(':text("No events")');
    
    if (await eventsTable.isVisible()) {
      console.log('✅ Events table is visible');
      
      // Count rows (excluding header)
      const rows = await page.locator('tbody tr, table tr').count();
      console.log('Events table rows:', rows);
      
      if (rows > 0) {
        // Get first event title
        const firstEventTitle = await page.locator('tbody tr:first-child td:first-child, table tr:first-child td:first-child').textContent();
        console.log('First event title:', firstEventTitle);
      }
    } else if (await errorAlert.isVisible()) {
      const errorText = await errorAlert.textContent();
      console.log('❌ Error alert visible:', errorText);
    } else if (await noEventsText.isVisible()) {
      console.log('⚠️ "No events" message visible');
    } else {
      console.log('❓ Unknown state - no table, error, or no-events message found');
    }
    
    // Take screenshot for debugging
    await page.screenshot({ path: 'admin-events-diagnostic.png' });
    
    console.log('API Requests made:', apiRequests.length);
  });
});