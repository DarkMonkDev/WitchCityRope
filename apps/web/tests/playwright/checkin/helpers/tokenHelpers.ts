/**
 * Check-In Token Helpers for E2E Tests
 *
 * Helper functions for generating and managing check-in session tokens.
 *
 * KIOSK MODE AUTHENTICATION:
 * - Check-in interface uses session tokens (NO user authentication)
 * - Admin/EventOrganizer generates tokens via API
 * - Token sent in X-CheckIn-Token header for API calls
 * - Token embedded in URL: /events/{eventId}/checkin?token={token}&event={eventId}
 *
 * IMPORTANT: Tests should clear cookies to simulate kiosk mode (no user auth)
 */

import { Page } from '@playwright/test';

/**
 * Login as Administrator to generate tokens
 * Admin authentication is required to access token generation endpoints
 */
export async function loginAsAdmin(page: Page) {
  await page.goto('http://localhost:5173/login');
  await page.waitForLoadState('networkidle');

  await page.locator('[data-testid="email-or-scenename-input"]').fill('admin@witchcityrope.com');
  await page.locator('[data-testid="password-input"]').fill('Test123!');
  await page.locator('[data-testid="login-button"]').click();

  await page.waitForURL('**/dashboard', { timeout: 10000 });
  await page.waitForLoadState('networkidle');
}

/**
 * Generate a check-in session token via API (requires admin auth)
 *
 * @param page - Playwright page with admin authentication cookie
 * @param eventId - Event ID to generate token for
 * @param expiresInHours - Optional expiration time (default 12 hours)
 * @returns The generated session token string
 *
 * API Endpoint: POST /api/checkin/session-tokens/generate
 * Requires: Administrator or EventOrganizer authentication
 */
export async function generateSessionToken(
  page: Page,
  eventId: string,
  expiresInHours: number = 12
): Promise<string> {
  const response = await page.request.post(
    'http://localhost:5655/api/checkin/session-tokens/generate',
    {
      data: { eventId, expirationHours: expiresInHours }
    }
  );

  if (!response.ok()) {
    throw new Error(`Failed to generate token: ${response.status()} ${await response.text()}`);
  }

  const data = await response.json();
  return data.token;
}

/**
 * Revoke a session token via API (requires admin auth)
 *
 * @param page - Playwright page with admin authentication cookie
 * @param token - Token to revoke
 *
 * API Endpoint: POST /api/checkin/session-tokens/revoke
 * Requires: Administrator or EventOrganizer authentication
 */
export async function revokeSessionToken(page: Page, token: string): Promise<void> {
  const response = await page.request.post(
    'http://localhost:5655/api/checkin/session-tokens/revoke',
    {
      data: { token }
    }
  );

  if (!response.ok()) {
    throw new Error(`Failed to revoke token: ${response.status()} ${await response.text()}`);
  }
}

/**
 * Get test event ID from API
 * Returns the first event from the events endpoint
 *
 * @param page - Playwright page (no auth required for public events endpoint)
 * @returns Event ID string
 */
export async function getTestEventId(page: Page): Promise<string> {
  const response = await page.request.get('http://localhost:5655/api/events');

  if (!response.ok()) {
    throw new Error(`Failed to fetch events: ${response.status()} ${await response.text()}`);
  }

  const data = await response.json();

  // API returns raw array, not wrapped in { success, data }
  if (Array.isArray(data) && data.length > 0) {
    return data[0].id;
  }

  throw new Error('No test events found. Please seed test data.');
}

/**
 * Navigate to check-in interface with session token (NO LOGIN REQUIRED)
 *
 * IMPORTANT: This function should be called AFTER clearing cookies
 * to simulate kiosk mode (no user authentication).
 *
 * @param page - Playwright page (cookies should be cleared first)
 * @param eventId - Event ID
 * @param token - Session token
 */
export async function navigateToCheckIn(page: Page, eventId: string, token: string) {
  await page.goto(`http://localhost:5173/events/${eventId}/checkin?token=${token}&event=${eventId}`);
  await page.waitForLoadState('networkidle');
}

/**
 * Navigate to check-in dashboard with session token (NO LOGIN REQUIRED)
 *
 * IMPORTANT: This function should be called AFTER clearing cookies
 * to simulate kiosk mode (no user authentication).
 *
 * @param page - Playwright page (cookies should be cleared first)
 * @param eventId - Event ID
 * @param token - Session token
 */
export async function navigateToCheckInDashboard(page: Page, eventId: string, token: string) {
  await page.goto(`http://localhost:5173/events/${eventId}/checkin/dashboard?token=${token}&event=${eventId}`);
  await page.waitForLoadState('networkidle');
}

/**
 * Get attendees list from API using session token (NO AUTH COOKIE)
 *
 * @param page - Playwright page
 * @param eventId - Event ID
 * @param sessionToken - Check-in session token
 * @returns Attendees data from API response
 */
export async function getAttendees(page: Page, eventId: string, sessionToken: string) {
  const response = await page.request.get(
    `http://localhost:5655/api/checkin/events/${eventId}/attendees`,
    {
      headers: {
        'X-CheckIn-Token': sessionToken
      }
    }
  );

  const data = await response.json();
  return response.ok() ? data : null;
}

/**
 * Get dashboard data from API using session token (NO AUTH COOKIE)
 *
 * @param page - Playwright page
 * @param eventId - Event ID
 * @param sessionToken - Check-in session token
 * @returns Dashboard data from API response
 */
export async function getDashboardData(page: Page, eventId: string, sessionToken: string) {
  const response = await page.request.get(
    `http://localhost:5655/api/checkin/events/${eventId}/dashboard`,
    {
      headers: {
        'X-CheckIn-Token': sessionToken
      }
    }
  );

  const data = await response.json();
  return response.ok() ? data : null;
}
