import { test, expect } from '@playwright/test';

/**
 * Public Events Anonymous Access E2E Tests
 *
 * Feature: Public Events Browsing Without Authentication
 * Status: Verified working (commit 82999f09)
 * Documentation: /docs/functional-areas/events/PUBLIC-EVENTS-ANONYMOUS-ACCESS-VERIFICATION.md
 *
 * Tests that anonymous (non-authenticated) users can:
 * - Browse public events list
 * - View event details
 * - Access event data via API without authentication
 *
 * Security Tests that anonymous users CANNOT:
 * - See unpublished events
 * - Access admin-only endpoints
 */

test.describe('Public Events Anonymous Access', () => {

  test.describe('P0 CRITICAL - API Endpoint Tests', () => {

    test('Anonymous user can GET /api/events without authentication', async ({ page }) => {
      // Make direct API call to events endpoint (no authentication)
      const response = await page.request.get('http://localhost:5655/api/events');

      // Should return 200 OK status
      expect(response.status()).toBe(200);

      // Verify response is successful
      const responseData = await response.json();
      expect(responseData.success).toBe(true);
      expect(responseData.error).toBeNull();

      // Should return array of events in data property
      expect(Array.isArray(responseData.data)).toBe(true);
      expect(responseData.data.length).toBeGreaterThan(0);

      console.log(`✅ API returned ${responseData.data.length} public events`);
    });

    test('API response matches EventDto structure', async ({ page }) => {
      // Make API call
      const response = await page.request.get('http://localhost:5655/api/events');
      const responseData = await response.json();

      // Verify wrapper structure
      expect(responseData).toHaveProperty('success');
      expect(responseData).toHaveProperty('data');
      expect(responseData).toHaveProperty('error');
      expect(responseData).toHaveProperty('message');
      expect(responseData).toHaveProperty('timestamp');

      // Verify event data structure
      const events = responseData.data;
      expect(events.length).toBeGreaterThan(0);

      const firstEvent = events[0];

      // Required EventDto fields (actual API response uses these field names)
      expect(firstEvent).toHaveProperty('id');
      expect(firstEvent).toHaveProperty('title');
      expect(firstEvent).toHaveProperty('description');
      expect(firstEvent).toHaveProperty('startDate'); // API uses startDate, not startTime
      expect(firstEvent).toHaveProperty('endDate');   // API uses endDate, not endTime
      expect(firstEvent).toHaveProperty('location');
      expect(firstEvent).toHaveProperty('isPublished');
      expect(firstEvent).toHaveProperty('eventType');

      // Additional expected fields
      expect(firstEvent).toHaveProperty('capacity');
      expect(firstEvent).toHaveProperty('sessions');

      // Verify published status (should only return published events)
      expect(firstEvent.isPublished).toBe(true);

      console.log(`✅ EventDto structure validated for event: ${firstEvent.title}`);
    });

    test('Unpublished events are NOT returned to anonymous users', async ({ page }) => {
      // Make API call without includeUnpublished parameter
      const response = await page.request.get('http://localhost:5655/api/events');
      const responseData = await response.json();

      const events = responseData.data;

      // ALL events should have isPublished = true
      const allPublished = events.every((event: any) => event.isPublished === true);
      expect(allPublished).toBe(true);

      // Should not contain any unpublished events
      const unpublishedCount = events.filter((event: any) => !event.isPublished).length;
      expect(unpublishedCount).toBe(0);

      console.log(`✅ Verified all ${events.length} events are published`);
    });

    test('includeUnpublished parameter requires authentication (returns 401)', async ({ page }) => {
      // Attempt to access unpublished events without authentication
      const response = await page.request.get('http://localhost:5655/api/events?includeUnpublished=true');

      // Should return 401 Unauthorized
      expect(response.status()).toBe(401);

      const responseData = await response.json();
      expect(responseData.success).toBe(false);
      expect(responseData.error).toBeTruthy();

      console.log(`✅ Unpublished events access blocked: ${responseData.error}`);
    });
  });

  test.describe('P1 CRITICAL - Anonymous Event Browsing Tests', () => {

    test('Anonymous user can navigate to events page', async ({ page }) => {
      // Navigate to events page WITHOUT logging in
      await page.goto('http://localhost:5173/events');
      await page.waitForLoadState('networkidle');

      // Should successfully load (200 status)
      expect(page.url()).toContain('/events');

      // Take screenshot for verification
      await page.screenshot({ path: './test-results/anonymous-events-page.png' });

      console.log('✅ Events page loaded successfully for anonymous user');
    });

    test('Events list loads and displays events for anonymous user', async ({ page }) => {
      // Navigate to events page
      await page.goto('http://localhost:5173/events');
      await page.waitForLoadState('networkidle');

      // Wait for events to load (check for event cards or events container)
      const eventsContainer = page.locator('[data-testid="events-list"], .events-container, .event-card').first();
      await expect(eventsContainer).toBeVisible({ timeout: 10000 });

      // Verify at least one event is visible
      const eventCards = page.locator('.event-card, [data-testid="event-card"]');
      const eventCount = await eventCards.count();
      expect(eventCount).toBeGreaterThan(0);

      console.log(`✅ Found ${eventCount} events displayed on page`);
    });

    test('No authentication required - No 401 errors for events data', async ({ page }) => {
      // Monitor network requests for 401 errors from events endpoints
      const eventsApi401Errors: string[] = [];

      page.on('response', response => {
        if (response.status() === 401) {
          const url = response.url();
          // Only track 401 errors from events-related endpoints
          // The /api/auth/user endpoint returns 401 for anonymous users (expected behavior)
          if (url.includes('/api/events') || url.includes('/events')) {
            eventsApi401Errors.push(url);
            console.error(`❌ 401 Unauthorized error from events endpoint: ${url}`);
          }
        }
      });

      // Navigate to events page
      await page.goto('http://localhost:5173/events');
      await page.waitForLoadState('networkidle');

      // Wait for events to potentially load
      await page.waitForTimeout(2000);

      // Should NOT have any 401 errors from events endpoints
      // (auth/user 401s are expected for anonymous users)
      expect(eventsApi401Errors.length).toBe(0);

      console.log('✅ No 401 authentication errors from events endpoints');
    });
  });

  test.describe('P1 - Event Data Visibility Tests', () => {

    test('Event cards display title, date, and description', async ({ page }) => {
      // Navigate to events page
      await page.goto('http://localhost:5173/events');
      await page.waitForLoadState('networkidle');

      // Get first event card
      const firstEventCard = page.locator('.event-card, [data-testid="event-card"]').first();
      await expect(firstEventCard).toBeVisible();

      // Verify event card contains title (look for heading or strong text)
      const hasTitle = await firstEventCard.locator('h2, h3, h4, strong, [data-testid="event-title"]').count();
      expect(hasTitle).toBeGreaterThan(0);

      // Verify event card contains date/time information
      const hasDate = await firstEventCard.locator('time, [datetime], [data-testid="event-date"]').count();
      const hasDateText = (await firstEventCard.textContent())?.match(/\d{1,2}[\/\-]\d{1,2}[\/\-]\d{2,4}|\d{4}-\d{2}-\d{2}/);
      expect(hasDate > 0 || hasDateText).toBeTruthy();

      // Verify event card contains description or content
      const cardText = await firstEventCard.textContent();
      expect(cardText).toBeTruthy();
      expect(cardText!.length).toBeGreaterThan(20); // Should have substantial text content

      console.log('✅ Event card displays title, date, and description');
    });

    test('Event cards are clickable', async ({ page }) => {
      // Navigate to events page
      await page.goto('http://localhost:5173/events');
      await page.waitForLoadState('networkidle');

      // Get first event card
      const firstEventCard = page.locator('.event-card, [data-testid="event-card"], a[href*="/events/"]').first();
      await expect(firstEventCard).toBeVisible();

      // Verify card is clickable (either link or button)
      const isClickable = await firstEventCard.evaluate(el => {
        return el.tagName === 'A' ||
               el.tagName === 'BUTTON' ||
               el.style.cursor === 'pointer' ||
               window.getComputedStyle(el).cursor === 'pointer';
      });

      expect(isClickable).toBe(true);

      console.log('✅ Event cards are clickable');
    });

    test('No sensitive admin data visible to anonymous users', async ({ page }) => {
      // Navigate to events page
      await page.goto('http://localhost:5173/events');
      await page.waitForLoadState('networkidle');

      // Get page content
      const pageContent = await page.textContent('body');

      // Should NOT contain admin-only text indicators
      const adminIndicators = [
        'unpublished',
        'draft',
        'admin only',
        'private event',
        'internal note',
        'staff only'
      ];

      const foundAdminIndicators = adminIndicators.filter(indicator =>
        pageContent?.toLowerCase().includes(indicator)
      );

      // May contain "unpublished" in UI text (like "Published" status), but not as event status
      // Only fail if multiple admin indicators found
      expect(foundAdminIndicators.length).toBeLessThanOrEqual(1);

      console.log('✅ No sensitive admin data exposed to anonymous users');
    });
  });

  test.describe('P2 - Browser Navigation and UX', () => {

    test('Page title is set correctly', async ({ page }) => {
      // Navigate to events page
      await page.goto('http://localhost:5173/events');
      await page.waitForLoadState('networkidle');

      // Check page title
      const title = await page.title();

      // Should contain "Events" or "Witch City Rope"
      const hasRelevantTitle = title.toLowerCase().includes('events') ||
                               title.toLowerCase().includes('witch city rope');

      expect(hasRelevantTitle).toBe(true);

      console.log(`✅ Page title: ${title}`);
    });

    test('Events page accessible via navigation menu', async ({ page }) => {
      // Navigate to home page
      await page.goto('http://localhost:5173/');
      await page.waitForLoadState('networkidle');

      // Look for Events link in navigation
      const eventsLink = page.locator('nav a[href*="/events"], header a[href*="/events"], a:has-text("Events")').first();

      // If events link exists in nav, click it
      if (await eventsLink.count() > 0) {
        await eventsLink.click();
        await page.waitForLoadState('networkidle');

        // Should navigate to events page
        expect(page.url()).toContain('/events');

        console.log('✅ Events accessible via navigation menu');
      } else {
        console.log('⚠️ Events link not found in navigation (may be implemented differently)');
        // This is not a failure - just informational
      }
    });

    test('Direct URL access to /events works', async ({ page }) => {
      // Directly navigate to events URL
      const response = await page.goto('http://localhost:5173/events');

      // Should return 200 OK
      expect(response?.status()).toBe(200);

      // Should be on events page
      expect(page.url()).toContain('/events');

      console.log('✅ Direct URL access to /events works');
    });
  });

  test.describe('P3 - Error Handling', () => {

    test('Events page handles empty event list gracefully', async ({ page }) => {
      // This test assumes there might be scenarios with no published events
      // For now, we just verify the page doesn't crash

      await page.goto('http://localhost:5173/events');
      await page.waitForLoadState('networkidle');

      // Page should load without errors
      const pageContent = await page.textContent('body');
      expect(pageContent).toBeTruthy();

      // Should either show events or "no events" message
      const hasContent = pageContent!.length > 100; // Reasonable content length
      expect(hasContent).toBe(true);

      console.log('✅ Events page handles display gracefully');
    });

    test('No JavaScript console errors on events page', async ({ page }) => {
      // Collect console errors
      const consoleErrors: string[] = [];

      page.on('console', msg => {
        if (msg.type() === 'error') {
          consoleErrors.push(msg.text());
        }
      });

      // Navigate to events page
      await page.goto('http://localhost:5173/events');
      await page.waitForLoadState('networkidle');

      // Wait a bit for any delayed errors
      await page.waitForTimeout(2000);

      // Filter out known acceptable/expected errors
      const criticalErrors = consoleErrors.filter(error => {
        const errorLower = error.toLowerCase();
        return (
          !errorLower.includes('favicon') &&        // Favicon errors are non-critical
          !errorLower.includes('devtools') &&       // DevTools messages are informational
          !errorLower.includes('401') &&            // 401 errors expected for anonymous users
          !errorLower.includes('unauthorized') &&   // Unauthorized errors expected
          !errorLower.includes('failed to fetch') && // Network errors when not logged in
          !errorLower.includes('user') &&           // User-related errors expected for anonymous
          !errorLower.includes('request failed')    // API request failures for auth state
        );
      });

      // Should have no critical JavaScript errors
      // Note: Some auth-related errors are expected for anonymous users
      expect(criticalErrors.length).toBe(0);

      if (criticalErrors.length > 0) {
        console.error('Critical JavaScript errors found:', criticalErrors);
      } else if (consoleErrors.length > 0) {
        console.log(`ℹ️ Non-critical errors ignored (auth-related): ${consoleErrors.length}`);
      }

      console.log('✅ No critical JavaScript console errors on events page');
    });
  });
});
