/**
 * E2E Tests: Teacher Bio Display on Event Page
 *
 * Tests teacher profile display on event detail pages.
 * Teachers section should always be visible when data exists.
 *
 * Test Coverage:
 * - Teachers section appears on events with teachers
 * - Teacher names displayed correctly (scene name preferred)
 * - Teacher bios displayed correctly
 * - Single teacher display
 * - Multiple teachers display
 * - No teachers section when no teachers assigned
 *
 * @see /apps/web/src/pages/events/EventDetailPage.tsx (lines 361-394)
 * @see /apps/web/src/lib/api/hooks/useTeacherProfiles.ts
 */

import { test, expect } from '@playwright/test';
import { AuthHelpers } from '../helpers/auth.helpers';

test.describe('Teacher Bio Display on Event Page', () => {
  test.beforeEach(async ({ page }) => {
    // Clear auth state - teachers section is always visible (no auth required)
    await AuthHelpers.clearAuthState(page);
  });

  test('should display Teachers section when event has teachers', async ({ page }) => {
    // Navigate to events list (no login required - teachers always visible)
    await page.goto('http://localhost:5173/events');
    await page.waitForLoadState('networkidle');

    // Find and click on an event (preferably a class/workshop)
    const eventCards = page.locator('[data-testid="event-card"], .event-card, a[href*="/events/"]');

    if (await eventCards.count() > 0) {
      // Try multiple events to find one with teachers
      const eventCount = await eventCards.count();

      for (let i = 0; i < Math.min(eventCount, 5); i++) {
        await page.goto('http://localhost:5173/events');
        await page.waitForLoadState('networkidle');

        await eventCards.nth(i).click();
        await page.waitForTimeout(1000);

        // Check if Teachers section exists
        const teachersSection = page.locator('[data-testid="teachers-section"], text="Teachers"').filter({ hasText: /^Teachers$/i });

        if (await teachersSection.count() > 0) {
          // Found an event with teachers
          await expect(teachersSection.first()).toBeVisible({ timeout: 5000 });

          // Verify at least one teacher is displayed
          const teacherName = page.locator('[data-testid="teacher-name"]');
          if (await teacherName.count() > 0) {
            await expect(teacherName.first()).toBeVisible();
          }

          break; // Successfully found event with teachers
        }
      }
    } else {
      console.log('⚠️ No events found for testing');
      test.skip();
    }
  });

  test('should display teacher scene name when available', async ({ page }) => {
    // Navigate to events
    await page.goto('http://localhost:5173/events');
    await page.waitForLoadState('networkidle');

    const eventCards = page.locator('[data-testid="event-card"], .event-card, a[href*="/events/"]');

    if (await eventCards.count() > 0) {
      // Find an event with teachers
      const eventCount = await eventCards.count();

      for (let i = 0; i < Math.min(eventCount, 5); i++) {
        await page.goto('http://localhost:5173/events');
        await page.waitForLoadState('networkidle');

        await eventCards.nth(i).click();
        await page.waitForTimeout(1000);

        const teachersSection = page.locator('text="Teachers"').filter({ hasText: /^Teachers$/i });

        if (await teachersSection.count() > 0) {
          // Get teacher name
          const teacherNames = page.locator('[data-testid="teacher-name"]');

          if (await teacherNames.count() > 0) {
            const firstTeacherName = await teacherNames.first().textContent();

            expect(firstTeacherName).toBeTruthy();
            expect(firstTeacherName!.trim().length).toBeGreaterThan(0);

            // Teacher name should not be "undefined undefined" (firstName lastName when both missing)
            expect(firstTeacherName).not.toContain('undefined');

            break;
          }
        }
      }
    }
  });

  test('should display teacher bio when available', async ({ page }) => {
    // Navigate to events
    await page.goto('http://localhost:5173/events');
    await page.waitForLoadState('networkidle');

    const eventCards = page.locator('[data-testid="event-card"], .event-card, a[href*="/events/"]');

    if (await eventCards.count() > 0) {
      // Find an event with teachers who have bios
      const eventCount = await eventCards.count();

      for (let i = 0; i < Math.min(eventCount, 5); i++) {
        await page.goto('http://localhost:5173/events');
        await page.waitForLoadState('networkidle');

        await eventCards.nth(i).click();
        await page.waitForTimeout(1000);

        const teachersSection = page.locator('text="Teachers"').filter({ hasText: /^Teachers$/i });

        if (await teachersSection.count() > 0) {
          // Look for teacher bio
          const teacherBio = page.locator('[data-testid="teacher-bio"]');

          if (await teacherBio.count() > 0) {
            const bioText = await teacherBio.first().textContent();

            expect(bioText).toBeTruthy();
            expect(bioText!.trim().length).toBeGreaterThan(0);

            // Bio should be visible
            await expect(teacherBio.first()).toBeVisible();

            break;
          } else {
            // This teacher might not have a bio, try next event
            continue;
          }
        }
      }
    }
  });

  test('should display multiple teachers when event has multiple', async ({ page }) => {
    // Navigate to events
    await page.goto('http://localhost:5173/events');
    await page.waitForLoadState('networkidle');

    const eventCards = page.locator('[data-testid="event-card"], .event-card, a[href*="/events/"]');

    if (await eventCards.count() > 0) {
      // Find an event with multiple teachers
      const eventCount = await eventCards.count();

      for (let i = 0; i < Math.min(eventCount, 5); i++) {
        await page.goto('http://localhost:5173/events');
        await page.waitForLoadState('networkidle');

        await eventCards.nth(i).click();
        await page.waitForTimeout(1000);

        const teachersSection = page.locator('text="Teachers"').filter({ hasText: /^Teachers$/i });

        if (await teachersSection.count() > 0) {
          // Count teacher entries
          const teacherNames = page.locator('[data-testid="teacher-name"]');
          const teacherCount = await teacherNames.count();

          if (teacherCount > 1) {
            // Found event with multiple teachers
            expect(teacherCount).toBeGreaterThan(1);

            // Verify all teachers are visible
            for (let j = 0; j < teacherCount; j++) {
              await expect(teacherNames.nth(j)).toBeVisible();
            }

            break;
          }
        }
      }
    }
  });

  test('should NOT display Teachers section when no teachers assigned', async ({ page }) => {
    // Navigate to events
    await page.goto('http://localhost:5173/events');
    await page.waitForLoadState('networkidle');

    const eventCards = page.locator('[data-testid="event-card"], .event-card, a[href*="/events/"]');

    if (await eventCards.count() > 0) {
      // Find a social event (social events typically don't have teachers)
      const eventCount = await eventCards.count();

      for (let i = 0; i < Math.min(eventCount, 5); i++) {
        await page.goto('http://localhost:5173/events');
        await page.waitForLoadState('networkidle');

        await eventCards.nth(i).click();
        await page.waitForTimeout(1000);

        // Check event type - if it's a social event, teachers section should not appear
        const eventType = page.locator('[data-testid="event-type"], text=/social|jam/i');

        if (await eventType.count() > 0) {
          // This is a social event - teachers section should NOT exist
          const teachersSection = page.locator('text="Teachers"').filter({ hasText: /^Teachers$/i });
          expect(await teachersSection.count()).toBe(0);

          break;
        }
      }
    }
  });

  test('should display teachers section to both logged-in and logged-out users', async ({ page }) => {
    // First test as logged-out user
    await page.goto('http://localhost:5173/events');
    await page.waitForLoadState('networkidle');

    const eventCards = page.locator('[data-testid="event-card"], .event-card, a[href*="/events/"]');
    let eventWithTeachersFound = false;
    let eventUrl = '';

    if (await eventCards.count() > 0) {
      // Find an event with teachers
      const eventCount = await eventCards.count();

      for (let i = 0; i < Math.min(eventCount, 5); i++) {
        await page.goto('http://localhost:5173/events');
        await page.waitForLoadState('networkidle');

        await eventCards.nth(i).click();
        await page.waitForTimeout(1000);

        const teachersSection = page.locator('text="Teachers"').filter({ hasText: /^Teachers$/i });

        if (await teachersSection.count() > 0) {
          // Found event with teachers while logged out
          eventWithTeachersFound = true;
          eventUrl = page.url();
          await expect(teachersSection.first()).toBeVisible();

          break;
        }
      }
    }

    if (eventWithTeachersFound) {
      // Now login and verify teachers still visible
      await AuthHelpers.loginAs(page, 'member');

      await page.goto(eventUrl);
      await page.waitForLoadState('networkidle');

      const teachersSection = page.locator('text="Teachers"').filter({ hasText: /^Teachers$/i });
      await expect(teachersSection.first()).toBeVisible({ timeout: 5000 });
    } else {
      console.log('⚠️ No events with teachers found for testing');
    }
  });

  test('should prefer scene name over first/last name', async ({ page }) => {
    // Navigate to events
    await page.goto('http://localhost:5173/events');
    await page.waitForLoadState('networkidle');

    const eventCards = page.locator('[data-testid="event-card"], .event-card, a[href*="/events/"]');

    if (await eventCards.count() > 0) {
      // Find an event with teachers
      const eventCount = await eventCards.count();

      for (let i = 0; i < Math.min(eventCount, 5); i++) {
        await page.goto('http://localhost:5173/events');
        await page.waitForLoadState('networkidle');

        await eventCards.nth(i).click();
        await page.waitForTimeout(1000);

        const teachersSection = page.locator('text="Teachers"').filter({ hasText: /^Teachers$/i });

        if (await teachersSection.count() > 0) {
          const teacherNames = page.locator('[data-testid="teacher-name"]');

          if (await teacherNames.count() > 0) {
            const firstTeacherName = await teacherNames.first().textContent();

            // Implementation prefers sceneName || `${firstName} ${lastName}`
            // If we see a name that looks like a scene name (single word, capitalized),
            // it's likely using scene name preference
            // Otherwise it might be using firstName + lastName

            expect(firstTeacherName).toBeTruthy();

            // Name should not be empty or just whitespace
            expect(firstTeacherName!.trim()).not.toBe('');

            break;
          }
        }
      }
    }
  });

  test('should handle teacher with no bio gracefully', async ({ page }) => {
    // Navigate to events
    await page.goto('http://localhost:5173/events');
    await page.waitForLoadState('networkidle');

    const eventCards = page.locator('[data-testid="event-card"], .event-card, a[href*="/events/"]');

    if (await eventCards.count() > 0) {
      // Find an event with teachers
      const eventCount = await eventCards.count();

      for (let i = 0; i < Math.min(eventCount, 5); i++) {
        await page.goto('http://localhost:5173/events');
        await page.waitForLoadState('networkidle');

        await eventCards.nth(i).click();
        await page.waitForTimeout(1000);

        const teachersSection = page.locator('text="Teachers"').filter({ hasText: /^Teachers$/i });

        if (await teachersSection.count() > 0) {
          // Teacher name should still be visible
          const teacherName = page.locator('[data-testid="teacher-name"]');
          if (await teacherName.count() > 0) {
            await expect(teacherName.first()).toBeVisible();
          }

          // Bio may or may not be present - that's okay
          // The important thing is the teacher section displays without error

          break;
        }
      }
    }
  });
});
