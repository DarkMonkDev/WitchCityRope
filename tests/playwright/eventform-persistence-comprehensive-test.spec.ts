import { test, expect } from '@playwright/test';

test.describe('EventForm Persistence Fix Verification', () => {
  test.beforeEach(async ({ page }) => {
    // Navigate to the app
    await page.goto('http://localhost:5173');

    // Login as admin
    await page.click('text=LOGIN');
    await page.fill('input[name="email"]', 'admin@witchcityrope.com');
    await page.fill('input[name="password"]', 'Test123!');
    await page.click('button[type="submit"]');

    // Wait for login to complete
    await page.waitForSelector('text=Admin', { timeout: 10000 });

    // Navigate to admin events
    await page.click('text=Admin');
    await page.click('text=Events');
    await page.waitForLoadState('networkidle');
  });

  test('CRITICAL: Add session, save, refresh - session should persist', async ({ page }) => {
    console.log('🔍 Testing session persistence after save and refresh...');

    // Find and edit an event
    const editButtons = page.locator('button[data-testid*="edit-event"], button:has-text("Edit")');
    await expect(editButtons.first()).toBeVisible({ timeout: 10000 });
    await editButtons.first().click();

    // Wait for EventForm to load
    await page.waitForSelector('text=Setup', { timeout: 10000 });

    // Go to Setup tab
    await page.click('text=Setup');
    await page.waitForTimeout(1000);

    // Add a new session
    console.log('🔍 Adding new session...');
    const addSessionButton = page.locator('button:has-text("Add New Session")');
    await expect(addSessionButton).toBeVisible({ timeout: 5000 });
    await addSessionButton.click();

    // Fill session form
    const sessionModal = page.locator('.modal, [role="dialog"]').first();
    await expect(sessionModal).toBeVisible({ timeout: 5000 });

    await page.fill('input[name="title"]', 'TEST SESSION PERSISTENCE');
    await page.fill('input[name="instructor"]', 'Test Instructor');
    await page.fill('input[name="duration"]', '90');

    // Save the session modal
    await page.click('button:has-text("Save"), button:has-text("Add Session")');
    await page.waitForTimeout(2000);

    // Verify session appears in list
    console.log('🔍 Verifying session appears in list...');
    await expect(page.locator('text=TEST SESSION PERSISTENCE')).toBeVisible({ timeout: 5000 });

    // Monitor network for the main save request
    const savePromise = page.waitForResponse(response =>
      response.url().includes('/api/events/') && response.request().method() === 'PUT'
    );

    // Click the main Save button
    console.log('🔍 Clicking main Save button...');
    const mainSaveButton = page.locator('button:has-text("Save Event"), button:has-text("Save")').last();
    await mainSaveButton.click();

    // Wait for save request and check payload
    const saveResponse = await savePromise;
    console.log('🔍 Save response status:', saveResponse.status());

    if (saveResponse.status() === 200) {
      const responseBody = await saveResponse.json();
      console.log('✅ Save successful');
    } else {
      console.log('❌ Save failed with status:', saveResponse.status());
    }

    // Wait for save to complete
    await page.waitForTimeout(3000);

    // CRITICAL TEST: Refresh the page immediately
    console.log('🔍 CRITICAL: Refreshing page to test persistence...');
    await page.reload();
    await page.waitForLoadState('networkidle');

    // Navigate back to the same event
    const editButtonsAfterRefresh = page.locator('button[data-testid*="edit-event"], button:has-text("Edit")');
    await expect(editButtonsAfterRefresh.first()).toBeVisible({ timeout: 10000 });
    await editButtonsAfterRefresh.first().click();

    // Go to Setup tab
    await page.waitForSelector('text=Setup', { timeout: 10000 });
    await page.click('text=Setup');
    await page.waitForTimeout(2000);

    // VERIFY: Is the session still there?
    console.log('🔍 CRITICAL VERIFICATION: Checking if session persisted after refresh...');
    const persistedSession = page.locator('text=TEST SESSION PERSISTENCE');

    try {
      await expect(persistedSession).toBeVisible({ timeout: 5000 });
      console.log('✅ SUCCESS: Session persisted after save and refresh!');
    } catch (error) {
      console.log('❌ FAILURE: Session NOT found after refresh!');

      // Debug: Show what sessions are present
      const allSessions = await page.locator('[data-testid*="session"], .session-item, li').allTextContents();
      console.log('📋 Sessions found after refresh:', allSessions);

      throw new Error('Session did not persist after save and refresh');
    }
  });

  test('Add ticket type and verify persistence', async ({ page }) => {
    console.log('🔍 Testing ticket type persistence...');

    // Find and edit an event
    const editButtons = page.locator('button[data-testid*="edit-event"], button:has-text("Edit")');
    await editButtons.first().click();

    // Go to Setup tab
    await page.waitForSelector('text=Setup', { timeout: 10000 });
    await page.click('text=Setup');
    await page.waitForTimeout(1000);

    // Add a new ticket type
    const addTicketButton = page.locator('button:has-text("Add New Ticket Type"), button:has-text("Add Ticket")');
    await addTicketButton.click();

    // Fill ticket form
    await page.fill('input[name="name"]', 'TEST TICKET PERSISTENCE');
    await page.fill('input[name="price"]', '25.00');
    await page.fill('input[name="capacity"]', '50');

    // Save the ticket modal
    await page.click('button:has-text("Save"), button:has-text("Add Ticket")');
    await page.waitForTimeout(2000);

    // Verify ticket appears
    await expect(page.locator('text=TEST TICKET PERSISTENCE')).toBeVisible();

    // Save main form
    const mainSaveButton = page.locator('button:has-text("Save Event"), button:has-text("Save")').last();
    await mainSaveButton.click();
    await page.waitForTimeout(3000);

    // Refresh and verify
    await page.reload();
    await page.waitForLoadState('networkidle');

    const editButtonsAfterRefresh = page.locator('button[data-testid*="edit-event"], button:has-text("Edit")');
    await editButtonsAfterRefresh.first().click();
    await page.waitForSelector('text=Setup', { timeout: 10000 });
    await page.click('text=Setup');
    await page.waitForTimeout(2000);

    await expect(page.locator('text=TEST TICKET PERSISTENCE')).toBeVisible({ timeout: 5000 });
    console.log('✅ Ticket type persisted successfully');
  });

  test('Add volunteer position and verify persistence', async ({ page }) => {
    console.log('🔍 Testing volunteer position persistence...');

    // Find and edit an event
    const editButtons = page.locator('button[data-testid*="edit-event"], button:has-text("Edit")');
    await editButtons.first().click();

    // Go to Volunteers tab
    await page.waitForSelector('text=Volunteers', { timeout: 10000 });
    await page.click('text=Volunteers');
    await page.waitForTimeout(1000);

    // Add a new volunteer position
    const addVolunteerButton = page.locator('button:has-text("Add New Position"), button:has-text("Add Position")');
    await addVolunteerButton.click();

    // Fill volunteer form
    await page.fill('input[name="title"]', 'TEST VOLUNTEER PERSISTENCE');
    await page.fill('input[name="description"]', 'Test volunteer description');
    await page.fill('input[name="slots"]', '3');

    // Save the volunteer modal
    await page.click('button:has-text("Save"), button:has-text("Add Position")');
    await page.waitForTimeout(2000);

    // Verify position appears
    await expect(page.locator('text=TEST VOLUNTEER PERSISTENCE')).toBeVisible();

    // Save main form
    const mainSaveButton = page.locator('button:has-text("Save Event"), button:has-text("Save")').last();
    await mainSaveButton.click();
    await page.waitForTimeout(3000);

    // Refresh and verify
    await page.reload();
    await page.waitForLoadState('networkidle');

    const editButtonsAfterRefresh = page.locator('button[data-testid*="edit-event"], button:has-text("Edit")');
    await editButtonsAfterRefresh.first().click();
    await page.waitForSelector('text=Volunteers', { timeout: 10000 });
    await page.click('text=Volunteers');
    await page.waitForTimeout(2000);

    await expect(page.locator('text=TEST VOLUNTEER PERSISTENCE')).toBeVisible({ timeout: 5000 });
    console.log('✅ Volunteer position persisted successfully');
  });

  test('Delete session and verify persistence', async ({ page }) => {
    console.log('🔍 Testing session deletion persistence...');

    // First add a session to delete
    const editButtons = page.locator('button[data-testid*="edit-event"], button:has-text("Edit")');
    await editButtons.first().click();
    await page.waitForSelector('text=Setup', { timeout: 10000 });
    await page.click('text=Setup');

    // Add session first
    const addSessionButton = page.locator('button:has-text("Add New Session")');
    await addSessionButton.click();

    await page.fill('input[name="title"]', 'TEST DELETE SESSION');
    await page.fill('input[name="instructor"]', 'Delete Test');
    await page.fill('input[name="duration"]', '60');

    await page.click('button:has-text("Save"), button:has-text("Add Session")');
    await page.waitForTimeout(2000);

    // Verify session exists
    await expect(page.locator('text=TEST DELETE SESSION')).toBeVisible();

    // Delete the session
    const deleteButton = page.locator('button[data-testid*="delete"], button:has-text("Delete"), button:has-text("Remove")').first();
    if (await deleteButton.isVisible()) {
      await deleteButton.click();
      await page.waitForTimeout(1000);
    }

    // Save main form
    const mainSaveButton = page.locator('button:has-text("Save Event"), button:has-text("Save")').last();
    await mainSaveButton.click();
    await page.waitForTimeout(3000);

    // Refresh and verify deletion persisted
    await page.reload();
    await page.waitForLoadState('networkidle');

    const editButtonsAfterRefresh = page.locator('button[data-testid*="edit-event"], button:has-text("Edit")');
    await editButtonsAfterRefresh.first().click();
    await page.waitForSelector('text=Setup', { timeout: 10000 });
    await page.click('text=Setup');
    await page.waitForTimeout(2000);

    // Verify session is gone
    await expect(page.locator('text=TEST DELETE SESSION')).not.toBeVisible();
    console.log('✅ Session deletion persisted successfully');
  });

  test('Monitor network tab during save operation', async ({ page }) => {
    console.log('🔍 Monitoring network activity during save...');

    const networkRequests: any[] = [];

    // Capture all network requests
    page.on('request', request => {
      if (request.url().includes('/api/events/')) {
        networkRequests.push({
          method: request.method(),
          url: request.url(),
          postData: request.postData()
        });
      }
    });

    page.on('response', response => {
      if (response.url().includes('/api/events/')) {
        console.log(`📡 ${response.request().method()} ${response.url()} -> ${response.status()}`);
      }
    });

    // Edit event and add session
    const editButtons = page.locator('button[data-testid*="edit-event"], button:has-text("Edit")');
    await editButtons.first().click();
    await page.waitForSelector('text=Setup', { timeout: 10000 });
    await page.click('text=Setup');

    const addSessionButton = page.locator('button:has-text("Add New Session")');
    await addSessionButton.click();

    await page.fill('input[name="title"]', 'NETWORK MONITOR TEST');
    await page.fill('input[name="instructor"]', 'Network Test');
    await page.fill('input[name="duration"]', '45');

    await page.click('button:has-text("Save"), button:has-text("Add Session")');
    await page.waitForTimeout(2000);

    // Save main form and monitor payload
    console.log('🔍 Saving main form - monitoring payload...');
    const mainSaveButton = page.locator('button:has-text("Save Event"), button:has-text("Save")').last();
    await mainSaveButton.click();

    await page.waitForTimeout(5000);

    // Analyze network requests
    console.log('📋 Network requests captured:', networkRequests.length);
    const putRequest = networkRequests.find(req => req.method === 'PUT');

    if (putRequest) {
      console.log('✅ PUT request found');
      console.log('📦 Request URL:', putRequest.url);

      if (putRequest.postData) {
        try {
          const payload = JSON.parse(putRequest.postData);
          console.log('📦 Sessions in payload:', payload.sessions?.length || 0);
          console.log('📦 Ticket types in payload:', payload.ticketTypes?.length || 0);
          console.log('📦 Volunteer positions in payload:', payload.volunteerPositions?.length || 0);

          // Check if our new session is in the payload
          const hasNewSession = payload.sessions?.some((s: any) => s.title === 'NETWORK MONITOR TEST');
          console.log(hasNewSession ? '✅ New session found in payload' : '❌ New session NOT in payload');
        } catch (e) {
          console.log('❌ Could not parse payload JSON');
        }
      }
    } else {
      console.log('❌ No PUT request found');
    }
  });
});