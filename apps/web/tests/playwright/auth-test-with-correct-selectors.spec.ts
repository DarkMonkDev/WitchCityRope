import { test, expect } from '@playwright/test';

const testAccounts = {
  admin: {
    email: 'admin@witchcityrope.com',
    password: 'Test123!'
  },
  test: {
    email: 'test@witchcityrope.com', 
    password: 'Test1234'
  }
};

test.describe('Authentication with Correct Selectors', () => {
  test('should login successfully with correct form selectors', async ({ page }) => {
    // Navigate to login page
    await page.goto('http://localhost:5173/login');
    
    // Wait for page to load
    await expect(page.locator('h1')).toContainText('Welcome to WitchCityRope');
    
    // Use correct data-testid selectors
    await page.fill('[data-testid="email-input"]', testAccounts.test.email);
    await page.fill('[data-testid="password-input"]', testAccounts.test.password);
    
    // Submit the form
    await page.click('[data-testid="login-button"]');
    
    // Wait for navigation to dashboard or success
    await page.waitForURL(/\/(dashboard|home|\?)/, { timeout: 10000 });
    
    // Take a screenshot for verification
    await page.screenshot({ path: 'test-results/auth-login-success.png' });
  });
  
  test('should show login form elements', async ({ page }) => {
    // Navigate to login page
    await page.goto('http://localhost:5173/login');
    
    // Verify all form elements are present
    await expect(page.locator('[data-testid="login-form"]')).toBeVisible();
    await expect(page.locator('[data-testid="email-input"]')).toBeVisible();
    await expect(page.locator('[data-testid="password-input"]')).toBeVisible();
    await expect(page.locator('[data-testid="login-button"]')).toBeVisible();
    
    // Take a screenshot
    await page.screenshot({ path: 'test-results/auth-form-elements.png' });
  });
});