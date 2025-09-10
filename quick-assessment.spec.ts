import { test, expect } from '@playwright/test';

test.describe('Quick Feature Assessment', () => {
  test('capture current app state and test key features', async ({ page }) => {
    console.log('🎯 Starting quick assessment...');

    // Navigate to homepage
    await page.goto('http://localhost:5173');
    await page.waitForTimeout(3000);
    
    // Capture homepage
    await page.screenshot({ path: 'test-results/current-homepage.png', fullPage: true });
    console.log('📸 Homepage captured');

    // Check for specific elements
    const logoLink = page.locator('a').filter({ hasText: 'WITCH CITY ROPE' });
    const eventsLink = page.locator('[data-testid="link-events"]');
    const loginButton = page.locator('text=Login').last(); // Get last one to avoid multiple matches
    
    console.log(`🔍 Logo link visible: ${await logoLink.isVisible()}`);
    console.log(`🔍 Events link visible: ${await eventsLink.isVisible()}`);
    console.log(`🔍 Login button visible: ${await loginButton.isVisible()}`);

    // Test events page
    if (await eventsLink.isVisible()) {
      console.log('🚀 Testing Events page...');
      await eventsLink.click();
      await page.waitForTimeout(3000);
      await page.screenshot({ path: 'test-results/current-events-page.png', fullPage: true });
      
      // Check for events content
      const eventsContent = await page.textContent('body');
      console.log(`📝 Events page has content: ${eventsContent ? eventsContent.length > 1000 : false}`);
      console.log(`📝 Events page includes "Events": ${eventsContent?.includes('Events') || false}`);
      console.log(`📝 Events page includes event data: ${eventsContent?.includes('Rope') || false}`);
    }

    // Test login page
    console.log('🔐 Testing Login page...');
    await page.goto('http://localhost:5173/login');
    await page.waitForTimeout(2000);
    await page.screenshot({ path: 'test-results/current-login-page.png', fullPage: true });
    
    // Check for login form
    const emailInput = page.locator('input[type="email"]').first();
    const passwordInput = page.locator('input[type="password"]').first();
    const submitButton = page.locator('button[type="submit"]').first();
    
    console.log(`📧 Email input exists: ${await emailInput.count() > 0}`);
    console.log(`🔒 Password input exists: ${await passwordInput.count() > 0}`);
    console.log(`🔘 Submit button exists: ${await submitButton.count() > 0}`);

    // Test login attempt if form exists
    if (await emailInput.count() > 0 && await passwordInput.count() > 0) {
      console.log('🔐 Testing login flow...');
      await emailInput.fill('admin@witchcityrope.com');
      await passwordInput.fill('Test123!');
      await submitButton.click();
      await page.waitForTimeout(3000);
      
      await page.screenshot({ path: 'test-results/after-login-attempt.png', fullPage: true });
      
      // Check if authenticated
      const welcomeText = page.locator('text=Welcome');
      const logoutButton = page.locator('text=Logout');
      const dashboardLink = page.locator('text=Dashboard');
      
      console.log(`👋 Welcome text visible: ${await welcomeText.isVisible()}`);
      console.log(`🚪 Logout button visible: ${await logoutButton.isVisible()}`);
      console.log(`📊 Dashboard link visible: ${await dashboardLink.isVisible()}`);
      
      // If authenticated, check dashboard
      if (await dashboardLink.isVisible()) {
        console.log('📊 Testing Dashboard...');
        await dashboardLink.click();
        await page.waitForTimeout(2000);
        await page.screenshot({ path: 'test-results/current-dashboard.png', fullPage: true });
        
        const dashboardContent = await page.textContent('body');
        console.log(`📝 Dashboard has content: ${dashboardContent ? dashboardContent.length > 500 : false}`);
      }
    }

    console.log('✅ Quick assessment completed');
  });
});