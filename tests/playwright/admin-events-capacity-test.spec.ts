import { test, expect } from '@playwright/test';

test.describe('Admin Events Capacity Display Issues', () => {

  test('Admin Events List - Capacity column shows 0 for all events', async ({ page }) => {
    console.log('🔍 Testing Admin Events List capacity display');

    // Navigate to login page
    await page.goto('http://localhost:5173');

    // Take screenshot of initial page
    await page.screenshot({ path: './test-results/01-initial-page.png', fullPage: true });

    // Click login button
    await page.click('[data-testid="login-button"]', { timeout: 10000 });

    // Wait for login modal to appear
    await page.waitForSelector('[data-testid="email-input"]', { timeout: 10000 });

    // Take screenshot of login modal
    await page.screenshot({ path: './test-results/02-login-modal.png', fullPage: true });

    // Login as admin
    await page.fill('[data-testid="email-input"]', 'admin@witchcityrope.com');
    await page.fill('[data-testid="password-input"]', 'Test123!');

    // Click submit login
    await page.click('[data-testid="submit-login"]');

    // Wait for dashboard to load
    await page.waitForSelector('[data-testid="admin-menu"]', { timeout: 15000 });

    // Take screenshot of admin dashboard
    await page.screenshot({ path: './test-results/03-admin-dashboard.png', fullPage: true });

    // Navigate to admin events
    await page.click('[data-testid="admin-menu"]');
    await page.waitForSelector('[data-testid="admin-events-link"]', { timeout: 5000 });
    await page.click('[data-testid="admin-events-link"]');

    // Wait for events list to load
    await page.waitForSelector('[data-testid="events-table"]', { timeout: 10000 });

    // Take screenshot of admin events list
    await page.screenshot({ path: './test-results/04-admin-events-list.png', fullPage: true });

    // Check for capacity column
    const capacityHeader = page.locator('th:has-text("Capacity")');
    await expect(capacityHeader).toBeVisible();

    // Find all capacity cells
    const capacityCells = page.locator('td[data-testid*="capacity"]');
    const capacityCount = await capacityCells.count();

    console.log(`Found ${capacityCount} capacity cells`);

    // Document what each capacity cell shows
    const capacityValues = [];
    for (let i = 0; i < capacityCount; i++) {
      const cellText = await capacityCells.nth(i).textContent();
      capacityValues.push(cellText?.trim() || 'empty');
      console.log(`Capacity cell ${i}: "${cellText}"`);
    }

    // Check console for errors
    const consoleMessages = [];
    page.on('console', msg => {
      if (msg.type() === 'error') {
        consoleMessages.push(msg.text());
      }
    });

    // Take final screenshot
    await page.screenshot({ path: './test-results/05-capacity-columns-detail.png', fullPage: true });

    // Test API calls directly to see what data is returned
    const eventsApiResponse = await page.request.get('http://localhost:5655/api/admin/events');
    const eventsData = await eventsApiResponse.json();

    console.log('📊 Events API Response:', JSON.stringify(eventsData, null, 2));

    // Log findings
    console.log('🔍 FINDINGS:');
    console.log(`- Capacity values displayed: ${JSON.stringify(capacityValues)}`);
    console.log(`- Events API status: ${eventsApiResponse.status()}`);
    console.log(`- Events API returned ${eventsData?.length || 0} events`);
    console.log(`- Console errors: ${consoleMessages.length}`);

    if (consoleMessages.length > 0) {
      console.log('Console errors:', consoleMessages);
    }

    // Document the discrepancy
    const report = {
      test: 'Admin Events Capacity Display',
      timestamp: new Date().toISOString(),
      capacityValuesShown: capacityValues,
      apiResponseStatus: eventsApiResponse.status(),
      eventsFromApi: eventsData?.length || 0,
      consoleErrors: consoleMessages,
      screenshots: [
        './test-results/01-initial-page.png',
        './test-results/02-login-modal.png',
        './test-results/03-admin-dashboard.png',
        './test-results/04-admin-events-list.png',
        './test-results/05-capacity-columns-detail.png'
      ]
    };

    console.log('📋 Test Report:', JSON.stringify(report, null, 2));
  });

  test('Check API vs UI data discrepancy for event capacity', async ({ page }) => {
    console.log('🔍 Testing API vs UI data for event capacity');

    // Get events from API directly
    const eventsResponse = await page.request.get('http://localhost:5655/api/admin/events');
    const eventsApiData = await eventsResponse.json();

    console.log('📊 Raw API Response:', JSON.stringify(eventsApiData, null, 2));

    // Login and get UI data
    await page.goto('http://localhost:5173');
    await page.click('[data-testid="login-button"]');
    await page.waitForSelector('[data-testid="email-input"]');
    await page.fill('[data-testid="email-input"]', 'admin@witchcityrope.com');
    await page.fill('[data-testid="password-input"]', 'Test123!');
    await page.click('[data-testid="submit-login"]');

    // Navigate to events
    await page.waitForSelector('[data-testid="admin-menu"]');
    await page.click('[data-testid="admin-menu"]');
    await page.click('[data-testid="admin-events-link"]');
    await page.waitForSelector('[data-testid="events-table"]');

    // Extract data from UI table
    const eventRows = page.locator('[data-testid="event-row"]');
    const rowCount = await eventRows.count();

    const uiEventData = [];
    for (let i = 0; i < rowCount; i++) {
      const row = eventRows.nth(i);
      const eventName = await row.locator('[data-testid="event-name"]').textContent();
      const capacity = await row.locator('[data-testid="event-capacity"]').textContent();

      uiEventData.push({
        name: eventName?.trim(),
        capacityShown: capacity?.trim()
      });
    }

    console.log('📊 UI Data:', JSON.stringify(uiEventData, null, 2));

    // Take screenshot of comparison
    await page.screenshot({ path: './test-results/06-api-vs-ui-comparison.png', fullPage: true });

    // Document the comparison
    const comparison = {
      test: 'API vs UI Capacity Data',
      timestamp: new Date().toISOString(),
      apiEvents: eventsApiData?.length || 0,
      uiEventsShown: uiEventData.length,
      apiStatus: eventsResponse.status(),
      detailedComparison: {
        apiData: eventsApiData,
        uiData: uiEventData
      }
    };

    console.log('📋 API vs UI Comparison:', JSON.stringify(comparison, null, 2));
  });
});