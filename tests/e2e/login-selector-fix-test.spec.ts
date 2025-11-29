import { test, expect } from '@playwright/test'
import { AuthHelpers } from './test-utils/helpers/auth.helpers';

/**
 * SIMPLE LOGIN SELECTOR FIX TEST
 *
 * This test demonstrates that AuthHelper uses the correct selectors
 * and provides a reliable login method.
 */

test.describe('Login Selector Fix - AuthHelper Approach', () => {
  test('BROKEN vs WORKING selectors comparison', async ({ page }) => {
    console.log('🔍 Testing login selector patterns...')

    // Navigate to login page
    await page.goto('/login')
    await page.waitForLoadState('domcontentloaded')
    
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
    const workingEmailSelector = '[data-testid="email-or-scenename-input"]'
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

  test('WORKING: Direct login using AuthHelper', async ({ page }) => {
    console.log('✅ Demonstrating working login with AuthHelpers...')

    // Use AuthHelper for reliable login
    const loginSuccess = await AuthHelpers.loginAs(page, 'admin');

    // Verify we're on dashboard
    expect(loginSuccess).toBeTruthy();
    expect(page.url()).toContain('/dashboard')

    console.log('✅ Login successful using AuthHelper!')
  })

  test('Show current login page HTML structure for debugging', async ({ page }) => {
    console.log('🔍 Analyzing current login page HTML structure...')

    await page.goto('/login')
    await page.waitForLoadState('domcontentloaded')
    
    // Get the login form HTML
    const formHTML = await page.locator('[data-testid="login-form"]').innerHTML()
    console.log('📋 Login form HTML structure:')
    console.log(formHTML.substring(0, 1000) + '...')
    
    // Check for specific elements
    const elements = await page.evaluate(() => {
      const form = document.querySelector('[data-testid="login-form"]')
      if (!form) return { error: 'Login form not found' }

      return {
        emailInput: document.querySelector('[data-testid="email-or-scenename-input"]') ? 'EXISTS' : 'NOT FOUND',
        passwordInput: document.querySelector('[data-testid="password-input"]') ? 'EXISTS' : 'NOT FOUND',
        loginButton: document.querySelector('[data-testid="login-button"]') ? 'EXISTS' : 'NOT FOUND',
        brokenEmailSelector: document.querySelector('input[placeholder="your@email.com"]') ? 'EXISTS' : 'NOT FOUND',
        // Note: :has-text() is Playwright-specific and doesn't work in native DOM
        brokenButtonSelector: 'N/A (requires Playwright selector)'
      }
    })
    
    console.log('📊 Element availability check:')
    console.log(JSON.stringify(elements, null, 2))
    
    // This test always passes - it's for debugging
    expect(elements.emailInput).toBe('EXISTS')
  })
})