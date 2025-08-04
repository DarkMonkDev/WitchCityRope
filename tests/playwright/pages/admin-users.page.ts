import { Page, Locator } from '@playwright/test';
import { BlazorHelpers } from '../helpers/blazor.helpers';
import { testConfig } from '../helpers/test.config';

/**
 * Page Object Model for the Admin Users Management page
 * Encapsulates all user management interactions
 */
export class AdminUsersPage {
  readonly page: Page;
  
  // Page elements
  readonly pageTitle: Locator;
  readonly searchInput: Locator;
  readonly searchButton: Locator;
  readonly createUserButton: Locator;
  readonly usersTable: Locator;
  readonly pagination: Locator;
  
  // Filter controls
  readonly roleFilter: Locator;
  readonly statusFilter: Locator;
  readonly sortDropdown: Locator;
  
  // Table headers
  readonly nameHeader: Locator;
  readonly emailHeader: Locator;
  readonly roleHeader: Locator;
  readonly statusHeader: Locator;
  readonly joinedHeader: Locator;
  readonly actionsHeader: Locator;
  
  // User modal/form elements
  readonly userModal: Locator;
  readonly firstNameInput: Locator;
  readonly lastNameInput: Locator;
  readonly emailInput: Locator;
  readonly roleSelect: Locator;
  readonly statusSelect: Locator;
  readonly saveUserButton: Locator;
  readonly cancelButton: Locator;
  readonly deleteUserButton: Locator;
  
  // Bulk actions
  readonly selectAllCheckbox: Locator;
  readonly bulkActionsDropdown: Locator;
  readonly applyBulkActionButton: Locator;

  constructor(page: Page) {
    this.page = page;
    
    // Initialize locators
    this.pageTitle = page.locator('h1:has-text("Users"), .page-title:has-text("User Management")').first();
    this.searchInput = page.locator('input[placeholder*="Search users"], #userSearch').first();
    this.searchButton = page.locator('button:has-text("Search"), button[type="submit"]').filter({ hasText: /search/i }).first();
    this.createUserButton = page.locator('button:has-text("Create User"), button:has-text("Add User"), a:has-text("New User")').first();
    this.usersTable = page.locator('table.users-table, .wcr-table, table').filter({ has: page.locator('th:has-text("Email")') }).first();
    this.pagination = page.locator('.pagination, .wcr-pagination').first();
    
    // Filter controls
    this.roleFilter = page.locator('select#roleFilter, select[name="role"], .role-filter').first();
    this.statusFilter = page.locator('select#statusFilter, select[name="status"], .status-filter').first();
    this.sortDropdown = page.locator('select#sortBy, .sort-dropdown').first();
    
    // Table headers
    this.nameHeader = page.locator('th:has-text("Name")').first();
    this.emailHeader = page.locator('th:has-text("Email")').first();
    this.roleHeader = page.locator('th:has-text("Role")').first();
    this.statusHeader = page.locator('th:has-text("Status")').first();
    this.joinedHeader = page.locator('th:has-text("Joined")').first();
    this.actionsHeader = page.locator('th:has-text("Actions")').first();
    
    // User modal/form elements
    this.userModal = page.locator('.modal:has-text("User"), .user-form-modal, #userModal').first();
    this.firstNameInput = page.locator('input[name="firstName"], #firstName').first();
    this.lastNameInput = page.locator('input[name="lastName"], #lastName').first();
    this.emailInput = page.locator('input[name="email"], #email').first();
    this.roleSelect = page.locator('select[name="role"], #userRole').first();
    this.statusSelect = page.locator('select[name="status"], #userStatus').first();
    this.saveUserButton = page.locator('button:has-text("Save"), button[type="submit"]:has-text("Create")').first();
    this.cancelButton = page.locator('button:has-text("Cancel"), button[data-dismiss="modal"]').first();
    this.deleteUserButton = page.locator('button:has-text("Delete User"), .btn-danger:has-text("Delete")').first();
    
    // Bulk actions
    this.selectAllCheckbox = page.locator('input[type="checkbox"].select-all, th input[type="checkbox"]').first();
    this.bulkActionsDropdown = page.locator('select#bulkActions, .bulk-actions-dropdown').first();
    this.applyBulkActionButton = page.locator('button:has-text("Apply"), button:has-text("Apply Action")').first();
  }

  /**
   * Navigate to the admin users page
   */
  async goto(): Promise<void> {
    await this.page.goto(testConfig.urls.adminUsers);
    await BlazorHelpers.waitForBlazorReady(this.page);
    
    // Wait for users table to load
    await this.pageTitle.waitFor({ state: 'visible', timeout: testConfig.timeouts.navigation });
  }

  /**
   * Search for users by query
   */
  async searchUsers(query: string): Promise<void> {
    await BlazorHelpers.fillAndWait(this.page, this.searchInput, query);
    await BlazorHelpers.clickAndWait(this.page, this.searchButton);
    
    // Wait for table to update
    await this.page.waitForTimeout(500);
  }

  /**
   * Filter users by role
   */
  async filterByRole(role: 'All' | 'Admin' | 'Teacher' | 'Member' | 'Guest'): Promise<void> {
    await this.roleFilter.selectOption(role);
    await this.page.waitForTimeout(500);
  }

  /**
   * Filter users by status
   */
  async filterByStatus(status: 'All' | 'Active' | 'Inactive' | 'Pending'): Promise<void> {
    await this.statusFilter.selectOption(status);
    await this.page.waitForTimeout(500);
  }

  /**
   * Sort users by field
   */
  async sortBy(field: 'name' | 'email' | 'joined' | 'role'): Promise<void> {
    await this.sortDropdown.selectOption(field);
    await this.page.waitForTimeout(500);
  }

  /**
   * Open create user modal
   */
  async openCreateUserModal(): Promise<void> {
    await BlazorHelpers.clickAndWait(this.page, this.createUserButton);
    await this.userModal.waitFor({ state: 'visible', timeout: 5000 });
  }

  /**
   * Create a new user
   */
  async createUser(userData: {
    firstName: string;
    lastName: string;
    email: string;
    role: string;
    status?: string;
  }): Promise<void> {
    await this.openCreateUserModal();
    
    // Fill user form
    await BlazorHelpers.fillAndWait(this.page, this.firstNameInput, userData.firstName);
    await BlazorHelpers.fillAndWait(this.page, this.lastNameInput, userData.lastName);
    await BlazorHelpers.fillAndWait(this.page, this.emailInput, userData.email);
    await this.roleSelect.selectOption(userData.role);
    
    if (userData.status) {
      await this.statusSelect.selectOption(userData.status);
    }
    
    // Save user
    await BlazorHelpers.clickAndWait(this.page, this.saveUserButton);
    
    // Wait for modal to close
    await this.userModal.waitFor({ state: 'hidden', timeout: 5000 });
  }

  /**
   * Edit user by email
   */
  async editUser(email: string): Promise<void> {
    const userRow = await this.getUserRow(email);
    if (!userRow) {
      throw new Error(`User with email ${email} not found`);
    }
    
    const editButton = userRow.locator('button:has-text("Edit"), a:has-text("Edit")').first();
    await BlazorHelpers.clickAndWait(this.page, editButton);
    
    // Wait for modal to open
    await this.userModal.waitFor({ state: 'visible', timeout: 5000 });
  }

  /**
   * Delete user by email
   */
  async deleteUser(email: string, confirm: boolean = true): Promise<void> {
    await this.editUser(email);
    
    // Click delete button in modal
    await BlazorHelpers.clickAndWait(this.page, this.deleteUserButton);
    
    if (confirm) {
      // Handle confirmation dialog
      const confirmButton = this.page.locator('button:has-text("Confirm"), button:has-text("Yes")').first();
      await BlazorHelpers.clickAndWait(this.page, confirmButton);
    }
    
    // Wait for modal to close
    await this.userModal.waitFor({ state: 'hidden', timeout: 5000 });
  }

  /**
   * Get user row by email
   */
  async getUserRow(email: string): Promise<Locator | null> {
    const rows = this.usersTable.locator('tbody tr');
    const count = await rows.count();
    
    for (let i = 0; i < count; i++) {
      const row = rows.nth(i);
      const rowText = await row.textContent();
      if (rowText?.includes(email)) {
        return row;
      }
    }
    
    return null;
  }

  /**
   * Get all users from current page
   */
  async getUsers(): Promise<Array<{
    name: string;
    email: string;
    role: string;
    status: string;
  }>> {
    const users = [];
    const rows = this.usersTable.locator('tbody tr');
    const count = await rows.count();
    
    for (let i = 0; i < count; i++) {
      const row = rows.nth(i);
      const cells = row.locator('td');
      
      users.push({
        name: await cells.nth(0).textContent() || '',
        email: await cells.nth(1).textContent() || '',
        role: await cells.nth(2).textContent() || '',
        status: await cells.nth(3).textContent() || ''
      });
    }
    
    return users;
  }

  /**
   * Get total user count
   */
  async getTotalUserCount(): Promise<number> {
    // Look for count in various places
    const countText = await this.page.locator('.user-count, .total-users, .results-count').first().textContent();
    const match = countText?.match(/\d+/);
    return match ? parseInt(match[0]) : 0;
  }

  /**
   * Navigate to specific page
   */
  async goToPage(pageNumber: number): Promise<void> {
    const pageLink = this.pagination.locator(`a:has-text("${pageNumber}")`).first();
    await BlazorHelpers.clickAndWait(this.page, pageLink);
  }

  /**
   * Select users for bulk action
   */
  async selectUsersForBulkAction(emails: string[]): Promise<void> {
    for (const email of emails) {
      const userRow = await this.getUserRow(email);
      if (userRow) {
        const checkbox = userRow.locator('input[type="checkbox"]').first();
        await checkbox.check();
      }
    }
  }

  /**
   * Apply bulk action
   */
  async applyBulkAction(action: 'delete' | 'activate' | 'deactivate' | 'changeRole'): Promise<void> {
    await this.bulkActionsDropdown.selectOption(action);
    await BlazorHelpers.clickAndWait(this.page, this.applyBulkActionButton);
    
    // Handle confirmation if needed
    const confirmButton = this.page.locator('button:has-text("Confirm"), button:has-text("Yes")');
    if (await confirmButton.isVisible()) {
      await BlazorHelpers.clickAndWait(this.page, confirmButton);
    }
  }

  /**
   * Check if user exists
   */
  async userExists(email: string): Promise<boolean> {
    const userRow = await this.getUserRow(email);
    return userRow !== null;
  }

  /**
   * Get user details
   */
  async getUserDetails(email: string): Promise<{
    name: string;
    email: string;
    role: string;
    status: string;
    joined?: string;
  } | null> {
    const userRow = await this.getUserRow(email);
    if (!userRow) return null;
    
    const cells = userRow.locator('td');
    return {
      name: await cells.nth(0).textContent() || '',
      email: await cells.nth(1).textContent() || '',
      role: await cells.nth(2).textContent() || '',
      status: await cells.nth(3).textContent() || '',
      joined: await cells.nth(4).textContent() || ''
    };
  }

  /**
   * Check if create user button is visible
   */
  async canCreateUsers(): Promise<boolean> {
    return await this.createUserButton.isVisible();
  }

  /**
   * Check if bulk actions are available
   */
  async hasBulkActions(): Promise<boolean> {
    return await this.bulkActionsDropdown.isVisible();
  }

  /**
   * Take a screenshot for debugging
   */
  async screenshot(name: string): Promise<void> {
    await this.page.screenshot({ 
      path: `test-results/screenshots/admin-users-${name}.png`,
      fullPage: true 
    });
  }
}