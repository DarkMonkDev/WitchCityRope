import { Page, Locator } from '@playwright/test';
import { BlazorHelpers } from '../helpers/blazor.helpers';
import { testConfig } from '../helpers/test.config';

/**
 * Page Object Model for the Admin Dashboard
 * Encapsulates all admin dashboard interactions and navigation
 */
export class AdminDashboardPage {
  readonly page: Page;
  
  // Page elements
  readonly pageTitle: Locator;
  readonly dashboardStats: Locator;
  readonly navigationMenu: Locator;
  readonly userDropdown: Locator;
  
  // Navigation links
  readonly eventsLink: Locator;
  readonly usersLink: Locator;
  readonly reportsLink: Locator;
  readonly settingsLink: Locator;
  
  // Quick action buttons
  readonly createEventButton: Locator;
  readonly viewAllEventsButton: Locator;
  readonly manageUsersButton: Locator;
  
  // Dashboard widgets
  readonly totalEventsWidget: Locator;
  readonly totalUsersWidget: Locator;
  readonly upcomingEventsWidget: Locator;
  readonly recentActivityWidget: Locator;

  constructor(page: Page) {
    this.page = page;
    
    // Initialize locators
    this.pageTitle = page.locator('h1:has-text("Admin Dashboard"), .page-title:has-text("Admin")').first();
    this.dashboardStats = page.locator('.dashboard-stats, .wcr-stats-container').first();
    this.navigationMenu = page.locator('.admin-nav, nav.admin-menu').first();
    this.userDropdown = page.locator('.user-dropdown, .wcr-user-menu').first();
    
    // Navigation links
    this.eventsLink = page.locator('a[href*="/admin/events"], .nav-link:has-text("Events")').first();
    this.usersLink = page.locator('a[href*="/admin/users"], .nav-link:has-text("Users")').first();
    this.reportsLink = page.locator('a[href*="/admin/reports"], .nav-link:has-text("Reports")').first();
    this.settingsLink = page.locator('a[href*="/admin/settings"], .nav-link:has-text("Settings")').first();
    
    // Quick action buttons
    this.createEventButton = page.locator('button:has-text("Create Event"), a:has-text("Create Event")').first();
    this.viewAllEventsButton = page.locator('a:has-text("View All Events"), button:has-text("All Events")').first();
    this.manageUsersButton = page.locator('a:has-text("Manage Users"), button:has-text("Manage Users")').first();
    
    // Dashboard widgets
    this.totalEventsWidget = page.locator('.widget:has-text("Total Events"), .stat-card:has-text("Events")').first();
    this.totalUsersWidget = page.locator('.widget:has-text("Total Users"), .stat-card:has-text("Users")').first();
    this.upcomingEventsWidget = page.locator('.widget:has-text("Upcoming Events"), .upcoming-events-list').first();
    this.recentActivityWidget = page.locator('.widget:has-text("Recent Activity"), .activity-feed').first();
  }

  /**
   * Navigate to the admin dashboard
   */
  async goto(): Promise<void> {
    // Ensure we're using a relative URL that works with the baseURL
    const adminUrl = testConfig.urls.adminDashboard.startsWith('/') ? testConfig.urls.adminDashboard : `/${testConfig.urls.adminDashboard}`;
    
    try {
      await this.page.goto(adminUrl);
      await BlazorHelpers.waitForBlazorReady(this.page);
    } catch (error) {
      console.error(`Failed to navigate to admin dashboard: ${adminUrl}`, error);
      // Try with full URL as fallback
      const fullUrl = `${testConfig.baseUrl}${adminUrl}`;
      console.log(`Attempting navigation with full URL: ${fullUrl}`);
      await this.page.goto(fullUrl);
      await BlazorHelpers.waitForBlazorReady(this.page);
    }
    
    // Wait for dashboard to load
    await this.pageTitle.waitFor({ state: 'visible', timeout: testConfig.timeouts.navigation });
  }

  /**
   * Navigate to admin dashboard with authentication check
   */
  async gotoWithAuth(): Promise<void> {
    await this.goto();
    
    // Check if redirected to login
    const currentUrl = this.page.url();
    if (currentUrl.includes('/login')) {
      throw new Error('Not authenticated - redirected to login page');
    }
  }

  /**
   * Check if user has admin access
   */
  async hasAdminAccess(): Promise<boolean> {
    try {
      await this.goto();
      const currentUrl = this.page.url();
      
      // If not redirected to login and dashboard title is visible
      if (!currentUrl.includes('/login')) {
        await this.pageTitle.waitFor({ state: 'visible', timeout: 5000 });
        return true;
      }
      return false;
    } catch {
      return false;
    }
  }

  /**
   * Navigate to Events management
   */
  async navigateToEvents(): Promise<void> {
    await BlazorHelpers.clickAndWait(this.page, this.eventsLink);
    await BlazorHelpers.waitForNavigation(this.page, /.*\/admin\/events/);
  }

  /**
   * Navigate to Users management
   */
  async navigateToUsers(): Promise<void> {
    await BlazorHelpers.clickAndWait(this.page, this.usersLink);
    await BlazorHelpers.waitForNavigation(this.page, /.*\/admin\/users/);
  }

  /**
   * Navigate to Reports
   */
  async navigateToReports(): Promise<void> {
    await BlazorHelpers.clickAndWait(this.page, this.reportsLink);
    await BlazorHelpers.waitForNavigation(this.page, /.*\/admin\/reports/);
  }

  /**
   * Navigate to Settings
   */
  async navigateToSettings(): Promise<void> {
    await BlazorHelpers.clickAndWait(this.page, this.settingsLink);
    await BlazorHelpers.waitForNavigation(this.page, /.*\/admin\/settings/);
  }

  /**
   * Click create event quick action
   */
  async clickCreateEvent(): Promise<void> {
    await BlazorHelpers.clickAndWait(this.page, this.createEventButton);
  }

  /**
   * Get dashboard statistics
   */
  async getDashboardStats(): Promise<{
    totalEvents?: string;
    totalUsers?: string;
    upcomingEvents?: string;
  }> {
    const stats: any = {};
    
    try {
      // Extract text from stat widgets
      if (await this.totalEventsWidget.isVisible()) {
        const eventText = await this.totalEventsWidget.textContent();
        const eventMatch = eventText?.match(/\d+/);
        if (eventMatch) stats.totalEvents = eventMatch[0];
      }
      
      if (await this.totalUsersWidget.isVisible()) {
        const userText = await this.totalUsersWidget.textContent();
        const userMatch = userText?.match(/\d+/);
        if (userMatch) stats.totalUsers = userMatch[0];
      }
      
      if (await this.upcomingEventsWidget.isVisible()) {
        const upcomingCount = await this.upcomingEventsWidget.locator('.event-item, li').count();
        stats.upcomingEvents = upcomingCount.toString();
      }
    } catch (error) {
      console.log('Error getting dashboard stats:', error);
    }
    
    return stats;
  }

  /**
   * Get recent activity items
   */
  async getRecentActivity(): Promise<string[]> {
    try {
      if (await this.recentActivityWidget.isVisible()) {
        return await this.recentActivityWidget.locator('.activity-item, li').allTextContents();
      }
    } catch {
      // Widget might not be present
    }
    return [];
  }

  /**
   * Check if dashboard is fully loaded
   */
  async isLoaded(): Promise<boolean> {
    try {
      await this.pageTitle.waitFor({ state: 'visible', timeout: 5000 });
      
      // Check for at least one widget
      const widgetsVisible = await this.page.locator('.widget, .stat-card, .dashboard-card').first().isVisible();
      
      return widgetsVisible;
    } catch {
      return false;
    }
  }

  /**
   * Take a screenshot for debugging
   */
  async screenshot(name: string): Promise<void> {
    await this.page.screenshot({ 
      path: `test-results/screenshots/admin-dashboard-${name}.png`,
      fullPage: true 
    });
  }

  /**
   * Logout from admin dashboard
   */
  async logout(): Promise<void> {
    // Click user dropdown
    await this.userDropdown.click();
    
    // Find and click logout
    const logoutLink = this.page.locator('a:has-text("Logout"), button:has-text("Logout")').first();
    await BlazorHelpers.clickAndWait(this.page, logoutLink);
    
    // Should be redirected to home or login
    await BlazorHelpers.waitForNavigation(this.page, /\/(login|$)/);
  }

  /**
   * Check for any error messages on dashboard
   */
  async hasErrors(): Promise<boolean> {
    const errorSelectors = [
      '.alert-danger',
      '.error-message',
      '.wcr-error',
      '[role="alert"]'
    ];
    
    for (const selector of errorSelectors) {
      if (await this.page.locator(selector).isVisible()) {
        return true;
      }
    }
    
    return false;
  }

  /**
   * Get all available navigation links
   */
  async getNavigationLinks(): Promise<string[]> {
    const links = await this.navigationMenu.locator('a').allTextContents();
    return links.filter(text => text.trim().length > 0);
  }
}