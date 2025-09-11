import { test, expect } from '@playwright/test';

test.describe('Quick Visual Test', () => {
  test('capture screenshots of current application state', async ({ page }) => {
    console.log('🔍 Testing current application state on port 5174...');
    
    // Screenshot home page
    try {
      await page.goto('http://localhost:5174/', { waitUntil: 'domcontentloaded', timeout: 10000 });
      await page.screenshot({ path: 'test-results/home-page.png', fullPage: true });
      console.log('✅ Home page screenshot captured');
    } catch (error) {
      console.log('❌ Home page failed:', error.message);
    }
    
    // Screenshot login page
    try {
      await page.goto('http://localhost:5174/login', { waitUntil: 'domcontentloaded', timeout: 10000 });
      await page.screenshot({ path: 'test-results/login-page.png', fullPage: true });
      console.log('✅ Login page screenshot captured');
      
      // Check for Welcome Back text
      const welcomeText = await page.locator('h1').textContent();
      console.log('📄 Login page h1 text:', welcomeText);
    } catch (error) {
      console.log('❌ Login page failed:', error.message);
    }
    
    // Screenshot events page
    try {
      await page.goto('http://localhost:5174/events', { waitUntil: 'domcontentloaded', timeout: 10000 });
      await page.screenshot({ path: 'test-results/events-page.png', fullPage: true });
      console.log('✅ Events page screenshot captured');
    } catch (error) {
      console.log('❌ Events page failed:', error.message);
    }
    
    // Screenshot dashboard page (might redirect)
    try {
      await page.goto('http://localhost:5174/dashboard', { waitUntil: 'domcontentloaded', timeout: 10000 });
      await page.screenshot({ path: 'test-results/dashboard-page.png', fullPage: true });
      console.log('✅ Dashboard page screenshot captured');
    } catch (error) {
      console.log('❌ Dashboard page failed:', error.message);
    }
  });
});