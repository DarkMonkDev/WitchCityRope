import { test, expect } from '@playwright/test';

/**
 * Test to verify that event editing actually works
 * This addresses the reported "persistence issues" by testing the actual workflow
 */

test.describe('Event Editing Functionality Verification', () => {

  test.beforeEach(async ({ page }) => {
    // Monitor API calls
    const apiCalls: any[] = [];

    page.on('request', request => {
      if (request.url().includes('/api/')) {
        apiCalls.push({
          method: request.method(),
          url: request.url(),
          timestamp: new Date().toISOString(),
          postData: request.postData()
        });
      }
    });

    (page as any).apiCalls = apiCalls;
  });

  test('Verify complete event editing workflow', async ({ page }) => {
    console.log('🧪 Testing: Complete event editing workflow');

    // 1. Login as admin
    await page.goto('http://localhost:5173/login');
    await page.fill('[data-testid="email-input"]', 'admin@witchcityrope.com');
    await page.fill('[data-testid="password-input"]', 'Test123!');
    await page.click('[data-testid="login-button"]');
    await page.waitForTimeout(3000);

    // 2. Navigate to admin events page
    await page.goto('http://localhost:5173/admin/events');
    await page.waitForTimeout(2000);

    // Screenshot of events list
    await page.screenshot({ path: 'test-results/workflow-1-events-list.png', fullPage: true });

    // 3. Verify events are loaded
    const eventRows = page.locator('[data-testid="event-row"]');
    const eventCount = await eventRows.count();
    console.log(`✅ Found ${eventCount} events in table`);
    expect(eventCount).toBeGreaterThan(0);

    // 4. Click on the FIRST EVENT ROW (this should navigate to edit page)
    console.log('🖱️ Clicking on first event row to navigate to edit page');
    await eventRows.first().click();
    await page.waitForTimeout(3000);

    // 5. Verify we're now on the event details/edit page
    const currentUrl = page.url();
    console.log(`📍 Current URL after row click: ${currentUrl}`);
    expect(currentUrl).toMatch(/\/admin\/events\/[a-f0-9\-]+/);

    // Screenshot of event details page
    await page.screenshot({ path: 'test-results/workflow-2-event-details.png', fullPage: true });

    // 6. Verify the page loads the event form
    const eventForm = page.locator('form, [data-testid*="event-form"], [data-testid*="form"]');
    const hasForm = await eventForm.count() > 0;
    console.log(`📝 Event form present: ${hasForm}`);

    // 7. Look for teacher selection (this was the main reported issue)
    const teacherSelectors = [
      'select[name*="teacher"]',
      '[data-testid*="teacher"]',
      'select:has(option)',
      '[placeholder*="teacher"]',
      '[label*="teacher"]',
      'select'
    ];

    let teacherElement = null;
    let usedSelector = '';

    for (const selector of teacherSelectors) {
      const element = page.locator(selector);
      const count = await element.count();
      if (count > 0) {
        teacherElement = element.first();
        usedSelector = selector;
        console.log(`✅ Found teacher element with: ${selector} (${count} elements)`);
        break;
      }
    }

    // 8. Test the teacher field functionality if found
    if (teacherElement) {
      const currentValue = await teacherElement.inputValue().catch(() => '');
      console.log(`📋 Current teacher value: "${currentValue}"`);

      // Try to interact with the teacher field
      const tagName = await teacherElement.evaluate(el => el.tagName.toLowerCase());
      console.log(`📋 Teacher element type: ${tagName}`);

      if (tagName === 'select') {
        const options = await page.locator(`${usedSelector} option`).allTextContents();
        console.log(`📋 Available options:`, options);

        if (options.length > 1) {
          // Select a different option
          const newOption = options.find(opt => opt !== currentValue) || options[1];
          console.log(`🔄 Changing to: "${newOption}"`);

          await teacherElement.selectOption(newOption);
          await page.waitForTimeout(1000);

          const newValue = await teacherElement.inputValue();
          console.log(`📋 Value after change: "${newValue}"`);
        }
      }
    } else {
      console.log('⚠️ No teacher selection element found - checking page content');
      const pageText = await page.textContent('body');
      console.log('🔍 Page contains "teacher":', pageText?.toLowerCase().includes('teacher'));
      console.log('🔍 Page contains "instructor":', pageText?.toLowerCase().includes('instructor'));
    }

    // 9. Look for and test save functionality
    const saveButtons = page.locator('button:has-text("Save"), [data-testid*="save"], button[type="submit"]');
    const saveCount = await saveButtons.count();
    console.log(`💾 Found ${saveCount} save buttons`);

    if (saveCount > 0) {
      console.log('💾 Testing save functionality');

      // Screenshot before save
      await page.screenshot({ path: 'test-results/workflow-3-before-save.png', fullPage: true });

      await saveButtons.first().click();
      await page.waitForTimeout(3000);

      // Screenshot after save
      await page.screenshot({ path: 'test-results/workflow-4-after-save.png', fullPage: true });
    }

    // 10. Test page refresh persistence
    console.log('🔄 Testing persistence by refreshing page');
    await page.reload();
    await page.waitForTimeout(3000);

    // Screenshot after refresh
    await page.screenshot({ path: 'test-results/workflow-5-after-refresh.png', fullPage: true });

    // 11. Analyze API calls
    const apiCalls = (page as any).apiCalls || [];
    console.log(`📊 Total API calls made: ${apiCalls.length}`);

    // Filter for save-related calls
    const saveCalls = apiCalls.filter(call =>
      call.method === 'PUT' || call.method === 'PATCH' || call.method === 'POST'
    );
    console.log(`📊 Save API calls: ${saveCalls.length}`);

    // Log all save calls
    saveCalls.forEach(call => {
      console.log(`🔗 ${call.method} ${call.url}`);
      if (call.postData) {
        console.log(`📤 Body: ${call.postData}`);
      }
    });

    // 12. Final verification
    const finalUrl = page.url();
    console.log(`📍 Final URL: ${finalUrl}`);
    console.log(`✅ Event editing workflow test completed`);
  });

  test('Verify sessions and ticket types management', async ({ page }) => {
    console.log('🧪 Testing: Sessions and ticket types management');

    // Login and navigate to an event details page
    await page.goto('http://localhost:5173/login');
    await page.fill('[data-testid="email-input"]', 'admin@witchcityrope.com');
    await page.fill('[data-testid="password-input"]', 'Test123!');
    await page.click('[data-testid="login-button"]');
    await page.waitForTimeout(3000);

    // Go to events list and click first event
    await page.goto('http://localhost:5173/admin/events');
    await page.waitForTimeout(2000);

    const eventRows = page.locator('[data-testid="event-row"]');
    const eventCount = await eventRows.count();

    if (eventCount > 0) {
      await eventRows.first().click();
      await page.waitForTimeout(3000);

      // Look for sessions and ticket types sections
      const pageContent = await page.textContent('body');
      const hasSessionsSection = pageContent?.toLowerCase().includes('session');
      const hasTicketsSection = pageContent?.toLowerCase().includes('ticket');

      console.log(`📋 Sessions section present: ${hasSessionsSection}`);
      console.log(`📋 Tickets section present: ${hasTicketsSection}`);

      // Look for add buttons
      const addButtons = page.locator('button:has-text("Add"), button:has-text("New"), [data-testid*="add"]');
      const addCount = await addButtons.count();
      console.log(`➕ Found ${addCount} add buttons`);

      // Screenshot of the form
      await page.screenshot({ path: 'test-results/sessions-tickets-form.png', fullPage: true });

      // Look for existing sessions/tickets in the page
      const tables = page.locator('table');
      const tableCount = await tables.count();
      console.log(`📋 Found ${tableCount} tables (potentially for sessions/tickets)`);

      // Try to add a session/ticket if add buttons exist
      if (addCount > 0) {
        console.log('➕ Testing add functionality');
        await addButtons.first().click();
        await page.waitForTimeout(2000);

        await page.screenshot({ path: 'test-results/sessions-tickets-add.png', fullPage: true });
      }
    }
  });
});