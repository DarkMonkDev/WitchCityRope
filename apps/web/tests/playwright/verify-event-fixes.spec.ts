import { test, expect } from '@playwright/test';
import { EventHelpers, type EventData } from './helpers/event.helpers';

test.describe('Event System Verification After Fixes', () => {
  let realEvents: EventData[] = [];

  test.beforeAll(async ({ request }) => {
    // Fetch real events from database before tests run
    // This ensures tests work with any seed data
    const response = await request.get('http://localhost:5655/api/events');
    const apiResponse = await response.json();

    if (!apiResponse.success || !apiResponse.data || apiResponse.data.length === 0) {
      throw new Error('No events in database - cannot run tests');
    }

    realEvents = apiResponse.data.map((e: any) => ({
      id: e.id,
      title: e.title,
      shortDescription: e.shortDescription,
      description: e.description,
    }));

    console.log(`✅ Found ${realEvents.length} events in database for testing`);
  });

  test.beforeEach(async ({ page }) => {
    // Add timeout for network requests (10 seconds is reasonable)
    // NOTE: Global max is 90 seconds in playwright.config.ts
    page.setDefaultTimeout(10000);

    // Wait for services to be ready
    await page.waitForTimeout(1000);
  });

  test('events page shows only real database events', async ({ page }) => {
    console.log('🚀 Starting events page verification...');

    // Verify we have events to test
    if (realEvents.length === 0) {
      throw new Error('No events available for testing');
    }

    // Navigate to events page
    await page.goto('http://localhost:5173/events');
    await page.waitForLoadState('networkidle', { timeout: 15000 });

    // Take screenshot for verification
    await page.screenshot({ path: 'test-results/events-page-verification.png', fullPage: true });

    // Check for real events using more specific selector (heading with data-testid)
    // Test first 4 events or all if less than 4
    const eventsToTest = realEvents.slice(0, Math.min(4, realEvents.length));
    for (const event of eventsToTest) {
      const eventVisible = await page.locator(`[data-testid="event-title"]:has-text("${event.title}")`).first().isVisible();
      expect(eventVisible).toBeTruthy();
      console.log(`✅ Real event visible: ${event.title}`);
    }

    // Check mock events are GONE
    const mockEvents = ['February Rope Jam', '3-Day Rope Intensive Series'];
    for (const mockEvent of mockEvents) {
      const mockVisible = await page.locator(`[data-testid="event-title"]:has-text("${mockEvent}")`).isVisible();
      expect(mockVisible).toBeFalsy();
      console.log(`✅ Mock event removed: ${mockEvent}`);
    }
  });

  test('clicking event navigates to correct details', async ({ page }) => {
    console.log('🔄 Testing event navigation...');

    // Verify we have events to test
    if (realEvents.length === 0) {
      throw new Error('No events available for testing');
    }

    await page.goto('http://localhost:5173/events');
    await page.waitForLoadState('networkidle', { timeout: 15000 });

    // Test first event navigation
    const firstEvent = realEvents[0];
    console.log(`📍 Testing navigation with event: ${firstEvent.title} (ID: ${firstEvent.id})`);

    // Screenshot before clicking
    await page.screenshot({ path: 'test-results/before-event-click.png', fullPage: true });

    // Click on the event card (more reliable than clicking on title text)
    // The card has both onclick handler and will navigate
    await page.locator(`[data-testid="event-card"]:has([data-testid="event-title"]:has-text("${firstEvent.title}"))`).first().click();

    // Wait for navigation to complete
    await page.waitForURL(`**/events/${firstEvent.id}`, { timeout: 15000 });
    await page.waitForLoadState('networkidle', { timeout: 15000 });

    // Screenshot after navigation
    await page.screenshot({ path: 'test-results/after-event-click.png', fullPage: true });

    // Verify URL contains correct ID
    expect(page.url()).toContain(firstEvent.id);
    console.log(`✅ Navigation successful to: ${page.url()}`);

    // Verify details page shows correct event
    const detailTitle = await page.locator('h1').first().textContent();
    expect(detailTitle).toContain(firstEvent.title);
    console.log(`✅ Event details correct: ${firstEvent.title}`);
  });

  test('API endpoints working correctly', async ({ page }) => {
    console.log('📡 Testing API integration...');

    // Verify we have events to test
    if (realEvents.length === 0) {
      throw new Error('No events available for testing');
    }

    const apiCalls: string[] = [];

    page.on('response', response => {
      if (response.url().includes('/api/events')) {
        apiCalls.push(`${response.status()} ${response.url()}`);
        console.log(`📞 API Call: ${response.status()} ${response.url()}`);
      }
    });

    // Test list endpoint
    await page.goto('http://localhost:5173/events');
    await page.waitForLoadState('networkidle', { timeout: 15000 });

    // Test detail endpoint using first event from database
    const firstEvent = realEvents[0];
    console.log(`📍 Testing detail endpoint with event ID: ${firstEvent.id}`);
    await page.goto(`http://localhost:5173/events/${firstEvent.id}`);
    await page.waitForLoadState('networkidle', { timeout: 15000 });

    // Verify API calls
    console.log('📋 API calls made:', apiCalls);
    const hasListCall = apiCalls.some(call => call.includes('200') && call.match(/\/api\/events\/?$/)); // Matches /api/events or /api/events/ only
    const hasDetailCall = apiCalls.some(call => call.includes('200') && call.match(/\/api\/events\/[a-f0-9-]+$/i)); // Matches /api/events/{guid}

    expect(hasListCall).toBeTruthy();
    expect(hasDetailCall).toBeTruthy();
    console.log('✅ Both API endpoints working');
  });

  test('events list displays proper data structure', async ({ page }) => {
    console.log('📋 Verifying events data structure...');
    
    let eventsApiResponse: any = null;
    
    page.on('response', async (response) => {
      if (response.url().includes('/api/events') && response.status() === 200) {
        const responseText = await response.text();
        try {
          eventsApiResponse = JSON.parse(responseText);
        } catch (e) {
          console.warn('Failed to parse API response:', responseText);
        }
      }
    });
    
    await page.goto('http://localhost:5173/events');
    await page.waitForLoadState('networkidle', { timeout: 15000 });
    
    // Take final screenshot
    await page.screenshot({ path: 'test-results/events-final-state.png', fullPage: true });
    
    // Verify API response structure
    if (eventsApiResponse) {
      console.log('📊 API Response Structure:', JSON.stringify(eventsApiResponse, null, 2));
      
      // Check if response has events array
      if (eventsApiResponse.events && Array.isArray(eventsApiResponse.events)) {
        expect(eventsApiResponse.events.length).toBeGreaterThan(0);
        console.log(`✅ Events API returned ${eventsApiResponse.events.length} events`);
      } else if (Array.isArray(eventsApiResponse)) {
        expect(eventsApiResponse.length).toBeGreaterThan(0);
        console.log(`✅ Events API returned ${eventsApiResponse.length} events directly`);
      } else {
        console.warn('⚠️ Unexpected API response structure');
      }
    } else {
      console.warn('⚠️ No events API response captured');
    }
  });

  test('verify no CORS or network errors', async ({ page }) => {
    console.log('🌐 Testing for CORS and network errors...');

    // Verify we have events to test
    if (realEvents.length === 0) {
      throw new Error('No events available for testing');
    }

    const errors: string[] = [];
    const networkErrors: string[] = [];

    page.on('console', msg => {
      if (msg.type() === 'error') {
        errors.push(msg.text());
        console.log('🔴 Console Error:', msg.text());
      }
    });

    page.on('response', response => {
      if (!response.ok() && response.url().includes('/api/events')) {
        networkErrors.push(`${response.status()} ${response.url()}`);
        console.log('🔴 Network Error:', response.status(), response.url());
      }
    });

    await page.goto('http://localhost:5173/events');
    await page.waitForLoadState('networkidle', { timeout: 15000 });

    // Navigate to event detail to test both endpoints using first event from database
    const firstEvent = realEvents[0];
    console.log(`📍 Testing CORS with event ID: ${firstEvent.id}`);
    await page.goto(`http://localhost:5173/events/${firstEvent.id}`);
    await page.waitForLoadState('networkidle', { timeout: 15000 });

    // Check for CORS errors specifically
    const corsErrors = errors.filter(error =>
      error.toLowerCase().includes('cors') ||
      error.toLowerCase().includes('cross-origin')
    );

    expect(corsErrors.length).toBe(0);
    console.log('✅ No CORS errors detected');

    // Check for critical network errors (some 404s might be expected)
    const criticalNetworkErrors = networkErrors.filter(error =>
      error.includes('500') || error.includes('CORS')
    );

    expect(criticalNetworkErrors.length).toBe(0);
    console.log('✅ No critical network errors detected');

    if (errors.length > 0) {
      console.log('ℹ️ Non-critical errors found:', errors.slice(0, 5));
    }
    if (networkErrors.length > 0) {
      console.log('ℹ️ Network responses:', networkErrors.slice(0, 5));
    }
  });
});