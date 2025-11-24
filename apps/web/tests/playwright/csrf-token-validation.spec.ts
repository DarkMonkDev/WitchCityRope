import { test, expect, Page } from '@playwright/test'

/**
 * CSRF Token Validation Test Suite
 *
 * Tests the complete CSRF token flow:
 * 1. Login (public endpoint - no CSRF required)
 * 2. CSRF token cookie is set after login
 * 3. Logout sends CSRF token in X-CSRF-TOKEN header
 * 4. Logout with valid token succeeds (200 OK)
 * 5. User is properly logged out
 *
 * This ensures the authentication pattern migration works correctly
 * with proper CSRF protection on state-changing operations.
 */

test.describe('CSRF Token Validation', () => {

  test('should complete full login/logout flow with CSRF token', async ({ page }) => {
    let loginRequestCsrf: string | null = null
    let logoutRequestCsrf: string | null = null
    let logoutResponseStatus: number | null = null

    // Intercept auth requests to verify CSRF token presence/absence
    page.on('request', request => {
      if (request.url().includes('/api/auth/login')) {
        // Check for the actual header name used by axios interceptor
        loginRequestCsrf = request.headers()['x-csrf-token'] || null
        console.log('📝 Login request - CSRF token:', loginRequestCsrf || 'NOT PRESENT (expected)')
      }
      if (request.url().includes('/api/auth/logout')) {
        // CRITICAL: Check the ACTUAL header name used in client.ts (X-CSRF-TOKEN, not X-XSRF-TOKEN)
        logoutRequestCsrf = request.headers()['x-csrf-token'] || null
        console.log('📝 Logout request - CSRF token:', logoutRequestCsrf ? 'PRESENT ✓' : 'MISSING ✗')
      }
    })

    page.on('response', response => {
      if (response.url().includes('/api/auth/logout')) {
        logoutResponseStatus = response.status()
        console.log('📝 Logout response status:', logoutResponseStatus)
      }
    })

    // Step 1: Navigate to login page
    await page.goto('http://localhost:5173/login')
    await expect(page).toHaveURL(/.*login/)

    // Step 2: Login (public endpoint - should NOT have CSRF token)
    const emailInput = page.locator('[data-testid="email-or-scenename-input"]')
    await emailInput.waitFor({ state: 'visible', timeout: 10000 })
    await emailInput.fill('admin@witchcityrope.com')

    const passwordInput = page.locator('input[type="password"]')
    await passwordInput.fill('Test123!')

    const submitButton = page.locator('button[type="submit"]')
    await submitButton.click()

    // Step 3: Wait for successful login (redirects to dashboard)
    await expect(page).toHaveURL(/.*dashboard/, { timeout: 10000 })

    // Step 4: Verify CSRF token cookie was set after login
    const cookies = await page.context().cookies()
    const csrfCookie = cookies.find(c => c.name === 'XSRF-TOKEN')
    expect(csrfCookie, 'XSRF-TOKEN cookie should be set after login').toBeDefined()
    console.log('✓ CSRF token cookie set:', csrfCookie?.value.substring(0, 30) + '...')

    // Step 5: Verify user is logged in (logout button visible)
    const logoutButton = page.locator('[data-testid="button-logout"]')
    await expect(logoutButton).toBeVisible({ timeout: 5000 })

    // Step 6: Click logout button
    console.log('🔐 Clicking logout button...')
    await logoutButton.click()

    // Step 7: Wait for redirect to home page
    await expect(page).toHaveURL('http://localhost:5173/', { timeout: 10000 })

    // Step 8: Verify user is logged out (login button visible)
    const loginLink = page.locator('a[href="/login"]').first()
    await expect(loginLink).toBeVisible({ timeout: 5000 })

    // Step 9: Verify cannot access protected routes
    await page.goto('http://localhost:5173/dashboard')
    await expect(page).toHaveURL(/.*login/, { timeout: 5000 })

    console.log('✓ User successfully logged out and redirected to login')

    // Assertions on intercepted requests
    expect(loginRequestCsrf, 'Login should NOT have CSRF token (public endpoint)').toBeNull()
    expect(logoutRequestCsrf, 'Logout MUST have CSRF token (protected endpoint)').toBeTruthy()
    expect(logoutResponseStatus, 'Logout should return 200 OK').toBe(200)

    console.log('\n✅ CSRF Token Validation Test PASSED')
  })

  test('should handle logout with automatic CSRF token refresh', async ({ page }) => {
    // This test verifies the automatic retry logic when CSRF token is missing/expired

    // Step 1: Login
    await page.goto('http://localhost:5173/login')
    const emailInput = page.locator('[data-testid="email-or-scenename-input"]')
    await emailInput.waitFor({ state: 'visible', timeout: 10000 })
    await emailInput.fill('admin@witchcityrope.com')

    const passwordInput = page.locator('input[type="password"]')
    await passwordInput.fill('Test123!')

    const submitButton = page.locator('button[type="submit"]')
    await submitButton.click()

    await expect(page).toHaveURL(/.*dashboard/, { timeout: 10000 })

    // Step 2: Clear CSRF token cookie to simulate expired/missing token
    console.log('🧪 Clearing CSRF token to test auto-refresh...')
    await page.context().clearCookies({ name: 'XSRF-TOKEN' })

    // Step 3: Verify CSRF cookie is cleared
    let cookies = await page.context().cookies()
    let csrfCookie = cookies.find(c => c.name === 'XSRF-TOKEN')
    expect(csrfCookie, 'CSRF token should be cleared').toBeUndefined()

    // Step 4: Try to logout (should automatically fetch new token and succeed)
    const logoutButton = page.locator('[data-testid="button-logout"]')
    await logoutButton.click()

    // Step 5: Should still successfully logout despite missing token
    await expect(page).toHaveURL('http://localhost:5173/', { timeout: 10000 })

    const loginLink = page.locator('a[href="/login"]').first()
    await expect(loginLink).toBeVisible({ timeout: 5000 })

    console.log('✅ Logout succeeded with automatic CSRF token refresh')
  })

  test('should maintain CSRF token across page navigation', async ({ page }) => {
    // Verify CSRF token persists across navigation

    // Login
    await page.goto('http://localhost:5173/login')
    const emailInput = page.locator('[data-testid="email-or-scenename-input"]')
    await emailInput.waitFor({ state: 'visible', timeout: 10000 })
    await emailInput.fill('admin@witchcityrope.com')

    const passwordInput = page.locator('input[type="password"]')
    await passwordInput.fill('Test123!')

    const submitButton = page.locator('button[type="submit"]')
    await submitButton.click()

    await expect(page).toHaveURL(/.*dashboard/, { timeout: 10000 })

    // Get CSRF token after login
    let cookies = await page.context().cookies()
    let csrfCookie1 = cookies.find(c => c.name === 'XSRF-TOKEN')
    expect(csrfCookie1).toBeDefined()
    const token1 = csrfCookie1?.value

    // Navigate to different pages
    await page.goto('http://localhost:5173/events')
    await page.waitForLoadState('networkidle')

    await page.goto('http://localhost:5173/dashboard')
    await page.waitForLoadState('networkidle')

    // Get CSRF token after navigation
    cookies = await page.context().cookies()
    const csrfCookie2 = cookies.find(c => c.name === 'XSRF-TOKEN')
    expect(csrfCookie2).toBeDefined()
    const token2 = csrfCookie2?.value

    // Token should persist across navigation
    expect(token2, 'CSRF token should persist across navigation').toBe(token1)
    console.log('✓ CSRF token maintained across navigation')

    // Logout should still work
    const logoutButton = page.locator('[data-testid="button-logout"]')
    await logoutButton.click()
    await expect(page).toHaveURL('http://localhost:5173/', { timeout: 10000 })

    console.log('✅ CSRF token persistence test PASSED')
  })

  test('should verify CSRF token is httpOnly=false (readable by JavaScript)', async ({ page }) => {
    // The XSRF-TOKEN cookie must be httpOnly=false so JavaScript can read it
    // The .AspNetCore.Antiforgery cookie should be httpOnly=true for security

    await page.goto('http://localhost:5173/login')
    const emailInput = page.locator('[data-testid="email-or-scenename-input"]')
    await emailInput.waitFor({ state: 'visible', timeout: 10000 })
    await emailInput.fill('admin@witchcityrope.com')

    const passwordInput = page.locator('input[type="password"]')
    await passwordInput.fill('Test123!')

    const submitButton = page.locator('button[type="submit"]')
    await submitButton.click()

    await expect(page).toHaveURL(/.*dashboard/, { timeout: 10000 })

    // Get cookies from context (these include httpOnly cookies)
    const cookies = await page.context().cookies()
    const xsrfCookie = cookies.find(c => c.name === 'XSRF-TOKEN')
    const antiforgeryCookie = cookies.find(c => c.name.includes('Antiforgery'))

    expect(xsrfCookie, 'XSRF-TOKEN cookie should exist').toBeDefined()
    expect(xsrfCookie?.httpOnly, 'XSRF-TOKEN should be httpOnly=false (readable by JS)').toBe(false)

    if (antiforgeryCookie) {
      expect(antiforgeryCookie.httpOnly, '.AspNetCore.Antiforgery should be httpOnly=true (secure)').toBe(true)
      console.log('✓ Antiforgery cookie is httpOnly=true (secure)')
    }

    // Verify JavaScript can read XSRF-TOKEN
    const jsCanReadToken = await page.evaluate(() => {
      return document.cookie.includes('XSRF-TOKEN')
    })
    expect(jsCanReadToken, 'JavaScript should be able to read XSRF-TOKEN cookie').toBe(true)

    console.log('✅ Cookie httpOnly configuration correct')
  })
})
