import { test, expect } from '@playwright/test';

test.describe('RSVP Working Investigation', () => {
  test('1. Check Event Data on Public Events Page', async ({ page }) => {
    console.log('🔍 Starting RSVP investigation...');

    // Navigate to events page first
    await page.goto('http://localhost:5173/events');

    // Wait for events to load
    await page.waitForLoadState('networkidle');

    // Take screenshot for evidence
    await page.screenshot({ path: 'test-results/events-page-load.png', fullPage: true });

    // Look for event cards or event data
    const eventElements = await page.locator('[data-testid*="event"], .event-card, article').all();
    console.log(`📋 Found ${eventElements.length} potential event elements`);

    if (eventElements.length > 0) {
      // Check each event for RSVP/participant information
      for (let i = 0; i < Math.min(eventElements.length, 5); i++) {
        const element = eventElements[i];
        const text = await element.textContent();
        console.log(`📝 Event ${i + 1} text: ${text?.substring(0, 200)}...`);
      }
    }

    // Look for capacity/participant information patterns
    const pageText = await page.textContent('body');
    const capacityMatches = pageText?.match(/\d+\/\d+/g) || [];
    console.log(`🎯 Found ${capacityMatches.length} capacity patterns: ${capacityMatches.slice(0, 5).join(', ')}`);

    // Check if all show zero participants
    const zeroParticipants = capacityMatches.filter(match => match.startsWith('0/'));
    console.log(`📊 Events with zero participants: ${zeroParticipants.length}/${capacityMatches.length}`);

    if (zeroParticipants.length === capacityMatches.length && capacityMatches.length > 0) {
      console.log('⚠️ ALL events show zero participants - this matches the user report');
    }
  });

  test('2. Attempt Admin Login and Check Admin Interface', async ({ page }) => {
    console.log('🔐 Testing admin login...');

    // Navigate to home page
    await page.goto('http://localhost:5173/');

    // Click LOGIN button
    await page.locator('[data-testid="login-button"]').click();

    // Fill login form
    await page.locator('[data-testid="email-input"]').fill('admin@witchcityrope.com');
    await page.locator('[data-testid="password-input"]').fill('Test123!');

    // Submit login
    await page.locator('[data-testid="sign-in-button"]').click();

    // Wait for navigation
    await page.waitForURL('**/dashboard');
    await page.waitForLoadState('networkidle');

    console.log('✅ Successfully logged in as admin');

    // Take screenshot of admin dashboard
    await page.screenshot({ path: 'test-results/admin-dashboard.png', fullPage: true });

    // Look for admin-specific RSVP management features
    const adminText = await page.textContent('body');
    console.log(`📋 Admin dashboard content preview: ${adminText?.substring(0, 300)}...`);

    // Check for RSVP-related admin features
    const rsvpKeywords = ['rsvp', 'participant', 'attendance', 'registration'];
    const foundKeywords = rsvpKeywords.filter(keyword =>
      adminText?.toLowerCase().includes(keyword)
    );
    console.log(`🎯 Found RSVP-related keywords: ${foundKeywords.join(', ')}`);
  });

  test('3. Check API Endpoints for RSVP Data', async ({ page }) => {
    console.log('🌐 Testing API endpoints...');

    // Login first to get authentication
    await page.goto('http://localhost:5173/');
    await page.locator('[data-testid="login-button"]').click();
    await page.locator('[data-testid="email-input"]').fill('admin@witchcityrope.com');
    await page.locator('[data-testid="password-input"]').fill('Test123!');
    await page.locator('[data-testid="sign-in-button"]').click();
    await page.waitForURL('**/dashboard');

    // Test events API endpoint
    const eventsResponse = await page.request.get('http://localhost:5655/api/events');
    console.log(`📡 Events API status: ${eventsResponse.status()}`);

    if (eventsResponse.ok()) {
      const eventsData = await eventsResponse.json();
      console.log(`📋 Events API returned ${eventsData?.length || 0} events`);

      if (eventsData?.length > 0) {
        const firstEvent = eventsData[0];
        console.log(`🎯 First event structure: ${JSON.stringify(firstEvent, null, 2).substring(0, 500)}...`);

        // Check for RSVP/participant fields
        const hasRsvpFields = ['participants', 'rsvps', 'attendees', 'registrations'].some(field =>
          firstEvent.hasOwnProperty(field)
        );
        console.log(`📊 Event has RSVP-related fields: ${hasRsvpFields}`);
      }
    }

    // Test dashboard events endpoint
    const dashboardResponse = await page.request.get('http://localhost:5655/api/dashboard/events?count=3');
    console.log(`📡 Dashboard API status: ${dashboardResponse.status()}`);

    if (dashboardResponse.ok()) {
      const dashboardData = await dashboardResponse.json();
      console.log(`📋 Dashboard API preview: ${JSON.stringify(dashboardData, null, 2).substring(0, 300)}...`);
    }
  });
});