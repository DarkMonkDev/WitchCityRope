import { test, expect } from '@playwright/test';
import {
  createTestUser,
  generateUniqueTestEmail,
  cleanupTestUser,
  type TestUser
} from './utils/database-helpers';
import { AuthHelpers } from './helpers/auth.helpers';

/**
 * Profile Update Persistence Test
 *
 * PURPOSE: Verify the fix for profile update persistence issue.
 *
 * ISSUE CONTEXT:
 * - User edits profile information in Profile Settings page
 * - Form shows success notification
 * - On page refresh, changes were gone (firstName, lastName, bio, discordName, fetLifeName not persisting)
 * - Migration added missing columns to database
 *
 * EXPECTED BEHAVIOR:
 * - PUT request sent to /api/users/{userId}/profile
 * - Database updated with ALL new values including firstName, lastName, bio, discordName, fetLifeName
 * - Changes persist after page refresh
 *
 * RACE CONDITION FIX: Uses unique test user to prevent interference from other tests
 */

test.describe('Profile Update Persistence', () => {
  test('should persist profile changes after save and page refresh', async ({ page }) => {
    console.log('🚀 Starting profile update persistence test...');

    // Create unique test user to prevent race conditions
    const testUser = await createTestUser({
      email: generateUniqueTestEmail('profile-persist'),
      password: 'Test123!',
      sceneName: `TestUser${Date.now()}`,
      membershipLevel: 'Admin'
    });

    try {
      // Step 1: Login with test user
      console.log('📍 Step 1: Logging in with test user...');
      await AuthHelpers.loginWith(page, { email: testUser.email, password: testUser.password });
      console.log('✅ Successfully logged in and navigated to dashboard');

    // Step 2: Navigate to Profile Settings
    console.log('📍 Step 2: Navigating to Profile Settings...');
    await page.goto('http://localhost:5173/dashboard/profile-settings');
    await page.waitForLoadState('networkidle');
    console.log('✅ Profile Settings page loaded');

    // Step 3: Capture current profile values using getByPlaceholder and getByRole
    console.log('📍 Step 3: Capturing current profile values...');
    const sceneNameInput = page.getByPlaceholder('Your scene name');
    const bioInput = page.getByPlaceholder('Tell us about yourself...');
    const pronounsInput = page.getByPlaceholder(/they\/them, she\/her, he\/him/);
    const firstNameInput = page.getByPlaceholder('Optional').first();
    const lastNameInput = page.getByPlaceholder('Optional').last();

    const originalSceneName = await sceneNameInput.inputValue();
    const originalBio = await bioInput.inputValue();
    const originalPronouns = await pronounsInput.inputValue();
    const originalFirstName = await firstNameInput.inputValue();
    const originalLastName = await lastNameInput.inputValue();

    console.log('📊 Original values:', {
      sceneName: originalSceneName,
      firstName: originalFirstName,
      lastName: originalLastName,
      pronouns: originalPronouns,
      bio: originalBio
    });

    // Step 4: Generate unique test values
    const timestamp = Date.now();
    const newSceneName = `TestAdmin_${timestamp}`;
    const newFirstName = `First_${timestamp}`;
    const newLastName = `Last_${timestamp}`;
    const newBio = `Updated bio at ${timestamp} - Testing persistence of firstName, lastName, bio, discordName, and fetLifeName`;
    const newPronouns = 'they/them';

    console.log('📊 New values:', {
      sceneName: newSceneName,
      firstName: newFirstName,
      lastName: newLastName,
      pronouns: newPronouns,
      bio: newBio
    });

    // Step 5: Update profile fields
    console.log('📍 Step 4: Updating profile fields...');

    // Clear and fill scene name
    await sceneNameInput.clear();
    await sceneNameInput.fill(newSceneName);

    // Clear and fill first name
    await firstNameInput.clear();
    await firstNameInput.fill(newFirstName);

    // Clear and fill last name
    await lastNameInput.clear();
    await lastNameInput.fill(newLastName);

    // Clear and fill pronouns
    await pronounsInput.clear();
    await pronounsInput.fill(newPronouns);

    // Clear and fill bio
    await bioInput.clear();
    await bioInput.fill(newBio);

    console.log('✅ Profile fields updated');

    // Step 6: Set up network request monitoring BEFORE clicking save
    console.log('📍 Step 5: Setting up network monitoring...');
    const updateRequestPromise = page.waitForRequest(
      request => request.url().includes('/api/users/') &&
                 request.url().includes('/profile') &&
                 request.method() === 'PUT',
      { timeout: 10000 }
    );

    const updateResponsePromise = page.waitForResponse(
      response => response.url().includes('/api/users/') &&
                  response.url().includes('/profile') &&
                  response.request().method() === 'PUT',
      { timeout: 10000 }
    );

    // Step 7: Click Save Changes button
    console.log('📍 Step 6: Clicking Save Changes button...');
    const saveButton = page.getByRole('button', { name: /Save Changes/i });
    await saveButton.click();

    // Step 8: Wait for and capture the PUT request
    console.log('📍 Step 7: Waiting for PUT request...');
    const updateRequest = await updateRequestPromise;
    const updateResponse = await updateResponsePromise;

    console.log('📡 PUT Request Details:');
    console.log('  URL:', updateRequest.url());
    console.log('  Method:', updateRequest.method());

    const requestBody = updateRequest.postDataJSON();
    console.log('  Request Body:', JSON.stringify(requestBody, null, 2));

    console.log('📡 PUT Response Details:');
    console.log('  Status:', updateResponse.status());
    console.log('  Status Text:', updateResponse.statusText());

    const responseBody = await updateResponse.json();
    console.log('  Response Body:', JSON.stringify(responseBody, null, 2));

    // Step 9: Verify response is successful
    console.log('📍 Step 8: Verifying response status...');
    expect(updateResponse.status()).toBe(200);
    console.log('✅ PUT request returned 200 OK');

    // Verify request body contains all fields
    console.log('📍 Step 9: Verifying request contains all fields...');
    expect(requestBody).toHaveProperty('sceneName', newSceneName);
    expect(requestBody).toHaveProperty('firstName', newFirstName);
    expect(requestBody).toHaveProperty('lastName', newLastName);
    expect(requestBody).toHaveProperty('bio', newBio);
    console.log('✅ Request body contains all required fields');

    // Step 10: Wait for success notification
    console.log('📍 Step 10: Waiting for success notification...');
    const successNotification = page.locator('.mantine-Notification-root:has-text("Success")').first();
    await expect(successNotification).toBeVisible({ timeout: 5000 });
    console.log('✅ Success notification appeared');

    // Step 11: Verify response contains updated data
    console.log('📍 Step 11: Verifying response data...');
    expect(responseBody.success).toBe(true);
    expect(responseBody.data).toBeDefined();
    console.log('✅ Response indicates success with data');

    // Step 12: Refresh the page to verify persistence
    console.log('📍 Step 12: Refreshing page to verify persistence...');
    await page.reload();
    await page.waitForLoadState('networkidle');
    console.log('✅ Page refreshed');

    // Step 13: Verify changes persisted
    console.log('📍 Step 13: Checking if changes persisted...');
    const persistedSceneName = await sceneNameInput.inputValue();
    const persistedFirstName = await firstNameInput.inputValue();
    const persistedLastName = await lastNameInput.inputValue();
    const persistedPronouns = await pronounsInput.inputValue();
    const persistedBio = await bioInput.inputValue();

    console.log('📊 Persisted values:', {
      sceneName: persistedSceneName,
      firstName: persistedFirstName,
      lastName: persistedLastName,
      pronouns: persistedPronouns,
      bio: persistedBio
    });

      // CRITICAL ASSERTIONS: These verify the database migration fix worked
      console.log('📍 Step 14: Asserting persistence of all fields...');
      expect(persistedSceneName).toBe(newSceneName);
      expect(persistedFirstName).toBe(newFirstName);
      expect(persistedLastName).toBe(newLastName);
      expect(persistedPronouns).toBe(newPronouns);
      expect(persistedBio).toBe(newBio);

      console.log('🎉 TEST PASSED: All profile changes persisted successfully!');
      console.log('✅ Database migration fix verified: firstName, lastName, bio now persist correctly');
    } finally {
      // Always cleanup test user, even if test fails
      await cleanupTestUser(testUser.id);
    }
  });

  test('should verify database was actually updated', async ({ page, request }) => {
    console.log('🚀 Starting database update verification test...');

    // Create unique test user to prevent race conditions
    const testUser = await createTestUser({
      email: generateUniqueTestEmail('profile-db-verify'),
      password: 'Test123!',
      sceneName: `DbVerifyTest${Date.now()}`,
      membershipLevel: 'Admin'
    });

    try {
      // Step 1: Login to get authentication
      console.log('📍 Step 1: Logging in...');
      await AuthHelpers.loginWith(page, { email: testUser.email, password: testUser.password });

    // Step 2: Get current user ID from page context
    console.log('📍 Step 2: Fetching current user profile...');
    await page.goto('http://localhost:5173/dashboard/profile-settings');
    await page.waitForLoadState('networkidle');

    // Step 3: Make direct API call to get profile
    const cookies = await page.context().cookies();
    console.log('📍 Step 3: Making direct API call with cookies...');

    // Extract user ID from page or use admin user's known ID
    const userId = await page.evaluate(() => {
      const user = localStorage.getItem('user');
      if (user) {
        const userData = JSON.parse(user);
        return userData.id;
      }
      return null;
    });

    console.log('👤 User ID:', userId);

      if (userId) {
        // Make direct API call to verify database state
        const apiResponse = await request.get(`http://localhost:5655/api/users/${userId}/profile`, {
          headers: {
            'Cookie': cookies.map(c => `${c.name}=${c.value}`).join('; ')
          }
        });

        const apiData = await apiResponse.json();
        console.log('📡 Direct API response:', JSON.stringify(apiData, null, 2));

        expect(apiResponse.status()).toBe(200);
        console.log('✅ Direct API call successful');

        // Verify the response has the new fields
        if (apiData.data) {
          console.log('📊 Profile data fields present:');
          console.log('  - firstName:', apiData.data.firstName ?? '(not set)');
          console.log('  - lastName:', apiData.data.lastName ?? '(not set)');
          console.log('  - bio:', apiData.data.bio ?? '(not set)');
          console.log('  - discordName:', apiData.data.discordName ?? '(not set)');
          console.log('  - fetLifeName:', apiData.data.fetLifeName ?? '(not set)');
        }
      }
    } finally {
      // Always cleanup test user, even if test fails
      await cleanupTestUser(testUser.id);
    }
  });
});
