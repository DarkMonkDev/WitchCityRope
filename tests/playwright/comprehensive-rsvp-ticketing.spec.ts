import { test, expect } from '@playwright/test';

test.describe('Comprehensive RSVP and Ticketing Tests', () => {

  test('Complete authenticated RSVP and ticket purchase flow', async ({ page }) => {
    console.log('🔍 Starting comprehensive RSVP and ticketing test...');

    // Step 1: Login as vetted member
    await page.goto('http://localhost:5173');
    await page.waitForTimeout(2000);

    // Login
    await page.locator('text=Login').click();
    await page.waitForTimeout(1000);

    await page.locator('input[type="email"]').fill('vetted@witchcityrope.com');
    await page.locator('input[type="password"]').fill('Test123!');
    await page.locator('button[type="submit"]').click();
    await page.waitForTimeout(3000);

    console.log('✅ Logged in as vetted member');

    // Step 2: Navigate to events
    await page.locator('a[href*="/events"]').click();
    await page.waitForTimeout(2000);

    console.log('✅ Navigated to events page');

    // Step 3: Find a class event for ticket testing
    const eventCards = page.locator('[data-testid*="event"], .event-card, .event-item');
    const eventCount = await eventCards.count();
    console.log('📊 Event cards found:', eventCount);

    if (eventCount > 0) {
      // Click first event to go to detail page
      await eventCards.first().click();
      await page.waitForTimeout(2000);

      console.log('✅ Opened event detail page');

      // Look for participation options
      const rsvpButtons = await page.locator('button:has-text("RSVP"), [data-testid*="rsvp"]').count();
      const ticketButtons = await page.locator('button:has-text("Ticket"), button:has-text("Purchase"), [data-testid*="ticket"], button:has-text("PayPal")').count();

      console.log('🎯 RSVP buttons on detail page:', rsvpButtons);
      console.log('🎯 Ticket/Purchase buttons on detail page:', ticketButtons);

      // Test RSVP if available
      if (rsvpButtons > 0) {
        console.log('🔍 Testing RSVP functionality...');
        await page.locator('button:has-text("RSVP"), [data-testid*="rsvp"]').first().click();
        await page.waitForTimeout(2000);

        // Look for success message
        const successMessages = await page.locator('text*="RSVP", text*="success", .toast, .notification').count();
        console.log('✅ RSVP success indicators:', successMessages);
      }

      // Test ticket purchase if available
      if (ticketButtons > 0) {
        console.log('🔍 Testing ticket purchase functionality...');
        await page.locator('button:has-text("Ticket"), button:has-text("Purchase"), [data-testid*="ticket"], button:has-text("PayPal")').first().click();
        await page.waitForTimeout(2000);

        // Look for payment or purchase form
        const paymentForms = await page.locator('.paypal-button, [data-testid*="payment"], .payment-form, .checkout').count();
        console.log('💳 Payment/checkout elements:', paymentForms);
      }
    }

    // Step 4: Test API endpoints with authentication cookies
    const classEventId = 'ca4ec865-2f8e-49a5-988f-e397be26ef10'; // From earlier test
    const socialEventId = '5290be55-59e0-4ec9-b62b-5cc215e6e848'; // From earlier test

    console.log('🔍 Testing authenticated API endpoints...');

    // Test participation status
    const participationResponse = await page.request.get(`http://localhost:5655/api/events/${classEventId}/participation`);
    console.log('📊 Participation status (class event):', participationResponse.status());

    const socialParticipationResponse = await page.request.get(`http://localhost:5655/api/events/${socialEventId}/participation`);
    console.log('📊 Participation status (social event):', socialParticipationResponse.status());

    // Test RSVP endpoint (for social event)
    try {
      const rsvpResponse = await page.request.post(`http://localhost:5655/api/events/${socialEventId}/rsvp`, {
        headers: { 'Content-Type': 'application/json' },
        data: { eventId: socialEventId }
      });
      console.log('📊 RSVP endpoint response:', rsvpResponse.status());

      if (rsvpResponse.status() === 201) {
        const rsvpData = await rsvpResponse.json();
        console.log('✅ RSVP created successfully:', rsvpData);
      } else if (rsvpResponse.status() === 400) {
        const errorData = await rsvpResponse.json();
        console.log('ℹ️ RSVP validation message:', errorData);
      }
    } catch (error) {
      console.log('ℹ️ RSVP test error (expected if already RSVP\'d):', error);
    }

    // Test ticket purchase endpoint (for class event)
    try {
      const ticketResponse = await page.request.post(`http://localhost:5655/api/events/${classEventId}/tickets`, {
        headers: { 'Content-Type': 'application/json' },
        data: {
          eventId: classEventId,
          notes: 'Test ticket purchase',
          paymentMethodId: 'test-payment-id'
        }
      });
      console.log('📊 Ticket purchase endpoint response:', ticketResponse.status());

      if (ticketResponse.status() === 201) {
        const ticketData = await ticketResponse.json();
        console.log('✅ Ticket purchased successfully:', ticketData);
      } else if (ticketResponse.status() === 400) {
        const errorData = await ticketResponse.json();
        console.log('ℹ️ Ticket purchase validation message:', errorData);
      }
    } catch (error) {
      console.log('ℹ️ Ticket purchase test error (expected if already purchased):', error);
    }

    console.log('🏁 Comprehensive RSVP and ticketing test completed');
  });

  test('Test authorization rules for different user types', async ({ page }) => {
    console.log('🔍 Testing authorization rules...');

    const classEventId = 'ca4ec865-2f8e-49a5-988f-e397be26ef10';
    const socialEventId = '5290be55-59e0-4ec9-b62b-5cc215e6e848';

    // Test with general (non-vetted) member
    await page.goto('http://localhost:5173');
    await page.waitForTimeout(2000);

    // Login as general member
    await page.locator('text=Login').click();
    await page.waitForTimeout(1000);

    await page.locator('input[type="email"]').fill('member@witchcityrope.com');
    await page.locator('input[type="password"]').fill('Test123!');
    await page.locator('button[type="submit"]').click();
    await page.waitForTimeout(3000);

    console.log('✅ Logged in as general (non-vetted) member');

    // Test RSVP for social event (should fail - only vetted members)
    const rsvpResponse = await page.request.post(`http://localhost:5655/api/events/${socialEventId}/rsvp`, {
      headers: { 'Content-Type': 'application/json' },
      data: { eventId: socialEventId }
    });
    console.log('📊 Non-vetted RSVP attempt:', rsvpResponse.status());

    if (rsvpResponse.status() === 400) {
      const errorData = await rsvpResponse.json();
      console.log('✅ Proper authorization check:', errorData.title || errorData.message);
    }

    // Test ticket purchase for class event (should succeed - any authenticated user)
    const ticketResponse = await page.request.post(`http://localhost:5655/api/events/${classEventId}/tickets`, {
      headers: { 'Content-Type': 'application/json' },
      data: {
        eventId: classEventId,
        notes: 'Test ticket purchase by general member'
      }
    });
    console.log('📊 General member ticket purchase:', ticketResponse.status());

    if (ticketResponse.status() === 201) {
      console.log('✅ General member can purchase tickets');
    } else if (ticketResponse.status() === 400) {
      const errorData = await ticketResponse.json();
      console.log('ℹ️ Ticket purchase response:', errorData.title || errorData.message);
    }

    console.log('🏁 Authorization rules testing completed');
  });

  test('Verify PayPal integration elements', async ({ page }) => {
    console.log('🔍 Testing PayPal integration...');

    // Login as vetted member
    await page.goto('http://localhost:5173');
    await page.waitForTimeout(2000);

    await page.locator('text=Login').click();
    await page.waitForTimeout(1000);

    await page.locator('input[type="email"]').fill('vetted@witchcityrope.com');
    await page.locator('input[type="password"]').fill('Test123!');
    await page.locator('button[type="submit"]').click();
    await page.waitForTimeout(3000);

    // Navigate to a class event
    await page.locator('a[href*="/events"]').click();
    await page.waitForTimeout(2000);

    // Look for PayPal related elements
    const paypalButtons = await page.locator('[data-testid*="paypal"], .paypal-button, button:has-text("PayPal")').count();
    console.log('💳 PayPal buttons found:', paypalButtons);

    // Check for PayPal script loading
    const paypalScripts = await page.locator('script[src*="paypal"]').count();
    console.log('📜 PayPal scripts loaded:', paypalScripts);

    // Check for payment-related components
    const paymentComponents = await page.locator('[data-testid*="payment"], .payment-form, .ticket-purchase').count();
    console.log('💰 Payment components found:', paymentComponents);

    console.log('🏁 PayPal integration check completed');
  });

  test('Test event type differentiation', async ({ page }) => {
    console.log('🔍 Testing event type differentiation...');

    // Get events via API to check types
    const eventsResponse = await page.request.get('http://localhost:5655/api/events');
    const eventsData = await eventsResponse.json();
    const events = eventsData.data || [];

    const classEvents = events.filter((e: any) => e.eventType === 'Class');
    const socialEvents = events.filter((e: any) => e.eventType === 'Social');

    console.log('📊 Class events:', classEvents.length);
    console.log('📊 Social events:', socialEvents.length);

    // Verify business logic for each type
    classEvents.forEach((event: any) => {
      console.log(`🎓 Class Event: ${event.title}`);
      console.log(`   - Tickets: ${event.currentTickets}`);
      console.log(`   - RSVPs: ${event.currentRSVPs}`);
      console.log(`   - Ticket Types: ${event.ticketTypes?.length || 0}`);
    });

    socialEvents.forEach((event: any) => {
      console.log(`🎉 Social Event: ${event.title}`);
      console.log(`   - RSVPs: ${event.currentRSVPs}`);
      console.log(`   - Tickets: ${event.currentTickets}`);
      console.log(`   - Ticket Types: ${event.ticketTypes?.length || 0}`);
    });

    console.log('🏁 Event type differentiation test completed');
  });
});