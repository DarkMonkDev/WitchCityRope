import { test, expect } from '@playwright/test';
import { quickLogin } from './helpers/auth.helper';

/**
 * TDD E2E Tests for Admin Events Edit Screen - Session Management
 * 
 * These tests are designed to FAIL initially (Red phase of TDD)
 * They test the session management bugs described in the business requirements
 * 
 * Expected Failures:
 * - Session creation modal doesn't exist
 * - Add Session button doesn't exist  
 * - Session grid doesn't update without page refresh
 * - S# ID assignment not implemented
 * - Edit session modal doesn't pre-populate data
 */

test.describe('Admin Events Edit Screen - Session Management', () => {
  test.beforeEach(async ({ page }) => {
    // Login as admin user using established pattern from lessons learned
    await quickLogin(page, 'admin');
  });

  test('should add a new session via modal without page refresh', async ({ page }) => {
    // Navigate to admin event edit page (assuming event ID 1 exists from seed data)
    await page.goto('http://localhost:5173/admin/events/1');
    
    // Wait for page to load
    await expect(page.locator('[data-testid="page-admin-event-details"]')).toBeVisible();
    
    // Navigate to Sessions tab (this likely doesn't exist yet - will fail)
    const sessionsTab = page.locator('[data-testid="tab-sessions"]');
    await expect(sessionsTab).toBeVisible({ timeout: 5000 });
    await sessionsTab.click();
    
    // Click Add Session button (this likely doesn't exist yet - will fail)  
    const addSessionButton = page.locator('[data-testid="button-add-session"]');
    await expect(addSessionButton).toBeVisible({ timeout: 5000 });
    await addSessionButton.click();
    
    // Verify Add Session modal opens (this likely doesn't exist yet - will fail)
    const sessionModal = page.locator('[data-testid="modal-add-session"]');
    await expect(sessionModal).toBeVisible({ timeout: 5000 });
    
    // Fill session form fields (these likely don't exist yet - will fail)
    await page.locator('[data-testid="input-session-name"]').fill('Morning Workshop');
    await page.locator('[data-testid="input-session-start-time"]').fill('09:00');
    await page.locator('[data-testid="input-session-end-time"]').fill('12:00');
    await page.locator('[data-testid="input-session-capacity"]').fill('20');
    
    // Save session
    const saveButton = page.locator('[data-testid="button-save-session"]');
    await expect(saveButton).toBeVisible();
    await saveButton.click();
    
    // Verify modal closes (will fail because modal doesn't exist)
    await expect(sessionModal).not.toBeVisible({ timeout: 5000 });
    
    // Verify session appears in grid WITHOUT page refresh (will fail - no grid exists)
    const sessionGrid = page.locator('[data-testid="grid-sessions"]');
    await expect(sessionGrid).toBeVisible();
    
    // Verify session has S1 ID format (will fail - S# ID system not implemented)
    const newSessionRow = sessionGrid.locator('[data-testid="session-row"]').first();
    await expect(newSessionRow.locator('[data-testid="session-id"]')).toHaveText('S1');
    await expect(newSessionRow.locator('[data-testid="session-name"]')).toHaveText('Morning Workshop');
  });

  test('should edit existing session via modal', async ({ page }) => {
    // Navigate to admin event edit page
    await page.goto('http://localhost:5173/admin/events/1');
    
    // Wait for page to load and navigate to Sessions tab
    await expect(page.locator('[data-testid="page-admin-event-details"]')).toBeVisible();
    const sessionsTab = page.locator('[data-testid="tab-sessions"]');
    await expect(sessionsTab).toBeVisible({ timeout: 5000 });
    await sessionsTab.click();
    
    // Verify sessions grid exists (will fail - not implemented)
    const sessionGrid = page.locator('[data-testid="grid-sessions"]');
    await expect(sessionGrid).toBeVisible();
    
    // Click edit on first session (will fail - edit button doesn't exist)
    const editButton = sessionGrid.locator('[data-testid="button-edit-session"]').first();
    await expect(editButton).toBeVisible();
    await editButton.click();
    
    // Verify edit modal opens and pre-populates data (will fail - modal doesn't exist)
    const editModal = page.locator('[data-testid="modal-edit-session"]');
    await expect(editModal).toBeVisible();
    
    // Verify form is pre-populated with existing session data (will fail)
    await expect(page.locator('[data-testid="input-session-name"]')).toHaveValue(/^S\d+/); // Should have existing session name
    await expect(page.locator('[data-testid="input-session-start-time"]')).not.toHaveValue('');
    await expect(page.locator('[data-testid="input-session-end-time"]')).not.toHaveValue('');
    await expect(page.locator('[data-testid="input-session-capacity"]')).not.toHaveValue('');
    
    // Change session name
    await page.locator('[data-testid="input-session-name"]').fill('Updated Session Name');
    
    // Save changes
    const saveButton = page.locator('[data-testid="button-save-session"]');
    await saveButton.click();
    
    // Verify modal closes
    await expect(editModal).not.toBeVisible({ timeout: 5000 });
    
    // Verify updates appear in grid without page refresh (will fail)
    await expect(sessionGrid.locator('[data-testid="session-name"]').first()).toHaveText('Updated Session Name');
  });

  test('should assign S# IDs automatically to new sessions', async ({ page }) => {
    // Navigate to admin event edit page
    await page.goto('http://localhost:5173/admin/events/1');
    
    // Navigate to Sessions tab
    await expect(page.locator('[data-testid="page-admin-event-details"]')).toBeVisible();
    const sessionsTab = page.locator('[data-testid="tab-sessions"]');
    await expect(sessionsTab).toBeVisible({ timeout: 5000 });
    await sessionsTab.click();
    
    const sessionGrid = page.locator('[data-testid="grid-sessions"]');
    
    // Get current session count
    const initialSessionCount = await sessionGrid.locator('[data-testid="session-row"]').count();
    
    // Add first session
    await page.locator('[data-testid="button-add-session"]').click();
    await page.locator('[data-testid="input-session-name"]').fill('First Session');
    await page.locator('[data-testid="input-session-start-time"]').fill('09:00');
    await page.locator('[data-testid="input-session-end-time"]').fill('12:00');
    await page.locator('[data-testid="input-session-capacity"]').fill('20');
    await page.locator('[data-testid="button-save-session"]').click();
    
    // Verify first session gets S1 ID (will fail - S# system not implemented)
    const expectedId = `S${initialSessionCount + 1}`;
    await expect(sessionGrid.locator('[data-testid="session-row"]').last().locator('[data-testid="session-id"]')).toHaveText(expectedId);
    
    // Add second session  
    await page.locator('[data-testid="button-add-session"]').click();
    await page.locator('[data-testid="input-session-name"]').fill('Second Session');
    await page.locator('[data-testid="input-session-start-time"]').fill('13:00');
    await page.locator('[data-testid="input-session-end-time"]').fill('16:00');
    await page.locator('[data-testid="input-session-capacity"]').fill('25');
    await page.locator('[data-testid="button-save-session"]').click();
    
    // Verify second session gets S2 ID (will fail)
    const expectedSecondId = `S${initialSessionCount + 2}`;
    await expect(sessionGrid.locator('[data-testid="session-row"]').last().locator('[data-testid="session-id"]')).toHaveText(expectedSecondId);
  });

  test('should delete session with confirmation dialog', async ({ page }) => {
    // Navigate to admin event edit page
    await page.goto('http://localhost:5173/admin/events/1');
    
    // Navigate to Sessions tab
    await expect(page.locator('[data-testid="page-admin-event-details"]')).toBeVisible();
    const sessionsTab = page.locator('[data-testid="tab-sessions"]');
    await expect(sessionsTab).toBeVisible({ timeout: 5000 });
    await sessionsTab.click();
    
    const sessionGrid = page.locator('[data-testid="grid-sessions"]');
    await expect(sessionGrid).toBeVisible();
    
    // Get initial session count
    const initialCount = await sessionGrid.locator('[data-testid="session-row"]').count();
    
    // Click delete on first session (will fail - delete button doesn't exist)
    const deleteButton = sessionGrid.locator('[data-testid="button-delete-session"]').first();
    await expect(deleteButton).toBeVisible();
    await deleteButton.click();
    
    // Verify confirmation dialog appears (will fail - confirmation dialog not implemented)
    const confirmDialog = page.locator('[data-testid="dialog-confirm-delete-session"]');
    await expect(confirmDialog).toBeVisible();
    await expect(confirmDialog).toContainText('Are you sure you want to delete this session?');
    
    // Confirm deletion
    const confirmButton = confirmDialog.locator('[data-testid="button-confirm-delete"]');
    await confirmButton.click();
    
    // Verify dialog closes
    await expect(confirmDialog).not.toBeVisible({ timeout: 5000 });
    
    // Verify session removed from grid without page refresh (will fail)
    await expect(sessionGrid.locator('[data-testid="session-row"]')).toHaveCount(initialCount - 1);
  });

  test('should validate session form fields', async ({ page }) => {
    // Navigate to admin event edit page
    await page.goto('http://localhost:5173/admin/events/1');
    
    // Navigate to Sessions tab and open add modal
    await expect(page.locator('[data-testid="page-admin-event-details"]')).toBeVisible();
    await page.locator('[data-testid="tab-sessions"]').click();
    await page.locator('[data-testid="button-add-session"]').click();
    
    const sessionModal = page.locator('[data-testid="modal-add-session"]');
    await expect(sessionModal).toBeVisible();
    
    // Try to save empty form (should show validation errors - will fail if validation not implemented)
    await page.locator('[data-testid="button-save-session"]').click();
    
    // Verify validation errors appear (will fail - validation not implemented)
    await expect(page.locator('[data-testid="error-session-name"]')).toBeVisible();
    await expect(page.locator('[data-testid="error-session-name"]')).toHaveText(/required/i);
    
    await expect(page.locator('[data-testid="error-session-start-time"]')).toBeVisible();
    await expect(page.locator('[data-testid="error-session-end-time"]')).toBeVisible();
    await expect(page.locator('[data-testid="error-session-capacity"]')).toBeVisible();
    
    // Test invalid time range (end before start) - will fail if validation not implemented
    await page.locator('[data-testid="input-session-name"]').fill('Test Session');
    await page.locator('[data-testid="input-session-start-time"]').fill('15:00');
    await page.locator('[data-testid="input-session-end-time"]').fill('14:00'); // Before start time
    await page.locator('[data-testid="input-session-capacity"]').fill('10');
    
    await page.locator('[data-testid="button-save-session"]').click();
    
    // Should show time range validation error
    await expect(page.locator('[data-testid="error-session-end-time"]')).toBeVisible();
    await expect(page.locator('[data-testid="error-session-end-time"]')).toHaveText(/must be after start time/i);
    
    // Test invalid capacity (will fail if validation not implemented)
    await page.locator('[data-testid="input-session-end-time"]').fill('16:00');
    await page.locator('[data-testid="input-session-capacity"]').fill('0'); // Invalid capacity
    
    await page.locator('[data-testid="button-save-session"]').click();
    
    // Should show capacity validation error
    await expect(page.locator('[data-testid="error-session-capacity"]')).toBeVisible();
    await expect(page.locator('[data-testid="error-session-capacity"]')).toHaveText(/minimum.*1/i);
  });

  test('should show loading states and error handling', async ({ page }) => {
    // Navigate to admin event edit page
    await page.goto('http://localhost:5173/admin/events/1');
    
    // Navigate to Sessions tab
    await expect(page.locator('[data-testid="page-admin-event-details"]')).toBeVisible();
    await page.locator('[data-testid="tab-sessions"]').click();
    
    // Mock API failure for session creation
    await page.route('**/api/events/1/sessions', route => {
      route.fulfill({
        status: 500,
        contentType: 'application/json',
        body: JSON.stringify({ error: 'Server error' })
      });
    });
    
    // Try to add session
    await page.locator('[data-testid="button-add-session"]').click();
    await page.locator('[data-testid="input-session-name"]').fill('Test Session');
    await page.locator('[data-testid="input-session-start-time"]').fill('09:00');
    await page.locator('[data-testid="input-session-end-time"]').fill('12:00');
    await page.locator('[data-testid="input-session-capacity"]').fill('20');
    
    const saveButton = page.locator('[data-testid="button-save-session"]');
    await saveButton.click();
    
    // Should show loading state (will fail if loading states not implemented)
    await expect(saveButton).toHaveAttribute('disabled');
    await expect(saveButton.locator('[data-testid="loading-spinner"]')).toBeVisible();
    
    // Should show error message (will fail if error handling not implemented)
    const errorAlert = page.locator('[data-testid="alert-session-error"]');
    await expect(errorAlert).toBeVisible({ timeout: 10000 });
    await expect(errorAlert).toContainText(/failed to create session/i);
  });
});