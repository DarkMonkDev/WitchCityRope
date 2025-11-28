import { test, expect } from '@playwright/test'
import { AuthHelpers } from './test-utils/helpers/auth.helpers';

/**
 * SIMPLE DEMONSTRATION OF WORKING MANTINE UI LOGIN
 *
 * This test demonstrates the correct approach for login with Mantine UI components
 * that actually works and can be used as a reference for other tests.
 */

test.describe('Demo: Working Login with Mantine UI', () => {
  test('Login successfully using AuthHelper', async ({ page }) => {
    console.log('🎯 Demonstrating working login approach for Mantine UI')

    // Use AuthHelper for clean, centralized login
    const loginSuccess = await AuthHelpers.loginAs(page, 'admin');

    // Verify we're authenticated and on the correct page
    expect(loginSuccess).toBeTruthy();
    expect(page.url()).toContain('/dashboard')

    console.log('🎉 DEMO COMPLETE: Login working successfully with AuthHelper!')
  })

  test('Show what happens with WRONG selectors (this should fail)', async ({ page }) => {
    console.log('❌ Demonstrating what DOESN\'T work - wrong selectors')

    await page.goto('/login')
    await page.waitForLoadState('networkidle')
    
    // ❌ This approach FAILS because LoginPage.tsx doesn't use name attributes
    const emailInput = page.locator('input[name="email"]')
    const passwordInput = page.locator('input[name="password"]')
    
    console.log('Attempting to use input[name="email"] selector...')
    
    try {
      // This will timeout because these elements don't exist
      await emailInput.fill('admin@witchcityrope.com', { timeout: 5000 })
      console.log('❌ This should not succeed!')
    } catch (error) {
      console.log('✅ Expected timeout: input[name="email"] selector doesn\'t work')
      console.log(`   Error: ${error.message}`)
    }
    
    // Verify the elements don't exist
    const emailCount = await page.locator('input[name="email"]').count()
    const passwordCount = await page.locator('input[name="password"]').count()
    
    console.log(`input[name="email"] elements found: ${emailCount}`)
    console.log(`input[name="password"] elements found: ${passwordCount}`)
    
    expect(emailCount).toBe(0)
    expect(passwordCount).toBe(0)
    
    console.log('✅ DEMO COMPLETE: Confirmed wrong selectors don\'t work')
  })

  test('Console error analysis - CSS warnings don\'t block login', async ({ page }) => {
    console.log('🔍 Analyzing console errors during login')
    
    const allErrors: string[] = []
    const cssWarnings: string[] = []
    
    // Capture all console errors
    page.on('console', msg => {
      if (msg.type() === 'error') {
        const errorText = msg.text()
        allErrors.push(errorText)
        
        // Identify CSS warnings from Mantine
        if (errorText.includes('style property') || 
            errorText.includes('focus-visible') || 
            errorText.includes('maxWidth')) {
          cssWarnings.push(errorText)
        }
      }
    })
    
    // Perform successful login
    await page.goto('/login')
    await page.waitForLoadState('networkidle')
    
    await page.locator('[data-testid="email-or-scenename-input"]').fill('admin@witchcityrope.com')
    await page.locator('[data-testid="password-input"]').fill('Test123!')
    await page.locator('[data-testid="login-button"]').click()
    
    // Should still succeed despite CSS warnings
    await page.waitForURL('**/dashboard', { timeout: 15000 })
    
    // Analysis
    console.log(`Total console errors: ${allErrors.length}`)
    console.log(`CSS warnings: ${cssWarnings.length}`)
    console.log(`Non-CSS errors: ${allErrors.length - cssWarnings.length}`)
    
    if (cssWarnings.length > 0) {
      console.log('CSS warnings detected (these are harmless):')
      cssWarnings.forEach((warning, index) => {
        const shortWarning = warning.substring(0, 100) + '...'
        console.log(`  ${index + 1}: ${shortWarning}`)
      })
    }
    
    // Key insight: CSS warnings don't prevent successful login
    expect(page.url()).toContain('/dashboard')
    
    console.log('✅ KEY FINDING: CSS warnings from Mantine do NOT block login functionality')
  })
})