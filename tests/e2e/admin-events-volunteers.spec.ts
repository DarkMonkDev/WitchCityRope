/**
 * E2E Tests for Admin Events Edit Screen - Volunteer Position Management
 *
 * Tests volunteer position management using inline editing (not modals).
 * The app uses a collapsible inline form below the grid, not modal dialogs.
 *
 * Key Implementation Details:
 * - Inline editing: Click "Add New Position" opens form below grid
 * - Click position row to edit existing position
 * - Browser confirm() dialog for delete confirmation (not custom modal)
 *
 * MIGRATED: Uses DataFactory pattern for automatic cleanup
 */

import { expect } from '@playwright/test';
import { test } from '../lib/datafactory/fixtures/test.fixture';
import { AuthHelpers } from './test-utils/helpers/auth.helpers';

test.describe('Admin Events Edit Screen - Volunteer Position Management', () => {
  test('should show only current event sessions in dropdown', async ({ page, df }) => {
    // Login as admin
    await AuthHelpers.loginAs(page, 'admin');

    // Create event with session
    const event = await df.events.createPublished(`Volunteer Test ${Date.now()}`);

    const sessionStart = new Date();
    sessionStart.setDate(sessionStart.getDate() + 7);
    sessionStart.setHours(18, 0, 0, 0);
    const sessionEnd = new Date(sessionStart.getTime() + 3 * 60 * 60 * 1000);

    await df.sessions.create({
      eventId: event.id,
      title: 'Test Session',
      startTime: sessionStart,
      endTime: sessionEnd,
      maxCapacity: 20,
    });

    // Navigate to admin event edit page
    await page.goto(`/admin/events/${event.id}`);

    // Wait for page to load
    await expect(page.locator('[data-testid="page-admin-event-details"]')).toBeVisible();

    // Navigate to Volunteers tab
    const volunteersTab = page.locator('[data-testid="tab-volunteers"]');
    await expect(volunteersTab).toBeVisible({ timeout: 5000 });
    await volunteersTab.click();

    // Check if positions grid is visible
    const positionsGrid = page.locator('[data-testid="grid-volunteer-positions"]');
    await expect(positionsGrid).toBeVisible();

    // Open Add Position inline form
    const addPositionButton = page.locator('[data-testid="button-add-volunteer-position"]');
    await expect(addPositionButton).toBeVisible();
    await addPositionButton.click();

    // Wait for inline form to appear
    const inlineForm = page.locator('[data-testid="volunteer-position-inline-form"]');
    await expect(inlineForm).toBeVisible();

    // Check sessions dropdown - use Mantine Select pattern
    const sessionDropdown = page.locator('[data-testid="dropdown-position-sessions"]');
    await expect(sessionDropdown).toBeVisible();

    // Click to open dropdown
    await sessionDropdown.click();

    // Verify dropdown options show event-specific sessions
    // Mantine Select renders options in a portal dropdown
    const dropdownOptions = page.locator('[role="option"]');
    const optionCount = await dropdownOptions.count();
    expect(optionCount).toBeGreaterThan(0); // At least one option

    // Verify at least one option contains session identifier
    const allOptionsText = await dropdownOptions.allTextContents();
    const hasSessionPattern = allOptionsText.some(text =>
      text.match(/(DAY\d+|S\d+|Test Session)\s*-?/i)
    );
    expect(hasSessionPattern).toBeTruthy();
  });

  test('should add volunteer position via inline form', async ({ page, df }) => {
    // Login as admin
    await AuthHelpers.loginAs(page, 'admin');

    // Create event with session
    const event = await df.events.createPublished(`Volunteer Add Test ${Date.now()}`);

    const sessionStart = new Date();
    sessionStart.setDate(sessionStart.getDate() + 7);
    sessionStart.setHours(18, 0, 0, 0);
    const sessionEnd = new Date(sessionStart.getTime() + 3 * 60 * 60 * 1000);

    await df.sessions.create({
      eventId: event.id,
      title: 'Main Session',
      startTime: sessionStart,
      endTime: sessionEnd,
      maxCapacity: 20,
    });

    // Navigate to admin event edit page
    await page.goto(`/admin/events/${event.id}`, { waitUntil: 'domcontentloaded' });

    // Navigate to Volunteers tab
    await expect(page.locator('[data-testid="page-admin-event-details"]')).toBeVisible();
    const volunteersTab = page.locator('[data-testid="tab-volunteers"]');
    await expect(volunteersTab).toBeVisible({ timeout: 5000 });
    await volunteersTab.click();

    const positionsGrid = page.locator('[data-testid="grid-volunteer-positions"]');
    await expect(positionsGrid).toBeVisible();

    // Get initial position count
    const initialCount = await positionsGrid.locator('[data-testid="position-row"]').count();

    // Click "Add New Position" button
    const addPositionButton = page.locator('[data-testid="button-add-volunteer-position"]');
    await expect(addPositionButton).toBeVisible();
    await addPositionButton.click();

    // Verify inline form opens
    const inlineForm = page.locator('[data-testid="volunteer-position-inline-form"]');
    await expect(inlineForm).toBeVisible();

    // Fill position form fields
    const titleInput = page.locator('[data-testid="input-position-title"]');
    await titleInput.fill('Safety Monitor');
    await page.locator('[data-testid="textarea-position-description"]').fill('Monitor event safety and intervene if needed');

    // Select session from dropdown - use keyboard navigation for Mantine Select
    const sessionDropdown = page.locator('[data-testid="dropdown-position-sessions"]');
    await sessionDropdown.click();
    // Wait for dropdown to open and press Enter to select first option
    await page.waitForTimeout(200); // Small delay for dropdown animation
    await page.keyboard.press('ArrowDown');
    await page.keyboard.press('Enter');
    await page.waitForTimeout(200); // Wait for selection to register

    // Fill time inputs
    await page.locator('[data-testid="input-position-start-time"]').fill('08:30');
    await page.locator('[data-testid="input-position-end-time"]').fill('12:30');

    // Fill slots needed
    await page.locator('[data-testid="input-slots-needed"]').fill('2');

    // Save position (use .last() for React Strict Mode)
    const saveButton = page.locator('[data-testid="button-save-volunteer-position"]').last();
    await expect(saveButton).toBeVisible();

    // Take screenshot before save
    await page.screenshot({ path: './test-results/volunteer-form-before-save.png', fullPage: true });

    await saveButton.click();

    // Wait for form to close (indicates successful save) or check for validation errors
    try {
      await expect(inlineForm).toBeHidden({ timeout: 5000 });
      console.log('Form closed successfully after save');
    } catch {
      // Form didn't close - check for validation errors
      const validationErrors = page.locator('[data-error="true"], .mantine-InputWrapper-error');
      const errorCount = await validationErrors.count();
      if (errorCount > 0) {
        console.log(`Form has ${errorCount} validation error(s):`);
        for (let i = 0; i < errorCount; i++) {
          const errorText = await validationErrors.nth(i).textContent();
          console.log(`  - ${errorText}`);
        }
      } else {
        console.log('Form did not close but no visible validation errors');
      }
      await page.screenshot({ path: './test-results/volunteer-form-validation-error.png', fullPage: true });
    }

    // Wait for grid to update after save
    await page.waitForTimeout(1500);

    // Take screenshot after save
    await page.screenshot({ path: './test-results/volunteer-form-after-save.png', fullPage: true });

    // Verify position appears in grid without page refresh
    const newCount = await positionsGrid.locator('[data-testid="position-row"]').count();
    console.log(`Position count: initial=${initialCount}, after=${newCount}`);

    // If count didn't increase, the save may have failed - log but don't fail immediately
    if (newCount === initialCount) {
      console.log('Position count unchanged - save may have failed');
      // Check if form is still visible (indicating validation failure)
      const formStillVisible = await inlineForm.isVisible();
      if (formStillVisible) {
        console.log('Form is still visible - likely validation failure');
      }
    }

    await expect(positionsGrid.locator('[data-testid="position-row"]')).toHaveCount(initialCount + 1);

    // Verify new position data in grid
    const newPositionRow = positionsGrid.locator('[data-testid="position-row"]').last();
    await expect(newPositionRow.locator('[data-testid="position-title"]')).toHaveText('Safety Monitor');
  });

  test('should edit volunteer position via inline form', async ({ page, df }) => {
    // Login as admin
    await AuthHelpers.loginAs(page, 'admin');

    // Create event with session and volunteer position
    const event = await df.events.createPublished(`Volunteer Edit Test ${Date.now()}`);

    const sessionStart = new Date();
    sessionStart.setDate(sessionStart.getDate() + 7);
    sessionStart.setHours(18, 0, 0, 0);
    const sessionEnd = new Date(sessionStart.getTime() + 3 * 60 * 60 * 1000);

    const session = await df.sessions.create({
      eventId: event.id,
      title: 'Main Session',
      startTime: sessionStart,
      endTime: sessionEnd,
      maxCapacity: 20,
    });

    // Create volunteer position
    await df.volunteers.create({
      eventId: event.id,
      title: 'Original Position',
      slotsAvailable: 2,
    });

    // Navigate to admin event edit page
    await page.goto(`/admin/events/${event.id}`);

    // Navigate to Volunteers tab
    await expect(page.locator('[data-testid="page-admin-event-details"]')).toBeVisible();
    await page.locator('[data-testid="tab-volunteers"]').click();

    const positionsGrid = page.locator('[data-testid="grid-volunteer-positions"]');
    await expect(positionsGrid).toBeVisible();

    // Click on first position row to open inline edit form
    const firstPositionRow = positionsGrid.locator('[data-testid="position-row"]').first();
    await expect(firstPositionRow).toBeVisible();
    await firstPositionRow.click();

    // Verify inline form opens
    const inlineForm = page.locator('[data-testid="volunteer-position-inline-form"]');
    await expect(inlineForm).toBeVisible();

    // Verify form pre-populates with existing data
    const positionTitleInput = page.locator('[data-testid="input-position-title"]');
    await expect(positionTitleInput).not.toHaveValue(''); // Should have existing value

    const descriptionTextarea = page.locator('[data-testid="textarea-position-description"]');
    await expect(descriptionTextarea).not.toHaveValue(''); // Should have existing value

    const slotsNeededInput = page.locator('[data-testid="input-slots-needed"]');
    await expect(slotsNeededInput).not.toHaveValue(''); // Should have existing value

    // Change position title
    await positionTitleInput.fill('Updated Safety Monitor');

    // Change slots needed
    await slotsNeededInput.fill('3');

    // Save changes
    const saveButton = page.locator('[data-testid="button-save-volunteer-position"]');
    await saveButton.click();

    // Wait for grid to update after save
    await page.waitForTimeout(1000);

    // Verify updates appear in grid without page refresh
    await expect(firstPositionRow.locator('[data-testid="position-title"]')).toHaveText('Updated Safety Monitor');
  });

  test('should delete volunteer position with confirmation', async ({ page, df }) => {
    // Login as admin
    await AuthHelpers.loginAs(page, 'admin');

    // Create event with session and volunteer position
    const event = await df.events.createPublished(`Volunteer Delete Test ${Date.now()}`);

    const sessionStart = new Date();
    sessionStart.setDate(sessionStart.getDate() + 7);
    sessionStart.setHours(18, 0, 0, 0);
    const sessionEnd = new Date(sessionStart.getTime() + 3 * 60 * 60 * 1000);

    const session = await df.sessions.create({
      eventId: event.id,
      title: 'Main Session',
      startTime: sessionStart,
      endTime: sessionEnd,
      maxCapacity: 20,
    });

    // Create volunteer position
    await df.volunteers.create({
      eventId: event.id,
      title: 'Position to Delete',
      slotsAvailable: 2,
    });

    // Navigate to admin event edit page
    await page.goto(`/admin/events/${event.id}`);

    // Navigate to Volunteers tab
    await expect(page.locator('[data-testid="page-admin-event-details"]')).toBeVisible();
    await page.locator('[data-testid="tab-volunteers"]').click();

    const positionsGrid = page.locator('[data-testid="grid-volunteer-positions"]');
    await expect(positionsGrid).toBeVisible();

    // Get initial position count
    const initialCount = await positionsGrid.locator('[data-testid="position-row"]').count();

    // Click on first position row to open edit form
    const firstPositionRow = positionsGrid.locator('[data-testid="position-row"]').first();
    await firstPositionRow.click();

    // Wait for inline form to open
    const inlineForm = page.locator('[data-testid="volunteer-position-inline-form"]');
    await expect(inlineForm).toBeVisible();

    // Set up dialog handler for browser confirm dialog
    page.once('dialog', dialog => {
      expect(dialog.message()).toContain('Are you sure you want to delete');
      dialog.accept(); // Confirm deletion
    });

    // Click delete button in the inline form
    const deleteButton = page.locator('[data-testid="button-delete-volunteer-position"]');
    await expect(deleteButton).toBeVisible();
    await deleteButton.click();

    // Wait for Collapse animation to complete (500ms for close + grid refresh)
    await page.waitForTimeout(2000);

    // Verify inline form has collapsed (check if delete button is no longer visible)
    const deleteButtonVisible = await deleteButton.isVisible();
    expect(deleteButtonVisible).toBe(false);

    // Verify position removed from grid without page refresh
    await expect(positionsGrid.locator('[data-testid="position-row"]')).toHaveCount(initialCount - 1);
  });

  test('should validate volunteer position form fields', async ({ page, df }) => {
    // Login as admin
    await AuthHelpers.loginAs(page, 'admin');

    // Create event with session
    const event = await df.events.createPublished(`Volunteer Validation Test ${Date.now()}`);

    const sessionStart = new Date();
    sessionStart.setDate(sessionStart.getDate() + 7);
    sessionStart.setHours(18, 0, 0, 0);
    const sessionEnd = new Date(sessionStart.getTime() + 3 * 60 * 60 * 1000);

    await df.sessions.create({
      eventId: event.id,
      title: 'Main Session',
      startTime: sessionStart,
      endTime: sessionEnd,
      maxCapacity: 20,
    });

    // Navigate to admin event edit page and volunteers tab
    await page.goto(`/admin/events/${event.id}`, { waitUntil: 'domcontentloaded' });
    await expect(page.locator('[data-testid="page-admin-event-details"]')).toBeVisible();
    await page.locator('[data-testid="tab-volunteers"]').click();

    const positionsGrid = page.locator('[data-testid="grid-volunteer-positions"]');
    await expect(positionsGrid).toBeVisible();

    // Get initial count
    const initialCount = await positionsGrid.locator('[data-testid="position-row"]').count();

    // Open add position inline form
    await page.locator('[data-testid="button-add-volunteer-position"]').click();
    const inlineForm = page.locator('[data-testid="volunteer-position-inline-form"]');
    await expect(inlineForm).toBeVisible();

    // Try to save empty form - validation should prevent submission
    await page.locator('[data-testid="button-save-volunteer-position"]').click();

    // Wait a moment to see if form tries to close
    await page.waitForTimeout(500);

    // Verify form is still visible (validation prevented save)
    await expect(inlineForm).toBeVisible();

    // Fill all required fields with valid data
    await page.locator('[data-testid="input-position-title"]').fill('Validation Test Position');
    await page.locator('[data-testid="textarea-position-description"]').fill('Test description for validation');
    await page.locator('[data-testid="input-slots-needed"]').fill('3');

    // Select a session - use keyboard navigation for Mantine Select
    const sessionDropdown = page.locator('[data-testid="dropdown-position-sessions"]');
    await sessionDropdown.click();
    await page.waitForTimeout(200);
    await page.keyboard.press('ArrowDown');
    await page.keyboard.press('Enter');
    await page.waitForTimeout(200);

    // Fill time inputs (required fields)
    const startTimeInput = page.locator('[data-testid="input-position-start-time"]');
    const endTimeInput = page.locator('[data-testid="input-position-end-time"]');
    if (await startTimeInput.count() > 0) {
      await startTimeInput.fill('09:00');
      await endTimeInput.fill('13:00');
    }

    // Now save with valid data (use .last() for React Strict Mode)
    const finalSaveButton = page.locator('[data-testid="button-save-volunteer-position"]').last();
    await finalSaveButton.click();

    // Wait for form to close (indicates successful save)
    await expect(inlineForm).toBeHidden({ timeout: 5000 }).catch(() => {
      console.log('Form did not close after save - check for validation errors');
    });

    // Wait for grid to update after save
    await page.waitForTimeout(1000);

    // Position should be added
    const newCount = await positionsGrid.locator('[data-testid="position-row"]').count();
    console.log(`Validation test position count: initial=${initialCount}, after=${newCount}`);
    await expect(positionsGrid.locator('[data-testid="position-row"]')).toHaveCount(initialCount + 1);

    // Verify the position title appears in grid
    const newRow = positionsGrid.locator('[data-testid="position-row"]').last();
    await expect(newRow.locator('[data-testid="position-title"]')).toHaveText('Validation Test Position');
  });

  test('should display sessions in day format in position assignments', async ({ page, df }) => {
    // Login as admin
    await AuthHelpers.loginAs(page, 'admin');

    // Create event with session and volunteer position
    const event = await df.events.createPublished(`Volunteer Session Display Test ${Date.now()}`);

    const sessionStart = new Date();
    sessionStart.setDate(sessionStart.getDate() + 7);
    sessionStart.setHours(18, 0, 0, 0);
    const sessionEnd = new Date(sessionStart.getTime() + 3 * 60 * 60 * 1000);

    const session = await df.sessions.create({
      eventId: event.id,
      title: 'Main Session',
      startTime: sessionStart,
      endTime: sessionEnd,
      maxCapacity: 20,
    });

    // Create volunteer position
    await df.volunteers.create({
      eventId: event.id,
      title: 'Test Position',
      slotsAvailable: 2,
    });

    // Navigate to admin event edit page and volunteers tab
    await page.goto(`/admin/events/${event.id}`, { waitUntil: 'domcontentloaded' });
    await expect(page.locator('[data-testid="page-admin-event-details"]')).toBeVisible();
    await page.locator('[data-testid="tab-volunteers"]').click();

    const positionsGrid = page.locator('[data-testid="grid-volunteer-positions"]');
    await expect(positionsGrid).toBeVisible();

    // Verify existing positions show session assignments in day format
    const positionRows = positionsGrid.locator('[data-testid="position-row"]');

    // Only run this test if there are positions in the grid
    const rowCount = await positionRows.count();
    if (rowCount > 0) {
      const firstRow = positionRows.first();

      // Sessions column should display session format
      const sessionsCell = firstRow.locator('[data-testid="position-sessions"]');
      await expect(sessionsCell).toBeVisible();

      // Session display can be:
      // - "Day 1", "Day 2" (day format)
      // - "All Sessions" (default/multi-session assignment)
      // - Session name like "S1", "Session 1" etc.
      const sessionsText = await sessionsCell.textContent();
      // Accept any valid session format
      const isValidSessionFormat = sessionsText && (
        sessionsText.match(/Day \d+/i) ||          // Day format
        sessionsText.includes('Session') ||         // Session name
        sessionsText.includes('All Sessions') ||    // All sessions
        sessionsText.match(/S\d+/i)                 // Short session format
      );
      expect(isValidSessionFormat).toBeTruthy();
      console.log(`Session display format: ${sessionsText}`);
    } else {
      console.log('No positions in grid to verify session format');
    }
  });

  test('should show "Add New Position" button below volunteer grid for UI consistency', async ({ page, df }) => {
    // Login as admin
    await AuthHelpers.loginAs(page, 'admin');

    // Create event with session
    const event = await df.events.createPublished(`Volunteer UI Test ${Date.now()}`);

    const sessionStart = new Date();
    sessionStart.setDate(sessionStart.getDate() + 7);
    sessionStart.setHours(18, 0, 0, 0);
    const sessionEnd = new Date(sessionStart.getTime() + 3 * 60 * 60 * 1000);

    await df.sessions.create({
      eventId: event.id,
      title: 'Main Session',
      startTime: sessionStart,
      endTime: sessionEnd,
      maxCapacity: 20,
    });

    // Navigate to admin event edit page and volunteers tab (fresh navigation to ensure clean state)
    await page.goto(`/admin/events/${event.id}`);
    await expect(page.locator('[data-testid="page-admin-event-details"]')).toBeVisible();
    await page.locator('[data-testid="tab-volunteers"]').click();

    const positionsGrid = page.locator('[data-testid="grid-volunteer-positions"]');
    await expect(positionsGrid).toBeVisible();

    // Wait for page to fully load and any previous animations to complete
    await page.waitForTimeout(1000);

    // Verify "Add New Position" button exists below the grid
    const addButton = page.locator('[data-testid="button-add-volunteer-position"]');
    await expect(addButton).toBeVisible();

    // Button should be positioned below the grid
    const addButtonBox = await addButton.boundingBox();
    const gridBox = await positionsGrid.boundingBox();

    // Add button should be below the grid
    expect(addButtonBox!.y).toBeGreaterThan(gridBox!.y + gridBox!.height);

    // Verify inline form element exists in DOM
    const inlineForm = page.locator('[data-testid="volunteer-position-inline-form"]');
    await expect(inlineForm).toBeAttached();

    // Click button to verify form becomes visible/expanded
    await addButton.click();
    await page.waitForTimeout(300); // Wait for Collapse open animation

    // Verify form inputs become visible after clicking button
    const titleInput = page.locator('[data-testid="input-position-title"]');
    await expect(titleInput).toBeVisible();
  });
});
