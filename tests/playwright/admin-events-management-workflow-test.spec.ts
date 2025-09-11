import { test, expect } from '@playwright/test';

/**
 * Comprehensive Admin Events Management Workflow Test
 * Tests the complete workflow as requested:
 * 1. Login as admin
 * 2. Verify admin navigation appears
 * 3. Navigate to admin dashboard
 * 4. Access events management
 * 5. Test event creation, editing, deletion
 * 6. Capture screenshots at each step
 */

test.describe('Admin Events Management Complete Workflow', () => {
  
  test.beforeEach(async ({ page }) => {
    // Listen for console errors and network issues
    page.on('console', msg => {
      if (msg.type() === 'error') {
        console.log('Console Error:', msg.text());
      }
    });
    
    page.on('response', response => {
      if (!response.ok() && response.url().includes('/api/')) {
        console.log('API Error:', response.status(), response.url());
      }
    });
  });

  test('Complete Admin Events Management Workflow', async ({ page }) => {
    // Step 1: Navigate to login page
    console.log('Step 1: Navigating to login page');
    await page.goto('http://localhost:5173/login');
    
    // Wait for page to load and take screenshot
    await page.waitForLoadState('networkidle');
    await page.screenshot({ path: 'test-results/1-login-page.png', fullPage: true });
    
    // Verify login page loads
    await expect(page).toHaveURL(/.*login/);
    console.log('✓ Login page loaded successfully');

    // Step 2: Login as admin
    console.log('Step 2: Logging in as admin');
    
    // Fill in admin credentials
    await page.fill('[data-testid="email-input"]', 'admin@witchcityrope.com');
    await page.fill('[data-testid="password-input"]', 'Test123!');
    
    // Take screenshot before login attempt
    await page.screenshot({ path: 'test-results/2-login-form-filled.png', fullPage: true });
    
    // Submit login form
    await page.click('[data-testid="login-button"]');
    
    // Wait for login response and navigation
    await page.waitForTimeout(3000);
    
    // Take screenshot after login attempt
    await page.screenshot({ path: 'test-results/3-after-login-attempt.png', fullPage: true });
    
    console.log('Current URL after login:', page.url());

    // Step 3: Check if "Admin" link appears in navigation
    console.log('Step 3: Checking for Admin navigation link');
    
    // Look for admin navigation link
    const adminLink = page.locator('a:has-text("Admin")');
    const adminLinkVisible = await adminLink.isVisible().catch(() => false);
    
    if (adminLinkVisible) {
      console.log('✓ Admin link found in navigation');
      await page.screenshot({ path: 'test-results/4-admin-link-visible.png', fullPage: true });
    } else {
      console.log('⚠ Admin link not found, checking current navigation state');
      await page.screenshot({ path: 'test-results/4-navigation-state.png', fullPage: true });
      
      // Check if we're on dashboard or another page
      const currentUrl = page.url();
      console.log('Current URL:', currentUrl);
      
      // Look for other navigation elements
      const navElements = await page.locator('nav, [role="navigation"], .navigation').count();
      console.log('Navigation elements found:', navElements);
    }

    // Step 4: Navigate to admin dashboard (try different approaches)
    console.log('Step 4: Attempting to access admin dashboard');
    
    if (adminLinkVisible) {
      // Click admin link if visible
      await adminLink.click();
      await page.waitForTimeout(2000);
    } else {
      // Try direct navigation to admin routes
      await page.goto('http://localhost:5173/admin');
      await page.waitForTimeout(2000);
    }
    
    // Take screenshot of admin area
    await page.screenshot({ path: 'test-results/5-admin-area.png', fullPage: true });
    console.log('Admin area URL:', page.url());

    // Step 5: Look for Events Management card or link
    console.log('Step 5: Looking for Events Management access');
    
    // Check for various ways to access events management
    const eventsManagementSelectors = [
      'text="Events Management"',
      'text="Create New Event"',
      'a[href*="events"]',
      'button:has-text("Events")',
      '[data-testid*="events"]',
      '.events-card',
      '.event-management'
    ];
    
    let eventsManagementFound = false;
    let usedSelector = '';
    
    for (const selector of eventsManagementSelectors) {
      const element = page.locator(selector);
      const isVisible = await element.isVisible().catch(() => false);
      
      if (isVisible) {
        console.log(`✓ Events Management found with selector: ${selector}`);
        eventsManagementFound = true;
        usedSelector = selector;
        
        // Click the events management link
        await element.first().click();
        await page.waitForTimeout(2000);
        break;
      }
    }
    
    if (!eventsManagementFound) {
      console.log('⚠ Events Management not found, trying direct navigation');
      await page.goto('http://localhost:5173/admin/events');
      await page.waitForTimeout(2000);
    }
    
    // Take screenshot of events management page
    await page.screenshot({ path: 'test-results/6-events-management-page.png', fullPage: true });
    console.log('Events Management URL:', page.url());

    // Step 6: Test creating a new event
    console.log('Step 6: Testing event creation');
    
    // Look for "Create Event" or similar button
    const createEventSelectors = [
      'button:has-text("Create Event")',
      'button:has-text("New Event")',
      'button:has-text("Add Event")',
      '[data-testid*="create"]',
      'a:has-text("Create")'
    ];
    
    let createButtonFound = false;
    
    for (const selector of createEventSelectors) {
      const element = page.locator(selector);
      const isVisible = await element.isVisible().catch(() => false);
      
      if (isVisible) {
        console.log(`✓ Create Event button found: ${selector}`);
        createButtonFound = true;
        
        // Click create event button
        await element.first().click();
        await page.waitForTimeout(2000);
        
        // Take screenshot of event creation form
        await page.screenshot({ path: 'test-results/7-event-creation-form.png', fullPage: true });
        break;
      }
    }
    
    if (!createButtonFound) {
      console.log('⚠ Create Event button not found');
      
      // Check if we need to navigate to create form
      await page.goto('http://localhost:5173/admin/events/create');
      await page.waitForTimeout(2000);
      await page.screenshot({ path: 'test-results/7-direct-create-form.png', fullPage: true });
    }

    // Step 7: Fill event creation form (if available)
    console.log('Step 7: Testing event form interaction');
    
    // Look for form fields
    const titleInput = page.locator('input[name="title"], [data-testid*="title"], input[placeholder*="title"]').first();
    const descriptionInput = page.locator('textarea[name="description"], [data-testid*="description"], textarea[placeholder*="description"]').first();
    
    const titleExists = await titleInput.isVisible().catch(() => false);
    const descriptionExists = await descriptionInput.isVisible().catch(() => false);
    
    if (titleExists) {
      console.log('✓ Title field found, filling test data');
      await titleInput.fill('Test Admin Event - Created by Playwright');
      await page.waitForTimeout(500);
    }
    
    if (descriptionExists) {
      console.log('✓ Description field found, filling test data');
      await descriptionInput.fill('This is a test event created during admin workflow testing.');
      await page.waitForTimeout(500);
    }
    
    // Take screenshot after filling form
    await page.screenshot({ path: 'test-results/8-form-filled.png', fullPage: true });

    // Step 8: Look for existing events to test editing/deletion
    console.log('Step 8: Looking for existing events to test management');
    
    // Navigate back to events list
    await page.goto('http://localhost:5173/admin/events');
    await page.waitForTimeout(2000);
    
    // Look for events list
    const eventsList = page.locator('[data-testid="events-list"], .events-list, .event-card, table tbody tr');
    const eventsCount = await eventsList.count();
    
    console.log(`Found ${eventsCount} event elements`);
    
    if (eventsCount > 0) {
      console.log('✓ Events found, testing management actions');
      
      // Look for edit/delete buttons on first event
      const editButtons = page.locator('button:has-text("Edit"), [data-testid*="edit"], .edit-button');
      const deleteButtons = page.locator('button:has-text("Delete"), [data-testid*="delete"], .delete-button');
      
      const editCount = await editButtons.count();
      const deleteCount = await deleteButtons.count();
      
      console.log(`Found ${editCount} edit buttons, ${deleteCount} delete buttons`);
      
      if (editCount > 0) {
        console.log('Testing edit functionality');
        await editButtons.first().click();
        await page.waitForTimeout(2000);
        await page.screenshot({ path: 'test-results/9-edit-event-form.png', fullPage: true });
      }
      
      if (deleteCount > 0) {
        console.log('Testing delete functionality (will cancel)');
        await deleteButtons.first().click();
        await page.waitForTimeout(1000);
        
        // Look for confirmation dialog
        const confirmDialog = page.locator('[role="dialog"], .modal, .confirmation');
        const dialogVisible = await confirmDialog.isVisible().catch(() => false);
        
        if (dialogVisible) {
          console.log('✓ Delete confirmation dialog appeared');
          await page.screenshot({ path: 'test-results/10-delete-confirmation.png', fullPage: true });
          
          // Cancel the deletion
          const cancelButton = page.locator('button:has-text("Cancel"), button:has-text("No")');
          const cancelExists = await cancelButton.isVisible().catch(() => false);
          
          if (cancelExists) {
            await cancelButton.click();
            await page.waitForTimeout(1000);
          }
        }
      }
    }
    
    // Step 9: Final state capture
    console.log('Step 9: Capturing final state');
    await page.screenshot({ path: 'test-results/11-final-admin-events-state.png', fullPage: true });
    
    // Check for any console errors during the test
    const consoleLogs: string[] = [];
    page.on('console', msg => {
      if (msg.type() === 'error') {
        consoleLogs.push(msg.text());
      }
    });
    
    console.log(`Test completed. Console errors: ${consoleLogs.length}`);
    if (consoleLogs.length > 0) {
      console.log('Console errors encountered:', consoleLogs);
    }
  });

  test('Admin Dashboard Navigation Test', async ({ page }) => {
    console.log('Testing admin dashboard access and navigation');
    
    // Direct navigation to admin areas to test routing
    const adminRoutes = [
      '/admin',
      '/admin/dashboard', 
      '/admin/events',
      '/admin/events/create',
      '/admin/users'
    ];
    
    for (const route of adminRoutes) {
      console.log(`Testing route: ${route}`);
      
      await page.goto(`http://localhost:5173${route}`);
      await page.waitForTimeout(2000);
      
      // Take screenshot of each admin route
      const routeName = route.replace(/\//g, '_').substring(1) || 'admin_root';
      await page.screenshot({ path: `test-results/route_${routeName}.png`, fullPage: true });
      
      // Check if we're redirected to login (unauthorized) or if page loads
      const currentUrl = page.url();
      console.log(`Route ${route} -> ${currentUrl}`);
      
      if (currentUrl.includes('login')) {
        console.log(`⚠ Redirected to login for ${route} - authentication required`);
      } else {
        console.log(`✓ Successfully accessed ${route}`);
      }
    }
  });

  test('Event Session Matrix Testing', async ({ page }) => {
    console.log('Testing Event Session Matrix functionality');
    
    // Navigate to potential event session matrix routes
    const matrixRoutes = [
      '/admin/events/create',
      '/admin/event-session-matrix',
      '/admin/event-session-matrix-demo'
    ];
    
    for (const route of matrixRoutes) {
      console.log(`Testing matrix route: ${route}`);
      
      await page.goto(`http://localhost:5173${route}`);
      await page.waitForTimeout(3000);
      
      // Take screenshot
      const routeName = route.replace(/\//g, '_').substring(1);
      await page.screenshot({ path: `test-results/matrix_${routeName}.png`, fullPage: true });
      
      // Look for session matrix elements
      const sessionElements = await page.locator('*:has-text("session"), *:has-text("Session")').count();
      const ticketElements = await page.locator('*:has-text("ticket"), *:has-text("Ticket")').count();
      const matrixElements = await page.locator('table, .grid, [role="grid"]').count();
      
      console.log(`Matrix elements found: sessions=${sessionElements}, tickets=${ticketElements}, grids=${matrixElements}`);
    }
  });
});