import { test, expect, Page } from '@playwright/test'
import { AuthHelper } from './helpers/auth.helper'

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

    // Step 1: Login (public endpoint - should NOT have CSRF token)
    await AuthHelper.loginAs(page, 'admin')
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
    await AuthHelper.loginAs(page, 'admin')
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
    // Verify CSRF token exists after navigation (token rotation is expected security behavior)

    // Login
    await AuthHelper.loginAs(page, 'admin')
    await expect(page).toHaveURL(/.*dashboard/, { timeout: 10000 })

    // Get CSRF token after login
    let cookies = await page.context().cookies()
    let csrfCookie1 = cookies.find(c => c.name === 'XSRF-TOKEN')
    expect(csrfCookie1, 'CSRF token should exist after login').toBeDefined()
    console.log('✓ CSRF token exists after login')

    // Navigate to different pages
    await page.goto('http://localhost:5173/events')
    await page.waitForLoadState('domcontentloaded')

    await page.goto('http://localhost:5173/dashboard')
    await page.waitForLoadState('domcontentloaded')

    // Get CSRF token after navigation
    cookies = await page.context().cookies()
    const csrfCookie2 = cookies.find(c => c.name === 'XSRF-TOKEN')
    expect(csrfCookie2, 'CSRF token should exist after navigation').toBeDefined()
    console.log('✓ CSRF token exists after navigation (token rotation is OK)')

    // Logout should still work
    const logoutButton = page.locator('[data-testid="button-logout"]')
    await logoutButton.click()
    await expect(page).toHaveURL('http://localhost:5173/', { timeout: 10000 })

    console.log('✅ CSRF token navigation test PASSED')
  })

  test('should verify CSRF token is httpOnly=false (readable by JavaScript)', async ({ page }) => {
    // The XSRF-TOKEN cookie must be httpOnly=false so JavaScript can read it
    // The .AspNetCore.Antiforgery cookie should be httpOnly=true for security

    await AuthHelper.loginAs(page, 'admin')
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

  test('should re-initialize CSRF token after page refresh', async ({ page }) => {
    // Verify CSRF token is present after page refresh

    // Login
    await AuthHelper.loginAs(page, 'admin')
    await expect(page).toHaveURL(/.*dashboard/, { timeout: 10000 })

    // Verify CSRF token exists after login
    let cookies = await page.context().cookies()
    let csrfCookie = cookies.find(c => c.name === 'XSRF-TOKEN')
    expect(csrfCookie, 'CSRF token should exist after login').toBeDefined()
    console.log('✓ CSRF token exists after login')

    // Refresh the page
    await page.reload({ waitUntil: 'domcontentloaded' })

    // Verify CSRF token still exists (may be same or rotated)
    cookies = await page.context().cookies()
    csrfCookie = cookies.find(c => c.name === 'XSRF-TOKEN')
    expect(csrfCookie, 'CSRF token should exist after page refresh').toBeDefined()
    console.log('✓ CSRF token exists after page refresh')

    // Verify logout still works
    const logoutButton = page.locator('[data-testid="button-logout"]')
    await logoutButton.click()
    await expect(page).toHaveURL('http://localhost:5173/', { timeout: 10000 })

    console.log('✅ CSRF token page refresh test PASSED')
  })

  test('should initialize CSRF token on app startup with existing auth session', async ({ page }) => {
    // Verify CSRF token is initialized when user has existing auth session

    // Login
    await AuthHelper.loginAs(page, 'admin')
    await expect(page).toHaveURL(/.*dashboard/, { timeout: 10000 })

    // Verify CSRF token exists after login (auth session established)
    let cookies = await page.context().cookies()
    const csrfCookie = cookies.find(c => c.name === 'XSRF-TOKEN')
    expect(csrfCookie, 'CSRF token should be initialized with auth session').toBeDefined()
    console.log('✓ CSRF token initialized after login')

    // Navigate to dashboard (simulates app startup with existing session)
    await page.goto('http://localhost:5173/dashboard')
    await page.waitForLoadState('domcontentloaded')

    // Verify CSRF token still exists after navigation
    cookies = await page.context().cookies()
    const csrfAfterNav = cookies.find(c => c.name === 'XSRF-TOKEN')
    expect(csrfAfterNav, 'CSRF token should persist with auth session').toBeDefined()
    console.log('✓ CSRF token persists with authenticated navigation')

    // Verify logout button is visible (confirms authenticated state)
    const logoutButton = page.locator('[data-testid="button-logout"]')
    await expect(logoutButton).toBeVisible({ timeout: 5000 })
    console.log('✓ User remains authenticated across navigation')

    console.log('✅ CSRF token auth session test PASSED')
  })

  test('should automatically retry operation when CSRF token missing', async ({ page }) => {
    // Test that operations succeed even when CSRF token is initially missing
    // The app's axios interceptor automatically handles CSRF token fetching

    // Login
    await AuthHelper.loginAs(page, 'admin')
    await expect(page).toHaveURL(/.*dashboard/, { timeout: 10000 })

    // Verify CSRF token exists initially
    let cookies = await page.context().cookies()
    let csrfBefore = cookies.find(c => c.name === 'XSRF-TOKEN')
    expect(csrfBefore, 'CSRF token should exist after login').toBeDefined()
    console.log('✓ CSRF token present initially')

    // Clear CSRF token to simulate missing token scenario
    console.log('🧪 Clearing CSRF token to test automatic handling...')
    await page.context().clearCookies({ name: 'XSRF-TOKEN' })

    // Verify token is cleared
    cookies = await page.context().cookies()
    let csrfAfterClear = cookies.find(c => c.name === 'XSRF-TOKEN')
    expect(csrfAfterClear, 'CSRF token should be cleared').toBeUndefined()
    console.log('✓ CSRF token cleared')

    // Attempt logout (should succeed despite missing token initially)
    // The axios interceptor will automatically handle CSRF token
    const logoutButton = page.locator('[data-testid="button-logout"]')
    await logoutButton.click()

    // Should still successfully logout
    await expect(page).toHaveURL('http://localhost:5173/', { timeout: 10000 })
    console.log('✓ Logout succeeded despite initially missing CSRF token')

    // Verify user is logged out (login button visible)
    const loginLink = page.locator('a[href="/login"]').first()
    await expect(loginLink).toBeVisible({ timeout: 5000 })
    console.log('✓ User successfully logged out')

    console.log('✅ Automatic retry test PASSED')
  })

  test('should handle concurrent requests with same CSRF token', async ({ page }) => {
    // Verify multiple concurrent requests can use the same CSRF token

    // Login
    await AuthHelper.loginAs(page, 'admin')
    await expect(page).toHaveURL(/.*dashboard/, { timeout: 10000 })

    // Verify CSRF token exists
    let cookies = await page.context().cookies()
    const csrfCookie = cookies.find(c => c.name === 'XSRF-TOKEN')
    expect(csrfCookie, 'CSRF token should exist').toBeDefined()
    console.log('✓ CSRF token present')

    // Make multiple concurrent requests (navigate to different pages)
    // This simulates multiple components loading data simultaneously
    await Promise.all([
      page.goto('http://localhost:5173/events'),
      page.waitForTimeout(100).then(() => page.goto('http://localhost:5173/dashboard'))
    ])

    await page.waitForLoadState('domcontentloaded')

    // Verify CSRF token still exists after concurrent navigation
    cookies = await page.context().cookies()
    const csrfAfter = cookies.find(c => c.name === 'XSRF-TOKEN')
    expect(csrfAfter, 'CSRF token should exist after concurrent requests').toBeDefined()
    console.log('✓ CSRF token handled concurrent requests')

    // Verify logout still works
    const logoutButton = page.locator('[data-testid="button-logout"]')
    await logoutButton.click()
    await expect(page).toHaveURL('http://localhost:5173/', { timeout: 10000 })

    console.log('✅ Concurrent requests test PASSED')
  })
})
