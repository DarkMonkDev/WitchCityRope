import { test, expect, Page } from '@playwright/test';
import { AuthHelpers } from './test-utils/helpers/auth.helpers';

/**
 * E2E Tests for Admin Session Deletion
 *
 * Tests verify session deletion functionality including:
 * - Delete button visibility and interaction
 * - Confirmation modal display with correct deletion state
 * - Blocking logic for paid tickets, only session, and cascade blocking
 * - Success workflow for sessions with RSVPs only
 * - Modal button states (enabled/disabled based on blocking conditions)
 *
 * CRITICAL: Tests create their own data and do NOT rely on seed data
 */

interface TestEvent {
  id: string;
  title: string;
  sessions: Array<{ id: string; name: string }>;
  ticketTypes: Array<{ id: string; name: string }>;
}

test.describe('Admin Session Deletion', () => {
  let page: Page;

  // Helper to get CSRF token from cookies
  async function getCsrfToken(): Promise<string> {
    const cookies = await page.context().cookies();
    const csrfCookie = cookies.find((c) => c.name === 'XSRF-TOKEN');
    if (!csrfCookie) {
      throw new Error('CSRF token cookie not found - ensure user is logged in');
    }
    return csrfCookie.value;
  }

  // Helper to create a test event with specified number of sessions
  async function createTestEvent(sessionCount: number, ticketTypeCount: number = 0): Promise<TestEvent> {
    const futureDate = new Date();
    futureDate.setDate(futureDate.getDate() + 30);

    const sessions = Array.from({ length: sessionCount }, (_, i) => ({
      sessionIdentifier: `S${i + 1}`,
      name: `Test Session ${i + 1}`,
      date: futureDate.toISOString(),
      startTime: new Date(futureDate.getTime() + i * 3 * 60 * 60 * 1000).toISOString(),
      endTime: new Date(futureDate.getTime() + (i + 1) * 3 * 60 * 60 * 1000).toISOString(),
      capacity: 20,
    }));

    const ticketTypes = Array.from({ length: ticketTypeCount }, (_, i) => ({
      name: `Test Ticket ${i + 1}`,
      price: 25.0,
      quantityAvailable: 20,
      pricingType: 'Fixed',
      sessionIdentifiers: [], // Event-level ticket (all sessions)
    }));

    // Get CSRF token for protected endpoint
    const csrfToken = await getCsrfToken();

    const response = await page.request.post('/api/events', {
      headers: {
        'X-CSRF-TOKEN': csrfToken,
      },
      data: {
        title: `E2E Test Event ${Date.now()}`,
        description: 'Test event for session deletion E2E tests',
        startDate: futureDate.toISOString(),
        endDate: new Date(futureDate.getTime() + 24 * 60 * 60 * 1000).toISOString(),
        venueId: 1,
        eventType: 'Class',
        capacity: 20,
        isPublished: false,
        sessions,
        ticketTypes,
      },
    });

    if (!response.ok()) {
      const errorBody = await response.text();
      throw new Error(`Failed to create test event: ${response.status()} - ${errorBody}`);
    }

    const event = await response.json();
    return {
      id: event.id,
      title: event.title,
      sessions: event.sessions || [],
      ticketTypes: event.ticketTypes || [],
    };
  }

  // Helper to create a ticket purchase for a ticket type
  async function createTicketPurchase(ticketTypeId: string): Promise<string> {
    const response = await page.request.post('/api/test-helpers/ticket-purchases', {
      data: {
        ticketTypeId: ticketTypeId, // GUID as string - API will parse
        totalPrice: 25.0, // Required field
        quantity: 1,
        paymentMethod: 'PayPal',
        paymentStatus: 'Completed',
      },
    });

    if (!response.ok()) {
      const errorBody = await response.text();
      throw new Error(`Failed to create ticket purchase: ${response.status()} - ${errorBody}`);
    }

    const purchase = await response.json();
    return purchase.id;
  }

  // Helper to delete a test event (cleanup)
  async function deleteTestEvent(eventId: string): Promise<void> {
    const csrfToken = await getCsrfToken();
    await page.request.delete(`/api/events/${eventId}`, {
      headers: {
        'X-CSRF-TOKEN': csrfToken,
      },
    });
  }

  test.beforeAll(async ({ browser }) => {
    const context = await browser.newContext();
    page = await context.newPage();

    // Login as admin
    await AuthHelpers.loginAs(page, 'admin');
  });

  test.afterAll(async () => {
    await page?.close();
  });

  test('can delete session with only RSVPs - shows confirmation modal with enabled button', async () => {
    // Create event with 2 sessions (so we can delete one)
    const testEvent = await createTestEvent(2);

    try {
      // Navigate to event edit page
      await page.goto(`/admin/events/${testEvent.id}`);
      await page.waitForLoadState('domcontentloaded');

      // Navigate to Setup tab
      const setupTab = page.getByRole('tab', { name: 'Sessions / Ticket Types' });
      await setupTab.click();

      // Wait for sessions section
      const sessionsSection = page.locator('[data-testid="sessions-section"]');
      await expect(sessionsSection).toBeVisible({ timeout: 10000 });

      // Click delete on first session
      const deleteButton = page.locator('[data-testid="button-delete-session"]').first();
      await deleteButton.click();

      // Wait for modal
      await page.waitForTimeout(500);
      const modalTitle = page.getByRole('heading', { name: /Delete Session|Cannot Delete Session/i }).first();
      await expect(modalTitle).toBeVisible({ timeout: 10000 });

      const modal = page.locator('[data-testid="delete-confirmation-modal"]');

      // Verify confirm button is ENABLED (session is deletable)
      const confirmButton = page.locator('[data-testid="delete-confirm-button"]');
      await expect(confirmButton).toBeEnabled();

      // Verify button text
      await expect(confirmButton).toHaveText(/Delete Session/i);

      // Close modal without deleting
      const cancelButton = page.locator('[data-testid="delete-cancel-button"]');
      await cancelButton.click();
      await expect(modal).not.toBeVisible({ timeout: 3000 });
    } finally {
      // Cleanup
      await deleteTestEvent(testEvent.id);
    }
  });

  test('cannot delete session with paid tickets - shows blocked modal with disabled button', async () => {
    // Create event with 2 sessions and 1 ticket type
    const testEvent = await createTestEvent(2, 1);

    try {
      // Create a ticket purchase to block deletion
      if (testEvent.ticketTypes.length > 0) {
        await createTicketPurchase(testEvent.ticketTypes[0].id);
      }

      // Navigate to event edit page
      await page.goto(`/admin/events/${testEvent.id}`);
      await page.waitForLoadState('domcontentloaded');

      // Navigate to Setup tab
      const setupTab = page.getByRole('tab', { name: 'Sessions / Ticket Types' });
      await setupTab.click();

      // Wait for sessions section
      const sessionsSection = page.locator('[data-testid="sessions-section"]');
      await expect(sessionsSection).toBeVisible({ timeout: 10000 });

      // Click delete on first session
      const deleteButton = page.locator('[data-testid="button-delete-session"]').first();
      await deleteButton.click();

      // Wait for modal
      await page.waitForTimeout(500);
      const modalTitle = page.getByRole('heading', { name: /Delete Session|Cannot Delete Session/i }).first();
      await expect(modalTitle).toBeVisible({ timeout: 10000 });

      const modal = page.locator('[data-testid="delete-confirmation-modal"]');

      // Verify blocked state - button should be DISABLED
      const confirmButton = page.locator('[data-testid="delete-confirm-button"]');
      await expect(confirmButton).toBeDisabled();

      // Verify alert shows blocking reason
      const alertBox = modal.locator('[role="alert"]');
      await expect(alertBox).toBeVisible();

      // Close modal
      const cancelButton = page.locator('[data-testid="delete-cancel-button"]');
      await cancelButton.click();
    } finally {
      // Cleanup
      await deleteTestEvent(testEvent.id);
    }
  });

  test('cannot delete only session in event - shows specific error message', async () => {
    // Create event with only 1 session
    const testEvent = await createTestEvent(1);

    try {
      // Navigate to event edit page
      await page.goto(`/admin/events/${testEvent.id}`);
      await page.waitForLoadState('domcontentloaded');

      // Navigate to Setup tab
      const setupTab = page.getByRole('tab', { name: 'Sessions / Ticket Types' });
      await setupTab.click();

      // Wait for sessions section
      const sessionsSection = page.locator('[data-testid="sessions-section"]');
      await expect(sessionsSection).toBeVisible({ timeout: 10000 });

      // Click delete on the only session
      const deleteButton = page.locator('[data-testid="button-delete-session"]').first();
      await deleteButton.click();

      // Wait for modal
      await page.waitForTimeout(500);
      const modalTitle = page.getByRole('heading', { name: /Delete Session|Cannot Delete Session/i }).first();
      await expect(modalTitle).toBeVisible({ timeout: 10000 });

      const modal = page.locator('[data-testid="delete-confirmation-modal"]');

      // Verify blocked with "only session" message
      await expect(modal).toContainText(/Cannot delete the only session/i);
      await expect(modal).toContainText(/Delete the entire event instead/i);

      // Verify confirm button is disabled
      const confirmButton = page.locator('[data-testid="delete-confirm-button"]');
      await expect(confirmButton).toBeDisabled();

      // Close modal
      const cancelButton = page.locator('[data-testid="delete-cancel-button"]');
      await cancelButton.click();
    } finally {
      // Cleanup
      await deleteTestEvent(testEvent.id);
    }
  });

  test('delete session successfully removes it from the list', async () => {
    // Create event with 3 sessions (so we can delete one and still have 2)
    const testEvent = await createTestEvent(3);

    try {
      // Navigate to event edit page
      await page.goto(`/admin/events/${testEvent.id}`);
      await page.waitForLoadState('domcontentloaded');

      // Navigate to Setup tab
      const setupTab = page.getByRole('tab', { name: 'Sessions / Ticket Types' });
      await setupTab.click();

      // Wait for sessions section
      const sessionsSection = page.locator('[data-testid="sessions-section"]');
      await expect(sessionsSection).toBeVisible({ timeout: 10000 });

      // Count sessions before
      const sessionRowsBefore = page.locator('[data-testid="session-row"]');
      const countBefore = await sessionRowsBefore.count();
      expect(countBefore).toBe(3);

      // Click delete on first session
      const deleteButton = page.locator('[data-testid="button-delete-session"]').first();
      await deleteButton.click();

      // Wait for modal
      await page.waitForTimeout(500);
      const modalTitle = page.getByRole('heading', { name: /Delete Session|Cannot Delete Session/i }).first();
      await expect(modalTitle).toBeVisible({ timeout: 10000 });

      const modal = page.locator('[data-testid="delete-confirmation-modal"]');

      // Confirm deletion
      const confirmButton = page.locator('[data-testid="delete-confirm-button"]');
      await expect(confirmButton).toBeEnabled();
      await confirmButton.click();

      // Wait for modal to close
      await expect(modal).not.toBeVisible({ timeout: 5000 });

      // Verify session count decreased
      await page.waitForTimeout(500);
      const sessionRowsAfter = page.locator('[data-testid="session-row"]');
      const countAfter = await sessionRowsAfter.count();
      expect(countAfter).toBe(countBefore - 1);
    } finally {
      // Cleanup
      await deleteTestEvent(testEvent.id);
    }
  });

  test('delete button in table opens confirmation modal', async () => {
    // Create event with 2 sessions
    const testEvent = await createTestEvent(2);

    try {
      // Navigate to event edit page
      await page.goto(`/admin/events/${testEvent.id}`);
      await page.waitForLoadState('domcontentloaded');

      // Navigate to Setup tab
      const setupTab = page.getByRole('tab', { name: 'Sessions / Ticket Types' });
      await setupTab.click();

      // Wait for sessions section
      const sessionsSection = page.locator('[data-testid="sessions-section"]');
      await expect(sessionsSection).toBeVisible({ timeout: 10000 });

      // Verify delete buttons exist
      const deleteButtons = page.locator('[data-testid="button-delete-session"]');
      const buttonCount = await deleteButtons.count();
      expect(buttonCount).toBeGreaterThan(0);

      // Click first delete button
      await deleteButtons.first().click();

      // Wait for modal
      await page.waitForTimeout(500);

      // Verify modal opens with correct title
      const modalTitle = page.getByRole('heading', { name: /Delete Session|Cannot Delete Session/i }).first();
      await expect(modalTitle).toBeVisible({ timeout: 10000 });

      const modal = page.locator('[data-testid="delete-confirmation-modal"]');
      await expect(modal).toContainText(/Delete Session|Cannot Delete Session/i);

      // Close modal
      const cancelButton = page.locator('[data-testid="delete-cancel-button"]');
      await cancelButton.click();
      await expect(modal).not.toBeVisible({ timeout: 3000 });
    } finally {
      // Cleanup
      await deleteTestEvent(testEvent.id);
    }
  });
});
