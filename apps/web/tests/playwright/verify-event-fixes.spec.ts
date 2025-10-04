import { test, expect } from '@playwright/test';

test.describe('Event System Verification After Fixes', () => {
  // Expected real events from database
  const realEvents = [
    { id: 'e1111111-1111-1111-1111-111111111111', title: 'Introduction to Rope Bondage' },
    { id: 'e2222222-2222-2222-2222-222222222222', title: 'Midnight Rope Performance' },
    { id: 'e3333333-3333-3333-3333-333333333333', title: 'Monthly Rope Social' },
    { id: 'e4444444-4444-4444-4444-444444444444', title: 'Advanced Suspension Techniques' }
  ];

  test.beforeEach(async ({ page }) => {
    // Add timeout for network requests
    page.setDefaultTimeout(10000);
    
    // Wait for services to be ready
    await page.waitForTimeout(1000);
  });

  test('events page shows only real database events', async ({ page }) => {
    console.log('🚀 Starting events page verification...');
    
    // Navigate to events page
    await page.goto('http://localhost:5173/events');
    await page.waitForLoadState('networkidle', { timeout: 15000 });
    
    // Take screenshot for verification
    await page.screenshot({ path: 'test-results/events-page-verification.png', fullPage: true });
    
    // Check for real events
    for (const event of realEvents) {
      const eventVisible = await page.locator(`text="${event.title}"`).isVisible();
      expect(eventVisible).toBeTruthy();
      console.log(`✅ Real event visible: ${event.title}`);
    }
    
    // Check mock events are GONE
    const mockEvents = ['February Rope Jam', '3-Day Rope Intensive Series'];
    for (const mockEvent of mockEvents) {
      const mockVisible = await page.locator(`text="${mockEvent}"`).isVisible();
      expect(mockVisible).toBeFalsy();
      console.log(`✅ Mock event removed: ${mockEvent}`);
    }
  });

  test('clicking event navigates to correct details', async ({ page }) => {
    console.log('🔄 Testing event navigation...');
    
    await page.goto('http://localhost:5173/events');
    await page.waitForLoadState('networkidle', { timeout: 15000 });
    
    // Test first event navigation
    const firstEvent = realEvents[0];
    
    // Screenshot before clicking
    await page.screenshot({ path: 'test-results/before-event-click.png', fullPage: true });
    
    await page.click(`text="${firstEvent.title}"`);
    await page.waitForLoadState('networkidle', { timeout: 15000 });
    
    // Screenshot after navigation
    await page.screenshot({ path: 'test-results/after-event-click.png', fullPage: true });
    
    // Verify URL contains correct ID
    expect(page.url()).toContain(firstEvent.id);
    
    // Verify details page shows correct event
    const detailTitle = await page.locator('h1, h2').first().textContent();
    expect(detailTitle).toContain(firstEvent.title);
    console.log(`✅ Event details correct: ${firstEvent.title}`);
  });

  test('API endpoints working correctly', async ({ page }) => {
    console.log('📡 Testing API integration...');
    
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
    
    // Test detail endpoint
    await page.goto(`http://localhost:5173/events/${realEvents[0].id}`);
    await page.waitForLoadState('networkidle', { timeout: 15000 });
    
    // Verify API calls
    console.log('📋 API calls made:', apiCalls);
    const hasListCall = apiCalls.some(call => call.includes('200') && call.includes('/api/events') && !call.includes('/api/events/e'));
    const hasDetailCall = apiCalls.some(call => call.includes('200') && call.includes('/api/events/e'));
    
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
    
    // Navigate to event detail to test both endpoints
    await page.goto(`http://localhost:5173/events/${realEvents[0].id}`);
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