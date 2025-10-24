import { test, expect } from '@playwright/test';
import { AuthHelpers } from './helpers/auth.helpers';
import { setupConsoleErrorFiltering } from './helpers/console.helpers';

test.describe('Application State Visual Evidence', () => {
  test.beforeEach(async ({ page }) => {
    // Set up console error filtering
    setupConsoleErrorFiltering(page, {
      filter401Errors: true,
      logFilteredMessages: false,
    });
  });

  test('capture events page actual state', async ({ page }) => {
    console.log('📸 Capturing events page state for analysis...');
    
    // Navigate to events page
    await page.goto('http://localhost:5173/events');

    // Wait for page to load
    await page.waitForLoadState('networkidle');
    
    // Capture screenshot
    await page.screenshot({ 
      path: '../../test-results/actual-events-page.png',
      fullPage: true 
    });
    
    // Log what we actually see
    const pageTitle = await page.locator('h1').first().textContent();
    const pageContent = await page.textContent('body');
    
    console.log(`Events page title: ${pageTitle}`);
    console.log(`Page contains "event": ${pageContent?.includes('event') ? 'YES' : 'NO'}`);
    console.log(`Page contains "loading": ${pageContent?.includes('loading') ? 'YES' : 'NO'}`);
  });

  test('capture login page actual state', async ({ page }) => {
    console.log('📸 Capturing login page state for analysis...');
    
    // Navigate to login page
    await page.goto('http://localhost:5173/login');

    // Wait for page to load
    await page.waitForLoadState('networkidle');
    
    // Capture screenshot
    await page.screenshot({ 
      path: '../../test-results/actual-login-page.png',
      fullPage: true 
    });
    
    // Log what we actually see
    const pageTitle = await page.locator('h1').first().textContent();
    const formExists = await page.locator('form').count() > 0;
    const emailInput = await page.locator('input[type="email"], input[placeholder*="email"]').count();
    
    console.log(`Login page title: "${pageTitle}"`);
    console.log(`Form exists: ${formExists}`);
    console.log(`Email inputs found: ${emailInput}`);
  });
  
  test('capture dashboard page actual state', async ({ page }) => {
    console.log('📸 Capturing dashboard page state...');

    // Login as member before accessing dashboard
    await AuthHelpers.loginAs(page, 'member');

    // Navigate to dashboard page (may require auth)
    await page.goto('http://localhost:5173/dashboard');

    // Wait for page to load
    await page.waitForLoadState('networkidle');
    
    // Capture screenshot
    await page.screenshot({ 
      path: '../../test-results/actual-dashboard-page.png',
      fullPage: true 
    });
    
    // Log what we see
    const currentUrl = page.url();
    const pageTitle = await page.locator('h1').first().textContent();
    
    console.log(`Current URL: ${currentUrl}`);
    console.log(`Page title: "${pageTitle}"`);
  });
});