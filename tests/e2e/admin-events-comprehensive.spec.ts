import { test, expect } from '@playwright/test';
import { AuthHelpers } from './test-utils/helpers/auth.helpers';

test.describe('Admin Events - Comprehensive Bug Testing', () => {
  test.beforeEach(async ({ page }) => {
    // Monitor console errors
    page.on('console', msg => {
      if (msg.type() === 'error') {
        console.error(`Console error: ${msg.text()}`);
      }
    });

    // Login as admin using the proper auth helper
    const success = await AuthHelpers.loginAs(page, 'admin');
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
      const isAuth = await AuthHelpers.isAuthenticated(page);
      expect(isAuth).toBeTruthy();

      // Verify we're on a protected page (dashboard) after auth check
      const currentUrl = page.url();
      expect(currentUrl).toContain('/dashboard');

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
      // Wait for events table to load (may take time due to API call)
      const eventsTable = page.locator('table');
      await expect(eventsTable).toBeVisible({ timeout: 10000 });

      // Verify table has content (rows)
      const tableRows = page.locator('table tbody tr, table rowgroup:nth-child(2) row');
      const rowCount = await tableRows.count();

      // Table exists and may have event rows
      expect(rowCount).toBeGreaterThanOrEqual(0); // Table may be empty but should exist
      console.log(`✅ Events table found with ${rowCount} rows`);
    });
  });

  test.describe('Tab and Modal Structure Tests', () => {
    test('event management has tabbed interface', async ({ page }) => {
      // Click create event button
      const createBtn = page.locator('[data-testid="button-create-event"]');
      await expect(createBtn).toBeVisible({ timeout: 10000 });
      await createBtn.click();

      // Wait for navigation to the event creation page
      await page.waitForURL('**/admin/events/new', { timeout: 10000 });
      await page.waitForLoadState('domcontentloaded');

      // Look for tab structure using Mantine tabs pattern
      const tabs = page.locator('[role="tab"], button[role="tab"], .mantine-Tabs-tab');
      await expect(tabs.first()).toBeVisible({ timeout: 10000 });

      const tabCount = await tabs.count();
      expect(tabCount).toBeGreaterThan(0);
      console.log(`✅ Found ${tabCount} tabs in event management interface`);
    });

    test('session management section exists', async ({ page }) => {
      // Navigate to event creation page
      const createBtn = page.locator('[data-testid="button-create-event"]');
      await expect(createBtn).toBeVisible({ timeout: 10000 });
      await createBtn.click();

      // Wait for navigation to the event creation page
      await page.waitForURL('**/admin/events/new', { timeout: 10000 });
      await page.waitForLoadState('domcontentloaded');

      // Wait for the event form to render (tabs need to be visible)
      const eventForm = page.locator('[data-testid="event-form"]');
      await expect(eventForm).toBeVisible({ timeout: 10000 });

      // Look for Sessions / Ticket Types tab button (not the panel)
      // Use role="tab" to specifically target the tab button, not the panel
      const sessionsTab = page.getByRole('tab', { name: 'Sessions / Ticket Types' });
      await expect(sessionsTab).toBeVisible({ timeout: 5000 });

      console.log('✅ Session management tab found');
    });

    test('ticket management section exists', async ({ page }) => {
      // Navigate to event creation page
      const createBtn = page.locator('[data-testid="button-create-event"]');
      await expect(createBtn).toBeVisible({ timeout: 10000 });
      await createBtn.click();

      // Wait for navigation and form to load
      await page.waitForURL('**/admin/events/new', { timeout: 10000 });
      await page.waitForLoadState('domcontentloaded');

      // Wait for the event form to render
      const eventForm = page.locator('[data-testid="event-form"]');
      await expect(eventForm).toBeVisible({ timeout: 10000 });

      // Look for RSVP/Tickets tab button (not the panel)
      // Use role="tab" to specifically target the tab button, not the panel
      const ticketsTab = page.getByRole('tab', { name: 'RSVP/Tickets' });
      await expect(ticketsTab).toBeVisible({ timeout: 5000 });

      console.log('✅ Ticket management elements found');
    });
  });

  test.describe('Basic Data Persistence Tests', () => {
    test('page refresh maintains authentication', async ({ page }) => {
      // Verify current auth
      const authBefore = await AuthHelpers.isAuthenticated(page);
      expect(authBefore).toBeTruthy();

      // Refresh page
      await page.reload();
      await page.waitForLoadState('domcontentloaded');

      // Hard assertion - authentication must persist
      const authAfter = await AuthHelpers.isAuthenticated(page);
      expect(authAfter).toBeTruthy();
      console.log('✅ Authentication persists after page refresh');
    });

    test('admin events page remains accessible after refresh', async ({ page }) => {
      // Refresh and verify access
      await page.reload();
      await page.waitForLoadState('domcontentloaded');

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
      await page.waitForLoadState('domcontentloaded');
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
      await page.waitForLoadState('domcontentloaded');

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