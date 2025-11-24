/**
 * E2E Tests: Admin Venue Editing
 *
 * Tests admin ability to edit existing venues including
 * toggling active/inactive status.
 *
 * Test Coverage:
 * - Admin can edit venue name, directions, and notes
 * - Toggle venue active/inactive status
 * - Changes persist after save
 * - Non-admins cannot edit venues
 *
 * @see /apps/api/Endpoints/Admin/VenueEndpoints.cs
 */

import { test, expect } from '@playwright/test';
import { AuthHelpers } from './test-utils/helpers/auth.helpers';

// Venue Management is implemented in AdminSettingsPage (right column card)
// Route: /admin/settings (NOT /admin/venues)
// Component: VenueManagementCard - select venue from dropdown to edit
test.describe('Admin Venue Editing', () => {
  test.beforeEach(async ({ page }) => {
    // Clear auth state before each test
    await AuthHelpers.clearAuthState(page);
  });

  test('should edit existing venue name and directions', async ({ page }) => {
    // Login as admin
    await AuthHelpers.loginAs(page, 'admin');

    // Navigate to settings
    await page.goto('http://localhost:5173/admin/settings');
    await page.waitForLoadState('networkidle');

    // Scope all selectors to the Venue Management card container
    // Find the card by filtering for the one that contains both "Venue Management" heading AND "Select a venue" placeholder
    const venueCard = page.getByRole('main').locator('[style*="background"]').filter({ hasText: 'Venue Management' });

    // Open venue dropdown within the card
    const venueDropdown = venueCard.getByPlaceholder('Select a venue');
    await venueDropdown.click();
    await page.waitForTimeout(500);

    // Select first REAL venue from dropdown (not "Add New")
    // Wait for options to appear, then click the first one that's NOT "Add New"
    await page.getByRole('option', { name: 'Add New' }).waitFor({ state: 'visible', timeout: 5000 });

    // Get all option elements and click the second one (first is "Add New", second is a real venue)
    const allOptions = page.getByRole('option');
    await allOptions.nth(1).click();  // Index 0 is "Add New", index 1 is first real venue
    await page.waitForTimeout(500);

    // Wait for form to fully load with venue data
    await page.waitForTimeout(1000);

    // Fill updated venue details (scope to card)
    const updatedName = `Updated Venue ${Date.now()}`;
    const updatedDirections = `Updated directions ${Date.now()}`;

    const nameInput = venueCard.locator('input[placeholder="Enter venue name"]');
    await nameInput.waitFor({ state: 'visible', timeout: 5000 });

    // Clear and fill name - use triple click to select all first
    await nameInput.click({ clickCount: 3 });
    await nameInput.fill(updatedName);

    const directionsInput = venueCard.locator('textarea[placeholder="Enter directions to venue"]');
    await directionsInput.click({ clickCount: 3 });
    await directionsInput.fill(updatedDirections);

    // Wait a moment for form validation to process
    await page.waitForTimeout(500);

    // Save changes - use data-testid for more reliable selection
    const updateButton = venueCard.getByTestId('venue-submit-button');
    await updateButton.scrollIntoViewIfNeeded();

    // Wait for button to be enabled (form validation)
    await expect(updateButton).toBeEnabled({ timeout: 5000 });

    await updateButton.click();

    // Wait for save to complete and verify success notification
    const successNotification = page.locator('[role="alert"]:has-text("Venue updated successfully")');
    await expect(successNotification).toBeVisible({ timeout: 10000 });

    // Verify updated name appears in dropdown
    await page.goto('http://localhost:5173/admin/settings');
    await page.waitForLoadState('networkidle');

    // Re-scope to venue card after page reload
    const venueCard2 = page.getByRole('main').locator('[style*="background"]').filter({ hasText: 'Venue Management' });
    const venueDropdown2 = venueCard2.getByPlaceholder('Select a venue');
    await venueDropdown2.click();
    await page.waitForTimeout(500);

    // Verify updated name appears as an option in the dropdown
    await expect(page.getByRole('option', { name: updatedName })).toBeVisible({ timeout: 5000 });
  });

  test('should update venue notes (admin-only field)', async ({ page }) => {
    // Login as admin
    await AuthHelpers.loginAs(page, 'admin');

    // Navigate to settings
    await page.goto('http://localhost:5173/admin/settings');
    await page.waitForLoadState('networkidle');

    // Scope all selectors to the Venue Management card container
    // Find the card by filtering for the one that contains both "Venue Management" heading AND "Select a venue" placeholder
    const venueCard = page.getByRole('main').locator('[style*="background"]').filter({ hasText: 'Venue Management' });

    // Select first venue
    const venueDropdown = venueCard.getByPlaceholder('Select a venue');
    await venueDropdown.click();
    await page.waitForTimeout(500);

    // Select first REAL venue from dropdown (not "Add New")
    await page.getByRole('option', { name: 'Add New' }).waitFor({ state: 'visible', timeout: 5000 });

    // Get all options and extract venue name from second option (first real venue)
    const allOptions = page.getByRole('option');
    const firstVenueOption = allOptions.nth(1);  // Index 0 is "Add New", index 1 is first real venue
    let venueName = (await firstVenueOption.textContent()) || '';
    await firstVenueOption.click();
    await page.waitForTimeout(500);

    // Update notes field (scope to card)
    const updatedNotes = `Admin notes updated ${Date.now()}`;
    const notesInput = venueCard.locator('textarea[placeholder="Enter additional notes"]');

    await notesInput.waitFor({ state: 'visible', timeout: 5000 });
    await notesInput.clear();
    await notesInput.fill(updatedNotes);

    // Save changes - use data-testid for more reliable selection
    const updateButton = venueCard.getByTestId('venue-submit-button');
    await updateButton.scrollIntoViewIfNeeded();
    await updateButton.click();
    await page.waitForTimeout(1000);

    // Verify success
    await expect(page.locator('[role="alert"]:has-text("Venue updated successfully")')).toBeVisible({ timeout: 5000 });

    // Verify saved by re-selecting venue
    await page.goto('http://localhost:5173/admin/settings');
    await page.waitForLoadState('networkidle');

    // Re-scope to venue card after page reload
    const venueCard2 = page.getByRole('main').locator('[style*="background"]').filter({ hasText: 'Venue Management' });
    const venueDropdown2 = venueCard2.getByPlaceholder('Select a venue');
    await venueDropdown2.click();
    await page.waitForTimeout(500);

    // Click the venue option in the dropdown
    await page.getByRole('option', { name: venueName }).click();
    await page.waitForTimeout(1000);

    // Verify notes were saved (scope to card)
    const notesValue = await venueCard2.locator('textarea[placeholder="Enter additional notes"]').inputValue();
    expect(notesValue).toContain(updatedNotes);
  });

  test('should toggle venue active/inactive status', async ({ page }) => {
    // Login as admin
    await AuthHelpers.loginAs(page, 'admin');

    // Navigate to settings
    await page.goto('http://localhost:5173/admin/settings');
    await page.waitForLoadState('networkidle');

    // Scope all selectors to the Venue Management card container
    // Find the card by filtering for the one that contains both "Venue Management" heading AND "Select a venue" placeholder
    const venueCard = page.getByRole('main').locator('[style*="background"]').filter({ hasText: 'Venue Management' });

    // Select first venue
    const venueDropdown = venueCard.getByPlaceholder('Select a venue');
    await venueDropdown.click();
    await page.waitForTimeout(500);

    // Select first REAL venue from dropdown (not "Add New")
    await page.getByRole('option', { name: 'Add New' }).waitFor({ state: 'visible', timeout: 5000 });

    // Get all options and extract venue name from second option (first real venue)
    const allOptions = page.getByRole('option');
    const firstVenueOption = allOptions.nth(1);  // Index 0 is "Add New", index 1 is first real venue
    let venueName = (await firstVenueOption.textContent()) || '';
    await firstVenueOption.click();
    await page.waitForTimeout(500);

    // Wait for form to fully load with venue data
    await page.waitForTimeout(1000);

    // Find "Active Venue" checkbox (scope to card)
    // Only shown when editing (isEditMode = true) - use data-testid for reliable selection
    const activeToggle = venueCard.getByTestId('venue-active-checkbox');

    // Wait for checkbox to be visible
    await activeToggle.waitFor({ state: 'visible', timeout: 5000 });

    // Get current state
    const wasChecked = await activeToggle.isChecked();

    // Toggle it
    await activeToggle.click();

    // Verify it toggled
    const isNowChecked = await activeToggle.isChecked();
    expect(isNowChecked).toBe(!wasChecked);

    // Wait for form validation
    await page.waitForTimeout(500);

    // Save changes - use data-testid for more reliable selection
    const updateButton = venueCard.getByTestId('venue-submit-button');
    await updateButton.scrollIntoViewIfNeeded();

    // Wait for button to be enabled
    await expect(updateButton).toBeEnabled({ timeout: 5000 });

    await updateButton.click();

    // Verify success
    await expect(page.locator('[role="alert"]:has-text("Venue updated successfully")')).toBeVisible({ timeout: 10000 });

    // Verify change persisted by re-selecting venue
    await page.goto('http://localhost:5173/admin/settings');
    await page.waitForLoadState('networkidle');

    // Re-scope to venue card after page reload
    const venueCard2 = page.getByRole('main').locator('[style*="background"]').filter({ hasText: 'Venue Management' });
    const venueDropdown2 = venueCard2.getByPlaceholder('Select a venue');
    await venueDropdown2.click();
    await page.waitForTimeout(500);

    // If we deactivated, venue should appear with "(Inactive)" suffix (line 129)
    const venueOption = wasChecked
      ? page.locator(`text="${venueName} (Inactive)"`)
      : page.locator(`text="${venueName}"`).filter({ hasNot: page.locator('text="(Inactive)"') });

    await venueOption.click();
    await page.waitForTimeout(500);

    const activeToggle2 = venueCard2.getByTestId('venue-active-checkbox');
    await activeToggle2.waitFor({ state: 'visible', timeout: 5000 });
    const finalState = await activeToggle2.isChecked();
    expect(finalState).toBe(!wasChecked);
  });

  test('should show inactive venues in admin dropdown', async ({ page }) => {
    // Login as admin
    await AuthHelpers.loginAs(page, 'admin');

    // Navigate to settings
    await page.goto('http://localhost:5173/admin/settings');
    await page.waitForLoadState('networkidle');

    // Scope all selectors to the Venue Management card container
    // Find the card by filtering for the one that contains both "Venue Management" heading AND "Select a venue" placeholder
    const venueCard = page.getByRole('main').locator('[style*="background"]').filter({ hasText: 'Venue Management' });

    // Select first venue and make it inactive
    const venueDropdown = venueCard.getByPlaceholder('Select a venue');
    await venueDropdown.click();
    await page.waitForTimeout(500);

    // Select first REAL venue from dropdown (not "Add New")
    await page.getByRole('option', { name: 'Add New' }).waitFor({ state: 'visible', timeout: 5000 });

    // Get all options and click the second one (first is "Add New", second is a real venue)
    const allOptions = page.getByRole('option');
    await allOptions.nth(1).click();  // Index 0 is "Add New", index 1 is first real venue
    await page.waitForTimeout(500);

    // Wait for form to fully load with venue data
    await page.waitForTimeout(1000);

    // Get venue name from input (scope to card)
    const nameInput = venueCard.locator('input[placeholder="Enter venue name"]');
    await nameInput.waitFor({ state: 'visible', timeout: 5000 });
    const venueNameValue = await nameInput.inputValue();

    // Set to inactive (scope to card) - use data-testid for reliable selection
    const activeToggle = venueCard.getByTestId('venue-active-checkbox');
    await activeToggle.waitFor({ state: 'visible', timeout: 5000 });

    if (await activeToggle.isChecked()) {
      await activeToggle.click();
    }

    // Wait for form validation
    await page.waitForTimeout(500);

    // Save (scope to card) - use data-testid for more reliable selection
    const updateButton = venueCard.getByTestId('venue-submit-button');
    await updateButton.scrollIntoViewIfNeeded();

    // Wait for button to be enabled
    await expect(updateButton).toBeEnabled({ timeout: 5000 });

    await updateButton.click();

    // Wait for save to complete
    await expect(page.locator('[role="alert"]:has-text("Venue updated successfully")')).toBeVisible({ timeout: 10000 });

    // Verify inactive venue still appears in admin dropdown with "(Inactive)" label
    await page.goto('http://localhost:5173/admin/settings');
    await page.waitForLoadState('networkidle');

    // Re-scope to venue card after page reload
    const venueCard2 = page.getByRole('main').locator('[style*="background"]').filter({ hasText: 'Venue Management' });
    const venueDropdown2 = venueCard2.getByPlaceholder('Select a venue');
    await venueDropdown2.click();
    await page.waitForTimeout(500);

    // Inactive venues shown with "(Inactive)" suffix (VenueManagementCard line 125-131)
    await expect(page.locator(`text="${venueNameValue} (Inactive)"`)).toBeVisible({ timeout: 5000 });
  });

  test('should prevent non-admin from accessing admin settings', async ({ page }) => {
    // Login as regular member
    await AuthHelpers.loginAs(page, 'member');

    // Try to navigate to admin settings (where venue management is)
    await page.goto('http://localhost:5173/admin/settings');
    await page.waitForTimeout(1000);

    // Should be redirected or see forbidden/unauthorized message
    const currentUrl = page.url();

    // Either redirected away from admin page or see error
    const isBlocked =
      !currentUrl.includes('/admin/settings') ||
      (await page.locator('text=/forbidden/i, text=/unauthorized/i, text=/access denied/i').count()) > 0 ||
      (await page.locator('[data-testid="error-message"], [role="alert"]').count()) > 0;

    expect(isBlocked).toBe(true);
  });

  test('should discard changes by selecting different venue', async ({ page }) => {
    // Login as admin
    await AuthHelpers.loginAs(page, 'admin');

    // Navigate to settings
    await page.goto('http://localhost:5173/admin/settings');
    await page.waitForLoadState('networkidle');

    // Scope all selectors to the Venue Management card container
    // Find the card by filtering for the one that contains both "Venue Management" heading AND "Select a venue" placeholder
    const venueCard = page.getByRole('main').locator('[style*="background"]').filter({ hasText: 'Venue Management' });

    // Select first venue
    const venueDropdown = venueCard.getByPlaceholder('Select a venue');
    await venueDropdown.click();
    await page.waitForTimeout(500);

    // Select first REAL venue from dropdown (not "Add New")
    await page.getByRole('option', { name: 'Add New' }).waitFor({ state: 'visible', timeout: 5000 });

    // Get all options and click the second one (first is "Add New", second is a real venue)
    const allOptions = page.getByRole('option');
    await allOptions.nth(1).click();  // Index 0 is "Add New", index 1 is first real venue
    await page.waitForTimeout(500);

    // Get original name (scope to card)
    const nameInput = venueCard.locator('input[placeholder="Enter venue name"]');
    await nameInput.waitFor({ state: 'visible', timeout: 5000 });
    const originalName = await nameInput.inputValue();

    // Verify we got a venue name (not empty)
    expect(originalName).toBeTruthy();
    expect(originalName.length).toBeGreaterThan(0);

    // Make a change (scope to card)
    await nameInput.clear();
    await nameInput.fill('This Should Not Save');

    // Select a different venue (discards changes)
    await venueDropdown.click();
    await page.waitForTimeout(500);

    // Select "Add New" to clear the form
    await page.locator('text="Add New"').click();
    await page.waitForTimeout(500);

    // Go back and re-select original venue to verify changes were not saved
    await venueDropdown.click();
    await page.waitForTimeout(500);

    // Use role option for more reliable selection
    await page.getByRole('option', { name: originalName }).click();
    await page.waitForTimeout(500);

    // Verify original name is still there (scope to card)
    const currentName = await venueCard.locator('input[placeholder="Enter venue name"]').inputValue();
    expect(currentName).toBe(originalName);
  });
});
