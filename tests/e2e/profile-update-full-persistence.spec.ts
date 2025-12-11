/**
 * Profile Update Full Persistence E2E Test - DataFactory Migration
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
 * MIGRATION NOTES:
 * - Migrated from deprecated database-helpers.ts createTestUser()
 * - Now uses DataFactory with df.users.createVerified()
 * - Automatic cleanup via df fixture
 */

import { expect } from '@playwright/test';
import { test } from '../lib/datafactory/fixtures/test.fixture';
import {
  testProfileUpdatePersistence,
  testSingleFieldUpdate,
} from './templates/profile-update-persistence-template';

test.describe('Profile Update Persistence Tests', () => {
  test('should persist complete profile update to database', async ({ page, df }) => {
    const timestamp = Date.now();

    // Create unique test user using DataFactory
    const testUser = await df.users.createVerified({
      email: `profile-complete-${timestamp}@test.com`,
      password: 'Test123!',
      firstName: 'TestUser',
      lastName: `${timestamp}`,
    });

    await testProfileUpdatePersistence(page, {
      userEmail: testUser.email,
      userPassword: 'Test123!',
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

    // Note: df.cleanupAll() called automatically after test
  });

  test('should persist single field update (firstName only)', async ({ page, df }) => {
    const timestamp = Date.now();

    const testUser = await df.users.createVerified({
      email: `profile-firstname-${timestamp}@test.com`,
      password: 'Test123!',
      firstName: 'FirstNameTest',
      lastName: `${timestamp}`,
    });

    await testSingleFieldUpdate(
      page,
      testUser.email,
      'Test123!',
      'firstName',
      `UpdatedName${timestamp}`
    );
  });

  test('should persist bio update with special characters', async ({ page, df }) => {
    const timestamp = Date.now();
    const specialBio = `Test bio with "quotes", 'apostrophes', and (parentheses) - ${timestamp}`;

    const testUser = await df.users.createVerified({
      email: `profile-bio-special-${timestamp}@test.com`,
      password: 'Test123!',
      firstName: 'BioTest',
      lastName: `${timestamp}`,
    });

    await testSingleFieldUpdate(
      page,
      testUser.email,
      'Test123!',
      'bio',
      specialBio
    );
  });

  test('should persist clearing bio field', async ({ page, df }) => {
    const timestamp = Date.now();

    const testUser = await df.users.createVerified({
      email: `profile-bio-clear-${timestamp}@test.com`,
      password: 'Test123!',
      firstName: 'ClearBioTest',
      lastName: `${timestamp}`,
    });

    await testSingleFieldUpdate(
      page,
      testUser.email,
      'Test123!',
      'bio',
      ''
    );
  });

  test('should persist Discord name update', async ({ page, df }) => {
    const timestamp = Date.now();

    const testUser = await df.users.createVerified({
      email: `profile-discord-${timestamp}@test.com`,
      password: 'Test123!',
      firstName: 'DiscordTest',
      lastName: `${timestamp}`,
    });

    await testSingleFieldUpdate(
      page,
      testUser.email,
      'Test123!',
      'discordName',
      `DiscordUser#${Math.floor(Math.random() * 10000)}`
    );
  });

  test('should persist FetLife name update', async ({ page, df }) => {
    const timestamp = Date.now();

    const testUser = await df.users.createVerified({
      email: `profile-fetlife-${timestamp}@test.com`,
      password: 'Test123!',
      firstName: 'FetLifeTest',
      lastName: `${timestamp}`,
    });

    await testSingleFieldUpdate(
      page,
      testUser.email,
      'Test123!',
      'fetLifeName',
      `FetLifeUser${timestamp}`
    );
  });

  test('should persist pronouns update', async ({ page, df }) => {
    const timestamp = Date.now();
    const pronounOptions = ['he/him', 'she/her', 'they/them', 'ze/hir'];
    const randomPronouns = pronounOptions[Math.floor(Math.random() * pronounOptions.length)];

    const testUser = await df.users.createVerified({
      email: `profile-pronouns-${timestamp}@test.com`,
      password: 'Test123!',
      firstName: 'PronounsTest',
      lastName: `${timestamp}`,
    });

    await testSingleFieldUpdate(
      page,
      testUser.email,
      'Test123!',
      'pronouns',
      randomPronouns
    );
  });

  test('should persist multiple profile updates in sequence', async ({ page, df }) => {
    const timestamp = Date.now();

    const testUser = await df.users.createVerified({
      email: `profile-sequence-${timestamp}@test.com`,
      password: 'Test123!',
      firstName: 'SequenceTest',
      lastName: `${timestamp}`,
    });

    // First update
    await testSingleFieldUpdate(
      page,
      testUser.email,
      'Test123!',
      'firstName',
      `First${timestamp}`
    );

    // Second update
    await testSingleFieldUpdate(
      page,
      testUser.email,
      'Test123!',
      'lastName',
      `Last${timestamp}`
    );

    // Third update
    await testSingleFieldUpdate(
      page,
      testUser.email,
      'Test123!',
      'bio',
      `Sequential bio update ${timestamp}`
    );
  });

  test('should handle empty string updates (clearing optional fields)', async ({ page, df }) => {
    const timestamp = Date.now();

    const testUser = await df.users.createVerified({
      email: `profile-clear-fields-${timestamp}@test.com`,
      password: 'Test123!',
      firstName: 'ClearTest',
      lastName: `${timestamp}`,
    });

    await testProfileUpdatePersistence(page, {
      userEmail: testUser.email,
      userPassword: 'Test123!',
      updatedFields: {
        bio: '',
        discordName: '',
        fetLifeName: '',
      },
    });
  });

  test('should persist long bio text', async ({ page, df }) => {
    const timestamp = Date.now();
    const longBio = 'This is a very long bio. '.repeat(50) + `Updated at ${timestamp}`;

    const testUser = await df.users.createVerified({
      email: `profile-long-bio-${timestamp}@test.com`,
      password: 'Test123!',
      firstName: 'LongBioTest',
      lastName: `${timestamp}`,
    });

    await testSingleFieldUpdate(
      page,
      testUser.email,
      'Test123!',
      'bio',
      longBio
    );
  });

  test('CRITICAL: should detect if profile update shows success but fails to persist', async ({ page, df }) => {
    // This is the exact bug scenario that occurred:
    // - Backend silently ignored new fields
    // - UI showed success
    // - Database wasn't updated
    // - Page refresh showed old values

    const timestamp = Date.now();

    const testUser = await df.users.createVerified({
      email: `profile-critical-${timestamp}@test.com`,
      password: 'Test123!',
      firstName: 'CriticalTest',
      lastName: `${timestamp}`,
    });

    try {
      await testProfileUpdatePersistence(page, {
        userEmail: testUser.email,
        userPassword: 'Test123!',
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
  test('should handle special characters in all fields', async ({ page, df }) => {
    const timestamp = Date.now();

    const testUser = await df.users.createVerified({
      email: `profile-special-chars-${timestamp}@test.com`,
      password: 'Test123!',
      firstName: 'SpecialTest',
      lastName: `${timestamp}`,
    });

    await testProfileUpdatePersistence(page, {
      userEmail: testUser.email,
      userPassword: 'Test123!',
      updatedFields: {
        firstName: `O'Brien-${timestamp}`,
        lastName: `Müller & Sons`,
        bio: `Bio with "quotes", 'apostrophes', & ampersands, (parentheses), and émojis 🎉`,
        discordName: `user#1234`,
        fetLifeName: `fetlife_user-${timestamp}`,
      },
    });
  });

  test('should handle null vs empty string correctly', async ({ page, df }) => {
    const timestamp = Date.now();

    const testUser = await df.users.createVerified({
      email: `profile-null-empty-${timestamp}@test.com`,
      password: 'Test123!',
      firstName: 'NullTest',
      lastName: `${timestamp}`,
    });

    // First set to values
    await testProfileUpdatePersistence(page, {
      userEmail: testUser.email,
      userPassword: 'Test123!',
      updatedFields: {
        bio: `Has bio ${timestamp}`,
        discordName: `has_discord`,
      },
    });

    // Then clear to empty string
    await testProfileUpdatePersistence(page, {
      userEmail: testUser.email,
      userPassword: 'Test123!',
      updatedFields: {
        bio: '',
        discordName: '',
      },
    });
  });

  test('should handle rapid successive updates', async ({ page, df }) => {
    // Test that multiple rapid updates don't cause race conditions
    const timestamp = Date.now();

    const testUser = await df.users.createVerified({
      email: `profile-rapid-${timestamp}@test.com`,
      password: 'Test123!',
      firstName: 'RapidTest',
      lastName: `${timestamp}`,
    });

    for (let i = 0; i < 3; i++) {
      await testSingleFieldUpdate(
        page,
        testUser.email,
        'Test123!',
        'bio',
        `Rapid update #${i} at ${timestamp}`
      );
    }
  });
});
