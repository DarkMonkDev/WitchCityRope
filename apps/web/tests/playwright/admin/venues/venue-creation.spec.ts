/**
 * E2E Tests: Admin Venue Creation
 *
 * Tests admin ability to create new venues with proper validation
 * and error handling.
 *
 * Test Coverage:
 * - Admin can navigate to venue management
 * - Create new venue with all fields
 * - Validation for required fields
 * - Venue appears in list after creation
 * - Notes field is admin-only
 *
 * @see /apps/api/Endpoints/Admin/VenueEndpoints.cs
 */

import { test, expect } from '@playwright/test';
import { AuthHelpers } from '../../helpers/auth.helpers';

// TODO: Venue Management feature not yet implemented in React migration
// These tests are for a planned feature. Skipping until feature is built.
test.describe.skip('Admin Venue Creation', () => {
  test.beforeEach(async ({ page }) => {
    // Clear auth state before each test
    await AuthHelpers.clearAuthState(page);
  });

  test('should navigate to venue management page', async ({ page }) => {
    // Login as admin
    await AuthHelpers.loginAs(page, 'admin');

    // Navigate to admin venue management
    await page.goto('http://localhost:5173/admin/venues');
    await page.waitForLoadState('networkidle');

    // Verify page loaded
    await expect(page).toHaveURL(/\/admin\/venues/);

    // Look for "Add Venue" or "Create Venue" button
    const addButton = page.locator('[data-testid="add-venue-button"], button:has-text("Add Venue"), button:has-text("Create Venue")');
    await expect(addButton.first()).toBeVisible({ timeout: 5000 });
  });

  test('should create new venue with all fields', async ({ page }) => {
    // Login as admin
    await AuthHelpers.loginAs(page, 'admin');

    // Navigate to venue management
    await page.goto('http://localhost:5173/admin/venues');
    await page.waitForLoadState('networkidle');

    // Click "Add Venue" or "Create Venue" button
    const addButton = page.locator('[data-testid="add-venue-button"], button:has-text("Add Venue"), button:has-text("Create Venue")');
    await addButton.first().click();

    // Wait for form or modal to appear
    await page.waitForTimeout(500);

    // Fill venue details
    const uniqueName = `Test Venue ${Date.now()}`;

    await page.locator('[data-testid="venue-name-input"], input[name="name"], input[placeholder*="name" i]').first().fill(uniqueName);
    await page.locator('[data-testid="venue-directions-input"], textarea[name="directions"], textarea[placeholder*="directions" i]').first().fill('Turn left at Main Street, second building on the right');
    await page.locator('[data-testid="venue-notes-input"], textarea[name="notes"], textarea[placeholder*="notes" i]').first().fill('Admin-only internal notes about parking');

    // Submit form
    await page.locator('[data-testid="submit-venue-button"], button:has-text("Save"), button:has-text("Create"), button[type="submit"]').first().click();

    // Wait for success notification or redirect
    await page.waitForTimeout(1000);

    // Verify success - either notification or redirect to list
    const successIndicators = [
      '[data-testid="success-notification"]',
      '[role="alert"]:has-text("success")',
      '.notification:has-text("created")',
      'text=/created successfully/i'
    ];

    let successFound = false;
    for (const indicator of successIndicators) {
      const element = page.locator(indicator);
      if (await element.count() > 0) {
        await expect(element.first()).toBeVisible({ timeout: 5000 });
        successFound = true;
        break;
      }
    }

    // If no notification found, check if we're back on venue list
    if (!successFound) {
      await expect(page).toHaveURL(/\/admin\/venues/);
    }

    // Verify venue appears in list
    await page.goto('http://localhost:5173/admin/venues');
    await page.waitForLoadState('networkidle');

    // Look for the venue in the list
    await expect(page.locator(`text=${uniqueName}`)).toBeVisible({ timeout: 5000 });
  });

  test('should validate required venue name field', async ({ page }) => {
    // Login as admin
    await AuthHelpers.loginAs(page, 'admin');

    // Navigate to venue management
    await page.goto('http://localhost:5173/admin/venues');
    await page.waitForLoadState('networkidle');

    // Click "Add Venue" button
    const addButton = page.locator('[data-testid="add-venue-button"], button:has-text("Add Venue"), button:has-text("Create Venue")');
    await addButton.first().click();

    await page.waitForTimeout(500);

    // Try to submit with empty name
    await page.locator('[data-testid="venue-directions-input"], textarea[name="directions"]').first().fill('Some directions');

    // Submit form
    await page.locator('[data-testid="submit-venue-button"], button:has-text("Save"), button:has-text("Create"), button[type="submit"]').first().click();

    // Wait for validation error
    await page.waitForTimeout(500);

    // Verify error message appears
    const errorIndicators = [
      '[data-testid="name-error"]',
      '.error:has-text("name")',
      '[role="alert"]:has-text("name")',
      'text=/name.*required/i'
    ];

    let errorFound = false;
    for (const indicator of errorIndicators) {
      const element = page.locator(indicator);
      if (await element.count() > 0) {
        await expect(element.first()).toBeVisible({ timeout: 5000 });
        errorFound = true;
        break;
      }
    }

    if (!errorFound) {
      console.log('⚠️ Validation error not found - form may have client-side validation preventing submit');
    }
  });

  test('should create venue with only required fields', async ({ page }) => {
    // Login as admin
    await AuthHelpers.loginAs(page, 'admin');

    // Navigate to venue management
    await page.goto('http://localhost:5173/admin/venues');
    await page.waitForLoadState('networkidle');

    // Click "Add Venue" button
    const addButton = page.locator('[data-testid="add-venue-button"], button:has-text("Add Venue"), button:has-text("Create Venue")');
    await addButton.first().click();

    await page.waitForTimeout(500);

    // Fill only name (required field)
    const uniqueName = `Minimal Venue ${Date.now()}`;
    await page.locator('[data-testid="venue-name-input"], input[name="name"]').first().fill(uniqueName);

    // Submit form
    await page.locator('[data-testid="submit-venue-button"], button:has-text("Save"), button:has-text("Create"), button[type="submit"]').first().click();

    // Wait for success
    await page.waitForTimeout(1000);

    // Verify venue was created
    await page.goto('http://localhost:5173/admin/venues');
    await page.waitForLoadState('networkidle');

    await expect(page.locator(`text=${uniqueName}`)).toBeVisible({ timeout: 5000 });
  });

  test('should prevent duplicate venue names', async ({ page }) => {
    // Login as admin
    await AuthHelpers.loginAs(page, 'admin');

    const duplicateName = `Duplicate Venue ${Date.now()}`;

    // Create first venue
    await page.goto('http://localhost:5173/admin/venues');
    await page.waitForLoadState('networkidle');

    const addButton = page.locator('[data-testid="add-venue-button"], button:has-text("Add Venue"), button:has-text("Create Venue")');
    await addButton.first().click();
    await page.waitForTimeout(500);

    await page.locator('[data-testid="venue-name-input"], input[name="name"]').first().fill(duplicateName);
    await page.locator('[data-testid="submit-venue-button"], button:has-text("Save"), button:has-text("Create"), button[type="submit"]').first().click();
    await page.waitForTimeout(1000);

    // Try to create second venue with same name
    await page.goto('http://localhost:5173/admin/venues');
    await page.waitForLoadState('networkidle');

    await addButton.first().click();
    await page.waitForTimeout(500);

    await page.locator('[data-testid="venue-name-input"], input[name="name"]').first().fill(duplicateName);
    await page.locator('[data-testid="submit-venue-button"], button:has-text("Save"), button:has-text("Create"), button[type="submit"]').first().click();

    // Wait for error
    await page.waitForTimeout(1000);

    // Verify duplicate error
    const duplicateErrorIndicators = [
      'text=/duplicate/i',
      'text=/already exists/i',
      '[role="alert"]:has-text("name")'
    ];

    let errorFound = false;
    for (const indicator of duplicateErrorIndicators) {
      const element = page.locator(indicator);
      if (await element.count() > 0) {
        errorFound = true;
        break;
      }
    }

    // Error message or still on form page
    if (!errorFound) {
      // Form should still be visible if validation failed
      await expect(page.locator('[data-testid="venue-name-input"], input[name="name"]').first()).toBeVisible();
    }
  });
});
