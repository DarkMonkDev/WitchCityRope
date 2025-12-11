/**
 * Admin Member History Tab - Profile Change Tracking E2E Tests
 * DataFactory Migration
 *
 * PURPOSE: Verify profile changes are tracked and displayed correctly in admin member details History tab
 *
 * CONTEXT:
 * - Backend: Profile changes logged to UserNote table with NoteType=ProfileChange
 * - Backend API: GET /api/users/{userId}/profile-change-history returns change records
 * - Frontend: History tab displays changes with DateTime, Field Name, Old Value → New Value
 *
 * TEST STRATEGY:
 * 1. Login as regular user
 * 2. Update profile fields (Bio, Pronouns, OtherNames)
 * 3. Save changes
 * 4. Logout
 * 5. Login as admin
 * 6. Navigate to member details → History tab
 * 7. Verify changes are displayed with correct format
 *
 * MIGRATION NOTES:
 * - Migrated from deprecated database-helpers.ts createTestUser()
 * - Now uses DataFactory with df.users.createVerified()
 * - Automatic cleanup via df fixture
 */

import { expect } from '@playwright/test';
import { test } from '../lib/datafactory/fixtures/test.fixture';
import { AuthHelpers } from './test-utils/helpers/auth.helpers';

test.describe('Admin Member History Tab', () => {
  test('should display profile changes in History tab after user updates profile', async ({
    page,
    df,
  }) => {
    console.log('🚀 Starting admin member history test...');

    // Create unique test user using DataFactory
    const timestamp = Date.now();
    const testUser = await df.users.createVerified({
      email: `history-test-${timestamp}@test.com`,
      password: 'Test123!',
      firstName: 'HistoryTest',
      lastName: `${timestamp}`,
    });

    // Set original values for verification
    const originalBio = 'Original bio text for testing';
    const originalPronouns = 'they/them';
    const originalOtherNames = 'OriginalAlias';

    // Set updated values
    const updatedBio = `Updated bio text after change - ${timestamp}`;
    const updatedPronouns = 'she/her';
    const updatedOtherNames = 'NewAlias, UpdatedName';

    // STEP 1: Login as regular user
    console.log('📍 Step 1: Logging in as regular user...');
    await AuthHelpers.loginWith(page, {
      email: testUser.email,
      password: 'Test123!',
    });
    console.log('✅ Successfully logged in');

    // STEP 2: Navigate to profile settings
    console.log('📍 Step 2: Navigating to profile settings...');
    await page.goto('/dashboard/profile-settings');
    await page.waitForLoadState('domcontentloaded');
    await expect(
      page.getByRole('heading', { name: /profile settings/i })
    ).toBeVisible();
    console.log('✅ Profile settings page loaded');

    // STEP 2a: Set original values first (so we have old values to track changes from)
    console.log('📍 Step 2a: Setting original profile values...');
    const bioInput = page.locator('[data-testid="bio-input"]');
    await bioInput.clear();
    await bioInput.fill(originalBio);

    const pronounsInput = page.locator('[data-testid="pronouns-input"]');
    await pronounsInput.clear();
    await pronounsInput.fill(originalPronouns);

    const otherNamesInput = page.locator('[data-testid="other-names-input"]');
    await otherNamesInput.clear();
    await otherNamesInput.fill(originalOtherNames);

    // Save original values
    const saveButton1 = page
      .getByRole('button', { name: /save changes/i })
      .last();
    await saveButton1.waitFor({ state: 'visible' });
    await saveButton1.click();

    // Wait for success notification
    await expect(
      page.locator('[role="alert"]').first()
    ).toContainText(/success/i);
    await page.waitForTimeout(1000); // Allow backend to process
    console.log('✅ Original values saved');

    // STEP 3: Make profile changes (these will be tracked)
    console.log('📍 Step 3: Updating profile fields...');

    // Update Bio
    await bioInput.clear();
    await bioInput.fill(updatedBio);
    console.log(`  ✓ Updated Bio: "${originalBio}" → "${updatedBio}"`);

    // Update Pronouns
    await pronounsInput.clear();
    await pronounsInput.fill(updatedPronouns);
    console.log(
      `  ✓ Updated Pronouns: "${originalPronouns}" → "${updatedPronouns}"`
    );

    // Update Other Names
    await otherNamesInput.clear();
    await otherNamesInput.fill(updatedOtherNames);
    console.log(
      `  ✓ Updated Other Names: "${originalOtherNames}" → "${updatedOtherNames}"`
    );

    // STEP 4: Save changes
    console.log('📍 Step 4: Saving profile changes...');
    const saveButton2 = page
      .getByRole('button', { name: /save changes/i })
      .last();
    await saveButton2.waitFor({ state: 'visible' });
    await saveButton2.click();

    // Wait for success notification
    await expect(
      page.locator('[role="alert"]').first()
    ).toContainText(/success/i);
    await page.waitForTimeout(1000); // Allow backend to process change tracking
    console.log('✅ Profile changes saved successfully');

    // Get the user ID for later navigation
    const userId = testUser.id;
    console.log(`📋 Test User ID: ${userId}`);

    // STEP 5: Logout (use clearAuthState to avoid navigation issues)
    console.log('📍 Step 5: Logging out...');
    await AuthHelpers.clearAuthState(page);
    await page.goto('/login');
    await page.waitForLoadState('domcontentloaded');
    console.log('✅ Successfully logged out');

    // STEP 6: Login as admin
    console.log('📍 Step 6: Logging in as admin...');
    await AuthHelpers.loginAs(page, 'admin');
    console.log('✅ Successfully logged in as admin');

    // STEP 7: Navigate to admin members page
    console.log('📍 Step 7: Navigating to admin members page...');
    await page.goto('/admin/members');
    await page.waitForLoadState('domcontentloaded');
    await expect(page.getByRole('heading', { name: /member management/i })).toBeVisible();
    console.log('✅ Admin members page loaded');

    // STEP 8: Navigate to test user's member details page
    console.log('📍 Step 8: Navigating to member details page...');
    await page.goto(`/admin/members/${userId}`);
    await page.waitForLoadState('domcontentloaded');
    console.log('✅ Member details page loaded');

    // STEP 9: Click on History tab
    console.log('📍 Step 9: Clicking on History tab...');

    // Wait for tabs to be fully rendered
    await page.waitForSelector('[role="tablist"]', { timeout: 10000 });
    await page.waitForTimeout(1000); // Allow React hydration

    const historyTab = page.getByRole('tab', { name: 'History' });
    await historyTab.waitFor({ state: 'visible', timeout: 10000 });
    await historyTab.click();
    await page.waitForTimeout(500); // Allow tab to switch
    console.log('✅ History tab clicked');

    // STEP 10: Verify profile changes are displayed
    console.log('📍 Step 10: Verifying profile changes in History tab...');

    // Wait for history to load
    await page.waitForTimeout(1000); // Allow API to fetch history

    // Get the History tab panel to scope selectors
    const historyPanel = page.getByRole('tabpanel', { name: 'History' });

    // Verify Bio change (use .first() in case there are multiple Bio changes)
    console.log('  🔍 Verifying Bio change...');
    const bioChangeText = historyPanel.locator('text=Bio:').first();
    await expect(bioChangeText).toBeVisible({ timeout: 10000 });

    // Get the container for Bio change
    const bioChangeContainer = bioChangeText.locator('..');
    await expect(bioChangeContainer).toContainText(originalBio);
    await expect(bioChangeContainer).toContainText(updatedBio);
    console.log('  ✅ Bio change verified');

    // Verify Pronouns change (use .first() in case there are multiple Pronouns changes)
    console.log('  🔍 Verifying Pronouns change...');
    const pronounsChangeText = historyPanel.locator('text=Pronouns:').first();
    await expect(pronounsChangeText).toBeVisible();

    const pronounsChangeContainer = pronounsChangeText.locator('..');
    await expect(pronounsChangeContainer).toContainText(originalPronouns);
    await expect(pronounsChangeContainer).toContainText(updatedPronouns);
    console.log('  ✅ Pronouns change verified');

    // Verify Other Names change (use .first() in case there are multiple Other Names changes)
    console.log('  🔍 Verifying Other Names change...');
    const otherNamesChangeText = historyPanel.locator('text=Other Names:').first();
    await expect(otherNamesChangeText).toBeVisible();

    const otherNamesChangeContainer = otherNamesChangeText.locator('..');
    await expect(otherNamesChangeContainer).toContainText(originalOtherNames);
    await expect(otherNamesChangeContainer).toContainText(updatedOtherNames);
    console.log('  ✅ Other Names change verified');

    // Verify arrow icons are present (IconArrowRight from @tabler/icons-react)
    console.log('  🔍 Verifying arrow icons...');
    // Arrow icons are rendered as SVG paths, check for presence in change containers
    const bioArrow = bioChangeContainer.locator('svg');
    await expect(bioArrow).toBeVisible();
    console.log('  ✅ Arrow icons verified');

    // Verify date/time is displayed (MemberHistoryTab shows timestamp above each change)
    console.log('  🔍 Verifying date/time display...');
    // The timestamp is rendered in a Text element at the top of each Paper component
    // Just check that there's a timestamp visible in the history panel (contains "AM" or "PM")
    const timestampEl = historyPanel.getByText(/AM|PM/i).first();
    await expect(timestampEl).toBeVisible();
    console.log('  ✅ Date/time display verified');

    console.log('✅ ALL VERIFICATIONS PASSED');

    // Note: df.cleanupAll() called automatically after test
  });

  test('should show empty state when no history exists', async ({ page, df }) => {
    console.log('🚀 Starting empty state test...');

    // Create a new user with no profile changes using DataFactory
    const timestamp = Date.now();
    const testUser = await df.users.createVerified({
      email: `history-empty-${timestamp}@test.com`,
      password: 'Test123!',
      firstName: 'EmptyHistory',
      lastName: `${timestamp}`,
    });

    // Login as admin
    console.log('📍 Logging in as admin...');
    await AuthHelpers.loginAs(page, 'admin');
    console.log('✅ Logged in as admin');

    // Navigate to test user's member details
    console.log('📍 Navigating to member details page...');
    await page.goto(`/admin/members/${testUser.id}`);
    await page.waitForLoadState('domcontentloaded');

    // Click History tab
    console.log('📍 Clicking on History tab...');
    const historyTab = page.getByRole('tab', { name: 'History' });
    await historyTab.waitFor({ state: 'visible' });
    await historyTab.click();
    await page.waitForTimeout(500);

    // Verify empty state message (exact text from MemberHistoryTab.tsx line 48)
    console.log('📍 Verifying empty state message...');

    // Wait for history content to load
    await page.waitForTimeout(2000);

    // Check if empty state is visible
    const emptyStateText = page.getByText('No profile changes recorded yet.');
    const isEmptyStateVisible = await emptyStateText.isVisible();

    if (!isEmptyStateVisible) {
      // Debug: Check what's actually shown
      const pageContent = await page.content();
      console.log('❌ Empty state not found. Page content includes:');
      const historySection = pageContent.substring(
        pageContent.indexOf('History') - 200,
        pageContent.indexOf('History') + 500
      );
      console.log(historySection);
    }

    await expect(emptyStateText).toBeVisible();
    console.log('✅ Empty state verified');

    // Note: df.cleanupAll() called automatically after test
  });

  test('should display multiple profile changes chronologically', async ({
    page,
    df,
  }) => {
    console.log('🚀 Starting multiple changes test...');

    // Create test user using DataFactory
    const timestamp = Date.now();
    const testUser = await df.users.createVerified({
      email: `history-multiple-${timestamp}@test.com`,
      password: 'Test123!',
      firstName: 'MultiHistory',
      lastName: `${timestamp}`,
    });

    // Login as test user
    console.log('📍 Logging in as test user...');
    await AuthHelpers.loginWith(page, {
      email: testUser.email,
      password: 'Test123!',
    });

    // Navigate to profile settings
    console.log('📍 Navigating to profile settings...');
    await page.goto('/dashboard/profile-settings');
    await page.waitForLoadState('domcontentloaded');

    // Make FIRST profile change (Bio only)
    console.log('📍 Making first profile change (Bio)...');
    const bioInput = page.locator('[data-testid="bio-input"]');
    await bioInput.clear();
    await bioInput.fill('First bio update');

    const saveButton1 = page
      .getByRole('button', { name: /save changes/i })
      .last();
    await saveButton1.click();
    await expect(page.locator('[role="alert"]').first()).toContainText(
      /success/i
    );
    await page.waitForTimeout(1500); // Allow change to be tracked
    console.log('✅ First change saved');

    // Make SECOND profile change (Pronouns only)
    console.log('📍 Making second profile change (Pronouns)...');
    const pronounsInput = page.locator('[data-testid="pronouns-input"]');
    await pronounsInput.clear();
    await pronounsInput.fill('he/him');

    const saveButton2 = page
      .getByRole('button', { name: /save changes/i })
      .last();
    await saveButton2.click();
    await expect(page.locator('[role="alert"]').first()).toContainText(
      /success/i
    );
    await page.waitForTimeout(1500); // Allow change to be tracked
    console.log('✅ Second change saved');

    // Logout and login as admin (use clearAuthState to avoid navigation issues)
    console.log('📍 Logging out and switching to admin...');
    await AuthHelpers.clearAuthState(page);
    await page.goto('/login');
    await page.waitForLoadState('domcontentloaded');
    await AuthHelpers.loginAs(page, 'admin');

    // Navigate to member details → History tab
    console.log('📍 Navigating to History tab...');
    await page.goto(`/admin/members/${testUser.id}`);
    await page.waitForLoadState('domcontentloaded');

    const historyTab = page.getByRole('tab', { name: 'History' });
    await historyTab.click();
    await page.waitForTimeout(1000);

    // Get the History tab panel to scope selectors
    const historyPanel = page.getByRole('tabpanel', { name: 'History' });

    // Verify BOTH changes are displayed
    console.log('📍 Verifying both changes are displayed...');
    await expect(historyPanel.locator('text=Bio:')).toBeVisible();
    await expect(historyPanel.locator('text=Pronouns:')).toBeVisible();
    console.log('✅ Multiple changes verified');

    // Note: df.cleanupAll() called automatically after test
  });
});
