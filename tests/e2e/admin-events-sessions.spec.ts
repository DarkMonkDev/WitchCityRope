import { test, expect } from '@playwright/test';
import { quickLogin } from './test-utils/helpers/auth.helper';

/**
 * TDD E2E Tests for Admin Events Edit Screen - Session Management
 *
 * These tests verify session management functionality including:
 * - Session creation via modal
 * - Session editing with pre-populated data
 * - Automatic S# ID assignment
 * - Form validation
 * - Error handling
 */

test.describe('Admin Events Edit Screen - Session Management', () => {
  let testEventId: string;

  test.beforeEach(async ({ page }) => {
    // Login as admin user using established pattern from lessons learned
    await quickLogin(page, 'admin');

    // Fetch a real event ID from the API
    const eventsResponse = await page.request.get('http://api:5655/api/events');
    const events = await eventsResponse.json();

    if (!events || events.length === 0) {
      throw new Error('No events found in database. Run seed data first.');
    }

    testEventId = events[0].id;
  });

  test('should add a new session via modal without page refresh', async ({ page }) => {
    // Navigate to admin event edit page
    await page.goto(`/admin/events/${testEventId}`);
    
    // Wait for page to load
    await expect(page.locator('[data-testid="page-admin-event-details"]')).toBeVisible();
    
    // Navigate to Setup tab (contains Sessions and Ticket Types)
    const setupTab = page.locator('[data-testid="setup-tab"]');
    await expect(setupTab).toBeVisible({ timeout: 5000 });
    await setupTab.click();

    // Wait for sessions section within setup tab
    const sessionsSection = page.locator('[data-testid="sessions-section"]');
    await expect(sessionsSection).toBeVisible({ timeout: 5000 });
    
    // Click Add Session button (this likely doesn't exist yet - will fail)  
    const addSessionButton = page.locator('[data-testid="button-add-session"]');
    await expect(addSessionButton).toBeVisible({ timeout: 5000 });
    await addSessionButton.click();
    
    // Verify Add Session modal opens
    const sessionModal = page.locator('[role="dialog"]');
    await expect(sessionModal).toBeVisible({ timeout: 5000 });

    // Fill session form fields using Mantine patterns (getByLabel for form fields)
    await page.getByLabel('Session Name').fill('Morning Workshop');
    await page.getByLabel('Start Time').fill('09:00');
    await page.getByLabel('End Time').fill('12:00');
    await page.getByLabel('Capacity').fill('20');
    
    // Save session
    const saveButton = page.locator('[data-testid="button-save-session"]');
    await expect(saveButton).toBeVisible();
    await saveButton.click();
    
    // Verify modal closes
    await expect(sessionModal).not.toBeVisible({ timeout: 5000 });

    // Verify session appears in grid WITHOUT page refresh
    const sessionGrid = page.locator('[data-testid="grid-sessions"]');
    await expect(sessionGrid).toBeVisible();

    // Verify session appears in the grid - find row containing "Morning Workshop"
    const newSessionRow = sessionGrid.locator('tr').filter({ hasText: 'Morning Workshop' });
    await expect(newSessionRow).toBeVisible();

    // Verify session has S# ID format (session identifier should be auto-generated like S1, S2, etc.)
    const sessionId = newSessionRow.locator('[data-testid="session-id"]');
    await expect(sessionId).toHaveText(/^S\d+$/);
    await expect(newSessionRow.locator('[data-testid="session-name"]')).toHaveText('Morning Workshop');
  });

  test('should edit existing session via modal', async ({ page }) => {
    // Navigate to admin event edit page
    await page.goto(`/admin/events/${testEventId}`);

    // Wait for page to load and navigate to Setup tab
    await expect(page.locator('[data-testid="page-admin-event-details"]')).toBeVisible();
    const setupTab = page.locator('[data-testid="setup-tab"]');
    await expect(setupTab).toBeVisible({ timeout: 5000 });
    await setupTab.click();

    // Wait for sessions section
    const sessionsSection = page.locator('[data-testid="sessions-section"]');
    await expect(sessionsSection).toBeVisible({ timeout: 5000 });

    // Verify sessions grid exists
    const sessionGrid = page.locator('[data-testid="grid-sessions"]');
    await expect(sessionGrid).toBeVisible();

    // Click on first session row to edit (EventSessionsGrid uses row onClick)
    const firstRow = sessionGrid.locator('[data-testid="session-row"]').first();
    await expect(firstRow).toBeVisible();
    await firstRow.click();

    // Verify edit modal opens
    const editModal = page.locator('[role="dialog"]');
    await expect(editModal).toBeVisible();

    // Verify form is pre-populated with existing session data
    await expect(page.getByLabel('Session Name')).not.toHaveValue('');
    await expect(page.getByLabel('Start Time')).not.toHaveValue('');
    await expect(page.getByLabel('End Time')).not.toHaveValue('');
    await expect(page.getByLabel('Capacity')).not.toHaveValue('');

    // Change session name
    await page.getByLabel('Session Name').fill('Updated Session Name');

    // Save changes
    const saveButton = page.locator('[data-testid="button-save-session"]');
    await saveButton.click();

    // Verify modal closes
    await expect(editModal).not.toBeVisible({ timeout: 5000 });

    // Verify updates appear in grid without page refresh
    const updatedRow = sessionGrid.locator('tr').filter({ hasText: 'Updated Session Name' });
    await expect(updatedRow).toBeVisible();
  });

  test('should assign S# IDs automatically to new sessions', async ({ page }) => {
    // Navigate to admin event edit page
    await page.goto(`/admin/events/${testEventId}`);

    // Navigate to Setup tab
    await expect(page.locator('[data-testid="page-admin-event-details"]')).toBeVisible();
    const setupTab = page.locator('[data-testid="setup-tab"]');
    await expect(setupTab).toBeVisible({ timeout: 5000 });
    await setupTab.click();

    // Wait for sessions section
    const sessionsSection = page.locator('[data-testid="sessions-section"]');
    await expect(sessionsSection).toBeVisible({ timeout: 5000 });

    const sessionGrid = page.locator('[data-testid="grid-sessions"]');

    // Get current session count
    const initialSessionCount = await sessionGrid.locator('[data-testid="session-row"]').count();

    // Add first session
    await page.locator('[data-testid="button-add-session"]').click();
    const modal = page.locator('[role="dialog"]');
    await expect(modal).toBeVisible();

    await page.getByLabel('Session Name').fill('First Session');
    await page.getByLabel('Start Time').fill('09:00');
    await page.getByLabel('End Time').fill('12:00');
    await page.getByLabel('Capacity').fill('20');
    await page.locator('[data-testid="button-save-session"]').click();

    // Wait for modal to close
    await expect(modal).not.toBeVisible({ timeout: 5000 });

    // Verify first session gets sequential S# ID
    const expectedId = `S${initialSessionCount + 1}`;
    const firstSessionRow = sessionGrid.locator('tr').filter({ hasText: 'First Session' });
    await expect(firstSessionRow.locator('[data-testid="session-id"]')).toHaveText(expectedId);

    // Add second session
    await page.locator('[data-testid="button-add-session"]').click();
    await expect(modal).toBeVisible();

    await page.getByLabel('Session Name').fill('Second Session');
    await page.getByLabel('Start Time').fill('13:00');
    await page.getByLabel('End Time').fill('16:00');
    await page.getByLabel('Capacity').fill('25');
    await page.locator('[data-testid="button-save-session"]').click();

    // Wait for modal to close
    await expect(modal).not.toBeVisible({ timeout: 5000 });

    // Verify second session gets next sequential S# ID
    const expectedSecondId = `S${initialSessionCount + 2}`;
    const secondSessionRow = sessionGrid.locator('tr').filter({ hasText: 'Second Session' });
    await expect(secondSessionRow.locator('[data-testid="session-id"]')).toHaveText(expectedSecondId);
  });

  test('should delete session with confirmation dialog', async ({ page }) => {
    // Navigate to admin event edit page
    await page.goto(`/admin/events/${testEventId}`);

    // Navigate to Setup tab
    await expect(page.locator('[data-testid="page-admin-event-details"]')).toBeVisible();
    const setupTab = page.locator('[data-testid="setup-tab"]');
    await expect(setupTab).toBeVisible({ timeout: 5000 });
    await setupTab.click();

    // Wait for sessions section
    const sessionsSection = page.locator('[data-testid="sessions-section"]');
    await expect(sessionsSection).toBeVisible({ timeout: 5000 });

    const sessionGrid = page.locator('[data-testid="grid-sessions"]');
    await expect(sessionGrid).toBeVisible();

    // Get initial session count
    const initialCount = await sessionGrid.locator('[data-testid="session-row"]').count();

    // Click delete on first session (using data-testid if exists)
    const deleteButton = sessionGrid.locator('[data-testid="button-delete-session"]').first();
    await expect(deleteButton).toBeVisible();
    await deleteButton.click();

    // Verify confirmation dialog appears
    const confirmDialog = page.locator('[data-testid="dialog-confirm-delete-session"]');
    await expect(confirmDialog).toBeVisible();
    await expect(confirmDialog).toContainText('Are you sure you want to delete this session?');

    // Confirm deletion
    const confirmButton = confirmDialog.locator('[data-testid="button-confirm-delete"]');
    await confirmButton.click();

    // Verify dialog closes
    await expect(confirmDialog).not.toBeVisible({ timeout: 5000 });

    // Verify session removed from grid without page refresh
    await expect(sessionGrid.locator('[data-testid="session-row"]')).toHaveCount(initialCount - 1);
  });

  test('should validate session form fields', async ({ page }) => {
    // Navigate to admin event edit page
    await page.goto(`/admin/events/${testEventId}`);

    // Navigate to Setup tab and open add modal
    await expect(page.locator('[data-testid="page-admin-event-details"]')).toBeVisible();
    const setupTab = page.locator('[data-testid="setup-tab"]');
    await setupTab.click();

    // Wait for sessions section
    const sessionsSection = page.locator('[data-testid="sessions-section"]');
    await expect(sessionsSection).toBeVisible({ timeout: 5000 });

    await page.locator('[data-testid="button-add-session"]').click();

    const sessionModal = page.locator('[role="dialog"]');
    await expect(sessionModal).toBeVisible();

    // Clear pre-filled values and try to save empty form
    await page.getByLabel('Session Name').clear();
    await page.getByLabel('Start Time').clear();
    await page.getByLabel('End Time').clear();
    await page.getByLabel('Capacity').clear();

    await page.locator('[data-testid="button-save-session"]').click();

    // Verify validation errors appear (Mantine form validation shows errors inline)
    // Check for error text near the fields
    await expect(sessionModal.getByText(/Session name is required/i)).toBeVisible();
    await expect(sessionModal.getByText(/Start time is required/i)).toBeVisible();
    await expect(sessionModal.getByText(/End time is required/i)).toBeVisible();
    await expect(sessionModal.getByText(/Capacity must be at least 1/i)).toBeVisible();

    // Test invalid time range (end before start)
    await page.getByLabel('Session Name').fill('Test Session');
    await page.getByLabel('Start Time').fill('15:00');
    await page.getByLabel('End Time').fill('14:00'); // Before start time
    await page.getByLabel('Capacity').fill('10');

    await page.locator('[data-testid="button-save-session"]').click();

    // Should show time range validation error
    await expect(sessionModal.getByText(/must be after start time/i)).toBeVisible();

    // Test invalid capacity
    await page.getByLabel('End Time').fill('16:00');
    await page.getByLabel('Capacity').clear();
    await page.getByLabel('Capacity').fill('0'); // Invalid capacity

    await page.locator('[data-testid="button-save-session"]').click();

    // Should show capacity validation error
    await expect(sessionModal.getByText(/Capacity must be at least 1/i)).toBeVisible();
  });

  test('should show loading states and error handling', async ({ page }) => {
    // Navigate to admin event edit page
    await page.goto(`/admin/events/${testEventId}`);

    // Navigate to Setup tab
    await expect(page.locator('[data-testid="page-admin-event-details"]')).toBeVisible();
    const setupTab = page.locator('[data-testid="setup-tab"]');
    await setupTab.click();

    // Wait for sessions section
    const sessionsSection = page.locator('[data-testid="sessions-section"]');
    await expect(sessionsSection).toBeVisible({ timeout: 5000 });

    // Mock API failure for session update
    await page.route(`**/api/admin/events/${testEventId}`, route => {
      route.fulfill({
        status: 500,
        contentType: 'application/json',
        body: JSON.stringify({ error: 'Server error' })
      });
    });

    // Try to add session
    await page.locator('[data-testid="button-add-session"]').click();

    const modal = page.locator('[role="dialog"]');
    await expect(modal).toBeVisible();

    await page.getByLabel('Session Name').fill('Test Session');
    await page.getByLabel('Start Time').fill('09:00');
    await page.getByLabel('End Time').fill('12:00');
    await page.getByLabel('Capacity').fill('20');

    const saveButton = page.locator('[data-testid="button-save-session"]');
    await saveButton.click();

    // Should show error notification (Mantine notifications library)
    // Look for notification with error message
    const notification = page.locator('.mantine-Notification-root, [role="alert"]');
    await expect(notification).toBeVisible({ timeout: 10000 });
    await expect(notification).toContainText(/failed/i);
  });
});