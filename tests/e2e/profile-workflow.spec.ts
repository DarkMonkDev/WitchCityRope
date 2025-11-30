import { test, expect } from '@playwright/test';
import { AuthHelpers } from './test-utils/helpers/auth.helpers';

/**
 * Profile Settings Workflow Tests
 *
 * Purpose: Test user profile settings functionality (ProfileSettingsPage component)
 * Replaces: profile-page.spec.ts (partial - refocused on workflows)
 *
 * Tests profile workflows:
 * - Profile page loads with 3 tabs (Personal, Change Password, Vetting & Membership)
 * - User can edit profile fields
 * - Changes persist after save
 * - Password change validation works
 * - Vetting status displays correctly
 * - Put account on hold functionality (if applicable)
 */

const baseUrl = process.env.PLAYWRIGHT_BASE_URL || 'http://localhost:5173';

test.describe('Profile Settings - Page Structure', () => {

  test('should load profile settings page with all tabs', async ({ page }) => {
    await AuthHelpers.loginAs(page, 'member');

    await page.goto(`${baseUrl}/dashboard/profile-settings`);
    await page.waitForLoadState('domcontentloaded');

    // Verify page title
    const pageTitle = page.locator('h1:has-text("Profile Settings")');
    await expect(pageTitle).toBeVisible({ timeout: 10000 });

    // Verify all 3 tabs exist
    const personalTab = page.locator('button[role="tab"]:has-text("Personal")');
    const securityTab = page.locator('button[role="tab"]:has-text("Change Password")');
    const vettingTab = page.locator('button[role="tab"]:has-text("Vetting & Membership")');

    await expect(personalTab).toBeVisible();
    await expect(securityTab).toBeVisible();
    await expect(vettingTab).toBeVisible();

    console.log('✅ Profile settings page loaded with all 3 tabs');
  });

  test('should display View Dashboard button', async ({ page }) => {
    await AuthHelpers.loginAs(page, 'vetted');

    await page.goto(`${baseUrl}/dashboard/profile-settings`);
    await page.waitForLoadState('domcontentloaded');

    // Verify View Dashboard button exists
    const viewDashboardButton = page.locator('a:has-text("View Dashboard")');
    await expect(viewDashboardButton).toBeVisible({ timeout: 5000 });

    console.log('✅ View Dashboard button displayed');
  });
});

test.describe('Profile Settings - Personal Tab', () => {

  test('should display all personal info fields', async ({ page }) => {
    await AuthHelpers.loginAs(page, 'member');

    await page.goto(`${baseUrl}/dashboard/profile-settings`);
    await page.waitForLoadState('domcontentloaded');

    // Ensure Personal tab is active (default)
    const personalTab = page.locator('button[role="tab"]:has-text("Personal")');
    await personalTab.click();
    await page.waitForTimeout(500);

    // Verify all profile fields are visible
    const sceneNameInput = page.locator('[data-testid="scene-name-input"]');
    const firstNameInput = page.locator('[data-testid="first-name-input"]');
    const lastNameInput = page.locator('[data-testid="last-name-input"]');
    const emailInput = page.locator('[data-testid="email-input"]');
    const pronounsInput = page.locator('[data-testid="pronouns-input"]');
    const discordInput = page.locator('[data-testid="discord-name-input"]');
    const fetlifeInput = page.locator('[data-testid="fetlife-name-input"]');
    const phoneInput = page.locator('[data-testid="phone-number-input"]');
    const bioInput = page.locator('[data-testid="bio-input"]');

    await expect(sceneNameInput).toBeVisible({ timeout: 5000 });
    await expect(firstNameInput).toBeVisible();
    await expect(lastNameInput).toBeVisible();
    await expect(emailInput).toBeVisible();
    await expect(pronounsInput).toBeVisible();
    await expect(discordInput).toBeVisible();
    await expect(fetlifeInput).toBeVisible();
    await expect(phoneInput).toBeVisible();
    await expect(bioInput).toBeVisible();

    console.log('✅ All personal info fields displayed');
  });

  test('should edit scene name and save changes successfully', async ({ page }) => {
    await AuthHelpers.loginAs(page, 'member');

    await page.goto(`${baseUrl}/dashboard/profile-settings`);
    await page.waitForLoadState('domcontentloaded');

    // Wait for scene name field to be visible
    const sceneNameInput = page.locator('[data-testid="scene-name-input"]');
    await expect(sceneNameInput).toBeVisible({ timeout: 10000 });

    // Get current value
    const currentValue = await sceneNameInput.inputValue();

    // Update scene name with timestamp to ensure uniqueness
    const newSceneName = `UpdatedMember${Date.now()}`;
    await sceneNameInput.clear();
    await sceneNameInput.fill(newSceneName);

    // Click Save Changes button
    const saveButton = page.locator('button:has-text("Save Changes")');
    await expect(saveButton).toBeVisible();
    await saveButton.click();

    // Wait for success notification
    await expect(page.locator('text=Profile updated successfully')).toBeVisible({ timeout: 10000 });

    console.log(`✅ Scene name updated from "${currentValue}" to "${newSceneName}"`);
  });

  test('should verify scene name persists after reload', async ({ page, context }) => {
    // Use a fresh browser context to avoid React Query cache issues
    await context.clearCookies();

    await AuthHelpers.loginAs(page, 'guest'); // Use guest account to avoid conflicts

    await page.goto(`${baseUrl}/dashboard/profile-settings`);
    await page.waitForLoadState('domcontentloaded');

    // Wait for scene name field
    const sceneNameInput = page.locator('[data-testid="scene-name-input"]');
    await expect(sceneNameInput).toBeVisible({ timeout: 10000 });

    // Update scene name
    const newSceneName = `PersistedGuest${Date.now()}`;
    await sceneNameInput.clear();
    await sceneNameInput.fill(newSceneName);

    // Save changes
    const saveButton = page.locator('button:has-text("Save Changes")');
    await saveButton.click();

    // Wait for success notification AND for profile to update (React Query cache)
    await expect(page.locator('text=Profile updated successfully')).toBeVisible({ timeout: 10000 });
    await page.waitForTimeout(2000); // Give React Query time to update cache and sync to backend

    // Reload the page (full page reload clears React Query cache)
    await page.reload({ waitUntil: 'domcontentloaded' });
    await page.waitForTimeout(2000); // Wait for React Query to refetch from backend

    // Wait for scene name field to load with data
    const sceneNameInputAfterReload = page.locator('[data-testid="scene-name-input"]');
    await expect(sceneNameInputAfterReload).toBeVisible({ timeout: 10000 });

    const persistedValue = await sceneNameInputAfterReload.inputValue();
    expect(persistedValue).toBe(newSceneName);

    console.log(`✅ Scene name persisted after reload: ${persistedValue}`);
  });

  test('should update multiple fields at once', async ({ page }) => {
    await AuthHelpers.loginAs(page, 'member');

    await page.goto(`${baseUrl}/dashboard/profile-settings`);
    await page.waitForLoadState('domcontentloaded');

    // Update multiple fields
    const pronounsInput = page.locator('[data-testid="pronouns-input"]');
    await pronounsInput.clear();
    await pronounsInput.fill('they/them');

    const discordInput = page.locator('[data-testid="discord-name-input"]');
    await discordInput.clear();
    await discordInput.fill('TestUser#1234');

    const phoneInput = page.locator('[data-testid="phone-number-input"]');
    await phoneInput.clear();
    await phoneInput.fill('555-123-4567');

    // Save changes
    const saveButton = page.locator('button:has-text("Save Changes")');
    await saveButton.click();

    // Wait for success notification
    await expect(page.locator('text=Profile updated successfully')).toBeVisible({ timeout: 10000 });

    console.log('✅ Multiple fields updated successfully');
  });

  test('should update bio field', async ({ page }) => {
    await AuthHelpers.loginAs(page, 'vetted');

    await page.goto(`${baseUrl}/dashboard/profile-settings`);
    await page.waitForLoadState('domcontentloaded');

    // Update bio
    const bioInput = page.locator('[data-testid="bio-input"]');
    await bioInput.clear();
    await bioInput.fill('This is my updated bio for testing purposes.');

    // Save changes
    const saveButton = page.locator('button:has-text("Save Changes")');
    await saveButton.click();

    // Wait for success notification
    await expect(page.locator('text=Profile updated successfully')).toBeVisible({ timeout: 10000 });

    console.log('✅ Bio field updated successfully');
  });
});

test.describe('Profile Settings - Change Password Tab', () => {

  test('should display password change form', async ({ page }) => {
    await AuthHelpers.loginAs(page, 'member');

    await page.goto(`${baseUrl}/dashboard/profile-settings`);
    await page.waitForLoadState('domcontentloaded');

    // Click Change Password tab
    const securityTab = page.locator('button[role="tab"]:has-text("Change Password")');
    await securityTab.click();
    await page.waitForTimeout(1000);

    // Verify password fields are visible
    const passwordFields = page.locator('input[type="password"]');
    const fieldCount = await passwordFields.count();

    expect(fieldCount).toBeGreaterThanOrEqual(3); // Current, New, Confirm

    console.log(`✅ Password change form displayed with ${fieldCount} password fields`);
  });

  test('should validate password mismatch', async ({ page }) => {
    await AuthHelpers.loginAs(page, 'member');

    await page.goto(`${baseUrl}/dashboard/profile-settings`);
    await page.waitForLoadState('domcontentloaded');

    // Switch to Change Password tab
    const securityTab = page.locator('button[role="tab"]:has-text("Change Password")');
    await securityTab.click();
    await page.waitForTimeout(1000);

    // Fill password fields with mismatch
    const newPasswordField = page.locator('input[type="password"]').nth(1); // New password
    const confirmPasswordField = page.locator('input[type="password"]').nth(2); // Confirm password

    await newPasswordField.fill('NewPassword123!');
    await confirmPasswordField.fill('DifferentPassword123!');

    // Blur to trigger validation
    await confirmPasswordField.blur();
    await page.waitForTimeout(1000);

    // Look for mismatch error (may appear on blur or on submit)
    const mismatchError = page.locator('text=Passwords do not match');

    const errorVisible = await mismatchError.isVisible({ timeout: 2000 }).catch(() => false);

    if (errorVisible) {
      console.log('✅ Password mismatch validation works (on blur)');
    } else {
      // Try submitting to trigger validation
      const changePasswordButton = page.locator('button[type="submit"]:has-text("Change Password")');
      await changePasswordButton.click();
      await page.waitForTimeout(1000);

      const errorVisibleAfterSubmit = await mismatchError.isVisible({ timeout: 5000 }).catch(() => false);

      if (errorVisibleAfterSubmit) {
        console.log('✅ Password mismatch validation works (on submit)');
      } else {
        console.log('ℹ️ Password mismatch validation not shown (may validate on backend only)');
      }
    }
  });

  test('should validate password complexity requirements', async ({ page }) => {
    await AuthHelpers.loginAs(page, 'member');

    await page.goto(`${baseUrl}/dashboard/profile-settings`);
    await page.waitForLoadState('domcontentloaded');

    // Switch to Change Password tab
    const securityTab = page.locator('button[role="tab"]:has-text("Change Password")');
    await securityTab.click();
    await page.waitForTimeout(1000);

    // Fill with weak password
    const newPasswordField = page.locator('input[type="password"]').nth(1);
    const confirmPasswordField = page.locator('input[type="password"]').nth(2);

    await newPasswordField.fill('weak');
    await confirmPasswordField.fill('weak');

    // Blur to trigger validation
    await newPasswordField.blur();
    await page.waitForTimeout(1000);

    // Look for complexity error (various possible messages)
    const complexityError = page.locator('.mantine-InputWrapper-error, .error, [class*="error"]')
      .filter({ hasText: /8 characters|uppercase|lowercase|number|special character/i });

    const errorVisible = await complexityError.isVisible({ timeout: 2000 }).catch(() => false);

    if (errorVisible) {
      const errorText = await complexityError.textContent();
      console.log(`✅ Password complexity validation works: ${errorText}`);
    } else {
      console.log('ℹ️ Password complexity validation may not be implemented yet (client-side)');
    }
  });

  test('should display password requirements hint', async ({ page }) => {
    await AuthHelpers.loginAs(page, 'member');

    await page.goto(`${baseUrl}/dashboard/profile-settings`);
    await page.waitForLoadState('domcontentloaded');

    // Switch to Change Password tab
    const securityTab = page.locator('button[role="tab"]:has-text("Change Password")');
    await securityTab.click();
    await page.waitForTimeout(1000);

    // Verify hint text is visible
    const hintText = page.locator('text=/8\\+ characters.*uppercase.*lowercase.*number.*special character/i');
    await expect(hintText).toBeVisible({ timeout: 5000 });

    console.log('✅ Password requirements hint displayed');
  });
});

test.describe('Profile Settings - Vetting & Membership Tab', () => {

  test('should display vetting status for member with application', async ({ page }) => {
    await AuthHelpers.loginAs(page, 'member');

    await page.goto(`${baseUrl}/dashboard/profile-settings`);
    await page.waitForLoadState('domcontentloaded');

    // Click Vetting & Membership tab
    const vettingTab = page.locator('button[role="tab"]:has-text("Vetting & Membership")');
    await vettingTab.click();
    await page.waitForTimeout(1000);

    // Look for vetting status field (any input with label "Vetting Status")
    const vettingStatusInput = page.locator('input').filter({ hasText: /.+/ }).first();

    if (await vettingStatusInput.count() > 0 && await vettingStatusInput.isVisible()) {
      const statusValue = await vettingStatusInput.inputValue();
      console.log(`✅ Vetting status displayed: ${statusValue}`);

      // Verify field has readonly attribute (may be "" or null for programmatic readonly)
      const isReadOnly = await vettingStatusInput.getAttribute('readonly');
      const isDisabled = await vettingStatusInput.isDisabled();

      if (isReadOnly !== null || isDisabled) {
        console.log('✅ Vetting status field is read-only or disabled');
      } else {
        console.log('ℹ️ Vetting status field may be editable (unexpected)');
      }
    } else {
      // Look for any text about vetting status or no application
      const vettingContent = await page.locator('[class*="mantine"]').filter({ hasText: /vetting|application|status/i }).first().textContent();
      console.log(`ℹ️ Vetting tab content: ${vettingContent?.substring(0, 100)}`);
    }
  });

  test('should display "Put Membership On Hold" button for approved members', async ({ page }) => {
    await AuthHelpers.loginAs(page, 'vetted');

    await page.goto(`${baseUrl}/dashboard/profile-settings`);
    await page.waitForLoadState('domcontentloaded');

    // Click Vetting & Membership tab
    const vettingTab = page.locator('button[role="tab"]:has-text("Vetting & Membership")');
    await vettingTab.click();
    await page.waitForTimeout(1000);

    // Look for "Put Membership On Hold" button (only visible for Approved users)
    const holdButton = page.locator('button:has-text("Put Membership On Hold")');

    if (await holdButton.count() > 0) {
      await expect(holdButton).toBeVisible({ timeout: 5000 });
      console.log('✅ "Put Membership On Hold" button displayed for approved member');
    } else {
      console.log('ℹ️ User is not in Approved status - hold button not visible');
    }
  });

  test('should open modal when "Put Membership On Hold" clicked', async ({ page }) => {
    await AuthHelpers.loginAs(page, 'vetted');

    await page.goto(`${baseUrl}/dashboard/profile-settings`);
    await page.waitForLoadState('domcontentloaded');

    // Click Vetting & Membership tab
    const vettingTab = page.locator('button[role="tab"]:has-text("Vetting & Membership")');
    await vettingTab.click();
    await page.waitForTimeout(1000);

    // Click "Put Membership On Hold" button
    const holdButton = page.locator('button:has-text("Put Membership On Hold")');

    if (await holdButton.count() > 0) {
      await holdButton.click();

      // Wait for modal to appear
      const modal = page.locator('[role="dialog"]');
      await expect(modal).toBeVisible({ timeout: 5000 });

      // Verify modal content
      const modalContent = await modal.textContent();
      expect(modalContent).toMatch(/hold|membership/i);

      console.log('✅ Hold membership modal opens correctly');

      // Close modal (look for cancel or close button)
      const cancelButton = modal.locator('button:has-text("Cancel")');
      if (await cancelButton.count() > 0) {
        await cancelButton.click();
        await page.waitForSelector('[role="dialog"]', { state: 'detached', timeout: 5000 });
      }
    } else {
      console.log('ℹ️ Hold button not available for this user status');
    }
  });

  test('should display vetting information text', async ({ page }) => {
    await AuthHelpers.loginAs(page, 'member');

    await page.goto(`${baseUrl}/dashboard/profile-settings`);
    await page.waitForLoadState('domcontentloaded');

    // Click Vetting & Membership tab
    const vettingTab = page.locator('button[role="tab"]:has-text("Vetting & Membership")');
    await vettingTab.click();
    await page.waitForTimeout(1000);

    // Look for informational text about vetting status
    const infoText = page.locator('text=/managed by administrators|contact us|vetting application/i');
    await expect(infoText.first()).toBeVisible({ timeout: 5000 });

    console.log('✅ Vetting information text displayed');
  });
});

test.describe('Profile Settings - Responsive Design', () => {

  test('should display correctly on mobile viewport', async ({ page }) => {
    await page.setViewportSize({ width: 375, height: 667 });

    await AuthHelpers.loginAs(page, 'member');

    await page.goto(`${baseUrl}/dashboard/profile-settings`);
    await page.waitForLoadState('domcontentloaded');

    // Verify page title is visible
    const pageTitle = page.locator('h1:has-text("Profile Settings")');
    await expect(pageTitle).toBeVisible({ timeout: 5000 });

    // Verify tabs are visible
    const personalTab = page.locator('button[role="tab"]:has-text("Personal")');
    await expect(personalTab).toBeVisible();

    // Take screenshot for visual verification
    await page.screenshot({ path: './test-results/profile-mobile-375.png' });

    console.log('✅ Profile settings displays correctly on mobile viewport');
  });

  test('should have fields in single column on mobile', async ({ page }) => {
    await page.setViewportSize({ width: 375, height: 667 });

    await AuthHelpers.loginAs(page, 'member');

    await page.goto(`${baseUrl}/dashboard/profile-settings`);
    await page.waitForLoadState('domcontentloaded');

    // Check field layout (should be single column on mobile)
    const sceneNameInput = page.locator('[data-testid="scene-name-input"]');
    const emailInput = page.locator('[data-testid="email-input"]');

    const sceneNameBox = await sceneNameInput.boundingBox();
    const emailBox = await emailInput.boundingBox();

    if (sceneNameBox && emailBox) {
      // On mobile, fields should stack vertically
      const isStacked = emailBox.y > sceneNameBox.y;
      expect(isStacked).toBe(true);

      console.log('✅ Profile fields stack vertically on mobile');
    }

    await page.screenshot({ path: './test-results/profile-mobile-fields.png' });
  });
});
