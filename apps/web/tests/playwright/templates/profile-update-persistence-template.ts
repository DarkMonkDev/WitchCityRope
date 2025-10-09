/**
 * Profile Update Persistence Test Template
 *
 * CRITICAL BUG THIS CATCHES: Profile update bug where:
 * - User updates profile fields (firstName, lastName, bio, etc.)
 * - UI shows success message
 * - Database is NOT updated (backend silently ignored fields)
 * - Page refresh shows old values
 *
 * This template ensures profile updates actually persist to database.
 */

import { Page, expect } from '@playwright/test';
import {
  PersistenceTestTemplate,
  TestConfig,
  PersistenceTestOptions,
  verifySuccessMessage,
  verifyNoErrorMessage,
  verifyApiSuccess,
} from './persistence-test-template';
import { DatabaseHelpers, ProfileFields } from '../utils/database-helpers';

// ============================================================================
// PROFILE UPDATE TEST CONFIG
// ============================================================================

export interface ProfileUpdateTestConfig {
  /** User credentials for login */
  userEmail: string;
  userPassword: string;
  /** Profile fields to update */
  updatedFields: ProfileFields;
  /** Optional: Expected success message */
  successMessage?: string;
  /** Optional: Screenshot path */
  screenshotPath?: string;
}

/**
 * Create a complete profile update persistence test
 *
 * Test Pattern:
 * 1. Login as user
 * 2. Navigate to profile settings
 * 3. Update profile fields
 * 4. Click Save
 * 5. Verify UI shows success
 * 6. Verify API returned 200
 * 7. Verify database has new values
 * 8. Refresh page
 * 9. Verify form shows new values (persistence)
 * 10. Verify database still has new values
 */
export async function testProfileUpdatePersistence(
  page: Page,
  testConfig: ProfileUpdateTestConfig
): Promise<void> {
  const { userEmail, userPassword, updatedFields, successMessage, screenshotPath } = testConfig;

  let userId: string;

  const config: TestConfig = {
    description: `Profile update persistence for ${userEmail}`,

    // STEP 1: Setup - Login and navigate to profile settings
    setup: async (page: Page) => {
      // Login
      await page.goto('http://localhost:5173/login');
      await page.waitForLoadState('networkidle');

      await page.locator('[data-testid="email-input"]').fill(userEmail);
      await page.locator('[data-testid="password-input"]').fill(userPassword);
      await page.locator('[data-testid="login-button"]').click();

      // Wait for dashboard
      await page.waitForURL('**/dashboard', { timeout: 10000 });
      await page.waitForLoadState('networkidle');

      // Get user ID for database verification
      userId = await DatabaseHelpers.getUserIdFromEmail(userEmail);
      console.log(`✅ Logged in as ${userEmail} (ID: ${userId})`);

      // Navigate to profile settings
      // Try multiple possible selectors for profile link
      const profileLink = page.locator(
        'a[href*="profile"], a:has-text("Edit Profile"), a:has-text("Profile Settings"), [data-testid="profile-link"]'
      ).first();

      if (await profileLink.count() > 0) {
        await profileLink.click();
        await page.waitForLoadState('networkidle');
      } else {
        // Fallback: Navigate directly
        await page.goto('http://localhost:5173/profile/settings');
        await page.waitForLoadState('networkidle');
      }

      console.log('✅ Navigated to profile settings');
    },

    // STEP 2: Action - Update profile fields and save
    action: async (page: Page) => {
      // Define which fields belong to which tabs
      const personalFields = ['firstName', 'lastName', 'bio', 'pronouns'];
      const socialFields = ['discordName', 'fetLifeName', 'phoneNumber'];

      // Separate fields by tab
      const personalUpdates: Record<string, string> = {};
      const socialUpdates: Record<string, string> = {};
      let lastTabUsed: 'personal' | 'social' = 'personal';

      for (const [field, value] of Object.entries(updatedFields)) {
        if (value === null || value === undefined) continue;

        if (socialFields.includes(field)) {
          socialUpdates[field] = value;
          lastTabUsed = 'social';
        } else if (personalFields.includes(field)) {
          personalUpdates[field] = value;
          if (Object.keys(socialUpdates).length === 0) {
            lastTabUsed = 'personal';
          }
        }
      }

      // Update personal fields if any
      if (Object.keys(personalUpdates).length > 0) {
        const personalTab = page.locator('button:has-text("Personal"), [data-value="personal"]').first();
        if (await personalTab.count() > 0) {
          await personalTab.click();
          await page.waitForTimeout(500);
          console.log('✅ Switched to Personal tab');
        }

        for (const [field, value] of Object.entries(personalUpdates)) {
          const selectorMap: Record<string, string> = {
            firstName: '[data-testid="first-name-input"], input[name="firstName"], #firstName',
            lastName: '[data-testid="last-name-input"], input[name="lastName"], #lastName',
            bio: '[data-testid="bio-input"], textarea[name="bio"], #bio',
            pronouns: '[data-testid="pronouns-input"], input[name="pronouns"], #pronouns',
          };

          const selector = selectorMap[field];
          if (!selector) continue;

          const input = page.locator(selector).first();
          if (await input.count() === 0) continue;

          await input.waitFor({ state: 'visible', timeout: 5000 });
          await input.clear();
          await input.fill(value);
          console.log(`✅ Updated ${field} to "${value}"`);
        }

        // Click save for personal tab
        const personalSaveButton = page.locator('button[type="submit"]:visible').first();
        await personalSaveButton.waitFor({ state: 'visible', timeout: 5000 });
        await personalSaveButton.click();
        console.log('✅ Clicked Save button on Personal tab');
        await page.waitForTimeout(1500); // Wait for save to complete
      }

      // Update social fields if any
      if (Object.keys(socialUpdates).length > 0) {
        const socialTab = page.locator('button:has-text("Social"), [data-value="social"]').first();
        if (await socialTab.count() > 0) {
          await socialTab.click();
          await page.waitForTimeout(500);
          console.log('✅ Switched to Social tab');
        }

        for (const [field, value] of Object.entries(socialUpdates)) {
          const selectorMap: Record<string, string> = {
            discordName: '[data-testid="discord-name-input"], input[name="discordName"], #discordName',
            fetLifeName: '[data-testid="fetlife-name-input"], input[name="fetLifeName"], #fetLifeName',
            phoneNumber: '[data-testid="phone-number-input"], input[name="phoneNumber"], #phoneNumber',
          };

          const selector = selectorMap[field];
          if (!selector) continue;

          const input = page.locator(selector).first();
          if (await input.count() === 0) continue;

          await input.waitFor({ state: 'visible', timeout: 5000 });
          await input.clear();
          await input.fill(value);
          console.log(`✅ Updated ${field} to "${value}"`);
        }

        // Click save for social tab
        const socialSaveButton = page.locator('button[type="submit"]:visible').first();
        await socialSaveButton.waitFor({ state: 'visible', timeout: 5000 });
        await socialSaveButton.click();
        console.log('✅ Clicked Save button on Social tab');
        await page.waitForTimeout(1500); // Wait for save to complete
      }
    },

    // STEP 3: Verify UI shows success
    verifyUiSuccess: async (page: Page) => {
      await verifySuccessMessage(page, successMessage);
      await verifyNoErrorMessage(page);
      console.log('✅ UI shows success message');
    },

    // STEP 4: Verify API response (optional)
    verifyApiResponse: async (response) => {
      await verifyApiSuccess(response);
    },

    // STEP 5: Verify database was updated (CRITICAL)
    verifyDatabaseState: async (page: Page) => {
      const dbFields = await DatabaseHelpers.verifyProfileFields(userId, updatedFields);
      console.log('✅ Database verified:', dbFields);
    },

    // STEP 6: Verify persistence after refresh (CRITICAL)
    verifyPersistence: async (page: Page) => {
      // Define which fields belong to which tabs
      const personalFields = ['firstName', 'lastName', 'bio', 'pronouns'];
      const socialFields = ['discordName', 'fetLifeName', 'phoneNumber'];

      // Verify form fields still show updated values
      for (const [field, expectedValue] of Object.entries(updatedFields)) {
        if (expectedValue === null || expectedValue === undefined) continue;

        // Navigate to correct tab based on field
        if (socialFields.includes(field)) {
          // Switch to Social tab
          const socialTab = page.locator('button:has-text("Social"), [data-value="social"]').first();
          if (await socialTab.count() > 0) {
            await socialTab.click();
            await page.waitForTimeout(500); // Wait for tab transition
          }
        } else if (personalFields.includes(field)) {
          // Switch to Personal tab
          const personalTab = page.locator('button:has-text("Personal"), [data-value="personal"]').first();
          if (await personalTab.count() > 0) {
            await personalTab.click();
            await page.waitForTimeout(500); // Wait for tab transition
          }
        }

        const selectorMap: Record<string, string> = {
          firstName: '[data-testid="first-name-input"], input[name="firstName"], #firstName',
          lastName: '[data-testid="last-name-input"], input[name="lastName"], #lastName',
          bio: '[data-testid="bio-input"], textarea[name="bio"], #bio',
          pronouns: '[data-testid="pronouns-input"], input[name="pronouns"], #pronouns',
          discordName: '[data-testid="discord-name-input"], input[name="discordName"], #discordName',
          fetLifeName: '[data-testid="fetlife-name-input"], input[name="fetLifeName"], #fetLifeName',
          phoneNumber: '[data-testid="phone-number-input"], input[name="phoneNumber"], #phoneNumber',
        };

        const selector = selectorMap[field];
        if (!selector) continue;

        const input = page.locator(selector).first();
        if (await input.count() === 0) continue;

        // Wait for input to be visible
        await input.waitFor({ state: 'visible', timeout: 5000 });

        const actualValue = await input.inputValue();
        if (actualValue !== expectedValue) {
          throw new Error(
            `Field ${field} not persisted: expected "${expectedValue}" but got "${actualValue}"`
          );
        }

        console.log(`✅ Field ${field} persisted: "${actualValue}"`);
      }
    },
  };

  const options: PersistenceTestOptions = {
    screenshotPath: screenshotPath || '/tmp/profile-update-test',
    apiEndpoint: /\/api\/(user|profile|me)/i,
    apiMethod: 'PUT',
    waitAfterAction: 1500, // Wait for API call to complete
  };

  await PersistenceTestTemplate.runTest(page, config, options);
}

// ============================================================================
// COMMON PROFILE UPDATE SCENARIOS
// ============================================================================

/**
 * Test updating all profile fields at once
 */
export async function testCompleteProfileUpdate(
  page: Page,
  userEmail: string,
  userPassword: string
): Promise<void> {
  const timestamp = Date.now();
  const updatedFields: ProfileFields = {
    firstName: `TestFirst${timestamp}`,
    lastName: `TestLast${timestamp}`,
    bio: `Test bio updated at ${new Date().toISOString()}`,
    pronouns: 'they/them',
    discordName: `discord_user_${timestamp}`,
    fetLifeName: `fetlife_user_${timestamp}`,
  };

  await testProfileUpdatePersistence(page, {
    userEmail,
    userPassword,
    updatedFields,
    successMessage: 'Profile updated successfully',
  });
}

/**
 * Test updating only one field (minimal update)
 */
export async function testSingleFieldUpdate(
  page: Page,
  userEmail: string,
  userPassword: string,
  field: keyof ProfileFields,
  value: string
): Promise<void> {
  const updatedFields: ProfileFields = {
    [field]: value,
  };

  await testProfileUpdatePersistence(page, {
    userEmail,
    userPassword,
    updatedFields,
  });
}

/**
 * Test clearing profile fields (set to empty string)
 */
export async function testClearProfileFields(
  page: Page,
  userEmail: string,
  userPassword: string
): Promise<void> {
  const updatedFields: ProfileFields = {
    bio: '',
    discordName: '',
    fetLifeName: '',
  };

  await testProfileUpdatePersistence(page, {
    userEmail,
    userPassword,
    updatedFields,
  });
}

// Export all functions
export default {
  testProfileUpdatePersistence,
  testCompleteProfileUpdate,
  testSingleFieldUpdate,
  testClearProfileFields,
};
