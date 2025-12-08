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
 *
 * CONTAINER COMPATIBILITY:
 * - All API calls use page.evaluate() with relative URLs
 * - This ensures compatibility with Docker test containers
 * - DO NOT use hardcoded localhost URLs
 */

import { Page } from '@playwright/test';
import { AuthHelpers } from '../../test-utils/helpers/auth.helpers';

/**
 * Login as Administrator to generate tokens
 * Admin authentication is required to access token generation endpoints
 *
 * IMPORTANT: This re-exports AuthHelpers.loginAs for convenience in check-in tests.
 * DO NOT duplicate login logic - always use AuthHelpers.loginAs as the source of truth.
 */
export async function loginAsAdmin(page: Page) {
  await AuthHelpers.loginAs(page, 'admin');
}

/**
 * Get sessions for an event via API
 *
 * @param page - Playwright page (no auth required for public endpoint)
 * @param eventId - Event ID to get sessions for
 * @returns Array of session objects with id, name, startTime
 *
 * CONTAINER COMPATIBLE: Uses page.evaluate() with relative URLs
 */
export async function getEventSessions(
  page: Page,
  eventId: string
): Promise<Array<{ id: string; name: string; startTime: string }>> {
  // Ensure page has a URL for fetch context
  const currentUrl = page.url();
  if (!currentUrl || currentUrl === 'about:blank') {
    await page.goto('/', { waitUntil: 'domcontentloaded' });
  }

  const result = await page.evaluate(async (evtId: string) => {
    const response = await fetch(`/api/events/${evtId}/sessions`, {
      credentials: 'include'
    });

    if (!response.ok) {
      const text = await response.text();
      return { success: false, error: `${response.status} ${text}`, sessions: [] };
    }

    const data = await response.json();
    return { success: true, sessions: data };
  }, eventId);

  if (!result.success) {
    throw new Error(`Failed to fetch sessions: ${result.error}`);
  }

  return result.sessions;
}

/**
 * Generate a check-in session token via API (requires admin auth)
 *
 * @param page - Playwright page with admin authentication cookie
 * @param eventId - Event ID to generate token for
 * @param sessionId - Session ID to generate token for (optional - will fetch first session if not provided)
 * @param expiresInHours - Optional expiration time (default 12 hours)
 * @returns The generated session token string
 *
 * API Endpoint: POST /api/checkin/session-tokens/generate
 * Requires: Administrator or EventOrganizer authentication
 *
 * CONTAINER COMPATIBLE: Uses page.evaluate() with relative URLs
 */
export async function generateSessionToken(
  page: Page,
  eventId: string,
  sessionIdOrExpiration?: string | number,
  expiresInHours?: number
): Promise<string> {
  // Handle overloaded parameters
  let sessionId: string | undefined;
  let expiration: number = 12;

  if (typeof sessionIdOrExpiration === 'string') {
    sessionId = sessionIdOrExpiration;
    expiration = expiresInHours ?? 12;
  } else if (typeof sessionIdOrExpiration === 'number') {
    expiration = sessionIdOrExpiration;
  }

  // If no sessionId provided, fetch the first session for this event
  if (!sessionId) {
    const sessions = await getEventSessions(page, eventId);
    if (sessions.length === 0) {
      throw new Error(`No sessions found for event ${eventId}`);
    }
    sessionId = sessions[0].id;
  }

  // Use page.evaluate() for container compatibility - runs fetch in browser context
  const result = await page.evaluate(async (params: { eventId: string; sessionId: string; expirationHours: number }) => {
    const response = await fetch('/api/checkin/session-tokens/generate', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      credentials: 'include', // Include auth cookies
      body: JSON.stringify(params)
    });

    if (!response.ok) {
      const text = await response.text();
      return { success: false, error: `${response.status} ${text}` };
    }

    const data = await response.json();
    return { success: true, token: data.token };
  }, { eventId, sessionId, expirationHours: expiration });

  if (!result.success) {
    throw new Error(`Failed to generate token: ${result.error}`);
  }

  return result.token!;
}

/**
 * Revoke a session token via API (requires admin auth)
 *
 * @param page - Playwright page with admin authentication cookie
 * @param token - Token to revoke
 *
 * API Endpoint: POST /api/checkin/session-tokens/revoke
 * Requires: Administrator or EventOrganizer authentication
 *
 * CONTAINER COMPATIBLE: Uses page.evaluate() with relative URLs
 */
export async function revokeSessionToken(page: Page, token: string): Promise<void> {
  // Use page.evaluate() for container compatibility - runs fetch in browser context
  const result = await page.evaluate(async (tokenToRevoke: string) => {
    const response = await fetch('/api/checkin/session-tokens/revoke', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      credentials: 'include', // Include auth cookies
      body: JSON.stringify({ token: tokenToRevoke })
    });

    if (!response.ok) {
      const text = await response.text();
      return { success: false, error: `${response.status} ${text}` };
    }

    return { success: true };
  }, token);

  if (!result.success) {
    throw new Error(`Failed to revoke token: ${result.error}`);
  }
}

/**
 * Get test event ID from API
 * Returns the first event from the events endpoint
 *
 * @param page - Playwright page (no auth required for public events endpoint)
 * @returns Event ID string
 *
 * CONTAINER COMPATIBLE: Navigates to base URL first, then uses page.evaluate()
 * This ensures the browser has a valid origin for relative fetch calls.
 */
export async function getTestEventId(page: Page): Promise<string> {
  // CRITICAL: Navigate to root first to establish base URL for fetch calls
  // Without this, page.evaluate() with relative URLs fails in beforeAll hooks
  const currentUrl = page.url();
  if (!currentUrl || currentUrl === 'about:blank') {
    await page.goto('/', { waitUntil: 'domcontentloaded' });
  }

  // Use page.evaluate() for container compatibility - runs fetch in browser context
  const result = await page.evaluate(async () => {
    const response = await fetch('/api/events', { credentials: 'include' });

    if (!response.ok) {
      const text = await response.text();
      return { success: false, error: `${response.status} ${text}` };
    }

    const data = await response.json();

    // API returns raw array, not wrapped in { success, data }
    if (Array.isArray(data) && data.length > 0) {
      return { success: true, eventId: data[0].id };
    }

    return { success: false, error: 'No test events found' };
  });

  if (!result.success) {
    throw new Error(`Failed to fetch events: ${result.error}`);
  }

  return result.eventId!;
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
  // Use relative URL for container compatibility (baseURL from playwright.config.ts)
  await page.goto(`/events/${eventId}/checkin?token=${token}&event=${eventId}`, {
    waitUntil: 'domcontentloaded'
  });
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
  // Use relative URL for container compatibility (baseURL from playwright.config.ts)
  await page.goto(`/events/${eventId}/checkin/dashboard?token=${token}&event=${eventId}`, {
    waitUntil: 'domcontentloaded'
  });
}

/**
 * Get attendees list from API using session token (NO AUTH COOKIE)
 *
 * @param page - Playwright page
 * @param eventId - Event ID
 * @param sessionToken - Check-in session token
 * @returns Attendees data from API response, or null if failed
 *
 * CONTAINER COMPATIBLE: Uses page.evaluate() with relative URLs
 */
export async function getAttendees(page: Page, eventId: string, sessionToken: string) {
  // Use page.evaluate() for container compatibility - runs fetch in browser context
  const result = await page.evaluate(async (params: { eventId: string; token: string }) => {
    const response = await fetch(`/api/checkin/events/${params.eventId}/attendees`, {
      headers: {
        'X-CheckIn-Token': params.token
      }
    });

    if (!response.ok) {
      return { success: false, data: null };
    }

    const data = await response.json();
    return { success: true, data };
  }, { eventId, token: sessionToken });

  return result.success ? result.data : null;
}

/**
 * Get dashboard data from API using session token (NO AUTH COOKIE)
 *
 * @param page - Playwright page
 * @param eventId - Event ID
 * @param sessionToken - Check-in session token
 * @returns Dashboard data from API response, or null if failed
 *
 * CONTAINER COMPATIBLE: Uses page.evaluate() with relative URLs
 */
export async function getDashboardData(page: Page, eventId: string, sessionToken: string) {
  // Use page.evaluate() for container compatibility - runs fetch in browser context
  const result = await page.evaluate(async (params: { eventId: string; token: string }) => {
    const response = await fetch(`/api/checkin/events/${params.eventId}/dashboard`, {
      headers: {
        'X-CheckIn-Token': params.token
      }
    });

    if (!response.ok) {
      return { success: false, data: null };
    }

    const data = await response.json();
    return { success: true, data };
  }, { eventId, token: sessionToken });

  return result.success ? result.data : null;
}
