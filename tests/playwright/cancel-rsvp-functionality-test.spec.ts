import { test, expect } from '@playwright/test';

test.describe('Cancel RSVP Button Functionality Issues', () => {

  test('Cancel RSVP Button does nothing when clicked', async ({ page }) => {
    console.log('🔍 Testing Cancel RSVP button functionality');

    // Login as admin
    await page.goto('http://localhost:5173');
    await page.click('[data-testid="login-button"]');
    await page.waitForSelector('[data-testid="email-input"]');
    await page.fill('[data-testid="email-input"]', 'admin@witchcityrope.com');
    await page.fill('[data-testid="password-input"]', 'Test123!');
    await page.click('[data-testid="submit-login"]');

    // Navigate to events page to find an event to RSVP to
    await page.goto('http://localhost:5173/events');
    await page.waitForTimeout(2000);

    // Take screenshot of events page
    await page.screenshot({ path: './test-results/18-events-page-for-rsvp.png', fullPage: true });

    // Look for events that allow RSVP
    const eventCards = page.locator('[data-testid*="event"], .event-card');
    const eventCount = await eventCards.count();

    console.log(`Found ${eventCount} events on events page`);

    let rsvpMade = false;
    let eventWithRsvp = null;

    // Try to find an event we can RSVP to
    for (let i = 0; i < Math.min(eventCount, 3); i++) {
      const eventCard = eventCards.nth(i);
      const eventTitle = await eventCard.locator('[data-testid*="title"], h2, h3').textContent();

      console.log(`Checking event ${i}: "${eventTitle}"`);

      // Click on the event
      await eventCard.click();
      await page.waitForTimeout(2000);

      // Take screenshot of event details
      await page.screenshot({ path: `./test-results/19-event-${i}-details.png`, fullPage: true });

      // Look for RSVP button
      const rsvpButton = page.locator('[data-testid*="rsvp"], button:has-text("RSVP"), button:has-text("Register"), button:has-text("Join")');

      if (await rsvpButton.count() > 0) {
        console.log(`✅ Found RSVP button for event: ${eventTitle}`);

        // Check if already RSVP'd
        const alreadyRsvpd = await page.locator('text=/Already registered/, text=/RSVP confirmed/, text=/You are registered/').count() > 0;

        if (!alreadyRsvpd) {
          // Make RSVP
          await rsvpButton.first().click();
          await page.waitForTimeout(3000);

          // Take screenshot after RSVP
          await page.screenshot({ path: `./test-results/20-after-rsvp-event-${i}.png`, fullPage: true });

          rsvpMade = true;
          eventWithRsvp = { title: eventTitle, index: i };
          break;
        } else {
          console.log(`Already RSVP'd to event: ${eventTitle}`);
          eventWithRsvp = { title: eventTitle, index: i };
          rsvpMade = true;
          break;
        }
      }

      // Go back to events list
      await page.goto('http://localhost:5173/events');
      await page.waitForTimeout(1000);
    }

    if (!rsvpMade) {
      console.log('❌ Could not find any event to RSVP to');

      // Check if user already has RSVPs we can cancel
      await page.goto('http://localhost:5173/dashboard');
      await page.waitForTimeout(2000);

      // Take screenshot of dashboard to see current RSVPs
      await page.screenshot({ path: './test-results/21-dashboard-existing-rsvps.png', fullPage: true });

      // Look for existing RSVP items with cancel buttons
      const existingRsvps = page.locator('[data-testid*="rsvp"], [data-testid*="registration"]');
      const existingRsvpCount = await existingRsvps.count();

      console.log(`Found ${existingRsvpCount} existing RSVP items on dashboard`);

      if (existingRsvpCount > 0) {
        eventWithRsvp = { title: 'Existing RSVP', index: 0 };
        rsvpMade = true;
      }
    }

    if (rsvpMade && eventWithRsvp) {
      console.log(`🎯 Testing cancel functionality for: ${eventWithRsvp.title}`);

      // Look for cancel RSVP button
      const cancelButtons = page.locator('[data-testid*="cancel"], button:has-text("Cancel"), button:has-text("Unregister"), button:has-text("Remove")');
      const cancelButtonCount = await cancelButtons.count();

      console.log(`Found ${cancelButtonCount} potential cancel buttons`);

      if (cancelButtonCount > 0) {
        // Take screenshot before cancel attempt
        await page.screenshot({ path: './test-results/22-before-cancel-attempt.png', fullPage: true });

        // Get current page state for comparison
        const beforeCancelUrl = page.url();
        const beforeCancelContent = await page.content();

        // Monitor network requests
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

        // Monitor console messages
        const consoleMessages = [];
        page.on('console', msg => {
          consoleMessages.push({
            type: msg.type(),
            text: msg.text(),
            timestamp: new Date().toISOString()
          });
        });

        // Click cancel button
        console.log('🔄 Clicking cancel RSVP button...');
        await cancelButtons.first().click();

        // Wait for any potential response
        await page.waitForTimeout(5000);

        // Take screenshot after cancel attempt
        await page.screenshot({ path: './test-results/23-after-cancel-attempt.png', fullPage: true });

        // Check if anything changed
        const afterCancelUrl = page.url();
        const afterCancelContent = await page.content();

        const urlChanged = beforeCancelUrl !== afterCancelUrl;
        const contentChanged = beforeCancelContent !== afterCancelContent;

        console.log(`URL changed: ${urlChanged} (${beforeCancelUrl} -> ${afterCancelUrl})`);
        console.log(`Content changed: ${contentChanged}`);
        console.log(`Network requests made: ${networkRequests.length}`);
        console.log(`Console messages: ${consoleMessages.length}`);

        // Log network requests
        if (networkRequests.length > 0) {
          console.log('📡 Cancel button network requests:');
          networkRequests.forEach(req => {
            console.log(`  ${req.method} ${req.url}`);
          });
        }

        // Log console messages
        if (consoleMessages.length > 0) {
          console.log('💬 Console messages during cancel:');
          consoleMessages.forEach(msg => {
            console.log(`  ${msg.type}: ${msg.text}`);
          });
        }

        // Check if RSVP still appears to be active
        const stillHasRsvp = await page.locator('text=/Already registered/, text=/RSVP confirmed/, text=/You are registered/').count() > 0;

        console.log(`RSVP still appears active: ${stillHasRsvp}`);

        // Test direct API call for cancel
        console.log('🔍 Testing direct API cancel call...');

        // Try to determine the RSVP/event ID from the current context
        const currentUrl = page.url();
        const eventIdMatch = currentUrl.match(/events\/([^\/\?]+)/);
        const eventId = eventIdMatch ? eventIdMatch[1] : null;

        if (eventId) {
          console.log(`Attempting API cancel for event ID: ${eventId}`);

          const cancelEndpoints = [
            `http://localhost:5655/api/events/${eventId}/cancel-rsvp`,
            `http://localhost:5655/api/rsvps/${eventId}/cancel`,
            `http://localhost:5655/api/user/rsvps/${eventId}`,
            `http://localhost:5655/api/participations/${eventId}/cancel`
          ];

          for (const endpoint of cancelEndpoints) {
            try {
              const deleteResponse = await page.request.delete(endpoint);
              console.log(`DELETE ${endpoint}: ${deleteResponse.status()}`);

              const postResponse = await page.request.post(endpoint);
              console.log(`POST ${endpoint}: ${postResponse.status()}`);

            } catch (error) {
              console.log(`❌ ${endpoint}: ${error.message}`);
            }
          }
        }

        const report = {
          test: 'Cancel RSVP Button Functionality',
          timestamp: new Date().toISOString(),
          event: eventWithRsvp,
          cancelButtonFound: cancelButtonCount > 0,
          buttonClickResults: {
            urlChanged: urlChanged,
            contentChanged: contentChanged,
            networkRequestsCount: networkRequests.length,
            consoleMessagesCount: consoleMessages.length
          },
          networkRequests: networkRequests,
          consoleMessages: consoleMessages,
          rsvpStillActive: stillHasRsvp,
          screenshots: [
            './test-results/18-events-page-for-rsvp.png',
            './test-results/22-before-cancel-attempt.png',
            './test-results/23-after-cancel-attempt.png'
          ]
        };

        console.log('📋 Cancel RSVP Report:', JSON.stringify(report, null, 2));

      } else {
        console.log('❌ No cancel RSVP button found');

        const report = {
          test: 'Cancel RSVP Button Functionality',
          timestamp: new Date().toISOString(),
          event: eventWithRsvp,
          cancelButtonFound: false,
          issue: 'No cancel button found despite having RSVP'
        };

        console.log('📋 Cancel RSVP Report:', JSON.stringify(report, null, 2));
      }

    } else {
      console.log('❌ Could not establish RSVP state to test cancel functionality');
    }
  });

  test('Test all possible cancel RSVP API endpoints', async ({ page }) => {
    console.log('🔍 Testing all possible cancel RSVP API endpoints');

    // Login first
    await page.goto('http://localhost:5173');
    await page.click('[data-testid="login-button"]');
    await page.waitForSelector('[data-testid="email-input"]');
    await page.fill('[data-testid="email-input"]', 'admin@witchcityrope.com');
    await page.fill('[data-testid="password-input"]', 'Test123!');
    await page.click('[data-testid="submit-login"]');

    // Get current user RSVPs/events
    const userRsvpsResponse = await page.request.get('http://localhost:5655/api/user/rsvps');
    const userRsvps = await userRsvpsResponse.json();

    console.log(`📊 User has ${Array.isArray(userRsvps) ? userRsvps.length : 0} RSVPs`);

    // Get all events to test cancel endpoints
    const eventsResponse = await page.request.get('http://localhost:5655/api/events');
    const events = await eventsResponse.json();

    if (events && events.length > 0) {
      const testEventId = events[0].id;
      console.log(`🎯 Testing cancel endpoints with event ID: ${testEventId}`);

      const cancelEndpointTests = [
        // DELETE methods
        { method: 'DELETE', url: `api/events/${testEventId}/rsvp` },
        { method: 'DELETE', url: `api/events/${testEventId}/cancel-rsvp` },
        { method: 'DELETE', url: `api/rsvps/${testEventId}` },
        { method: 'DELETE', url: `api/user/rsvps/${testEventId}` },
        { method: 'DELETE', url: `api/participations/${testEventId}` },

        // POST methods (some APIs use POST for cancel)
        { method: 'POST', url: `api/events/${testEventId}/cancel-rsvp` },
        { method: 'POST', url: `api/rsvps/${testEventId}/cancel` },
        { method: 'POST', url: `api/user/cancel-rsvp/${testEventId}` },

        // PUT methods
        { method: 'PUT', url: `api/events/${testEventId}/rsvp/cancel` },
        { method: 'PUT', url: `api/user/rsvps/${testEventId}/status` }
      ];

      const endpointResults = [];

      for (const endpoint of cancelEndpointTests) {
        try {
          let response;
          const fullUrl = 'http://localhost:5655/' + endpoint.url;

          switch (endpoint.method) {
            case 'DELETE':
              response = await page.request.delete(fullUrl);
              break;
            case 'POST':
              response = await page.request.post(fullUrl, {
                data: { action: 'cancel', status: 'cancelled' }
              });
              break;
            case 'PUT':
              response = await page.request.put(fullUrl, {
                data: { status: 'cancelled' }
              });
              break;
          }

          const responseText = await response.text();

          endpointResults.push({
            method: endpoint.method,
            endpoint: endpoint.url,
            status: response.status(),
            response: responseText
          });

          console.log(`${endpoint.method} ${endpoint.url}: ${response.status()}`);

        } catch (error) {
          endpointResults.push({
            method: endpoint.method,
            endpoint: endpoint.url,
            status: 'ERROR',
            error: error.message
          });

          console.log(`❌ ${endpoint.method} ${endpoint.url}: ${error.message}`);
        }
      }

      const report = {
        test: 'Cancel RSVP API Endpoints',
        timestamp: new Date().toISOString(),
        testEventId: testEventId,
        userRsvpCount: Array.isArray(userRsvps) ? userRsvps.length : 0,
        endpointResults: endpointResults
      };

      console.log('📋 Cancel API Endpoints Report:', JSON.stringify(report, null, 2));
    }
  });
});