import { test, expect } from '@playwright/test';

test.describe('Final Persistence Verification - All CRUD Operations', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('http://localhost:5173');

    // Login as admin to access event management
    await page.click('text=LOGIN');
    await page.fill('input[name="email"]', 'admin@witchcityrope.com');
    await page.fill('input[name="password"]', 'Test123!');
    await page.click('button[type="submit"]');

    // Wait for login to complete and navigate to admin events
    await page.waitForTimeout(2000);
    await page.goto('http://localhost:5173/admin/events');
    await page.waitForTimeout(2000);
  });

  test('Verify API includes all data arrays', async ({ page }) => {
    console.log('🔍 Testing API endpoint completeness...');

    const response = await page.request.get('http://localhost:5655/api/events');
    expect(response.status()).toBe(200);

    const data = await response.json();
    expect(data.data).toBeDefined();
    expect(data.data.length).toBeGreaterThan(0);

    const firstEvent = data.data[0];
    console.log('📊 API Response includes:');
    console.log(`- Sessions: ${firstEvent.sessions?.length || 0} items`);
    console.log(`- Ticket Types: ${firstEvent.ticketTypes?.length || 0} items`);
    console.log(`- Volunteer Positions: ${firstEvent.volunteerPositions?.length || 0} items`);

    // Verify all expected arrays exist
    expect(firstEvent.sessions).toBeDefined();
    expect(firstEvent.ticketTypes).toBeDefined();
    expect(firstEvent.volunteerPositions).toBeDefined();

    console.log('✅ API includes all required data arrays');
  });

  test('Complete Sessions CRUD verification', async ({ page }) => {
    console.log('🔍 Testing Sessions CRUD operations...');

    // Find an event to edit
    await page.click('[data-testid="edit-event"]:first-child');
    await page.waitForTimeout(2000);

    console.log('📝 Adding new session...');
    // Add a new session
    await page.click('text=Add Session');
    await page.fill('input[name="sessionName"]', 'Test Session CRUD');
    await page.fill('input[name="capacity"]', '25');
    await page.click('text=Save Session');
    await page.waitForTimeout(1000);

    // Save event
    await page.click('button:has-text("Save Event")');
    await page.waitForTimeout(2000);

    // Refresh page to verify persistence
    await page.reload();
    await page.waitForTimeout(2000);

    // Verify session was saved
    await expect(page.locator('text=Test Session CRUD')).toBeVisible();
    console.log('✅ Session ADD persisted successfully');

    console.log('✏️ Editing session...');
    // Edit the session
    await page.click('[data-testid="edit-session"]:has-text("Test Session CRUD")');
    await page.fill('input[name="sessionName"]', 'Test Session EDITED');
    await page.click('text=Save Session');
    await page.waitForTimeout(1000);

    // Save and refresh again
    await page.click('button:has-text("Save Event")');
    await page.waitForTimeout(2000);
    await page.reload();
    await page.waitForTimeout(2000);

    // Verify edit persisted
    await expect(page.locator('text=Test Session EDITED')).toBeVisible();
    console.log('✅ Session EDIT persisted successfully');

    console.log('🗑️ Deleting session...');
    // Delete the session
    await page.click('[data-testid="delete-session"]:has-text("Test Session EDITED")');
    await page.click('text=Confirm Delete');
    await page.waitForTimeout(1000);

    // Save and refresh
    await page.click('button:has-text("Save Event")');
    await page.waitForTimeout(2000);
    await page.reload();
    await page.waitForTimeout(2000);

    // Verify deletion persisted
    await expect(page.locator('text=Test Session EDITED')).not.toBeVisible();
    console.log('✅ Session DELETE persisted successfully');
  });

  test('Complete Ticket Types CRUD verification', async ({ page }) => {
    console.log('🔍 Testing Ticket Types CRUD operations...');

    // Find an event to edit
    await page.click('[data-testid="edit-event"]:first-child');
    await page.waitForTimeout(2000);

    console.log('📝 Adding new ticket type...');
    // Add a new ticket type
    await page.click('text=Add Ticket Type');
    await page.fill('input[name="ticketName"]', 'Test Ticket CRUD');
    await page.fill('input[name="price"]', '15.00');
    await page.fill('input[name="quantity"]', '20');
    await page.click('text=Save Ticket Type');
    await page.waitForTimeout(1000);

    // Save event
    await page.click('button:has-text("Save Event")');
    await page.waitForTimeout(2000);

    // Refresh page to verify persistence
    await page.reload();
    await page.waitForTimeout(2000);

    // Verify ticket type was saved
    await expect(page.locator('text=Test Ticket CRUD')).toBeVisible();
    console.log('✅ Ticket Type ADD persisted successfully');

    console.log('✏️ Editing ticket type...');
    // Edit the ticket type
    await page.click('[data-testid="edit-ticket"]:has-text("Test Ticket CRUD")');
    await page.fill('input[name="ticketName"]', 'Test Ticket EDITED');
    await page.fill('input[name="price"]', '18.00');
    await page.click('text=Save Ticket Type');
    await page.waitForTimeout(1000);

    // Save and refresh again
    await page.click('button:has-text("Save Event")');
    await page.waitForTimeout(2000);
    await page.reload();
    await page.waitForTimeout(2000);

    // Verify edit persisted
    await expect(page.locator('text=Test Ticket EDITED')).toBeVisible();
    await expect(page.locator('text=$18.00')).toBeVisible();
    console.log('✅ Ticket Type EDIT persisted successfully');

    console.log('🗑️ Deleting ticket type...');
    // Delete the ticket type
    await page.click('[data-testid="delete-ticket"]:has-text("Test Ticket EDITED")');
    await page.click('text=Confirm Delete');
    await page.waitForTimeout(1000);

    // Save and refresh
    await page.click('button:has-text("Save Event")');
    await page.waitForTimeout(2000);
    await page.reload();
    await page.waitForTimeout(2000);

    // Verify deletion persisted
    await expect(page.locator('text=Test Ticket EDITED')).not.toBeVisible();
    console.log('✅ Ticket Type DELETE persisted successfully');
  });

  test('Complete Volunteer Positions CRUD verification', async ({ page }) => {
    console.log('🔍 Testing Volunteer Positions CRUD operations...');

    // Find an event to edit
    await page.click('[data-testid="edit-event"]:first-child');
    await page.waitForTimeout(2000);

    console.log('📝 Adding new volunteer position...');
    // Add a new volunteer position
    await page.click('text=Add Volunteer Position');
    await page.fill('input[name="title"]', 'Test Volunteer CRUD');
    await page.fill('textarea[name="description"]', 'Test volunteer position for CRUD testing');
    await page.fill('input[name="slotsNeeded"]', '3');
    await page.click('text=Save Position');
    await page.waitForTimeout(1000);

    // Save event
    await page.click('button:has-text("Save Event")');
    await page.waitForTimeout(2000);

    // Refresh page to verify persistence
    await page.reload();
    await page.waitForTimeout(2000);

    // Verify volunteer position was saved
    await expect(page.locator('text=Test Volunteer CRUD')).toBeVisible();
    console.log('✅ Volunteer Position ADD persisted successfully');

    console.log('✏️ Editing volunteer position...');
    // Edit the volunteer position
    await page.click('[data-testid="edit-volunteer"]:has-text("Test Volunteer CRUD")');
    await page.fill('input[name="title"]', 'Test Volunteer EDITED');
    await page.fill('textarea[name="description"]', 'Edited volunteer position description');
    await page.fill('input[name="slotsNeeded"]', '5');
    await page.click('text=Save Position');
    await page.waitForTimeout(1000);

    // Save and refresh again
    await page.click('button:has-text("Save Event")');
    await page.waitForTimeout(2000);
    await page.reload();
    await page.waitForTimeout(2000);

    // Verify edit persisted
    await expect(page.locator('text=Test Volunteer EDITED')).toBeVisible();
    await expect(page.locator('text=Edited volunteer position description')).toBeVisible();
    console.log('✅ Volunteer Position EDIT persisted successfully');

    console.log('🗑️ Deleting volunteer position...');
    // Delete the volunteer position
    await page.click('[data-testid="delete-volunteer"]:has-text("Test Volunteer EDITED")');
    await page.click('text=Confirm Delete');
    await page.waitForTimeout(1000);

    // Save and refresh
    await page.click('button:has-text("Save Event")');
    await page.waitForTimeout(2000);
    await page.reload();
    await page.waitForTimeout(2000);

    // Verify deletion persisted
    await expect(page.locator('text=Test Volunteer EDITED')).not.toBeVisible();
    console.log('✅ Volunteer Position DELETE persisted successfully');
  });

  test('Teacher Changes Persistence verification', async ({ page }) => {
    console.log('🔍 Testing Teacher changes persistence...');

    // Find an event to edit
    await page.click('[data-testid="edit-event"]:first-child');
    await page.waitForTimeout(2000);

    console.log('👩‍🏫 Changing teacher assignment...');
    // Change teacher
    await page.click('select[name="teacherId"]');
    await page.selectOption('select[name="teacherId"]', { index: 1 }); // Select different teacher
    await page.waitForTimeout(500);

    // Save event
    await page.click('button:has-text("Save Event")');
    await page.waitForTimeout(2000);

    // Refresh page to verify persistence
    await page.reload();
    await page.waitForTimeout(2000);

    // Verify teacher change persisted by checking selected option
    const selectedTeacher = await page.locator('select[name="teacherId"] option:checked').textContent();
    console.log(`✅ Teacher change persisted: ${selectedTeacher}`);

    expect(selectedTeacher).toBeTruthy();
    expect(selectedTeacher).not.toBe('Select a teacher...');
  });

  test('Browser console and network monitoring', async ({ page }) => {
    console.log('🔍 Monitoring browser console and network...');

    const consoleErrors: string[] = [];
    const networkErrors: string[] = [];

    // Monitor console errors
    page.on('console', msg => {
      if (msg.type() === 'error') {
        consoleErrors.push(msg.text());
        console.log('❌ Console Error:', msg.text());
      }
    });

    // Monitor network failures
    page.on('response', response => {
      if (!response.ok() && response.status() >= 400) {
        networkErrors.push(`${response.status()} ${response.url()}`);
        console.log('❌ Network Error:', response.status(), response.url());
      }
    });

    // Navigate to admin events and perform actions
    await page.goto('http://localhost:5173/admin/events');
    await page.waitForTimeout(3000);

    // Click on first event to edit
    if (await page.locator('[data-testid="edit-event"]').first().isVisible()) {
      await page.click('[data-testid="edit-event"]:first-child');
      await page.waitForTimeout(2000);

      // Try to save without changes to trigger any persistence logic
      await page.click('button:has-text("Save Event")');
      await page.waitForTimeout(2000);
    }

    console.log(`📊 Console Errors: ${consoleErrors.length}`);
    console.log(`📊 Network Errors: ${networkErrors.length}`);

    if (consoleErrors.length > 0) {
      console.log('Console Errors Details:', consoleErrors);
    }

    if (networkErrors.length > 0) {
      console.log('Network Errors Details:', networkErrors);
    }

    // Report clean slate or issues
    if (consoleErrors.length === 0 && networkErrors.length === 0) {
      console.log('✅ No console or network errors detected');
    } else {
      console.log('⚠️ Errors detected during testing');
    }
  });
});