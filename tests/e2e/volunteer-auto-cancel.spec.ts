/**
 * Volunteer Auto-Cancel on Ticket Cancellation E2E Tests
 *
 * Tests that when a user cancels a ticket for a specific session,
 * their volunteer signup for that session is automatically cancelled,
 * while their volunteer signups for other sessions remain active.
 *
 * Each test is INDEPENDENT and creates its own complete test data.
 *
 * CRITICAL TIMING CONFIGURATION (prevents business logic failures):
 * - RegistrationOpenHours: null
 * - RegistrationCloseHours: 0
 * - CancellationCloseHours: 0
 * - VolunteerRegistrationCloseHours: 0
 * - VolunteerCancellationCloseHours: 0
 * - Sessions start 7+ days in future
 *
 * Created: 2025-12-09
 */

import { test, expect, Page } from '@playwright/test';
import { AuthHelpers } from './test-utils/helpers/auth.helpers';

// Helper to get CSRF token from cookies, fetching it if not present
async function getCsrfToken(page: Page): Promise<string | null> {
  let cookies = await page.context().cookies();
  let csrfCookie = cookies.find((c) => c.name === 'XSRF-TOKEN');

  if (!csrfCookie) {
    await page.request.get('/api/antiforgery/token');
    cookies = await page.context().cookies();
    csrfCookie = cookies.find((c) => c.name === 'XSRF-TOKEN');
  }

  return csrfCookie?.value || null;
}

// Helper to make authenticated API request using Playwright's request API
async function apiRequest(page: Page, method: string, url: string, data?: any): Promise<any> {
  const headers: Record<string, string> = {};

  if (method !== 'GET') {
    const csrfToken = await getCsrfToken(page);
    if (csrfToken) {
      headers['X-CSRF-TOKEN'] = csrfToken;
    }
  }

  let response;
  if (method === 'GET') {
    response = await page.request.get(url, { headers });
  } else if (method === 'POST') {
    response = await page.request.post(url, { headers, data });
  } else if (method === 'PUT') {
    response = await page.request.put(url, { headers, data });
  } else if (method === 'DELETE') {
    response = await page.request.delete(url, { headers });
  } else {
    throw new Error(`Unsupported method: ${method}`);
  }

  const text = await response.text();
  try {
    return { status: response.status(), data: JSON.parse(text) };
  } catch {
    return { status: response.status(), data: text };
  }
}

interface TestSetupResult {
  testEventId: string;
  testUserId: string;
  testUserEmail: string;
  ticketTypeSession1Id: string;
  ticketTypeSession2Id: string;
  volunteerPosition1Id: string;
  volunteerPosition2Id: string;
  session1Id: string;
  session2Id: string;
}

/**
 * Creates a complete test setup with:
 * - Test user (vetted)
 * - Event with 2 sessions
 * - Ticket types for each session
 * - Volunteer positions for each session
 */
async function createTestSetup(page: Page, testName: string): Promise<TestSetupResult> {
  const timestamp = Date.now();

  // Create test user
  const createUserResponse = await apiRequest(page, 'POST', '/api/test-helpers/users', {
    email: `vol-cancel-${testName}-${timestamp}@test.local`,
    password: 'Test123!',
    sceneName: `VolCancel${timestamp}`,
    firstName: 'VolCancel',
    lastName: 'Test',
    role: 'Member',
    vettingStatus: 3, // Approved (vetted) - required for volunteering
  });

  const testUserId = createUserResponse.data.id;
  const testUserEmail = createUserResponse.data.email;

  // Login as admin to create event
  await AuthHelpers.loginAs(page, 'admin');

  // Get first venue ID
  const venuesResponse = await apiRequest(page, 'GET', '/api/venues');
  const venueId = venuesResponse.data[0]?.id;

  if (!venueId) {
    throw new Error('No venues found - cannot create test event');
  }

  // Create event with 2 sessions
  const startDate = new Date();
  startDate.setDate(startDate.getDate() + 7);

  const session1Start = new Date(startDate);
  session1Start.setHours(18, 0, 0, 0);

  const session2Start = new Date(startDate);
  session2Start.setDate(session2Start.getDate() + 1);
  session2Start.setHours(18, 0, 0, 0);

  const eventData = {
    title: `Vol AutoCancel ${testName} ${timestamp}`,
    shortDescription: 'Test event for volunteer auto-cancellation',
    description: 'Tests automatic volunteer cancellation when ticket is cancelled.',
    eventType: 'Class',
    startDate: session1Start.toISOString(),
    endDate: session2Start.toISOString(),
    venueId: venueId,
    capacity: 20,
    isPublished: true,
    registrationOpenHours: null,
    registrationCloseHours: 0,
    cancellationCloseHours: 0,
    volunteerRegistrationCloseHours: 0,
    volunteerCancellationCloseHours: 0,
    sessions: [
      {
        sessionIdentifier: 'S1',
        name: 'Session 1',
        startTime: session1Start.toISOString(),
        endTime: new Date(session1Start.getTime() + 2 * 60 * 60 * 1000).toISOString(),
        capacity: 20,
      },
      {
        sessionIdentifier: 'S2',
        name: 'Session 2',
        startTime: session2Start.toISOString(),
        endTime: new Date(session2Start.getTime() + 2 * 60 * 60 * 1000).toISOString(),
        capacity: 20,
      },
    ],
    ticketTypes: [
      {
        name: 'Session 1 Ticket',
        pricingType: 'Fixed',
        price: 15.0,
        sessionIdentifiers: ['S1'],
      },
      {
        name: 'Session 2 Ticket',
        pricingType: 'Fixed',
        price: 15.0,
        sessionIdentifiers: ['S2'],
      },
    ],
    volunteerPositions: [
      {
        title: 'Session 1 Volunteer',
        description: 'Help with Session 1',
        slotsNeeded: 3,
        sessionIdentifier: 'S1',
      },
      {
        title: 'Session 2 Volunteer',
        description: 'Help with Session 2',
        slotsNeeded: 3,
        sessionIdentifier: 'S2',
      },
    ],
  };

  const createResponse = await apiRequest(page, 'POST', '/api/events', eventData);
  const testEventId = createResponse.data.id;

  const sessions = createResponse.data.sessions || [];
  const session1Id = sessions.find((s: any) => s.sessionIdentifier === 'S1')?.id;
  const session2Id = sessions.find((s: any) => s.sessionIdentifier === 'S2')?.id;

  const ticketTypes = createResponse.data.ticketTypes || [];
  const ticketTypeSession1Id = ticketTypes.find((t: any) => t.name === 'Session 1 Ticket')?.id;
  const ticketTypeSession2Id = ticketTypes.find((t: any) => t.name === 'Session 2 Ticket')?.id;

  const volunteerPositions = createResponse.data.volunteerPositions || [];
  const volunteerPosition1Id = volunteerPositions.find((v: any) => v.title === 'Session 1 Volunteer')?.id;
  const volunteerPosition2Id = volunteerPositions.find((v: any) => v.title === 'Session 2 Volunteer')?.id;

  // CRITICAL: Link volunteer positions to sessions by updating the event
  // The backend doesn't support sessionIdentifier for volunteer positions (only SessionId GUID)
  // We need to update the event with the correct SessionId values
  if (session1Id && session2Id && volunteerPosition1Id && volunteerPosition2Id) {
    const updatedVolunteerPositions = [
      {
        id: volunteerPosition1Id,
        title: 'Session 1 Volunteer',
        description: 'Help with Session 1',
        slotsNeeded: 3,
        slotsFilled: 0,
        sessionId: session1Id, // Use actual session GUID
      },
      {
        id: volunteerPosition2Id,
        title: 'Session 2 Volunteer',
        description: 'Help with Session 2',
        slotsNeeded: 3,
        slotsFilled: 0,
        sessionId: session2Id, // Use actual session GUID
      },
    ];

    await apiRequest(page, 'PUT', `/api/events/${testEventId}`, {
      volunteerPositions: updatedVolunteerPositions,
    });
  }

  return {
    testEventId,
    testUserId,
    testUserEmail,
    ticketTypeSession1Id,
    ticketTypeSession2Id,
    volunteerPosition1Id,
    volunteerPosition2Id,
    session1Id,
    session2Id,
  };
}

/**
 * Cleanup test data
 */
async function cleanupTestData(page: Page, testEventId: string, testUserId: string): Promise<void> {
  await AuthHelpers.loginAs(page, 'admin');
  await apiRequest(page, 'DELETE', `/api/events/${testEventId}`);
  await apiRequest(page, 'DELETE', `/api/test-helpers/users/${testUserId}`);
}

/**
 * Purchase a ticket via the checkout flow
 */
async function purchaseTicket(page: Page, testEventId: string, ticketTypeId: string): Promise<void> {
  await page.goto(`/checkout/${testEventId}?ticketTypeId=${ticketTypeId}`, {
    waitUntil: 'domcontentloaded',
  });
  await page.waitForLoadState('networkidle');

  const continueButton = page.locator('button').filter({ hasText: /continue|next/i }).last();
  if (await continueButton.isVisible({ timeout: 3000 }).catch(() => false)) {
    await continueButton.click();
    await page.waitForTimeout(500);
  }

  const termsCheckbox = page.getByRole('checkbox', { name: /agree.*waiver/i });
  await termsCheckbox.waitFor({ state: 'visible', timeout: 5000 });
  await termsCheckbox.click({ force: true });

  await page.waitForTimeout(500);

  const payButton = page.getByRole('button', { name: /pay with credit card/i });
  await payButton.waitFor({ state: 'visible', timeout: 5000 });
  await expect(payButton).toBeEnabled({ timeout: 3000 });
  await payButton.click();
  await page.waitForTimeout(3000);
}

/**
 * Sign up for a volunteer position via API
 */
async function signupForVolunteerPosition(page: Page, positionId: string): Promise<void> {
  await apiRequest(page, 'POST', `/api/volunteer-positions/${positionId}/signup`, {
    acceptedWaiver: true,
  });
}

test.describe('Volunteer Auto-Cancel on Ticket Cancellation', () => {
  test('cancelling Session 1 ticket auto-cancels Session 1 volunteer signup', async ({ page }) => {
    // SETUP: Create complete test data
    const setup = await createTestSetup(page, 'cancel-s1');

    try {
      // Login as test user (loginWith handles clearing previous auth state)
      await AuthHelpers.loginWith(page, { email: setup.testUserEmail, password: 'Test123!' });

      await purchaseTicket(page, setup.testEventId, setup.ticketTypeSession1Id);
      await purchaseTicket(page, setup.testEventId, setup.ticketTypeSession2Id);

      // Verify both tickets purchased
      const participationsResponse = await apiRequest(page, 'GET', '/api/user/participations');
      const ticketCount =
        participationsResponse.data?.filter(
          (p: any) => p.eventId === setup.testEventId && p.status === 'Active'
        ).length || 0;
      expect(ticketCount).toBe(2);

      // Sign up for volunteer positions in BOTH sessions
      await signupForVolunteerPosition(page, setup.volunteerPosition1Id);
      await signupForVolunteerPosition(page, setup.volunteerPosition2Id);

      // Verify volunteer signups before cancellation
      const positionsBefore = await apiRequest(
        page,
        'GET',
        `/api/events/${setup.testEventId}/volunteer-positions`
      );
      const positions = Array.isArray(positionsBefore.data) ? positionsBefore.data : [];
      const s1Before = positions.find((p: any) => p.title?.includes('Session 1'));
      const s2Before = positions.find((p: any) => p.title?.includes('Session 2'));

      // hasUserSignedUp is a boolean in the VolunteerPositionDto
      expect(s1Before?.hasUserSignedUp).toBe(true);
      expect(s2Before?.hasUserSignedUp).toBe(true);

      // Navigate to event detail page where ParticipationCard with cancel button is located
      await page.goto(`/events/${setup.testEventId}`, { waitUntil: 'domcontentloaded' });
      await page.waitForLoadState('networkidle');
      await page.waitForTimeout(1000);

      // Find and click the "Cancel Ticket" button on ParticipationCard
      const cancelButton = page.getByRole('button', { name: /cancel ticket/i });
      await expect(cancelButton).toBeVisible({ timeout: 10000 });
      await cancelButton.click();
      await page.waitForTimeout(1000);

      // In cancel mode, select only Session 1 ticket (first checkbox/session)
      // The ParticipationCard shows checkboxes for each session when in cancel mode
      const ticketCheckboxes = page.locator('input[type="checkbox"]');
      const checkboxCount = await ticketCheckboxes.count();

      if (checkboxCount > 1) {
        // Ensure only Session 1 is checked (first checkbox)
        // Uncheck Session 2 if checked
        const session2Checkbox = ticketCheckboxes.nth(1);
        if (await session2Checkbox.isChecked().catch(() => false)) {
          await session2Checkbox.click({ force: true });
        }
        // Ensure Session 1 is checked
        const session1Checkbox = ticketCheckboxes.first();
        if (!(await session1Checkbox.isChecked().catch(() => false))) {
          await session1Checkbox.click({ force: true });
        }
      }

      await page.waitForTimeout(500);

      // Confirm cancellation - look for confirm button in the cancel mode UI
      const confirmButton = page.getByRole('button', { name: /confirm.*cancel|yes.*cancel|cancel selected/i });
      await expect(confirmButton).toBeVisible({ timeout: 5000 });
      await confirmButton.click();
      await page.waitForTimeout(2000);

      // VERIFY: Session 1 volunteer signup is cancelled
      const positionsAfter = await apiRequest(
        page,
        'GET',
        `/api/events/${setup.testEventId}/volunteer-positions`
      );
      const positionsAfterArr = Array.isArray(positionsAfter.data) ? positionsAfter.data : [];
      const s1After = positionsAfterArr.find((p: any) => p.title?.includes('Session 1'));
      const s2After = positionsAfterArr.find((p: any) => p.title?.includes('Session 2'));

      // Session 1 volunteer should be cancelled (hasUserSignedUp should be false)
      expect(s1After?.hasUserSignedUp).toBe(false);

      // Session 2 volunteer should still be active
      expect(s2After?.hasUserSignedUp).toBe(true);
    } finally {
      // CLEANUP
      await cleanupTestData(page, setup.testEventId, setup.testUserId);
    }
  });

  test('cancelling Session 2 ticket preserves Session 1 volunteer signup', async ({ page }) => {
    // SETUP: Create complete test data
    const setup = await createTestSetup(page, 'cancel-s2');

    try {
      // Login as test user (loginWith handles clearing previous auth state)
      await AuthHelpers.loginWith(page, { email: setup.testUserEmail, password: 'Test123!' });

      await purchaseTicket(page, setup.testEventId, setup.ticketTypeSession1Id);
      await purchaseTicket(page, setup.testEventId, setup.ticketTypeSession2Id);

      // Sign up for volunteer positions in BOTH sessions
      await signupForVolunteerPosition(page, setup.volunteerPosition1Id);
      await signupForVolunteerPosition(page, setup.volunteerPosition2Id);

      // Navigate to event detail page where ParticipationCard with cancel button is located
      await page.goto(`/events/${setup.testEventId}`, { waitUntil: 'domcontentloaded' });
      await page.waitForLoadState('networkidle');
      await page.waitForTimeout(1000);

      // Find and click the "Cancel Ticket" button on ParticipationCard
      const cancelButton = page.getByRole('button', { name: /cancel ticket/i });
      await expect(cancelButton).toBeVisible({ timeout: 10000 });
      await cancelButton.click();
      await page.waitForTimeout(1000);

      // In cancel mode, select only Session 2 ticket (second checkbox/session)
      const ticketCheckboxes = page.locator('input[type="checkbox"]');
      const checkboxCount = await ticketCheckboxes.count();

      if (checkboxCount > 1) {
        // Ensure only Session 2 is checked (second checkbox)
        // Uncheck Session 1 if checked
        const session1Checkbox = ticketCheckboxes.first();
        if (await session1Checkbox.isChecked().catch(() => false)) {
          await session1Checkbox.click({ force: true });
        }
        // Ensure Session 2 is checked
        const session2Checkbox = ticketCheckboxes.nth(1);
        if (!(await session2Checkbox.isChecked().catch(() => false))) {
          await session2Checkbox.click({ force: true });
        }
      }

      await page.waitForTimeout(500);

      // Confirm cancellation - look for confirm button in the cancel mode UI
      const confirmButton = page.getByRole('button', { name: /confirm.*cancel|yes.*cancel|cancel selected/i });
      await expect(confirmButton).toBeVisible({ timeout: 5000 });
      await confirmButton.click();
      await page.waitForTimeout(2000);

      // VERIFY: Session 2 volunteer signup is cancelled, Session 1 preserved
      const positionsAfter = await apiRequest(
        page,
        'GET',
        `/api/events/${setup.testEventId}/volunteer-positions`
      );
      const positionsAfterArr = Array.isArray(positionsAfter.data) ? positionsAfter.data : [];
      const s1After = positionsAfterArr.find((p: any) => p.title?.includes('Session 1'));
      const s2After = positionsAfterArr.find((p: any) => p.title?.includes('Session 2'));

      // Session 1 volunteer should still be active
      expect(s1After?.hasUserSignedUp).toBe(true);

      // Session 2 volunteer should be cancelled (hasUserSignedUp should be false)
      expect(s2After?.hasUserSignedUp).toBe(false);
    } finally {
      // CLEANUP
      await cleanupTestData(page, setup.testEventId, setup.testUserId);
    }
  });

  test('cancelling all tickets cancels all volunteer signups', async ({ page }) => {
    // SETUP: Create complete test data
    const setup = await createTestSetup(page, 'cancel-all');

    try {
      // Login as test user (loginWith handles clearing previous auth state)
      await AuthHelpers.loginWith(page, { email: setup.testUserEmail, password: 'Test123!' });

      await purchaseTicket(page, setup.testEventId, setup.ticketTypeSession1Id);
      await purchaseTicket(page, setup.testEventId, setup.ticketTypeSession2Id);

      // Sign up for volunteer positions in BOTH sessions
      await signupForVolunteerPosition(page, setup.volunteerPosition1Id);
      await signupForVolunteerPosition(page, setup.volunteerPosition2Id);

      // Navigate to event detail page where ParticipationCard with cancel button is located
      await page.goto(`/events/${setup.testEventId}`, { waitUntil: 'domcontentloaded' });
      await page.waitForLoadState('networkidle');
      await page.waitForTimeout(1000);

      // Find and click the "Cancel Ticket" button on ParticipationCard
      const cancelButton = page.getByRole('button', { name: /cancel ticket/i });
      await expect(cancelButton).toBeVisible({ timeout: 10000 });
      await cancelButton.click();
      await page.waitForTimeout(1000);

      // In cancel mode, select ALL ticket checkboxes (both sessions)
      const ticketCheckboxes = page.locator('input[type="checkbox"]');
      const checkboxCount = await ticketCheckboxes.count();

      for (let i = 0; i < checkboxCount; i++) {
        const cb = ticketCheckboxes.nth(i);
        if (!(await cb.isChecked().catch(() => false))) {
          await cb.click({ force: true });
        }
      }

      await page.waitForTimeout(500);

      // Confirm cancellation - look for confirm button in the cancel mode UI
      const confirmButton = page.getByRole('button', { name: /confirm.*cancel|yes.*cancel|cancel selected/i });
      await expect(confirmButton).toBeVisible({ timeout: 5000 });
      await confirmButton.click();
      await page.waitForTimeout(2000);

      // VERIFY: Both volunteer signups are cancelled
      const positionsAfter = await apiRequest(
        page,
        'GET',
        `/api/events/${setup.testEventId}/volunteer-positions`
      );
      const positionsAfterArr = Array.isArray(positionsAfter.data) ? positionsAfter.data : [];
      const s1After = positionsAfterArr.find((p: any) => p.title?.includes('Session 1'));
      const s2After = positionsAfterArr.find((p: any) => p.title?.includes('Session 2'));

      // Both volunteer signups should be cancelled (hasUserSignedUp should be false)
      expect(s1After?.hasUserSignedUp).toBe(false);
      expect(s2After?.hasUserSignedUp).toBe(false);
    } finally {
      // CLEANUP
      await cleanupTestData(page, setup.testEventId, setup.testUserId);
    }
  });
});
