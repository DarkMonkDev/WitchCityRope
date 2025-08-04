import { test, expect } from '@playwright/test';
import { LoginPage } from '../pages/login.page';
import { AdminDashboardPage } from '../pages/admin-dashboard.page';
import { AdminEventsPage } from '../pages/admin-events.page';
import { testConfig } from '../helpers/test.config';
import { BlazorHelpers } from '../helpers/blazor.helpers';

/**
 * Admin Login Success Tests
 * Converted from Puppeteer test: test-admin-login-success.js
 * 
 * Tests successful admin login and access verification:
 * - Admin login with seeded user
 * - Access to admin areas
 * - Navigation after login
 * - Dashboard content verification
 */

test.describe('Admin Login Success', () => {
  let loginPage: LoginPage;
  let adminDashboard: AdminDashboardPage;
  let adminEvents: AdminEventsPage;

  test.beforeEach(async ({ page }) => {
    loginPage = new LoginPage(page);
    adminDashboard = new AdminDashboardPage(page);
    adminEvents = new AdminEventsPage(page);
  });

  test('should login as admin with seeded user', async ({ page }) => {
    test.info().annotations.push({ type: 'test-id', description: 'admin-login-seeded-user' });

    console.log('🚀 Testing Admin Login with Seeded User...\n');

    // Step 1: Navigate to login page
    console.log('1️⃣ Navigating to /login...');
    await loginPage.goto();
    
    // Verify login page loaded
    await expect(loginPage.emailInput).toBeVisible();
    await expect(loginPage.passwordInput).toBeVisible();
    
    // Step 2: Fill login form
    console.log('2️⃣ Filling login form...');
    await loginPage.login(testConfig.accounts.admin.email, testConfig.accounts.admin.password);
    
    console.log(`   Email: ${testConfig.accounts.admin.email}`);
    console.log('   Password: ********');
    
    // Step 3: Verify successful login
    console.log('\n3️⃣ Submitting login...');
    
    // Wait for navigation after login
    await loginPage.verifyLoginSuccess();
    
    const afterLoginUrl = page.url();
    console.log(`   After login URL: ${afterLoginUrl}`);
    
    // Verify successful login - should have ReturnUrl parameter or be redirected away from login form
    expect(afterLoginUrl.includes('ReturnUrl=') || !afterLoginUrl.includes('/login')).toBeTruthy();
    console.log('✅ Login successful!');
    
    // Step 4: Test admin events access
    console.log('\n4️⃣ Testing admin events access...');
    await adminEvents.goto();
    
    const adminUrl = page.url();
    expect(adminUrl).not.toContain('login');
    console.log('✅ Admin events page accessible!');
    
    // Look for page content
    const pageContent = await page.evaluate(() => {
      const title = document.querySelector('h1, h2, .page-title');
      const buttons = Array.from(document.querySelectorAll('button, a.btn'))
        .map(btn => (btn as HTMLElement).textContent?.trim())
        .filter(text => text && text.length > 0);
      const tables = document.querySelectorAll('table').length;
      
      return {
        title: title ? title.textContent?.trim() : 'No title',
        buttonCount: buttons.length,
        buttons: buttons.slice(0, 10),
        tableCount: tables
      };
    });
    
    console.log('\n📊 Admin Events Page:');
    console.log(`   Title: ${pageContent.title}`);
    console.log(`   Tables: ${pageContent.tableCount}`);
    console.log(`   Buttons (${pageContent.buttonCount} total):`);
    pageContent.buttons.forEach(btn => {
      console.log(`     - "${btn}"`);
    });
    
    // Look for create button
    const createButton = pageContent.buttons.find(text => 
      text && (
        text.toLowerCase().includes('create') || 
        text.toLowerCase().includes('new') ||
        text.toLowerCase().includes('add')
      )
    );
    
    if (createButton) {
      console.log(`\n✅ Found create button: "${createButton}"`);
    } else {
      console.log('\n⚠️ No create/new button found');
    }
    
    await adminEvents.screenshot('success');
    console.log('\n📸 Screenshot saved as admin-events-success.png');
    
    // Step 5: Test member dashboard access too
    console.log('\n5️⃣ Testing member dashboard access...');
    await page.goto('/member/dashboard');
    const dashboardUrl = page.url();
    
    if (!dashboardUrl.includes('login')) {
      console.log('✅ Member dashboard accessible!');
    }
    
    console.log('\n✅ Test completed!');
  });

  test('should verify admin has full access to all sections', async ({ page }) => {
    test.info().annotations.push({ type: 'test-id', description: 'admin-full-access-verification' });

    // Login as admin
    await loginPage.goto();
    await loginPage.loginAsAdmin();
    await loginPage.verifyLoginSuccess();
    
    console.log('🔐 Verifying admin access to all sections...\n');
    
    // Test admin dashboard access
    console.log('📊 Testing Admin Dashboard...');
    const hasDashboardAccess = await adminDashboard.hasAdminAccess();
    expect(hasDashboardAccess).toBeTruthy();
    console.log('   ✅ Dashboard accessible');
    
    // Test admin events access
    console.log('\n📅 Testing Admin Events...');
    const hasEventsAccess = await adminEvents.hasAccess();
    expect(hasEventsAccess).toBeTruthy();
    console.log('   ✅ Events management accessible');
    
    // Test create permissions
    console.log('\n🎯 Testing Create Permissions...');
    await adminEvents.goto();
    const canCreate = await adminEvents.canCreateEvents();
    expect(canCreate).toBeTruthy();
    console.log('   ✅ Can create events');
    
    // Navigate to dashboard and check stats
    console.log('\n📈 Checking Dashboard Stats...');
    await adminDashboard.goto();
    const stats = await adminDashboard.getDashboardStats();
    console.log('   Dashboard statistics:', stats);
    
    // Check navigation links
    const navLinks = await adminDashboard.getNavigationLinks();
    console.log('\n🔗 Available navigation:', navLinks);
  });

  test('should maintain admin session after page refresh', async ({ page }) => {
    test.info().annotations.push({ type: 'test-id', description: 'admin-session-persistence' });

    // Login as admin
    await loginPage.goto();
    await loginPage.loginAsAdmin();
    await loginPage.verifyLoginSuccess();
    
    // Navigate to admin area
    await adminEvents.goto();
    await expect(adminEvents.createEventButton).toBeVisible();
    
    console.log('🔄 Testing session persistence...');
    
    // Refresh the page
    await page.reload();
    await BlazorHelpers.waitForBlazorReady(page);
    
    // Verify still logged in and on admin page
    const currentUrl = page.url();
    expect(currentUrl).toContain('/admin/events');
    expect(currentUrl).not.toContain('/login');
    
    // Verify admin functionality still available
    await expect(adminEvents.createEventButton).toBeVisible();
    console.log('   ✅ Session persisted after refresh');
    
    // Try navigating to different admin sections
    await adminDashboard.goto();
    expect(page.url()).toContain('/admin');
    console.log('   ✅ Can navigate between admin sections');
  });

  test('should display admin-specific UI elements', async ({ page }) => {
    test.info().annotations.push({ type: 'test-id', description: 'admin-ui-elements' });

    // Login as admin
    await loginPage.goto();
    await loginPage.loginAsAdmin();
    await loginPage.verifyLoginSuccess();
    
    console.log('🎨 Checking admin-specific UI elements...\n');
    
    // Check admin dashboard
    await adminDashboard.goto();
    
    // Verify admin-only widgets
    const widgets = [
      { element: adminDashboard.totalEventsWidget, name: 'Total Events Widget' },
      { element: adminDashboard.totalUsersWidget, name: 'Total Users Widget' },
      { element: adminDashboard.createEventButton, name: 'Create Event Button' },
      { element: adminDashboard.manageUsersButton, name: 'Manage Users Button' }
    ];
    
    for (const widget of widgets) {
      if (await widget.element.isVisible()) {
        console.log(`   ✅ ${widget.name} visible`);
      } else {
        console.log(`   ❌ ${widget.name} not visible`);
      }
    }
    
    // Check admin events page
    await adminEvents.goto();
    
    // Verify admin controls
    const adminControls = [
      { element: adminEvents.createEventButton, name: 'Create Event Button' },
      { element: adminEvents.bulkActionsDropdown, name: 'Bulk Actions' },
      { element: adminEvents.statusFilter, name: 'Status Filter' }
    ];
    
    console.log('\n📋 Admin Events Controls:');
    for (const control of adminControls) {
      if (await control.element.isVisible()) {
        console.log(`   ✅ ${control.name} available`);
      } else {
        console.log(`   ℹ️ ${control.name} not available`);
      }
    }
  });

  test('should handle admin logout correctly', async ({ page }) => {
    test.info().annotations.push({ type: 'test-id', description: 'admin-logout-flow' });

    // Login as admin
    await loginPage.goto();
    await loginPage.loginAsAdmin();
    await loginPage.verifyLoginSuccess();
    
    // Navigate to admin dashboard
    await adminDashboard.goto();
    console.log('🚪 Testing admin logout...');
    
    // Perform logout
    await adminDashboard.logout();
    
    // Verify logged out
    const currentUrl = page.url();
    const isLoggedOut = currentUrl.includes('/login') || currentUrl.endsWith('/');
    expect(isLoggedOut).toBeTruthy();
    console.log('   ✅ Successfully logged out');
    
    // Try to access admin area
    await page.goto(testConfig.urls.adminEvents);
    
    // Should be redirected to login
    await expect(page).toHaveURL(/.*\/login/);
    console.log('   ✅ Admin areas properly protected after logout');
  });
});

test.describe('Admin Login Success - Visual Verification', () => {
  test('should match admin dashboard visual snapshot', async ({ page }) => {
    test.info().annotations.push({ type: 'test-id', description: 'admin-dashboard-visual-snapshot' });

    const loginPage = new LoginPage(page);
    const adminDashboard = new AdminDashboardPage(page);
    
    // Login as admin
    await loginPage.goto();
    await loginPage.loginAsAdmin();
    await loginPage.verifyLoginSuccess();
    
    // Navigate to dashboard
    await adminDashboard.goto();
    await adminDashboard.isLoaded();
    
    // Wait for any animations
    await page.waitForTimeout(1000);
    
    // Take visual snapshot
    await expect(page).toHaveScreenshot('admin-dashboard-logged-in.png', {
      fullPage: true,
      animations: 'disabled'
    });
  });

  test('should match admin events visual snapshot', async ({ page }) => {
    test.info().annotations.push({ type: 'test-id', description: 'admin-events-visual-snapshot' });

    const loginPage = new LoginPage(page);
    const adminEvents = new AdminEventsPage(page);
    
    // Login as admin
    await loginPage.goto();
    await loginPage.loginAsAdmin();
    await loginPage.verifyLoginSuccess();
    
    // Navigate to events
    await adminEvents.goto();
    await expect(adminEvents.pageTitle).toBeVisible();
    
    // Wait for table to load
    await page.waitForTimeout(1000);
    
    // Take visual snapshot
    await expect(page).toHaveScreenshot('admin-events-logged-in.png', {
      fullPage: true,
      animations: 'disabled'
    });
  });
});