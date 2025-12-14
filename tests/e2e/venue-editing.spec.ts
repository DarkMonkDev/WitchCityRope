/**
 * E2E Tests: Admin Venue Editing
 *
 * Tests admin ability to edit existing venues including
 * toggling active/inactive status.
 *
 * Test Coverage:
 * - Admin can edit venue name, directions, and information
 * - Toggle venue active/inactive status
 * - Changes persist after save
 * - Non-admins cannot edit venues
 *
 * @see /apps/api/Endpoints/Admin/VenueEndpoints.cs
 */

import { test, expect } from '@playwright/test';
import { AuthHelpers } from './test-utils/helpers/auth.helpers';

// Environment-aware URLs for container/host compatibility
const WEB_BASE_URL = process.env.PLAYWRIGHT_BASE_URL || 'http://localhost:5173';
const API_BASE_URL = process.env.API_URL || 'http://localhost:5655';


// Venue Management is implemented in AdminSettingsPage (right column card)
// Route: /admin/settings (NOT /admin/venues)
// Component: VenueManagementCard - select venue from dropdown to edit
const baseUrl = process.env.PLAYWRIGHT_BASE_URL || WEB_BASE_URL;

test.describe('Admin Venue Editing', () => {
  test.beforeEach(async ({ page }) => {
    // Clear auth state before each test
    await AuthHelpers.clearAuthState(page);
  });

  test('should edit existing venue name and directions', async ({ page }) => {
    // Login as admin
    await AuthHelpers.loginAs(page, 'admin');

    // Navigate to settings
    await page.goto(`${baseUrl}/admin/settings`, { waitUntil: 'domcontentloaded' });
    await page.waitForLoadState('domcontentloaded');

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
    await page.goto(`${baseUrl}/admin/settings`, { waitUntil: 'domcontentloaded' });
    await page.waitForLoadState('domcontentloaded');

    // Re-scope to venue card after page reload
    const venueCard2 = page.getByRole('main').locator('[style*="background"]').filter({ hasText: 'Venue Management' });
    const venueDropdown2 = venueCard2.getByPlaceholder('Select a venue');
    await venueDropdown2.click();
    await page.waitForTimeout(500);

    // Verify updated name appears as an option in the dropdown
    await expect(page.getByRole('option', { name: updatedName })).toBeVisible({ timeout: 5000 });
  });

  test('should update venue information (admin-only field)', async ({ page }) => {
    // Login as admin
    await AuthHelpers.loginAs(page, 'admin');

    // Navigate to settings
    await page.goto(`${baseUrl}/admin/settings`, { waitUntil: 'domcontentloaded' });
    await page.waitForLoadState('domcontentloaded');

    // Scope all selectors to the Venue Management card container using data-testid
    const venueCard = page.getByTestId('venue-management-card');
    await venueCard.waitFor({ state: 'visible', timeout: 5000 });

    // Select a venue (use index 2 to avoid conflict with delete test)
    const venueDropdown = venueCard.getByTestId('venue-select');
    await venueDropdown.click();
    await page.waitForTimeout(500);

    // Select SECOND real venue from dropdown (index 2) to avoid conflict with delete test
    await page.getByRole('option', { name: 'Add New' }).waitFor({ state: 'visible', timeout: 5000 });

    // Get all options and extract venue name from third option (second real venue)
    // Using index 2 because delete test uses index 1, avoiding parallel test conflicts
    const allOptions = page.getByRole('option');
    const secondVenueOption = allOptions.nth(2);  // Index 0 is "Add New", index 2 is second real venue
    let venueName = (await secondVenueOption.textContent()) || '';
    await secondVenueOption.click();
    await page.waitForTimeout(500);

    // Update venue information field using correct data-testid
    const updatedInfo = `Venue info updated ${Date.now()}. Capacity: 50.`;
    const infoInput = venueCard.getByTestId('venue-information-input');

    await infoInput.waitFor({ state: 'visible', timeout: 5000 });
    await infoInput.clear();
    await infoInput.fill(updatedInfo);

    // Save changes - use data-testid for more reliable selection
    const updateButton = venueCard.getByTestId('venue-submit-button');
    await updateButton.scrollIntoViewIfNeeded();
    await updateButton.click();
    await page.waitForTimeout(1000);

    // Verify success
    await expect(page.locator('[role="alert"]:has-text("Venue updated successfully")')).toBeVisible({ timeout: 5000 });

    // Verify saved by re-selecting venue
    await page.goto(`${baseUrl}/admin/settings`, { waitUntil: 'domcontentloaded' });
    await page.waitForLoadState('domcontentloaded');

    // Re-scope to venue card after page reload using data-testid
    const venueCard2 = page.getByTestId('venue-management-card');
    const venueDropdown2 = venueCard2.getByTestId('venue-select');
    await venueDropdown2.click();
    await page.waitForTimeout(500);

    // Click the venue option in the dropdown (wait for it to appear first)
    const venueOption = page.getByRole('option', { name: venueName });
    await venueOption.waitFor({ state: 'visible', timeout: 5000 });
    await venueOption.click();
    await page.waitForTimeout(1000);

    // Verify venue information was saved using correct data-testid
    const infoValue = await venueCard2.getByTestId('venue-information-input').inputValue();
    expect(infoValue).toContain(updatedInfo);
  });

  test('should delete venue (soft delete sets inactive)', async ({ page }) => {
    // CORRECTED: There is NO active/inactive checkbox toggle
    // The UI uses DELETE button for soft delete (sets IsActive = false)
    // Login as admin
    await AuthHelpers.loginAs(page, 'admin');

    // Navigate to settings
    await page.goto(`${baseUrl}/admin/settings`, { waitUntil: 'domcontentloaded' });
    await page.waitForLoadState('domcontentloaded');

    // Scope all selectors to the Venue Management card using data-testid
    const venueCard = page.getByTestId('venue-management-card');
    await venueCard.waitFor({ state: 'visible', timeout: 5000 });

    // Select first venue
    const venueDropdown = venueCard.getByTestId('venue-select');
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

    // Find Delete button (only shown in edit mode)
    const deleteButton = venueCard.getByTestId('venue-delete-button');
    await expect(deleteButton).toBeVisible({ timeout: 5000 });

    // Setup dialog handler BEFORE clicking delete button
    page.on('dialog', async dialog => {
      expect(dialog.type()).toBe('confirm');
      await dialog.accept();
    });

    // Click delete button
    await deleteButton.click();

    // Wait for deletion to complete
    await page.waitForTimeout(2000);

    // Verify success
    await expect(page.locator('[role="alert"]:has-text("Venue deleted successfully")')).toBeVisible({ timeout: 10000 });

    // Verify venue no longer appears in dropdown (soft deleted = IsActive = false)
    await page.goto(`${baseUrl}/admin/settings`, { waitUntil: 'domcontentloaded' });
    await page.waitForLoadState('domcontentloaded');

    const venueCard2 = page.getByTestId('venue-management-card');
    const venueDropdown2 = venueCard2.getByTestId('venue-select');
    await venueDropdown2.click();
    await page.waitForTimeout(500);

    // Deleted venue should NOT appear in dropdown (component only shows active venues)
    const deletedVenueOption = page.getByRole('option', { name: venueName });
    await expect(deletedVenueOption).not.toBeVisible({ timeout: 2000 });
  });

  test('should only show active venues in dropdown', async ({ page }) => {
    // CORRECTED: Component query fetches ONLY active venues
    // See line 59: queryKey: ['admin', 'venues', 'active']
    // See line 61: const response = await api.get<VenueDto[]>('/api/admin/venues/active');
    // Inactive venues DO NOT appear in dropdown - this is by design

    // Login as admin
    await AuthHelpers.loginAs(page, 'admin');

    // Navigate to settings
    await page.goto(`${baseUrl}/admin/settings`, { waitUntil: 'domcontentloaded' });
    await page.waitForLoadState('domcontentloaded');

    // Scope all selectors to the Venue Management card using data-testid
    const venueCard = page.getByTestId('venue-management-card');
    await venueCard.waitFor({ state: 'visible', timeout: 5000 });

    // Open venue dropdown
    const venueDropdown = venueCard.getByTestId('venue-select');
    await venueDropdown.click();
    await page.waitForTimeout(500);

    // Verify "Add New" option is visible
    await expect(page.getByRole('option', { name: 'Add New' })).toBeVisible({ timeout: 5000 });

    // Get all options
    const allOptions = page.getByRole('option');
    const optionCount = await allOptions.count();

    // Verify we have at least 2 options (Add New + at least one active venue)
    expect(optionCount).toBeGreaterThanOrEqual(2);

    // Verify all options are either "Add New" or active venues (no "(Inactive)" suffix)
    for (let i = 0; i < optionCount; i++) {
      const optionText = await allOptions.nth(i).textContent();
      // Should NOT contain "(Inactive)" - only active venues shown
      expect(optionText).not.toContain('(Inactive)');
    }

    console.log(`✅ Verified ${optionCount} options, all are active venues or "Add New"`);
  });

  test('should prevent non-admin from accessing admin settings', async ({ page }) => {
    // Login as regular member
    await AuthHelpers.loginAs(page, 'member');

    // Try to navigate to admin settings (where venue management is)
    await page.goto(`${baseUrl}/admin/settings`, { waitUntil: 'domcontentloaded' });
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
    await page.goto(`${baseUrl}/admin/settings`, { waitUntil: 'domcontentloaded' });
    await page.waitForLoadState('domcontentloaded');

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
