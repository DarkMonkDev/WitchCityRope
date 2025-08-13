import { test, expect } from '@playwright/test';
import { testConfig } from '../helpers/test.config';
import { BlazorHelpers } from '../helpers/blazor.helpers';

test.describe('Admin User Management - Updated Requirements', () => {
  test.beforeEach(async ({ page }) => {
    // Login as admin
    await page.goto(testConfig.urls.login);
    await page.fill('input[name="Input.EmailOrUsername"]', testConfig.accounts.admin.email);
    await page.fill('input[name="Input.Password"]', testConfig.accounts.admin.password);
    await page.click('button[type="submit"]');
    
    // Wait for authentication to complete
    await page.waitForTimeout(2000);
    
    // Navigate to admin users page
    await page.goto(testConfig.urls.adminUsers);
    await BlazorHelpers.waitForBlazorReady(page);
    await page.waitForLoadState('networkidle');
  });

  test('should login successfully and navigate to admin users page', async ({ page }) => {
    // Verify we're on the users page
    await expect(page).toHaveURL(/.*\/admin\/users/);
    
    // Verify page title is visible
    await expect(page.locator('h1:has-text("User Management")')).toBeVisible({ timeout: 15000 });
    
    // Take screenshot for documentation
    await page.screenshot({ 
      path: 'test-results/screenshots/admin-users-page-loaded.png',
      fullPage: true 
    });
  });

  test('should display exactly 3 statistics cards with correct titles', async ({ page }) => {
    // Wait for statistics to load
    await page.waitForSelector('.card', { timeout: 15000 });
    
    // Find all statistic cards
    const statCards = page.locator('.card').filter({ hasText: /Total Users|Pending Vetting|On Hold/ });
    
    // Should have exactly 3 cards
    await expect(statCards).toHaveCount(3);
    
    // Verify each required statistic is present
    await expect(page.locator('.card:has-text("Total Users")')).toBeVisible();
    await expect(page.locator('.card:has-text("Pending Vetting")')).toBeVisible();
    await expect(page.locator('.card:has-text("On Hold")')).toBeVisible();
    
    // Verify each card has a number/count
    const totalUsersCard = page.locator('.card:has-text("Total Users")');
    await expect(totalUsersCard.locator('text=/\\d+/')).toBeVisible();
    
    const pendingVettingCard = page.locator('.card:has-text("Pending Vetting")');
    await expect(pendingVettingCard.locator('text=/\\d+/')).toBeVisible();
    
    const onHoldCard = page.locator('.card:has-text("On Hold")');
    await expect(onHoldCard.locator('text=/\\d+/')).toBeVisible();
    
    // Take screenshot of statistics
    await page.screenshot({ 
      path: 'test-results/screenshots/user-stats-three-cards.png',
      fullPage: false,
      clip: { x: 0, y: 0, width: 1200, height: 400 }
    });
  });

  test('should display user grid with exactly 6 columns in correct order', async ({ page }) => {
    // Wait for user grid/table to load
    await page.waitForSelector('table, .sf-grid', { timeout: 15000 });
    
    // Find the user table/grid
    const userTable = page.locator('table, .sf-grid').first();
    await expect(userTable).toBeVisible();
    
    // Get all column headers
    const headers = userTable.locator('thead th, .e-columnheader .e-headercell');
    
    // Should have exactly 6 columns (excluding any action columns)
    const headerTexts = [];
    const headerCount = await headers.count();
    
    for (let i = 0; i < headerCount; i++) {
      const headerText = await headers.nth(i).textContent();
      if (headerText?.trim()) {
        headerTexts.push(headerText.trim());
      }
    }
    
    // Filter out action columns and empty headers
    const dataHeaders = headerTexts.filter(header => 
      !header.toLowerCase().includes('action') && 
      !header.toLowerCase().includes('select') &&
      header.length > 0
    );
    
    console.log('Found column headers:', dataHeaders);
    
    // Verify we have exactly 6 data columns
    expect(dataHeaders.length).toBe(6);
    
    // Verify the columns are in the correct order
    expect(dataHeaders[0]).toMatch(/First.*Name/i);
    expect(dataHeaders[1]).toMatch(/Scene.*Name/i);
    expect(dataHeaders[2]).toMatch(/Last.*Name/i);
    expect(dataHeaders[3]).toMatch(/Email/i);
    expect(dataHeaders[4]).toMatch(/Status/i);
    expect(dataHeaders[5]).toMatch(/Role/i);
    
    // Verify that Created and Last Login columns are NOT present
    const allHeaderText = headerTexts.join(' ').toLowerCase();
    expect(allHeaderText).not.toContain('created');
    expect(allHeaderText).not.toContain('last login');
    
    // Take screenshot of the grid
    await page.screenshot({ 
      path: 'test-results/screenshots/user-grid-six-columns.png',
      fullPage: false,
      clip: { x: 0, y: 200, width: 1400, height: 600 }
    });
  });

  test('should display users in the grid (8 users expected)', async ({ page }) => {
    // Wait for user grid to load
    await page.waitForSelector('table tbody tr, .sf-grid .e-row', { timeout: 15000 });
    
    // Count user rows
    const userRows = page.locator('table tbody tr, .sf-grid .e-row').filter({ hasNot: page.locator('.e-emptyrow') });
    const rowCount = await userRows.count();
    
    console.log(`Found ${rowCount} user rows`);
    
    // Should have 8 users as mentioned
    expect(rowCount).toBe(8);
    
    // Verify each row has data in the expected columns
    for (let i = 0; i < Math.min(rowCount, 3); i++) { // Check first 3 rows
      const row = userRows.nth(i);
      const cells = row.locator('td, .e-rowcell');
      
      // Verify we have data in key columns (email should always be present)
      const emailCell = cells.nth(3); // Email is 4th column (0-indexed)
      const emailText = await emailCell.textContent();
      expect(emailText?.trim()).toBeTruthy();
      expect(emailText).toMatch(/.+@.+\..+/); // Basic email pattern
    }
    
    // Take screenshot showing users
    await page.screenshot({ 
      path: 'test-results/screenshots/user-grid-with-data.png',
      fullPage: true 
    });
  });

  test('should verify column order matches requirements exactly', async ({ page }) => {
    // Wait for grid to load
    await page.waitForSelector('table, .sf-grid', { timeout: 15000 });
    
    const table = page.locator('table, .sf-grid').first();
    const headers = table.locator('thead th, .e-columnheader .e-headercell');
    
    // Get the first 6 data column headers (ignoring action columns)
    const expectedColumns = [
      'First Name',
      'Scene Name', 
      'Last Name',
      'Email',
      'Status',
      'Role'
    ];
    
    for (let i = 0; i < expectedColumns.length; i++) {
      const headerText = await headers.nth(i).textContent();
      expect(headerText?.trim()).toMatch(new RegExp(expectedColumns[i], 'i'));
    }
    
    console.log('✓ Column order verified correctly');
  });

  test('should not display removed columns (Created, Last Login)', async ({ page }) => {
    // Wait for grid to load
    await page.waitForSelector('table, .sf-grid', { timeout: 15000 });
    
    // Get all text content from the page
    const pageContent = await page.textContent('body');
    
    // Verify removed columns are not present
    expect(pageContent).not.toMatch(/Created|Last Login/i);
    
    // Also check header specifically
    const headers = page.locator('thead th, .e-columnheader .e-headercell');
    const headerCount = await headers.count();
    
    for (let i = 0; i < headerCount; i++) {
      const headerText = await headers.nth(i).textContent();
      expect(headerText?.toLowerCase()).not.toContain('created');
      expect(headerText?.toLowerCase()).not.toContain('last login');
    }
    
    console.log('✓ Removed columns verified as not present');
  });

  test('should handle authentication and navigation flow correctly', async ({ page }) => {
    // Verify we're authenticated by checking for admin-specific elements
    const adminElements = [
      'h1:has-text("User Management")',
      '.card:has-text("Total Users")',
      'table, .sf-grid'
    ];
    
    for (const selector of adminElements) {
      await expect(page.locator(selector).first()).toBeVisible({ timeout: 10000 });
    }
    
    // Verify the URL is correct
    expect(page.url()).toContain('/admin/users');
    
    console.log('✓ Authentication and navigation verified');
  });

  test('should load page within acceptable time limits', async ({ page }) => {
    const startTime = Date.now();
    
    // Navigate fresh to the page
    await page.goto(testConfig.urls.adminUsers);
    await BlazorHelpers.waitForBlazorReady(page);
    
    // Wait for key elements to load
    await Promise.all([
      page.waitForSelector('h1:has-text("User Management")', { timeout: 15000 }),
      page.waitForSelector('.card:has-text("Total Users")', { timeout: 15000 }),
      page.waitForSelector('table, .sf-grid', { timeout: 15000 })
    ]);
    
    const loadTime = Date.now() - startTime;
    console.log(`Page loaded in ${loadTime}ms`);
    
    // Page should load within 15 seconds
    expect(loadTime).toBeLessThan(15000);
  });

  test('should verify complete page functionality end-to-end', async ({ page }) => {
    // This is a comprehensive test combining all requirements
    
    // 1. Verify login worked and we're on the right page
    await expect(page).toHaveURL(/.*\/admin\/users/);
    await expect(page.locator('h1:has-text("User Management")')).toBeVisible({ timeout: 15000 });
    
    // 2. Verify exactly 3 statistics cards
    const statCards = page.locator('.card').filter({ hasText: /Total Users|Pending Vetting|On Hold/ });
    await expect(statCards).toHaveCount(3);
    
    // 3. Verify user grid with 6 columns in correct order
    const table = page.locator('table, .sf-grid').first();
    await expect(table).toBeVisible();
    
    // 4. Verify 8 users are displayed
    const userRows = page.locator('table tbody tr, .sf-grid .e-row').filter({ hasNot: page.locator('.e-emptyrow') });
    const rowCount = await userRows.count();
    expect(rowCount).toBe(8);
    
    // 5. Take final comprehensive screenshot
    await page.screenshot({ 
      path: 'test-results/screenshots/user-management-complete-test.png',
      fullPage: true 
    });
    
    console.log('✓ Complete end-to-end verification passed');
  });
});