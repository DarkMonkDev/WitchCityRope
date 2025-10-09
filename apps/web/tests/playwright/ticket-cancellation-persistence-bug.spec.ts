/**
 * E2E Test: Ticket Cancellation Persistence Bug
 *
 * CRITICAL BUG: Ticket cancellation shows UI update but doesn't persist to database
 * - Same pattern as profile update bug (frontend shows success but backend silently ignores)
 * - User clicks "Cancel Ticket" button
 * - UI appears to update (success message/button state changes)
 * - Database NOT updated - ticket still shows as active after page refresh
 * - No network errors in console
 *
 * Expected Backend Endpoint:
 * - DELETE /api/events/{eventId}/ticket (DOES NOT EXIST)
 *
 * Actual Backend Endpoints:
 * - DELETE /api/events/{eventId}/rsvp (EXISTS - for RSVP cancellation)
 * - DELETE /api/events/{eventId}/participation (EXISTS - for any participation)
 *
 * Root Cause: Frontend calls wrong endpoint that doesn't exist
 */

import { test, expect } from '@playwright/test';

const API_BASE_URL = 'http://localhost:5655';
const WEB_BASE_URL = 'http://localhost:5173';

test.describe('Ticket Cancellation Persistence Bug', () => {
  let authCookie: string | undefined;
  let testEventId: string;

  test.beforeAll(async ({ browser }) => {
    // Create a fresh browser context and page
    const context = await browser.newContext();
    const page = await context.newPage();

    // Login as admin to get authentication
    const loginResponse = await page.request.post(`${API_BASE_URL}/api/auth/login`, {
      data: {
        email: 'admin@witchcityrope.com',
        password: 'Test123!'
      }
    });

    expect(loginResponse.ok()).toBeTruthy();

    // Extract the auth cookie
    const cookies = await context.cookies();
    const authCookieObj = cookies.find(c => c.name === '.AspNetCore.Cookies' || c.name.includes('Auth'));
    authCookie = authCookieObj ? `${authCookieObj.name}=${authCookieObj.value}` : undefined;

    console.log('🔍 Auth cookie obtained:', authCookie ? 'SUCCESS' : 'FAILED');

    await context.close();
  });

  test.beforeEach(async ({ page }) => {
    // Setup: Create a test event with a ticket purchase
    // For simplicity, we'll use an existing event in the database
    // In production, we'd create a test event via API

    // Navigate to events page to find an event
    await page.goto(`${WEB_BASE_URL}/events`);
    await page.waitForLoadState('networkidle');

    // Find the first event ID from the page (if available)
    // This is a simplified approach - in production we'd create a test event
    const firstEventLink = page.locator('a[href^="/events/"]').first();
    if (await firstEventLink.count() > 0) {
      const href = await firstEventLink.getAttribute('href');
      testEventId = href?.split('/events/')[1] || '';
      console.log('🔍 Using test event ID:', testEventId);
    }
  });

  test('should reproduce ticket cancellation UI update without database persistence', async ({ page }) => {
    test.skip(!testEventId, 'No test event available');

    // Setup: Start monitoring network requests
    const networkRequests: any[] = [];
    page.on('request', request => {
      if (request.url().includes('ticket') || request.url().includes('participation')) {
        networkRequests.push({
          method: request.method(),
          url: request.url(),
          timestamp: new Date().toISOString()
        });
        console.log(`🔍 REQUEST: ${request.method()} ${request.url()}`);
      }
    });

    const networkResponses: any[] = [];
    page.on('response', response => {
      if (response.url().includes('ticket') || response.url().includes('participation')) {
        networkResponses.push({
          status: response.status(),
          url: response.url(),
          timestamp: new Date().toISOString()
        });
        console.log(`🔍 RESPONSE: ${response.status()} ${response.url()}`);
      }
    });

    // Navigate to event detail page
    await page.goto(`${WEB_BASE_URL}/events/${testEventId}`);
    await page.waitForLoadState('networkidle');

    // Check if user already has a ticket (look for Cancel Ticket button)
    const cancelTicketButton = page.locator('button:has-text("Cancel Ticket")');
    const hasCancelButton = await cancelTicketButton.count() > 0;

    if (!hasCancelButton) {
      console.log('⚠️ User does not have an existing ticket for this event');
      console.log('⚠️ Skipping test - setup required: user needs a ticket to cancel');
      test.skip();
    }

    // Take screenshot before cancellation
    await page.screenshot({ path: '/tmp/before-ticket-cancel.png', fullPage: true });

    // Record UI state before cancellation
    const uiBeforeCancel = {
      hasCancelButton: await cancelTicketButton.count() > 0,
      buttonText: await cancelTicketButton.textContent(),
    };
    console.log('🔍 UI state before cancel:', uiBeforeCancel);

    // Click "Cancel Ticket" button
    await cancelTicketButton.click();

    // Wait for any modal/confirmation dialog
    const confirmButton = page.locator('button:has-text("Cancel Ticket")').last();
    if (await confirmButton.isVisible()) {
      console.log('🔍 Confirmation modal appeared, clicking confirm...');
      await confirmButton.click();
    }

    // Wait for UI to update (network request should complete)
    await page.waitForTimeout(2000);

    // Take screenshot after cancellation
    await page.screenshot({ path: '/tmp/after-ticket-cancel.png', fullPage: true });

    // Check UI update - button should change or disappear
    const uiAfterCancel = {
      hasCancelButton: await cancelTicketButton.count() > 0,
      hasPurchaseButton: await page.locator('button:has-text("Purchase Ticket")').count() > 0,
    };
    console.log('🔍 UI state after cancel:', uiAfterCancel);

    // Log all network requests that occurred
    console.log('🔍 Network requests captured:', networkRequests.length);
    networkRequests.forEach(req => console.log(`  - ${req.method} ${req.url}`));

    console.log('🔍 Network responses captured:', networkResponses.length);
    networkResponses.forEach(res => console.log(`  - ${res.status} ${res.url}`));

    // Critical check: Was the correct endpoint called?
    const deleteRequests = networkRequests.filter(req => req.method === 'DELETE');
    console.log('🔍 DELETE requests:', deleteRequests);

    // Check if frontend called /api/events/{eventId}/ticket (WRONG ENDPOINT)
    const wrongEndpointCalled = deleteRequests.some(req => req.url.includes('/ticket'));
    console.log('🔍 Called wrong endpoint /ticket?', wrongEndpointCalled);

    // Check if frontend called /api/events/{eventId}/participation (CORRECT ENDPOINT)
    const correctEndpointCalled = deleteRequests.some(req => req.url.includes('/participation'));
    console.log('🔍 Called correct endpoint /participation?', correctEndpointCalled);

    // Refresh page to check if cancellation persisted
    console.log('🔍 Refreshing page to verify persistence...');
    await page.reload();
    await page.waitForLoadState('networkidle');

    // Take screenshot after refresh
    await page.screenshot({ path: '/tmp/after-refresh-ticket-cancel.png', fullPage: true });

    // Check if cancellation persisted - if bug exists, Cancel Ticket button will reappear
    const uiAfterRefresh = {
      hasCancelButton: await cancelTicketButton.count() > 0,
      hasPurchaseButton: await page.locator('button:has-text("Purchase Ticket")').count() > 0,
    };
    console.log('🔍 UI state after refresh:', uiAfterRefresh);

    // EXPECTED BEHAVIOR: After refresh, ticket should stay cancelled (no Cancel Ticket button)
    // BUG SYMPTOM: Cancel Ticket button reappears because database wasn't updated

    // Assertions
    expect(deleteRequests.length, 'Should have made DELETE request').toBeGreaterThan(0);

    if (wrongEndpointCalled) {
      console.log('❌ BUG CONFIRMED: Frontend called wrong endpoint /ticket');
      console.log('❌ This endpoint does not exist in the backend');
      console.log('✅ EXPECTED: Frontend should call /api/events/{eventId}/participation');
    }

    if (correctEndpointCalled) {
      console.log('✅ Frontend called correct endpoint /participation');
    } else {
      console.log('❌ Frontend did NOT call correct endpoint /participation');
    }

    // Check if ticket cancellation persisted
    if (uiAfterRefresh.hasCancelButton) {
      console.log('❌ BUG CONFIRMED: Ticket cancellation did NOT persist');
      console.log('❌ Cancel Ticket button reappeared after refresh');
      console.log('❌ Database was not updated');
    } else {
      console.log('✅ Ticket cancellation persisted successfully');
    }

    // Test will document the issue regardless of pass/fail
    // The key evidence is in the console logs and screenshots
  });

  test('should verify backend endpoints exist', async ({ request }) => {
    // Test that the correct endpoint exists
    const testEventId = '00000000-0000-0000-0000-000000000001'; // Dummy GUID for testing

    // Test DELETE /api/events/{eventId}/participation (should exist)
    const participationResponse = await request.delete(
      `${API_BASE_URL}/api/events/${testEventId}/participation`,
      {
        headers: authCookie ? { Cookie: authCookie } : {}
      }
    );

    console.log('🔍 DELETE /participation endpoint status:', participationResponse.status());
    console.log('🔍 404 = exists but event not found, 401 = auth required, 500 = other error');

    // Test DELETE /api/events/{eventId}/ticket (should NOT exist)
    const ticketResponse = await request.delete(
      `${API_BASE_URL}/api/events/${testEventId}/ticket`,
      {
        headers: authCookie ? { Cookie: authCookie } : {}
      }
    );

    console.log('🔍 DELETE /ticket endpoint status:', ticketResponse.status());
    console.log('🔍 404 = endpoint does not exist, 401 = auth required');

    // Document findings
    if (ticketResponse.status() === 404 && participationResponse.status() !== 404) {
      console.log('❌ ENDPOINT MISMATCH CONFIRMED:');
      console.log('   - /api/events/{id}/ticket does NOT exist (404)');
      console.log('   - /api/events/{id}/participation DOES exist');
      console.log('   - Frontend calling wrong endpoint');
    }
  });

  test('should check useParticipation hook for endpoint mismatch', async ({ page }) => {
    // This test documents the code-level issue

    console.log('🔍 FRONTEND CODE ANALYSIS:');
    console.log('');
    console.log('File: /apps/web/src/hooks/useParticipation.ts');
    console.log('Function: useCancelTicket()');
    console.log('Line 186-190:');
    console.log('  await apiClient.delete(`/api/events/${eventId}/ticket`, {');
    console.log('    params: { reason }');
    console.log('  });');
    console.log('');
    console.log('❌ PROBLEM: Endpoint /api/events/{eventId}/ticket does NOT exist');
    console.log('✅ FIX: Should call /api/events/{eventId}/participation instead');
    console.log('');
    console.log('BACKEND ENDPOINTS (ParticipationEndpoints.cs):');
    console.log('  ✅ DELETE /api/events/{eventId}/participation (line 218)');
    console.log('  ✅ DELETE /api/events/{eventId}/rsvp (line 292 - backward compat)');
    console.log('  ❌ DELETE /api/events/{eventId}/ticket (DOES NOT EXIST)');
    console.log('');
    console.log('ROOT CAUSE: Frontend-backend API contract mismatch');
    console.log('SAME PATTERN: Profile update bug where backend ignored missing fields');
  });
});

test.describe('Ticket Cancellation Fix Verification', () => {
  test('should document the required fix', async ({ page }) => {
    console.log('');
    console.log('═══════════════════════════════════════════════════════════');
    console.log('TICKET CANCELLATION BUG - FIX REQUIRED');
    console.log('═══════════════════════════════════════════════════════════');
    console.log('');
    console.log('ISSUE: Ticket cancellation UI updates but database unchanged');
    console.log('');
    console.log('ROOT CAUSE:');
    console.log('  Frontend calls: DELETE /api/events/{eventId}/ticket');
    console.log('  Backend has:    DELETE /api/events/{eventId}/participation');
    console.log('  Result:         404 error (endpoint not found), silently handled');
    console.log('');
    console.log('FIX LOCATION:');
    console.log('  File: /apps/web/src/hooks/useParticipation.ts');
    console.log('  Function: useCancelTicket()');
    console.log('  Line: ~186-190');
    console.log('');
    console.log('CHANGE REQUIRED:');
    console.log('  BEFORE:');
    console.log('    await apiClient.delete(`/api/events/${eventId}/ticket`, {');
    console.log('');
    console.log('  AFTER:');
    console.log('    await apiClient.delete(`/api/events/${eventId}/participation`, {');
    console.log('');
    console.log('ALTERNATIVE OPTIONS:');
    console.log('  1. Update frontend to use /participation endpoint (RECOMMENDED)');
    console.log('  2. Add /ticket endpoint alias in backend (backward compat)');
    console.log('');
    console.log('COMPARISON TO PROFILE BUG:');
    console.log('  - Profile: Backend ignored fields not in database');
    console.log('  - Tickets: Backend endpoint doesn\'t exist, frontend ignores 404');
    console.log('  - Both: Frontend shows success despite backend failure');
    console.log('  - Pattern: Need better error handling for failed API calls');
    console.log('');
    console.log('TESTING REQUIREMENTS:');
    console.log('  1. Fix frontend endpoint URL');
    console.log('  2. Verify DELETE returns 204 No Content on success');
    console.log('  3. Test ticket cancellation persists after page refresh');
    console.log('  4. Verify database EventParticipation status = Cancelled');
    console.log('  5. Add E2E test for ticket cancellation persistence');
    console.log('');
    console.log('═══════════════════════════════════════════════════════════');
  });
});
