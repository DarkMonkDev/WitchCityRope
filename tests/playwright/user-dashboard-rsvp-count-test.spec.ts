import { test, expect } from '@playwright/test';

test.describe('User Dashboard RSVP Count Issues', () => {

  test('User Dashboard shows "0 RSVP Social Events" for admin who has RSVPs', async ({ page }) => {
    console.log('🔍 Testing User Dashboard RSVP count display');

    // Login as admin (who should have RSVPs according to user report)
    await page.goto('http://localhost:5173');
    await page.click('[data-testid="login-button"]');
    await page.waitForSelector('[data-testid="email-input"]');
    await page.fill('[data-testid="email-input"]', 'admin@witchcityrope.com');
    await page.fill('[data-testid="password-input"]', 'Test123!');
    await page.click('[data-testid="submit-login"]');

    // Wait for dashboard to load
    await page.waitForSelector('[data-testid="dashboard"]', { timeout: 15000 });

    // Take screenshot of dashboard
    await page.screenshot({ path: './test-results/11-user-dashboard.png', fullPage: true });

    // Look for RSVP count displays
    const rsvpCountElements = page.locator('text=/\\d+ RSVP/, text=/RSVP.*\\d+/, [data-testid*="rsvp-count"], [data-testid*="social-events"]');
    const rsvpCount = await rsvpCountElements.count();

    console.log(`Found ${rsvpCount} RSVP count elements on dashboard`);

    // Extract all RSVP-related text
    const rsvpTexts = [];
    for (let i = 0; i < rsvpCount; i++) {
      const text = await rsvpCountElements.nth(i).textContent();
      rsvpTexts.push(text?.trim());
      console.log(`RSVP text ${i}: "${text}"`);
    }

    // Check specifically for "0 RSVP Social Events"
    const zeroRsvpText = page.locator('text=0 RSVP Social Events');
    const hasZeroRsvpText = await zeroRsvpText.count() > 0;

    console.log(`❌ Shows "0 RSVP Social Events": ${hasZeroRsvpText}`);

    // Check for any other RSVP-related sections
    const rsvpSections = page.locator('[data-testid*="rsvp"], [data-testid*="social"], .rsvp, .social-events');
    const rsvpSectionCount = await rsvpSections.count();

    console.log(`Found ${rsvpSectionCount} RSVP-related sections`);

    // Take detailed screenshot of RSVP area
    if (rsvpSectionCount > 0) {
      await rsvpSections.first().screenshot({ path: './test-results/12-rsvp-section-detail.png' });
    }

    // Test API calls to see what RSVP data exists for this user
    console.log('🔍 Checking API for user RSVP data...');

    const userApiEndpoints = [
      'http://localhost:5655/api/user/rsvps',
      'http://localhost:5655/api/user/events',
      'http://localhost:5655/api/user/participations',
      'http://localhost:5655/api/dashboard/events',
      'http://localhost:5655/api/dashboard/statistics',
      'http://localhost:5655/api/auth/user'
    ];

    const apiResults = [];

    for (const endpoint of userApiEndpoints) {
      try {
        const response = await page.request.get(endpoint);
        const data = await response.json();

        apiResults.push({
          endpoint: endpoint.replace('http://localhost:5655/', ''),
          status: response.status(),
          data: data
        });

        console.log(`📊 ${endpoint}: ${response.status()}`);
        console.log(`    Data:`, JSON.stringify(data, null, 2));

      } catch (error) {
        apiResults.push({
          endpoint: endpoint.replace('http://localhost:5655/', ''),
          status: 'ERROR',
          error: error.message
        });
        console.log(`❌ ${endpoint}: ${error.message}`);
      }
    }

    // Check if admin actually has any RSVPs in the database
    console.log('🔍 Checking raw database for admin RSVPs...');

    // Monitor network requests to see what the dashboard is actually calling
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

    // Refresh dashboard to capture network calls
    await page.reload();
    await page.waitForSelector('[data-testid="dashboard"]');
    await page.waitForTimeout(3000); // Wait for API calls

    console.log('📡 Dashboard network requests:');
    networkRequests.forEach(req => {
      console.log(`  ${req.method} ${req.url}`);
    });

    // Take final dashboard screenshot
    await page.screenshot({ path: './test-results/13-dashboard-final.png', fullPage: true });

    const report = {
      test: 'User Dashboard RSVP Count',
      timestamp: new Date().toISOString(),
      user: 'admin@witchcityrope.com',
      showsZeroRsvps: hasZeroRsvpText,
      rsvpTextsFound: rsvpTexts,
      rsvpSectionsCount: rsvpSectionCount,
      apiResults: apiResults,
      dashboardNetworkRequests: networkRequests,
      screenshots: [
        './test-results/11-user-dashboard.png',
        './test-results/12-rsvp-section-detail.png',
        './test-results/13-dashboard-final.png'
      ]
    };

    console.log('📋 Dashboard RSVP Count Report:', JSON.stringify(report, null, 2));
  });

  test('Compare dashboard data with actual RSVP records', async ({ page }) => {
    console.log('🔍 Comparing dashboard display with actual RSVP data');

    // First, make a test RSVP to ensure there's data to compare
    await page.goto('http://localhost:5173');
    await page.click('[data-testid="login-button"]');
    await page.waitForSelector('[data-testid="email-input"]');
    await page.fill('[data-testid="email-input"]', 'admin@witchcityrope.com');
    await page.fill('[data-testid="password-input"]', 'Test123!');
    await page.click('[data-testid="submit-login"]');

    // Navigate to public events to potentially make an RSVP
    await page.goto('http://localhost:5173/events');
    await page.waitForTimeout(2000);

    // Take screenshot of events page
    await page.screenshot({ path: './test-results/14-public-events-page.png', fullPage: true });

    // Look for social events to RSVP to
    const socialEvents = page.locator('[data-testid*="event"], .event-card').filter({ hasText: 'Social' });
    const socialEventCount = await socialEvents.count();

    console.log(`Found ${socialEventCount} social events on public page`);

    if (socialEventCount > 0) {
      // Click on first social event
      await socialEvents.first().click();
      await page.waitForTimeout(2000);

      // Take screenshot of event details
      await page.screenshot({ path: './test-results/15-social-event-details.png', fullPage: true });

      // Look for RSVP button
      const rsvpButton = page.locator('[data-testid*="rsvp"], button:has-text("RSVP"), button:has-text("Register")');

      if (await rsvpButton.count() > 0) {
        console.log('✅ Found RSVP button');

        // Check current RSVP status
        const rsvpStatus = await page.locator('[data-testid*="rsvp-status"], text=/Already registered/, text=/RSVP confirmed/').textContent();
        console.log(`Current RSVP status: ${rsvpStatus}`);

        // If not already RSVP'd, try to RSVP
        if (!rsvpStatus || !rsvpStatus.includes('Already')) {
          await rsvpButton.first().click();
          await page.waitForTimeout(2000);

          // Take screenshot after RSVP attempt
          await page.screenshot({ path: './test-results/16-after-rsvp-attempt.png', fullPage: true });
        }
      }
    }

    // Now go back to dashboard and check if count updated
    await page.goto('http://localhost:5173/dashboard');
    await page.waitForSelector('[data-testid="dashboard"]');

    // Take screenshot of dashboard after RSVP
    await page.screenshot({ path: './test-results/17-dashboard-after-rsvp.png', fullPage: true });

    // Get dashboard RSVP count again
    const dashboardRsvpCount = await page.locator('text=/\\d+ RSVP Social Events/').textContent();
    console.log(`Dashboard shows: "${dashboardRsvpCount}"`);

    // Get actual RSVP data from API
    const userRsvpResponse = await page.request.get('http://localhost:5655/api/user/rsvps');
    const userRsvpData = await userRsvpResponse.json();

    console.log('📊 User RSVP API data:', JSON.stringify(userRsvpData, null, 2));

    // Get all user events
    const userEventsResponse = await page.request.get('http://localhost:5655/api/user/events');
    const userEventsData = await userEventsResponse.json();

    console.log('📊 User Events API data:', JSON.stringify(userEventsData, null, 2));

    // Generate comparison report
    const comparison = {
      test: 'Dashboard vs Actual RSVP Data',
      timestamp: new Date().toISOString(),
      dashboardDisplay: dashboardRsvpCount,
      apiRsvpCount: Array.isArray(userRsvpData) ? userRsvpData.length : 0,
      apiEventsCount: Array.isArray(userEventsData) ? userEventsData.length : 0,
      discrepancy: {
        dashboardShowsZero: dashboardRsvpCount?.includes('0 RSVP') || false,
        apiHasData: (Array.isArray(userRsvpData) && userRsvpData.length > 0) || (Array.isArray(userEventsData) && userEventsData.length > 0)
      },
      rawApiData: {
        rsvps: userRsvpData,
        events: userEventsData
      }
    };

    console.log('📋 Dashboard vs API Comparison:', JSON.stringify(comparison, null, 2));
  });
});