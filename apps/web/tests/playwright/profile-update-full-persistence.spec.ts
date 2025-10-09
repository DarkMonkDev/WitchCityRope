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
 */

import { test, expect } from '@playwright/test';
import {
  testProfileUpdatePersistence,
  testCompleteProfileUpdate,
  testSingleFieldUpdate,
  testClearProfileFields,
} from './templates/profile-update-persistence-template';
import { globalCleanup } from './templates/persistence-test-template';

// Test accounts
const VETTED_USER = {
  email: 'vetted@witchcityrope.com',
  password: 'Test123!',
};

const MEMBER_USER = {
  email: 'member@witchcityrope.com',
  password: 'Test123!',
};

test.describe('Profile Update Persistence Tests', () => {
  test.afterAll(async () => {
    await globalCleanup();
  });

  test('should persist complete profile update to database', async ({ page }) => {
    const timestamp = Date.now();

    await testProfileUpdatePersistence(page, {
      userEmail: VETTED_USER.email,
      userPassword: VETTED_USER.password,
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
  });

  test('should persist single field update (firstName only)', async ({ page }) => {
    await testSingleFieldUpdate(
      page,
      MEMBER_USER.email,
      MEMBER_USER.password,
      'firstName',
      `UpdatedName${Date.now()}`
    );
  });

  test('should persist bio update with special characters', async ({ page }) => {
    const specialBio = `Test bio with "quotes", 'apostrophes', and (parentheses) - ${Date.now()}`;

    await testSingleFieldUpdate(
      page,
      VETTED_USER.email,
      VETTED_USER.password,
      'bio',
      specialBio
    );
  });

  test('should persist clearing bio field', async ({ page }) => {
    await testSingleFieldUpdate(
      page,
      MEMBER_USER.email,
      MEMBER_USER.password,
      'bio',
      ''
    );
  });

  test('should persist Discord name update', async ({ page }) => {
    await testSingleFieldUpdate(
      page,
      VETTED_USER.email,
      VETTED_USER.password,
      'discordName',
      `DiscordUser#${Math.floor(Math.random() * 10000)}`
    );
  });

  test('should persist FetLife name update', async ({ page }) => {
    await testSingleFieldUpdate(
      page,
      MEMBER_USER.email,
      MEMBER_USER.password,
      'fetLifeName',
      `FetLifeUser${Date.now()}`
    );
  });

  test('should persist pronouns update', async ({ page }) => {
    const pronounOptions = ['he/him', 'she/her', 'they/them', 'ze/hir'];
    const randomPronouns = pronounOptions[Math.floor(Math.random() * pronounOptions.length)];

    await testSingleFieldUpdate(
      page,
      VETTED_USER.email,
      VETTED_USER.password,
      'pronouns',
      randomPronouns
    );
  });

  test('should persist multiple profile updates in sequence', async ({ page }) => {
    const timestamp = Date.now();

    // First update
    await testSingleFieldUpdate(
      page,
      MEMBER_USER.email,
      MEMBER_USER.password,
      'firstName',
      `First${timestamp}`
    );

    // Second update
    await testSingleFieldUpdate(
      page,
      MEMBER_USER.email,
      MEMBER_USER.password,
      'lastName',
      `Last${timestamp}`
    );

    // Third update
    await testSingleFieldUpdate(
      page,
      MEMBER_USER.email,
      MEMBER_USER.password,
      'bio',
      `Sequential bio update ${timestamp}`
    );
  });

  test('should handle empty string updates (clearing optional fields)', async ({ page }) => {
    await testProfileUpdatePersistence(page, {
      userEmail: VETTED_USER.email,
      userPassword: VETTED_USER.password,
      updatedFields: {
        bio: '',
        discordName: '',
        fetLifeName: '',
      },
    });
  });

  test('should persist long bio text', async ({ page }) => {
    const longBio = 'This is a very long bio. '.repeat(50) + `Updated at ${Date.now()}`;

    await testSingleFieldUpdate(
      page,
      MEMBER_USER.email,
      MEMBER_USER.password,
      'bio',
      longBio
    );
  });

  test('CRITICAL: should detect if profile update shows success but fails to persist', async ({ page }) => {
    // This is the exact bug scenario that occurred:
    // - Backend silently ignored new fields
    // - UI showed success
    // - Database wasn't updated
    // - Page refresh showed old values

    const timestamp = Date.now();

    try {
      await testProfileUpdatePersistence(page, {
        userEmail: MEMBER_USER.email,
        userPassword: MEMBER_USER.password,
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
    }
  });
});

test.describe('Profile Update Edge Cases', () => {
  test.afterAll(async () => {
    await globalCleanup();
  });

  test('should handle special characters in all fields', async ({ page }) => {
    const timestamp = Date.now();

    await testProfileUpdatePersistence(page, {
      userEmail: VETTED_USER.email,
      userPassword: VETTED_USER.password,
      updatedFields: {
        firstName: `O'Brien-${timestamp}`,
        lastName: `Müller & Sons`,
        bio: `Bio with "quotes", 'apostrophes', & ampersands, (parentheses), and émojis 🎉`,
        discordName: `user#1234`,
        fetLifeName: `fetlife_user-${timestamp}`,
      },
    });
  });

  test('should handle null vs empty string correctly', async ({ page }) => {
    // First set to values
    const timestamp = Date.now();
    await testProfileUpdatePersistence(page, {
      userEmail: MEMBER_USER.email,
      userPassword: MEMBER_USER.password,
      updatedFields: {
        bio: `Has bio ${timestamp}`,
        discordName: `has_discord`,
      },
    });

    // Then clear to empty string
    await testProfileUpdatePersistence(page, {
      userEmail: MEMBER_USER.email,
      userPassword: MEMBER_USER.password,
      updatedFields: {
        bio: '',
        discordName: '',
      },
    });
  });

  test('should handle rapid successive updates', async ({ page }) => {
    // Test that multiple rapid updates don't cause race conditions
    const timestamp = Date.now();

    for (let i = 0; i < 3; i++) {
      await testSingleFieldUpdate(
        page,
        VETTED_USER.email,
        VETTED_USER.password,
        'bio',
        `Rapid update #${i} at ${timestamp}`
      );
    }
  });
});
