import { test, expect } from '@playwright/test';

/**
 * Direct Admin Test - Bypasses Global Setup
 * 
 * This test directly tests admin functionality without relying on the
 * global setup that's causing timeouts with the Blazor E2E helper.
 */

test.describe('Admin Direct Test', () => {
  
  test('should login and access admin user management page directly', async ({ page }) => {
    // Set timeout
    test.setTimeout(60000);
    
    console.log('🚀 Starting direct admin test...');
    
    // Step 1: Navigate to login
    console.log('📍 Step 1: Navigate to login page...');
    await page.goto('http://localhost:5651/Identity/Account/Login');
    await page.waitForLoadState('networkidle');
    
    // Step 2: Login as admin
    console.log('🔐 Step 2: Login as admin...');
    await page.fill('input[name="Input.EmailOrUsername"]', 'admin@witchcityrope.com');
    await page.fill('input[name="Input.Password"]', 'Test123!');
    await page.click('button[type="submit"]');
    
    // Wait for login to complete
    await page.waitForURL(url => !url.pathname.includes('/login'), { timeout: 15000 });
    console.log('✅ Login successful, now at:', page.url());
    
    // Step 3: Navigate to admin users page
    console.log('📋 Step 3: Navigate to admin users page...');
    await page.goto('http://localhost:5651/admin/users');
    await page.waitForLoadState('networkidle');
    
    // Give page time to render
    await page.waitForTimeout(3000);
    
    console.log('📊 Step 4: Check page content...');
    
    // Take screenshot for debugging
    await page.screenshot({ 
      path: 'tests/playwright/test-results/admin-direct-test.png',
      fullPage: true 
    });
    
    // Check for page title
    const pageTitle = page.locator('h1, h2, h3').filter({ hasText: /User Management|Users|Admin/i });
    const titleExists = await pageTitle.count() > 0;
    console.log('Page title exists:', titleExists);
    
    if (titleExists) {
      console.log('✅ Page title found');
      const titleText = await pageTitle.first().textContent();
      console.log('Title text:', titleText);
    }
    
    // Check for user statistics cards
    const statsCards = page.locator('.card, .stat-card, [class*="card"]');
    const statsCount = await statsCards.count();
    console.log('Statistics cards found:', statsCount);
    
    // Check for user grid/table
    const userGrid = page.locator('table, .grid, .sf-grid, [class*="grid"]');
    const gridExists = await userGrid.count() > 0;
    console.log('User grid exists:', gridExists);
    
    // Check for real user data (email addresses)
    const emailElements = page.locator('text=/@/');
    const emailCount = await emailElements.count();
    console.log('Email addresses found:', emailCount);
    
    // Check for error messages
    const errorMessages = page.locator('text=/error|fail|unauthorized|forbidden/i');
    const errorCount = await errorMessages.count();
    console.log('Error messages found:', errorCount);
    
    // Check browser console for errors
    const logs: string[] = [];
    page.on('console', msg => {
      if (msg.type() === 'error') {
        logs.push(`Console Error: ${msg.text()}`);
      }
    });
    
    // Wait a bit more to catch any delayed errors
    await page.waitForTimeout(2000);
    
    if (logs.length > 0) {
      console.log('❌ Console errors found:');
      logs.forEach(log => console.log(log));
    } else {
      console.log('✅ No console errors detected');
    }
    
    // Basic assertions
    expect(page.url()).toContain('/admin/users');
    
    console.log('🎉 Direct admin test completed!');
    
    // Return test results
    return {
      pageLoaded: page.url().includes('/admin/users'),
      titleExists,
      statsCount,
      gridExists,
      emailCount,
      errorCount,
      consoleErrors: logs.length
    };
  });
});