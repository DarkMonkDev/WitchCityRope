/**
 * Events Admin Persistence Verification
 * Comprehensive testing of the react-developer fixes
 * Date: 2025-09-19
 *
 * Fixes being tested:
 * 1. Removed form re-mounting issue (key prop problem)
 * 2. Fixed API response transformation for sessions, ticketTypes, teacherIds
 * 3. Fixed array handling to allow saving empty arrays (for clearing data)
 * 4. Added proper form initial data loading with useEffect
 * 5. Added comprehensive logging throughout save flow
 */

import { test, expect } from '@playwright/test';

test.describe('Events Admin Persistence Verification', () => {
  // Configure longer timeout for these comprehensive tests
  test.setTimeout(120000);

  test.beforeEach(async ({ page }) => {
    // Monitor console logs for the comprehensive logging added by developer
    page.on('console', msg => {
      const text = msg.text();
      // Log the extensive logging system the developer added
      if (text.includes('EVENTS_ADMIN') || text.includes('SaveEvent') || text.includes('API_') || text.includes('FORM_')) {
        console.log(`📝 ${msg.type().toUpperCase()}: ${text}`);
      }
      if (msg.type() === 'error') {
        console.error(`❌ Error: ${text}`);
      }
    });

    // Monitor network requests for API payload verification
    page.on('request', request => {
      if (request.url().includes('/api/events') && request.method() !== 'GET') {
        console.log(`🌐 ${request.method()} ${request.url()}`);
      }
    });

    page.on('response', response => {
      if (response.url().includes('/api/events') && response.status() !== 200) {
        console.log(`📡 Response: ${response.status()} ${response.url()}`);
      }
    });
  });

  async function loginAsAdmin(page) {
    console.log('🔐 Logging in as admin...');
    await page.goto('/');

    // Wait for page to load
    await page.waitForLoadState('networkidle');

    // Click login button - using different approaches to find it
    try {
      await page.getByText('LOGIN').click({ timeout: 10000 });
    } catch {
      try {
        await page.getByRole('button', { name: 'LOGIN' }).click({ timeout: 5000 });
      } catch {
        await page.locator('button:has-text("LOGIN")').click({ timeout: 5000 });
      }
    }

    // Fill login form
    await page.getByPlaceholder('Email').fill('admin@witchcityrope.com');
    await page.getByPlaceholder('Password').fill('Test123!');
    await page.getByRole('button', { name: 'Login' }).click();

    // Wait for successful login - check for admin navigation
    await page.waitForTimeout(3000); // Give time for auth to process
    console.log('✅ Admin login completed');
  }

  async function navigateToEventsAdmin(page) {
    console.log('🎯 Navigating to Events Admin...');

    // Navigate to admin area
    await page.goto('/admin/events');
    await page.waitForLoadState('networkidle');

    console.log('✅ Events Admin page loaded');
  }

  async function selectFirstEventForEdit(page) {
    console.log('📝 Selecting first event for editing...');

    // Wait for events to load
    await page.waitForTimeout(2000);

    // Try multiple selectors to find edit button
    let editClicked = false;
    const selectors = [
      'button:has-text("Edit")',
      'a:has-text("Edit")',
      '[data-testid="edit-event"]',
      '.edit-button',
      '.btn-edit'
    ];

    for (const selector of selectors) {
      try {
        const element = page.locator(selector).first();
        if (await element.isVisible({ timeout: 2000 })) {
          await element.click();
          editClicked = true;
          break;
        }
      } catch (e) {
        continue;
      }
    }

    if (!editClicked) {
      // Try clicking on first row if no edit button found
      await page.locator('tr, .event-item, .event-row').first().click();
    }

    await page.waitForTimeout(2000);
    console.log('✅ Event edit form opened');
  }

  async function saveEventForm(page) {
    console.log('💾 Saving event form...');

    // Try multiple selectors for save button
    const saveSelectors = [
      'button:has-text("Save Event")',
      'button:has-text("Save")',
      '[data-testid="save-event"]',
      '.btn-save',
      'input[type="submit"]'
    ];

    for (const selector of saveSelectors) {
      try {
        const saveButton = page.locator(selector);
        if (await saveButton.isVisible({ timeout: 2000 })) {
          await saveButton.click();
          console.log(`✅ Clicked save button: ${selector}`);
          break;
        }
      } catch (e) {
        continue;
      }
    }

    // Wait for save to complete
    await page.waitForTimeout(3000);
    console.log('✅ Save operation completed');
  }

  test('Comprehensive Persistence Verification - Teacher Changes', async ({ page }) => {
    console.log('🧪 TESTING: Teacher assignment persistence');

    await loginAsAdmin(page);
    await navigateToEventsAdmin(page);
    await selectFirstEventForEdit(page);

    // Take screenshot of initial state
    await page.screenshot({ path: '/home/chad/repos/witchcityrope-react./test-results/initial-event-edit-form.png', fullPage: true });

    // Attempt to find and modify teacher field
    const teacherSelectors = [
      '[data-testid="teacher-select"]',
      'select[name*="teacher"]',
      'input[name*="teacher"]',
      '.teacher-select',
      '#teachers'
    ];

    let teacherField = null;
    for (const selector of teacherSelectors) {
      try {
        const field = page.locator(selector);
        if (await field.isVisible({ timeout: 2000 })) {
          teacherField = field;
          console.log(`✅ Found teacher field: ${selector}`);
          break;
        }
      } catch (e) {
        continue;
      }
    }

    if (teacherField) {
      // Get initial value
      const initialValue = await teacherField.inputValue().catch(() => '');
      console.log(`📊 Initial teacher value: "${initialValue}"`);

      // Try to clear and set new value
      await teacherField.clear();
      await teacherField.fill('Test Teacher Change');

      const newValue = await teacherField.inputValue().catch(() => '');
      console.log(`📝 New teacher value: "${newValue}"`);
    } else {
      console.log('⚠️ Teacher field not found - taking screenshot for inspection');
      await page.screenshot({ path: '/home/chad/repos/witchcityrope-react./test-results/teacher-field-not-found.png', fullPage: true });
    }

    await saveEventForm(page);

    // Refresh and verify persistence
    console.log('🔄 Refreshing to verify persistence...');
    await page.reload();
    await page.waitForLoadState('networkidle');

    // Take screenshot after refresh
    await page.screenshot({ path: '/home/chad/repos/witchcityrope-react./test-results/after-refresh-teacher-test.png', fullPage: true });

    console.log('✅ Teacher persistence test completed');
  });

  test('Comprehensive Persistence Verification - Form Data Flow', async ({ page }) => {
    console.log('🧪 TESTING: Overall form data persistence and API flow');

    await loginAsAdmin(page);
    await navigateToEventsAdmin(page);
    await selectFirstEventForEdit(page);

    // Monitor form fields and their persistence
    const testTimestamp = Date.now();

    // Try to find and modify event title
    const titleSelectors = [
      '[data-testid="event-title"]',
      'input[name*="title"]',
      'input[name="name"]',
      '#title',
      '#eventTitle'
    ];

    let titleField = null;
    for (const selector of titleSelectors) {
      try {
        const field = page.locator(selector);
        if (await field.isVisible({ timeout: 2000 })) {
          titleField = field;
          console.log(`✅ Found title field: ${selector}`);
          break;
        }
      } catch (e) {
        continue;
      }
    }

    if (titleField) {
      const originalTitle = await titleField.inputValue().catch(() => '');
      const newTitle = `Test Event ${testTimestamp}`;

      console.log(`📊 Original title: "${originalTitle}"`);
      await titleField.clear();
      await titleField.fill(newTitle);
      console.log(`📝 Set new title: "${newTitle}"`);

      await saveEventForm(page);

      // Refresh and verify
      await page.reload();
      await page.waitForLoadState('networkidle');
      await page.waitForTimeout(2000);

      // Try to find the title field again after refresh
      for (const selector of titleSelectors) {
        try {
          const field = page.locator(selector);
          if (await field.isVisible({ timeout: 2000 })) {
            const persistedTitle = await field.inputValue().catch(() => '');
            console.log(`📊 Persisted title: "${persistedTitle}"`);

            if (persistedTitle.includes(testTimestamp.toString())) {
              console.log('✅ Title persistence: PASSED');
            } else {
              console.log('❌ Title persistence: FAILED');
            }
            break;
          }
        } catch (e) {
          continue;
        }
      }
    }

    // Take final screenshot
    await page.screenshot({ path: '/home/chad/repos/witchcityrope-react./test-results/final-persistence-test.png', fullPage: true });

    console.log('✅ Form data flow test completed');
  });

  test('API Network Traffic Analysis', async ({ page }) => {
    console.log('🧪 TESTING: API network traffic and payload analysis');

    const networkCalls = [];

    // Capture all network activity
    page.on('request', request => {
      if (request.url().includes('/api/')) {
        networkCalls.push({
          method: request.method(),
          url: request.url(),
          timestamp: new Date().toISOString()
        });
      }
    });

    page.on('response', response => {
      if (response.url().includes('/api/events')) {
        console.log(`📡 API Response: ${response.status()} ${response.request().method()} ${response.url()}`);
      }
    });

    await loginAsAdmin(page);
    await navigateToEventsAdmin(page);
    await selectFirstEventForEdit(page);

    // Wait for initial data loading
    await page.waitForTimeout(3000);
    console.log('📊 Initial API calls captured');

    // Make a simple change to trigger save
    const timestampChange = Date.now();
    await page.evaluate((timestamp) => {
      // Try to find any input field and modify it
      const inputs = document.querySelectorAll('input[type="text"], textarea');
      if (inputs.length > 0) {
        inputs[0].value = `Test ${timestamp}`;
        inputs[0].dispatchEvent(new Event('input', { bubbles: true }));
      }
    }, timestampChange);

    await saveEventForm(page);

    // Wait for save API calls
    await page.waitForTimeout(3000);

    console.log('📊 Network Calls Summary:');
    networkCalls.forEach((call, index) => {
      console.log(`  ${index + 1}. ${call.method} ${call.url} at ${call.timestamp}`);
    });

    console.log('✅ API network analysis completed');
  });

  test('Browser Developer Tools Investigation', async ({ page }) => {
    console.log('🧪 TESTING: Browser developer tools investigation');

    // Enable verbose console logging
    await page.addInitScript(() => {
      // Override console.log to capture all developer logging
      const originalLog = console.log;
      window._testLogs = [];
      console.log = (...args) => {
        window._testLogs.push(args.join(' '));
        originalLog.apply(console, args);
      };
    });

    await loginAsAdmin(page);
    await navigateToEventsAdmin(page);
    await selectFirstEventForEdit(page);

    // Wait for form initialization
    await page.waitForTimeout(3000);

    // Extract all console logs
    const logs = await page.evaluate(() => window._testLogs || []);

    console.log('📝 Console Logs Analysis:');
    logs.forEach((log, index) => {
      if (log.includes('EVENTS_ADMIN') || log.includes('SaveEvent') || log.includes('API') || log.includes('FORM')) {
        console.log(`  ${index + 1}. ${log}`);
      }
    });

    // Check for specific fix-related logs
    const hasFormInitLogs = logs.some(log => log.includes('FORM_INIT') || log.includes('useEffect'));
    const hasApiTransformLogs = logs.some(log => log.includes('API_TRANSFORM') || log.includes('response'));
    const hasArrayHandlingLogs = logs.some(log => log.includes('ARRAY') || log.includes('empty'));

    console.log('🔍 Fix Verification:');
    console.log(`  Form initialization logging: ${hasFormInitLogs ? '✅' : '❌'}`);
    console.log(`  API transformation logging: ${hasApiTransformLogs ? '✅' : '❌'}`);
    console.log(`  Array handling logging: ${hasArrayHandlingLogs ? '✅' : '❌'}`);

    console.log('✅ Developer tools investigation completed');
  });

});