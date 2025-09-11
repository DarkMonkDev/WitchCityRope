import { test, expect } from '@playwright/test';

/**
 * Visual verification test for navigation updates
 * Captures screenshots to document the working implementation
 */

test.describe('Navigation Visual Verification', () => {
  
  test('capture guest user navigation state', async ({ page }) => {
    await page.goto('/');
    await page.waitForLoadState('networkidle');
    
    // Capture full page showing guest navigation
    await page.screenshot({ 
      path: 'test-results/guest-navigation-state.png',
      fullPage: true 
    });
    
    // Verify login button is visible
    const loginButton = page.locator('[data-testid="nav-main"] a').filter({ hasText: 'Login' });
    await expect(loginButton).toBeVisible();
  });
  
  test('capture member user navigation state', async ({ page }) => {
    // Login as member
    await page.goto('/login');
    await page.waitForLoadState('networkidle');
    
    await page.fill('[data-testid="email-input"]', 'member@witchcityrope.com');
    await page.fill('[data-testid="password-input"]', 'Test123!');
    await page.click('[data-testid="login-button"]');
    
    // Wait for login success and navigation
    await page.waitForURL(url => !url.pathname.includes('/login'), { timeout: 10000 });
    await page.waitForLoadState('networkidle');
    
    // Navigate to homepage to see full navigation
    await page.goto('/');
    await page.waitForLoadState('networkidle');
    
    // Capture full page showing member navigation
    await page.screenshot({ 
      path: 'test-results/member-navigation-state.png',
      fullPage: true 
    });
    
    // Verify dashboard button and user greeting
    const dashboardButton = page.locator('[data-testid="link-dashboard"]');
    const userGreeting = page.locator('[data-testid="user-greeting"]');
    const logoutButton = page.locator('[data-testid="button-logout"]');
    
    await expect(dashboardButton).toBeVisible();
    await expect(userGreeting).toBeVisible();
    await expect(logoutButton).toBeVisible();
  });
  
  test('capture navigation component close-up', async ({ page }) => {
    await page.goto('/');
    await page.waitForLoadState('networkidle');
    
    // Capture just the navigation header
    const navHeader = page.locator('[data-testid="nav-main"]');
    await navHeader.screenshot({ 
      path: 'test-results/navigation-header-detail.png'
    });
  });
  
  test('capture utility bar close-up', async ({ page }) => {
    // Login first to show utility bar with user info
    await page.goto('/login');
    await page.waitForLoadState('networkidle');
    
    await page.fill('[data-testid="email-input"]', 'member@witchcityrope.com');
    await page.fill('[data-testid="password-input"]', 'Test123!');
    await page.click('[data-testid="login-button"]');
    
    await page.waitForURL(url => !url.pathname.includes('/login'), { timeout: 10000 });
    await page.goto('/');
    await page.waitForLoadState('networkidle');
    
    // Capture just the utility bar to show user greeting and logout
    await page.screenshot({ 
      path: 'test-results/utility-bar-with-user.png',
      clip: { x: 0, y: 0, width: 1280, height: 60 }
    });
  });
});