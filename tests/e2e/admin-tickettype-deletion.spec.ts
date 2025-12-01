import { test, expect, Page } from '@playwright/test';
import { AuthHelpers } from './test-utils/helpers/auth.helpers';

/**
 * E2E Tests for Admin Ticket Type Deletion
 *
 * Tests verify ticket type deletion functionality including:
 * - Delete button visibility and interaction in ticket types grid
 * - Confirmation modal display with correct deletion state
 * - Blocking logic for ticket types with sales
 * - Success workflow for ticket types without sales
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

test.describe('Admin Ticket Type Deletion', () => {
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

  // Helper to create a test event with sessions and ticket types
  async function createTestEvent(sessionCount: number, ticketTypeCount: number): Promise<TestEvent> {
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
        description: 'Test event for ticket type deletion E2E tests',
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

  test('can delete ticket type with no sales - shows confirmation modal with enabled button', async () => {
    // Create event with 1 session and 2 ticket types (so we can delete one)
    const testEvent = await createTestEvent(1, 2);

    try {
      // Navigate to event edit page
      await page.goto(`/admin/events/${testEvent.id}`);
      await page.waitForLoadState('domcontentloaded');

      // Navigate to Setup tab
      const setupTab = page.getByRole('tab', { name: 'Sessions / Ticket Types' });
      await setupTab.click();

      // Wait for ticket types section
      const ticketsSection = page.locator('[data-testid="tickets-section"]');
      await expect(ticketsSection).toBeVisible({ timeout: 10000 });

      // Click delete on first ticket type
      const deleteButton = page.locator('[data-testid="button-delete-tickettype"]').first();
      await deleteButton.click();

      // Wait for modal
      await page.waitForTimeout(500);
      const modalTitle = page.getByRole('heading', { name: /Delete Ticket Type|Cannot Delete Ticket Type/i }).first();
      await expect(modalTitle).toBeVisible({ timeout: 10000 });

      const modal = page.locator('[data-testid="delete-confirmation-modal"]');

      // Verify confirm button is ENABLED (ticket type is deletable)
      const confirmButton = page.locator('[data-testid="delete-confirm-button"]');
      await expect(confirmButton).toBeEnabled();

      // Verify button text
      await expect(confirmButton).toHaveText(/Delete Ticket Type/i);

      // Close modal without deleting
      const cancelButton = page.locator('[data-testid="delete-cancel-button"]');
      await cancelButton.click();
      await expect(modal).not.toBeVisible({ timeout: 3000 });
    } finally {
      // Cleanup
      await deleteTestEvent(testEvent.id);
    }
  });

  test('cannot delete ticket type with sales - shows blocked modal with disabled button', async () => {
    // Create event with 1 session and 1 ticket type
    const testEvent = await createTestEvent(1, 1);

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

      // Wait for ticket types section
      const ticketsSection = page.locator('[data-testid="tickets-section"]');
      await expect(ticketsSection).toBeVisible({ timeout: 10000 });

      // Click delete on the ticket type
      const deleteButton = page.locator('[data-testid="button-delete-tickettype"]').first();
      await deleteButton.click();

      // Wait for modal
      await page.waitForTimeout(500);
      const modalTitle = page.getByRole('heading', { name: /Delete Ticket Type|Cannot Delete Ticket Type/i }).first();
      await expect(modalTitle).toBeVisible({ timeout: 10000 });

      const modal = page.locator('[data-testid="delete-confirmation-modal"]');

      // Verify blocked state - button should be DISABLED
      const confirmButton = page.locator('[data-testid="delete-confirm-button"]');
      await expect(confirmButton).toBeDisabled();

      // Verify alert shows blocking reason
      const alertBox = modal.locator('[role="alert"]');
      await expect(alertBox).toBeVisible();
      await expect(alertBox).toContainText(/tickets sold/i);

      // Verify blocking message
      await expect(modal).toContainText(/Cannot be deleted to protect member purchases/i);

      // Close modal
      const cancelButton = page.locator('[data-testid="delete-cancel-button"]');
      await cancelButton.click();
    } finally {
      // Cleanup
      await deleteTestEvent(testEvent.id);
    }
  });

  test('delete ticket type successfully removes it from the list', async () => {
    // Create event with 1 session and 3 ticket types
    const testEvent = await createTestEvent(1, 3);

    try {
      // Navigate to event edit page
      await page.goto(`/admin/events/${testEvent.id}`);
      await page.waitForLoadState('domcontentloaded');

      // Navigate to Setup tab
      const setupTab = page.getByRole('tab', { name: 'Sessions / Ticket Types' });
      await setupTab.click();

      // Wait for ticket types section
      const ticketsSection = page.locator('[data-testid="tickets-section"]');
      await expect(ticketsSection).toBeVisible({ timeout: 10000 });

      // Count ticket types before
      const ticketRowsBefore = page.locator('[data-testid="tickettype-row"]');
      const countBefore = await ticketRowsBefore.count();
      expect(countBefore).toBe(3);

      // Click delete on first ticket type
      const deleteButton = page.locator('[data-testid="button-delete-tickettype"]').first();
      await deleteButton.click();

      // Wait for modal
      await page.waitForTimeout(500);
      const modalTitle = page.getByRole('heading', { name: /Delete Ticket Type|Cannot Delete Ticket Type/i }).first();
      await expect(modalTitle).toBeVisible({ timeout: 10000 });

      const modal = page.locator('[data-testid="delete-confirmation-modal"]');

      // Confirm deletion
      const confirmButton = page.locator('[data-testid="delete-confirm-button"]');
      await expect(confirmButton).toBeEnabled();
      await confirmButton.click();

      // Wait for modal to close
      await expect(modal).not.toBeVisible({ timeout: 5000 });

      // Verify ticket type count decreased
      await page.waitForTimeout(500);
      const ticketRowsAfter = page.locator('[data-testid="tickettype-row"]');
      const countAfter = await ticketRowsAfter.count();
      expect(countAfter).toBe(countBefore - 1);
    } finally {
      // Cleanup
      await deleteTestEvent(testEvent.id);
    }
  });

  test('delete button in table opens confirmation modal', async () => {
    // Create event with 1 session and 2 ticket types
    const testEvent = await createTestEvent(1, 2);

    try {
      // Navigate to event edit page
      await page.goto(`/admin/events/${testEvent.id}`);
      await page.waitForLoadState('domcontentloaded');

      // Navigate to Setup tab
      const setupTab = page.getByRole('tab', { name: 'Sessions / Ticket Types' });
      await setupTab.click();

      // Wait for ticket types section
      const ticketsSection = page.locator('[data-testid="tickets-section"]');
      await expect(ticketsSection).toBeVisible({ timeout: 10000 });

      // Verify delete buttons exist
      const deleteButtons = page.locator('[data-testid="button-delete-tickettype"]');
      const buttonCount = await deleteButtons.count();
      expect(buttonCount).toBeGreaterThan(0);

      // Click first delete button
      await deleteButtons.first().click();

      // Wait for modal
      await page.waitForTimeout(500);

      // Verify modal opens with correct title
      const modalTitle = page.getByRole('heading', { name: /Delete Ticket Type|Cannot Delete Ticket Type/i }).first();
      await expect(modalTitle).toBeVisible({ timeout: 10000 });

      const modal = page.locator('[data-testid="delete-confirmation-modal"]');
      await expect(modal).toContainText(/Delete Ticket Type|Cannot Delete Ticket Type/i);

      // Close modal
      const cancelButton = page.locator('[data-testid="delete-cancel-button"]');
      await cancelButton.click();
      await expect(modal).not.toBeVisible({ timeout: 3000 });
    } finally {
      // Cleanup
      await deleteTestEvent(testEvent.id);
    }
  });

  test('ticket type deletion shows correct sales count in blocked modal', async () => {
    // Create event with 1 session and 1 ticket type
    const testEvent = await createTestEvent(1, 1);

    try {
      // Create 3 ticket purchases to show count
      if (testEvent.ticketTypes.length > 0) {
        await createTicketPurchase(testEvent.ticketTypes[0].id);
        await createTicketPurchase(testEvent.ticketTypes[0].id);
        await createTicketPurchase(testEvent.ticketTypes[0].id);
      }

      // Navigate to event edit page
      await page.goto(`/admin/events/${testEvent.id}`);
      await page.waitForLoadState('domcontentloaded');

      // Navigate to Setup tab
      const setupTab = page.getByRole('tab', { name: 'Sessions / Ticket Types' });
      await setupTab.click();

      // Wait for ticket types section
      const ticketsSection = page.locator('[data-testid="tickets-section"]');
      await expect(ticketsSection).toBeVisible({ timeout: 10000 });

      // Click delete
      const deleteButton = page.locator('[data-testid="button-delete-tickettype"]').first();
      await deleteButton.click();

      // Wait for modal
      await page.waitForTimeout(500);
      const modalTitle = page.getByRole('heading', { name: /Delete Ticket Type|Cannot Delete Ticket Type/i }).first();
      await expect(modalTitle).toBeVisible({ timeout: 10000 });

      const modal = page.locator('[data-testid="delete-confirmation-modal"]');

      // Verify sales count is shown (should be 3)
      const alertBox = modal.locator('[role="alert"]');
      const alertText = await alertBox.textContent();
      expect(alertText).toMatch(/3\s+tickets?\s+sold/i);

      // Close modal
      const cancelButton = page.locator('[data-testid="delete-cancel-button"]');
      await cancelButton.click();
    } finally {
      // Cleanup
      await deleteTestEvent(testEvent.id);
    }
  });
});
