import { test, expect } from '@playwright/test';
import { AuthHelper } from './test-utils/helpers/auth.helper';

test.describe('Admin Events - Comprehensive Bug Testing', () => {
  test.beforeEach(async ({ page }) => {
    // Monitor console errors
    page.on('console', msg => {
      if (msg.type() === 'error') {
        console.error(`Console error: ${msg.text()}`);
      }
    });

    // Login as admin using the proper auth helper
    const success = await AuthHelper.loginAs(page, 'admin');
    if (!success) {
      throw new Error('Failed to login as admin');
    }
    await page.goto('/admin/events');
  });

  test.describe('Environment and Login Validation', () => {
    test('admin can access events page', async ({ page }) => {
      // Verify we're on the admin events page
      await expect(page).toHaveURL(/.*\/admin\/events/);
      
      // Check for key admin elements
      const createButton = page.locator('[data-testid="button-create-event"]');
      await expect(createButton).toBeVisible({ timeout: 10000 });
      
      console.log('✅ Admin events page loaded and accessible');
    });

    test('admin login credentials work', async ({ page }) => {
      // Verify admin authentication
      const isAuth = await AuthHelper.isAuthenticated(page);
      expect(isAuth).toBeTruthy();
      
      // Check admin role indicators if available  
      const userInfo = await AuthHelper.getCurrentUserInfo(page);
      console.log('Current user info:', userInfo);
      
      console.log('✅ Admin authentication validated');
    });
  });

  test.describe('Basic Event Management UI Tests', () => {
    test('create event navigates to new event page', async ({ page }) => {
      // Test page navigation (NOT modal)
      await page.click('[data-testid="button-create-event"]');

      // Should navigate to /admin/events/new (page navigation, not modal)
      await page.waitForURL('**/admin/events/new');
      expect(page.url()).toContain('/admin/events/new');

      // Look for event form on the new page
      const form = page.locator('[data-testid="event-form"]');
      await expect(form).toBeVisible();

      console.log('✅ Event creation page navigation working');
    });

    test('event form has required fields', async ({ page }) => {
      // Open event creation
      await page.click('[data-testid="button-create-event"]');
      await page.waitForURL('**/admin/events/new');

      // Check for key form fields using label-based selectors (Mantine pattern)
      // Use .first() to handle multiple matches from Mantine's structure
      // Event Title field
      const titleField = page.getByLabel('Event Title').first();
      await expect(titleField).toBeVisible({ timeout: 5000 });

      // Short Description field
      const shortDescField = page.getByLabel(/Short Description/i).first();
      await expect(shortDescField).toBeVisible({ timeout: 5000 });

      // Venue dropdown
      const venueField = page.getByLabel('Venue').first();
      await expect(venueField).toBeVisible({ timeout: 5000 });

      console.log('✅ Basic event form fields are present');
    });

    test('event form can be filled', async ({ page }) => {
      // Open event creation
      await page.click('[data-testid="button-create-event"]');
      await page.waitForURL('**/admin/events/new');

      // Fill Event Title field using label-based selector
      const titleField = page.getByLabel('Event Title');
      await titleField.fill('Test Event Name');
      const value = await titleField.inputValue();
      expect(value).toBe('Test Event Name');

      console.log('✅ Event form fields can be filled');
    });

    test('events list displays', async ({ page }) => {
      // Check if events are displayed in some format
      const eventsList = page.locator('[data-testid="events-list"], .events-table, .event-card, table');
      const eventsContainer = page.locator('[data-testid="events-container"], .events-grid');

      // Hard assertion - at least one of these containers must be visible
      const hasEventsList = await eventsList.first().isVisible().catch(() => false);
      const hasEventsContainer = await eventsContainer.first().isVisible().catch(() => false);

      expect(hasEventsList || hasEventsContainer).toBeTruthy();
      console.log('✅ Events display container found');
    });
  });

  test.describe('Tab and Modal Structure Tests', () => {
    test('event management has tabbed interface', async ({ page }) => {
      // Try to create/edit an event to access tabs
      const createBtn = page.locator('[data-testid="button-create-event"]');
      const editBtn = page.locator('[data-testid="edit-event-button"]').first();

      // Hard assertion - at least one button must be visible
      const createVisible = await createBtn.isVisible().catch(() => false);
      const editVisible = await editBtn.isVisible().catch(() => false);
      expect(createVisible || editVisible).toBeTruthy();

      // Click the visible button
      if (createVisible) {
        await createBtn.click();
      } else {
        await editBtn.click();
      }

      // Look for tab structure - hard assertion that tabs exist
      const tabs = page.locator('[data-testid*="tab"], .tab, [role="tab"]');
      const tabCount = await tabs.count();

      expect(tabCount).toBeGreaterThan(0);
      console.log(`✅ Found ${tabCount} tabs in event management interface`);
    });

    test('session management section exists', async ({ page }) => {
      // Look for session-related elements
      const createBtn = page.locator('[data-testid="button-create-event"]');

      // Hard assertion - button must be visible
      await expect(createBtn).toBeVisible();
      await createBtn.click();

      // Hard assertion - session elements must exist
      const sessionElements = page.locator('[data-testid*="session"], *:has-text("Session"), *:has-text("Time Slot")');
      const sessionCount = await sessionElements.count();

      expect(sessionCount).toBeGreaterThan(0);
      console.log('✅ Session management elements found');
    });

    test('ticket management section exists', async ({ page }) => {
      // Look for ticket-related elements
      const createBtn = page.locator('[data-testid="button-create-event"]');

      // Hard assertion - button must be visible
      await expect(createBtn).toBeVisible();
      await createBtn.click();

      // Hard assertion - ticket elements must exist
      const ticketElements = page.locator('[data-testid*="ticket"], *:has-text("Ticket"), *:has-text("Price")');
      const ticketCount = await ticketElements.count();

      expect(ticketCount).toBeGreaterThan(0);
      console.log('✅ Ticket management elements found');
    });
  });

  test.describe('Basic Data Persistence Tests', () => {
    test('page refresh maintains authentication', async ({ page }) => {
      // Verify current auth
      const authBefore = await AuthHelper.isAuthenticated(page);
      expect(authBefore).toBeTruthy();

      // Refresh page
      await page.reload();
      await page.waitForLoadState('networkidle');

      // Hard assertion - authentication must persist
      const authAfter = await AuthHelper.isAuthenticated(page);
      expect(authAfter).toBeTruthy();
      console.log('✅ Authentication persists after page refresh');
    });

    test('admin events page remains accessible after refresh', async ({ page }) => {
      // Refresh and verify access
      await page.reload();
      await page.waitForLoadState('networkidle');

      // Hard assertion - must remain on admin events page
      await expect(page).toHaveURL(/.*\/admin\/events/);
      console.log('✅ Admin events page remains accessible after refresh');
    });
  });

  test.describe('Critical Event Form Fields', () => {
    test('event title field exists and works', async ({ page }) => {
      await page.click('[data-testid="button-create-event"]');
      await page.waitForURL('**/admin/events/new');

      // Use label-based selector
      const titleField = page.getByLabel('Event Title');
      await titleField.fill('Test Event');
      const value = await titleField.inputValue();
      expect(value).toBe('Test Event');

      console.log('✅ Event title field works correctly');
    });

    test('short description field exists', async ({ page }) => {
      await page.click('[data-testid="button-create-event"]');
      await page.waitForURL('**/admin/events/new');

      // Use label-based selector (Mantine pattern)
      // Use .first() to handle multiple matches
      const shortDescField = page.getByLabel(/Short Description/i).first();
      await expect(shortDescField).toBeVisible();

      console.log('✅ Short description field is present');
    });

    test('venue selection exists', async ({ page }) => {
      await page.click('[data-testid="button-create-event"]');
      await page.waitForURL('**/admin/events/new');

      // Use label-based selector for Mantine Select
      // Use .first() to handle multiple matches
      const venueField = page.getByLabel('Venue').first();
      await expect(venueField).toBeVisible();

      console.log('✅ Venue selection field found');
    });

    test('teacher selection exists', async ({ page }) => {
      await page.click('[data-testid="button-create-event"]');
      await page.waitForURL('**/admin/events/new');

      // Use label-based selector for Mantine MultiSelect
      // Use .first() to handle multiple matches (Mantine creates input + listbox)
      const teacherField = page.getByLabel('Select Teachers').first();
      await expect(teacherField).toBeVisible();

      console.log('✅ Teacher selection field found');
    });
  });

  test.describe('Performance and UI Health', () => {
    test('page loads within reasonable time', async ({ page }) => {
      const startTime = Date.now();
      await page.goto('/admin/events');
      await page.waitForLoadState('networkidle');
      const loadTime = Date.now() - startTime;

      console.log(`Page load time: ${loadTime}ms`);

      // Hard assertion - page must load within reasonable time
      expect(loadTime).toBeLessThan(30000);

      // Additional informational check
      if (loadTime < 5000) {
        console.log('✅ Page loads quickly');
      } else {
        console.log('⚠️  Page load time is slow but within acceptable limits');
      }
    });

    test('no JavaScript errors during basic navigation', async ({ page }) => {
      const errors: string[] = [];

      page.on('console', msg => {
        if (msg.type() === 'error') {
          errors.push(msg.text());
        }
      });

      page.on('pageerror', error => {
        errors.push(error.toString());
      });

      // Navigate through basic admin flow
      await page.goto('/admin/events');
      await page.waitForLoadState('networkidle');

      const createBtn = page.locator('[data-testid="button-create-event"]');

      // Hard assertion - create button must be visible
      await expect(createBtn).toBeVisible();
      await createBtn.click();
      await page.waitForTimeout(2000); // Give time for any errors to surface

      // Hard assertion - no JavaScript errors should occur
      expect(errors.length).toBe(0);
      if (errors.length > 0) {
        console.log(`JavaScript errors detected: ${errors.length}`);
        errors.forEach(error => console.log(`  - ${error}`));
      }
      console.log('✅ No JavaScript errors detected during navigation');
    });
  });
});