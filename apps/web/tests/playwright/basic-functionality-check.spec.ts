import { test, expect } from '@playwright/test';

/**
 * Basic Functionality Check - What Currently Works
 * 
 * This test verifies the current state of the application to establish
 * a baseline for what's functional vs what needs to be implemented.
 */

test.describe('Basic Functionality Check - Current State', () => {

  test('React app loads and displays basic content', async ({ page }) => {
    await page.goto('http://localhost:5173');

    // Should load without errors - verify the actual application title
    await expect(page).toHaveTitle(/Witch City Rope/);
    
    // Take screenshot of what actually loads
    await page.screenshot({ path: '/home/chad/repos/witchcityrope-react/test-results/current-app-state.png' });
    
    console.log('✅ React app loads successfully');
  });

  test('Check what routes are actually available', async ({ page }) => {
    const routes = [
      '/',
      '/events',
      '/login',
      '/dashboard',
      '/admin',
      '/admin/events'
    ];

    for (const route of routes) {
      try {
        await page.goto(`http://localhost:5173${route}`);
        await page.waitForLoadState('networkidle', { timeout: 5000 });
        
        const title = await page.title();
        const hasContent = await page.locator('body').isVisible();
        
        console.log(`Route ${route}: Title="${title}", HasContent=${hasContent}`);
        
        // Take screenshot of each route
        await page.screenshot({ 
          path: `/home/chad/repos/witchcityrope-react/test-results/route-${route.replace(/\//g, '_')}.png` 
        });
        
      } catch (error) {
        console.log(`Route ${route}: ERROR - ${error}`);
      }
    }
  });

  test('Check what components and elements are actually present', async ({ page }) => {
    await page.goto('http://localhost:5173');
    await page.waitForLoadState('networkidle');
    
    // Check for common elements that might exist
    const elementsToCheck = [
      'nav',
      'header', 
      'main',
      'footer',
      '[data-testid*="login"]',
      '[data-testid*="event"]',
      '[data-testid*="dashboard"]',
      '[data-testid*="user"]',
      'form',
      'button',
      'input',
      'a[href*="events"]',
      'a[href*="login"]',
      'a[href*="dashboard"]'
    ];

    for (const selector of elementsToCheck) {
      const element = page.locator(selector);
      const isVisible = await element.isVisible().catch(() => false);
      const count = await element.count().catch(() => 0);
      
      console.log(`Element "${selector}": Visible=${isVisible}, Count=${count}`);
    }
  });

  test('API connectivity test', async ({ request }) => {
    // Test various API endpoints to see what's actually working
    const endpoints = [
      'http://localhost:5655/',
      'http://localhost:5655/api',
      'http://localhost:5655/api/health',
      'http://localhost:5655/health',
      'http://localhost:5655/api/events',
      'http://localhost:5655/api/auth/login'
    ];

    for (const endpoint of endpoints) {
      try {
        const response = await request.get(endpoint);
        console.log(`${endpoint}: Status=${response.status()}`);
      } catch (error) {
        console.log(`${endpoint}: ERROR - ${error}`);
      }
    }
  });

  test('Database connectivity through API test', async ({ request }) => {
    // Try to get a successful response from any API endpoint
    try {
      // Try a POST to login with known test user
      const loginResponse = await request.post('http://localhost:5655/api/auth/login', {
        data: {
          email: 'member@witchcityrope.com',
          password: 'Test123!'
        }
      });
      
      console.log(`Login API: Status=${loginResponse.status()}`);
      
      if (loginResponse.ok()) {
        const data = await loginResponse.json();
        console.log('Login successful, received data:', Object.keys(data));
      } else {
        const errorText = await loginResponse.text();
        console.log('Login failed:', errorText.substring(0, 200));
      }
    } catch (error) {
      console.log('Login API test failed:', error);
    }
  });

  test('Form elements discovery', async ({ page }) => {
    // Check each route for form elements
    const routes = ['/', '/events', '/login'];
    
    for (const route of routes) {
      try {
        await page.goto(`http://localhost:5173${route}`);
        await page.waitForLoadState('networkidle', { timeout: 5000 });
        
        // Look for forms and inputs
        const forms = await page.locator('form').count();
        const inputs = await page.locator('input').count();
        const buttons = await page.locator('button').count();
        
        console.log(`Route ${route}: Forms=${forms}, Inputs=${inputs}, Buttons=${buttons}`);
        
        if (forms > 0) {
          // If forms exist, look for specific test IDs
          const testIds = await page.locator('[data-testid]').count();
          console.log(`  - Elements with data-testid: ${testIds}`);
          
          // List actual test IDs if they exist
          if (testIds > 0) {
            const testIdElements = page.locator('[data-testid]');
            for (let i = 0; i < Math.min(testIds, 10); i++) {
              const testId = await testIdElements.nth(i).getAttribute('data-testid');
              console.log(`  - data-testid="${testId}"`);
            }
          }
        }
      } catch (error) {
        console.log(`Route ${route} form check: ERROR - ${error}`);
      }
    }
  });

});