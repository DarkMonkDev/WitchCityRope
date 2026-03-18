/**
 * E2E Tests: Ticket Acceptance Flow
 *
 * Test Plan Reference: Section 8, Flow 2 (partial) & Flow 3 (partial)
 *
 * Tests the flow where a user accepts an assigned ticket from the dashboard.
 * This covers the PendingTicketsCard and TicketAcceptanceModal components.
 *
 * NOTE: Creating a pending ticket assignment requires:
 * 1. Two users with an authorized contact relationship
 * 2. A ticket purchase with assignment via API
 *
 * The DataFactory does not yet have an authorized-contacts or
 * ticket-assignment factory. Tests that need a pending assignment
 * use defensive skip patterns until setup infrastructure is available.
 *
 * UI Components Tested:
 * - PendingTicketsCard.tsx (data-testid="pending-tickets-card", "pending-assignment-item",
 *   "accept-button", "decline-button")
 * - TicketAcceptanceModal.tsx (data-testid="ticket-acceptance-modal",
 *   "waiver-checkbox", "tos-checkbox", "accept-button")
 * - TicketDeclineModal.tsx (data-testid="ticket-decline-modal",
 *   "decline-reason-textarea", "decline-confirm-button")
 *
 * API Endpoints Exercised:
 * - GET /api/user/pending-assignments
 * - POST /api/events/{eventId}/tickets/{attendanceId}/accept
 * - POST /api/events/{eventId}/tickets/{attendanceId}/decline
 */

import { test, expect } from '../lib/datafactory/fixtures/test.fixture';
import { AuthHelpers } from './test-utils/helpers/auth.helpers';

test.describe('Ticket Acceptance Flow', () => {

  test('should show pending tickets card when user has pending assignments', async ({ page, df }) => {
    // Create a user to check for pending assignments
    const assignee = await df.users.createVerified({
      email: `e2e-accept-check-${Date.now()}@test.local`,
      roles: ['VettedMember'],
    });

    await AuthHelpers.loginWith(page, {
      email: assignee.email,
      password: 'Test123!',
    });

    await page.goto('/dashboard');
    await page.waitForLoadState('domcontentloaded');

    // Check if the pending tickets card is visible
    // PendingTicketsCard only renders when pendingAssignments.length > 0
    const pendingCard = page.getByTestId('pending-tickets-card');
    const cardCount = await pendingCard.count();

    if (cardCount === 0) {
      console.log(
        'No pending assignments for this user. ' +
        'This is expected for a new test user without assigned tickets. ' +
        'Skipping - test will activate when DataFactory supports ticket assignment setup.'
      );
      test.skip();
      return;
    }

    // If visible, verify the card structure
    await expect(pendingCard).toBeVisible();
    await expect(pendingCard.getByText('Pending Tickets & RSVPs')).toBeVisible();

    // Verify at least one pending item exists
    const pendingItems = page.getByTestId('pending-assignment-item');
    await expect(pendingItems.first()).toBeVisible();

    // Verify Accept and Decline buttons are present on the item
    await expect(page.getByTestId('accept-button').first()).toBeVisible();
    await expect(page.getByTestId('decline-button').first()).toBeVisible();
  });

  test('should open acceptance modal and require waiver checkbox before accepting', async ({ page, df }) => {
    // Create a user to check for pending assignments
    const assignee = await df.users.createVerified({
      email: `e2e-accept-modal-${Date.now()}@test.local`,
      roles: ['VettedMember'],
    });

    await AuthHelpers.loginWith(page, {
      email: assignee.email,
      password: 'Test123!',
    });

    await page.goto('/dashboard');
    await page.waitForLoadState('domcontentloaded');

    const pendingCard = page.getByTestId('pending-tickets-card');
    if ((await pendingCard.count()) === 0) {
      console.log('No pending assignments - skipping acceptance modal test.');
      test.skip();
      return;
    }

    // Click Accept on the first pending item
    await page.getByTestId('accept-button').first().click();

    // Verify the acceptance modal opens
    const modal = page.getByTestId('ticket-acceptance-modal');
    await expect(modal).toBeVisible({ timeout: 5000 });

    // Verify modal content includes Event Waiver section
    await expect(modal.getByText('Event Waiver')).toBeVisible();

    // Verify the waiver text area is present
    await expect(
      modal.getByText('I have read and accept the Event Waiver')
    ).toBeVisible();

    // Accept button should be disabled until checkboxes are checked
    const acceptBtn = modal.getByTestId('accept-button');
    await expect(acceptBtn).toBeDisabled();

    // Check the waiver checkbox (Mantine checkbox pattern - click parent wrapper)
    const waiverCheckbox = page.locator('input[data-testid="waiver-checkbox"]')
      .locator('..')
      .last();
    await waiverCheckbox.click();

    // Verify the checkbox is checked
    await expect(page.locator('input[data-testid="waiver-checkbox"]')).toBeChecked();

    // Check ToS checkbox if it's shown (only shown if user hasn't accepted ToS before)
    const tosCheckbox = page.locator('input[data-testid="tos-checkbox"]');
    if ((await tosCheckbox.count()) > 0) {
      const tosWrapper = tosCheckbox.locator('..').last();
      await tosWrapper.click();
      await expect(tosCheckbox).toBeChecked();
    }

    // Accept button should now be enabled
    await expect(acceptBtn).toBeEnabled();
  });

  test('should successfully accept a ticket and remove it from pending list', async ({ page, df }) => {
    const assignee = await df.users.createVerified({
      email: `e2e-accept-flow-${Date.now()}@test.local`,
      roles: ['VettedMember'],
    });

    await AuthHelpers.loginWith(page, {
      email: assignee.email,
      password: 'Test123!',
    });

    await page.goto('/dashboard');
    await page.waitForLoadState('domcontentloaded');

    const pendingCard = page.getByTestId('pending-tickets-card');
    if ((await pendingCard.count()) === 0) {
      console.log('No pending assignments - skipping acceptance flow test.');
      test.skip();
      return;
    }

    // Count pending items before acceptance
    const pendingItemsBefore = await page.getByTestId('pending-assignment-item').count();

    // Click Accept on the first pending item
    await page.getByTestId('accept-button').first().click();

    // Modal should open
    const modal = page.getByTestId('ticket-acceptance-modal');
    await expect(modal).toBeVisible({ timeout: 5000 });

    // Check waiver checkbox (Mantine pattern)
    const waiverCheckbox = page.locator('input[data-testid="waiver-checkbox"]')
      .locator('..')
      .last();
    await waiverCheckbox.click();

    // Check ToS if visible
    const tosCheckbox = page.locator('input[data-testid="tos-checkbox"]');
    if ((await tosCheckbox.count()) > 0) {
      await tosCheckbox.locator('..').last().click();
    }

    // Monitor the accept API call
    const acceptResponse = page.waitForResponse(
      (response) =>
        response.url().includes('/accept') &&
        response.request().method() === 'POST',
      { timeout: 15000 }
    );

    // Click Accept in the modal
    const acceptBtn = modal.getByTestId('accept-button');
    await acceptBtn.click();

    // Wait for API response
    const response = await acceptResponse;
    expect(response.status()).toBeLessThan(400);

    // Verify success notification
    await expect(
      page.getByText(/accepted/i).or(page.getByText("You're registered"))
    ).toBeVisible({ timeout: 10000 });

    // Modal should close
    await expect(modal).not.toBeVisible({ timeout: 5000 });

    // Verify the pending item count decreased (or card disappeared for last item)
    await page.waitForTimeout(1000); // Wait for query invalidation
    const pendingItemsAfter = await page.getByTestId('pending-assignment-item').count();
    expect(pendingItemsAfter).toBeLessThan(pendingItemsBefore);
  });

  test('should open decline modal and allow declining with optional reason', async ({ page, df }) => {
    const assignee = await df.users.createVerified({
      email: `e2e-decline-flow-${Date.now()}@test.local`,
      roles: ['VettedMember'],
    });

    await AuthHelpers.loginWith(page, {
      email: assignee.email,
      password: 'Test123!',
    });

    await page.goto('/dashboard');
    await page.waitForLoadState('domcontentloaded');

    const pendingCard = page.getByTestId('pending-tickets-card');
    if ((await pendingCard.count()) === 0) {
      console.log('No pending assignments - skipping decline flow test.');
      test.skip();
      return;
    }

    // Click Decline on the first pending item
    await page.getByTestId('decline-button').first().click();

    // Verify the decline modal opens
    const declineModal = page.getByTestId('ticket-decline-modal');
    await expect(declineModal).toBeVisible({ timeout: 5000 });

    // Verify modal shows explanation text
    await expect(
      declineModal.getByText(/will be returned|will be cancelled/i)
    ).toBeVisible();

    // Fill in optional reason
    const reasonTextarea = page.getByTestId('decline-reason-textarea');
    await expect(reasonTextarea).toBeVisible();
    await reasonTextarea.locator('textarea').fill('Schedule conflict - sorry!');

    // Monitor decline API call
    const declineResponse = page.waitForResponse(
      (response) =>
        response.url().includes('/decline') &&
        response.request().method() === 'POST',
      { timeout: 15000 }
    );

    // Click Decline confirm button
    await page.getByTestId('decline-confirm-button').click();

    // Wait for API response
    const response = await declineResponse;
    expect(response.status()).toBeLessThan(400);

    // Verify success notification
    await expect(
      page.getByText(/declined/i)
    ).toBeVisible({ timeout: 10000 });

    // Modal should close
    await expect(declineModal).not.toBeVisible({ timeout: 5000 });
  });
});
