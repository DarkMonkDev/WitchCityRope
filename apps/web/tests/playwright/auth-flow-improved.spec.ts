import { test, expect } from '@playwright/test'

/**
 * Improved Authentication E2E Tests
 * 
 * Based on lessons learned and testing best practices:
 * - Uses proper wait strategies (no fixed timeouts)
 * - Uses data-testid attributes where possible
 * - Tests real API interactions
 * - Follows Arrange-Act-Assert pattern
 * - Uses existing test accounts for reliability
 */

// Use existing test accounts from the system
const testAccounts = {
  admin: {
    email: 'admin@witchcityrope.com',
    password: 'Test123!',
    expectedRole: 'admin'
  },
  member: {
    email: 'member@witchcityrope.com',
    password: 'Test123!',
    expectedRole: 'member'
  }
}

test.describe('Authentication Flow E2E', () => {
  test.beforeEach(async ({ page }) => {
    // Clear any existing auth state for clean test isolation
    await page.context().clearCookies()
    await page.goto('http://localhost:5173')
  })

  test('should complete successful login flow with admin user', async ({ page }) => {
    // Arrange - Navigate to login page
    await page.goto('/login')
    await expect(page.locator('h1')).toContainText('Login', { timeout: 10000 })

    // Act - Fill login form with existing admin account
    await page.fill('input[name="email"]', testAccounts.admin.email)
    await page.fill('input[name="password"]', testAccounts.admin.password)
    
    // Submit the form
    await page.click('button[type="submit"]')

    // Assert - Verify successful login and navigation
    await page.waitForURL('/dashboard', { timeout: 10000 })
    
    // Verify we're on the dashboard and logged in
    await expect(page.locator('text=Welcome')).toBeVisible({ timeout: 10000 })
    
    // Verify user info is displayed (indicates successful auth)
    const pageContent = await page.locator('body').textContent()
    expect(pageContent).toContain('admin@witchcityrope.com')
  })

  test('should redirect to login when accessing protected route unauthenticated', async ({ page }) => {
    // Arrange - Ensure we're not authenticated
    await page.context().clearCookies()

    // Act - Try to access protected dashboard route directly
    await page.goto('/dashboard')

    // Assert - Should redirect to login page
    await page.waitForURL('/login', { timeout: 10000 })
    await expect(page.locator('h1')).toContainText('Login')
  })

  test('should handle invalid credentials gracefully', async ({ page }) => {
    // Arrange - Navigate to login page
    await page.goto('/login')
    await expect(page.locator('h1')).toContainText('Login')

    // Act - Try to login with invalid credentials
    await page.fill('input[name="email"]', 'invalid@example.com')
    await page.fill('input[name="password"]', 'wrongpassword')
    await page.click('button[type="submit"]')

    // Assert - Should show error message and stay on login page
    await expect(page.locator('text=Invalid email or password')).toBeVisible({ timeout: 10000 })
    await expect(page).toHaveURL(/.*\/login/)
  })

  test('should complete logout flow successfully', async ({ page }) => {
    // Arrange - First login with valid user
    await page.goto('/login')
    await page.fill('input[name="email"]', testAccounts.admin.email)
    await page.fill('input[name="password"]', testAccounts.admin.password)
    await page.click('button[type="submit"]')
    
    // Wait for successful login
    await page.waitForURL('/dashboard', { timeout: 10000 })

    // Act - Logout
    await page.click('button:has-text("Logout"), button:has-text("Sign Out"), [data-testid="logout-button"]')

    // Assert - Should redirect to login page
    await page.waitForURL('/login', { timeout: 10000 })
    await expect(page.locator('h1')).toContainText('Login')

    // Verify we can't access protected routes after logout
    await page.goto('/dashboard')
    await page.waitForURL('/login', { timeout: 10000 })
  })

  test('should preserve authentication state across page refresh', async ({ page }) => {
    // Arrange - Login first
    await page.goto('/login')
    await page.fill('input[name="email"]', testAccounts.admin.email)
    await page.fill('input[name="password"]', testAccounts.admin.password)
    await page.click('button[type="submit"]')
    
    await page.waitForURL('/dashboard', { timeout: 10000 })
    const userInfoBefore = await page.locator('body').textContent()

    // Act - Refresh the page
    await page.reload()

    // Assert - Should still be authenticated and on dashboard
    await expect(page).toHaveURL('/dashboard')
    await expect(page.locator('text=Welcome')).toBeVisible({ timeout: 10000 })
    
    const userInfoAfter = await page.locator('body').textContent()
    expect(userInfoAfter).toContain('admin@witchcityrope.com')
  })

  test('should handle protected route with returnTo parameter', async ({ page }) => {
    // Arrange - Clear auth state
    await page.context().clearCookies()

    // Act - Try to access protected route
    await page.goto('/dashboard')
    
    // Should redirect to login with returnTo parameter
    await page.waitForURL('/login?returnTo=%2Fdashboard', { timeout: 10000 })

    // Login
    await page.fill('input[name="email"]', testAccounts.admin.email)
    await page.fill('input[name="password"]', testAccounts.admin.password)
    await page.click('button[type="submit"]')

    // Assert - Should redirect back to original protected route
    await page.waitForURL('/dashboard', { timeout: 10000 })
    await expect(page.locator('text=Welcome')).toBeVisible()
  })

  test('should verify authentication cookies are httpOnly (security)', async ({ page }) => {
    // Arrange - Login first
    await page.goto('/login')
    await page.fill('input[name="email"]', testAccounts.admin.email)
    await page.fill('input[name="password"]', testAccounts.admin.password)
    await page.click('button[type="submit"]')
    
    await page.waitForURL('/dashboard', { timeout: 10000 })

    // Act - Try to access auth cookies via JavaScript
    const accessibleCookies = await page.evaluate(() => {
      return document.cookie
    })

    // Assert - Auth tokens should not be accessible via JavaScript (httpOnly protection)
    expect(accessibleCookies).not.toContain('auth-token')
    expect(accessibleCookies).not.toContain('jwt')
    expect(accessibleCookies).not.toContain('session')
    
    // Verify we have some cookies but they're httpOnly
    const allCookies = await page.context().cookies()
    expect(allCookies.length).toBeGreaterThan(0)
  })
})

test.describe('Authentication Performance Tests', () => {
  test('login flow should complete within performance target', async ({ page }) => {
    // Arrange
    await page.goto('/login')
    const startTime = Date.now()

    // Act - Complete login flow
    await page.fill('input[name="email"]', testAccounts.admin.email)
    await page.fill('input[name="password"]', testAccounts.admin.password)
    await page.click('button[type="submit"]')
    
    await page.waitForURL('/dashboard', { timeout: 10000 })
    await expect(page.locator('text=Welcome')).toBeVisible()
    
    const endTime = Date.now()
    const loginDuration = endTime - startTime

    // Assert - Should complete within 3 seconds
    expect(loginDuration).toBeLessThan(3000)
  })
})

test.describe('Cross-Browser Authentication Tests', () => {
  // These tests run across all configured browsers in playwright.config.ts
  
  test('authentication should work consistently across browsers', async ({ page, browserName }) => {
    console.log(`Testing authentication in ${browserName}`)
    
    // Arrange
    await page.goto('/login')

    // Act
    await page.fill('input[name="email"]', testAccounts.admin.email)
    await page.fill('input[name="password"]', testAccounts.admin.password)
    await page.click('button[type="submit"]')

    // Assert
    await page.waitForURL('/dashboard', { timeout: 10000 })
    await expect(page.locator('text=Welcome')).toBeVisible()
    
    // Verify the authentication works the same way in all browsers
    const pageContent = await page.locator('body').textContent()
    expect(pageContent).toContain('admin@witchcityrope.com')
  })
})