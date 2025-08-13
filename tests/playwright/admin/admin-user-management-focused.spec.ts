import { test, expect } from '@playwright/test';
import { AdminUsersPage } from '../pages/admin-users.page';
import { testConfig } from '../helpers/test.config';

test.describe('Admin User Management - Focused Tests', () => {
  let adminUsersPage: AdminUsersPage;

  test.beforeEach(async ({ page }) => {
    adminUsersPage = new AdminUsersPage(page);
    
    // Login as admin
    await page.goto(testConfig.urls.login);
    await page.fill('input[name="Input.EmailOrUsername"]', testConfig.accounts.admin.email);
    await page.fill('input[name="Input.Password"]', testConfig.accounts.admin.password);
    await page.click('button[type="submit"]');
    
    // Wait for login to complete
    await page.waitForTimeout(3000);
    
    // Navigate to admin users page
    await adminUsersPage.goto();
  });

  test('login and navigation verification', async ({ page }) => {
    // Verify we're on the correct page
    await expect(page).toHaveURL(/.*\/admin\/users/);
    await expect(adminUsersPage.pageTitle).toBeVisible({ timeout: 15000 });
    
    console.log('✅ Successfully logged in and navigated to admin/users page');
  });

  test('verify 3 statistics cards', async ({ page }) => {
    // Wait for stats to load
    await page.waitForTimeout(2000);
    
    const statsCount = await adminUsersPage.getStatsCardsCount();
    console.log(`Found ${statsCount} statistics cards`);
    
    expect(statsCount).toBe(3);
    
    // Get the statistics data
    const stats = await adminUsersPage.getStatisticsData();
    console.log('Statistics found:', stats);
    
    // Verify we have the required statistics
    const titles = stats.map(s => s.title);
    expect(titles).toContain('Total Users');
    expect(titles).toContain('Pending Vetting');
    expect(titles).toContain('On Hold');
    
    console.log('✅ Successfully verified 3 statistics cards');
  });

  test('verify 6 columns in correct order', async ({ page }) => {
    // Wait for grid to load
    await page.waitForTimeout(2000);
    
    const headers = await adminUsersPage.getColumnHeaders();
    console.log('Column headers found:', headers);
    
    expect(headers.length).toBe(6);
    
    // Verify column order
    const isOrderCorrect = await adminUsersPage.verifyColumnOrder();
    expect(isOrderCorrect).toBe(true);
    
    console.log('✅ Successfully verified 6 columns in correct order');
  });

  test('verify 8 users are displayed', async ({ page }) => {
    // Wait for users to load
    await page.waitForTimeout(3000);
    
    const userCount = await adminUsersPage.getUserRowCount();
    console.log(`Found ${userCount} users in the grid`);
    
    expect(userCount).toBe(8);
    
    console.log('✅ Successfully verified 8 users are displayed');
  });

  test('verify removed columns are not present', async ({ page }) => {
    // Check that old columns are not present
    const hasRemovedColumns = await adminUsersPage.hasRemovedColumns();
    expect(hasRemovedColumns).toBe(false);
    
    console.log('✅ Successfully verified removed columns are not present');
  });

  test('complete functionality test', async ({ page }) => {
    console.log('🔍 Running complete functionality verification...');
    
    // Wait for everything to load
    await page.waitForTimeout(3000);
    
    // 1. Verify page loads correctly
    await expect(page).toHaveURL(/.*\/admin\/users/);
    await expect(adminUsersPage.pageTitle).toBeVisible();
    console.log('✓ Page navigation verified');
    
    // 2. Verify statistics
    const statsCount = await adminUsersPage.getStatsCardsCount();
    expect(statsCount).toBe(3);
    console.log('✓ Statistics count verified');
    
    // 3. Verify columns
    const headers = await adminUsersPage.getColumnHeaders();
    expect(headers.length).toBe(6);
    const isOrderCorrect = await adminUsersPage.verifyColumnOrder();
    expect(isOrderCorrect).toBe(true);
    console.log('✓ Column structure verified');
    
    // 4. Verify users
    const userCount = await adminUsersPage.getUserRowCount();
    expect(userCount).toBe(8);
    console.log('✓ User data verified');
    
    // 5. Verify removed columns
    const hasRemovedColumns = await adminUsersPage.hasRemovedColumns();
    expect(hasRemovedColumns).toBe(false);
    console.log('✓ Removed columns verified');
    
    // Take comprehensive screenshot
    await adminUsersPage.screenshot('complete-test-verification');
    
    console.log('🎉 All requirements verified successfully!');
  });

  test('take diagnostic screenshots', async ({ page }) => {
    // Wait for page to fully load
    await page.waitForTimeout(3000);
    
    // Take full page screenshot
    await page.screenshot({
      path: 'test-results/screenshots/admin-users-full-page.png',
      fullPage: true
    });
    
    // Take screenshot of just the statistics area
    const statsCards = page.locator('.card').filter({ hasText: /Total Users|Pending Vetting|On Hold/ }).first();
    if (await statsCards.isVisible()) {
      await statsCards.screenshot({
        path: 'test-results/screenshots/admin-users-statistics.png'
      });
    }
    
    // Take screenshot of just the user grid
    const userGrid = page.locator('table, .sf-grid').first();
    if (await userGrid.isVisible()) {
      await userGrid.screenshot({
        path: 'test-results/screenshots/admin-users-grid.png'
      });
    }
    
    console.log('📸 Diagnostic screenshots taken');
  });
});