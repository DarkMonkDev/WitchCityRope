import { test, expect } from '@playwright/test';
import { AuthHelpers } from './helpers/auth.helpers';

test.describe('Debug Login Issue', () => {
  test('should verify MSW status and basic page functionality', async ({ page }) => {
    // Capture console messages
    const consoleMessages: string[] = [];
    page.on('console', (msg) => {
      consoleMessages.push(`[${msg.type()}] ${msg.text()}`);
    });

    // Capture page errors
    const pageErrors: string[] = [];
    page.on('pageerror', (error) => {
      pageErrors.push(`[ERROR] ${error.toString()}`);
    });

    console.log('=== STARTING DEBUG TEST ===');
    
    // Go to home page first
    console.log('Navigating to home page...');
    await page.goto('/');
    
    // Wait for page to stabilize (but not too long)
    try {
      await page.waitForLoadState('domcontentloaded', { timeout: 5000 });
      console.log('✅ DOM Content Loaded');
    } catch (e) {
      console.log('⚠️ DOM Content Load timeout');
    }

    // Take screenshot
    await page.screenshot({ path: '/home/chad/repos/witchcityrope/test-results/debug-home-page.png' });
    
    // Check page content
    const title = await page.title();
    console.log('Page title:', title);
    
    const bodyText = await page.locator('body').textContent();
    console.log('Body text (first 200 chars):', bodyText?.substring(0, 200));

    // Log console messages
    console.log('=== CONSOLE MESSAGES ===');
    consoleMessages.forEach((msg, i) => console.log(`${i + 1}. ${msg}`));
    
    // Log page errors
    console.log('=== PAGE ERRORS ===');
    pageErrors.forEach((err, i) => console.log(`${i + 1}. ${err}`));
    
    // Check for MSW status
    const mswMessages = consoleMessages.filter(msg => 
      msg.toLowerCase().includes('msw') || 
      msg.toLowerCase().includes('mock') ||
      msg.toLowerCase().includes('service worker')
    );
    console.log('=== MSW STATUS MESSAGES ===');
    mswMessages.forEach((msg, i) => console.log(`${i + 1}. ${msg}`));
    
    // Try to navigate to login
    console.log('Attempting to navigate to login...');
    try {
      await page.goto('/login', { timeout: 5000 });
      console.log('✅ Navigated to login page');
      
      await page.screenshot({ path: '/home/chad/repos/witchcityrope/test-results/debug-login-page.png' });
      
      // Check if login form exists
      const hasForm = await page.locator('form').count() > 0;
      console.log('Has login form:', hasForm);
      
      if (hasForm) {
        // Check for input fields
        const emailInput = await page.locator('input[type="email"], input[name="email"]').count();
        const passwordInput = await page.locator('input[type="password"], input[name="password"]').count();
        const submitButton = await page.locator('button[type="submit"], button:has-text("Login")').count();
        
        console.log('Email inputs found:', emailInput);
        console.log('Password inputs found:', passwordInput);
        console.log('Submit buttons found:', submitButton);
        
        if (emailInput > 0 && passwordInput > 0 && submitButton > 0) {
          console.log('✅ LOGIN FORM COMPLETE - using AuthHelpers');

          // Use AuthHelpers for consistent login
          try {
            await AuthHelpers.loginAs(page, 'admin');
            console.log('✅ Login successful using AuthHelpers');

            // Take screenshot after login
            await page.screenshot({ path: '/home/chad/repos/witchcityrope/test-results/debug-form-filled.png' });
          } catch (error) {
            console.log('❌ Login failed:', error);
          }

        } else {
          console.log('❌ LOGIN FORM INCOMPLETE');
        }
      } else {
        console.log('❌ NO LOGIN FORM FOUND');
      }
      
    } catch (navError) {
      console.log('❌ Navigation to login failed:', navError);
    }
    
    // Final console messages check
    console.log('=== FINAL CONSOLE MESSAGES ===');
    const finalMessages = consoleMessages.slice(-10); // Last 10 messages
    finalMessages.forEach((msg, i) => console.log(`${i + 1}. ${msg}`));
    
    // Basic assertions
    expect(pageErrors.length).toBe(0); // No JavaScript errors
    expect(title).toContain('Vite'); // Page loaded
  });
  
  test('should test direct API connection from browser', async ({ page }) => {
    await page.goto('/');
    
    // Test direct fetch to API
    const apiTest = await page.evaluate(async () => {
      try {
        // Test health endpoint
        const healthResponse = await fetch('http://localhost:5655/health');
        const healthStatus = {
          ok: healthResponse.ok,
          status: healthResponse.status,
          text: await healthResponse.text()
        };
        
        // Test login endpoint using test credentials
        const loginResponse = await fetch('http://localhost:5655/api/auth/login', {
          method: 'POST',
          headers: {
            'Content-Type': 'application/json'
          },
          body: JSON.stringify({
            email: 'admin@witchcityrope.com', // Using known test account
            password: 'Test123!'
          })
        });
        
        const loginResult = {
          ok: loginResponse.ok,
          status: loginResponse.status,
          json: loginResponse.ok ? await loginResponse.json() : null
        };
        
        return {
          health: healthStatus,
          login: loginResult
        };
      } catch (error) {
        return {
          error: error.toString()
        };
      }
    });
    
    console.log('=== DIRECT API TEST RESULTS ===');
    console.log('Health check:', apiTest.health || 'Failed');
    console.log('Login test:', apiTest.login || 'Failed');
    console.log('Error:', apiTest.error || 'None');
    
    if (apiTest.login?.ok) {
      console.log('✅ DIRECT API LOGIN WORKS - issue is in React app');
      console.log('Login response:', JSON.stringify(apiTest.login.json, null, 2));
    } else {
      console.log('❌ DIRECT API LOGIN FAILED - API issue or CORS');
    }
    
    // Check CORS headers
    const corsTest = await page.evaluate(async () => {
      try {
        const response = await fetch('http://localhost:5655/health', {
          method: 'OPTIONS'
        });
        
        return {
          status: response.status,
          headers: {
            'access-control-allow-origin': response.headers.get('access-control-allow-origin'),
            'access-control-allow-methods': response.headers.get('access-control-allow-methods'),
            'access-control-allow-headers': response.headers.get('access-control-allow-headers'),
            'access-control-allow-credentials': response.headers.get('access-control-allow-credentials')
          }
        };
      } catch (error) {
        return { error: error.toString() };
      }
    });
    
    console.log('=== CORS TEST RESULTS ===');
    console.log('CORS Response:', JSON.stringify(corsTest, null, 2));
  });
});