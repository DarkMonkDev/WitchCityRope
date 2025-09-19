import { test, expect } from '@playwright/test';

/**
 * Specific investigation of Events Details persistence issues
 *
 * Testing scenarios:
 * 1. Teacher changes not persisting after save + refresh
 * 2. Volunteer positions, sessions, ticket types added via UI disappear on refresh
 * 3. Deleted sessions/ticket types reappear after refresh
 * 4. Verify seeded data shows up correctly
 */

test.describe('Events Details Persistence Investigation', () => {

  test.beforeEach(async ({ page }) => {
    // Track ALL API calls with detailed logging
    const apiCalls: any[] = [];
    const consoleLogs: string[] = [];

    page.on('console', msg => {
      consoleLogs.push(`[${msg.type()}] ${msg.text()}`);
      if (msg.type() === 'error') {
        console.log('🚨 Console Error:', msg.text());
      }
    });

    page.on('request', request => {
      if (request.url().includes('/api/')) {
        const payload = {
          method: request.method(),
          url: request.url(),
          timestamp: new Date().toISOString(),
          postData: request.postData()
        };
        apiCalls.push(payload);
        console.log(`📤 API Request: ${request.method()} ${request.url()}`);
        if (request.postData()) {
          console.log(`📤 Request Body:`, request.postData());
        }
      }
    });

    page.on('response', response => {
      if (response.url().includes('/api/')) {
        console.log(`📥 API Response: ${response.status()} ${response.url()}`);
        if (!response.ok()) {
          console.log(`🚨 API Error: ${response.status()} ${response.url()}`);
        }
      }
    });

    // Store API calls for later analysis
    (page as any).apiCalls = apiCalls;
    (page as any).consoleLogs = consoleLogs;
  });

  test('Investigate Teacher Change Persistence', async ({ page }) => {
    console.log('🔍 Starting Teacher Change Persistence Investigation');

    // Step 1: Login as admin
    await page.goto('http://localhost:5173/login');
    await page.fill('[data-testid="email-input"]', 'admin@witchcityrope.com');
    await page.fill('[data-testid="password-input"]', 'Test123!');
    await page.click('[data-testid="login-button"]');
    await page.waitForTimeout(3000);

    // Step 2: Navigate to admin events
    await page.goto('http://localhost:5173/admin/events');
    await page.waitForTimeout(2000);
    await page.screenshot({ path: 'test-results/teacher-test-1-events-list.png', fullPage: true });

    // Step 3: Look for the first event with an edit button
    const editButtons = page.locator('button:has-text("Edit"), [data-testid*="edit"], a[href*="/edit"]');
    const editCount = await editButtons.count();
    console.log(`Found ${editCount} edit buttons`);

    if (editCount === 0) {
      console.log('⚠️ No edit buttons found, trying to create event first');
      await page.goto('http://localhost:5173/admin/events/create');
      await page.waitForTimeout(2000);
      await page.screenshot({ path: 'test-results/teacher-test-2-create-form.png', fullPage: true });
      return;
    }

    // Step 4: Click first edit button to go to event details
    await editButtons.first().click();
    await page.waitForTimeout(3000);
    await page.screenshot({ path: 'test-results/teacher-test-3-event-details.png', fullPage: true });

    console.log('📍 Current URL:', page.url());

    // Step 5: Look for teacher selection
    const teacherSelectors = [
      'select[name*="teacher"]',
      '[data-testid*="teacher"]',
      'select:has(option:text("RopeMaster"))',
      'input[name*="teacher"]',
      '.teacher-select',
      '[placeholder*="teacher"]'
    ];

    let teacherElement = null;
    let usedSelector = '';

    for (const selector of teacherSelectors) {
      const element = page.locator(selector);
      const isVisible = await element.isVisible().catch(() => false);
      if (isVisible) {
        teacherElement = element;
        usedSelector = selector;
        console.log(`✅ Teacher element found with: ${selector}`);
        break;
      }
    }

    if (!teacherElement) {
      console.log('⚠️ No teacher selection element found, checking page content');
      const pageContent = await page.textContent('body');
      console.log('Page contains "teacher":', pageContent?.toLowerCase().includes('teacher'));
      console.log('Page contains "instructor":', pageContent?.toLowerCase().includes('instructor'));

      // Try looking for any form elements
      const formElements = await page.locator('select, input[type="text"], input[type="email"]').count();
      console.log(`Found ${formElements} form elements`);

      await page.screenshot({ path: 'test-results/teacher-test-4-no-teacher-element.png', fullPage: true });
      return;
    }

    // Step 6: Get current teacher value
    const currentTeacher = await teacherElement.inputValue().catch(() => '');
    console.log(`📋 Current teacher value: "${currentTeacher}"`);

    // Step 7: Change teacher if it's a select
    const tagName = await teacherElement.evaluate(el => el.tagName.toLowerCase());
    console.log(`📋 Teacher element type: ${tagName}`);

    if (tagName === 'select') {
      // Get all options
      const options = await page.locator(`${usedSelector} option`).allTextContents();
      console.log(`📋 Available teacher options:`, options);

      // Try to select a different teacher
      if (options.length > 1) {
        const newTeacher = options.find(opt => opt !== currentTeacher) || options[1];
        console.log(`🔄 Changing teacher to: "${newTeacher}"`);

        await teacherElement.selectOption(newTeacher);
        await page.waitForTimeout(1000);

        // Check if the value actually changed
        const newValue = await teacherElement.inputValue();
        console.log(`📋 Teacher value after change: "${newValue}"`);
      }
    }

    // Step 8: Look for Save button and click it
    const saveButtons = page.locator('button:has-text("Save"), [data-testid*="save"], button[type="submit"]');
    const saveCount = await saveButtons.count();
    console.log(`Found ${saveCount} save buttons`);

    if (saveCount > 0) {
      console.log('💾 Clicking save button');
      await saveButtons.first().click();
      await page.waitForTimeout(3000);
      await page.screenshot({ path: 'test-results/teacher-test-5-after-save.png', fullPage: true });
    }

    // Step 9: Refresh the page to test persistence
    console.log('🔄 Refreshing page to test persistence');
    await page.reload();
    await page.waitForTimeout(3000);
    await page.screenshot({ path: 'test-results/teacher-test-6-after-refresh.png', fullPage: true });

    // Step 10: Check if teacher change persisted
    const teacherAfterRefresh = await page.locator(usedSelector).inputValue().catch(() => '');
    console.log(`📋 Teacher value after refresh: "${teacherAfterRefresh}"`);

    // Step 11: Analyze API calls
    const apiCalls = (page as any).apiCalls || [];
    console.log(`📊 Total API calls made: ${apiCalls.length}`);

    const saveApiCalls = apiCalls.filter(call =>
      call.method === 'POST' || call.method === 'PUT' || call.method === 'PATCH'
    );
    console.log(`📊 Save API calls: ${saveApiCalls.length}`);

    if (saveApiCalls.length > 0) {
      console.log('💾 Save API calls detected:');
      saveApiCalls.forEach(call => {
        console.log(`  - ${call.method} ${call.url}`);
        if (call.postData) {
          console.log(`    Body: ${call.postData}`);
        }
      });
    } else {
      console.log('⚠️ No save API calls detected - this is the problem!');
    }
  });

  test('Investigate Session/Ticket Type Persistence', async ({ page }) => {
    console.log('🔍 Starting Session/Ticket Type Persistence Investigation');

    // Login and navigate to event details
    await page.goto('http://localhost:5173/login');
    await page.fill('[data-testid="email-input"]', 'admin@witchcityrope.com');
    await page.fill('[data-testid="password-input"]', 'Test123!');
    await page.click('[data-testid="login-button"]');
    await page.waitForTimeout(3000);

    await page.goto('http://localhost:5173/admin/events');
    await page.waitForTimeout(2000);

    // Find and click first edit button
    const editButtons = page.locator('button:has-text("Edit"), [data-testid*="edit"], a[href*="/edit"]');
    const editCount = await editButtons.count();

    if (editCount === 0) {
      console.log('⚠️ No events to edit, skipping test');
      return;
    }

    await editButtons.first().click();
    await page.waitForTimeout(3000);
    await page.screenshot({ path: 'test-results/session-test-1-event-details.png', fullPage: true });

    // Look for session/ticket type management
    const addButtons = page.locator('button:has-text("Add"), button:has-text("New"), [data-testid*="add"]');
    const addCount = await addButtons.count();
    console.log(`Found ${addCount} add buttons`);

    if (addCount > 0) {
      console.log('➕ Trying to add new session/ticket');

      // Take screenshot before adding
      await page.screenshot({ path: 'test-results/session-test-2-before-add.png', fullPage: true });

      await addButtons.first().click();
      await page.waitForTimeout(2000);

      // Take screenshot after clicking add
      await page.screenshot({ path: 'test-results/session-test-3-after-add-click.png', fullPage: true });

      // Look for form fields to fill
      const inputs = page.locator('input[type="text"], textarea');
      const inputCount = await inputs.count();
      console.log(`Found ${inputCount} input fields`);

      if (inputCount > 0) {
        // Fill first input with test data
        await inputs.first().fill('Test Session Added by Playwright');
        await page.waitForTimeout(1000);

        // Look for save/confirm button
        const confirmButtons = page.locator('button:has-text("Save"), button:has-text("Add"), button:has-text("Confirm")');
        const confirmCount = await confirmButtons.count();

        if (confirmCount > 0) {
          await confirmButtons.first().click();
          await page.waitForTimeout(2000);
          await page.screenshot({ path: 'test-results/session-test-4-after-add.png', fullPage: true });
        }
      }
    }

    // Now test deletion if there are delete buttons
    const deleteButtons = page.locator('button:has-text("Delete"), button:has-text("Remove"), [data-testid*="delete"]');
    const deleteCount = await deleteButtons.count();
    console.log(`Found ${deleteCount} delete buttons`);

    if (deleteCount > 0) {
      console.log('🗑️ Testing deletion');
      await deleteButtons.first().click();
      await page.waitForTimeout(1000);

      // Look for confirmation dialog
      const confirmDelete = page.locator('button:has-text("Delete"), button:has-text("Yes"), button:has-text("Confirm")');
      const confirmDeleteCount = await confirmDelete.count();

      if (confirmDeleteCount > 0) {
        await confirmDelete.first().click();
        await page.waitForTimeout(2000);
        await page.screenshot({ path: 'test-results/session-test-5-after-delete.png', fullPage: true });
      }
    }

    // Test persistence by refreshing
    console.log('🔄 Refreshing to test persistence');
    await page.reload();
    await page.waitForTimeout(3000);
    await page.screenshot({ path: 'test-results/session-test-6-after-refresh.png', fullPage: true });

    // Analyze API calls
    const apiCalls = (page as any).apiCalls || [];
    const consoleLogs = (page as any).consoleLogs || [];

    console.log(`📊 Total API calls: ${apiCalls.length}`);
    console.log(`📊 Console logs: ${consoleLogs.length}`);

    // Look for specific patterns
    const postCalls = apiCalls.filter(call => call.method === 'POST');
    const deleteCalls = apiCalls.filter(call => call.method === 'DELETE');
    const putCalls = apiCalls.filter(call => call.method === 'PUT');

    console.log(`📊 POST calls: ${postCalls.length}`);
    console.log(`📊 DELETE calls: ${deleteCalls.length}`);
    console.log(`📊 PUT calls: ${putCalls.length}`);

    // Log specific API calls
    [...postCalls, ...deleteCalls, ...putCalls].forEach(call => {
      console.log(`🔗 ${call.method} ${call.url}`);
      if (call.postData) {
        console.log(`📤 Body: ${call.postData}`);
      }
    });
  });

  test('Test API Endpoints Directly', async ({ page }) => {
    console.log('🔍 Testing API endpoints directly');

    // Login first to get cookies
    await page.goto('http://localhost:5173/login');
    await page.fill('[data-testid="email-input"]', 'admin@witchcityrope.com');
    await page.fill('[data-testid="password-input"]', 'Test123!');
    await page.click('[data-testid="login-button"]');
    await page.waitForTimeout(3000);

    // Test key API endpoints
    const endpoints = [
      '/api/events',
      '/api/events/1',
      '/api/teachers',
      '/api/events/1/sessions',
      '/api/events/1/tickets'
    ];

    for (const endpoint of endpoints) {
      console.log(`🔗 Testing ${endpoint}`);

      try {
        const response = await page.request.get(`http://localhost:5655${endpoint}`);
        console.log(`📥 ${endpoint}: ${response.status()}`);

        if (response.ok()) {
          const data = await response.text();
          console.log(`📄 ${endpoint} response length: ${data.length} chars`);

          // Try to parse as JSON
          try {
            const json = JSON.parse(data);
            if (Array.isArray(json)) {
              console.log(`📊 ${endpoint} returned ${json.length} items`);
            } else {
              console.log(`📊 ${endpoint} returned object with keys:`, Object.keys(json));
            }
          } catch (e) {
            console.log(`📄 ${endpoint} returned non-JSON data`);
          }
        } else {
          console.log(`🚨 ${endpoint} failed: ${response.status()}`);
        }
      } catch (error) {
        console.log(`🚨 ${endpoint} error:`, error);
      }
    }
  });
});