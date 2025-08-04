import { test, expect } from '@playwright/test';
import { LoginPage } from '../pages/login.page';
import { AdminDashboardPage } from '../pages/admin-dashboard.page';
import { AdminEventsPage } from '../pages/admin-events.page';
import { testConfig } from '../helpers/test.config';
import { BlazorHelpers } from '../helpers/blazor.helpers';

/**
 * Admin Access Tests
 * Converted from Puppeteer tests: test-admin-access-simple.js, test-admin-access-modernized.js
 * 
 * Tests admin authentication and authorization:
 * - Admin login functionality
 * - Access to admin dashboard
 * - Access to admin events management
 * - Authorization checks
 */

test.describe('Admin Access and Authorization', () => {
  let loginPage: LoginPage;
  let adminDashboard: AdminDashboardPage;
  let adminEvents: AdminEventsPage;

  test.beforeEach(async ({ page }) => {
    loginPage = new LoginPage(page);
    adminDashboard = new AdminDashboardPage(page);
    adminEvents = new AdminEventsPage(page);
  });

  test('should login as admin and access admin dashboard', async ({ page }) => {
    test.info().annotations.push({ type: 'test-id', description: 'admin-login-and-dashboard' });

    // Step 1: Navigate to login page
    console.log('1️⃣ Navigating to custom login page...');
    await loginPage.goto();
    
    // Verify login page loaded
    await expect(loginPage.pageTitle).toBeVisible();
    console.log('   Page title:', await loginPage.getPageTitle());
    
    // Step 2: Check page content and form inputs
    console.log('2️⃣ Checking page content...');
    await expect(loginPage.emailInput).toBeVisible();
    await expect(loginPage.passwordInput).toBeVisible();
    await expect(loginPage.signInButton).toBeVisible();
    
    console.log('   Found email input');
    console.log('   Found password input');
    console.log('   Found sign in button');
    
    // Step 3: Fill login form with admin credentials
    console.log('3️⃣ Filling custom login form...');
    await loginPage.login(testConfig.accounts.admin.email, testConfig.accounts.admin.password);
    
    console.log(`   Email filled: "${testConfig.accounts.admin.email}"`);
    console.log(`   Password filled: ********`);
    
    // Step 4: Verify successful login
    console.log('4️⃣ Verifying login success...');
    await loginPage.verifyLoginSuccess();
    
    const afterLoginUrl = page.url();
    console.log(`   After login URL: ${afterLoginUrl}`);
    expect(afterLoginUrl).not.toContain('/login');
    
    // Step 5: Test admin dashboard access
    console.log('5️⃣ Testing admin dashboard access...');
    await adminDashboard.goto();
    
    const dashboardUrl = page.url();
    console.log(`   Admin dashboard URL: ${dashboardUrl}`);
    
    // Check if we're redirected to login (authorization failed)
    if (dashboardUrl.includes('/login')) {
      console.log('❌ Redirected to login - authorization failed');
      throw new Error('Admin authorization failed');
    } else {
      console.log('✅ Successfully accessed admin dashboard');
      
      // Verify dashboard loaded
      await expect(adminDashboard.pageTitle).toBeVisible();
      const isLoaded = await adminDashboard.isLoaded();
      expect(isLoaded).toBeTruthy();
      
      // Check for dashboard statistics
      const stats = await adminDashboard.getDashboardStats();
      console.log('   Dashboard stats:', stats);
    }
    
    // Take screenshot for verification
    await adminDashboard.screenshot('access-test');
  });

  test('should access admin events page after login', async ({ page }) => {
    test.info().annotations.push({ type: 'test-id', description: 'admin-events-access' });

    // Login first
    await loginPage.goto();
    await loginPage.loginAsAdmin();
    await loginPage.verifyLoginSuccess();
    
    // Step 5: Test admin events access
    console.log('5️⃣ Testing admin events access...');
    await adminEvents.goto();
    
    const adminUrl = page.url();
    console.log(`   Admin page URL: ${adminUrl}`);
    
    // Check if we're redirected to login (authorization failed)
    if (adminUrl.includes('/login')) {
      console.log('❌ Redirected to login - authorization failed');
      throw new Error('Admin events authorization failed');
    } else {
      console.log('✅ Successfully accessed admin page');
      
      // Step 6: Check for create event button
      console.log('6️⃣ Looking for create event button...');
      
      const buttonExists = await adminEvents.canCreateEvents();
      console.log(`   Create button exists: ${buttonExists}`);
      
      if (buttonExists) {
        console.log('✅ Create event button found!');
        await expect(adminEvents.createEventButton).toBeVisible();
      } else {
        console.log('❌ Create event button not found');
        
        // Debug: check what buttons do exist
        const availableActions = await adminEvents.getAvailableActions();
        console.log('   Available buttons:');
        availableActions.forEach((action, i) => {
          console.log(`   ${i + 1}. "${action}"`);
        });
        
        throw new Error('Create event button not found');
      }
    }
    
    // Take screenshot
    await adminEvents.screenshot('access-test');
  });

  test('should redirect non-admin users to login', async ({ page }) => {
    test.info().annotations.push({ type: 'test-id', description: 'admin-auth-redirect' });

    // Try to access admin dashboard without login
    console.log('🔒 Testing admin authorization without login...');
    
    await page.goto(testConfig.urls.adminDashboard);
    await page.waitForURL(/.*\/(login|$)/, { timeout: 10000 });
    
    const currentUrl = page.url();
    console.log(`   Redirected to: ${currentUrl}`);
    
    expect(currentUrl).toContain('/login');
    console.log('✅ Correctly redirected to login page');
  });

  test('should show all admin navigation links after login', async ({ page }) => {
    test.info().annotations.push({ type: 'test-id', description: 'admin-navigation-links' });

    // Login as admin
    await loginPage.goto();
    await loginPage.loginAsAdmin();
    await loginPage.verifyLoginSuccess();
    
    // Navigate to admin dashboard
    await adminDashboard.goto();
    
    // Check navigation links
    console.log('🔗 Checking admin navigation links...');
    
    const navLinks = await adminDashboard.getNavigationLinks();
    console.log('   Found navigation links:', navLinks);
    
    // Verify expected links exist
    const expectedLinks = ['Events', 'Users', 'Dashboard'];
    for (const link of expectedLinks) {
      const hasLink = navLinks.some(nav => nav.includes(link));
      expect(hasLink).toBeTruthy();
      console.log(`   ✅ Found ${link} link`);
    }
  });

  test('should navigate between admin sections', async ({ page }) => {
    test.info().annotations.push({ type: 'test-id', description: 'admin-section-navigation' });

    // Login as admin
    await loginPage.goto();
    await loginPage.loginAsAdmin();
    await loginPage.verifyLoginSuccess();
    
    // Navigate to admin dashboard
    await adminDashboard.goto();
    
    // Test navigation to Events
    console.log('📋 Testing navigation to Events...');
    await adminDashboard.navigateToEvents();
    await expect(page).toHaveURL(/.*\/admin\/events/);
    await expect(adminEvents.pageTitle).toBeVisible();
    console.log('   ✅ Successfully navigated to Events');
    
    // Navigate back to dashboard
    await adminDashboard.goto();
    
    // Test navigation to Users
    console.log('👥 Testing navigation to Users...');
    await adminDashboard.navigateToUsers();
    await expect(page).toHaveURL(/.*\/admin\/users/);
    console.log('   ✅ Successfully navigated to Users');
  });

  test('should maintain admin session across page refreshes', async ({ page }) => {
    test.info().annotations.push({ type: 'test-id', description: 'admin-session-persistence' });

    // Login as admin
    await loginPage.goto();
    await loginPage.loginAsAdmin();
    await loginPage.verifyLoginSuccess();
    
    // Navigate to admin events
    await adminEvents.goto();
    await expect(adminEvents.createEventButton).toBeVisible();
    
    // Refresh the page
    console.log('🔄 Refreshing page to test session persistence...');
    await page.reload();
    await BlazorHelpers.waitForBlazorReady(page);
    
    // Verify still on admin events page
    const currentUrl = page.url();
    expect(currentUrl).toContain('/admin/events');
    expect(currentUrl).not.toContain('/login');
    
    // Verify create button still visible
    await expect(adminEvents.createEventButton).toBeVisible();
    console.log('   ✅ Admin session persisted after refresh');
  });
});

test.describe('Admin Role Verification', () => {
  test('should verify member cannot access admin pages', async ({ page }) => {
    test.info().annotations.push({ type: 'test-id', description: 'member-admin-access-denied' });

    const loginPage = new LoginPage(page);
    const adminEvents = new AdminEventsPage(page);
    
    // Login as member
    await loginPage.goto();
    await loginPage.loginAsMember();
    await loginPage.verifyLoginSuccess();
    
    // Try to access admin events
    console.log('🚫 Testing member access to admin pages...');
    await page.goto(testConfig.urls.adminEvents);
    
    // Should be redirected or show access denied
    const currentUrl = page.url();
    const hasAccess = await adminEvents.hasAccess();
    
    expect(hasAccess).toBeFalsy();
    console.log(`   Current URL: ${currentUrl}`);
    console.log('   ✅ Member correctly denied admin access');
  });
});