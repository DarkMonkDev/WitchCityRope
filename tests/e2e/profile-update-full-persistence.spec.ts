/**
 * Profile Update Full Persistence E2E Test
 *
 * CRITICAL BUG THIS CATCHES: Profile update bug where UI showed success
 * but database wasn't updated because backend silently ignored new fields.
 *
 * This test uses the persistence template to verify:
 * 1. UI shows success after profile update
 * 2. API returns 200 OK
 * 3. Database contains updated values (CRITICAL)
 * 4. Page refresh shows updated values (CRITICAL)
 * 5. Database still correct after refresh
 *
 * RACE CONDITION FIX: Each test creates a unique user to prevent
 * multiple tests from modifying the same account simultaneously.
 */

import { test, expect } from '@playwright/test';
import {
  testProfileUpdatePersistence,
  testCompleteProfileUpdate,
  testSingleFieldUpdate,
  testClearProfileFields,
} from './templates/profile-update-persistence-template';
import { globalCleanup } from './templates/persistence-test-template';
import {
  createTestUser,
  generateUniqueTestEmail,
  cleanupTestUser,
  type TestUser
} from './utils/database-helpers';

test.describe('Profile Update Persistence Tests', () => {
  test.afterAll(async () => {
    await globalCleanup();
  });

  test('should persist complete profile update to database', async ({ page }) => {
    const timestamp = Date.now();

    // Create unique test user to prevent race conditions
    const testUser = await createTestUser({
      email: generateUniqueTestEmail('profile-complete'),
      password: 'Test123!',
      sceneName: `TestUser${timestamp}`,
      membershipLevel: 'Member'
    });

    try {
      await testProfileUpdatePersistence(page, {
        userEmail: testUser.email,
        userPassword: testUser.password,
        updatedFields: {
          firstName: `TestFirst${timestamp}`,
          lastName: `TestLast${timestamp}`,
          bio: `Updated bio at ${new Date().toISOString()}`,
          pronouns: 'they/them',
          discordName: `discord_${timestamp}`,
          fetLifeName: `fetlife_${timestamp}`,
        },
        successMessage: 'Profile updated successfully',
        screenshotPath: '/tmp/profile-update-complete',
      });
    } finally {
      // Always cleanup test user, even if test fails
      await cleanupTestUser(testUser.id);
    }
  });

  test('should persist single field update (firstName only)', async ({ page }) => {
    const testUser = await createTestUser({
      email: generateUniqueTestEmail('profile-firstname'),
      password: 'Test123!',
      sceneName: `FirstNameTest${Date.now()}`,
      membershipLevel: 'Member'
    });

    try {
      await testSingleFieldUpdate(
        page,
        testUser.email,
        testUser.password,
        'firstName',
        `UpdatedName${Date.now()}`
      );
    } finally {
      await cleanupTestUser(testUser.id);
    }
  });

  test('should persist bio update with special characters', async ({ page }) => {
    const specialBio = `Test bio with "quotes", 'apostrophes', and (parentheses) - ${Date.now()}`;

    const testUser = await createTestUser({
      email: generateUniqueTestEmail('profile-bio-special'),
      password: 'Test123!',
      sceneName: `BioTest${Date.now()}`,
      membershipLevel: 'Member'
    });

    try {
      await testSingleFieldUpdate(
        page,
        testUser.email,
        testUser.password,
        'bio',
        specialBio
      );
    } finally {
      await cleanupTestUser(testUser.id);
    }
  });

  test('should persist clearing bio field', async ({ page }) => {
    const testUser = await createTestUser({
      email: generateUniqueTestEmail('profile-bio-clear'),
      password: 'Test123!',
      sceneName: `ClearBioTest${Date.now()}`,
      membershipLevel: 'Member'
    });

    try {
      await testSingleFieldUpdate(
        page,
        testUser.email,
        testUser.password,
        'bio',
        ''
      );
    } finally {
      await cleanupTestUser(testUser.id);
    }
  });

  test('should persist Discord name update', async ({ page }) => {
    const testUser = await createTestUser({
      email: generateUniqueTestEmail('profile-discord'),
      password: 'Test123!',
      sceneName: `DiscordTest${Date.now()}`,
      membershipLevel: 'Member'
    });

    try {
      await testSingleFieldUpdate(
        page,
        testUser.email,
        testUser.password,
        'discordName',
        `DiscordUser#${Math.floor(Math.random() * 10000)}`
      );
    } finally {
      await cleanupTestUser(testUser.id);
    }
  });

  test('should persist FetLife name update', async ({ page }) => {
    const testUser = await createTestUser({
      email: generateUniqueTestEmail('profile-fetlife'),
      password: 'Test123!',
      sceneName: `FetLifeTest${Date.now()}`,
      membershipLevel: 'Member'
    });

    try {
      await testSingleFieldUpdate(
        page,
        testUser.email,
        testUser.password,
        'fetLifeName',
        `FetLifeUser${Date.now()}`
      );
    } finally {
      await cleanupTestUser(testUser.id);
    }
  });

  test('should persist pronouns update', async ({ page }) => {
    const pronounOptions = ['he/him', 'she/her', 'they/them', 'ze/hir'];
    const randomPronouns = pronounOptions[Math.floor(Math.random() * pronounOptions.length)];

    const testUser = await createTestUser({
      email: generateUniqueTestEmail('profile-pronouns'),
      password: 'Test123!',
      sceneName: `PronounsTest${Date.now()}`,
      membershipLevel: 'Member'
    });

    try {
      await testSingleFieldUpdate(
        page,
        testUser.email,
        testUser.password,
        'pronouns',
        randomPronouns
      );
    } finally {
      await cleanupTestUser(testUser.id);
    }
  });

  test('should persist multiple profile updates in sequence', async ({ page }) => {
    const timestamp = Date.now();

    const testUser = await createTestUser({
      email: generateUniqueTestEmail('profile-sequence'),
      password: 'Test123!',
      sceneName: `SequenceTest${timestamp}`,
      membershipLevel: 'Member'
    });

    try {
      // First update
      await testSingleFieldUpdate(
        page,
        testUser.email,
        testUser.password,
        'firstName',
        `First${timestamp}`
      );

      // Second update
      await testSingleFieldUpdate(
        page,
        testUser.email,
        testUser.password,
        'lastName',
        `Last${timestamp}`
      );

      // Third update
      await testSingleFieldUpdate(
        page,
        testUser.email,
        testUser.password,
        'bio',
        `Sequential bio update ${timestamp}`
      );
    } finally {
      await cleanupTestUser(testUser.id);
    }
  });

  test('should handle empty string updates (clearing optional fields)', async ({ page }) => {
    const testUser = await createTestUser({
      email: generateUniqueTestEmail('profile-clear-fields'),
      password: 'Test123!',
      sceneName: `ClearTest${Date.now()}`,
      membershipLevel: 'Member'
    });

    try {
      await testProfileUpdatePersistence(page, {
        userEmail: testUser.email,
        userPassword: testUser.password,
        updatedFields: {
          bio: '',
          discordName: '',
          fetLifeName: '',
        },
      });
    } finally {
      await cleanupTestUser(testUser.id);
    }
  });

  test('should persist long bio text', async ({ page }) => {
    const longBio = 'This is a very long bio. '.repeat(50) + `Updated at ${Date.now()}`;

    const testUser = await createTestUser({
      email: generateUniqueTestEmail('profile-long-bio'),
      password: 'Test123!',
      sceneName: `LongBioTest${Date.now()}`,
      membershipLevel: 'Member'
    });

    try {
      await testSingleFieldUpdate(
        page,
        testUser.email,
        testUser.password,
        'bio',
        longBio
      );
    } finally {
      await cleanupTestUser(testUser.id);
    }
  });

  test('CRITICAL: should detect if profile update shows success but fails to persist', async ({ page }) => {
    // This is the exact bug scenario that occurred:
    // - Backend silently ignored new fields
    // - UI showed success
    // - Database wasn't updated
    // - Page refresh showed old values

    const timestamp = Date.now();

    const testUser = await createTestUser({
      email: generateUniqueTestEmail('profile-critical'),
      password: 'Test123!',
      sceneName: `CriticalTest${timestamp}`,
      membershipLevel: 'Member'
    });

    try {
      await testProfileUpdatePersistence(page, {
        userEmail: testUser.email,
        userPassword: testUser.password,
        updatedFields: {
          firstName: `CriticalTest${timestamp}`,
          lastName: `PersistenceCheck${timestamp}`,
        },
        successMessage: 'Profile updated successfully',
      });

      // If we get here, the test passed - persistence is working correctly
      console.log('✅ PERSISTENCE VERIFIED: Profile update correctly persists to database');
    } catch (error) {
      // If the test fails, it detected the bug
      console.error('❌ BUG DETECTED: Profile update did NOT persist to database!');
      console.error('This is the exact bug that was found in production:');
      console.error('- UI showed success message');
      console.error('- API returned 200 OK');
      console.error('- Database was NOT updated');
      console.error('- Page refresh showed old values');
      throw error;
    } finally {
      await cleanupTestUser(testUser.id);
    }
  });
});

test.describe('Profile Update Edge Cases', () => {
  test.afterAll(async () => {
    await globalCleanup();
  });

  test('should handle special characters in all fields', async ({ page }) => {
    const timestamp = Date.now();

    const testUser = await createTestUser({
      email: generateUniqueTestEmail('profile-special-chars'),
      password: 'Test123!',
      sceneName: `SpecialTest${timestamp}`,
      membershipLevel: 'Member'
    });

    try {
      await testProfileUpdatePersistence(page, {
        userEmail: testUser.email,
        userPassword: testUser.password,
        updatedFields: {
          firstName: `O'Brien-${timestamp}`,
          lastName: `Müller & Sons`,
          bio: `Bio with "quotes", 'apostrophes', & ampersands, (parentheses), and émojis 🎉`,
          discordName: `user#1234`,
          fetLifeName: `fetlife_user-${timestamp}`,
        },
      });
    } finally {
      await cleanupTestUser(testUser.id);
    }
  });

  test('should handle null vs empty string correctly', async ({ page }) => {
    // First set to values
    const timestamp = Date.now();

    const testUser = await createTestUser({
      email: generateUniqueTestEmail('profile-null-empty'),
      password: 'Test123!',
      sceneName: `NullTest${timestamp}`,
      membershipLevel: 'Member'
    });

    try {
      await testProfileUpdatePersistence(page, {
        userEmail: testUser.email,
        userPassword: testUser.password,
        updatedFields: {
          bio: `Has bio ${timestamp}`,
          discordName: `has_discord`,
        },
      });

      // Then clear to empty string
      await testProfileUpdatePersistence(page, {
        userEmail: testUser.email,
        userPassword: testUser.password,
        updatedFields: {
          bio: '',
          discordName: '',
        },
      });
    } finally {
      await cleanupTestUser(testUser.id);
    }
  });

  test('should handle rapid successive updates', async ({ page }) => {
    // Test that multiple rapid updates don't cause race conditions
    const timestamp = Date.now();

    const testUser = await createTestUser({
      email: generateUniqueTestEmail('profile-rapid'),
      password: 'Test123!',
      sceneName: `RapidTest${timestamp}`,
      membershipLevel: 'Member'
    });

    try {
      for (let i = 0; i < 3; i++) {
        await testSingleFieldUpdate(
          page,
          testUser.email,
          testUser.password,
          'bio',
          `Rapid update #${i} at ${timestamp}`
        );
      }
    } finally {
      await cleanupTestUser(testUser.id);
    }
  });
});
