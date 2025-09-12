import { test, expect } from '@playwright/test';
import { quickLogin } from './helpers/auth.helper';

/**
 * TDD E2E Tests for Admin Events Edit Screen - Volunteer Position Management
 * 
 * These tests are designed to FAIL initially (Red phase of TDD)
 * They test the volunteer position management bugs described in the business requirements
 * 
 * Expected Failures:
 * - Volunteers tab doesn't exist
 * - Sessions dropdown shows all platform sessions instead of event-specific ones
 * - "Add New Position" button doesn't exist (uses bottom form instead of modal)
 * - Edit position doesn't load existing data
 * - Add position form doesn't process correctly
 * - UI inconsistency - should use modals like other tabs
 */

test.describe('Admin Events Edit Screen - Volunteer Position Management', () => {
  test.beforeEach(async ({ page }) => {
    // Login as admin user using established pattern from lessons learned
    await quickLogin(page, 'admin');
  });

  test('should show only current event sessions in dropdown', async ({ page }) => {
    // Navigate to admin event edit page
    await page.goto('http://localhost:5173/admin/events/1');
    
    // Wait for page to load
    await expect(page.locator('[data-testid="page-admin-event-details"]')).toBeVisible();
    
    // Navigate to Volunteers/Staff tab (this likely doesn't exist yet - will fail)
    const volunteersTab = page.locator('[data-testid="tab-volunteers"]');
    await expect(volunteersTab).toBeVisible({ timeout: 5000 });
    await volunteersTab.click();
    
    // Check if positions grid is visible (will fail if not implemented)
    const positionsGrid = page.locator('[data-testid="grid-volunteer-positions"]');
    await expect(positionsGrid).toBeVisible();
    
    // Open Add Position modal/form (will fail - button doesn't exist)
    const addPositionButton = page.locator('[data-testid="button-add-volunteer-position"]');
    await expect(addPositionButton).toBeVisible();
    await addPositionButton.click();
    
    // Check sessions dropdown in add form (will fail if dropdown shows all sessions)
    const sessionDropdown = page.locator('[data-testid="dropdown-position-sessions"]');
    await expect(sessionDropdown).toBeVisible();
    await sessionDropdown.click();
    
    // Verify dropdown shows only event-specific sessions in S# format (will fail)
    const sessionOptions = page.locator('[data-testid="option-session"]');
    await expect(sessionOptions).toHaveCount({ min: 1, max: 5 }); // Reasonable range for one event
    
    // All options should start with "S" (S1, S2, S3, etc) or "All Sessions"
    const firstOption = sessionOptions.first();
    await expect(firstOption).toContainText(/^(S\d+|All Sessions)/);
    
    // Should NOT contain sessions from other events (will fail if global sessions shown)
    // This test assumes other events have different session patterns
    await expect(sessionOptions).not.toContainText(/Event \d+ Session/); // Generic pattern for other event sessions
  });

  test('should add volunteer position via modal', async ({ page }) => {
    // Navigate to admin event edit page
    await page.goto('http://localhost:5173/admin/events/1');
    
    // Navigate to Volunteers tab
    await expect(page.locator('[data-testid="page-admin-event-details"]')).toBeVisible();
    const volunteersTab = page.locator('[data-testid="tab-volunteers"]');
    await expect(volunteersTab).toBeVisible({ timeout: 5000 });
    await volunteersTab.click();
    
    const positionsGrid = page.locator('[data-testid="grid-volunteer-positions"]');
    await expect(positionsGrid).toBeVisible();
    
    // Get initial position count
    const initialCount = await positionsGrid.locator('[data-testid="position-row"]').count();
    
    // Click "Add New Position" button (will fail - doesn't exist, currently uses bottom form)
    const addPositionButton = page.locator('[data-testid="button-add-volunteer-position"]');
    await expect(addPositionButton).toBeVisible();
    await addPositionButton.click();
    
    // Verify modal opens (will fail - uses inline form instead of modal)
    const positionModal = page.locator('[data-testid="modal-add-volunteer-position"]');
    await expect(positionModal).toBeVisible();
    
    // Fill position form fields (will fail if form doesn't exist)
    await page.locator('[data-testid="input-position-name"]').fill('Safety Monitor');
    await page.locator('[data-testid="textarea-position-description"]').fill('Monitor event safety and intervene if needed');
    await page.locator('[data-testid="dropdown-position-sessions"]').click();
    await page.locator('[data-testid="option-session"]').first().click(); // Select first session
    await page.locator('[data-testid="input-position-start-time"]').fill('08:30');
    await page.locator('[data-testid="input-position-end-time"]').fill('12:30');
    await page.locator('[data-testid="input-volunteers-needed"]').fill('2');
    
    // Save position
    const saveButton = page.locator('[data-testid="button-save-volunteer-position"]');
    await expect(saveButton).toBeVisible();
    await saveButton.click();
    
    // Verify modal closes (will fail because modal doesn't exist)
    await expect(positionModal).not.toBeVisible({ timeout: 5000 });
    
    // Verify position appears in grid without page refresh (will fail if form doesn't work)
    await expect(positionsGrid.locator('[data-testid="position-row"]')).toHaveCount(initialCount + 1);
    
    // Verify new position data in grid
    const newPositionRow = positionsGrid.locator('[data-testid="position-row"]').last();
    await expect(newPositionRow.locator('[data-testid="position-name"]')).toHaveText('Safety Monitor');
    await expect(newPositionRow.locator('[data-testid="volunteers-needed"]')).toHaveText('2');
  });

  test('should edit volunteer position via modal', async ({ page }) => {
    // Navigate to admin event edit page
    await page.goto('http://localhost:5173/admin/events/1');
    
    // Navigate to Volunteers tab
    await expect(page.locator('[data-testid="page-admin-event-details"]')).toBeVisible();
    await page.locator('[data-testid="tab-volunteers"]').click();
    
    const positionsGrid = page.locator('[data-testid="grid-volunteer-positions"]');
    await expect(positionsGrid).toBeVisible();
    
    // Click edit on first position (will fail if edit button doesn't exist)
    const editButton = positionsGrid.locator('[data-testid="button-edit-volunteer-position"]').first();
    await expect(editButton).toBeVisible();
    await editButton.click();
    
    // Verify edit modal opens (will fail if uses inline form)
    const editModal = page.locator('[data-testid="modal-edit-volunteer-position"]');
    await expect(editModal).toBeVisible();
    
    // Verify form pre-populates with existing data (will fail if data not loaded)
    const positionNameInput = page.locator('[data-testid="input-position-name"]');
    await expect(positionNameInput).not.toHaveValue(''); // Should have existing value
    
    const descriptionTextarea = page.locator('[data-testid="textarea-position-description"]');
    await expect(descriptionTextarea).not.toHaveValue(''); // Should have existing value
    
    const volunteersNeededInput = page.locator('[data-testid="input-volunteers-needed"]');
    await expect(volunteersNeededInput).not.toHaveValue(''); // Should have existing value
    
    // Change position name
    await positionNameInput.fill('Updated Safety Monitor');
    
    // Change volunteers needed
    await volunteersNeededInput.fill('3');
    
    // Save changes
    const saveButton = page.locator('[data-testid="button-save-volunteer-position"]');
    await saveButton.click();
    
    // Verify modal closes
    await expect(editModal).not.toBeVisible({ timeout: 5000 });
    
    // Verify updates appear in grid without page refresh (will fail if edit doesn't work)
    const firstPositionRow = positionsGrid.locator('[data-testid="position-row"]').first();
    await expect(firstPositionRow.locator('[data-testid="position-name"]')).toHaveText('Updated Safety Monitor');
    await expect(firstPositionRow.locator('[data-testid="volunteers-needed"]')).toHaveText('3');
  });

  test('should delete volunteer position with confirmation', async ({ page }) => {
    // Navigate to admin event edit page
    await page.goto('http://localhost:5173/admin/events/1');
    
    // Navigate to Volunteers tab
    await expect(page.locator('[data-testid="page-admin-event-details"]')).toBeVisible();
    await page.locator('[data-testid="tab-volunteers"]').click();
    
    const positionsGrid = page.locator('[data-testid="grid-volunteer-positions"]');
    await expect(positionsGrid).toBeVisible();
    
    // Get initial position count
    const initialCount = await positionsGrid.locator('[data-testid="position-row"]').count();
    
    // Click delete on first position (will fail if delete button doesn't exist)
    const deleteButton = positionsGrid.locator('[data-testid="button-delete-volunteer-position"]').first();
    await expect(deleteButton).toBeVisible();
    await deleteButton.click();
    
    // Verify confirmation dialog appears (will fail if not implemented)
    const confirmDialog = page.locator('[data-testid="dialog-confirm-delete-position"]');
    await expect(confirmDialog).toBeVisible();
    await expect(confirmDialog).toContainText('Are you sure you want to delete this volunteer position?');
    
    // Confirm deletion
    const confirmButton = confirmDialog.locator('[data-testid="button-confirm-delete"]');
    await confirmButton.click();
    
    // Verify dialog closes
    await expect(confirmDialog).not.toBeVisible({ timeout: 5000 });
    
    // Verify position removed from grid without page refresh (will fail if delete doesn't work)
    await expect(positionsGrid.locator('[data-testid="position-row"]')).toHaveCount(initialCount - 1);
  });

  test('should validate volunteer position form fields', async ({ page }) => {
    // Navigate to admin event edit page and volunteers tab
    await page.goto('http://localhost:5173/admin/events/1');
    await expect(page.locator('[data-testid="page-admin-event-details"]')).toBeVisible();
    await page.locator('[data-testid="tab-volunteers"]').click();
    
    // Open add position modal
    await page.locator('[data-testid="button-add-volunteer-position"]').click();
    const positionModal = page.locator('[data-testid="modal-add-volunteer-position"]');
    await expect(positionModal).toBeVisible();
    
    // Try to save empty form (will fail if validation not implemented)
    await page.locator('[data-testid="button-save-volunteer-position"]').click();
    
    // Verify validation errors appear
    await expect(page.locator('[data-testid="error-position-name"]')).toBeVisible();
    await expect(page.locator('[data-testid="error-position-name"]')).toHaveText(/required/i);
    
    await expect(page.locator('[data-testid="error-volunteers-needed"]')).toBeVisible();
    await expect(page.locator('[data-testid="error-volunteers-needed"]')).toHaveText(/required/i);
    
    // Test invalid volunteers needed (zero or negative)
    await page.locator('[data-testid="input-position-name"]').fill('Test Position');
    await page.locator('[data-testid="input-volunteers-needed"]').fill('0');
    
    await page.locator('[data-testid="button-save-volunteer-position"]').click();
    
    // Should show validation error for invalid volunteer count
    await expect(page.locator('[data-testid="error-volunteers-needed"]')).toBeVisible();
    await expect(page.locator('[data-testid="error-volunteers-needed"]')).toHaveText(/minimum.*1/i);
    
    // Test invalid time range (end before start)
    await page.locator('[data-testid="input-volunteers-needed"]').fill('2');
    await page.locator('[data-testid="input-position-start-time"]').fill('15:00');
    await page.locator('[data-testid="input-position-end-time"]').fill('14:00'); // Before start time
    
    await page.locator('[data-testid="button-save-volunteer-position"]').click();
    
    // Should show time range validation error
    await expect(page.locator('[data-testid="error-position-end-time"]')).toBeVisible();
    await expect(page.locator('[data-testid="error-position-end-time"]')).toHaveText(/must be after start time/i);
  });

  test('should display sessions in S# format in position assignments', async ({ page }) => {
    // Navigate to admin event edit page and volunteers tab
    await page.goto('http://localhost:5173/admin/events/1');
    await expect(page.locator('[data-testid="page-admin-event-details"]')).toBeVisible();
    await page.locator('[data-testid="tab-volunteers"]').click();
    
    const positionsGrid = page.locator('[data-testid="grid-volunteer-positions"]');
    await expect(positionsGrid).toBeVisible();
    
    // Verify existing positions show session assignments in S# format (will fail if not implemented)
    const positionRows = positionsGrid.locator('[data-testid="position-row"]');
    const firstRow = positionRows.first();
    
    // Sessions column should display S1, S2, S3 format or "All Sessions"
    const sessionsCell = firstRow.locator('[data-testid="position-sessions"]');
    await expect(sessionsCell).toBeVisible();
    
    // Should match S# pattern or "All Sessions" text
    const sessionsText = await sessionsCell.textContent();
    expect(sessionsText).toMatch(/^(S\d+(, S\d+)*|All Sessions)$/);
  });

  test('should handle API errors gracefully', async ({ page }) => {
    // Navigate to admin event edit page and volunteers tab
    await page.goto('http://localhost:5173/admin/events/1');
    await expect(page.locator('[data-testid="page-admin-event-details"]')).toBeVisible();
    await page.locator('[data-testid="tab-volunteers"]').click();
    
    // Mock API failure for position creation
    await page.route('**/api/events/1/volunteer-positions', route => {
      route.fulfill({
        status: 500,
        contentType: 'application/json',
        body: JSON.stringify({ error: 'Server error' })
      });
    });
    
    // Try to add position
    await page.locator('[data-testid="button-add-volunteer-position"]').click();
    await page.locator('[data-testid="input-position-name"]').fill('Test Position');
    await page.locator('[data-testid="input-volunteers-needed"]').fill('1');
    
    const saveButton = page.locator('[data-testid="button-save-volunteer-position"]');
    await saveButton.click();
    
    // Should show loading state (will fail if not implemented)
    await expect(saveButton).toHaveAttribute('disabled');
    await expect(saveButton.locator('[data-testid="loading-spinner"]')).toBeVisible();
    
    // Should show error message (will fail if error handling not implemented)
    const errorAlert = page.locator('[data-testid="alert-volunteer-position-error"]');
    await expect(errorAlert).toBeVisible({ timeout: 10000 });
    await expect(errorAlert).toContainText(/failed to create volunteer position/i);
  });

  test('should show "Add New Position" button below volunteer grid for UI consistency', async ({ page }) => {
    // Navigate to admin event edit page and volunteers tab
    await page.goto('http://localhost:5173/admin/events/1');
    await expect(page.locator('[data-testid="page-admin-event-details"]')).toBeVisible();
    await page.locator('[data-testid="tab-volunteers"]').click();
    
    const positionsGrid = page.locator('[data-testid="grid-volunteer-positions"]');
    await expect(positionsGrid).toBeVisible();
    
    // Verify "Add New Position" button exists below the grid (will fail - currently uses bottom form)
    const addButton = page.locator('[data-testid="button-add-volunteer-position"]');
    await expect(addButton).toBeVisible();
    
    // Button should be positioned below the grid, not as part of an inline form
    const addButtonBox = await addButton.boundingBox();
    const gridBox = await positionsGrid.boundingBox();
    
    // Add button should be below the grid
    expect(addButtonBox!.y).toBeGreaterThan(gridBox!.y + gridBox!.height);
    
    // Should NOT see inline form at bottom (current implementation - will fail)
    const inlineForm = page.locator('[data-testid="form-volunteer-position-inline"]');
    await expect(inlineForm).not.toBeVisible();
  });
});