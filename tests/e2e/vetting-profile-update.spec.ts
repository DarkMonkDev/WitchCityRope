import { test, expect } from '@playwright/test';
import { AuthHelpers } from './test-utils/helpers/auth.helpers';

/**
 * E2E Tests for Automatic Profile Updates During Vetting Application Submission
 *
 * These tests verify that when a user submits a vetting application through the
 * simplified form, their user profile is automatically updated with:
 * - firstName (always updated)
 * - lastName (always updated)
 * - pronouns (optional - only updated if provided)
 * - fetLifeHandle (optional - only updated if provided)
 *
 * Implementation: /apps/api/Features/Vetting/Services/VettingService.cs
 * Method: SubmitSimplifiedApplicationAsync (lines ~1126-1154)
 *
 * CRITICAL: All tests run against Docker containers on port 5173 EXCLUSIVELY
 * Per docker-only-testing-standard.md
 */

test.describe('Vetting Application Profile Updates', () => {
  /**
   * Clean authentication state before each test
   */
  test.beforeEach(async ({ page }) => {
    await AuthHelpers.clearAuthState(page);
  });

  /**
   * TEST 1: User submits application with all fields - profile fully updated
   *
   * GIVEN: User logs in and navigates to vetting application form
   * WHEN: User submits application with firstName, lastName, pronouns, and fetLifeHandle
   * THEN:
   *   - Application is submitted successfully
   *   - User profile displays updated firstName and lastName
   *   - User profile displays updated pronouns
   *   - User profile displays updated fetLifeHandle
   */
  test('user submits application with all fields - profile fully updated', async ({ page }) => {
    // Arrange: Login as a test user
    await AuthHelpers.loginAs(page, 'member');

    // Navigate to vetting application form
    await page.goto('/join', { waitUntil: 'domcontentloaded' });

    // Wait for form to load
    const vettingForm = page.locator('form').last();
    await expect(vettingForm).toBeVisible({ timeout: 10000 });

    // Generate unique test data
    const timestamp = Date.now();
    const testData = {
      firstName: `FirstName${timestamp}`,
      lastName: `LastName${timestamp}`,
      sceneName: `Scene${timestamp}`,
      pronouns: 'they/them',
      fetLifeHandle: `FetLifeHandle${timestamp}`,
    };

    // Fill out form with all fields including optional ones (using data-testid attributes)
    const firstNameInput = page.getByTestId('first-name-input');
    if (await firstNameInput.count() > 0) {
      await firstNameInput.fill(testData.firstName);
    }

    const lastNameInput = page.getByTestId('last-name-input');
    if (await lastNameInput.count() > 0) {
      await lastNameInput.fill(testData.lastName);
    }

    // Note: Scene name is readonly in the vetting form (shown but not editable)
    // It's only set from the user's existing profile data

    const pronounsInput = page.getByTestId('pronouns-input');
    if (await pronounsInput.count() > 0) {
      await pronounsInput.fill(testData.pronouns);
    }

    const fetLifeInput = page.getByTestId('fetlife-handle-input');
    if (await fetLifeInput.count() > 0) {
      await fetLifeInput.fill(testData.fetLifeHandle);
    }

    // Fill required fields
    const whyJoinInput = page.getByTestId('why-join-textarea');
    if (await whyJoinInput.count() > 0) {
      await whyJoinInput.fill('I am interested in learning rope bondage in a safe community.');
    }

    const experienceInput = page.getByTestId('experience-with-rope-textarea');
    if (await experienceInput.count() > 0) {
      await experienceInput.fill('I have been practicing rope bondage for 2 years.');
    }

    const agreementCheckbox = page.getByTestId('community-standards-checkbox');
    if (await agreementCheckbox.count() > 0) {
      await agreementCheckbox.check();
    }

    // Act: Submit the application
    const submitButton = page.locator('button[type="submit"]').last();
    await submitButton.click();

    // Wait for submission to complete (success message or redirect)
    await page.waitForTimeout(2000);

    // Assert: Navigate to user profile/dashboard to verify updates
    await page.goto('/dashboard', { waitUntil: 'domcontentloaded' });

    // Verify profile displays updated information
    // Note: Actual selectors depend on dashboard implementation
    const profileSection = page.locator('[data-testid="user-profile"], .profile, .user-info').last();
    await expect(profileSection).toBeVisible({ timeout: 10000 });

    // Check for updated firstName and lastName
    await expect(page.locator(`text=${testData.firstName}`).last()).toBeVisible();
    await expect(page.locator(`text=${testData.lastName}`).last()).toBeVisible();

    // Check for updated pronouns
    await expect(page.locator(`text=${testData.pronouns}`).last()).toBeVisible();

    // Check for updated fetLifeHandle
    await expect(page.locator(`text=${testData.fetLifeHandle}`).last()).toBeVisible();

    // Screenshot for documentation
    await page.screenshot({
      path: './test-results/vetting-profile-update-all-fields.png',
      fullPage: true
    });
  });

  /**
   * TEST 2: User submits application with minimal fields - optional fields not overwritten
   *
   * GIVEN: User has existing pronouns and fetLifeHandle in profile
   * WHEN: User submits application with only firstName and lastName (no optional fields)
   * THEN:
   *   - firstName and lastName are updated
   *   - Existing pronouns are preserved (not overwritten with null)
   *   - Existing fetLifeHandle is preserved (not overwritten with null)
   */
  test('user submits application with minimal fields - existing optional fields preserved', async ({ page }) => {
    // NOTE: This test requires a user account with existing pronouns/fetLifeHandle
    // This is a limitation of E2E testing - we can't easily set up user data
    // This test will be SKIPPED if user doesn't have existing data
    // Consider creating a test account with pre-populated data

    test.skip(true, 'Requires user account with pre-populated optional fields');

    // Arrange: Login as a test user with existing profile data
    await AuthHelpers.loginAs(page, 'member');

    // Act: Submit vetting application with only required fields
    await page.goto('/join');
    await page.waitForLoadState('domcontentloaded');

    // Fill minimal fields
    // ... (similar to test above but without pronouns/fetLifeHandle)

    // Assert: Verify optional fields are NOT overwritten
    // ... (check that existing values are still present)
  });

  /**
   * TEST 3: Profile updates are visible in user dashboard after submission
   *
   * GIVEN: User submits vetting application with profile data
   * WHEN: User navigates to dashboard
   * THEN:
   *   - Dashboard displays updated firstName
   *   - Dashboard displays updated lastName
   *   - Dashboard displays updated pronouns (if provided)
   *   - Dashboard displays updated fetLifeHandle (if provided)
   */
  test('profile updates are visible in user dashboard after submission', async ({ page }) => {
    // Arrange: Login as test user
    await AuthHelpers.loginAs(page, 'member');

    // Navigate to vetting application
    await page.goto('/join', { waitUntil: 'domcontentloaded' });

    const timestamp = Date.now();
    const testData = {
      firstName: `Dashboard${timestamp}`,
      lastName: `Test${timestamp}`,
      pronouns: 'she/her',
    };

    // Fill and submit application (using data-testid attributes)
    const firstNameInput = page.getByTestId('first-name-input');
    if (await firstNameInput.count() > 0) {
      await firstNameInput.last().fill(testData.firstName);
    }

    const lastNameInput = page.getByTestId('last-name-input');
    if (await lastNameInput.count() > 0) {
      await lastNameInput.last().fill(testData.lastName);
    }

    const pronounsInput = page.getByTestId('pronouns-input');
    if (await pronounsInput.count() > 0) {
      await pronounsInput.last().fill(testData.pronouns);
    }

    // Fill required fields
    // Note: Scene name is readonly in the vetting form

    const whyJoinInput = page.getByTestId('why-join-textarea');
    if (await whyJoinInput.count() > 0) {
      await whyJoinInput.last().fill('I am interested in the community.');
    }

    const experienceInput = page.getByTestId('experience-with-rope-textarea');
    if (await experienceInput.count() > 0) {
      await experienceInput.last().fill('I have some experience with rope.');
    }

    const agreementCheckbox = page.getByTestId('community-standards-checkbox');
    if (await agreementCheckbox.count() > 0) {
      await agreementCheckbox.last().check();
    }

    const submitButton = page.locator('button[type="submit"]').last();
    await submitButton.click();

    // Wait for submission
    await page.waitForTimeout(2000);

    // Act: Navigate to dashboard
    await page.goto('/dashboard', { waitUntil: 'domcontentloaded' });

    // Assert: Verify profile section displays updated data
    const profileSection = page.locator('[data-testid="user-profile"], .profile').last();
    await expect(profileSection).toBeVisible({ timeout: 10000 });

    // Verify updated information is visible
    await expect(page.locator(`text=${testData.firstName}`).last()).toBeVisible({ timeout: 5000 });
    await expect(page.locator(`text=${testData.lastName}`).last()).toBeVisible({ timeout: 5000 });
    await expect(page.locator(`text=${testData.pronouns}`).last()).toBeVisible({ timeout: 5000 });

    // Screenshot for documentation
    await page.screenshot({
      path: './test-results/vetting-dashboard-profile-update.png',
      fullPage: true
    });
  });

  /**
   * TEST 4: Admin can see updated profile after user submits vetting application
   *
   * GIVEN: User submits vetting application with profile data
   * WHEN: Admin views the user's vetting application
   * THEN:
   *   - Application displays updated firstName
   *   - Application displays updated lastName
   *   - Application displays updated pronouns (if provided)
   *   - Application displays updated fetLifeHandle (if provided)
   */
  test('admin can see updated profile after user submits vetting application', async ({ page }) => {
    // NOTE: This test requires two-user flow (user submits, admin views)
    // This is complex for E2E testing - requires either:
    // 1. Two browser contexts (user + admin)
    // 2. Database seeding of test application
    // 3. API calls to create application, then admin UI verification

    test.skip(true, 'Requires multi-user workflow or API setup');

    // This would require:
    // 1. User creates vetting application (either via UI or API)
    // 2. Admin logs in
    // 3. Admin navigates to vetting application review page
    // 4. Admin verifies user profile fields are updated in application details
  });
});

/**
 * Additional Test Scenarios (Future)
 *
 * These tests would provide more comprehensive coverage but require
 * more complex test setup (database seeding, API calls, etc.)
 *
 * - Verify UpdatedAt timestamp changes in database
 * - Verify transaction atomicity (if application fails, profile not updated)
 * - Verify concurrent submissions don't cause race conditions
 * - Verify profile updates work for all user roles (Member, Teacher, etc.)
 * - Verify profile field character limits are enforced
 * - Verify XSS protection on profile fields
 */
