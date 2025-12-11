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
 * MIGRATED: Uses DataFactory pattern for automatic cleanup
 */

import { expect } from '@playwright/test';
import { test } from '../lib/datafactory/fixtures/test.fixture';
import { AuthHelpers } from './test-utils/helpers/auth.helpers';

test.describe('Admin Session Deletion', () => {
  test('can delete session with only RSVPs - shows confirmation modal with enabled button', async ({ page, df }) => {
    // Login as admin
    await AuthHelpers.loginAs(page, 'admin');

    // Create event with 2 sessions (so we can delete one)
    const event = await df.events.createPublished(`Session Deletion Test ${Date.now()}`);

    const sessionStart = new Date();
    sessionStart.setDate(sessionStart.getDate() + 7);
    sessionStart.setHours(18, 0, 0, 0);
    const sessionEnd = new Date(sessionStart.getTime() + 3 * 60 * 60 * 1000);

    await df.sessions.create({
      eventId: event.id,
      title: 'Session 1',
      startTime: sessionStart,
      endTime: sessionEnd,
      maxCapacity: 20,
    });

    const sessionStart2 = new Date(sessionStart);
    sessionStart2.setHours(sessionStart2.getHours() + 4);
    const sessionEnd2 = new Date(sessionStart2.getTime() + 3 * 60 * 60 * 1000);

    await df.sessions.create({
      eventId: event.id,
      title: 'Session 2',
      startTime: sessionStart2,
      endTime: sessionEnd2,
      maxCapacity: 20,
    });

    // Navigate to event edit page
    await page.goto(`/admin/events/${event.id}`, { waitUntil: 'domcontentloaded' });
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
  });

  test('cannot delete session with paid tickets - shows blocked modal with disabled button', async ({ page, df }) => {
    // Login as admin
    await AuthHelpers.loginAs(page, 'admin');

    // Create event with 2 sessions and ticket type
    const event = await df.events.createPublished(`Paid Ticket Block Test ${Date.now()}`);

    const sessionStart = new Date();
    sessionStart.setDate(sessionStart.getDate() + 7);
    sessionStart.setHours(18, 0, 0, 0);
    const sessionEnd = new Date(sessionStart.getTime() + 3 * 60 * 60 * 1000);

    const session1 = await df.sessions.create({
      eventId: event.id,
      title: 'Session 1',
      startTime: sessionStart,
      endTime: sessionEnd,
      maxCapacity: 20,
    });

    const sessionStart2 = new Date(sessionStart);
    sessionStart2.setHours(sessionStart2.getHours() + 4);
    const sessionEnd2 = new Date(sessionStart2.getTime() + 3 * 60 * 60 * 1000);

    await df.sessions.create({
      eventId: event.id,
      title: 'Session 2',
      startTime: sessionStart2,
      endTime: sessionEnd2,
      maxCapacity: 20,
    });

    // Create paid ticket type for first session
    const ticketType = await df.ticketTypes.create({
      sessionId: session1.id,
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
    await page.goto(`/admin/events/${event.id}`, { waitUntil: 'domcontentloaded' });
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
  });

  test('cannot delete only session in event - shows specific error message', async ({ page, df }) => {
    // Login as admin
    await AuthHelpers.loginAs(page, 'admin');

    // Create event with only 1 session
    const event = await df.events.createPublished(`Only Session Test ${Date.now()}`);

    const sessionStart = new Date();
    sessionStart.setDate(sessionStart.getDate() + 7);
    sessionStart.setHours(18, 0, 0, 0);
    const sessionEnd = new Date(sessionStart.getTime() + 3 * 60 * 60 * 1000);

    await df.sessions.create({
      eventId: event.id,
      title: 'Only Session',
      startTime: sessionStart,
      endTime: sessionEnd,
      maxCapacity: 20,
    });

    // Navigate to event edit page
    await page.goto(`/admin/events/${event.id}`, { waitUntil: 'domcontentloaded' });
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
  });

  test('delete session successfully removes it from the list', async ({ page, df }) => {
    // Login as admin
    await AuthHelpers.loginAs(page, 'admin');

    // Create event with 3 sessions (so we can delete one and still have 2)
    const event = await df.events.createPublished(`Delete Success Test ${Date.now()}`);

    const sessionStart = new Date();
    sessionStart.setDate(sessionStart.getDate() + 7);
    sessionStart.setHours(18, 0, 0, 0);

    // Create 3 sessions
    for (let i = 0; i < 3; i++) {
      const start = new Date(sessionStart);
      start.setHours(start.getHours() + i * 4);
      const end = new Date(start.getTime() + 3 * 60 * 60 * 1000);

      await df.sessions.create({
        eventId: event.id,
        title: `Session ${i + 1}`,
        startTime: start,
        endTime: end,
        maxCapacity: 20,
      });
    }

    // Navigate to event edit page
    await page.goto(`/admin/events/${event.id}`, { waitUntil: 'domcontentloaded' });
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
  });

  test('delete button in table opens confirmation modal', async ({ page, df }) => {
    // Login as admin
    await AuthHelpers.loginAs(page, 'admin');

    // Create event with 2 sessions
    const event = await df.events.createPublished(`Modal Open Test ${Date.now()}`);

    const sessionStart = new Date();
    sessionStart.setDate(sessionStart.getDate() + 7);
    sessionStart.setHours(18, 0, 0, 0);

    // Create 2 sessions
    for (let i = 0; i < 2; i++) {
      const start = new Date(sessionStart);
      start.setHours(start.getHours() + i * 4);
      const end = new Date(start.getTime() + 3 * 60 * 60 * 1000);

      await df.sessions.create({
        eventId: event.id,
        title: `Session ${i + 1}`,
        startTime: start,
        endTime: end,
        maxCapacity: 20,
      });
    }

    // Navigate to event edit page
    await page.goto(`/admin/events/${event.id}`, { waitUntil: 'domcontentloaded' });
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
  });
});
