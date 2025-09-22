import { test, expect } from '@playwright/test';

test.describe('API vs UI Data Discrepancy Analysis', () => {

  test('Compare API responses with UI display across all RSVP interfaces', async ({ page }) => {
    console.log('🔍 Comprehensive API vs UI data comparison');

    // Login as admin
    await page.goto('http://localhost:5173');
    await page.click('[data-testid="login-button"]');
    await page.waitForSelector('[data-testid="email-input"]');
    await page.fill('[data-testid="email-input"]', 'admin@witchcityrope.com');
    await page.fill('[data-testid="password-input"]', 'Test123!');
    await page.click('[data-testid="submit-login"]');

    // 1. GET ALL API DATA FIRST
    console.log('📊 Fetching all API data...');

    const apiData = {};

    // User-specific data
    try {
      const userResponse = await page.request.get('http://localhost:5655/api/auth/user');
      apiData.user = {
        status: userResponse.status(),
        data: await userResponse.json()
      };
    } catch (e) { apiData.user = { error: e.message }; }

    // User RSVPs
    try {
      const userRsvpsResponse = await page.request.get('http://localhost:5655/api/user/rsvps');
      apiData.userRsvps = {
        status: userRsvpsResponse.status(),
        data: await userRsvpsResponse.json()
      };
    } catch (e) { apiData.userRsvps = { error: e.message }; }

    // Dashboard data
    try {
      const dashboardResponse = await page.request.get('http://localhost:5655/api/dashboard/events');
      apiData.dashboard = {
        status: dashboardResponse.status(),
        data: await dashboardResponse.json()
      };
    } catch (e) { apiData.dashboard = { error: e.message }; }

    // Dashboard statistics
    try {
      const statsResponse = await page.request.get('http://localhost:5655/api/dashboard/statistics');
      apiData.statistics = {
        status: statsResponse.status(),
        data: await statsResponse.json()
      };
    } catch (e) { apiData.statistics = { error: e.message }; }

    // All events
    try {
      const eventsResponse = await page.request.get('http://localhost:5655/api/events');
      apiData.events = {
        status: eventsResponse.status(),
        data: await eventsResponse.json()
      };
    } catch (e) { apiData.events = { error: e.message }; }

    // Admin events
    try {
      const adminEventsResponse = await page.request.get('http://localhost:5655/api/admin/events');
      apiData.adminEvents = {
        status: adminEventsResponse.status(),
        data: await adminEventsResponse.json()
      };
    } catch (e) { apiData.adminEvents = { error: e.message }; }

    console.log('📊 API Data Summary:');
    Object.keys(apiData).forEach(key => {
      const data = apiData[key];
      if (data.error) {
        console.log(`  ${key}: ERROR - ${data.error}`);
      } else {
        const count = Array.isArray(data.data) ? data.data.length : (data.data ? 1 : 0);
        console.log(`  ${key}: ${data.status} - ${count} items`);
      }
    });

    // 2. CAPTURE UI DATA FROM EACH INTERFACE

    const uiData = {};

    // Dashboard UI data
    console.log('🖥️ Capturing Dashboard UI data...');
    await page.goto('http://localhost:5173/dashboard');
    await page.waitForSelector('[data-testid="dashboard"]');
    await page.screenshot({ path: './test-results/24-dashboard-ui.png', fullPage: true });

    uiData.dashboard = {
      rsvpCountText: await page.locator('text=/\\d+ RSVP/, text=/RSVP.*\\d+/').allTextContents(),
      eventCounts: await page.locator('[data-testid*="count"], [data-testid*="events"]').allTextContents(),
      sections: await page.locator('[data-testid*="section"], .dashboard-section').count()
    };

    // Public Events UI data
    console.log('🖥️ Capturing Public Events UI data...');
    await page.goto('http://localhost:5173/events');
    await page.waitForTimeout(2000);
    await page.screenshot({ path: './test-results/25-public-events-ui.png', fullPage: true });

    const publicEventCards = page.locator('[data-testid*="event"], .event-card');
    const publicEventCount = await publicEventCards.count();

    uiData.publicEvents = {
      eventsShown: publicEventCount,
      eventTitles: [],
      rsvpButtons: 0
    };

    for (let i = 0; i < Math.min(publicEventCount, 5); i++) {
      const title = await publicEventCards.nth(i).locator('h2, h3, [data-testid*="title"]').textContent();
      uiData.publicEvents.eventTitles.push(title?.trim());

      const hasRsvpButton = await publicEventCards.nth(i).locator('button:has-text("RSVP"), button:has-text("Register")').count() > 0;
      if (hasRsvpButton) uiData.publicEvents.rsvpButtons++;
    }

    // Admin Events UI data
    console.log('🖥️ Capturing Admin Events UI data...');
    await page.goto('http://localhost:5173');
    await page.waitForSelector('[data-testid="admin-menu"]');
    await page.click('[data-testid="admin-menu"]');
    await page.click('[data-testid="admin-events-link"]');
    await page.waitForSelector('[data-testid="events-table"]');
    await page.screenshot({ path: './test-results/26-admin-events-ui.png', fullPage: true });

    const adminEventRows = page.locator('[data-testid="event-row"]');
    const adminEventCount = await adminEventRows.count();

    uiData.adminEvents = {
      eventsShown: adminEventCount,
      capacityValues: [],
      eventTitles: []
    };

    for (let i = 0; i < Math.min(adminEventCount, 5); i++) {
      const row = adminEventRows.nth(i);
      const title = await row.locator('[data-testid*="title"], td:first-child').textContent();
      const capacity = await row.locator('[data-testid*="capacity"], td:has-text("0"), td:has-text("/")').textContent();

      uiData.adminEvents.eventTitles.push(title?.trim());
      uiData.adminEvents.capacityValues.push(capacity?.trim());
    }

    // 3. ANALYZE DISCREPANCIES

    console.log('🔍 Analyzing discrepancies...');

    const discrepancies = [];

    // Dashboard RSVP count vs API data
    const dashboardRsvpText = uiData.dashboard.rsvpCountText.join(' ');
    const apiRsvpCount = Array.isArray(apiData.userRsvps.data) ? apiData.userRsvps.data.length : 0;

    if (dashboardRsvpText.includes('0 RSVP') && apiRsvpCount > 0) {
      discrepancies.push({
        interface: 'Dashboard',
        issue: 'Shows 0 RSVPs but API has data',
        uiValue: dashboardRsvpText,
        apiValue: `${apiRsvpCount} RSVPs`
      });
    }

    // Public events count vs API
    const apiPublicEventCount = Array.isArray(apiData.events.data) ? apiData.events.data.length : 0;
    if (uiData.publicEvents.eventsShown !== apiPublicEventCount) {
      discrepancies.push({
        interface: 'Public Events',
        issue: 'Event count mismatch',
        uiValue: `${uiData.publicEvents.eventsShown} events shown`,
        apiValue: `${apiPublicEventCount} events in API`
      });
    }

    // Admin events capacity display
    const hasZeroCapacities = uiData.adminEvents.capacityValues.some(cap => cap === '0' || cap === '0/0');
    const apiHasEventData = Array.isArray(apiData.adminEvents.data) && apiData.adminEvents.data.length > 0;

    if (hasZeroCapacities && apiHasEventData) {
      discrepancies.push({
        interface: 'Admin Events',
        issue: 'Capacity shows 0 but events exist',
        uiValue: uiData.adminEvents.capacityValues,
        apiValue: `${apiData.adminEvents.data.length} admin events`
      });
    }

    // Take final comparison screenshot
    await page.screenshot({ path: './test-results/27-final-comparison.png', fullPage: true });

    // 4. GENERATE COMPREHENSIVE REPORT

    const comprehensiveReport = {
      test: 'Comprehensive API vs UI Data Analysis',
      timestamp: new Date().toISOString(),
      summary: {
        totalDiscrepancies: discrepancies.length,
        apiEndpointsTestend: Object.keys(apiData).length,
        uiInterfacesTested: Object.keys(uiData).length
      },
      apiData: apiData,
      uiData: uiData,
      discrepancies: discrepancies,
      recommendations: [
        'Check if UI components are calling correct API endpoints',
        'Verify data transformation between API and UI',
        'Check for caching issues preventing UI updates',
        'Validate authentication state for API calls'
      ],
      screenshots: [
        './test-results/24-dashboard-ui.png',
        './test-results/25-public-events-ui.png',
        './test-results/26-admin-events-ui.png',
        './test-results/27-final-comparison.png'
      ]
    };

    console.log('📋 COMPREHENSIVE REPORT:');
    console.log(JSON.stringify(comprehensiveReport, null, 2));

    console.log('\n🚨 DISCREPANCIES FOUND:');
    if (discrepancies.length === 0) {
      console.log('✅ No major discrepancies detected between API and UI');
    } else {
      discrepancies.forEach((disc, index) => {
        console.log(`${index + 1}. ${disc.interface}: ${disc.issue}`);
        console.log(`   UI: ${disc.uiValue}`);
        console.log(`   API: ${disc.apiValue}`);
      });
    }
  });

  test('Test specific RSVP workflow API calls vs UI updates', async ({ page }) => {
    console.log('🔍 Testing RSVP workflow API calls vs UI updates');

    // Monitor all network requests
    const networkRequests = [];
    page.on('request', request => {
      if (request.url().includes('api/')) {
        networkRequests.push({
          url: request.url(),
          method: request.method(),
          timestamp: new Date().toISOString()
        });
      }
    });

    // Monitor API responses
    const apiResponses = [];
    page.on('response', response => {
      if (response.url().includes('api/')) {
        apiResponses.push({
          url: response.url(),
          status: response.status(),
          timestamp: new Date().toISOString()
        });
      }
    });

    // Login
    await page.goto('http://localhost:5173');
    await page.click('[data-testid="login-button"]');
    await page.waitForSelector('[data-testid="email-input"]');
    await page.fill('[data-testid="email-input"]', 'admin@witchcityrope.com');
    await page.fill('[data-testid="password-input"]', 'Test123!');
    await page.click('[data-testid="submit-login"]');

    // Clear previous network data
    networkRequests.length = 0;
    apiResponses.length = 0;

    // Navigate to events and attempt RSVP workflow
    await page.goto('http://localhost:5173/events');
    await page.waitForTimeout(3000);

    console.log('📡 Network requests after loading events:');
    networkRequests.forEach(req => console.log(`  ${req.method} ${req.url}`));

    // Try to click on an event and RSVP
    const eventCards = page.locator('[data-testid*="event"], .event-card');
    const eventCount = await eventCards.count();

    if (eventCount > 0) {
      // Click first event
      await eventCards.first().click();
      await page.waitForTimeout(3000);

      console.log('📡 Network requests after clicking event:');
      const newRequests = networkRequests.slice(-10); // Last 10 requests
      newRequests.forEach(req => console.log(`  ${req.method} ${req.url}`));

      // Look for RSVP button and click if found
      const rsvpButton = page.locator('button:has-text("RSVP"), button:has-text("Register")');
      if (await rsvpButton.count() > 0) {
        await rsvpButton.first().click();
        await page.waitForTimeout(3000);

        console.log('📡 Network requests after RSVP click:');
        const rsvpRequests = networkRequests.slice(-5); // Last 5 requests
        rsvpRequests.forEach(req => console.log(`  ${req.method} ${req.url}`));
      }
    }

    // Go to dashboard and see what API calls are made
    await page.goto('http://localhost:5173/dashboard');
    await page.waitForTimeout(3000);

    console.log('📡 Network requests for dashboard load:');
    const dashboardRequests = networkRequests.filter(req =>
      req.timestamp > new Date(Date.now() - 5000).toISOString()
    );
    dashboardRequests.forEach(req => console.log(`  ${req.method} ${req.url}`));

    // Generate workflow report
    const workflowReport = {
      test: 'RSVP Workflow API Monitoring',
      timestamp: new Date().toISOString(),
      totalNetworkRequests: networkRequests.length,
      totalApiResponses: apiResponses.length,
      eventsPageRequests: networkRequests.filter(req => req.url.includes('/events')),
      rsvpRequests: networkRequests.filter(req =>
        req.url.includes('rsvp') || req.url.includes('register') || req.url.includes('participate')
      ),
      dashboardRequests: dashboardRequests,
      failedResponses: apiResponses.filter(resp => resp.status >= 400),
      fullRequestLog: networkRequests,
      fullResponseLog: apiResponses
    };

    console.log('📋 RSVP Workflow Report:');
    console.log(JSON.stringify(workflowReport, null, 2));
  });
});