import { test, expect } from '@playwright/test';

test.describe('Quick Manual Admin Events Management Test', () => {
  
  test('Manual navigation and screenshot capture', async ({ page }) => {
    console.log('Starting manual test of admin events management...');
    
    // Use global timeout from playwright.config.ts (90 seconds)
    // test.setTimeout(60000); // Removed - using global timeout
    
    // Step 1: Go to homepage first
    console.log('Step 1: Loading homepage');
    await page.goto('http://localhost:5173/', { waitUntil: 'networkidle' });
    await page.waitForTimeout(3000);
    await page.screenshot({ path: 'test-results/manual-1-homepage.png', fullPage: true });
    
    // Step 2: Navigate to login
    console.log('Step 2: Navigating to login');
    await page.goto('http://localhost:5173/login', { waitUntil: 'networkidle' });
    await page.waitForTimeout(3000);
    await page.screenshot({ path: 'test-results/manual-2-login-page.png', fullPage: true });
    
    // Check what elements are actually on the login page
    const bodyText = await page.textContent('body');
    console.log('Login page body text:', bodyText?.substring(0, 200));
    
    // Look for various input selectors
    const emailSelectors = ['input[type="email"]', 'input[name="email"]', '[placeholder*="email"]', 'input'];
    let emailInputFound = false;
    
    for (const selector of emailSelectors) {
      const count = await page.locator(selector).count();
      if (count > 0) {
        console.log(`Found ${count} elements for selector: ${selector}`);
        emailInputFound = true;
      }
    }
    
    if (!emailInputFound) {
      console.log('No email input found, checking if page loaded correctly');
      const title = await page.title();
      console.log('Page title:', title);
      
      // Check for any forms
      const formCount = await page.locator('form').count();
      console.log('Forms found:', formCount);
      
      // Check for any inputs
      const inputCount = await page.locator('input').count();
      console.log('Input elements found:', inputCount);
    }
    
    // Step 3: Try direct navigation to admin areas without login
    console.log('Step 3: Testing admin area access');
    
    await page.goto('http://localhost:5173/admin', { waitUntil: 'networkidle' });
    await page.waitForTimeout(2000);
    await page.screenshot({ path: 'test-results/manual-3-admin-direct.png', fullPage: true });
    console.log('Admin page URL:', page.url());
    
    // Step 4: Check admin dashboard
    await page.goto('http://localhost:5173/admin/dashboard', { waitUntil: 'networkidle' });
    await page.waitForTimeout(2000);
    await page.screenshot({ path: 'test-results/manual-4-admin-dashboard.png', fullPage: true });
    console.log('Admin dashboard URL:', page.url());
    
    // Step 5: Check admin events page
    await page.goto('http://localhost:5173/admin/events', { waitUntil: 'networkidle' });
    await page.waitForTimeout(2000);
    await page.screenshot({ path: 'test-results/manual-5-admin-events.png', fullPage: true });
    console.log('Admin events URL:', page.url());
    
    // Check for specific admin events management elements
    const eventsPageText = await page.textContent('body');
    console.log('Admin events page content:', eventsPageText?.substring(0, 300));
    
    // Look for common admin events management elements
    const adminElements = {
      'Events heading': await page.locator('h1, h2, h3').count(),
      'Create buttons': await page.locator('button:has-text("Create"), button:has-text("New"), button:has-text("Add")').count(),
      'Tables': await page.locator('table').count(),
      'Cards': await page.locator('.card, [class*="card"]').count(),
      'Forms': await page.locator('form').count(),
      'Lists': await page.locator('ul, ol').count()
    };
    
    console.log('Admin events page elements:', adminElements);
    
    // Step 6: Test event creation page
    await page.goto('http://localhost:5173/admin/events/create', { waitUntil: 'networkidle' });
    await page.waitForTimeout(2000);
    await page.screenshot({ path: 'test-results/manual-6-events-create.png', fullPage: true });
    console.log('Events create URL:', page.url());
    
    // Check for form elements
    const createPageElements = {
      'Input fields': await page.locator('input').count(),
      'Textareas': await page.locator('textarea').count(),
      'Select dropdowns': await page.locator('select').count(),
      'Buttons': await page.locator('button').count(),
      'Forms': await page.locator('form').count()
    };
    
    console.log('Create page elements:', createPageElements);
    
    // Step 7: Look for Event Session Matrix
    const matrixRoutes = [
      '/admin/event-session-matrix-demo',
      '/admin/event-session-matrix',
      '/admin/events-management-api-demo'
    ];
    
    for (const route of matrixRoutes) {
      console.log(`Testing route: ${route}`);
      await page.goto(`http://localhost:5173${route}`, { waitUntil: 'networkidle' });
      await page.waitForTimeout(2000);
      
      const routeName = route.replace(/\//g, '_').replace(/^_/, '');
      await page.screenshot({ path: `test-results/manual-7-${routeName}.png`, fullPage: true });
      
      const pageContent = await page.textContent('body');
      console.log(`${route} content:`, pageContent?.substring(0, 200));
    }
    
    // Final step: Capture overall findings
    console.log('Manual test completed. Check screenshots for visual verification.');
  });
});