import { test, expect } from '@playwright/test'

// Test data with unique values to avoid conflicts
const testUser = {
  email: `test-${Date.now()}@example.com`,
  password: 'TestPass123',
  sceneName: `TestRigger${Date.now()}`,
}

test.describe('Authentication Vertical Slice Tests', () => {
  test.beforeEach(async ({ page }) => {
    // Ensure we start from a clean state
    await page.goto('http://localhost:5173')
  })

  test('Complete authentication flow - Registration, Login, Protected Access, Logout', async ({
    page,
  }) => {
    // Step 1: Navigate to registration page
    await page.goto('http://localhost:5173/register')
    await expect(page.locator('h1')).toContainText('Register')

    // Step 2: Fill registration form
    await page.fill('input[type="email"]', testUser.email)
    await page.fill('input[type="password"]', testUser.password)
    await page.fill('input[placeholder*="Scene"]', testUser.sceneName)

    // Step 3: Submit registration
    await page.click('button[type="submit"]')

    // Step 4: Verify redirect to welcome page after registration
    await page.waitForURL('**/welcome', { timeout: 5000 })
    await expect(page.locator('h1')).toContainText('Welcome')
    await expect(page.locator('text=/Welcome.*' + testUser.sceneName + '/i')).toBeVisible()

    // Step 5: Logout
    await page.click('button:has-text("Logout")')
    await page.waitForURL('**/login', { timeout: 5000 })

    // Step 6: Login with the created account
    await page.fill('input[type="email"]', testUser.email)
    await page.fill('input[type="password"]', testUser.password)
    await page.click('button[type="submit"]')

    // Step 7: Verify successful login and redirect
    await page.waitForURL('**/welcome', { timeout: 5000 })
    await expect(page.locator('text=/Welcome.*' + testUser.sceneName + '/i')).toBeVisible()

    // Step 8: Test protected content access
    const welcomeText = await page.locator('body').textContent()
    expect(welcomeText).toContain('successfully authenticated')
    expect(welcomeText).toContain(testUser.email)
  })

  test('Login with invalid credentials shows error', async ({ page }) => {
    await page.goto('http://localhost:5173/login')

    // Try to login with invalid credentials
    await page.fill('input[type="email"]', 'invalid@example.com')
    await page.fill('input[type="password"]', 'WrongPassword')
    await page.click('button[type="submit"]')

    // Verify error message is displayed
    await expect(page.locator('[data-testid="login-error"]')).toBeVisible()
    await expect(page.locator('[data-testid="login-error"]')).toContainText(/invalid|failed|error/i)

    // Verify we stay on login page
    await expect(page).toHaveURL(/.*\/login/)
  })

  test('Protected route redirects to login when not authenticated', async ({ page }) => {
    // Clear any existing auth state
    await page.context().clearCookies()

    // Try to access protected route directly
    await page.goto('http://localhost:5173/welcome')

    // Should redirect to login page
    await page.waitForURL('**/login', { timeout: 5000 })
    await expect(page.locator('h1')).toContainText('Login')
  })

  test('Registration with invalid data shows validation errors', async ({ page }) => {
    await page.goto('http://localhost:5173/register')

    // Submit with invalid email
    await page.fill('input[type="email"]', 'invalid-email')
    await page.fill('input[type="password"]', 'Test123')
    await page.fill('input[placeholder*="Scene"]', 'TestName')
    await page.click('button[type="submit"]')

    // Check for email validation error
    await expect(page.locator('text=/invalid.*email/i')).toBeVisible()

    // Submit with short password
    await page.fill('input[type="email"]', 'valid@example.com')
    await page.fill('input[type="password"]', 'Short1')
    await page.click('button[type="submit"]')

    // Check for password validation error
    await expect(page.locator('text=/password.*8.*characters/i')).toBeVisible()
  })

  test('Logout clears authentication state', async ({ page }) => {
    // First login with valid user
    await page.goto('http://localhost:5173/login')
    await page.fill('input[type="email"]', 'testuser@example.com')
    await page.fill('input[type="password"]', 'Test1234')
    await page.click('button[type="submit"]')

    // Wait for successful login
    await page.waitForURL('**/welcome', { timeout: 5000 })

    // Logout
    await page.click('button:has-text("Logout")')

    // Verify redirect to login
    await page.waitForURL('**/login', { timeout: 5000 })

    // Try to access protected route - should redirect to login
    await page.goto('http://localhost:5173/welcome')
    await page.waitForURL('**/login', { timeout: 5000 })
  })

  test('Session persistence - authentication survives page refresh', async ({ page }) => {
    // Login
    await page.goto('http://localhost:5173/login')
    await page.fill('input[type="email"]', 'testuser@example.com')
    await page.fill('input[type="password"]', 'Test1234')
    await page.click('button[type="submit"]')

    // Wait for successful login
    await page.waitForURL('**/welcome', { timeout: 5000 })
    const sceneNameBefore = await page.locator('text=/Welcome.*TestRigger/i').textContent()

    // Refresh the page
    await page.reload()

    // Should still be on welcome page and authenticated
    await expect(page).toHaveURL(/.*\/welcome/)
    const sceneNameAfter = await page.locator('text=/Welcome.*TestRigger/i').textContent()
    expect(sceneNameAfter).toBe(sceneNameBefore)
  })
})

test.describe('Security Tests', () => {
  test('XSS Protection - HttpOnly cookies cannot be accessed via JavaScript', async ({ page }) => {
    // Login first
    await page.goto('http://localhost:5173/login')
    await page.fill('input[type="email"]', 'testuser@example.com')
    await page.fill('input[type="password"]', 'Test1234')
    await page.click('button[type="submit"]')
    await page.waitForURL('**/welcome', { timeout: 5000 })

    // Try to access cookies via JavaScript
    const cookieValue = await page.evaluate(() => {
      return document.cookie
    })

    // HttpOnly cookie should not be accessible
    expect(cookieValue).not.toContain('auth-token')
    expect(cookieValue).not.toContain('jwt')
  })

  test('CSRF Protection - Requests include proper headers', async ({ page }) => {
    let requestHeaders: Record<string, string> | null = null

    // Intercept login request to check headers
    page.on('request', (request) => {
      if (request.url().includes('/api/auth/login')) {
        requestHeaders = request.headers()
      }
    })

    await page.goto('http://localhost:5173/login')
    await page.fill('input[type="email"]', 'testuser@example.com')
    await page.fill('input[type="password"]', 'Test1234')
    await page.click('button[type="submit"]')

    // Verify proper headers are included
    expect(requestHeaders).toBeTruthy()
    expect(requestHeaders['content-type']).toContain('application/json')
  })
})

test.describe('Performance Tests', () => {
  test('Login completes within performance target', async ({ page }) => {
    await page.goto('http://localhost:5173/login')

    const startTime = Date.now()

    await page.fill('input[type="email"]', 'testuser@example.com')
    await page.fill('input[type="password"]', 'Test1234')
    await page.click('button[type="submit"]')
    await page.waitForURL('**/welcome', { timeout: 5000 })

    const endTime = Date.now()
    const loginTime = endTime - startTime

    // Login should complete within 2 seconds
    expect(loginTime).toBeLessThan(2000)
  })

  test('Protected API calls complete within performance target', async ({ page }) => {
    // Login first
    await page.goto('http://localhost:5173/login')
    await page.fill('input[type="email"]', 'testuser@example.com')
    await page.fill('input[type="password"]', 'Test1234')
    await page.click('button[type="submit"]')
    await page.waitForURL('**/welcome', { timeout: 5000 })

    // Measure API response time
    const responseTime = await page.evaluate(async () => {
      const start = performance.now()
      await fetch('http://localhost:5655/api/protected/welcome', {
        credentials: 'include',
      })
      const end = performance.now()
      return end - start
    })

    // API calls should complete within 200ms
    expect(responseTime).toBeLessThan(200)
  })
})
