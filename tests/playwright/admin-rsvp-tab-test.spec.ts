import { test, expect } from '@playwright/test';

test.describe('Admin Event Details RSVP Tab Issues', () => {

  test('Admin Event Details - RSVP/Tickets tab shows no data', async ({ page }) => {
    console.log('🔍 Testing Admin Event Details RSVP tab');

    // Login as admin
    await page.goto('http://localhost:5173');
    await page.click('[data-testid="login-button"]');
    await page.waitForSelector('[data-testid="email-input"]');
    await page.fill('[data-testid="email-input"]', 'admin@witchcityrope.com');
    await page.fill('[data-testid="password-input"]', 'Test123!');
    await page.click('[data-testid="submit-login"]');

    // Navigate to admin events
    await page.waitForSelector('[data-testid="admin-menu"]');
    await page.click('[data-testid="admin-menu"]');
    await page.click('[data-testid="admin-events-link"]');
    await page.waitForSelector('[data-testid="events-table"]');

    // Take screenshot of events list
    await page.screenshot({ path: './test-results/07-admin-events-before-details.png', fullPage: true });

    // Click on first event to view details
    const firstEventRow = page.locator('[data-testid="event-row"]').first();
    await firstEventRow.click();

    // Wait for event details page
    await page.waitForSelector('[data-testid="event-details"]', { timeout: 10000 });

    // Take screenshot of event details page
    await page.screenshot({ path: './test-results/08-event-details-page.png', fullPage: true });

    // Look for RSVP/Tickets tab
    const rsvpTab = page.locator('[data-testid="rsvp-tab"], [data-testid="tickets-tab"], text=RSVP, text=Tickets');

    if (await rsvpTab.count() > 0) {
      // Click RSVP tab
      await rsvpTab.first().click();

      // Wait for tab content
      await page.waitForTimeout(2000);

      // Take screenshot of RSVP tab
      await page.screenshot({ path: './test-results/09-rsvp-tab-content.png', fullPage: true });

      // Check for participation data
      const participationRows = page.locator('[data-testid*="participation"], [data-testid*="rsvp"], [data-testid*="ticket"]');
      const participationCount = await participationRows.count();

      console.log(`Found ${participationCount} participation entries in UI`);

      // Check for "no data" messages
      const noDataMessages = await page.locator('text=No RSVPs, text=No tickets, text=No participants, text=No data').count();
      console.log(`Found ${noDataMessages} "no data" messages`);

    } else {
      console.log('❌ No RSVP/Tickets tab found in event details');

      // Look for any tabs that might contain RSVP data
      const allTabs = page.locator('[role="tab"], .tab, .nav-tab');
      const tabCount = await allTabs.count();
      console.log(`Found ${tabCount} total tabs`);

      for (let i = 0; i < tabCount; i++) {
        const tabText = await allTabs.nth(i).textContent();
        console.log(`Tab ${i}: "${tabText}"`);
      }
    }

    // Test API call for event participants/RSVPs
    const eventId = await page.url().split('/').pop(); // Extract event ID from URL
    console.log(`Current event ID: ${eventId}`);

    if (eventId && eventId !== 'events') {
      // Try different API endpoints for RSVP/participation data
      const endpoints = [
        `http://localhost:5655/api/admin/events/${eventId}/participants`,
        `http://localhost:5655/api/admin/events/${eventId}/rsvps`,
        `http://localhost:5655/api/admin/events/${eventId}/tickets`,
        `http://localhost:5655/api/events/${eventId}/participations`
      ];

      for (const endpoint of endpoints) {
        try {
          const response = await page.request.get(endpoint);
          const data = await response.json();
          console.log(`📊 ${endpoint} (${response.status()}):`, JSON.stringify(data, null, 2));
        } catch (error) {
          console.log(`❌ ${endpoint}: ${error.message}`);
        }
      }
    }

    // Check browser console for errors
    const consoleErrors = [];
    page.on('console', msg => {
      if (msg.type() === 'error') {
        consoleErrors.push(msg.text());
      }
    });

    // Take final screenshot
    await page.screenshot({ path: './test-results/10-rsvp-tab-final.png', fullPage: true });

    // Document findings
    const report = {
      test: 'Admin Event Details RSVP Tab',
      timestamp: new Date().toISOString(),
      eventId: eventId,
      rsvpTabFound: await rsvpTab.count() > 0,
      consoleErrors: consoleErrors,
      screenshots: [
        './test-results/07-admin-events-before-details.png',
        './test-results/08-event-details-page.png',
        './test-results/09-rsvp-tab-content.png',
        './test-results/10-rsvp-tab-final.png'
      ]
    };

    console.log('📋 RSVP Tab Report:', JSON.stringify(report, null, 2));
  });

  test('Check all available API endpoints for RSVP data', async ({ page }) => {
    console.log('🔍 Testing all possible RSVP API endpoints');

    // Get list of events first
    const eventsResponse = await page.request.get('http://localhost:5655/api/admin/events');
    const events = await eventsResponse.json();

    console.log(`📊 Found ${events?.length || 0} events to check for RSVP data`);

    if (events && events.length > 0) {
      const firstEvent = events[0];
      const eventId = firstEvent.id;

      console.log(`🎯 Testing RSVP endpoints for event: ${firstEvent.title} (ID: ${eventId})`);

      // Test all possible RSVP-related endpoints
      const endpointTests = [
        'api/admin/events/' + eventId + '/participants',
        'api/admin/events/' + eventId + '/rsvps',
        'api/admin/events/' + eventId + '/tickets',
        'api/admin/events/' + eventId + '/attendees',
        'api/events/' + eventId + '/participations',
        'api/events/' + eventId + '/rsvps',
        'api/participations?eventId=' + eventId,
        'api/rsvps?eventId=' + eventId
      ];

      const endpointResults = [];

      for (const endpoint of endpointTests) {
        try {
          const response = await page.request.get('http://localhost:5655/' + endpoint);
          const data = await response.json();

          endpointResults.push({
            endpoint: endpoint,
            status: response.status(),
            dataLength: Array.isArray(data) ? data.length : (data ? 1 : 0),
            response: data
          });

          console.log(`✅ ${endpoint}: ${response.status()} - ${Array.isArray(data) ? data.length + ' items' : 'object'}`);

        } catch (error) {
          endpointResults.push({
            endpoint: endpoint,
            status: 'ERROR',
            error: error.message
          });
          console.log(`❌ ${endpoint}: ${error.message}`);
        }
      }

      // Check what the UI is actually calling
      await page.goto('http://localhost:5173');
      await page.click('[data-testid="login-button"]');
      await page.waitForSelector('[data-testid="email-input"]');
      await page.fill('[data-testid="email-input"]', 'admin@witchcityrope.com');
      await page.fill('[data-testid="password-input"]', 'Test123!');
      await page.click('[data-testid="submit-login"]');

      // Monitor network requests
      const networkRequests = [];
      page.on('request', request => {
        if (request.url().includes('api/')) {
          networkRequests.push({
            url: request.url(),
            method: request.method()
          });
        }
      });

      // Navigate to event details to see what API calls are made
      await page.waitForSelector('[data-testid="admin-menu"]');
      await page.click('[data-testid="admin-menu"]');
      await page.click('[data-testid="admin-events-link"]');
      await page.waitForSelector('[data-testid="events-table"]');

      const firstEventRow = page.locator('[data-testid="event-row"]').first();
      await firstEventRow.click();
      await page.waitForSelector('[data-testid="event-details"]');

      // Wait for all API calls to complete
      await page.waitForTimeout(3000);

      console.log('📡 Network requests made by UI:');
      networkRequests.forEach(req => {
        console.log(`  ${req.method} ${req.url}`);
      });

      const report = {
        test: 'All RSVP API Endpoints',
        timestamp: new Date().toISOString(),
        testedEvent: {
          id: eventId,
          title: firstEvent.title
        },
        endpointResults: endpointResults,
        uiNetworkRequests: networkRequests
      };

      console.log('📋 API Endpoints Report:', JSON.stringify(report, null, 2));
    }
  });
});