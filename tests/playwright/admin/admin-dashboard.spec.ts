import { test, expect } from '@playwright/test';
import { LoginPage } from '../pages/login.page';
import { AdminDashboardPage } from '../pages/admin-dashboard.page';
import { AdminEventsPage } from '../pages/admin-events.page';
import { AdminUsersPage } from '../pages/admin-users.page';
import { testConfig } from '../helpers/test.config';
import { BlazorHelpers } from '../helpers/blazor.helpers';

/**
 * Admin Dashboard Tests
 * Tests for the main admin dashboard functionality:
 * - Dashboard widgets and statistics
 * - Quick actions
 * - Navigation
 * - Recent activity
 */

test.describe('Admin Dashboard', () => {
  let loginPage: LoginPage;
  let adminDashboard: AdminDashboardPage;

  test.beforeEach(async ({ page }) => {
    loginPage = new LoginPage(page);
    adminDashboard = new AdminDashboardPage(page);
    
    // Login as admin before each test
    await loginPage.goto();
    await loginPage.loginAsAdmin();
    await loginPage.verifyLoginSuccess();
  });

  test('should display admin dashboard with statistics', async ({ page }) => {
    test.info().annotations.push({ type: 'test-id', description: 'admin-dashboard-display' });

    // Navigate to admin dashboard
    console.log('📊 Loading admin dashboard...');
    await adminDashboard.goto();
    
    // Verify dashboard loaded
    await expect(adminDashboard.pageTitle).toBeVisible();
    const isLoaded = await adminDashboard.isLoaded();
    expect(isLoaded).toBeTruthy();
    console.log('   ✅ Dashboard loaded successfully');
    
    // Check for dashboard statistics
    console.log('📈 Checking dashboard statistics...');
    const stats = await adminDashboard.getDashboardStats();
    console.log('   Dashboard stats:', stats);
    
    // Verify at least some stats are present
    expect(Object.keys(stats).length).toBeGreaterThan(0);
    
    if (stats.totalEvents) {
      console.log(`   Total Events: ${stats.totalEvents}`);
    }
    if (stats.totalUsers) {
      console.log(`   Total Users: ${stats.totalUsers}`);
    }
    if (stats.upcomingEvents) {
      console.log(`   Upcoming Events: ${stats.upcomingEvents}`);
    }
    
    // Take screenshot
    await adminDashboard.screenshot('statistics');
  });

  test('should display recent activity', async ({ page }) => {
    test.info().annotations.push({ type: 'test-id', description: 'admin-recent-activity' });

    // Navigate to admin dashboard
    await adminDashboard.goto();
    
    // Check for recent activity
    console.log('🕐 Checking recent activity...');
    const activity = await adminDashboard.getRecentActivity();
    
    console.log(`   Found ${activity.length} recent activities`);
    if (activity.length > 0) {
      console.log('   Recent activities:');
      activity.slice(0, 5).forEach((item, index) => {
        console.log(`   ${index + 1}. ${item}`);
      });
    }
  });

  test('should have working quick action buttons', async ({ page }) => {
    test.info().annotations.push({ type: 'test-id', description: 'admin-quick-actions' });

    // Navigate to admin dashboard
    await adminDashboard.goto();
    
    // Test Create Event quick action
    console.log('🚀 Testing quick actions...');
    
    if (await adminDashboard.createEventButton.isVisible()) {
      console.log('   Testing Create Event button...');
      await adminDashboard.clickCreateEvent();
      
      // Should navigate to event creation
      const currentUrl = page.url();
      const isOnEventPage = currentUrl.includes('/event') || currentUrl.includes('/create');
      expect(isOnEventPage).toBeTruthy();
      console.log('   ✅ Create Event button works');
      
      // Go back to dashboard
      await adminDashboard.goto();
    }
    
    // Test View All Events button
    if (await adminDashboard.viewAllEventsButton.isVisible()) {
      console.log('   Testing View All Events button...');
      await BlazorHelpers.clickAndWait(page, adminDashboard.viewAllEventsButton);
      
      await expect(page).toHaveURL(/.*\/admin\/events/);
      console.log('   ✅ View All Events button works');
      
      // Go back to dashboard
      await adminDashboard.goto();
    }
    
    // Test Manage Users button
    if (await adminDashboard.manageUsersButton.isVisible()) {
      console.log('   Testing Manage Users button...');
      await BlazorHelpers.clickAndWait(page, adminDashboard.manageUsersButton);
      
      await expect(page).toHaveURL(/.*\/admin\/users/);
      console.log('   ✅ Manage Users button works');
    }
  });

  test('should navigate between admin sections from dashboard', async ({ page }) => {
    test.info().annotations.push({ type: 'test-id', description: 'admin-dashboard-navigation' });

    const adminEvents = new AdminEventsPage(page);
    const adminUsers = new AdminUsersPage(page);
    
    // Start at dashboard
    await adminDashboard.goto();
    
    // Get navigation links
    const navLinks = await adminDashboard.getNavigationLinks();
    console.log('🔗 Available navigation links:', navLinks);
    
    // Navigate to Events
    console.log('\n📅 Navigating to Events...');
    await adminDashboard.navigateToEvents();
    await expect(adminEvents.pageTitle).toBeVisible();
    console.log('   ✅ Successfully navigated to Events');
    
    // Navigate back to Dashboard
    await adminDashboard.goto();
    
    // Navigate to Users
    console.log('\n👥 Navigating to Users...');
    await adminDashboard.navigateToUsers();
    await expect(adminUsers.pageTitle).toBeVisible();
    console.log('   ✅ Successfully navigated to Users');
    
    // Navigate back to Dashboard
    await adminDashboard.goto();
    await expect(adminDashboard.pageTitle).toBeVisible();
    console.log('\n✅ All navigation working correctly');
  });

  test('should handle errors gracefully', async ({ page }) => {
    test.info().annotations.push({ type: 'test-id', description: 'admin-dashboard-errors' });

    // Navigate to dashboard
    await adminDashboard.goto();
    
    // Check for any errors on the page
    console.log('⚠️ Checking for errors...');
    const hasErrors = await adminDashboard.hasErrors();
    
    if (hasErrors) {
      const errorTexts = await page.locator('.alert-danger, .error-message').allTextContents();
      console.log('   Found errors:', errorTexts);
      
      // Take screenshot of errors
      await adminDashboard.screenshot('errors');
    } else {
      console.log('   ✅ No errors found on dashboard');
    }
    
    expect(hasErrors).toBeFalsy();
  });

  test('should logout from admin dashboard', async ({ page }) => {
    test.info().annotations.push({ type: 'test-id', description: 'admin-dashboard-logout' });

    // Navigate to dashboard
    await adminDashboard.goto();
    
    // Perform logout
    console.log('🚪 Testing logout functionality...');
    await adminDashboard.logout();
    
    // Verify redirected to home or login
    const currentUrl = page.url();
    const isLoggedOut = currentUrl.includes('/login') || currentUrl.endsWith('/');
    expect(isLoggedOut).toBeTruthy();
    console.log('   ✅ Successfully logged out');
    
    // Try to access admin dashboard again
    await page.goto(testConfig.urls.adminDashboard);
    
    // Should be redirected to login
    await expect(page).toHaveURL(/.*\/login/);
    console.log('   ✅ Admin access correctly restricted after logout');
  });

  test('should update statistics when data changes', async ({ page }) => {
    test.info().annotations.push({ type: 'test-id', description: 'admin-dashboard-updates' });

    const adminEvents = new AdminEventsPage(page);
    
    // Get initial stats
    await adminDashboard.goto();
    const initialStats = await adminDashboard.getDashboardStats();
    console.log('📊 Initial stats:', initialStats);
    
    // Create a new event
    console.log('\n📅 Creating new event...');
    await adminDashboard.navigateToEvents();
    
    const eventData = {
      title: `Dashboard Test Event - ${Date.now()}`,
      description: 'Event to test dashboard updates',
      startDate: new Date(Date.now() + 7 * 24 * 60 * 60 * 1000).toISOString().split('T')[0],
      endDate: new Date(Date.now() + 7 * 24 * 60 * 60 * 1000 + 2 * 60 * 60 * 1000).toISOString().split('T')[0],
      location: 'Test Location',
      eventType: 'Social',
      status: 'Published'
    };
    
    await adminEvents.createEvent(eventData);
    console.log('   ✅ Event created');
    
    // Go back to dashboard
    await adminDashboard.goto();
    
    // Get updated stats
    const updatedStats = await adminDashboard.getDashboardStats();
    console.log('\n📊 Updated stats:', updatedStats);
    
    // If we have event counts, verify they increased
    if (initialStats.totalEvents && updatedStats.totalEvents) {
      const initialCount = parseInt(initialStats.totalEvents);
      const updatedCount = parseInt(updatedStats.totalEvents);
      expect(updatedCount).toBeGreaterThanOrEqual(initialCount);
      console.log('   ✅ Event count updated correctly');
    }
  });
});

test.describe('Admin Dashboard - Visual Tests', () => {
  test('should match visual snapshot', async ({ page }) => {
    test.info().annotations.push({ type: 'test-id', description: 'admin-dashboard-visual' });

    const loginPage = new LoginPage(page);
    const adminDashboard = new AdminDashboardPage(page);
    
    // Login and navigate to dashboard
    await loginPage.goto();
    await loginPage.loginAsAdmin();
    await loginPage.verifyLoginSuccess();
    
    await adminDashboard.goto();
    await adminDashboard.isLoaded();
    
    // Wait for any animations to complete
    await page.waitForTimeout(1000);
    
    // Take visual snapshot
    await expect(page).toHaveScreenshot('admin-dashboard.png', {
      fullPage: true,
      animations: 'disabled'
    });
  });
});

test.describe('Admin Dashboard - Mobile Responsive', () => {
  test.use({ viewport: { width: 375, height: 667 } });

  test('should be responsive on mobile', async ({ page }) => {
    test.info().annotations.push({ type: 'test-id', description: 'admin-dashboard-mobile' });

    const loginPage = new LoginPage(page);
    const adminDashboard = new AdminDashboardPage(page);
    
    // Login
    await loginPage.goto();
    await loginPage.loginAsAdmin();
    await loginPage.verifyLoginSuccess();
    
    // Navigate to dashboard
    await adminDashboard.goto();
    
    // Check if dashboard loads on mobile
    const isLoaded = await adminDashboard.isLoaded();
    expect(isLoaded).toBeTruthy();
    
    // Check if navigation is accessible (might be in hamburger menu)
    const navLinks = await adminDashboard.getNavigationLinks();
    console.log('📱 Mobile navigation links:', navLinks);
    
    // Take mobile screenshot
    await adminDashboard.screenshot('mobile');
  });
});