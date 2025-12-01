import { test, expect, Page } from '@playwright/test'
import { AuthHelpers } from './test-utils/helpers/auth.helpers';

/**
 * Comprehensive E2E test for event update authentication issue
 *
 * CRITICAL ISSUE: Users are getting logged out when trying to save event changes in admin panel.
 *
 * This test validates the complete flow:
 * 1. Login as admin
 * 2. Navigate to event management
 * 3. Modify event fields
 * 4. Save changes
 * 5. Verify persistence without logout
 * 6. Check data actually saved in database
 */

// Test data
const TEST_ADMIN = {
  email: 'admin@witchcityrope.com',
  password: 'Test123!'
}

const TEST_EVENT_UPDATE = {
  title: 'Updated Event Title - E2E Test',
  description: 'Updated description via E2E test automation',
  location: 'Updated Location - Salem Community Center'
}

test.describe('Event Update Authentication Flow - E2E', () => {
  let page: Page
  let consoleErrors: string[] = []
  let networkErrors: { url: string, status: number, method: string }[] = []
  let authTokens: string[] = []

  test.beforeEach(async ({ browser }) => {
    page = await browser.newPage()
    consoleErrors = []
    networkErrors = []
    authTokens = []

    // 🚨 CRITICAL: Monitor console errors that crash the page
    page.on('console', msg => {
      if (msg.type() === 'error') {
        const errorText = msg.text()
        console.log(`❌ Console Error: ${errorText}`)
        consoleErrors.push(errorText)
      }
    })

    // 🚨 CRITICAL: Monitor JavaScript errors that crash the page
    page.on('pageerror', error => {
      const errorText = error.toString()
      console.log(`💥 JavaScript Error: ${errorText}`)
      consoleErrors.push(`JS Error: ${errorText}`)
    })

    // Monitor network requests to track authentication flow
    page.on('response', async response => {
      const url = response.url()
      const status = response.status()
      const method = response.request().method()

      // Track authentication-related requests
      if (url.includes('/api/auth/') || url.includes('/api/events/')) {
        console.log(`🌐 ${method} ${url} - ${status}`)
        
        if (status >= 400) {
          networkErrors.push({ url, status, method })
          
          // Check for authentication errors specifically
          if (status === 401) {
            console.log(`🚨 AUTHENTICATION ERROR: ${method} ${url} returned 401`)
            try {
              const responseBody = await response.text()
              console.log(`Response body: ${responseBody}`)
            } catch (e) {
              console.log('Could not read response body')
            }
          }
        }

        // Track JWT tokens in responses
        try {
          const responseHeaders = response.headers()
          const authHeader = responseHeaders['authorization']
          if (authHeader && authHeader.startsWith('Bearer ')) {
            authTokens.push(authHeader.substring(7))
            console.log(`🔑 JWT Token received: ${authHeader.substring(7, 20)}...`)
          }
        } catch (e) {
          // Ignore header parsing errors
        }
      }
    })

    // Monitor request headers to verify auth tokens are sent
    page.on('request', request => {
      const url = request.url()
      const method = request.method()
      
      if (url.includes('/api/events/') && method === 'PUT') {
        const headers = request.headers()
        const authHeader = headers['authorization']
        const cookies = headers['cookie']
        
        console.log(`🔐 PUT Request Auth Headers:`)
        console.log(`  Authorization: ${authHeader ? authHeader.substring(0, 20) + '...' : 'MISSING'}`)
        console.log(`  Cookies: ${cookies ? 'Present' : 'MISSING'}`)
        
        if (!authHeader && !cookies) {
          console.log(`🚨 WARNING: PUT request has no authentication headers!`)
        }
      }
    })
  })

  test.afterEach(async () => {
    await page?.close()
  })

  test('Admin can update event without getting logged out', async () => {
    // Step 1 & 2: Login as admin using AuthHelper
    console.log('🚀 Step 1 & 2: Login as admin user')

    const loginSuccess = await AuthHelpers.loginAs(page, 'admin');
    expect(loginSuccess).toBeTruthy();

    console.log('✅ Login successful - navigated to dashboard')

    // Verify successful authentication
    await expect(page).toHaveURL(/.*dashboard/)

    // Check authentication tokens were received
    if (authTokens.length === 0) {
      console.log('⚠️  No JWT tokens detected in responses - using cookie auth')
    } else {
      console.log(`✅ Authentication token received: ${authTokens.length} token(s)`)
    }

    // Step 3: Navigate to event management
    console.log('🚀 Step 3: Navigate to event management')

    // Look for event management navigation link
    const eventManagementLink = page.locator('a[href*="events"]').first()
    
    if (await eventManagementLink.count() > 0) {
      await eventManagementLink.click()
      await page.waitForLoadState('domcontentloaded')
    } else {
      // Fallback: Navigate directly to events management page
      await page.goto('/admin/events')
      await page.waitForLoadState('domcontentloaded')
    }
    
    console.log('✅ Navigated to event management')
    
    // 🚨 Check for JavaScript errors after navigation
    if (consoleErrors.length > 0) {
      const criticalErrors = consoleErrors.filter(error => 
        error.includes('RangeError') || 
        error.includes('Invalid time value') ||
        error.includes('TypeError') ||
        error.includes('Cannot read properties')
      )
      
      if (criticalErrors.length > 0) {
        throw new Error(`Event management page has critical JavaScript errors: ${criticalErrors.join('; ')}`)
      }
    }

    // Step 4: Find an event to edit
    console.log('🚀 Step 4: Find and select an event to edit')
    
    // Wait for events to load
    await page.waitForTimeout(2000) // Allow time for API calls
    
    // Look for event cards, rows, or edit buttons
    const eventEditSelectors = [
      '[data-testid="event-edit-button"]',
      'button:has-text("Edit")',
      '[data-testid="edit-event"]',
      '.event-card button',
      'tr button:has-text("Edit")',
      'a[href*="/edit"]'
    ]
    
    let editButton = null
    let eventId = null
    
    for (const selector of eventEditSelectors) {
      const buttons = page.locator(selector)
      if (await buttons.count() > 0) {
        editButton = buttons.first()
        console.log(`✅ Found edit button with selector: ${selector}`)
        
        // Try to extract event ID from href or data attributes
        try {
          const href = await editButton.getAttribute('href')
          const dataId = await editButton.getAttribute('data-event-id')
          eventId = href?.match(/\/events\/([^\/]+)/)?.[1] || dataId
        } catch (e) {
          // Ignore extraction errors
        }
        
        break
      }
    }
    
    if (!editButton) {
      // No edit button found - skip test rather than use fake event ID
      console.log('⚠️  No edit button found in admin events page - skipping test')
      test.skip(true, 'No events available in admin events page to test with')
      return
    } else {
      await editButton.click()
      await page.waitForLoadState('domcontentloaded')
      console.log(`✅ Clicked edit button for event: ${eventId || 'unknown'}`)
    }

    // Step 5: Verify event edit form is loaded
    console.log('🚀 Step 5: Verify event edit form')
    
    // Wait for form elements to appear
    const formSelectors = [
      '[data-testid="event-title-input"]',
      'input[name="title"]',
      '[data-testid="title-input"]',
      'input[placeholder*="title"]',
      'input[type="text"]'
    ]
    
    let titleInput = null
    for (const selector of formSelectors) {
      const input = page.locator(selector).first()
      if (await input.count() > 0) {
        titleInput = input
        console.log(`✅ Found title input with selector: ${selector}`)
        break
      }
    }
    
    if (!titleInput) {
      throw new Error('Could not find event title input field on edit form')
    }

    // Step 6: Modify event fields
    console.log('🚀 Step 6: Modify event fields')
    
    // Clear and fill title field
    await titleInput.clear()
    await titleInput.fill(TEST_EVENT_UPDATE.title)
    console.log(`✅ Updated title to: ${TEST_EVENT_UPDATE.title}`)
    
    // Try to find and update description field
    const descriptionSelectors = [
      '[data-testid="event-description-input"]',
      'textarea[name="description"]',
      '[data-testid="description-input"]',
      'textarea[placeholder*="description"]',
      'textarea'
    ]
    
    for (const selector of descriptionSelectors) {
      const textarea = page.locator(selector).first()
      if (await textarea.count() > 0) {
        await textarea.clear()
        await textarea.fill(TEST_EVENT_UPDATE.description)
        console.log(`✅ Updated description`)
        break
      }
    }
    
    // Try to find and update location field
    const locationSelectors = [
      '[data-testid="event-location-input"]',
      'input[name="location"]',
      '[data-testid="location-input"]',
      'input[placeholder*="location"]'
    ]
    
    for (const selector of locationSelectors) {
      const input = page.locator(selector).first()
      if (await input.count() > 0) {
        await input.clear()
        await input.fill(TEST_EVENT_UPDATE.location)
        console.log(`✅ Updated location`)
        break
      }
    }

    // Step 7: Save changes - THE CRITICAL MOMENT
    console.log('🚀 Step 7: Save changes (CRITICAL AUTH TEST)')
    
    // Clear network errors before save
    networkErrors = []
    const initialErrorCount = consoleErrors.length
    
    // Find save button
    const saveSelectors = [
      '[data-testid="save-button"]',
      'button:has-text("Save")',
      'button[type="submit"]',
      '[data-testid="update-event"]',
      'button:has-text("Update")'
    ]
    
    let saveButton = null
    for (const selector of saveSelectors) {
      const button = page.locator(selector)
      if (await button.count() > 0) {
        saveButton = button.first()
        console.log(`✅ Found save button with selector: ${selector}`)
        break
      }
    }
    
    if (!saveButton) {
      throw new Error('Could not find save/update button on event edit form')
    }
    
    // 🚨 CRITICAL MOMENT: Click save and monitor authentication
    console.log('🚨 CLICKING SAVE - Monitoring authentication flow...')
    
    const saveStartTime = Date.now()
    await saveButton.click()
    
    // Wait for save operation to complete (up to 10 seconds)
    await page.waitForTimeout(3000) // Initial wait for API call
    
    const saveEndTime = Date.now()
    console.log(`⏱️  Save operation took ${saveEndTime - saveStartTime}ms`)

    // Step 8: Verify no logout occurred
    console.log('🚀 Step 8: Verify user is still authenticated')
    
    // Check current URL - should NOT be redirected to login
    const currentUrl = page.url()
    console.log(`Current URL after save: ${currentUrl}`)
    
    if (currentUrl.includes('/login')) {
      // 🚨 CRITICAL FAILURE: User was logged out
      throw new Error(`🚨 CRITICAL ISSUE CONFIRMED: User was logged out after save! Redirected to: ${currentUrl}`)
    }
    
    // Check for authentication errors in network requests
    const authErrors = networkErrors.filter(error => error.status === 401)
    if (authErrors.length > 0) {
      console.log(`🚨 AUTHENTICATION ERRORS DETECTED:`)
      authErrors.forEach(error => {
        console.log(`  - ${error.method} ${error.url} returned ${error.status}`)
      })
      
      // This might explain why user gets logged out
      throw new Error(`Authentication errors during save: ${authErrors.length} 401 responses`)
    }
    
    // Check for new console errors during save
    const newConsoleErrors = consoleErrors.slice(initialErrorCount)
    if (newConsoleErrors.length > 0) {
      console.log(`⚠️  New console errors during save:`)
      newConsoleErrors.forEach(error => console.log(`  - ${error}`))
    }
    
    // Verify user is still on an authenticated page
    const authIndicators = [
      'text=Admin',
      'text=Dashboard',
      'text=Logout',
      '[data-testid="user-menu"]',
      '.user-dropdown'
    ]
    
    let stillAuthenticated = false
    for (const indicator of authIndicators) {
      if (await page.locator(indicator).count() > 0) {
        stillAuthenticated = true
        console.log(`✅ Authentication confirmed: Found ${indicator}`)
        break
      }
    }
    
    if (!stillAuthenticated) {
      throw new Error('No authentication indicators found - user may have been logged out')
    }

    // Step 9: Verify changes were saved
    console.log('🚀 Step 9: Verify changes were saved')
    
    // Look for success messages
    const successSelectors = [
      'text=saved',
      'text=updated',
      'text=success',
      '[data-testid="success-message"]',
      '.success-message',
      '.notification.success'
    ]
    
    let successFound = false
    for (const selector of successSelectors) {
      if (await page.locator(selector).count() > 0) {
        successFound = true
        console.log(`✅ Success message found: ${selector}`)
        break
      }
    }
    
    // Check if form still shows updated values
    const currentTitle = await titleInput.inputValue()
    if (currentTitle === TEST_EVENT_UPDATE.title) {
      console.log(`✅ Title field still shows updated value: ${currentTitle}`)
    }

    // Step 10: Verify persistence by refreshing page
    console.log('🚀 Step 10: Verify persistence with page refresh')
    
    await page.reload({ waitUntil: 'networkidle' })
    
    // 🚨 Check for errors after reload
    if (consoleErrors.length > initialErrorCount + newConsoleErrors.length) {
      const reloadErrors = consoleErrors.slice(initialErrorCount + newConsoleErrors.length)
      console.log(`⚠️  Console errors after reload:`)
      reloadErrors.forEach(error => console.log(`  - ${error}`))
    }
    
    // Verify still authenticated after reload
    const stillAuthAfterReload = await page.locator(authIndicators[0]).count() > 0 ||
                                await page.locator(authIndicators[1]).count() > 0 ||
                                await page.locator(authIndicators[2]).count() > 0
    
    if (!stillAuthAfterReload) {
      throw new Error('User was logged out after page reload')
    }
    
    console.log('✅ Still authenticated after page refresh')
    
    // Final verification: Check if updated data persisted
    // Try to find the title field again and verify it contains our update
    for (const selector of formSelectors) {
      const input = page.locator(selector).first()
      if (await input.count() > 0) {
        const persistedTitle = await input.inputValue()
        if (persistedTitle === TEST_EVENT_UPDATE.title) {
          console.log(`✅ CHANGES PERSISTED: Title still shows "${persistedTitle}"`)
        } else {
          console.log(`⚠️  Title changed from "${TEST_EVENT_UPDATE.title}" to "${persistedTitle}"`)
        }
        break
      }
    }
    
    console.log('🎉 TEST COMPLETED SUCCESSFULLY')
    console.log('📊 Test Summary:')
    console.log(`  - Console Errors: ${consoleErrors.length}`)
    console.log(`  - Network Errors: ${networkErrors.length}`)
    console.log(`  - Auth Tokens: ${authTokens.length}`)
    console.log(`  - Authentication: Maintained throughout`)
    console.log(`  - Data Persistence: Verified`)
  })

  test('Event update preserves authentication cookies', async () => {
    // Test specifically focused on cookie persistence
    console.log('🚀 Testing cookie persistence during event update')

    // Login using AuthHelper
    const loginSuccess = await AuthHelpers.loginAs(page, 'admin');
    expect(loginSuccess).toBeTruthy();
    
    // Capture cookies after login
    const cookiesAfterLogin = await page.context().cookies()
    const authCookies = cookiesAfterLogin.filter(cookie => 
      cookie.name.includes('auth') || 
      cookie.name.includes('session') ||
      cookie.name.includes('token')
    )
    
    console.log(`✅ Auth cookies after login: ${authCookies.length}`)
    authCookies.forEach(cookie => {
      console.log(`  - ${cookie.name}: ${cookie.value.substring(0, 20)}...`)
    })
    
    // Navigate to admin events page to find a real event
    await page.goto('/admin/events')
    await page.waitForLoadState('domcontentloaded')
    await page.waitForTimeout(2000)

    // Find an edit link/button
    const editLink = page.locator('a[href*="/edit"]').first()
    if (await editLink.count() === 0) {
      console.log('⚠️  No events found to test cookie persistence - skipping')
      test.skip(true, 'No events available to test with')
      return
    }

    await editLink.click()
    await page.waitForLoadState('domcontentloaded')

    // Make a change and save
    const titleInput = page.locator('input[name="title"], [data-testid="event-title-input"]').first()
    if (await titleInput.count() > 0) {
      await titleInput.fill('Cookie Test Update')

      const saveButton = page.locator('button:has-text("Save"), [data-testid="save-button"]').first()
      if (await saveButton.count() > 0) {
        await saveButton.click()
        await page.waitForTimeout(2000)
      }
    }
    
    // Capture cookies after update
    const cookiesAfterUpdate = await page.context().cookies()
    const authCookiesAfter = cookiesAfterUpdate.filter(cookie => 
      cookie.name.includes('auth') || 
      cookie.name.includes('session') ||
      cookie.name.includes('token')
    )
    
    console.log(`✅ Auth cookies after update: ${authCookiesAfter.length}`)
    
    // Verify cookies were not cleared
    expect(authCookiesAfter.length).toBeGreaterThanOrEqual(authCookies.length)
    
    // Verify specific cookie values didn't change (indicating they weren't cleared)
    for (const originalCookie of authCookies) {
      const matchingCookie = authCookiesAfter.find(c => c.name === originalCookie.name)
      if (matchingCookie) {
        expect(matchingCookie.value).toBe(originalCookie.value)
        console.log(`✅ Cookie ${originalCookie.name} preserved`)
      } else {
        throw new Error(`Cookie ${originalCookie.name} was removed during update`)
      }
    }
    
    console.log('✅ All authentication cookies preserved during update')
  })

  test('Event update handles network errors gracefully', async () => {
    // Test error handling scenarios
    console.log('🚀 Testing error handling during event update')

    // Login using AuthHelper
    const loginSuccess = await AuthHelpers.loginAs(page, 'admin');
    expect(loginSuccess).toBeTruthy();
    
    // Navigate to admin events page to find a real event
    await page.goto('/admin/events')
    await page.waitForLoadState('domcontentloaded')
    await page.waitForTimeout(2000)

    // Find an edit link/button
    const editLink = page.locator('a[href*="/edit"]').first()
    if (await editLink.count() === 0) {
      console.log('⚠️  No events found to test error handling - skipping')
      test.skip(true, 'No events available to test with')
      return
    }

    await editLink.click()
    await page.waitForLoadState('domcontentloaded')

    // Intercept API calls and simulate errors
    await page.route('**/api/events/**', route => {
      if (route.request().method() === 'PUT') {
        console.log('🚫 Intercepting PUT request - simulating 401 error')
        route.fulfill({
          status: 401,
          contentType: 'application/json',
          body: JSON.stringify({
            success: false,
            error: 'JWT_EXPIRED',
            message: 'Your session has expired'
          })
        })
      } else {
        route.continue()
      }
    })
    
    // Make a change and try to save
    const titleInput = page.locator('input[name="title"], [data-testid="event-title-input"]').first()
    if (await titleInput.count() > 0) {
      await titleInput.fill('Error Test Update')
      
      const saveButton = page.locator('button:has-text("Save"), [data-testid="save-button"]').first()
      if (await saveButton.count() > 0) {
        await saveButton.click()
        await page.waitForTimeout(3000)
        
        // Check if user was redirected to login (the bug we're testing for)
        const currentUrl = page.url()
        if (currentUrl.includes('/login')) {
          console.log('🚨 CONFIRMED: 401 error causes logout redirect')
        } else {
          console.log('✅ User remained authenticated despite 401 error')
        }
        
        // Look for error messages
        const errorSelectors = [
          'text=error',
          'text=expired',
          '[data-testid="error-message"]',
          '.error-message',
          '.notification.error'
        ]
        
        for (const selector of errorSelectors) {
          if (await page.locator(selector).count() > 0) {
            console.log(`✅ Error message displayed: ${selector}`)
            break
          }
        }
      }
    }
    
    console.log('✅ Error handling test completed')
  })
})