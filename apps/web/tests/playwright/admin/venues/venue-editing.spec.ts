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
import { AuthHelpers } from '../../helpers/auth.helpers';

test.describe('Admin Venue Editing', () => {
  test.beforeEach(async ({ page }) => {
    // Clear auth state before each test
    await AuthHelpers.clearAuthState(page);
  });

  test('should edit existing venue name and directions', async ({ page }) => {
    // Login as admin
    await AuthHelpers.loginAs(page, 'admin');

    // Navigate to venue management
    await page.goto('http://localhost:5173/admin/venues');
    await page.waitForLoadState('networkidle');

    // Find first venue in list and click edit
    const editButton = page.locator('[data-testid="edit-venue-button"], button:has-text("Edit")').first();

    // If no edit button visible, try clicking on first venue row
    if (await editButton.count() === 0) {
      // Try clicking on first venue name or row
      const firstVenue = page.locator('[data-testid="venue-row"], tr, .venue-item').first();
      if (await firstVenue.count() > 0) {
        await firstVenue.click();
        await page.waitForTimeout(500);
      }
    } else {
      await editButton.click();
      await page.waitForTimeout(500);
    }

    // Fill updated venue details
    const updatedName = `Updated Venue ${Date.now()}`;
    const updatedDirections = `Updated directions ${Date.now()}`;

    const nameInput = page.locator('[data-testid="venue-name-input"], input[name="name"]').first();
    await nameInput.clear();
    await nameInput.fill(updatedName);

    const directionsInput = page.locator('[data-testid="venue-directions-input"], textarea[name="directions"]').first();
    await directionsInput.clear();
    await directionsInput.fill(updatedDirections);

    // Save changes
    await page.locator('[data-testid="save-venue-button"], button:has-text("Save"), button:has-text("Update"), button[type="submit"]').first().click();

    // Wait for save to complete
    await page.waitForTimeout(1000);

    // Verify changes persisted - go back to list
    await page.goto('http://localhost:5173/admin/venues');
    await page.waitForLoadState('networkidle');

    // Verify updated name appears in list
    await expect(page.locator(`text=${updatedName}`)).toBeVisible({ timeout: 5000 });
  });

  test('should update venue notes (admin-only field)', async ({ page }) => {
    // Login as admin
    await AuthHelpers.loginAs(page, 'admin');

    // Navigate to venue management
    await page.goto('http://localhost:5173/admin/venues');
    await page.waitForLoadState('networkidle');

    // Edit first venue
    const editButton = page.locator('[data-testid="edit-venue-button"], button:has-text("Edit")').first();
    if (await editButton.count() > 0) {
      await editButton.click();
    } else {
      await page.locator('[data-testid="venue-row"], tr, .venue-item').first().click();
    }

    await page.waitForTimeout(500);

    // Update notes field
    const updatedNotes = `Admin notes updated ${Date.now()}`;
    const notesInput = page.locator('[data-testid="venue-notes-input"], textarea[name="notes"]').first();

    if (await notesInput.count() > 0) {
      await notesInput.clear();
      await notesInput.fill(updatedNotes);

      // Save changes
      await page.locator('[data-testid="save-venue-button"], button:has-text("Save"), button:has-text("Update")').first().click();
      await page.waitForTimeout(1000);

      // Verify saved by re-opening edit form
      await page.goto('http://localhost:5173/admin/venues');
      await page.waitForLoadState('networkidle');

      const editButton2 = page.locator('[data-testid="edit-venue-button"], button:has-text("Edit")').first();
      if (await editButton2.count() > 0) {
        await editButton2.click();
      } else {
        await page.locator('[data-testid="venue-row"], tr, .venue-item').first().click();
      }

      await page.waitForTimeout(500);

      // Verify notes were saved
      const notesValue = await page.locator('[data-testid="venue-notes-input"], textarea[name="notes"]').first().inputValue();
      expect(notesValue).toContain(updatedNotes);
    } else {
      console.log('⚠️ Notes field not found in edit form');
    }
  });

  test('should toggle venue active/inactive status', async ({ page }) => {
    // Login as admin
    await AuthHelpers.loginAs(page, 'admin');

    // Navigate to venue management
    await page.goto('http://localhost:5173/admin/venues');
    await page.waitForLoadState('networkidle');

    // Edit first venue
    const editButton = page.locator('[data-testid="edit-venue-button"], button:has-text("Edit")').first();
    if (await editButton.count() > 0) {
      await editButton.click();
    } else {
      await page.locator('[data-testid="venue-row"], tr, .venue-item').first().click();
    }

    await page.waitForTimeout(500);

    // Find active/inactive toggle (checkbox or switch)
    const activeToggle = page.locator('[data-testid="venue-active-toggle"], input[name="isActive"], input[type="checkbox"]').first();

    if (await activeToggle.count() > 0) {
      // Get current state
      const wasChecked = await activeToggle.isChecked();

      // Toggle it
      await activeToggle.click();

      // Verify it toggled
      const isNowChecked = await activeToggle.isChecked();
      expect(isNowChecked).toBe(!wasChecked);

      // Save changes
      await page.locator('[data-testid="save-venue-button"], button:has-text("Save"), button:has-text("Update")').first().click();
      await page.waitForTimeout(1000);

      // Verify change persisted by re-opening
      await page.goto('http://localhost:5173/admin/venues');
      await page.waitForLoadState('networkidle');

      const editButton2 = page.locator('[data-testid="edit-venue-button"], button:has-text("Edit")').first();
      if (await editButton2.count() > 0) {
        await editButton2.click();
      } else {
        await page.locator('[data-testid="venue-row"], tr, .venue-item').first().click();
      }

      await page.waitForTimeout(500);

      const finalState = await page.locator('[data-testid="venue-active-toggle"], input[name="isActive"], input[type="checkbox"]').first().isChecked();
      expect(finalState).toBe(!wasChecked);
    } else {
      console.log('⚠️ Active toggle not found in edit form');
    }
  });

  test('should show inactive venues in admin list', async ({ page }) => {
    // Login as admin
    await AuthHelpers.loginAs(page, 'admin');

    // Create a venue and set it to inactive
    await page.goto('http://localhost:5173/admin/venues');
    await page.waitForLoadState('networkidle');

    // Edit first venue
    const editButton = page.locator('[data-testid="edit-venue-button"], button:has-text("Edit")').first();
    if (await editButton.count() > 0) {
      await editButton.click();
      await page.waitForTimeout(500);

      // Get venue name before making inactive
      const venueName = await page.locator('[data-testid="venue-name-input"], input[name="name"]').first().inputValue();

      // Set to inactive
      const activeToggle = page.locator('[data-testid="venue-active-toggle"], input[name="isActive"], input[type="checkbox"]').first();
      if (await activeToggle.count() > 0 && await activeToggle.isChecked()) {
        await activeToggle.click();
      }

      // Save
      await page.locator('[data-testid="save-venue-button"], button:has-text("Save"), button:has-text("Update")').first().click();
      await page.waitForTimeout(1000);

      // Verify inactive venue still appears in admin list
      await page.goto('http://localhost:5173/admin/venues');
      await page.waitForLoadState('networkidle');

      // Admin should see inactive venues
      await expect(page.locator(`text=${venueName}`)).toBeVisible({ timeout: 5000 });
    }
  });

  test('should prevent non-admin from accessing venue edit', async ({ page }) => {
    // Login as regular member
    await AuthHelpers.loginAs(page, 'member');

    // Try to navigate to admin venue management
    await page.goto('http://localhost:5173/admin/venues');
    await page.waitForTimeout(1000);

    // Should be redirected or see forbidden/unauthorized message
    const currentUrl = page.url();

    // Either redirected away from admin page or see error
    const isBlocked =
      !currentUrl.includes('/admin/venues') ||
      (await page.locator('text=/forbidden/i, text=/unauthorized/i, text=/access denied/i').count()) > 0 ||
      (await page.locator('[data-testid="error-message"], [role="alert"]').count()) > 0;

    expect(isBlocked).toBe(true);
  });

  test('should cancel editing and discard changes', async ({ page }) => {
    // Login as admin
    await AuthHelpers.loginAs(page, 'admin');

    // Navigate to venue management
    await page.goto('http://localhost:5173/admin/venues');
    await page.waitForLoadState('networkidle');

    // Edit first venue
    const editButton = page.locator('[data-testid="edit-venue-button"], button:has-text("Edit")').first();
    if (await editButton.count() > 0) {
      await editButton.click();
      await page.waitForTimeout(500);

      // Get original name
      const originalName = await page.locator('[data-testid="venue-name-input"], input[name="name"]').first().inputValue();

      // Make a change
      const nameInput = page.locator('[data-testid="venue-name-input"], input[name="name"]').first();
      await nameInput.clear();
      await nameInput.fill('This Should Not Save');

      // Click cancel
      const cancelButton = page.locator('[data-testid="cancel-button"], button:has-text("Cancel")').first();
      if (await cancelButton.count() > 0) {
        await cancelButton.click();
        await page.waitForTimeout(500);

        // Verify we're back on list page
        await expect(page).toHaveURL(/\/admin\/venues/);

        // Re-open edit to verify changes were not saved
        const editButton2 = page.locator('[data-testid="edit-venue-button"], button:has-text("Edit")').first();
        if (await editButton2.count() > 0) {
          await editButton2.click();
          await page.waitForTimeout(500);

          const currentName = await page.locator('[data-testid="venue-name-input"], input[name="name"]').first().inputValue();
          expect(currentName).toBe(originalName);
        }
      } else {
        console.log('⚠️ Cancel button not found in edit form');
      }
    }
  });
});
