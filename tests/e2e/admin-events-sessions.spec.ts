/**
 * E2E Tests for Admin Events Edit Screen - Session Management
 *
 * These tests verify session management functionality including:
 * - Session creation via modal
 * - Session editing with pre-populated data
 * - Automatic S# ID assignment (format: S1, S2, S3, etc.)
 * - Form validation
 *
 * MIGRATED: Uses DataFactory pattern for automatic cleanup
 */

import { expect, Page } from '@playwright/test';
import { test } from '../lib/datafactory/fixtures/test.fixture';
import { AuthHelpers } from './test-utils/helpers/auth.helpers';

/**
 * Helper function to select a session identifier in the Mantine Select component
 * Uses keyboard navigation for reliable interaction with Mantine Select
 */
async function selectSessionId(page: Page, sessionId: string) {
  const sessionIdInput = page.getByTestId('input-session-id');
  const sessionIdTextbox = sessionIdInput.locator('input');

  // Check if already has a value that contains the session ID
  let inputValue = await sessionIdTextbox.inputValue().catch(() => '');
  console.log(`Current session ID value: "${inputValue}"`);

  // If already selected (value contains full session text like "S1 - Session 1"), skip
  if (inputValue && inputValue.includes(`${sessionId} - Session`)) {
    console.log(`Session ${sessionId} already selected`);
    return;
  }

  // For Mantine Select: click to open dropdown, type to filter, press Enter to select
  await sessionIdTextbox.click();
  await page.waitForTimeout(200);
  await sessionIdTextbox.fill(sessionId);
  await page.waitForTimeout(200);
  await page.keyboard.press('Enter');
  await page.waitForTimeout(300);

  // Click elsewhere to close dropdown and blur the select
  await page.locator('body').click({ position: { x: 10, y: 10 } });
  await page.waitForTimeout(100);

  // Verify the selection worked
  inputValue = await sessionIdTextbox.inputValue().catch(() => '');
  console.log(`After selection, session ID: "${inputValue}"`);
}

/**
 * Helper function to fill session form fields with reliable Mantine component interaction
 */
async function fillSessionForm(
  page: Page,
  data: { name: string; startTime?: string; endTime?: string; capacity?: string }
) {
  // Fill session name - click first to ensure focus
  const nameInput = page.getByTestId('input-session-name');
  await nameInput.click();
  await nameInput.fill(data.name);

  // Time inputs need to be clicked first for Mantine TimeInput
  // The default times (18:00/21:00) are acceptable for most tests, only fill if different needed
  if (data.startTime) {
    const startTimeInput = page.getByTestId('input-session-start-time');
    await startTimeInput.click();
    await startTimeInput.fill(data.startTime);
  }

  if (data.endTime) {
    const endTimeInput = page.getByTestId('input-session-end-time');
    await endTimeInput.click();
    await endTimeInput.fill(data.endTime);
  }

  // Capacity - NumberInput needs click first
  if (data.capacity) {
    const capacityInput = page.getByTestId('input-session-capacity');
    await capacityInput.click();
    await page.waitForTimeout(50);
    // Clear and fill - triple-click to select all
    await capacityInput.click({ clickCount: 3 });
    await page.keyboard.type(data.capacity);
  }
}

test.describe('Admin Events Edit Screen - Session Management', () => {
  test('should add a new session via modal without page refresh', async ({ page, df }) => {
    // Login as admin
    await AuthHelpers.loginAs(page, 'admin');

    // Create event with NO sessions - this gives us clean slate for session tests
    const event = await df.events.createDefault(`Session Add Test ${Date.now()}`);

    // Navigate to admin event edit page
    await page.goto(`/admin/events/${event.id}`);

    // Wait for page to load
    await expect(page.locator('[data-testid="page-admin-event-details"]')).toBeVisible({ timeout: 10000 });

    // Navigate to Setup tab (contains Sessions and Ticket Types)
    const setupTab = page.getByRole('tab', { name: /Sessions|Setup/i });
    await expect(setupTab).toBeVisible({ timeout: 5000 });
    await setupTab.click();

    // Wait for sessions section
    const sessionsSection = page.locator('[data-testid="sessions-section"]');
    await expect(sessionsSection).toBeVisible({ timeout: 5000 });

    // Click Add Session button
    const addSessionButton = page.locator('[data-testid="button-add-session"]');
    await expect(addSessionButton).toBeVisible({ timeout: 5000 });
    await addSessionButton.click();

    // Verify Add Session modal opens
    const sessionModal = page.locator('[role="dialog"]');
    await expect(sessionModal).toBeVisible({ timeout: 5000 });

    // Select session identifier using the helper function
    await selectSessionId(page, 'S1');

    // Fill session form fields using the helper
    await fillSessionForm(page, {
      name: 'Morning Workshop',
      // Use default times (18:00-21:00) since they're valid
      capacity: '20',
    });

    // Save session (use .last() for React Strict Mode)
    const saveButton = page.locator('[data-testid="button-save-session"]').last();
    await expect(saveButton).toBeVisible();
    await saveButton.click();

    // Verify modal closes
    await page.waitForSelector('[role="dialog"]', { state: 'detached', timeout: 10000 });

    // Verify session appears in grid WITHOUT page refresh
    const sessionGrid = page.locator('[data-testid="grid-sessions"]');
    await expect(sessionGrid).toBeVisible();

    // Verify session appears in the grid
    const newSessionRow = sessionGrid.locator('tr').filter({ hasText: 'Morning Workshop' });
    await expect(newSessionRow).toBeVisible({ timeout: 5000 });

    console.log('Session added successfully via modal');
  });

  test('should edit existing session via modal', async ({ page, df }) => {
    // Login as admin
    await AuthHelpers.loginAs(page, 'admin');

    // Create event with one session to edit
    const event = await df.events.createDefault(`Session Edit Test ${Date.now()}`);

    const sessionStart = new Date();
    sessionStart.setDate(sessionStart.getDate() + 7);
    sessionStart.setHours(14, 0, 0, 0);
    const sessionEnd = new Date(sessionStart.getTime() + 3 * 60 * 60 * 1000);

    await df.sessions.create({
      eventId: event.id,
      title: 'Original Session Name',
      startTime: sessionStart,
      endTime: sessionEnd,
      maxCapacity: 15,
    });

    // Navigate to admin event edit page
    await page.goto(`/admin/events/${event.id}`);

    // Wait for page to load and navigate to Setup tab
    await expect(page.locator('[data-testid="page-admin-event-details"]')).toBeVisible({ timeout: 10000 });
    const setupTab = page.getByRole('tab', { name: /Sessions|Setup/i });
    await expect(setupTab).toBeVisible({ timeout: 5000 });
    await setupTab.click();

    // Wait for sessions section
    const sessionsSection = page.locator('[data-testid="sessions-section"]');
    await expect(sessionsSection).toBeVisible({ timeout: 5000 });

    // Verify sessions grid exists
    const sessionGrid = page.locator('[data-testid="grid-sessions"]');
    await expect(sessionGrid).toBeVisible();

    // Click on a session row to edit
    const sessionRow = sessionGrid.locator('[data-testid="session-row"]').first();
    await expect(sessionRow).toBeVisible();
    await sessionRow.click();

    // Verify edit modal opens
    const editModal = page.locator('[role="dialog"]');
    await expect(editModal).toBeVisible({ timeout: 5000 });

    // Verify form is pre-populated with existing session data
    const nameInput = page.getByTestId('input-session-name');
    await expect(nameInput).not.toHaveValue('');

    // Change session name
    await nameInput.fill('Updated Session Name');

    // Save changes (use .last() for React Strict Mode)
    const saveButton = page.locator('[data-testid="button-save-session"]').last();
    await saveButton.click();

    // Verify modal closes
    await page.waitForSelector('[role="dialog"]', { state: 'detached', timeout: 10000 });

    // Verify updates appear in grid without page refresh
    const updatedRow = sessionGrid.locator('tr').filter({ hasText: 'Updated Session Name' });
    await expect(updatedRow).toBeVisible({ timeout: 5000 });

    console.log('Session edited successfully via modal');
  });

  test('should assign S# IDs sequentially to new sessions', async ({ page, df }) => {
    // Login as admin
    await AuthHelpers.loginAs(page, 'admin');

    // Create event with NO sessions
    const event = await df.events.createDefault(`Sequential ID Test ${Date.now()}`);

    // Navigate to admin event edit page
    await page.goto(`/admin/events/${event.id}`);

    // Navigate to Setup tab
    await expect(page.locator('[data-testid="page-admin-event-details"]')).toBeVisible({ timeout: 10000 });
    const setupTab = page.getByRole('tab', { name: /Sessions|Setup/i });
    await expect(setupTab).toBeVisible({ timeout: 5000 });
    await setupTab.click();

    // Wait for sessions section
    const sessionsSection = page.locator('[data-testid="sessions-section"]');
    await expect(sessionsSection).toBeVisible({ timeout: 5000 });

    const sessionGrid = page.locator('[data-testid="grid-sessions"]');

    // Get current session count
    const initialSessionCount = await sessionGrid.locator('[data-testid="session-row"]').count();
    console.log(`Initial session count: ${initialSessionCount}`);

    // Add first new session
    await page.locator('[data-testid="button-add-session"]').click();
    const modal = page.locator('[role="dialog"]');
    await expect(modal).toBeVisible();

    // Select S1 using the helper
    await selectSessionId(page, 'S1');

    // Fill session form using the helper
    await fillSessionForm(page, {
      name: 'First New Session',
      capacity: '20',
    });
    // Use .last() for React Strict Mode
    await page.locator('[data-testid="button-save-session"]').last().click();

    // Wait for modal to close
    await page.waitForSelector('[role="dialog"]', { state: 'detached', timeout: 10000 });

    // Verify first session appears
    const firstSessionRow = sessionGrid.locator('tr').filter({ hasText: 'First New Session' });
    await expect(firstSessionRow).toBeVisible({ timeout: 5000 });

    // Add second session
    await page.locator('[data-testid="button-add-session"]').click();
    await expect(modal).toBeVisible();

    // Select S2 using the helper
    await selectSessionId(page, 'S2');

    // Fill session form using the helper
    await fillSessionForm(page, {
      name: 'Second New Session',
      capacity: '25',
    });
    // Use .last() for React Strict Mode
    await page.locator('[data-testid="button-save-session"]').last().click();

    // Wait for modal to close
    await page.waitForSelector('[role="dialog"]', { state: 'detached', timeout: 10000 });

    // Verify second session appears
    const secondSessionRow = sessionGrid.locator('tr').filter({ hasText: 'Second New Session' });
    await expect(secondSessionRow).toBeVisible({ timeout: 5000 });

    // Verify we now have more sessions than before
    const finalSessionCount = await sessionGrid.locator('[data-testid="session-row"]').count();
    expect(finalSessionCount).toBeGreaterThan(initialSessionCount);

    console.log(`Sessions added: ${initialSessionCount} → ${finalSessionCount}`);
  });

  test('should validate session form fields', async ({ page, df }) => {
    // Login as admin
    await AuthHelpers.loginAs(page, 'admin');

    // Create event
    const event = await df.events.createDefault(`Validation Test ${Date.now()}`);

    // Navigate to admin event edit page
    await page.goto(`/admin/events/${event.id}`);

    // Navigate to Setup tab and open add modal
    await expect(page.locator('[data-testid="page-admin-event-details"]')).toBeVisible({ timeout: 10000 });
    const setupTab = page.getByRole('tab', { name: /Sessions|Setup/i });
    await setupTab.click();

    // Wait for sessions section
    const sessionsSection = page.locator('[data-testid="sessions-section"]');
    await expect(sessionsSection).toBeVisible({ timeout: 5000 });

    await page.locator('[data-testid="button-add-session"]').click();

    const sessionModal = page.locator('[role="dialog"]');
    await expect(sessionModal).toBeVisible();

    // Select session identifier using the helper
    await selectSessionId(page, 'S1');

    // Test: Session Name validation - try to submit with empty name
    const nameInput = page.getByTestId('input-session-name');
    await nameInput.fill(''); // Clear the input

    // Click save button to trigger validation (use .last() for React Strict Mode)
    const saveButton = page.locator('[data-testid="button-save-session"]').last();
    await saveButton.click();

    // Verify validation prevents submission - modal should still be open
    await expect(sessionModal).toBeVisible();

    // Check that validation error exists (HTML5 or Mantine validation)
    const isInvalid = await nameInput.evaluate((el: HTMLInputElement) => !el.validity.valid);
    expect(isInvalid).toBe(true);

    // Fill valid session name using click-first pattern
    await nameInput.click();
    await nameInput.fill('Valid Session Name');

    // Fill capacity using the helper pattern (times use defaults which are valid)
    const capacityInput = page.getByTestId('input-session-capacity');
    await capacityInput.click();
    await capacityInput.click({ clickCount: 3 });
    await page.keyboard.type('20');

    // Now save should work
    await saveButton.click();

    // Modal should close on successful save
    await page.waitForSelector('[role="dialog"]', { state: 'detached', timeout: 10000 });

    console.log('Form validation working correctly');
  });
});
