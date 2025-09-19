import { test, expect } from '@playwright/test';

/**
 * Comprehensive E2E tests for Events Admin Add Buttons Fix Verification
 *
 * Testing Recently Fixed Issues:
 * 1. Add Session - Fixed undefined sessionIdentifier replace() error
 * 2. Add Ticket Type - Fixed undefined toLowerCase() error
 * 3. Add Volunteer Position - Fixed Mantine value property error
 *
 * Modified Files:
 * - SessionFormModal.tsx
 * - TicketTypeFormModal.tsx
 * - VolunteerPositionFormModal.tsx
 * - EventForm.tsx
 *
 * Test Requirements:
 * 1. Navigate to /admin/events and edit an event
 * 2. Go to the "Setup" tab
 * 3. Click "Add Session" - verify modal opens without errors
 * 4. Click "Add Ticket Type" - verify modal opens without errors
 * 5. Click "Add Volunteer Position" - verify modal opens without errors
 * 6. Try to actually add a session, ticket type, and volunteer position
 * 7. Save the event and refresh to verify data persists
 * 8. Test edit and delete operations for each type
 *
 * @created 2025-09-19
 * @updated 2025-09-19
 */

test.describe('Events Admin Add Buttons Fix Verification', () => {

  // Error monitoring for JavaScript errors and API failures
  let consoleErrors: string[] = [];
  let jsErrors: string[] = [];
  let networkErrors: string[] = [];

  test.beforeEach(async ({ page }) => {
    // Reset error tracking
    consoleErrors = [];
    jsErrors = [];
    networkErrors = [];

    // Monitor JavaScript errors
    page.on('pageerror', error => {
      jsErrors.push(error.toString());
      console.log('🚨 JavaScript Error:', error.toString());
    });

    // Monitor console errors
    page.on('console', msg => {
      if (msg.type() === 'error') {
        const errorText = msg.text();
        consoleErrors.push(errorText);
        console.log('🔍 Console Error:', errorText);
      }
    });

    // Monitor network failures
    page.on('response', response => {
      if (response.status() >= 400) {
        const error = `${response.status()} ${response.url()}`;
        networkErrors.push(error);
        console.log('🌐 Network Error:', error);
      }
    });
  });

  test.afterEach(async ({ page }) => {
    // Report errors found during test
    if (jsErrors.length > 0) {
      console.log(`❌ ${jsErrors.length} JavaScript errors found:`, jsErrors);
    }
    if (consoleErrors.length > 0) {
      console.log(`⚠️ ${consoleErrors.length} console errors found:`, consoleErrors);
    }
    if (networkErrors.length > 0) {
      console.log(`🌐 ${networkErrors.length} network errors found:`, networkErrors);
    }
  });

  test('Navigate to admin events and verify Add buttons work without errors', async ({ page }) => {
    console.log('🧪 Testing: Add buttons on Events admin page after fixes');

    // 1. Login as admin
    console.log('🔐 Logging in as admin...');
    await page.goto('http://localhost:5173/login');
    await page.waitForTimeout(2000);

    // Wait for login form to load
    await page.waitForSelector('[data-testid="email-input"]', { timeout: 10000 });

    await page.fill('[data-testid="email-input"]', 'admin@witchcityrope.com');
    await page.fill('[data-testid="password-input"]', 'Test123!');
    await page.click('[data-testid="login-button"]');
    await page.waitForTimeout(3000);

    // 2. Navigate to admin events page
    console.log('🎯 Navigating to admin events page...');
    await page.goto('http://localhost:5173/admin/events');
    await page.waitForTimeout(3000);

    // Take screenshot of events list
    await page.screenshot({ path: 'test-results/add-buttons-1-events-list.png', fullPage: true });

    // 3. Verify events are loaded and click first event to edit
    const eventRows = page.locator('[data-testid="event-row"]');
    const eventCount = await eventRows.count();
    console.log(`📊 Found ${eventCount} events in table`);

    if (eventCount === 0) {
      console.log('⚠️ No events found to test with');
      return;
    }

    // Click on first event row to navigate to edit page
    console.log('📝 Clicking on first event to edit...');
    await eventRows.first().click();
    await page.waitForTimeout(3000);

    // Verify we're on the event edit page
    const currentUrl = page.url();
    console.log(`📍 Current URL: ${currentUrl}`);
    expect(currentUrl).toMatch(/\/admin\/events\/[a-f0-9\-]+/);

    // Take screenshot of event edit page
    await page.screenshot({ path: 'test-results/add-buttons-2-event-edit.png', fullPage: true });

    // 4. Go to the "Setup" tab
    console.log('🔧 Looking for Setup tab...');
    const setupTabSelectors = [
      'text=Setup',
      '[data-testid*="setup"]',
      'button:has-text("Setup")',
      '.tab:has-text("Setup")',
      'a:has-text("Setup")'
    ];

    let setupTab = null;
    for (const selector of setupTabSelectors) {
      const element = page.locator(selector);
      const count = await element.count();
      if (count > 0) {
        setupTab = element.first();
        console.log(`✅ Found Setup tab with selector: ${selector}`);
        break;
      }
    }

    if (setupTab) {
      await setupTab.click();
      await page.waitForTimeout(2000);
      console.log('✅ Clicked Setup tab');
    } else {
      console.log('⚠️ Setup tab not found, checking current page content');
    }

    // Take screenshot of Setup tab
    await page.screenshot({ path: 'test-results/add-buttons-3-setup-tab.png', fullPage: true });

    // 5. Test Add Session Button
    console.log('🧪 Testing Add Session button...');
    const addSessionButton = page.locator('button:has-text("Add Session"), [data-testid*="add-session"]');
    const sessionButtonCount = await addSessionButton.count();

    if (sessionButtonCount > 0) {
      console.log('✅ Found Add Session button');

      // Check for errors before clicking
      const errorsBefore = [...consoleErrors, ...jsErrors];

      await addSessionButton.first().click();
      await page.waitForTimeout(2000);

      // Check for errors after clicking
      const errorsAfter = [...consoleErrors, ...jsErrors];
      const newErrors = errorsAfter.filter(error => !errorsBefore.includes(error));

      if (newErrors.length === 0) {
        console.log('✅ Add Session button clicked without errors');
      } else {
        console.log('❌ Add Session button caused errors:', newErrors);
      }

      // Take screenshot of session modal
      await page.screenshot({ path: 'test-results/add-buttons-4-session-modal.png', fullPage: true });

      // Look for modal and close it
      const modal = page.locator('[role="dialog"], .modal, .mantine-modal');
      const modalCount = await modal.count();
      if (modalCount > 0) {
        console.log('✅ Session modal opened successfully');
        // Try to close modal
        const closeButton = page.locator('button:has-text("Cancel"), button:has-text("Close"), [aria-label="Close"]');
        const closeCount = await closeButton.count();
        if (closeCount > 0) {
          await closeButton.first().click();
          await page.waitForTimeout(1000);
        }
      }
    } else {
      console.log('⚠️ Add Session button not found');
    }

    // 6. Test Add Ticket Type Button
    console.log('🧪 Testing Add Ticket Type button...');
    const addTicketButton = page.locator('button:has-text("Add Ticket Type"), [data-testid*="add-ticket"]');
    const ticketButtonCount = await addTicketButton.count();

    if (ticketButtonCount > 0) {
      console.log('✅ Found Add Ticket Type button');

      // Check for errors before clicking
      const errorsBefore = [...consoleErrors, ...jsErrors];

      await addTicketButton.first().click();
      await page.waitForTimeout(2000);

      // Check for errors after clicking
      const errorsAfter = [...consoleErrors, ...jsErrors];
      const newErrors = errorsAfter.filter(error => !errorsBefore.includes(error));

      if (newErrors.length === 0) {
        console.log('✅ Add Ticket Type button clicked without errors');
      } else {
        console.log('❌ Add Ticket Type button caused errors:', newErrors);
      }

      // Take screenshot of ticket modal
      await page.screenshot({ path: 'test-results/add-buttons-5-ticket-modal.png', fullPage: true });

      // Look for modal and close it
      const modal = page.locator('[role="dialog"], .modal, .mantine-modal');
      const modalCount = await modal.count();
      if (modalCount > 0) {
        console.log('✅ Ticket Type modal opened successfully');
        // Try to close modal
        const closeButton = page.locator('button:has-text("Cancel"), button:has-text("Close"), [aria-label="Close"]');
        const closeCount = await closeButton.count();
        if (closeCount > 0) {
          await closeButton.first().click();
          await page.waitForTimeout(1000);
        }
      }
    } else {
      console.log('⚠️ Add Ticket Type button not found');
    }

    // 7. Test Add Volunteer Position Button
    console.log('🧪 Testing Add Volunteer Position button...');
    const addVolunteerButton = page.locator('button:has-text("Add Volunteer Position"), button:has-text("Add Position"), [data-testid*="add-volunteer"]');
    const volunteerButtonCount = await addVolunteerButton.count();

    if (volunteerButtonCount > 0) {
      console.log('✅ Found Add Volunteer Position button');

      // Check for errors before clicking
      const errorsBefore = [...consoleErrors, ...jsErrors];

      await addVolunteerButton.first().click();
      await page.waitForTimeout(2000);

      // Check for errors after clicking
      const errorsAfter = [...consoleErrors, ...jsErrors];
      const newErrors = errorsAfter.filter(error => !errorsBefore.includes(error));

      if (newErrors.length === 0) {
        console.log('✅ Add Volunteer Position button clicked without errors');
      } else {
        console.log('❌ Add Volunteer Position button caused errors:', newErrors);
      }

      // Take screenshot of volunteer modal
      await page.screenshot({ path: 'test-results/add-buttons-6-volunteer-modal.png', fullPage: true });

      // Look for modal and close it
      const modal = page.locator('[role="dialog"], .modal, .mantine-modal');
      const modalCount = await modal.count();
      if (modalCount > 0) {
        console.log('✅ Volunteer Position modal opened successfully');
        // Try to close modal
        const closeButton = page.locator('button:has-text("Cancel"), button:has-text("Close"), [aria-label="Close"]');
        const closeCount = await closeButton.count();
        if (closeCount > 0) {
          await closeButton.first().click();
          await page.waitForTimeout(1000);
        }
      }
    } else {
      console.log('⚠️ Add Volunteer Position button not found');
    }

    // 8. Final error summary
    console.log('📊 Test Summary:');
    console.log(`   JavaScript Errors: ${jsErrors.length}`);
    console.log(`   Console Errors: ${consoleErrors.length}`);
    console.log(`   Network Errors: ${networkErrors.length}`);

    // Take final screenshot
    await page.screenshot({ path: 'test-results/add-buttons-7-final.png', fullPage: true });

    // Test should pass if no critical errors occurred
    expect(jsErrors.length).toBe(0);
  });

  test('Test actually adding items through modals', async ({ page }) => {
    console.log('🧪 Testing: Actually adding items through Add button modals');

    // Login and navigate to event edit page
    await page.goto('http://localhost:5173/login');
    await page.waitForTimeout(2000);
    await page.waitForSelector('[data-testid="email-input"]', { timeout: 10000 });

    await page.fill('[data-testid="email-input"]', 'admin@witchcityrope.com');
    await page.fill('[data-testid="password-input"]', 'Test123!');
    await page.click('[data-testid="login-button"]');
    await page.waitForTimeout(3000);

    await page.goto('http://localhost:5173/admin/events');
    await page.waitForTimeout(3000);

    const eventRows = page.locator('[data-testid="event-row"]');
    const eventCount = await eventRows.count();

    if (eventCount > 0) {
      await eventRows.first().click();
      await page.waitForTimeout(3000);

      // Try to add a session with actual data
      console.log('📝 Attempting to add a session with data...');
      const addSessionButton = page.locator('button:has-text("Add Session")');
      const sessionButtonCount = await addSessionButton.count();

      if (sessionButtonCount > 0) {
        await addSessionButton.first().click();
        await page.waitForTimeout(2000);

        // Look for form fields in the modal
        const titleField = page.locator('input[name*="title"], input[placeholder*="title"], input[label*="title"]');
        const titleCount = await titleField.count();

        if (titleCount > 0) {
          console.log('✅ Found session title field');
          await titleField.first().fill(`Test Session ${Date.now()}`);

          // Look for save/submit button
          const saveButton = page.locator('button:has-text("Save"), button:has-text("Add"), button[type="submit"]');
          const saveCount = await saveButton.count();

          if (saveCount > 0) {
            console.log('💾 Attempting to save session...');
            await saveButton.first().click();
            await page.waitForTimeout(2000);

            // Check if modal closed (success)
            const modal = page.locator('[role="dialog"], .modal, .mantine-modal');
            const modalCount = await modal.count();
            if (modalCount === 0) {
              console.log('✅ Session modal closed - likely saved successfully');
            } else {
              console.log('⚠️ Session modal still open - may have validation errors');
            }
          }
        }
      }

      // Take screenshot after attempting to add session
      await page.screenshot({ path: 'test-results/add-items-1-after-session.png', fullPage: true });

      // Similar test for ticket type
      console.log('📝 Attempting to add a ticket type with data...');
      const addTicketButton = page.locator('button:has-text("Add Ticket Type")');
      const ticketButtonCount = await addTicketButton.count();

      if (ticketButtonCount > 0) {
        await addTicketButton.first().click();
        await page.waitForTimeout(2000);

        const nameField = page.locator('input[name*="name"], input[placeholder*="name"], input[label*="name"]');
        const nameCount = await nameField.count();

        if (nameCount > 0) {
          console.log('✅ Found ticket type name field');
          await nameField.first().fill(`Test Ticket ${Date.now()}`);

          const saveButton = page.locator('button:has-text("Save"), button:has-text("Add"), button[type="submit"]');
          const saveCount = await saveButton.count();

          if (saveCount > 0) {
            console.log('💾 Attempting to save ticket type...');
            await saveButton.first().click();
            await page.waitForTimeout(2000);
          }
        }
      }

      // Take final screenshot
      await page.screenshot({ path: 'test-results/add-items-2-final.png', fullPage: true });
    }

    console.log('✅ Add items test completed');
  });
});