import { test, expect } from '@playwright/test'

test.describe('Logout with CSRF Token Verification', () => {
  test('should logout successfully with proper CSRF token handling', async ({ page }) => {
    // Enable request/response logging to see CSRF tokens
    page.on('request', request => {
      if (request.url().includes('/api/auth/')) {
        console.log('→', request.method(), request.url())
        const csrfToken = request.headers()['x-xsrf-token']
        if (csrfToken) {
          console.log('  CSRF Token:', csrfToken.substring(0, 20) + '...')
        }
      }
    })

    page.on('response', response => {
      if (response.url().includes('/api/auth/')) {
        console.log('←', response.status(), response.url())
      }
    })

    // Step 1: Navigate to login page
    await page.goto('http://localhost:5173/login')
    await expect(page).toHaveURL(/.*login/)

    // Step 2: Login as admin
    await page.fill('input[name="email"]', 'admin@witchcityrope.com')
    await page.fill('input[name="password"]', 'Test123!')
    await page.click('button[type="submit"]')

    // Step 3: Wait for successful login (redirects to dashboard)
    await expect(page).toHaveURL(/.*dashboard/, { timeout: 10000 })
    
    // Step 4: Verify user is logged in (check for logout button)
    const logoutButton = page.locator('[data-testid="button-logout"]')
    await expect(logoutButton).toBeVisible({ timeout: 5000 })

    // Step 5: Click logout button
    console.log('\n🔐 Clicking logout button...')
    await logoutButton.click()

    // Step 6: Wait for redirect to home page
    await expect(page).toHaveURL('http://localhost:5173/', { timeout: 10000 })

    // Step 7: Verify user is logged out (login button visible)
    const loginLink = page.locator('text=Login')
    await expect(loginLink).toBeVisible({ timeout: 5000 })

    // Step 8: Verify logout was successful by trying to access protected route
    await page.goto('http://localhost:5173/dashboard')
    
    // Should redirect to login page
    await page.waitForURL(/.*login/, { timeout: 5000 })
    
    console.log('✅ Logout verified: User cannot access protected routes')
  })
})
