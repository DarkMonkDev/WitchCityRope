import { test, expect, Page, BrowserContext } from '@playwright/test';
import { AuthHelpers } from './helpers/auth.helpers';

const TEST_ACCOUNTS = {
  member: {
    email: 'member@witchcityrope.com',
    password: 'Test123!'
  }
};

/**
 * Comprehensive E2E Test Suite: Events System Full User Journey
 *
 * This test validates the complete TDD user story from event discovery to registration management.
 * It tests the REAL user journey without mocks - this is true E2E testing.
 *
 * Test Flow:
 * 1. User discovers events on public page
 * 2. User views event details
 * 3. User attempts to RSVP/purchase ticket (should redirect to login)
 * 4. User logs in successfully
 * 5. User completes RSVP/ticket purchase for event
 * 6. User views RSVP/ticket in dashboard
 * 7. User cancels RSVP
 * 8. Admin views event management
 *
 * CRITICAL: This test uses real API calls (port 5655 Docker) and real database data.
 * No mocks or stubs - this validates the complete integration.
 */

test.describe('Events System - Complete User Journey E2E Tests', () => {
  
  test.beforeEach(async ({ page }) => {
    // Ensure we're starting from a clean state
    await page.goto('http://localhost:5173');
    await expect(page).toHaveTitle(/Witch City Rope/i);
  });

  test('1. User discovers events on public page', async ({ page }) => {
    // Navigate to events page (should be accessible without login)
    await page.goto('http://localhost:5173/events');
    
    // Wait for page to load
    await page.waitForLoadState('networkidle');
    
    // Should see events list
    await expect(page.locator('[data-testid="events-list"]')).toBeVisible({ timeout: 10000 });
    
    // Should see at least one event (we have 5 in database)
    const eventCards = page.locator('[data-testid="event-card"]');
    await expect(eventCards.first()).toBeVisible();
    
    // Count events displayed
    const eventCount = await eventCards.count();
    expect(eventCount).toBeGreaterThan(0);
    
    // Screenshot for evidence
    await page.screenshot({ path: '/home/chad/repos/witchcityrope-react/test-results/events-public-discovery.png' });
    
    console.log(`✅ Events discovery: Found ${eventCount} events on public page`);
  });

  test.skip('2. User views event details', async ({ page }) => {
    // TODO: Unskip when event detail modal/page is implemented
    // Feature Status: Not implemented - event cards not clickable to show details
    // Reference: /docs/functional-areas/events/event-detail-view.md
    // Expected: Click event card → navigate to event detail page with full description, RSVP button

    // Navigate to events page
    await page.goto('http://localhost:5173/events');
    await page.waitForLoadState('networkidle');

    // Click on first event to view details
    const firstEvent = page.locator('[data-testid="event-card"]').first();
    await expect(firstEvent).toBeVisible();

    // Get event title for verification
    const eventTitle = await firstEvent.locator('[data-testid="event-title"]').textContent();

    await firstEvent.click();

    // Should navigate to event detail page
    await page.waitForURL('**/events/**');

    // Should see event details
    await expect(page.locator('[data-testid="event-details"]')).toBeVisible({ timeout: 10000 });
    await expect(page.locator('[data-testid="event-title"]')).toBeVisible();
    await expect(page.locator('[data-testid="event-description"]')).toBeVisible();
    await expect(page.locator('[data-testid="event-date"]')).toBeVisible();

    // Should see RSVP or ticket purchase button
    const participationButton = page.locator('[data-testid="button-rsvp"], [data-testid="button-purchase-ticket"]');
    await expect(participationButton.first()).toBeVisible();

    await page.screenshot({ path: '/home/chad/repos/witchcityrope-react/test-results/event-details-view.png' });

    console.log(`✅ Event details: Successfully viewed details for "${eventTitle}"`);
  });

  test.skip('3. User attempts to RSVP/purchase ticket (should redirect to login)', async ({ page }) => {
    // TODO: Unskip when RSVP/ticket purchase flow is implemented
    // Feature Status: Not implemented - depends on event detail view (test 2)
    // Reference: /docs/functional-areas/events/rsvp-ticketing-workflow.md
    // Expected: Click RSVP/ticket button → redirect to login if not authenticated

    // Navigate to event details
    await page.goto('http://localhost:5173/events');
    await page.waitForLoadState('networkidle');

    const firstEvent = page.locator('[data-testid="event-card"]').first();
    await firstEvent.click();
    await page.waitForURL('**/events/**');

    // Try to RSVP/purchase ticket without being logged in
    const participationButton = page.locator('[data-testid="button-rsvp"], [data-testid="button-purchase-ticket"]').first();
    await expect(participationButton).toBeVisible();

    await participationButton.click();

    // Should redirect to login page
    await page.waitForURL('**/login**');

    // Should see login form
    await expect(page.locator('[data-testid="login-form"]')).toBeVisible({ timeout: 10000 });
    await expect(page.locator('[data-testid="email-input"]')).toBeVisible();
    await expect(page.locator('[data-testid="password-input"]')).toBeVisible();
    await expect(page.locator('[data-testid="login-button"]')).toBeVisible();

    // Should have returnTo parameter for redirect after login
    const url = page.url();
    expect(url).toContain('returnTo=');

    await page.screenshot({ path: '/home/chad/repos/witchcityrope-react/test-results/login-redirect-from-rsvp.png' });

    console.log('✅ RSVP redirect: Correctly redirected to login when not authenticated');
  });

  test('4. User logs in successfully', async ({ page }) => {
    // Login using AuthHelpers
    await AuthHelpers.loginAs(page, 'member');

    await page.screenshot({ path: '/home/chad/repos/witchcityrope-react/test-results/successful-login.png' });

    console.log('✅ Login successful: User member@witchcityrope.com logged in successfully');
  });

  test.skip('5. User completes RSVP/ticket purchase for event', async ({ page, context }) => {
    // TODO: Unskip when RSVP/ticket purchase submission flow is implemented
    // Feature Status: Not implemented - RSVP/ticket forms and submission endpoints not ready
    // Reference: /docs/functional-areas/events/rsvp-ticketing-workflow.md
    // Expected: Fill RSVP/ticket form → submit → see success confirmation

    // Login first using AuthHelpers
    await AuthHelpers.loginAs(page, 'member');

    // Navigate to events and select an event
    await page.goto('http://localhost:5173/events');
    await page.waitForLoadState('networkidle');

    const firstEvent = page.locator('[data-testid="event-card"]').first();
    const eventTitle = await firstEvent.locator('[data-testid="event-title"]').textContent();

    await firstEvent.click();
    await page.waitForURL('**/events/**');

    // Now that we're logged in, RSVP/ticket purchase should work
    const participationButton = page.locator('[data-testid="button-rsvp"], [data-testid="button-purchase-ticket"]').first();
    await expect(participationButton).toBeVisible({ timeout: 10000 });

    await participationButton.click();

    // Should show RSVP/ticket form or success message
    const participationSuccess = page.locator('[data-testid="rsvp-success"], [data-testid="ticket-success"]');
    const participationForm = page.locator('[data-testid="rsvp-form"], [data-testid="ticket-form"]');

    // Handle either immediate success or form-based participation
    if (await participationForm.first().isVisible({ timeout: 5000 })) {
      // Fill out form if it exists
      const submitButton = page.locator('[data-testid="rsvp-submit"], [data-testid="ticket-submit"]').first();
      await expect(submitButton).toBeVisible();
      await submitButton.click();
    }

    // Should see success confirmation
    await expect(participationSuccess.first()).toBeVisible({ timeout: 10000 });

    await page.screenshot({ path: '/home/chad/repos/witchcityrope-react/test-results/event-participation-complete.png' });

    console.log(`✅ Event RSVP/ticket: Successfully completed for "${eventTitle}"`);
  });

  test.skip('6. User views RSVP/tickets in dashboard', async ({ page }) => {
    // TODO: Unskip when dashboard RSVP/ticket display is implemented
    // Feature Status: Not implemented - dashboard registration/RSVP listing not ready
    // Reference: /docs/functional-areas/events/dashboard-registrations.md
    // Expected: Navigate to dashboard → see list of user's RSVPs and tickets

    // Login using AuthHelpers
    await AuthHelpers.loginAs(page, 'member');

    // Navigate to dashboard
    await page.goto('http://localhost:5173/dashboard');
    await page.waitForLoadState('networkidle');

    // Should see dashboard
    await expect(page.locator('[data-testid="dashboard"]')).toBeVisible({ timeout: 10000 });

    // Should see RSVPs/tickets section
    await expect(page.locator('[data-testid="my-rsvps"], [data-testid="my-tickets"]').first()).toBeVisible();

    // Should see at least RSVP/ticket details
    const participation = page.locator('[data-testid="rsvp-item"], [data-testid="ticket-item"]');
    // Note: User may or may not have existing RSVPs/tickets, so we don't assert count

    await page.screenshot({ path: '/home/chad/repos/witchcityrope-react/test-results/dashboard-rsvps.png' });

    console.log('✅ Dashboard view: Successfully viewed RSVPs/tickets in dashboard');
  });

  test.skip('7. User cancels an RSVP', async ({ page }) => {
    // TODO: Unskip when RSVP cancellation feature is implemented
    // Feature Status: Not implemented - RSVP cancellation workflow not ready
    // Reference: /docs/functional-areas/events/rsvp-cancellation.md
    // Expected: Click cancel button on RSVP → confirm → RSVP removed from dashboard

    // This test assumes user has an existing RSVP
    // In a real scenario, we'd create an RSVP first

    await AuthHelpers.loginAs(page, 'member');

    // Navigate to dashboard
    await page.goto('http://localhost:5173/dashboard');
    await page.waitForLoadState('networkidle');

    // Look for cancel buttons on RSVPs
    const cancelButtons = page.locator('[data-testid="cancel-rsvp"], [data-testid="remove-rsvp"]');

    if (await cancelButtons.count() > 0) {
      const firstCancelButton = cancelButtons.first();
      await firstCancelButton.click();

      // Should show confirmation dialog
      const confirmDialog = page.locator('[data-testid="cancel-confirmation"]');
      if (await confirmDialog.isVisible({ timeout: 5000 })) {
        const confirmButton = page.locator('[data-testid="confirm-cancel"]');
        await confirmButton.click();
      }

      // Should show success message
      await expect(page.locator('[data-testid="cancellation-success"]')).toBeVisible({ timeout: 10000 });

      console.log('✅ RSVP cancellation: Successfully cancelled an RSVP');
    } else {
      console.log('ℹ️  RSVP cancellation: No existing RSVPs to cancel');
    }

    await page.screenshot({ path: '/home/chad/repos/witchcityrope-react/test-results/rsvp-cancellation.png' });
  });

  test('8. Admin views event management', async ({ page }) => {
    // Login as admin using AuthHelpers
    await AuthHelpers.loginAs(page, 'admin');
    
    // Navigate to admin/events management
    await page.goto('http://localhost:5173/admin/events');
    await page.waitForLoadState('networkidle');
    
    // Should see admin events page
    await expect(page.locator('[data-testid="admin-events"]')).toBeVisible({ timeout: 10000 });
    
    // Should see events management controls
    await expect(page.locator('[data-testid="create-event-button"]')).toBeVisible();
    
    // Should see list of events with admin controls
    const eventRows = page.locator('[data-testid="admin-event-row"]');
    await expect(eventRows.first()).toBeVisible();
    
    // Should see edit and delete buttons
    await expect(page.locator('[data-testid="edit-event"]').first()).toBeVisible();
    
    await page.screenshot({ path: '/home/chad/repos/witchcityrope-react/test-results/admin-event-management.png' });
    
    console.log('✅ Admin management: Successfully accessed event management interface');
  });

  test.skip('9. Complete journey - Discovery to Registration', async ({ page }) => {
    // TODO: Unskip when full RSVP/ticket workflow is implemented
    // Feature Status: Not implemented - depends on tests 2, 3, 5, 6, 7
    // Reference: /docs/functional-areas/events/complete-user-journey.md
    // Expected: Full flow from event discovery → login → RSVP → dashboard view → cancellation

    console.log('🚀 Starting complete user journey test...');

    // Step 1: Discover events (unauthenticated)
    await page.goto('http://localhost:5173/events');
    await page.waitForLoadState('networkidle');

    const eventCards = page.locator('[data-testid="event-card"]');
    await expect(eventCards.first()).toBeVisible({ timeout: 10000 });
    const eventCount = await eventCards.count();
    console.log(`   📅 Found ${eventCount} events`);

    // Step 2: View event details
    const firstEvent = eventCards.first();
    const eventTitle = await firstEvent.locator('[data-testid="event-title"]').textContent();
    await firstEvent.click();
    await page.waitForURL('**/events/**');
    console.log(`   🔍 Viewing details for: ${eventTitle}`);

    // Step 3: Attempt RSVP/ticket purchase (should redirect to login)
    const participationButton = page.locator('[data-testid="button-rsvp"], [data-testid="button-purchase-ticket"]').first();
    await expect(participationButton).toBeVisible();
    await participationButton.click();
    await page.waitForURL('**/login**');
    console.log('   🔐 Redirected to login as expected');

    // Step 4: Login using AuthHelpers
    await AuthHelpers.loginAs(page, 'member');
    console.log('   ✅ Successfully logged in');

    // Step 5: Navigate back to event and complete RSVP/ticket purchase
    await page.goto('http://localhost:5173/events');
    await page.waitForLoadState('networkidle');
    await page.locator('[data-testid="event-card"]').first().click();
    await page.waitForURL('**/events/**');

    const participationButtonLoggedIn = page.locator('[data-testid="button-rsvp"], [data-testid="button-purchase-ticket"]').first();
    await expect(participationButtonLoggedIn).toBeVisible();
    await participationButtonLoggedIn.click();

    // Handle RSVP/ticket process
    const participationSuccess = page.locator('[data-testid="rsvp-success"], [data-testid="ticket-success"]');
    await expect(participationSuccess.first()).toBeVisible({ timeout: 10000 });
    console.log('   🎯 RSVP/ticket purchase completed successfully');

    // Step 6: Verify in dashboard
    await page.goto('http://localhost:5173/dashboard');
    await page.waitForLoadState('networkidle');
    await expect(page.locator('[data-testid="dashboard"]')).toBeVisible();
    await expect(page.locator('[data-testid="my-rsvps"], [data-testid="my-tickets"]').first()).toBeVisible();
    console.log('   📊 Verified RSVP/ticket in dashboard');

    await page.screenshot({ path: '/home/chad/repos/witchcityrope-react/test-results/complete-journey-success.png' });

    console.log('🎉 Complete user journey test PASSED!');
  });

  test('10. API Integration Verification', async ({ page, request }) => {
    console.log('🔧 Testing API integration...');
    
    // Test API endpoints directly
    const healthResponse = await request.get('http://localhost:5655/api/health');
    expect(healthResponse.status()).toBe(200);
    console.log('   ✅ API health endpoint working');
    
    // Test events API
    const eventsResponse = await request.get('http://localhost:5655/api/events');
    expect(eventsResponse.status()).toBe(200);

    const eventsApiResponse = await eventsResponse.json();
    expect(eventsApiResponse.success).toBe(true);
    expect(eventsApiResponse.error).toBeNull();
    expect(Array.isArray(eventsApiResponse.data)).toBe(true);
    expect(eventsApiResponse.data.length).toBeGreaterThan(0);
    console.log(`   📅 Events API returned ${eventsApiResponse.data.length} events`);
    
    // Test login API
    const loginResponse = await request.post('http://localhost:5655/api/auth/login', {
      data: {
        email: TEST_ACCOUNTS.member.email,
        password: TEST_ACCOUNTS.member.password
      }
    });

    expect(loginResponse.status()).toBe(200);
    const loginData = await loginResponse.json();
    expect(loginData.success).toBe(true);
    expect(loginData.user).toBeDefined();
    expect(loginData.user.email).toBe(TEST_ACCOUNTS.member.email);
    console.log('   🔐 Login API working correctly (cookie-based auth)');
    
    console.log('✅ API integration verification PASSED!');
  });

  test('11. Error Handling - Invalid Login', async ({ page }) => {
    console.log('🚨 Testing error handling...');

    // Try invalid login using AuthHelpers
    await AuthHelpers.loginExpectingError(
      page,
      { email: 'invalid@test.com', password: 'wrongpassword' }
    );

    // Should still be on login page
    expect(page.url()).toContain('login');

    console.log('✅ Error handling test PASSED!');
  });

  test('12. Performance and Responsiveness', async ({ page }) => {
    console.log('⚡ Testing performance...');
    
    // Measure page load times
    const startTime = Date.now();
    await page.goto('http://localhost:5173/events');
    await page.waitForLoadState('networkidle');
    const loadTime = Date.now() - startTime;
    
    console.log(`   📊 Events page load time: ${loadTime}ms`);
    expect(loadTime).toBeLessThan(10000); // 10 second timeout
    
    // Test mobile responsiveness
    await page.setViewportSize({ width: 375, height: 667 }); // iPhone SE
    await page.waitForTimeout(1000);
    
    // Events should still be visible
    await expect(page.locator('[data-testid="events-list"]')).toBeVisible();
    
    await page.screenshot({ path: '/home/chad/repos/witchcityrope-react/test-results/mobile-events-view.png' });
    
    console.log('✅ Performance and responsiveness test PASSED!');
  });

});

// Test Data Validation
test.describe('Test Environment Validation', () => {
  
  test('Environment Health Check', async ({ page, request }) => {
    console.log('🏥 Running environment health check...');
    
    // Test React app
    const reactResponse = await request.get('http://localhost:5173');
    expect(reactResponse.status()).toBe(200);
    console.log('   ✅ React app healthy');
    
    // Test API
    const apiResponse = await request.get('http://localhost:5655/api/health');
    expect(apiResponse.status()).toBe(200);
    console.log('   ✅ API healthy');
    
    // Test database connectivity through API
    const eventsResponse = await request.get('http://localhost:5655/api/events');
    expect(eventsResponse.status()).toBe(200);
    console.log('   ✅ Database connectivity verified');
    
    // Verify test accounts exist by attempting login
    const loginResponse = await request.post('http://localhost:5655/api/auth/login', {
      data: {
        email: TEST_ACCOUNTS.member.email,
        password: TEST_ACCOUNTS.member.password
      }
    });
    expect(loginResponse.status()).toBe(200);
    console.log('   ✅ Test accounts available');
    
    console.log('🎉 Environment health check PASSED!');
  });
  
});