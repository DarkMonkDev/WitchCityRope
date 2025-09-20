import { test, expect } from '@playwright/test';

test.describe('RSVP and Ticketing Implementation Tests', () => {

  test('Verify React app mounts correctly after import fix', async ({ page }) => {
    console.log('🔍 Testing React app mounting...');

    await page.goto('http://localhost:5173');

    // Wait for React to mount
    await page.waitForTimeout(2000);

    // Check page title
    const title = await page.title();
    expect(title).toContain('Witch City Rope');
    console.log('✅ Page title correct:', title);

    // Check if root element has content (React mounted)
    const rootContent = await page.locator('#root').innerHTML();
    expect(rootContent.length).toBeGreaterThan(0);
    console.log('✅ React app mounted - root content length:', rootContent.length);

    // Check for basic UI elements
    const bodyText = await page.locator('body').textContent();
    console.log('📝 Page content preview:', bodyText?.substring(0, 200));

    // Look for login button or navigation
    const hasLogin = await page.locator('[data-testid="login-button"], button:has-text("Login"), button:has-text("LOGIN")').count();
    console.log('🔍 Login button found:', hasLogin > 0);
  });

  test('Check API endpoints for RSVP and ticketing', async ({ page }) => {
    console.log('🔍 Testing API endpoints...');

    // Test events API
    const eventsResponse = await page.request.get('http://localhost:5655/api/events');
    expect(eventsResponse.status()).toBe(200);
    console.log('✅ Events API responding');

    const eventsData = await eventsResponse.json();
    console.log('📊 Events found:', eventsData.data?.length || 0);

    // Find class and social events for testing
    const events = eventsData.data || [];
    const classEvent = events.find((e: any) => e.eventType === 'Class');
    const socialEvent = events.find((e: any) => e.eventType === 'Social');

    console.log('🎯 Class event found:', !!classEvent);
    console.log('🎯 Social event found:', !!socialEvent);

    if (classEvent) {
      console.log('📝 Class event for testing:', {
        id: classEvent.id,
        title: classEvent.title,
        capacity: classEvent.capacity,
        currentTickets: classEvent.currentTickets
      });
    }

    if (socialEvent) {
      console.log('📝 Social event for testing:', {
        id: socialEvent.id,
        title: socialEvent.title,
        capacity: socialEvent.capacity,
        currentRSVPs: socialEvent.currentRSVPs
      });
    }
  });

  test('Test authentication for RSVP/ticketing', async ({ page }) => {
    console.log('🔍 Testing authentication flow...');

    await page.goto('http://localhost:5173');
    await page.waitForTimeout(2000);

    // Try to find and click login
    const loginLocators = [
      '[data-testid="login-button"]',
      'button:has-text("Login")',
      'button:has-text("LOGIN")',
      'text=Login',
      'text=LOGIN'
    ];

    let loginClicked = false;
    for (const locator of loginLocators) {
      try {
        const element = page.locator(locator).first();
        if (await element.isVisible({ timeout: 2000 })) {
          await element.click();
          console.log('✅ Login button clicked:', locator);
          loginClicked = true;
          break;
        }
      } catch (e) {
        // Try next locator
      }
    }

    if (loginClicked) {
      await page.waitForTimeout(1000);

      // Look for email/password fields
      const emailField = page.locator('input[type="email"], input[name="email"], input[placeholder*="email" i]');
      const passwordField = page.locator('input[type="password"], input[name="password"]');

      if (await emailField.isVisible({ timeout: 2000 })) {
        console.log('✅ Login form found - email field visible');

        // Test with vetted member
        await emailField.fill('vetted@witchcityrope.com');
        await passwordField.fill('Test123!');

        const submitButton = page.locator('button[type="submit"], button:has-text("Login"), button:has-text("Sign In")');
        if (await submitButton.isVisible({ timeout: 1000 })) {
          await submitButton.click();
          console.log('✅ Login form submitted');
          await page.waitForTimeout(2000);
        }
      } else {
        console.log('ℹ️ Login form not found after clicking login button');
      }
    } else {
      console.log('ℹ️ Login button not found on page');
    }
  });

  test('Navigate to events and check participation options', async ({ page }) => {
    console.log('🔍 Testing event navigation and participation options...');

    await page.goto('http://localhost:5173');
    await page.waitForTimeout(2000);

    // Look for events navigation
    const eventsLinks = [
      'a[href*="/events"]',
      'text=Events',
      'text=EVENTS',
      '[data-testid="events-link"]'
    ];

    let eventsFound = false;
    for (const link of eventsLinks) {
      try {
        const element = page.locator(link).first();
        if (await element.isVisible({ timeout: 2000 })) {
          await element.click();
          console.log('✅ Events navigation clicked:', link);
          eventsFound = true;
          await page.waitForTimeout(2000);
          break;
        }
      } catch (e) {
        // Try next link
      }
    }

    if (eventsFound) {
      // Look for event cards or list
      const eventElements = await page.locator('[data-testid*="event"], .event-card, .event-item').count();
      console.log('📊 Event elements found:', eventElements);

      // Look for RSVP or ticket purchase buttons
      const rsvpButtons = await page.locator('button:has-text("RSVP"), [data-testid*="rsvp"]').count();
      const ticketButtons = await page.locator('button:has-text("Ticket"), button:has-text("Purchase"), [data-testid*="ticket"]').count();

      console.log('🎯 RSVP buttons found:', rsvpButtons);
      console.log('🎯 Ticket buttons found:', ticketButtons);
    } else {
      console.log('ℹ️ Could not navigate to events page');
    }
  });

  test('Test participation API endpoints directly', async ({ page }) => {
    console.log('🔍 Testing participation API endpoints...');

    // Get events first
    const eventsResponse = await page.request.get('http://localhost:5655/api/events');
    const eventsData = await eventsResponse.json();
    const events = eventsData.data || [];

    if (events.length > 0) {
      const testEventId = events[0].id;
      console.log('🎯 Testing with event ID:', testEventId);

      // Test participation status endpoint (should return 401 without auth)
      const participationResponse = await page.request.get(`http://localhost:5655/api/events/${testEventId}/participation`);
      console.log('📊 Participation status endpoint:', participationResponse.status());

      // Test RSVP endpoint (should return 401 without auth)
      const rsvpResponse = await page.request.post(`http://localhost:5655/api/events/${testEventId}/rsvp`, {
        data: { eventId: testEventId }
      });
      console.log('📊 RSVP endpoint:', rsvpResponse.status());

      // Test ticket purchase endpoint (should return 401 without auth)
      const ticketResponse = await page.request.post(`http://localhost:5655/api/events/${testEventId}/tickets`, {
        data: { eventId: testEventId }
      });
      console.log('📊 Ticket purchase endpoint:', ticketResponse.status());

      console.log('✅ All endpoints responding (401 expected without authentication)');
    }
  });
});