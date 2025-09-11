import { test, expect, Page } from '@playwright/test';

/**
 * Navigation Updates Test Suite
 * 
 * Tests the recent navigation updates for logged-in users:
 * 1. Dashboard button replaces Login button for authenticated users
 * 2. Admin link appears left of "Events & Classes" for administrators only
 * 3. User greeting moved to LEFT side of utility bar
 * 4. Logout link added to RIGHT side of utility bar (after Contact)
 */

test.describe('Navigation Updates for Logged-in Users', () => {
  
  test.beforeEach(async ({ page }) => {
    // Navigate to homepage
    await page.goto('/');
    await page.waitForLoadState('networkidle');
  });

  test.describe('Guest User (Not Logged In)', () => {
    
    test('should show Login button in navigation (not Dashboard)', async ({ page }) => {
      // Verify Login button appears
      const loginButton = page.locator('[data-testid="nav-main"] a').filter({ hasText: 'Login' });
      await expect(loginButton).toBeVisible();
      
      // Verify Dashboard button does NOT appear
      const dashboardButton = page.locator('[data-testid="link-dashboard"]');
      await expect(dashboardButton).not.toBeVisible();
    });

    test('should NOT show Admin link', async ({ page }) => {
      const adminLink = page.locator('[data-testid="link-admin"]');
      await expect(adminLink).not.toBeVisible();
    });

    test('should NOT show user greeting in utility bar', async ({ page }) => {
      const userGreeting = page.locator('[data-testid="user-greeting"]');
      await expect(userGreeting).not.toBeVisible();
    });

    test('should NOT show Logout link in utility bar', async ({ page }) => {
      const logoutButton = page.locator('[data-testid="button-logout"]');
      await expect(logoutButton).not.toBeVisible();
    });
  });

  test.describe('Regular Member (Logged In)', () => {
    
    test.beforeEach(async ({ page }) => {
      // Login as regular member
      await loginAsUser(page, 'member@witchcityrope.com', 'Test123!');
    });

    test('should show Dashboard button (not Login)', async ({ page }) => {
      // Verify Dashboard button appears
      const dashboardButton = page.locator('[data-testid="link-dashboard"]');
      await expect(dashboardButton).toBeVisible();
      await expect(dashboardButton).toHaveText('Dashboard');
      
      // Verify Login button does NOT appear
      const loginButton = page.locator('[data-testid="nav-main"] a').filter({ hasText: 'Login' });
      await expect(loginButton).not.toBeVisible();
    });

    test('should NOT show Admin link', async ({ page }) => {
      const adminLink = page.locator('[data-testid="link-admin"]');
      await expect(adminLink).not.toBeVisible();
    });

    test('should show user greeting in utility bar LEFT side', async ({ page }) => {
      const userGreeting = page.locator('[data-testid="user-greeting"]');
      await expect(userGreeting).toBeVisible();
      await expect(userGreeting).toContainText('Welcome,');
    });

    test('should show Logout link in utility bar RIGHT side', async ({ page }) => {
      const logoutButton = page.locator('[data-testid="button-logout"]');
      await expect(logoutButton).toBeVisible();
      await expect(logoutButton).toHaveText('Logout');
    });

    test('should be able to logout', async ({ page }) => {
      const logoutButton = page.locator('[data-testid="button-logout"]');
      await logoutButton.click();
      
      // Should redirect to homepage and show login button again
      await expect(page).toHaveURL('/');
      const loginButton = page.locator('[data-testid="nav-main"] a').filter({ hasText: 'Login' });
      await expect(loginButton).toBeVisible();
    });
  });

  test.describe('Administrator (Logged In)', () => {
    
    test.beforeEach(async ({ page }) => {
      // Login as administrator
      await loginAsUser(page, 'admin@witchcityrope.com', 'Test123!');
    });

    test('should show Dashboard button', async ({ page }) => {
      const dashboardButton = page.locator('[data-testid="link-dashboard"]');
      await expect(dashboardButton).toBeVisible();
      await expect(dashboardButton).toHaveText('Dashboard');
    });

    test('should show Admin link left of "Events & Classes"', async ({ page }) => {
      const adminLink = page.locator('[data-testid="link-admin"]');
      const eventsLink = page.locator('[data-testid="link-events"]');
      
      await expect(adminLink).toBeVisible();
      await expect(adminLink).toHaveText('Admin');
      await expect(eventsLink).toBeVisible();
      
      // Verify admin link comes before events link in the navigation
      const navItems = page.locator('[data-testid="nav-main"] .nav a');
      const adminIndex = await getElementIndex(navItems, adminLink);
      const eventsIndex = await getElementIndex(navItems, eventsLink);
      
      expect(adminIndex).toBeLessThan(eventsIndex);
    });

    test('should show user greeting in utility bar', async ({ page }) => {
      const userGreeting = page.locator('[data-testid="user-greeting"]');
      await expect(userGreeting).toBeVisible();
      await expect(userGreeting).toContainText('Welcome,');
    });

    test('should show Logout link in utility bar', async ({ page }) => {
      const logoutButton = page.locator('[data-testid="button-logout"]');
      await expect(logoutButton).toBeVisible();
      await expect(logoutButton).toHaveText('Logout');
    });
  });

  test.describe('Visual and Style Testing', () => {
    
    test('should preserve existing animations and styles', async ({ page }) => {
      // Test scroll animation
      await page.evaluate(() => window.scrollTo(0, 200));
      await page.waitForTimeout(500);
      
      const header = page.locator('[data-testid="nav-main"]');
      await expect(header).toHaveClass(/scrolled/);
    });

    test('should be responsive on mobile view', async ({ page }) => {
      await page.setViewportSize({ width: 375, height: 667 });
      
      const mobileMenuButton = page.locator('[data-testid="button-mobile-menu"]');
      await expect(mobileMenuButton).toBeVisible();
    });

    test('should show all navigation items in correct order', async ({ page }) => {
      // Login as admin to see all possible navigation items
      await loginAsUser(page, 'admin@witchcityrope.com', 'Test123!');
      
      const navItems = page.locator('[data-testid="nav-main"] .nav a, [data-testid="nav-main"] .nav .btn');
      
      // Expected order: Admin, Events & Classes, How to Join, Resources, Dashboard
      await expect(navItems.nth(0)).toContainText('Admin');
      await expect(navItems.nth(1)).toContainText('Events & Classes');
      await expect(navItems.nth(2)).toContainText('How to Join');
      await expect(navItems.nth(3)).toContainText('Resources');
      await expect(navItems.nth(4)).toContainText('Dashboard');
    });
  });
});

// Helper functions
async function loginAsUser(page: Page, email: string, password: string) {
  await page.goto('/login');
  await page.waitForLoadState('networkidle');
  
  await page.fill('[data-testid="email-input"]', email);
  await page.fill('[data-testid="password-input"]', password);
  await page.click('[data-testid="login-button"]');
  
  // Wait for successful login and redirect
  await page.waitForURL(url => !url.pathname.includes('/login'), { timeout: 10000 });
  await page.waitForLoadState('networkidle');
}

async function getElementIndex(locator: any, targetElement: any): Promise<number> {
  const count = await locator.count();
  for (let i = 0; i < count; i++) {
    const element = locator.nth(i);
    if (await element.isVisible() && await targetElement.isVisible()) {
      const elementText = await element.textContent();
      const targetText = await targetElement.textContent();
      if (elementText === targetText) {
        return i;
      }
    }
  }
  return -1;
}