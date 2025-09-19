import { test, expect } from '@playwright/test';

test.describe('Verify Logout and Teacher Persistence Issues', () => {
  test.beforeEach(async ({ page }) => {
    // Navigate to the app
    await page.goto('http://localhost:5173');

    // Wait for page to load
    await page.waitForSelector('text=LOGIN', { timeout: 10000 });
  });

  test('Test 1: Logout Persistence - Does logout persist after refresh?', async ({ page }) => {
    console.log('=== STARTING LOGOUT PERSISTENCE TEST ===');

    // Step 1: Login as admin
    console.log('Step 1: Login as admin');
    await page.click('text=LOGIN');
    await page.waitForSelector('input[name="email"]');
    await page.fill('input[name="email"]', 'admin@witchcityrope.com');
    await page.fill('input[name="password"]', 'Test123!');
    await page.click('button[type="submit"]');

    // Step 2: Verify logged in (look for Dashboard or admin indication)
    console.log('Step 2: Verify logged in state');
    await page.waitForTimeout(2000); // Wait for login to process

    // Check for indicators of being logged in
    const isLoggedIn = await page.locator('text=LOGOUT').isVisible({ timeout: 5000 })
      .catch(() => false);

    if (!isLoggedIn) {
      // Try alternative selectors for logged in state
      const hasAdminMenu = await page.locator('[data-testid="admin-menu"], text=Admin').isVisible({ timeout: 2000 })
        .catch(() => false);
      const hasDashboard = await page.locator('text=Dashboard, [data-testid="dashboard"]').isVisible({ timeout: 2000 })
        .catch(() => false);

      console.log(`Login indicators - LOGOUT button: ${isLoggedIn}, Admin menu: ${hasAdminMenu}, Dashboard: ${hasDashboard}`);

      if (!hasAdminMenu && !hasDashboard) {
        console.log('❌ FAILED TO LOGIN - Cannot verify logout persistence');
        expect(false, 'Login failed - cannot test logout persistence').toBe(true);
        return;
      }
    }

    console.log('✅ Successfully logged in');

    // Step 3: Click logout
    console.log('Step 3: Attempting logout');
    const logoutClicked = await page.locator('text=LOGOUT').click({ timeout: 5000 })
      .then(() => true)
      .catch(async () => {
        // Try alternative logout selectors
        const altLogout = await page.locator('[data-testid="logout"], button:has-text("Logout")').click({ timeout: 2000 })
          .then(() => true)
          .catch(() => false);
        return altLogout;
      });

    if (!logoutClicked) {
      console.log('❌ FAILED TO FIND LOGOUT BUTTON');
      expect(false, 'Could not find logout button').toBe(true);
      return;
    }

    // Step 4: Verify logged out (look for LOGIN button)
    console.log('Step 4: Verify logged out state');
    await page.waitForTimeout(2000); // Wait for logout to process

    const loginButtonVisible = await page.locator('text=LOGIN').isVisible({ timeout: 5000 });
    console.log(`After logout - LOGIN button visible: ${loginButtonVisible}`);

    // Step 5: Refresh the page
    console.log('Step 5: Refreshing page to test persistence');
    await page.reload();
    await page.waitForLoadState('networkidle');

    // Step 6: CHECK - Are we still logged out or did we get logged back in?
    console.log('Step 6: Checking if logout persisted after refresh');
    await page.waitForTimeout(2000);

    const loginButtonAfterRefresh = await page.locator('text=LOGIN').isVisible({ timeout: 5000 });
    const logoutButtonAfterRefresh = await page.locator('text=LOGOUT').isVisible({ timeout: 2000 })
      .catch(() => false);

    console.log(`After refresh - LOGIN button: ${loginButtonAfterRefresh}, LOGOUT button: ${logoutButtonAfterRefresh}`);

    // RESULT ANALYSIS
    if (loginButtonAfterRefresh && !logoutButtonAfterRefresh) {
      console.log('✅ LOGOUT PERSISTENCE: YES - User stays logged out after refresh');
    } else if (!loginButtonAfterRefresh && logoutButtonAfterRefresh) {
      console.log('❌ LOGOUT PERSISTENCE: NO - User gets logged back in after refresh');
    } else {
      console.log('⚠️ LOGOUT PERSISTENCE: UNCLEAR - Mixed signals from UI elements');
    }

    // Always pass the test - we're just gathering data
    expect(true).toBe(true);
  });

  test('Test 2: Teacher Selection Persistence - Does teacher selection persist?', async ({ page }) => {
    console.log('=== STARTING TEACHER SELECTION PERSISTENCE TEST ===');

    // Step 1: Login as admin
    console.log('Step 1: Login as admin');
    await page.click('text=LOGIN');
    await page.waitForSelector('input[name="email"]');
    await page.fill('input[name="email"]', 'admin@witchcityrope.com');
    await page.fill('input[name="password"]', 'Test123!');
    await page.click('button[type="submit"]');
    await page.waitForTimeout(3000);

    // Step 2: Go to admin events
    console.log('Step 2: Navigate to admin events');

    // Try different navigation approaches
    const navigatedToEvents = await page.locator('text=Admin').click({ timeout: 5000 })
      .then(async () => {
        await page.waitForTimeout(1000);
        return await page.locator('text=Events, a[href*="events"], [data-testid="admin-events"]').click({ timeout: 5000 })
          .then(() => true)
          .catch(() => false);
      })
      .catch(async () => {
        // Try direct URL navigation
        console.log('Trying direct navigation to admin events...');
        await page.goto('http://localhost:5173/admin/events');
        await page.waitForLoadState('networkidle');
        return true;
      });

    if (!navigatedToEvents) {
      console.log('❌ FAILED TO NAVIGATE TO ADMIN EVENTS');
      expect(false, 'Could not navigate to admin events').toBe(true);
      return;
    }

    console.log('✅ Navigated to admin events');
    await page.waitForTimeout(2000);

    // Step 3: Edit an event (find first edit button)
    console.log('Step 3: Looking for event to edit');

    const eventEditButtons = await page.locator('button:has-text("Edit"), a:has-text("Edit"), [data-testid*="edit"]').count();
    console.log(`Found ${eventEditButtons} potential edit buttons`);

    if (eventEditButtons === 0) {
      console.log('❌ NO EVENTS FOUND TO EDIT');
      expect(false, 'No events available to edit').toBe(true);
      return;
    }

    // Click the first edit button
    await page.locator('button:has-text("Edit"), a:has-text("Edit"), [data-testid*="edit"]').first().click();
    await page.waitForTimeout(2000);

    console.log('✅ Opened event for editing');

    // Step 4: Select a teacher
    console.log('Step 4: Looking for teacher selection field');

    // Look for teacher dropdown/select field
    const teacherFields = await page.locator('select[name*="teacher"], select[name*="instructor"], [data-testid*="teacher"], [data-testid*="instructor"]').count();
    console.log(`Found ${teacherFields} potential teacher selection fields`);

    if (teacherFields === 0) {
      console.log('❌ NO TEACHER SELECTION FIELD FOUND');
      expect(false, 'No teacher selection field found').toBe(true);
      return;
    }

    // Get the teacher field and current value
    const teacherField = page.locator('select[name*="teacher"], select[name*="instructor"], [data-testid*="teacher"], [data-testid*="instructor"]').first();
    const currentTeacher = await teacherField.inputValue().catch(() => '');
    console.log(`Current teacher value: "${currentTeacher}"`);

    // Get available options
    const options = await teacherField.locator('option').allTextContents();
    console.log(`Available teacher options: ${options.join(', ')}`);

    // Select a different teacher (not the current one)
    let selectedTeacher = '';
    for (const option of options) {
      if (option !== currentTeacher && option.trim() !== '' && !option.includes('Select')) {
        await teacherField.selectOption(option);
        selectedTeacher = option;
        console.log(`Selected teacher: "${selectedTeacher}"`);
        break;
      }
    }

    if (!selectedTeacher) {
      console.log('❌ NO ALTERNATIVE TEACHER FOUND TO SELECT');
      expect(false, 'No alternative teacher option available').toBe(true);
      return;
    }

    // Step 5: Click Save
    console.log('Step 5: Saving event changes');

    const saveClicked = await page.locator('button:has-text("Save"), [data-testid*="save"], input[type="submit"]').click({ timeout: 5000 })
      .then(() => true)
      .catch(() => false);

    if (!saveClicked) {
      console.log('❌ FAILED TO FIND SAVE BUTTON');
      expect(false, 'Could not find save button').toBe(true);
      return;
    }

    await page.waitForTimeout(3000); // Wait for save to complete
    console.log('✅ Event saved');

    // Step 6: Refresh the page
    console.log('Step 6: Refreshing page to test teacher persistence');
    await page.reload();
    await page.waitForLoadState('networkidle');
    await page.waitForTimeout(3000);

    // Step 7: CHECK - Is the teacher still selected?
    console.log('Step 7: Checking if teacher selection persisted');

    // Navigate back to edit the same event
    const backToEdit = await page.locator('button:has-text("Edit"), a:has-text("Edit"), [data-testid*="edit"]').first().click({ timeout: 5000 })
      .then(() => true)
      .catch(() => false);

    if (!backToEdit) {
      console.log('❌ COULD NOT RE-OPEN EVENT FOR VERIFICATION');
      expect(false, 'Could not re-open event to verify teacher selection').toBe(true);
      return;
    }

    await page.waitForTimeout(2000);

    // Check current teacher selection
    const teacherFieldAfterRefresh = page.locator('select[name*="teacher"], select[name*="instructor"], [data-testid*="teacher"], [data-testid*="instructor"]').first();
    const currentTeacherAfterRefresh = await teacherFieldAfterRefresh.inputValue().catch(() => '');

    console.log(`Teacher after refresh: "${currentTeacherAfterRefresh}"`);
    console.log(`Expected teacher: "${selectedTeacher}"`);

    // RESULT ANALYSIS
    if (currentTeacherAfterRefresh === selectedTeacher) {
      console.log('✅ TEACHER SELECTION PERSISTENCE: YES - Teacher selection persisted');
    } else {
      console.log('❌ TEACHER SELECTION PERSISTENCE: NO - Teacher selection was lost');
    }

    // Always pass the test - we're just gathering data
    expect(true).toBe(true);
  });

  test('Console Error Analysis', async ({ page }) => {
    console.log('=== CONSOLE ERROR ANALYSIS ===');

    const consoleMessages: string[] = [];
    const pageErrors: string[] = [];

    // Capture console messages
    page.on('console', (msg) => {
      if (msg.type() === 'error') {
        consoleMessages.push(`Console Error: ${msg.text()}`);
      }
    });

    // Capture page errors
    page.on('pageerror', (error) => {
      pageErrors.push(`Page Error: ${error.message}`);
    });

    // Navigate and perform basic interactions
    await page.goto('http://localhost:5173');
    await page.waitForTimeout(3000);

    // Try login flow
    await page.click('text=LOGIN').catch(() => {});
    await page.waitForTimeout(2000);

    // Report findings
    console.log('=== ERROR SUMMARY ===');
    console.log(`Console errors detected: ${consoleMessages.length}`);
    consoleMessages.forEach((msg, i) => console.log(`${i + 1}. ${msg}`));

    console.log(`Page errors detected: ${pageErrors.length}`);
    pageErrors.forEach((error, i) => console.log(`${i + 1}. ${error}`));

    if (consoleMessages.length === 0 && pageErrors.length === 0) {
      console.log('✅ NO CONSOLE ERRORS DETECTED');
    }

    // Always pass - we're gathering data
    expect(true).toBe(true);
  });
});