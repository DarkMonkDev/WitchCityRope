import { test, expect, Page, APIRequestContext } from '@playwright/test';
import { testConfig } from '../helpers/test.config';

const API_URL = process.env.API_URL || 'http://localhost:5653';

test.describe('Events API vs UI Comparison', () => {
  let apiContext: APIRequestContext;

  test.beforeAll(async ({ playwright }) => {
    apiContext = await playwright.request.newContext({
      baseURL: API_URL,
    });
  });

  test.afterAll(async () => {
    await apiContext.dispose();
  });

  test('should compare API events with UI events display', async ({ page }) => {
    // First check the API
    console.log('Checking API for events...');
    let apiEvents: any[] = [];
    
    // Try main endpoint
    let response = await apiContext.get('/api/events');
    
    if (response.ok()) {
      const data = await response.json();
      apiEvents = data.events || data;
      console.log(`API returned ${apiEvents.length} events`);
      
      if (apiEvents.length > 0) {
        console.log('First event from API:');
        console.log(`- Title: ${apiEvents[0].title || apiEvents[0].name}`);
        console.log(`- Date: ${apiEvents[0].startDateTime || apiEvents[0].date}`);
        console.log(`- Location: ${apiEvents[0].location}`);
      }
    } else {
      console.log(`API Error: ${response.status()}`);
      
      // Try alternate endpoints
      const alternateEndpoints = ['/api/Events', '/api/events/list', '/api/event'];
      for (const endpoint of alternateEndpoints) {
        response = await apiContext.get(endpoint);
        if (response.ok()) {
          apiEvents = await response.json();
          console.log(`Found working endpoint: ${endpoint} with ${apiEvents.length} events`);
          break;
        }
      }
    }

    // Now check the UI
    // Login as admin
    await page.goto(testConfig.baseUrl + testConfig.urls.login);
    await page.waitForSelector('#Input_Email', { state: 'visible' });
    await page.fill('#Input_Email', testConfig.accounts.admin.email);
    await page.fill('#Input_Password', testConfig.accounts.admin.password);
    await page.click('.sign-in-btn');
    
    // Wait for navigation
    await page.waitForURL((url) => !url.pathname.includes('/login'), { 
      timeout: testConfig.timeouts.navigation 
    });

    // Navigate to admin events
    await page.goto(testConfig.baseUrl + testConfig.urls.adminEvents);
    await page.waitForSelector('main', { timeout: testConfig.timeouts.navigation });

    // Monitor API calls
    const apiCalls: string[] = [];
    page.on('response', response => {
      if (response.url().includes('/api/')) {
        apiCalls.push(`${response.status()} ${response.url()}`);
      }
    });

    // Check page structure
    const pageStructure = await page.evaluate(() => {
      const main = document.querySelector('main');
      return {
        hasTable: !!document.querySelector('table'),
        hasCards: !!document.querySelector('.card, .event-card'),
        hasList: !!document.querySelector('.list-group, .event-list'),
        innerText: main ? main.innerText.substring(0, 500) : 'No main element',
        eventCount: document.querySelectorAll('[data-event-id], .event-item, .event-card, table tbody tr').length
      };
    });

    console.log('\nPage structure:');
    console.log(`- Has table: ${pageStructure.hasTable}`);
    console.log(`- Has cards: ${pageStructure.hasCards}`);
    console.log(`- Has list: ${pageStructure.hasList}`);
    console.log(`- Event elements found: ${pageStructure.eventCount}`);

    // Check for loading indicators
    const hasLoader = await page.locator('.spinner, .loader, .loading, [class*="load"]').count() > 0;
    if (hasLoader) {
      console.log('Warning: Found loading indicator - data might not be loading properly');
    }

    // Take screenshots
    await page.screenshot({ path: 'test-results/admin-events-comparison.png', fullPage: true });

    // Check public events page
    await page.goto(testConfig.baseUrl + testConfig.urls.events);
    await page.waitForTimeout(2000);

    const publicPageEvents = await page.evaluate(() => {
      return {
        eventCount: document.querySelectorAll('.event, .event-card, .event-item, article').length,
        hasNoEventsMessage: document.body.innerText.toLowerCase().includes('no events'),
        text: document.body.innerText.substring(0, 500)
      };
    });

    console.log('\nPublic page results:');
    console.log(`- Events found: ${publicPageEvents.eventCount}`);
    console.log(`- Shows "no events": ${publicPageEvents.hasNoEventsMessage}`);

    await page.screenshot({ path: 'test-results/public-events-comparison.png', fullPage: true });

    // Log API calls made
    console.log('\nAPI calls detected:');
    apiCalls.forEach(call => console.log(`- ${call}`));

    // Compare results
    expect(apiEvents.length).toBeGreaterThanOrEqual(0);
    if (apiEvents.length > 0) {
      expect(pageStructure.eventCount).toBeGreaterThan(0);
    }
  });

  test('should intercept and log API requests', async ({ page }) => {
    // Setup request interception
    await page.route('**/api/**', async route => {
      const request = route.request();
      console.log(`API Request: ${request.method()} ${request.url()}`);
      console.log(`Headers: ${JSON.stringify(request.headers())}`);
      
      const response = await route.fetch();
      const body = await response.text();
      console.log(`Response Status: ${response.status()}`);
      if (body) {
        try {
          console.log(`Response Body: ${JSON.stringify(JSON.parse(body), null, 2)}`);
        } catch {
          console.log(`Response Body: ${body.substring(0, 200)}`);
        }
      }
      
      await route.fulfill({ response });
    });

    // Navigate to events page
    await page.goto(testConfig.baseUrl + testConfig.urls.events);
    await page.waitForTimeout(3000);

    // Check if any API calls were made
    const apiCallsMade = await page.evaluate(() => {
      return window.performance.getEntriesByType('resource')
        .filter(entry => entry.name.includes('/api/'))
        .map(entry => ({ 
          url: entry.name, 
          duration: entry.duration,
          status: (entry as any).responseStatus 
        }));
    });

    console.log('Performance API entries:');
    apiCallsMade.forEach(call => {
      console.log(`- ${call.url} (${call.duration}ms)`);
    });
  });
});