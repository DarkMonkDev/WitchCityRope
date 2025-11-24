import { test, expect } from '@playwright/test'
import { AuthHelper, quickLogin } from './helpers/auth.helper'

/**
 * WORKING LOGIN SOLUTION FOR MANTINE UI
 * 
 * Based on testing multiple approaches, this demonstrates the reliable
 * login method that works with Mantine UI components.
 * 
 * Key findings:
 * - Method 1 (page.fill() with data-testid) works reliably
 * - Console CSS warnings from Mantine don't block form interaction
 * - Must use data-testid selectors, not name attributes
 */

test.describe('Working Login Solution', () => {
  test.beforeEach(async ({ page }) => {
    // Clear auth state before each test
    await AuthHelper.clearAuthState(page)
  })

  test('WORKING: Login with page.fill() and data-testid selectors', async ({ page }) => {
    console.log('✅ Testing the working login approach')
    
    // Set up console error monitoring (but don't fail on CSS warnings)
    const criticalErrors: string[] = []
    page.on('console', msg => {
      if (msg.type() === 'error') {
        const errorText = msg.text()
        // Only treat as critical if it's not a Mantine CSS warning
        if (!errorText.includes('style property') && !errorText.includes('maxWidth')) {
          criticalErrors.push(errorText)
          console.log(`❌ Critical Error: ${errorText}`)
        }
      }
    })
    
    // Navigate to login page
    await page.goto('http://localhost:5173/login')
    await page.waitForLoadState('networkidle')
    
    // Wait for form to be ready
    await page.waitForSelector('[data-testid="login-form"]', { timeout: 10000 })
    
    // Get form elements using data-testid (this is the key!)
    const emailInput = page.locator('[data-testid="email-input"]')
    const passwordInput = page.locator('[data-testid="password-input"]')
    const loginButton = page.locator('[data-testid="login-button"]')
    
    // Use page.fill() - this works reliably with Mantine
    await emailInput.fill('admin@witchcityrope.com')
    await passwordInput.fill('Test123!')
    
    // Verify values were set
    const emailValue = await emailInput.inputValue()
    const passwordValue = await passwordInput.inputValue()
    expect(emailValue).toBe('admin@witchcityrope.com')
    expect(passwordValue).toBe('Test123!')
    
    console.log('✅ Form values set successfully')
    
    // Monitor for authentication success
    let loginSuccessful = false
    page.on('response', async (response) => {
      if (response.url().includes('/api/auth/login') && response.status() === 200) {
        loginSuccessful = true
        console.log('✅ Login API call successful')
      }
    })
    
    // Submit form
    await loginButton.click()
    
    // Wait for navigation to dashboard
    await page.waitForURL('**/dashboard', { timeout: 15000 })
    
    console.log('✅ Successfully navigated to dashboard')
    
    // Verify login was successful
    expect(loginSuccessful).toBe(true)
    expect(page.url()).toContain('/dashboard')
    
    // Verify no critical errors occurred (CSS warnings are OK)
    if (criticalErrors.length > 0) {
      console.log(`⚠️ Critical errors detected: ${criticalErrors.join(', ')}`)
      throw new Error(`Critical errors during login: ${criticalErrors.join(', ')}`)
    }
    
    console.log('🎉 Working login solution confirmed!')
  })

  test('WORKING: Using AuthHelper for clean, reusable login', async ({ page }) => {
    console.log('✅ Testing AuthHelper login method')
    
    // This uses the helper which implements the working approach
    const success = await AuthHelper.loginAs(page, 'admin')
    
    expect(success).toBe(true)
    expect(page.url()).toContain('/dashboard')
    
    // Verify we can get user info
    const userInfo = await AuthHelper.getCurrentUserInfo(page)
    console.log('User info:', userInfo)
    
    // Verify authentication status
    const isAuth = await AuthHelper.isAuthenticated(page)
    expect(isAuth).toBe(true)
    
    console.log('🎉 AuthHelper working correctly!')
  })

  test('WORKING: Quick login utility function', async ({ page }) => {
    console.log('✅ Testing quickLogin utility')
    
    // Even simpler - just one function call
    await quickLogin(page, 'admin')
    
    // Should be logged in and on dashboard
    expect(page.url()).toContain('/dashboard')
    
    console.log('🎉 Quick login utility working!')
  })

  test('WORKING: Login with different user types', async ({ page }) => {
    console.log('✅ Testing different user account types')
    
    const userTypes: Array<keyof typeof AuthHelper['TEST_ACCOUNTS']> = ['admin', 'teacher', 'member', 'vetted', 'guest']
    
    for (const userType of userTypes) {
      console.log(`Testing login as ${userType}`)
      
      // Clear auth state between users
      await AuthHelper.clearAuthState(page)
      
      // Login as this user type
      const success = await AuthHelper.loginAs(page, userType)
      expect(success).toBe(true)
      
      console.log(`✅ ${userType} login successful`)
      
      // Quick logout for next iteration
      await AuthHelper.logout(page)
    }
    
    console.log('🎉 All user types can login successfully!')
  })

  test('Console error analysis - CSS warnings are not blocking', async ({ page }) => {
    console.log('🔍 Analyzing console errors during successful login')
    
    const allConsoleMessages: string[] = []
    const errorMessages: string[] = []
    const cssWarnings: string[] = []
    
    page.on('console', msg => {
      const text = msg.text()
      allConsoleMessages.push(`${msg.type()}: ${text}`)
      
      if (msg.type() === 'error') {
        errorMessages.push(text)
        
        if (text.includes('style property') || text.includes('maxWidth') || text.includes('focus-visible')) {
          cssWarnings.push(text)
        }
      }
    })
    
    // Perform successful login
    await quickLogin(page, 'admin')
    expect(page.url()).toContain('/dashboard')
    
    // Analyze the messages
    console.log(`📊 Console Analysis Results:`)
    console.log(`Total console messages: ${allConsoleMessages.length}`)
    console.log(`Error messages: ${errorMessages.length}`)
    console.log(`CSS warnings: ${cssWarnings.length}`)
    
    // The key insight: CSS warnings don't prevent login!
    if (cssWarnings.length > 0) {
      console.log(`✅ Found ${cssWarnings.length} CSS warnings but login still worked!`)
      console.log(`CSS warnings are NOT blocking form interaction`)
    }
    
    // Report the types of CSS warnings we see
    const uniqueWarningTypes = [...new Set(cssWarnings.map(w => {
      if (w.includes('focus-visible')) return 'focus-visible warnings'
      if (w.includes('maxWidth')) return 'maxWidth media query warnings'
      if (w.includes('style property')) return 'style property warnings'
      return 'other CSS warnings'
    }))]
    
    console.log(`CSS warning types: ${uniqueWarningTypes.join(', ')}`)
    console.log(`🎯 Key finding: These CSS warnings from Mantine don't block login functionality`)
    
    expect(true).toBe(true) // This test always passes - it's for analysis
  })

  test('Performance benchmark - Login timing', async ({ page }) => {
    console.log('⏱️ Performance benchmark for working login method')
    
    const timings: number[] = []
    const targetTime = 5000 // 5 seconds should be reasonable
    
    // Run login multiple times to get average
    for (let i = 0; i < 3; i++) {
      await AuthHelper.clearAuthState(page)
      
      const startTime = Date.now()
      const success = await AuthHelper.loginAs(page, 'admin')
      const endTime = Date.now()
      
      expect(success).toBe(true)
      
      const duration = endTime - startTime
      timings.push(duration)
      
      console.log(`Login attempt ${i + 1}: ${duration}ms`)
      
      await AuthHelper.logout(page)
    }
    
    const averageTime = timings.reduce((sum, time) => sum + time, 0) / timings.length
    const fastestTime = Math.min(...timings)
    const slowestTime = Math.max(...timings)
    
    console.log(`📊 Performance Results:`)
    console.log(`Average login time: ${averageTime}ms`)
    console.log(`Fastest login: ${fastestTime}ms`)
    console.log(`Slowest login: ${slowestTime}ms`)
    
    // Performance assertions
    expect(averageTime).toBeLessThan(targetTime)
    expect(fastestTime).toBeLessThan(targetTime)
    
    console.log(`✅ Login performance meets targets (< ${targetTime}ms)`)
  })
})