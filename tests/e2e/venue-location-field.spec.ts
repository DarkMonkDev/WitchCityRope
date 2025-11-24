/**
 * E2E Tests: Admin Venue Location Field
 *
 * Tests the new Location field in the admin venue management form.
 * The Location field provides public city/state information shown to
 * non-vetted users before RSVP or ticket purchase.
 *
 * Test Coverage:
 * - Admin can create venue with Location field
 * - Admin can update venue Location field
 * - Location field is optional (nullable)
 * - Location field enforces 100 character max length
 * - Location field displays helper text
 * - Location field saves correctly to database
 *
 * @see /apps/web/src/components/admin/VenueManagementCard.tsx
 * @see /apps/api/Features/Admin/VenueEndpoints.cs
 */

import { test, expect } from '@playwright/test';
import { AuthHelpers } from './test-utils/helpers/auth.helpers';

test.describe('Admin Venue Location Field', () => {
  test.beforeEach(async ({ page }) => {
    // Clear auth state before each test
    await AuthHelpers.clearAuthState(page);
  });

  test('should display Location field in venue form', async ({ page }) => {
    // Login as admin
    await AuthHelpers.loginAs(page, 'admin');

    // Navigate to admin settings
    await page.goto('http://localhost:5173/admin/settings');
    await page.waitForLoadState('networkidle');

    // Verify page loaded
    await expect(page).toHaveURL(/\/admin\/settings/);

    // Look for "Venue Management" heading
    const venueHeading = page.locator('text=/Venue Management/i');
    await expect(venueHeading).toBeVisible({ timeout: 5000 });

    // Open venue dropdown and select "Add New"
    const venueDropdown = page.getByPlaceholder('Select a venue');
    await venueDropdown.click();
    await page.waitForTimeout(500);

    await page.locator('text="Add New"').click();
    await page.waitForTimeout(500);

    // Verify Location field is present with data-testid
    const locationField = page.getByTestId('venue-location-input');
    await expect(locationField).toBeVisible({ timeout: 5000 });

    // Verify placeholder text
    await expect(locationField).toHaveAttribute('placeholder', 'e.g., Salem, MA');

    // Verify helper text is present
    const helperText = page.locator('text=/Public location.*shown to non-vetted users/i');
    await expect(helperText).toBeVisible();

    console.log('✅ Location field displayed with correct attributes and helper text');
  });

  test('should create new venue with Location field', async ({ page }) => {
    // Login as admin
    await AuthHelpers.loginAs(page, 'admin');

    // Navigate to admin settings
    await page.goto('http://localhost:5173/admin/settings');
    await page.waitForLoadState('networkidle');

    // Open dropdown and select "Add New"
    const venueDropdown = page.getByPlaceholder('Select a venue');
    await venueDropdown.click();
    await page.waitForTimeout(500);

    await page.locator('text="Add New"').click();
    await page.waitForTimeout(500);

    // Fill venue details including location
    const uniqueName = `Test Venue with Location ${Date.now()}`;
    const locationValue = 'Salem, MA';

    await page.locator('input[placeholder="Enter venue name"]').fill(uniqueName);
    await page.getByTestId('venue-location-input').fill(locationValue);
    await page.locator('textarea[placeholder="Enter directions to venue"]').fill('123 Main Street, Salem, MA 01970');

    // Submit form
    const createButton = page.locator('button:has-text("Create Venue")');
    await createButton.click();

    // Wait for success notification
    await page.waitForTimeout(1000);

    // Verify success notification
    const successNotification = page.locator('[role="alert"]:has-text("Venue created successfully")');
    await expect(successNotification).toBeVisible({ timeout: 5000 });

    // Reload page to verify persistence
    await page.goto('http://localhost:5173/admin/settings');
    await page.waitForLoadState('networkidle');

    // Select the newly created venue from dropdown
    await venueDropdown.click();
    await page.waitForTimeout(500);

    const newVenueOption = page.locator(`text="${uniqueName}"`);
    await expect(newVenueOption).toBeVisible({ timeout: 5000 });
    await newVenueOption.click();

    await page.waitForTimeout(1000);

    // Verify Location field contains the saved value
    const locationField = page.getByTestId('venue-location-input');
    await expect(locationField).toHaveValue(locationValue);

    console.log('✅ Venue created with Location field and value persisted');
  });

  test('should create venue without Location field (optional)', async ({ page }) => {
    // Login as admin
    await AuthHelpers.loginAs(page, 'admin');

    // Navigate to admin settings
    await page.goto('http://localhost:5173/admin/settings');
    await page.waitForLoadState('networkidle');

    // Open dropdown and select "Add New"
    const venueDropdown = page.getByPlaceholder('Select a venue');
    await venueDropdown.click();
    await page.waitForTimeout(500);

    await page.locator('text="Add New"').click();
    await page.waitForTimeout(500);

    // Fill only required fields (name), leave Location empty
    const uniqueName = `Venue without Location ${Date.now()}`;

    await page.locator('input[placeholder="Enter venue name"]').fill(uniqueName);
    await page.locator('textarea[placeholder="Enter directions to venue"]').fill('Virtual event - Zoom link provided');

    // Leave Location field empty (verify it's optional)
    const locationField = page.getByTestId('venue-location-input');
    await expect(locationField).toHaveValue('');

    // Submit form
    const createButton = page.locator('button:has-text("Create Venue")');
    await createButton.click();

    // Wait for success
    await page.waitForTimeout(1000);

    // Verify success notification
    const successNotification = page.locator('[role="alert"]:has-text("Venue created successfully")');
    await expect(successNotification).toBeVisible({ timeout: 5000 });

    // Verify venue created successfully without location
    await page.goto('http://localhost:5173/admin/settings');
    await page.waitForLoadState('networkidle');

    await venueDropdown.click();
    await page.waitForTimeout(500);

    await expect(page.locator(`text="${uniqueName}"`)).toBeVisible({ timeout: 5000 });

    console.log('✅ Venue created successfully without Location field (optional)');
  });

  test('should update venue Location field', async ({ page }) => {
    // Login as admin
    await AuthHelpers.loginAs(page, 'admin');

    // First create a venue
    await page.goto('http://localhost:5173/admin/settings');
    await page.waitForLoadState('networkidle');

    const venueDropdown = page.getByPlaceholder('Select a venue');

    // Create venue with initial location
    await venueDropdown.click();
    await page.waitForTimeout(500);
    await page.locator('text="Add New"').click();
    await page.waitForTimeout(500);

    const uniqueName = `Venue to Update ${Date.now()}`;
    const initialLocation = 'Salem, MA';

    await page.locator('input[placeholder="Enter venue name"]').fill(uniqueName);
    await page.getByTestId('venue-location-input').fill(initialLocation);
    await page.locator('textarea[placeholder="Enter directions to venue"]').fill('Initial directions');

    await page.locator('button:has-text("Create Venue")').click();
    await page.waitForTimeout(1000);

    // Now update the location
    await page.goto('http://localhost:5173/admin/settings');
    await page.waitForLoadState('networkidle');

    // Select the venue
    await venueDropdown.click();
    await page.waitForTimeout(500);
    await page.locator(`text="${uniqueName}"`).click();
    await page.waitForTimeout(1000);

    // Update location field
    const locationField = page.getByTestId('venue-location-input');
    await locationField.clear();
    await locationField.fill('Boston, MA');

    // Submit update
    const updateButton = page.locator('button:has-text("Update Venue")');
    await updateButton.click();

    // Wait for success
    await page.waitForTimeout(1000);

    // Verify success notification
    const successNotification = page.locator('[role="alert"]:has-text("Venue updated successfully")');
    await expect(successNotification).toBeVisible({ timeout: 5000 });

    // Reload and verify updated value persisted
    await page.goto('http://localhost:5173/admin/settings');
    await page.waitForLoadState('networkidle');

    await venueDropdown.click();
    await page.waitForTimeout(500);
    await page.locator(`text="${uniqueName}"`).click();
    await page.waitForTimeout(1000);

    await expect(locationField).toHaveValue('Boston, MA');

    console.log('✅ Venue Location field updated successfully');
  });

  test('should clear venue Location field (set to null)', async ({ page }) => {
    // Login as admin
    await AuthHelpers.loginAs(page, 'admin');

    // Create venue with location
    await page.goto('http://localhost:5173/admin/settings');
    await page.waitForLoadState('networkidle');

    const venueDropdown = page.getByPlaceholder('Select a venue');

    await venueDropdown.click();
    await page.waitForTimeout(500);
    await page.locator('text="Add New"').click();
    await page.waitForTimeout(500);

    const uniqueName = `Venue Location Clear ${Date.now()}`;

    await page.locator('input[placeholder="Enter venue name"]').fill(uniqueName);
    await page.getByTestId('venue-location-input').fill('Salem, MA');
    await page.locator('textarea[placeholder="Enter directions to venue"]').fill('Some directions');

    await page.locator('button:has-text("Create Venue")').click();
    await page.waitForTimeout(1000);

    // Now clear the location
    await page.goto('http://localhost:5173/admin/settings');
    await page.waitForLoadState('networkidle');

    await venueDropdown.click();
    await page.waitForTimeout(500);
    await page.locator(`text="${uniqueName}"`).click();
    await page.waitForTimeout(1000);

    // Clear location field
    const locationField = page.getByTestId('venue-location-input');
    await locationField.clear();

    // Verify it's empty
    await expect(locationField).toHaveValue('');

    // Submit update
    await page.locator('button:has-text("Update Venue")').click();
    await page.waitForTimeout(1000);

    // Verify success
    const successNotification = page.locator('[role="alert"]:has-text("Venue updated successfully")');
    await expect(successNotification).toBeVisible({ timeout: 5000 });

    // Reload and verify location is still empty
    await page.goto('http://localhost:5173/admin/settings');
    await page.waitForLoadState('networkidle');

    await venueDropdown.click();
    await page.waitForTimeout(500);
    await page.locator(`text="${uniqueName}"`).click();
    await page.waitForTimeout(1000);

    await expect(locationField).toHaveValue('');

    console.log('✅ Venue Location field cleared successfully (set to null)');
  });

  test('should enforce 100 character max length on Location field', async ({ page }) => {
    // Login as admin
    await AuthHelpers.loginAs(page, 'admin');

    // Navigate to admin settings
    await page.goto('http://localhost:5173/admin/settings');
    await page.waitForLoadState('networkidle');

    // Open dropdown and select "Add New"
    const venueDropdown = page.getByPlaceholder('Select a venue');
    await venueDropdown.click();
    await page.waitForTimeout(500);

    await page.locator('text="Add New"').click();
    await page.waitForTimeout(500);

    // Try to enter more than 100 characters
    const locationField = page.getByTestId('venue-location-input');
    const longLocation = 'A'.repeat(150); // 150 characters

    await locationField.fill(longLocation);

    // Verify value is truncated to 100 characters
    const actualValue = await locationField.inputValue();
    expect(actualValue.length).toBeLessThanOrEqual(100);

    console.log(`✅ Location field enforces max length (${actualValue.length} chars max)`);
  });

  test('should display character counter or validation for Location field', async ({ page }) => {
    // Login as admin
    await AuthHelpers.loginAs(page, 'admin');

    // Navigate to admin settings
    await page.goto('http://localhost:5173/admin/settings');
    await page.waitForLoadState('networkidle');

    // Open dropdown and select "Add New"
    const venueDropdown = page.getByPlaceholder('Select a venue');
    await venueDropdown.click();
    await page.waitForTimeout(500);

    await page.locator('text="Add New"').click();
    await page.waitForTimeout(500);

    // Fill location field with text approaching limit
    const locationField = page.getByTestId('venue-location-input');
    const testLocation = 'A'.repeat(95); // 95 characters

    await locationField.fill(testLocation);

    // Verify max length attribute exists
    const maxLength = await locationField.getAttribute('maxLength');
    expect(maxLength).toBe('100');

    console.log('✅ Location field has maxLength=100 attribute');
  });
});
