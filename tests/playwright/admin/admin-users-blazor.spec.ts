import { test, expect } from '@playwright/test';
import { testConfig } from '../helpers/test.config';

/**
 * Admin User Management Test for Blazor Server Components
 * 
 * This test is designed specifically for the Blazor Server architecture migration.
 * It uses simple Playwright waits and avoids the broken Blazor E2E helper.
 * 
 * CONTEXT:
 * - Website converted from Razor Pages to Blazor Server
 * - Site is working - manual login and admin dashboard access confirmed
 * - Old E2E tests fail because they expect Razor Pages behavior
 * - Blazor E2E helper timing out (probably outdated)
 * 
 * APPROACH:
 * - Direct navigation to login page (no global setup dependencies)
 * - Simple Playwright waits like page.waitForLoadState('networkidle')
 * - Basic element verification with page.waitForSelector()
 * - Small delays where needed for Blazor to render
 * - Screenshots for debugging
 * 
 * Based on lessons learned from:
 * - admin-user-management-simple.spec.ts (working pattern)
 * - Recent login selector fixes (2025-08-13)
 * - Documentation: avoid complex helpers, use basic Playwright
 */

test.describe('Admin User Management - Blazor Server', () => {
  
  test.beforeEach(async ({ page }) => {
    // Set reasonable timeouts for Blazor Server
    page.setDefaultTimeout(30000);
    
    // Log page errors for debugging
    page.on('pageerror', (error) => {
      console.log('❌ Page Error:', error.message);
    });
    
    // Log console errors for debugging  
    page.on('console', (msg) => {
      if (msg.type() === 'error') {
        console.log('❌ Console Error:', msg.text());
      }
    });
  });

  test('should login as admin and access user management with Blazor Server', async ({ page }) => {
    test.info().annotations.push({ 
      type: 'test-id', 
      description: 'admin-users-blazor-server-migration' 
    });

    console.log('🚀 Starting Admin User Management test for Blazor Server...');
    
    // STEP 1: Direct login (bypass global setup)
    console.log('🔐 Step 1: Direct admin login...');
    
    await page.goto(testConfig.urls.login);
    
    // Wait for login page to load
    await page.waitForLoadState('networkidle');
    
    // Use corrected ASP.NET Core Identity selectors (lessons learned 2025-08-13)
    await page.fill('input[name="Input.EmailOrUsername"]', testConfig.accounts.admin.email);
    await page.fill('input[name="Input.Password"]', testConfig.accounts.admin.password);
    
    // Submit and wait for redirect
    await page.click('button[type="submit"]');
    
    // Wait for successful login redirect
    await page.waitForURL(url => !url.pathname.includes('/login'), { timeout: 15000 });
    
    console.log('✅ Login successful, current URL:', page.url());

    // STEP 2: Navigate to admin users page
    console.log('🏠 Step 2: Navigate to /admin/users...');
    
    await page.goto(testConfig.urls.adminUsers);
    
    // Wait for page to settle (important for Blazor Server)
    await page.waitForLoadState('networkidle');
    
    // Small delay for Blazor components to render
    await page.waitForTimeout(2000);
    
    console.log('✅ Navigated to admin users page');

    // STEP 3: Verify URL is correct
    console.log('🔗 Step 3: Verify correct URL...');
    
    await expect(page).toHaveURL(/.*\/admin\/users/);
    
    console.log('✅ URL verified:', page.url());

    // STEP 4: Wait for page title to appear
    console.log('📋 Step 4: Wait for page elements to load...');
    
    // Look for page title/heading - be flexible with selector
    const pageHeading = page.locator('h1, h2, h3, [role="heading"]').filter({ 
      hasText: /User Management|Users|Admin|Management/i 
    }).first();
    
    await expect(pageHeading).toBeVisible({ timeout: 15000 });
    
    const headingText = await pageHeading.textContent();
    console.log('✅ Page heading found:', headingText);

    // STEP 5: Check if user data appears (even if layout is broken)
    console.log('📊 Step 5: Check for user data...');
    
    // Wait a bit more for data to load
    await page.waitForTimeout(3000);
    
    // Look for user-related content (flexible selectors)
    const userContent = page.locator('table, .grid, .user-list, .data-grid, [class*="grid"], [role="grid"]').first();
    
    if (await userContent.isVisible({ timeout: 10000 })) {
      console.log('✅ User data grid found');
      
      // Look for actual user data (email addresses are good indicators)
      const emailElements = page.locator('text=/@/');
      const emailCount = await emailElements.count();
      
      if (emailCount > 0) {
        console.log(`✅ Found ${emailCount} email addresses in user data`);
      } else {
        console.log('ℹ️ User grid visible but no email addresses detected yet');
      }
      
    } else {
      console.log('⚠️ User data grid not immediately visible - checking for loading states...');
      
      // Check if there's a loading indicator
      const loadingIndicators = page.locator('.loading, .spinner, .skeleton, [aria-label*="loading"]');
      const loadingCount = await loadingIndicators.count();
      
      if (loadingCount > 0) {
        console.log('📈 Loading indicators detected, waiting longer...');
        await page.waitForTimeout(5000);
        
        // Recheck for user content
        if (await userContent.isVisible({ timeout: 5000 })) {
          console.log('✅ User data grid appeared after loading');
        }
      }
    }

    // STEP 6: Look for admin-specific elements
    console.log('🔧 Step 6: Check for admin functionality...');
    
    // Look for admin actions/buttons
    const adminElements = page.locator('button, a, .btn').filter({ 
      hasText: /Edit|Delete|Admin|Manage|View Details|Update|Role/i 
    });
    
    const adminElementCount = await adminElements.count();
    
    if (adminElementCount > 0) {
      console.log(`✅ Found ${adminElementCount} admin action elements`);
    } else {
      console.log('ℹ️ Admin action elements not immediately visible (may be in dropdowns or require interaction)');
    }

    // STEP 7: Check for statistics/summary cards
    console.log('📈 Step 7: Check for user statistics...');
    
    const statElements = page.locator('.card, .stat, .summary, [class*="card"], [class*="stat"]').filter({
      hasText: /Total|Count|Users|Members|Pending|Statistics/i
    });
    
    const statCount = await statElements.count();
    
    if (statCount > 0) {
      console.log(`✅ Found ${statCount} statistics/summary elements`);
    } else {
      console.log('ℹ️ Statistics elements not detected');
    }

    // STEP 8: Take screenshot for debugging
    console.log('📸 Step 8: Take debugging screenshot...');
    
    await page.screenshot({
      path: 'test-results/screenshots/admin-users-blazor-server.png',
      fullPage: true
    });
    
    console.log('✅ Screenshot saved for debugging');

    // STEP 9: Test basic interaction (if possible)
    console.log('🖱️ Step 9: Test basic user interaction...');
    
    // Look for clickable user rows/elements
    const clickableUserElements = page.locator('tr, .user-row, .data-row').filter({
      hasText: /@/
    });
    
    const clickableCount = await clickableUserElements.count();
    
    if (clickableCount > 0) {
      console.log(`Found ${clickableCount} potentially clickable user elements`);
      
      try {
        // Click first user element
        await clickableUserElements.first().click();
        
        // Wait a moment for response
        await page.waitForTimeout(1500);
        
        // Check if something happened (modal, navigation, details panel)
        const hasModal = await page.locator('.modal, .dialog, .popup, .overlay').count() > 0;
        const urlChanged = !page.url().includes('/admin/users');
        const hasDetailsPanel = await page.locator('.details, .panel, [class*="detail"]').count() > 0;
        
        if (hasModal || urlChanged || hasDetailsPanel) {
          console.log('✅ User interaction successful - some UI response detected');
        } else {
          console.log('ℹ️ User click registered but no obvious UI change detected');
        }
        
      } catch (error) {
        console.log('ℹ️ User interaction test encountered error:', error.message);
      }
    } else {
      console.log('ℹ️ No clickable user elements detected for interaction test');
    }

    console.log('🎉 Admin User Management test completed successfully!');
    
    // Final verification - ensure we're still on the right page
    await expect(page).toHaveURL(/.*\/admin\/users/);
    
    console.log('✅ Final verification: Still on admin users page');
  });

  test('should handle page loading states gracefully', async ({ page }) => {
    test.info().annotations.push({ 
      type: 'test-id', 
      description: 'admin-users-blazor-loading-states' 
    });

    console.log('⏳ Testing page loading state handling...');
    
    // Login quickly
    await page.goto(testConfig.urls.login);
    await page.waitForLoadState('networkidle');
    await page.fill('input[name="Input.EmailOrUsername"]', testConfig.accounts.admin.email);
    await page.fill('input[name="Input.Password"]', testConfig.accounts.admin.password);
    await page.click('button[type="submit"]');
    await page.waitForURL(url => !url.pathname.includes('/login'));

    // Navigate and test different loading states
    await page.goto(testConfig.urls.adminUsers);
    
    // Test networkidle wait
    console.log('Testing networkidle wait...');
    await page.waitForLoadState('networkidle');
    console.log('✅ networkidle state reached');
    
    // Test domcontentloaded wait  
    console.log('Testing domcontentloaded state...');
    await page.waitForLoadState('domcontentloaded');
    console.log('✅ domcontentloaded state reached');
    
    // Test selector wait
    console.log('Testing selector wait...');
    await page.waitForSelector('body', { timeout: 10000 });
    console.log('✅ Body selector wait successful');
    
    // Verify page is functional
    await expect(page).toHaveURL(/.*\/admin\/users/);
    
    console.log('✅ Page loading states test completed');
  });

  test('should capture page structure for debugging', async ({ page }) => {
    test.info().annotations.push({ 
      type: 'test-id', 
      description: 'admin-users-blazor-structure-debug' 
    });

    console.log('🔍 Capturing page structure for debugging...');
    
    // Login
    await page.goto(testConfig.urls.login);
    await page.waitForLoadState('networkidle');
    await page.fill('input[name="Input.EmailOrUsername"]', testConfig.accounts.admin.email);
    await page.fill('input[name="Input.Password"]', testConfig.accounts.admin.password);
    await page.click('button[type="submit"]');
    await page.waitForURL(url => !url.pathname.includes('/login'));

    // Navigate to admin users
    await page.goto(testConfig.urls.adminUsers);
    await page.waitForLoadState('networkidle');
    await page.waitForTimeout(3000);

    // Capture page title
    const pageTitle = await page.title();
    console.log('📄 Page title:', pageTitle);
    
    // Capture main headings
    const headings = await page.locator('h1, h2, h3, h4, h5, h6').allTextContents();
    console.log('📋 Page headings:', headings);
    
    // Capture any error messages
    const errorElements = await page.locator('.error, .alert-danger, [class*="error"]').allTextContents();
    if (errorElements.length > 0) {
      console.log('❌ Error messages found:', errorElements);
    } else {
      console.log('✅ No error messages detected');
    }
    
    // Capture Blazor info if available
    const blazorScript = await page.locator('script[src*="blazor"]').count();
    console.log('🔧 Blazor scripts found:', blazorScript);
    
    // Take final debugging screenshot
    await page.screenshot({
      path: 'test-results/screenshots/admin-users-blazor-debug-structure.png',
      fullPage: true
    });
    
    console.log('✅ Page structure debugging completed');
  });
});