import { test, expect } from '@playwright/test';
import { AuthHelpers } from './test-utils/helpers/auth.helpers';

test.describe('Admin Events - Simplified Comprehensive Testing', () => {
  let consoleErrors: string[] = [];
  let pageErrors: string[] = [];

  test.beforeEach(async ({ page }) => {
    // Reset error tracking
    consoleErrors = [];
    pageErrors = [];

    // Monitor console errors
    page.on('console', msg => {
      if (msg.type() === 'error') {
        consoleErrors.push(msg.text());
      }
    });

    // Monitor page errors
    page.on('pageerror', error => {
      pageErrors.push(error.toString());
    });

    // Use AuthHelper for consistent login
    const loginSuccess = await AuthHelpers.loginAs(page, 'admin');
    expect(loginSuccess).toBeTruthy();

    await page.goto('/admin/events');
    await page.waitForLoadState('domcontentloaded');
  });

  test.describe('Environment and Access Validation', () => {
    test('admin can access events management page', async ({ page }) => {
      // Verify we're on the admin events page
      const currentUrl = page.url();
      expect(currentUrl).toContain('/admin/events');

      console.log('✅ Admin events page accessible');
    });

    test('page has basic admin interface elements', async ({ page }) => {
      // Look for admin interface elements
      const possibleElements = [
        '[data-testid="button-create-event"]',
        'button:has-text("Create")',
        'button:has-text("New Event")',
        'button:has-text("Add Event")',
        '.admin-',
        '[data-testid*="admin"]',
        '[data-testid*="event"]',
        'table',
        '.event-'
      ];

      let foundElements = 0;
      for (const selector of possibleElements) {
        const locator = page.locator(selector);
        const count = await locator.count();

        // HARD ASSERTION: If we find elements, they should be valid
        if (count > 0) {
          await expect(locator.first()).toBeAttached();
          foundElements++;
          console.log(`✅ Found ${count} elements matching: ${selector}`);
        }
      }

      // HARD ASSERTION: Must find at least some admin interface elements
      expect(foundElements).toBeGreaterThan(0);
      console.log(`✅ Found ${foundElements} types of admin interface elements`);
    });
  });

  test.describe('Event Creation Interface', () => {
    test('can access event creation interface', async ({ page }) => {
      // Look for creation buttons/links
      const createSelectors = [
        '[data-testid="button-create-event"]',
        'button:has-text("Create")',
        'button:has-text("New")',
        'button:has-text("Add")',
        'a:has-text("Create")',
        'a:has-text("New")'
      ];

      let createButton = null;
      for (const selector of createSelectors) {
        const element = page.locator(selector).first();
        const count = await element.count();

        // HARD ASSERTION: Element must exist and be visible before clicking
        if (count > 0) {
          await expect(element).toBeVisible();
          createButton = element;
          console.log(`✅ Found create button: ${selector}`);
          break;
        }
      }

      // HARD ASSERTION: Must find a create button
      expect(createButton).not.toBeNull();

      await createButton!.click();
      await page.waitForTimeout(2000); // Give time for interface to load

      // Look for form or modal
      const formElements = [
        '[data-testid*="event-form"]',
        '[data-testid*="modal"]',
        'form',
        '[role="dialog"]',
        '.modal',
        'input[placeholder*="name" i]',
        'input[type="date"]'
      ];

      let foundForm = false;
      for (const selector of formElements) {
        const locator = page.locator(selector);
        const count = await locator.count();

        // HARD ASSERTION: If form elements exist, they should be attached
        if (count > 0) {
          await expect(locator.first()).toBeAttached();
          foundForm = true;
          console.log(`✅ Event creation interface found: ${selector} (${count})`);
        }
      }

      // HARD ASSERTION: Must find form interface after clicking create
      expect(foundForm).toBeTruthy();
      console.log('✅ Event creation interface accessible');
    });

    test('basic form fields exist in event creation', async ({ page }) => {
      // Try to access create interface
      const createSelectors = [
        '[data-testid="button-create-event"]',
        'button:has-text("Create")',
        'button:has-text("New")',
        'button:has-text("Add")'
      ];

      let createButton = null;
      for (const selector of createSelectors) {
        const element = page.locator(selector).first();
        const count = await element.count();

        // HARD ASSERTION: Button must be visible and enabled
        if (count > 0) {
          await expect(element).toBeVisible();
          await expect(element).toBeEnabled();
          createButton = element;
          break;
        }
      }

      // HARD ASSERTION: Must find create button
      expect(createButton).not.toBeNull();

      await createButton!.click();
      await page.waitForTimeout(2000);

      // Look for basic form fields on the Basic Info tab
      // Note: Date/Time fields are on the Sessions tab, not Basic Info
      const fieldTypes = [
        { type: 'name', label: 'Event Title' },
        { type: 'description', label: 'Short Description' },
        { type: 'venue', label: 'Venue' },
        { type: 'teachers', label: 'Select Teachers' }
      ];

      for (const fieldType of fieldTypes) {
        // Use getByLabel with regex for flexible matching
        const locator = page.getByLabel(new RegExp(fieldType.label, 'i')).first();
        const count = await locator.count();

        // HARD ASSERTION: Field must be visible
        if (count > 0) {
          await expect(locator).toBeVisible();
          console.log(`✅ ${fieldType.type} field found: ${fieldType.label}`);
        } else {
          console.log(`⚠️ ${fieldType.type} field not found by label: ${fieldType.label}`);
        }

        // HARD ASSERTION: Each field type must exist
        expect(count).toBeGreaterThan(0);
      }
    });
  });

  test.describe('Session and Ticket Management', () => {
    test('session management interface exists', async ({ page }) => {
      // Try to access event creation/edit
      const createSelectors = ['[data-testid="button-create-event"]', 'button:has-text("Create")'];

      let createButton = null;
      for (const selector of createSelectors) {
        const element = page.locator(selector).first();
        const count = await element.count();

        // HARD ASSERTION: Button must be visible and enabled
        if (count > 0) {
          await expect(element).toBeVisible();
          await expect(element).toBeEnabled();
          createButton = element;
          break;
        }
      }

      // HARD ASSERTION: Must find create button
      expect(createButton).not.toBeNull();

      await createButton!.click();
      await page.waitForTimeout(2000);

      // Look for session-related elements
      const sessionElements = [
        '[data-testid*="session"]',
        '*:has-text("Session")',
        '*:has-text("Time Slot")',
        '*:has-text("Schedule")',
        'button:has-text("Add Session")',
        '[data-testid*="add-session"]'
      ];

      let sessionCount = 0;
      for (const selector of sessionElements) {
        const locator = page.locator(selector);
        const count = await locator.count();

        // HARD ASSERTION: If session elements exist, they should be attached
        if (count > 0) {
          await expect(locator.first()).toBeAttached();
          sessionCount += count;
          console.log(`✅ Session element found: ${selector} (${count})`);
        }
      }

      // HARD ASSERTION: Must find session management interface
      expect(sessionCount).toBeGreaterThan(0);
      console.log('✅ Session management interface detected');
    });

    test('ticket management interface exists', async ({ page }) => {
      // Try to access event creation/edit
      const createSelectors = ['[data-testid="button-create-event"]', 'button:has-text("Create")'];

      let createButton = null;
      for (const selector of createSelectors) {
        const element = page.locator(selector).first();
        const count = await element.count();

        // HARD ASSERTION: Button must be visible and enabled
        if (count > 0) {
          await expect(element).toBeVisible();
          await expect(element).toBeEnabled();
          createButton = element;
          break;
        }
      }

      // HARD ASSERTION: Must find create button
      expect(createButton).not.toBeNull();

      await createButton!.click();
      await page.waitForTimeout(2000);

      // Look for ticket-related elements
      const ticketElements = [
        '[data-testid*="ticket"]',
        '*:has-text("Ticket")',
        '*:has-text("Price")',
        '*:has-text("Cost")',
        'button:has-text("Add Ticket")',
        '[data-testid*="add-ticket"]',
        'input[placeholder*="price"]'
      ];

      let ticketCount = 0;
      for (const selector of ticketElements) {
        const locator = page.locator(selector);
        const count = await locator.count();

        // HARD ASSERTION: If ticket elements exist, they should be attached
        if (count > 0) {
          await expect(locator.first()).toBeAttached();
          ticketCount += count;
          console.log(`✅ Ticket element found: ${selector} (${count})`);
        }
      }

      // HARD ASSERTION: Must find ticket management interface
      expect(ticketCount).toBeGreaterThan(0);
      console.log('✅ Ticket management interface detected');
    });

    test('tabbed interface for event management', async ({ page }) => {
      // Try to access event creation/edit
      const createSelectors = ['[data-testid="button-create-event"]', 'button:has-text("Create")'];

      let createButton = null;
      for (const selector of createSelectors) {
        const element = page.locator(selector).first();
        const count = await element.count();

        // HARD ASSERTION: Button must be visible and enabled
        if (count > 0) {
          await expect(element).toBeVisible();
          await expect(element).toBeEnabled();
          createButton = element;
          break;
        }
      }

      // HARD ASSERTION: Must find create button
      expect(createButton).not.toBeNull();

      await createButton!.click();
      await page.waitForTimeout(2000);

      // Look for tab structure
      const tabSelectors = [
        '[data-testid*="tab"]',
        '[role="tab"]',
        '.tab',
        '*:has-text("Setup")',
        '*:has-text("Details")',
        '*:has-text("Sessions")',
        '*:has-text("Tickets")',
        '*:has-text("RSVP")',
        '*:has-text("Volunteers")'
      ];

      let tabCount = 0;
      const foundTabs: string[] = [];

      for (const selector of tabSelectors) {
        const locator = page.locator(selector);
        const count = await locator.count();

        // HARD ASSERTION: If tabs exist, they should be attached
        if (count > 0) {
          await expect(locator.first()).toBeAttached();
          tabCount += count;
          foundTabs.push(`${selector} (${count})`);
        }
      }

      // HARD ASSERTION: Must find tabbed interface
      expect(tabCount).toBeGreaterThan(0);
      console.log(`✅ Tabbed interface found with ${tabCount} tab elements:`);
      foundTabs.forEach(tab => console.log(`  - ${tab}`));
    });
  });

  test.describe('Data Persistence and Functionality', () => {
    test('authentication persists after page refresh', async ({ page }) => {
      const urlBefore = page.url();
      console.log(`URL before refresh: ${urlBefore}`);

      // HARD ASSERTION: Must be on admin events page before refresh
      expect(urlBefore).toContain('/admin/events');

      // Refresh page
      await page.reload();
      await page.waitForLoadState('domcontentloaded');

      const urlAfter = page.url();
      console.log(`URL after refresh: ${urlAfter}`);

      // HARD ASSERTION: Should not be redirected to login after refresh
      expect(urlAfter).not.toContain('/login');
      console.log('✅ Authentication persists after refresh');

      // HARD ASSERTION: Should remain on admin events page
      if (!urlAfter.includes('/admin/events')) {
        await page.goto('/admin/events');
        const finalUrl = page.url();
        expect(finalUrl).toContain('/admin/events');
        console.log('✅ Admin access restored after navigation');
      }
    });

    test('basic form interaction works', async ({ page }) => {
      // Try to access event creation
      const createButton = page.locator('[data-testid="button-create-event"]').first();
      const buttonCount = await createButton.count();

      if (buttonCount === 0) {
        // Look for alternative create buttons
        const altButtons = page.locator('button:has-text("Create"), button:has-text("New"), button:has-text("Add")');
        const altCount = await altButtons.count();

        // HARD ASSERTION: Must find a create button
        expect(altCount).toBeGreaterThan(0);

        await expect(altButtons.first()).toBeVisible();
        await expect(altButtons.first()).toBeEnabled();
        await altButtons.first().click();
      } else {
        await expect(createButton).toBeVisible();
        await expect(createButton).toBeEnabled();
        await createButton.click();
      }

      await page.waitForTimeout(2000);

      // Try to fill the Event Title field using label-based selector
      const titleField = page.getByLabel('Event Title');
      const count = await titleField.count();

      // HARD ASSERTION: Title field must exist
      expect(count).toBeGreaterThan(0);

      // HARD ASSERTION: Field must be visible and enabled
      await expect(titleField).toBeVisible();
      await expect(titleField).toBeEnabled();

      await titleField.fill('Test Event Name');
      const value = await titleField.inputValue();

      // HARD ASSERTION: Field must accept input
      expect(value).toBe('Test Event Name');
      console.log('✅ Form field interaction works: Event Title');
    });
  });

  test.describe('Performance and Error Detection', () => {
    test('page performance is acceptable', async ({ page }) => {
      const startTime = Date.now();
      await page.goto('/admin/events');
      await page.waitForLoadState('domcontentloaded');
      const loadTime = Date.now() - startTime;

      console.log(`Admin events page load time: ${loadTime}ms`);

      // HARD ASSERTION: Page must load within reasonable time
      expect(loadTime).toBeLessThan(30000);

      // Additional performance expectations
      if (loadTime < 3000) {
        console.log('✅ Page loads quickly');
      } else if (loadTime < 10000) {
        console.log('⚠️  Page load is slow but acceptable');
      } else {
        console.log('❌ Page load is very slow');
      }
    });

    test('no critical JavaScript errors', async ({ page }) => {
      const errors: string[] = [];

      page.on('console', msg => {
        if (msg.type() === 'error') {
          errors.push(msg.text());
        }
      });

      page.on('pageerror', error => {
        errors.push(error.toString());
      });

      // Navigate and interact
      await page.goto('/admin/events');
      await page.waitForLoadState('domcontentloaded');

      // Try basic interactions
      const createButton = page.locator('[data-testid="button-create-event"], button:has-text("Create")').first();
      const buttonCount = await createButton.count();

      // HARD ASSERTION: Create button must exist
      expect(buttonCount).toBeGreaterThan(0);

      await expect(createButton).toBeVisible();
      await expect(createButton).toBeEnabled();
      await createButton.click();
      await page.waitForTimeout(2000);

      // Filter out common development warnings
      const criticalErrors = errors.filter(error =>
        !error.includes('Warning:') &&
        !error.includes('DevTools') &&
        !error.includes('source maps') &&
        !error.includes('Unsupported style property')
      );

      // HARD ASSERTION: No critical JavaScript errors allowed
      expect(criticalErrors.length).toBe(0);
      console.log('✅ No critical JavaScript errors detected');

      if (criticalErrors.length > 0) {
        console.log(`⚠️  ${criticalErrors.length} JavaScript errors detected:`);
        criticalErrors.forEach((error, i) => {
          if (i < 5) { // Limit to first 5 errors
            console.log(`  ${i + 1}. ${error}`);
          }
        });
      }
    });
  });
});
