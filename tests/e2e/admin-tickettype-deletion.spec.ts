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
 * MIGRATED: Uses DataFactory pattern for automatic cleanup
 */

import { expect } from '@playwright/test';
import { test } from '../lib/datafactory/fixtures/test.fixture';
import { AuthHelpers } from './test-utils/helpers/auth.helpers';

test.describe('Admin Ticket Type Deletion', () => {
  test('can delete ticket type with no sales - shows confirmation modal with enabled button', async ({ page, df }) => {
    // Login as admin
    await AuthHelpers.loginAs(page, 'admin');

    // Create event with 1 session and 2 ticket types (so we can delete one)
    const event = await df.events.createPublished(`Ticket Deletion Test ${Date.now()}`);

    const sessionStart = new Date();
    sessionStart.setDate(sessionStart.getDate() + 7);
    sessionStart.setHours(18, 0, 0, 0);
    const sessionEnd = new Date(sessionStart.getTime() + 3 * 60 * 60 * 1000);

    const session = await df.sessions.create({
      eventId: event.id,
      title: 'Test Session',
      startTime: sessionStart,
      endTime: sessionEnd,
      maxCapacity: 20,
    });

    // Create 2 ticket types
    await df.ticketTypes.create({
      eventId: event.id,
      sessionId: session.id,
      name: 'Ticket Type 1',
      price: 25.0,
      quantityAvailable: 20,
    });

    await df.ticketTypes.create({
      eventId: event.id,
      sessionId: session.id,
      name: 'Ticket Type 2',
      price: 30.0,
      quantityAvailable: 15,
    });

    // Navigate to event edit page
    await page.goto(`/admin/events/${event.id}`);
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
  });

  test('cannot delete ticket type with sales - shows blocked modal with disabled button', async ({ page, df }) => {
    // Login as admin
    await AuthHelpers.loginAs(page, 'admin');

    // Create event with 1 session and 1 ticket type
    const event = await df.events.createPublished(`Sales Block Test ${Date.now()}`);

    const sessionStart = new Date();
    sessionStart.setDate(sessionStart.getDate() + 7);
    sessionStart.setHours(18, 0, 0, 0);
    const sessionEnd = new Date(sessionStart.getTime() + 3 * 60 * 60 * 1000);

    const session = await df.sessions.create({
      eventId: event.id,
      title: 'Test Session',
      startTime: sessionStart,
      endTime: sessionEnd,
      maxCapacity: 20,
    });

    const ticketType = await df.ticketTypes.create({
      eventId: event.id,
      sessionId: session.id,
      name: 'Paid Ticket',
      price: 25.0,
      quantityAvailable: 20,
    });

    // Create test user and ticket purchase to block deletion
    const user = await df.users.createVerified({ email: `test-${Date.now()}@example.com` });
    await df.ticketPurchases.create({
      userId: user.id,
      ticketTypeId: ticketType.id,
      quantity: 1,
    });

    // Navigate to event edit page
    await page.goto(`/admin/events/${event.id}`);
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
  });

  test('delete ticket type successfully removes it from the list', async ({ page, df }) => {
    // Login as admin
    await AuthHelpers.loginAs(page, 'admin');

    // Create event with 1 session and 3 ticket types
    const event = await df.events.createPublished(`Delete Success Test ${Date.now()}`);

    const sessionStart = new Date();
    sessionStart.setDate(sessionStart.getDate() + 7);
    sessionStart.setHours(18, 0, 0, 0);
    const sessionEnd = new Date(sessionStart.getTime() + 3 * 60 * 60 * 1000);

    const session = await df.sessions.create({
      eventId: event.id,
      title: 'Test Session',
      startTime: sessionStart,
      endTime: sessionEnd,
      maxCapacity: 20,
    });

    // Create 3 ticket types
    for (let i = 1; i <= 3; i++) {
      await df.ticketTypes.create({
        eventId: event.id,
      sessionId: session.id,
        name: `Ticket Type ${i}`,
        price: 20.0 + i * 5,
        quantityAvailable: 20,
      });
    }

    // Navigate to event edit page
    await page.goto(`/admin/events/${event.id}`);
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
  });

  test('delete button in table opens confirmation modal', async ({ page, df }) => {
    // Login as admin
    await AuthHelpers.loginAs(page, 'admin');

    // Create event with 1 session and 2 ticket types
    const event = await df.events.createPublished(`Modal Open Test ${Date.now()}`);

    const sessionStart = new Date();
    sessionStart.setDate(sessionStart.getDate() + 7);
    sessionStart.setHours(18, 0, 0, 0);
    const sessionEnd = new Date(sessionStart.getTime() + 3 * 60 * 60 * 1000);

    const session = await df.sessions.create({
      eventId: event.id,
      title: 'Test Session',
      startTime: sessionStart,
      endTime: sessionEnd,
      maxCapacity: 20,
    });

    // Create 2 ticket types
    for (let i = 1; i <= 2; i++) {
      await df.ticketTypes.create({
        eventId: event.id,
      sessionId: session.id,
        name: `Ticket Type ${i}`,
        price: 25.0,
        quantityAvailable: 20,
      });
    }

    // Navigate to event edit page
    await page.goto(`/admin/events/${event.id}`);
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
  });

  test('ticket type deletion shows correct sales count in blocked modal', async ({ page, df }) => {
    // Login as admin
    await AuthHelpers.loginAs(page, 'admin');

    // Create event with 1 session and 1 ticket type
    const event = await df.events.createPublished(`Sales Count Test ${Date.now()}`);

    const sessionStart = new Date();
    sessionStart.setDate(sessionStart.getDate() + 7);
    sessionStart.setHours(18, 0, 0, 0);
    const sessionEnd = new Date(sessionStart.getTime() + 3 * 60 * 60 * 1000);

    const session = await df.sessions.create({
      eventId: event.id,
      title: 'Test Session',
      startTime: sessionStart,
      endTime: sessionEnd,
      maxCapacity: 20,
    });

    const ticketType = await df.ticketTypes.create({
      eventId: event.id,
      sessionId: session.id,
      name: 'Ticket with Sales',
      price: 25.0,
      quantityAvailable: 20,
    });

    // Create test user and 3 ticket purchases to show count
    const user = await df.users.createVerified({ email: `test-${Date.now()}@example.com` });
    for (let i = 0; i < 3; i++) {
      await df.ticketPurchases.create({
        userId: user.id,
        ticketTypeId: ticketType.id,
        quantity: 1,
      });
    }

    // Navigate to event edit page
    await page.goto(`/admin/events/${event.id}`);
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
  });
});
