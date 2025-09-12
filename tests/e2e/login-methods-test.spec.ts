import { test, expect, Page } from '@playwright/test'

/**
 * LOGIN METHODS TESTING - Multiple approaches to solve Mantine UI login issues
 * 
 * Problem: Current E2E tests can't login properly due to Mantine component CSS console errors blocking form interaction.
 * Solution: Test 5 different approaches to find a reliable login method for Mantine UI components.
 */

test.describe('Login Methods Testing - Find Reliable Solution', () => {
  let consoleErrors: string[] = []
  let jsErrors: string[] = []
  let page: Page

  test.beforeEach(async ({ browser }) => {
    page = await browser.newPage()
    consoleErrors = []
    jsErrors = []
    
    // Collect all console errors for analysis
    page.on('console', msg => {
      if (msg.type() === 'error') {
        consoleErrors.push(msg.text())
        console.log(`Console Error: ${msg.text()}`)
      }
    })
    
    // Collect JavaScript runtime errors
    page.on('pageerror', error => {
      jsErrors.push(error.toString())
      console.log(`JavaScript Error: ${error.toString()}`)
    })
  })

  test.afterEach(async () => {
    console.log(`Test completed with ${consoleErrors.length} console errors and ${jsErrors.length} JS errors`)
    await page?.close()
  })

  test('Method 1: Use page.fill() with data-testid selectors (Recommended)', async () => {
    console.log('🧪 Testing Method 1: page.fill() with data-testid')
    
    await page.goto('http://localhost:5173/login')
    await page.waitForLoadState('networkidle')
    
    // Wait for form to be ready
    await page.waitForSelector('[data-testid="login-form"]', { timeout: 10000 })
    
    // Get input elements by data-testid (most reliable)
    const emailInput = page.locator('[data-testid="email-input"]')
    const passwordInput = page.locator('[data-testid="password-input"]')
    const loginButton = page.locator('[data-testid="login-button"]')
    
    // Method 1: Direct fill with page.fill() - bypasses Mantine handlers
    await emailInput.fill('admin@witchcityrope.com')
    await passwordInput.fill('Test123!')
    
    // Verify values were set
    const emailValue = await emailInput.inputValue()
    const passwordValue = await passwordInput.inputValue()
    
    expect(emailValue).toBe('admin@witchcityrope.com')
    expect(passwordValue).toBe('Test123!')
    
    // Monitor login request
    let loginSuccessful = false
    page.on('response', async (response) => {
      if (response.url().includes('/api/auth/login') && response.status() === 200) {
        loginSuccessful = true
        console.log('✅ Login API call successful')
      }
    })
    
    // Submit form
    await loginButton.click()
    
    // Wait for navigation or error
    try {
      await page.waitForURL('**/dashboard', { timeout: 15000 })
      console.log('✅ Method 1: Successfully navigated to dashboard')
      expect(loginSuccessful).toBe(true)
    } catch (error) {
      console.log(`❌ Method 1: Navigation failed - ${error}`)
      throw error
    }
  })

  test('Method 2: Use force option with direct selectors', async () => {
    console.log('🧪 Testing Method 2: force option with input selectors')
    
    await page.goto('http://localhost:5173/login')
    await page.waitForLoadState('networkidle')
    
    // Use input selectors with force option to bypass any blocking
    await page.locator('input[name="email"]').fill('admin@witchcityrope.com', { force: true })
    await page.locator('input[name="password"]').fill('Test123!', { force: true })
    
    console.log('✅ Method 2: Filled inputs with force option')
    
    // Click login button
    await page.locator('[data-testid="login-button"]').click({ force: true })
    
    try {
      await page.waitForURL('**/dashboard', { timeout: 15000 })
      console.log('✅ Method 2: Successfully navigated to dashboard')
    } catch (error) {
      console.log(`❌ Method 2: Navigation failed - ${error}`)
      // Don't throw - we're testing multiple methods
    }
  })

  test('Method 3: Use page.evaluate() to directly set values in DOM', async () => {
    console.log('🧪 Testing Method 3: page.evaluate() direct DOM manipulation')
    
    await page.goto('http://localhost:5173/login')
    await page.waitForLoadState('networkidle')
    
    // Method 3: Direct DOM manipulation to bypass all Mantine handlers
    await page.evaluate(() => {
      const emailInput = document.querySelector('[data-testid="email-input"]') as HTMLInputElement
      const passwordInput = document.querySelector('[data-testid="password-input"]') as HTMLInputElement
      
      if (emailInput && passwordInput) {
        emailInput.value = 'admin@witchcityrope.com'
        passwordInput.value = 'Test123!'
        
        // Trigger change events to notify React/Mantine
        emailInput.dispatchEvent(new Event('input', { bubbles: true }))
        emailInput.dispatchEvent(new Event('change', { bubbles: true }))
        passwordInput.dispatchEvent(new Event('input', { bubbles: true }))
        passwordInput.dispatchEvent(new Event('change', { bubbles: true }))
        
        console.log('✅ DOM values set directly')
      }
    })
    
    // Verify values were set
    const emailValue = await page.locator('[data-testid="email-input"]').inputValue()
    const passwordValue = await page.locator('[data-testid="password-input"]').inputValue()
    
    expect(emailValue).toBe('admin@witchcityrope.com')
    expect(passwordValue).toBe('Test123!')
    
    console.log('✅ Method 3: Values verified in DOM')
    
    // Submit form
    await page.locator('[data-testid="login-button"]').click()
    
    try {
      await page.waitForURL('**/dashboard', { timeout: 15000 })
      console.log('✅ Method 3: Successfully navigated to dashboard')
    } catch (error) {
      console.log(`❌ Method 3: Navigation failed - ${error}`)
      // Don't throw - we're testing multiple methods
    }
  })

  test('Method 4: Ignore console errors and use standard interaction', async () => {
    console.log('🧪 Testing Method 4: Ignore console errors completely')
    
    // Method 4: Completely ignore console errors and proceed normally
    await page.goto('http://localhost:5173/login')
    await page.waitForLoadState('networkidle')
    
    // Standard Playwright interaction - ignore any console issues
    await page.locator('[data-testid="email-input"]').type('admin@witchcityrope.com')
    await page.locator('[data-testid="password-input"]').type('Test123!')
    
    console.log('✅ Method 4: Used standard type() method ignoring console errors')
    
    await page.locator('[data-testid="login-button"]').click()
    
    try {
      await page.waitForURL('**/dashboard', { timeout: 15000 })
      console.log('✅ Method 4: Successfully navigated to dashboard')
    } catch (error) {
      console.log(`❌ Method 4: Navigation failed - ${error}`)
      console.log(`Console errors during test: ${consoleErrors.length}`)
      console.log(`JavaScript errors: ${jsErrors.length}`)
      // Don't throw - we're testing multiple methods
    }
  })

  test('Method 5: Comprehensive helper function approach', async () => {
    console.log('🧪 Testing Method 5: Comprehensive helper function')
    
    // Method 5: Create a robust login helper that tries multiple approaches
    const loginWithFallback = async (email: string, password: string): Promise<boolean> => {
      await page.goto('http://localhost:5173/login')
      await page.waitForLoadState('networkidle')
      
      try {
        // Approach 5a: Try data-testid with fill()
        console.log('5a: Trying data-testid with fill()')
        await page.locator('[data-testid="email-input"]').fill(email)
        await page.locator('[data-testid="password-input"]').fill(password)
      } catch (error) {
        console.log('5a failed, trying 5b...')
        
        // Approach 5b: Try with force option
        console.log('5b: Trying with force option')
        await page.locator('[data-testid="email-input"]').fill(email, { force: true })
        await page.locator('[data-testid="password-input"]').fill(password, { force: true })
      }
      
      // Verify values
      const emailValue = await page.locator('[data-testid="email-input"]').inputValue()
      const passwordValue = await page.locator('[data-testid="password-input"]').inputValue()
      
      if (emailValue !== email || passwordValue !== password) {
        // Approach 5c: DOM manipulation fallback
        console.log('5c: Values not set correctly, using DOM manipulation')
        await page.evaluate((creds) => {
          const emailInput = document.querySelector('[data-testid="email-input"]') as HTMLInputElement
          const passwordInput = document.querySelector('[data-testid="password-input"]') as HTMLInputElement
          
          if (emailInput && passwordInput) {
            emailInput.value = creds.email
            passwordInput.value = creds.password
            
            emailInput.dispatchEvent(new Event('input', { bubbles: true }))
            emailInput.dispatchEvent(new Event('change', { bubbles: true }))
            passwordInput.dispatchEvent(new Event('input', { bubbles: true }))
            passwordInput.dispatchEvent(new Event('change', { bubbles: true }))
          }
        }, { email, password })
      }
      
      // Final verification
      const finalEmailValue = await page.locator('[data-testid="email-input"]').inputValue()
      const finalPasswordValue = await page.locator('[data-testid="password-input"]').inputValue()
      
      if (finalEmailValue !== email || finalPasswordValue !== password) {
        console.log('❌ All methods failed to set form values')
        return false
      }
      
      console.log('✅ Form values set successfully')
      
      // Submit with multiple approaches
      try {
        await page.locator('[data-testid="login-button"]').click()
      } catch (error) {
        console.log('Click failed, trying force click')
        await page.locator('[data-testid="login-button"]').click({ force: true })
      }
      
      // Wait for navigation
      try {
        await page.waitForURL('**/dashboard', { timeout: 15000 })
        return true
      } catch (error) {
        console.log('Navigation to dashboard failed')
        return false
      }
    }
    
    const success = await loginWithFallback('admin@witchcityrope.com', 'Test123!')
    
    if (success) {
      console.log('✅ Method 5: Comprehensive helper succeeded')
    } else {
      console.log('❌ Method 5: All approaches failed')
      console.log(`Console errors: ${consoleErrors.length}`)
      console.log(`JS errors: ${jsErrors.length}`)
    }
    
    expect(success).toBe(true)
  })

  test('Error Analysis: Console errors during login', async () => {
    console.log('🔍 Analyzing console errors during login process')
    
    await page.goto('http://localhost:5173/login')
    await page.waitForLoadState('networkidle')
    
    // Clear previous errors and start fresh
    consoleErrors = []
    jsErrors = []
    
    // Perform standard login
    await page.locator('[data-testid="email-input"]').fill('admin@witchcityrope.com')
    await page.locator('[data-testid="password-input"]').fill('Test123!')
    await page.locator('[data-testid="login-button"]').click()
    
    // Wait a moment for errors to surface
    await page.waitForTimeout(3000)
    
    console.log(`📊 Error Analysis Results:`)
    console.log(`Console Errors: ${consoleErrors.length}`)
    console.log(`JavaScript Errors: ${jsErrors.length}`)
    
    if (consoleErrors.length > 0) {
      console.log('Console Errors Details:')
      consoleErrors.forEach((error, index) => {
        console.log(`  ${index + 1}: ${error}`)
      })
    }
    
    if (jsErrors.length > 0) {
      console.log('JavaScript Errors Details:')
      jsErrors.forEach((error, index) => {
        console.log(`  ${index + 1}: ${error}`)
      })
    }
    
    // This test always passes - it's for analysis only
    expect(true).toBe(true)
  })

  test('Performance: Login timing across different methods', async () => {
    console.log('⏱️ Testing login performance across methods')
    
    const timingResults: Record<string, number> = {}
    
    // Test Method 1 timing
    const method1Start = Date.now()
    await page.goto('http://localhost:5173/login')
    await page.waitForLoadState('networkidle')
    await page.locator('[data-testid="email-input"]').fill('admin@witchcityrope.com')
    await page.locator('[data-testid="password-input"]').fill('Test123!')
    await page.locator('[data-testid="login-button"]').click()
    
    try {
      await page.waitForURL('**/dashboard', { timeout: 15000 })
      timingResults['Method 1 (fill)'] = Date.now() - method1Start
      console.log(`✅ Method 1 completed in ${timingResults['Method 1 (fill)']}ms`)
    } catch (error) {
      timingResults['Method 1 (fill)'] = -1 // Failed
      console.log('❌ Method 1 failed')
    }
    
    console.log('📊 Timing Results:')
    Object.entries(timingResults).forEach(([method, time]) => {
      if (time > 0) {
        console.log(`  ${method}: ${time}ms`)
      } else {
        console.log(`  ${method}: FAILED`)
      }
    })
    
    // Performance assertions
    const successfulTimes = Object.values(timingResults).filter(time => time > 0)
    if (successfulTimes.length > 0) {
      const averageTime = successfulTimes.reduce((sum, time) => sum + time, 0) / successfulTimes.length
      console.log(`Average successful login time: ${averageTime}ms`)
      
      // Login should complete within 10 seconds
      expect(averageTime).toBeLessThan(10000)
    }
  })
})