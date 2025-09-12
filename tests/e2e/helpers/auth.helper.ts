import { Page, expect } from '@playwright/test'

/**
 * RELIABLE AUTHENTICATION HELPER FOR MANTINE UI COMPONENTS
 * 
 * This helper provides multiple fallback strategies to handle Mantine UI form interactions
 * and console errors that can block form submission in E2E tests.
 * 
 * Based on testing multiple approaches, this uses the most reliable method first,
 * with fallbacks for edge cases.
 */

export interface LoginCredentials {
  email: string
  password: string
  rememberMe?: boolean
}

export interface AuthTestOptions {
  timeout?: number
  ignoreConsoleErrors?: boolean
  suppressErrorLogging?: boolean
}

export class AuthHelper {
  private static readonly DEFAULT_TIMEOUT = 15000
  private static readonly TEST_ACCOUNTS = {
    admin: { email: 'admin@witchcityrope.com', password: 'Test123!' },
    teacher: { email: 'teacher@witchcityrope.com', password: 'Test123!' },
    member: { email: 'member@witchcityrope.com', password: 'Test123!' },
    vetted: { email: 'vetted@witchcityrope.com', password: 'Test123!' },
    guest: { email: 'guest@witchcityrope.com', password: 'Test123!' }
  }

  /**
   * Primary login method using the most reliable approach for Mantine UI
   */
  static async loginAs(page: Page, accountType: keyof typeof AuthHelper.TEST_ACCOUNTS, options: AuthTestOptions = {}): Promise<boolean> {
    const credentials = this.TEST_ACCOUNTS[accountType]
    return this.loginWithCredentials(page, credentials, options)
  }

  /**
   * Login with custom credentials - uses multiple fallback strategies
   */
  static async loginWithCredentials(page: Page, credentials: LoginCredentials, options: AuthTestOptions = {}): Promise<boolean> {
    const timeout = options.timeout || this.DEFAULT_TIMEOUT
    const suppressLogging = options.suppressErrorLogging || false

    // Clear any existing authentication state
    await this.clearAuthState(page)

    try {
      // Navigate to login page
      await page.goto('http://localhost:5173/login')
      await page.waitForLoadState('networkidle')

      // Setup console error monitoring (unless suppressed)
      const consoleErrors: string[] = []
      if (!suppressLogging) {
        page.on('console', msg => {
          if (msg.type() === 'error' && !options.ignoreConsoleErrors) {
            consoleErrors.push(msg.text())
          }
        })
      }

      // Wait for login form to be ready
      await page.waitForSelector('[data-testid="login-form"]', { timeout })

      // Strategy 1: Use data-testid selectors with fill() - Most reliable for Mantine
      const success = await this.tryMantineFormFill(page, credentials, timeout)
      
      if (success) {
        // Verify we're on the dashboard
        const currentUrl = page.url()
        if (currentUrl.includes('/dashboard')) {
          if (!suppressLogging) {
            console.log('✅ Login successful - navigated to dashboard')
          }
          return true
        }
      }

      return false

    } catch (error) {
      if (!suppressLogging) {
        console.log(`❌ Login failed: ${error}`)
      }
      return false
    }
  }

  /**
   * Primary strategy: Mantine form fill with data-testid selectors
   * This is the most reliable method based on testing
   */
  private static async tryMantineFormFill(page: Page, credentials: LoginCredentials, timeout: number): Promise<boolean> {
    try {
      // Get form elements using data-testid attributes (most stable)
      const emailInput = page.locator('[data-testid="email-input"]')
      const passwordInput = page.locator('[data-testid="password-input"]')
      const loginButton = page.locator('[data-testid="login-button"]')

      // Method that works best with Mantine: direct fill()
      await emailInput.fill(credentials.email)
      await passwordInput.fill(credentials.password)

      // Handle remember me checkbox if specified
      if (credentials.rememberMe) {
        const rememberCheckbox = page.locator('[data-testid="remember-me-checkbox"]')
        await rememberCheckbox.check()
      }

      // Verify values were set correctly
      const emailValue = await emailInput.inputValue()
      const passwordValue = await passwordInput.inputValue()

      if (emailValue !== credentials.email || passwordValue !== credentials.password) {
        console.log('❌ Form values not set correctly, trying fallback method')
        return this.tryDOMManipulationFallback(page, credentials, timeout)
      }

      // Monitor authentication API call
      let authSuccess = false
      const responsePromise = page.waitForResponse(
        response => response.url().includes('/api/auth/login'),
        { timeout }
      ).then(response => {
        authSuccess = response.status() === 200
        return response
      }).catch(() => null)

      // Submit form
      await loginButton.click()

      // Wait for either navigation or API response
      const navigationPromise = page.waitForURL('**/dashboard', { timeout }).catch(() => null)
      
      await Promise.race([navigationPromise, responsePromise])

      return authSuccess && page.url().includes('/dashboard')

    } catch (error) {
      console.log('❌ Mantine form fill failed, trying fallback')
      return this.tryDOMManipulationFallback(page, credentials, timeout)
    }
  }

  /**
   * Fallback strategy: Direct DOM manipulation
   * Use when Mantine form handling fails
   */
  private static async tryDOMManipulationFallback(page: Page, credentials: LoginCredentials, timeout: number): Promise<boolean> {
    try {
      console.log('🔄 Attempting DOM manipulation fallback')

      // Direct DOM value setting to bypass Mantine event handling
      await page.evaluate((creds) => {
        const emailInput = document.querySelector('[data-testid="email-input"]') as HTMLInputElement
        const passwordInput = document.querySelector('[data-testid="password-input"]') as HTMLInputElement
        
        if (emailInput && passwordInput) {
          emailInput.value = creds.email
          passwordInput.value = creds.password
          
          // Trigger React/Mantine change events
          const inputEvent = new Event('input', { bubbles: true })
          const changeEvent = new Event('change', { bubbles: true })
          
          emailInput.dispatchEvent(inputEvent)
          emailInput.dispatchEvent(changeEvent)
          passwordInput.dispatchEvent(inputEvent)
          passwordInput.dispatchEvent(changeEvent)
        }
      }, credentials)

      // Verify values were set
      const emailValue = await page.locator('[data-testid="email-input"]').inputValue()
      const passwordValue = await page.locator('[data-testid="password-input"]').inputValue()

      if (emailValue !== credentials.email || passwordValue !== credentials.password) {
        console.log('❌ DOM manipulation also failed')
        return false
      }

      // Submit form with force option
      await page.locator('[data-testid="login-button"]').click({ force: true })

      // Wait for navigation
      await page.waitForURL('**/dashboard', { timeout })
      return true

    } catch (error) {
      console.log('❌ DOM manipulation fallback also failed')
      return false
    }
  }

  /**
   * Clear authentication state to ensure test isolation
   */
  static async clearAuthState(page: Page): Promise<void> {
    try {
      // Clear cookies and permissions
      await page.context().clearCookies()
      await page.context().clearPermissions()
      
      // Navigate to login page first to establish context for storage access
      await page.goto('http://localhost:5173/login')
      
      // Clear storage safely
      await page.evaluate(() => {
        if (typeof localStorage !== 'undefined') {
          localStorage.clear()
        }
        if (typeof sessionStorage !== 'undefined') {
          sessionStorage.clear()
        }
      })
    } catch (error) {
      console.warn('Storage clearing failed, but continuing:', error)
    }
  }

  /**
   * Logout helper
   */
  static async logout(page: Page): Promise<void> {
    try {
      // Look for logout button/link - adjust selector based on your UI
      const logoutButton = page.locator('[data-testid="logout-button"], a[href="/logout"], button:has-text("Logout"), button:has-text("Sign Out")')
      
      if (await logoutButton.count() > 0) {
        await logoutButton.click()
        await page.waitForURL('**/login', { timeout: 10000 })
      } else {
        // Fallback: navigate directly to logout endpoint
        await page.goto('http://localhost:5173/logout')
      }
    } catch (error) {
      console.warn('Logout failed, clearing auth state manually:', error)
      await this.clearAuthState(page)
    }
  }

  /**
   * Verify user is authenticated
   */
  static async isAuthenticated(page: Page): Promise<boolean> {
    try {
      const currentUrl = page.url()
      
      // Check if on dashboard or other authenticated page
      if (currentUrl.includes('/dashboard') || currentUrl.includes('/admin') || currentUrl.includes('/profile')) {
        return true
      }
      
      // Check for authentication indicators in the DOM
      const authIndicators = await page.locator('[data-testid="user-menu"], .user-avatar, [data-testid="logout-button"]').count()
      return authIndicators > 0
      
    } catch (error) {
      return false
    }
  }

  /**
   * Get current user info from the page (if available)
   */
  static async getCurrentUserInfo(page: Page): Promise<{email?: string, role?: string} | null> {
    try {
      return await page.evaluate(() => {
        // Try to get user info from common DOM locations
        const userEmail = document.querySelector('[data-testid="user-email"]')?.textContent
        const userRole = document.querySelector('[data-testid="user-role"]')?.textContent
        
        // Or try to get from JavaScript globals/storage
        const authData = sessionStorage.getItem('auth') || localStorage.getItem('auth')
        if (authData) {
          try {
            const parsed = JSON.parse(authData)
            return {
              email: userEmail || parsed.email,
              role: userRole || parsed.role
            }
          } catch (e) {
            // Ignore parsing errors
          }
        }
        
        return userEmail || userRole ? { email: userEmail || undefined, role: userRole || undefined } : null
      })
    } catch (error) {
      return null
    }
  }

  /**
   * Wait for authentication state to be ready
   */
  static async waitForAuthReady(page: Page, timeout: number = 10000): Promise<void> {
    try {
      // Wait for either login form or authenticated content
      await Promise.race([
        page.waitForSelector('[data-testid="login-form"]', { timeout }),
        page.waitForSelector('[data-testid="user-menu"], .dashboard-content, [data-testid="authenticated-layout"]', { timeout })
      ])
    } catch (error) {
      // Continue - page might be in a different state
    }
  }

  /**
   * Monitor console errors during authentication
   */
  static setupConsoleErrorMonitoring(page: Page, onError?: (error: string) => void): void {
    page.on('console', msg => {
      if (msg.type() === 'error') {
        const errorText = msg.text()
        console.log(`Console Error: ${errorText}`)
        onError?.(errorText)
      }
    })

    page.on('pageerror', error => {
      const errorText = error.toString()
      console.log(`JavaScript Error: ${errorText}`)
      onError?.(errorText)
    })
  }
}

/**
 * Simple login function for quick use
 */
export async function quickLogin(page: Page, accountType: keyof typeof AuthHelper['TEST_ACCOUNTS'] = 'admin'): Promise<void> {
  const success = await AuthHelper.loginAs(page, accountType)
  if (!success) {
    throw new Error(`Failed to login as ${accountType}`)
  }
}

/**
 * Export test accounts for reference
 */
export const TEST_ACCOUNTS = {
  admin: { email: 'admin@witchcityrope.com', password: 'Test123!' },
  teacher: { email: 'teacher@witchcityrope.com', password: 'Test123!' },
  member: { email: 'member@witchcityrope.com', password: 'Test123!' },
  vetted: { email: 'vetted@witchcityrope.com', password: 'Test123!' },
  guest: { email: 'guest@witchcityrope.com', password: 'Test123!' }
} as const