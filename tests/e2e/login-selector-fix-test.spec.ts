import { test, expect } from '@playwright/test'

/**
 * SIMPLE LOGIN SELECTOR FIX TEST
 * 
 * This test demonstrates the exact selector issue and shows the fix.
 * Based on the error analysis, tests are failing because they use wrong selectors.
 */

test.describe('Login Selector Fix - Direct Approach', () => {
  test('BROKEN vs WORKING selectors comparison', async ({ page }) => {
    console.log('🔍 Testing login selector patterns...')
    
    // Navigate to login page
    await page.goto('http://localhost:5173/login')
    await page.waitForLoadState('networkidle')
    
    console.log('❌ TESTING BROKEN SELECTORS (what failing tests use):')
    
    // These are the selectors that failing tests are using
    const brokenEmailSelector = 'input[placeholder="your@email.com"]'
    const brokenPasswordSelector = 'input[type="password"]'
    const brokenLoginButtonSelector = 'button[type="submit"]:has-text("Login")'
    
    const brokenEmailExists = await page.locator(brokenEmailSelector).count()
    const brokenPasswordExists = await page.locator(brokenPasswordSelector).count()  
    const brokenButtonExists = await page.locator(brokenLoginButtonSelector).count()
    
    console.log(`  Email selector "${brokenEmailSelector}": ${brokenEmailExists} found`)
    console.log(`  Password selector "${brokenPasswordSelector}": ${brokenPasswordExists} found`)
    console.log(`  Button selector "${brokenLoginButtonSelector}": ${brokenButtonExists} found`)
    
    console.log('✅ TESTING WORKING SELECTORS (data-testid approach):')
    
    // These are the correct selectors from the working solution
    const workingEmailSelector = '[data-testid="email-input"]'
    const workingPasswordSelector = '[data-testid="password-input"]'
    const workingLoginButtonSelector = '[data-testid="login-button"]'
    
    const workingEmailExists = await page.locator(workingEmailSelector).count()
    const workingPasswordExists = await page.locator(workingPasswordSelector).count()
    const workingButtonExists = await page.locator(workingLoginButtonSelector).count()
    
    console.log(`  Email selector "${workingEmailSelector}": ${workingEmailExists} found`)
    console.log(`  Password selector "${workingPasswordSelector}": ${workingPasswordExists} found`)
    console.log(`  Button selector "${workingLoginButtonSelector}": ${workingButtonExists} found`)
    
    // Verify the working selectors find elements while broken ones don't
    expect(brokenButtonExists).toBe(0) // This is why tests fail
    expect(workingButtonExists).toBe(1) // This is why fixed tests work
    expect(workingEmailExists).toBe(1)
    expect(workingPasswordExists).toBe(1)
    
    console.log('🎯 CONCLUSION: Tests fail because they use selectors that do not exist!')
    console.log('💡 SOLUTION: Update failing tests to use data-testid selectors')
  })

  test('WORKING: Direct login using correct selectors', async ({ page }) => {
    console.log('✅ Demonstrating working login with correct selectors...')
    
    // Navigate to login page
    await page.goto('http://localhost:5173/login')
    await page.waitForLoadState('networkidle')
    
    // Wait for form to be ready
    await page.waitForSelector('[data-testid="login-form"]', { timeout: 10000 })
    
    // Use the correct selectors (data-testid)
    const emailInput = page.locator('[data-testid="email-input"]')
    const passwordInput = page.locator('[data-testid="password-input"]')
    const loginButton = page.locator('[data-testid="login-button"]')
    
    // Verify elements exist (they should with correct selectors)
    await expect(emailInput).toBeVisible({ timeout: 5000 })
    await expect(passwordInput).toBeVisible({ timeout: 5000 })
    await expect(loginButton).toBeVisible({ timeout: 5000 })
    
    // Fill in the form
    await emailInput.fill('admin@witchcityrope.com')
    await passwordInput.fill('Test123!')
    
    // Verify values were set
    const emailValue = await emailInput.inputValue()
    const passwordValue = await passwordInput.inputValue()
    expect(emailValue).toBe('admin@witchcityrope.com')
    expect(passwordValue).toBe('Test123!')
    
    console.log('✅ Form filled successfully with correct selectors')
    
    // Click login button
    await loginButton.click()
    
    // Wait for navigation (this should work)
    await page.waitForURL('**/dashboard', { timeout: 15000 })
    
    // Verify we're on dashboard
    expect(page.url()).toContain('/dashboard')
    
    console.log('✅ Login successful using data-testid selectors!')
  })

  test('Show current login page HTML structure for debugging', async ({ page }) => {
    console.log('🔍 Analyzing current login page HTML structure...')
    
    await page.goto('http://localhost:5173/login')
    await page.waitForLoadState('networkidle')
    
    // Get the login form HTML
    const formHTML = await page.locator('[data-testid="login-form"]').innerHTML()
    console.log('📋 Login form HTML structure:')
    console.log(formHTML.substring(0, 1000) + '...')
    
    // Check for specific elements
    const elements = await page.evaluate(() => {
      const form = document.querySelector('[data-testid="login-form"]')
      if (!form) return { error: 'Login form not found' }
      
      return {
        emailInput: document.querySelector('[data-testid="email-input"]') ? 'EXISTS' : 'NOT FOUND',
        passwordInput: document.querySelector('[data-testid="password-input"]') ? 'EXISTS' : 'NOT FOUND',
        loginButton: document.querySelector('[data-testid="login-button"]') ? 'EXISTS' : 'NOT FOUND',
        brokenEmailSelector: document.querySelector('input[placeholder="your@email.com"]') ? 'EXISTS' : 'NOT FOUND',
        brokenButtonSelector: document.querySelector('button[type="submit"]:has-text("Login")') ? 'EXISTS' : 'NOT FOUND'
      }
    })
    
    console.log('📊 Element availability check:')
    console.log(JSON.stringify(elements, null, 2))
    
    // This test always passes - it's for debugging
    expect(elements.emailInput).toBe('EXISTS')
  })
})