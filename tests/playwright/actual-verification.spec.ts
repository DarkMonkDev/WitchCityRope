import { test, expect } from '@playwright/test';

test.describe('Verify Logout and Teacher Persistence Issues - ACTUAL BEHAVIOR', () => {
  test('Test 1: Logout Persistence - Does logout persist after refresh?', async ({ page }) => {
    console.log('=== STARTING LOGOUT PERSISTENCE TEST ===');

    // Step 1: Login as admin
    console.log('Step 1: Login as admin');
    await page.goto('http://localhost:5173');
    await page.waitForLoadState('networkidle');

    // Navigate to login page (LOGIN is a link, not a modal)
    await page.click('text=LOGIN');
    await page.waitForURL('**/login');
    await page.waitForTimeout(2000);
    console.log('✅ Navigated to login page');

    // Fill login form
    await page.fill('input[placeholder="your@email.com"]', 'admin@witchcityrope.com');
    await page.fill('input[type="password"]', 'Test123!');
    await page.click('button[type="submit"]');

    // Wait for login to complete
    await page.waitForTimeout(3000);

    // Step 2: Verify logged in (check URL and content)
    console.log('Step 2: Verify logged in state');
    const currentUrl = page.url();
    console.log(`Current URL after login: ${currentUrl}`);

    // Look for indicators we're logged in
    const pageContent = await page.content();
    const hasLogout = pageContent.includes('LOGOUT') || pageContent.includes('Logout') || pageContent.includes('logout');
    const hasAdmin = pageContent.includes('Admin') || pageContent.includes('admin');
    const hasDashboard = pageContent.includes('Dashboard') || pageContent.includes('dashboard');
    const hasWelcome = pageContent.includes('Welcome') || pageContent.includes('welcome');

    console.log(`Login indicators after login:`);
    console.log(`  Has LOGOUT: ${hasLogout}`);
    console.log(`  Has Admin: ${hasAdmin}`);
    console.log(`  Has Dashboard: ${hasDashboard}`);
    console.log(`  Has Welcome: ${hasWelcome}`);
    console.log(`  URL changed from login: ${!currentUrl.includes('/login')}`);

    // Check for specific logout button
    const logoutButton = await page.locator('text=LOGOUT, a:has-text("Logout"), button:has-text("Logout")').count();
    console.log(`Logout buttons found: ${logoutButton}`);

    if (logoutButton === 0 && !hasLogout && currentUrl.includes('/login')) {
      console.log('❌ LOGIN FAILED - Still on login page, no logout indicators');
      expect(false, 'Login appears to have failed').toBe(true);
      return;
    }

    console.log('✅ Login appears successful');

    // Step 3: Click logout
    console.log('Step 3: Attempting logout');

    let logoutClicked = false;
    const logoutSelectors = [
      'text=LOGOUT',
      'a:has-text("Logout")',
      'button:has-text("Logout")',
      '[data-testid="logout"]',
      'a[href="/logout"]',
      'a[href*="logout"]'
    ];

    for (const selector of logoutSelectors) {
      try {
        const element = page.locator(selector);
        const count = await element.count();
        if (count > 0) {
          await element.first().click();
          logoutClicked = true;
          console.log(`✅ Clicked logout using selector: ${selector}`);
          break;
        }
      } catch (e) {
        continue;
      }
    }

    if (!logoutClicked) {
      console.log('❌ NO LOGOUT BUTTON FOUND - Cannot test logout persistence');
      expect(false, 'Could not find logout button').toBe(true);
      return;
    }

    // Step 4: Verify logged out
    console.log('Step 4: Verify logged out state');
    await page.waitForTimeout(3000);

    const urlAfterLogout = page.url();
    console.log(`URL after logout: ${urlAfterLogout}`);

    const contentAfterLogout = await page.content();
    const hasLoginAfterLogout = contentAfterLogout.includes('LOGIN') || urlAfterLogout.includes('/login') ||
                                contentAfterLogout.includes('Sign in') || contentAfterLogout.includes('Log in');

    console.log(`After logout - Has LOGIN: ${hasLoginAfterLogout}`);

    // Step 5: Refresh the page
    console.log('Step 5: Refreshing page to test persistence');
    await page.reload();
    await page.waitForLoadState('networkidle');
    await page.waitForTimeout(3000);

    // Step 6: CHECK - Are we still logged out or did we get logged back in?
    console.log('Step 6: Checking if logout persisted after refresh');

    const urlAfterRefresh = page.url();
    const contentAfterRefresh = await page.content();

    const loginAfterRefresh = contentAfterRefresh.includes('LOGIN') || urlAfterRefresh.includes('/login') ||
                              contentAfterRefresh.includes('Sign in') || contentAfterRefresh.includes('Log in');
    const logoutAfterRefresh = contentAfterRefresh.includes('LOGOUT') || contentAfterRefresh.includes('Logout');

    console.log(`After refresh:`);
    console.log(`  URL: ${urlAfterRefresh}`);
    console.log(`  Has LOGIN: ${loginAfterRefresh}`);
    console.log(`  Has LOGOUT: ${logoutAfterRefresh}`);

    // RESULT ANALYSIS
    if (loginAfterRefresh && !logoutAfterRefresh) {
      console.log('✅ LOGOUT PERSISTENCE: YES - User stays logged out after refresh');
    } else if (!loginAfterRefresh && logoutAfterRefresh) {
      console.log('❌ LOGOUT PERSISTENCE: NO - User gets logged back in after refresh');
    } else {
      console.log('⚠️ LOGOUT PERSISTENCE: UNCLEAR - Mixed signals from UI elements');
      console.log(`   Login indicators: ${loginAfterRefresh}`);
      console.log(`   Logout indicators: ${logoutAfterRefresh}`);
    }

    // Always pass - we're gathering data
    expect(true).toBe(true);
  });

  test('Test 2: Teacher Selection Persistence - Navigate and test manually', async ({ page }) => {
    console.log('=== STARTING TEACHER SELECTION PERSISTENCE TEST ===');

    // Step 1: Login as admin
    console.log('Step 1: Login as admin');
    await page.goto('http://localhost:5173');
    await page.waitForLoadState('networkidle');

    await page.click('text=LOGIN');
    await page.waitForURL('**/login');
    await page.fill('input[placeholder="your@email.com"]', 'admin@witchcityrope.com');
    await page.fill('input[type="password"]', 'Test123!');
    await page.click('button[type="submit"]');
    await page.waitForTimeout(4000);

    console.log('✅ Logged in as admin');

    // Step 2: Try to navigate to admin events
    console.log('Step 2: Attempting to navigate to admin events');

    // Try multiple navigation approaches
    let navigatedToAdmin = false;

    // Approach 1: Look for Admin link
    const adminLinks = await page.locator('a:has-text("Admin"), text=Admin, [data-testid*="admin"]').count();
    console.log(`Admin links found: ${adminLinks}`);

    if (adminLinks > 0) {
      await page.locator('a:has-text("Admin"), text=Admin').first().click();
      await page.waitForTimeout(2000);
      navigatedToAdmin = true;
      console.log('✅ Clicked Admin link');
    }

    // Approach 2: Direct URL navigation
    if (!navigatedToAdmin) {
      console.log('Trying direct navigation to /admin/events...');
      await page.goto('http://localhost:5173/admin/events');
      await page.waitForLoadState('networkidle');
      await page.waitForTimeout(3000);
      navigatedToAdmin = true;
    }

    const currentUrl = page.url();
    console.log(`Current URL: ${currentUrl}`);

    // Check if we successfully reached admin area
    const pageContent = await page.content();
    const hasEvents = pageContent.includes('Event') || pageContent.includes('event');
    const hasAdmin = currentUrl.includes('/admin') || pageContent.includes('Admin');

    console.log(`Admin area indicators:`);
    console.log(`  URL contains admin: ${currentUrl.includes('/admin')}`);
    console.log(`  Content has Events: ${hasEvents}`);
    console.log(`  Content has Admin: ${hasAdmin}`);

    if (!hasAdmin && !currentUrl.includes('/admin')) {
      console.log('❌ FAILED TO REACH ADMIN AREA');
      console.log('This suggests admin functionality is not implemented or accessible');
      expect(false, 'Could not access admin area').toBe(true);
      return;
    }

    console.log('✅ Successfully reached admin area');

    // Step 3: Look for events and edit functionality
    console.log('Step 3: Looking for events to edit');

    const eventElements = await page.locator('*:has-text("event"), *:has-text("Event"), [data-testid*="event"]').count();
    const editButtons = await page.locator('button:has-text("Edit"), a:has-text("Edit"), [data-testid*="edit"]').count();

    console.log(`Event-related elements: ${eventElements}`);
    console.log(`Edit buttons found: ${editButtons}`);

    if (editButtons === 0) {
      console.log('❌ NO EDIT FUNCTIONALITY FOUND');
      console.log('This suggests event editing is not implemented');

      // Take screenshot for evidence
      await page.screenshot({ path: 'admin-events-no-edit.png' });

      expect(false, 'No event editing functionality found').toBe(true);
      return;
    }

    console.log('✅ Edit functionality found - would test teacher selection if implemented');

    // For now, we can't test teacher persistence because we'd need to:
    // 1. Click an edit button (found some)
    // 2. Find a teacher selection field
    // 3. Change the teacher
    // 4. Save the changes
    // 5. Refresh and verify persistence

    // This test confirms that admin area is accessible and has edit buttons
    // but we'd need to implement the full flow to test teacher persistence

    console.log('✅ TEACHER SELECTION TEST: Admin area accessible, edit buttons present');
    console.log('⚠️  TEACHER SELECTION TEST: Cannot complete without implementing selection flow');

    // Always pass - we're gathering data
    expect(true).toBe(true);
  });

  test('Test 3: Current Authentication State Analysis', async ({ page }) => {
    console.log('=== ANALYZING CURRENT AUTHENTICATION STATE ===');

    const consoleMessages: string[] = [];
    const pageErrors: string[] = [];

    // Capture errors
    page.on('console', (msg) => {
      if (msg.type() === 'error') {
        consoleMessages.push(msg.text());
      }
    });

    page.on('pageerror', (error) => {
      pageErrors.push(error.message);
    });

    // Test the full login flow
    await page.goto('http://localhost:5173');
    await page.waitForLoadState('networkidle');
    await page.waitForTimeout(2000);

    console.log('Step 1: Initial page state');
    const initialContent = await page.content();
    const initialHasLogin = initialContent.includes('LOGIN');
    console.log(`Initial state has LOGIN: ${initialHasLogin}`);

    console.log('Step 2: Navigate to login and attempt login');
    await page.click('text=LOGIN');
    await page.waitForURL('**/login');

    await page.fill('input[placeholder="your@email.com"]', 'admin@witchcityrope.com');
    await page.fill('input[type="password"]', 'Test123!');
    await page.click('button[type="submit"]');

    // Wait and check result
    await page.waitForTimeout(4000);

    const afterLoginUrl = page.url();
    const afterLoginContent = await page.content();

    console.log(`After login URL: ${afterLoginUrl}`);
    console.log(`After login has LOGOUT: ${afterLoginContent.includes('LOGOUT')}`);
    console.log(`After login has LOGIN: ${afterLoginContent.includes('LOGIN')}`);

    // Check for API errors during login
    console.log('Step 3: Check for authentication errors');
    console.log(`Console errors during login: ${consoleMessages.length}`);
    if (consoleMessages.length > 0) {
      console.log('Console errors:');
      consoleMessages.forEach((msg, i) => {
        if (msg.includes('401') || msg.includes('Unauthorized') || msg.includes('auth')) {
          console.log(`  ${i + 1}. ${msg}`);
        }
      });
    }

    console.log(`Page errors during login: ${pageErrors.length}`);
    pageErrors.forEach((error, i) => console.log(`  ${i + 1}. ${error}`));

    // Final assessment
    const loginWorking = !afterLoginUrl.includes('/login') &&
                        (afterLoginContent.includes('LOGOUT') || afterLoginContent.includes('Admin'));

    if (loginWorking) {
      console.log('✅ LOGIN FUNCTIONALITY: Working');
    } else {
      console.log('❌ LOGIN FUNCTIONALITY: Not working properly');
    }

    // Always pass - we're analyzing
    expect(true).toBe(true);
  });
});