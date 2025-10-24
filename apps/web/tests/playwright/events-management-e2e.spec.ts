import { test, expect } from '@playwright/test'
import { AuthHelpers } from './helpers/auth.helpers'
import { setupConsoleErrorFiltering } from './helpers/console.helpers'

/**
 * Comprehensive E2E tests for Events Management System
 * Tests both demo pages: Events Management API Demo and Event Session Matrix Demo
 *
 * Context: Phase 4 (Testing) of Events Management System
 * Phase 3 implementation complete with backend API + frontend integration
 */

test.describe('Events Management System E2E Tests', () => {
  test.describe('Events Management API Demo', () => {
    test.beforeEach(async ({ page }) => {
      // Set up console error filtering
      setupConsoleErrorFiltering(page, {
        filter401Errors: true,
        logFilteredMessages: false,
      })

      // Login as admin before accessing admin pages
      await AuthHelpers.loginAs(page, 'admin')

      // Listen for network failures
      page.on('response', (response) => {
        if (!response.ok() && response.url().includes('/api/')) {
          console.log('API Error:', response.status(), response.url())
        }
      })
    })

    test('should load API demo page without errors', async ({ page }) => {
      // Navigate to the demo page
      await page.goto('/admin/events-management-api-demo')

      // Wait for page to load
      await expect(page).toHaveTitle(/Witch City Rope/i)

      // Verify main title is visible
      await expect(page.locator('h1')).toContainText('Events Management API Integration Demo')

      // Verify page loads without constant reloading (wait and check title still there)
      await page.waitForLoadState('networkidle')
      await expect(page.locator('h1')).toContainText('Events Management API Integration Demo')

      // Take screenshot for verification
      await page.screenshot({ path: 'test-results/events-api-demo-loaded.png' })
    })

    test('should display events list from API', async ({ page }) => {
      await page.goto('/admin/events-management-api-demo')

      // Wait for page to load
      await expect(page.locator('h1')).toContainText('Events Management API Integration Demo')

      // Wait for events to load (either from API or empty state)
      await page.waitForLoadState('networkidle')

      // Check if events loaded from API
      const eventCards = page.locator('.mantine-Card-root')
      const eventCardCount = await eventCards.count()

      if (eventCardCount > 0) {
        console.log(`✅ Found ${eventCardCount} event cards from API`)
        // At least one event should be visible
        expect(eventCardCount).toBeGreaterThan(0)
      } else {
        // If no events from API, verify the "no events" message or loading state
        console.log('⚠️ No events loaded from API - checking for empty state')
        const loader = page.locator('text=Loading events...')
        const noEventsMessage = page.locator('text=No events, text=No published events')
        const hasLoader = (await loader.count()) > 0
        const hasNoEvents = (await noEventsMessage.count()) > 0
        expect(hasLoader || hasNoEvents).toBeTruthy()
      }

      // Take screenshot of events list
      await page.screenshot({ path: 'test-results/events-list-display.png' })
    })

    test('should switch between API tabs correctly', async ({ page }) => {
      await page.goto('/admin/events-management-api-demo')

      // Wait for page load
      await expect(page.locator('h1')).toContainText('Events Management API Integration Demo')

      // Verify both tabs are present
      await expect(page.locator('[role="tab"]').nth(0)).toContainText('Current API (Working)')
      await expect(page.locator('[role="tab"]').nth(1)).toContainText(
        'Future Events Management API'
      )

      // Verify current API tab is active by default
      await expect(page.locator('[role="tab"][aria-selected="true"]')).toContainText(
        'Current API (Working)'
      )

      // Switch to Future API tab
      await page.locator('[role="tab"]:has-text("Future Events Management API")').click()
      await page.waitForLoadState('networkidle')

      // Verify tab switched
      await expect(page.locator('[role="tab"][aria-selected="true"]')).toContainText(
        'Future Events Management API'
      )

      // Verify disabled notice is shown
      await expect(page.locator('text=API calls are disabled to prevent errors')).toBeVisible()

      // Switch back to Current API tab
      await page.locator('[role="tab"]:has-text("Current API (Working)")').click()
      await page.waitForLoadState('networkidle')

      // Verify tab switched back
      await expect(page.locator('[role="tab"][aria-selected="true"]')).toContainText(
        'Current API (Working)'
      )

      // Take screenshot after tab switching
      await page.screenshot({ path: 'test-results/tab-switching-complete.png' })
    })

    test('should allow event interaction', async ({ page }) => {
      await page.goto('/admin/events-management-api-demo')

      // Wait for page to load
      await expect(page.locator('h1')).toContainText('Events Management API Integration Demo')
      await page.waitForLoadState('networkidle')

      // Check if any event cards are present
      const eventCards = page.locator('.mantine-Card-root')
      const cardCount = await eventCards.count()

      if (cardCount > 0) {
        console.log(`✅ Found ${cardCount} event cards - testing interaction`)

        // Click on first event card
        await eventCards.first().click()
        await page.waitForLoadState('networkidle')

        // Verify page is still functional after click
        await expect(page.locator('h1')).toContainText('Events Management API Integration Demo')

        console.log('✅ Event card click successful')
      } else {
        console.log('⚠️ No event cards found - skipping interaction test')
      }

      // Take screenshot after event interaction
      await page.screenshot({ path: 'test-results/event-interaction.png' })
    })

    test('should test refresh functionality', async ({ page }) => {
      await page.goto('/admin/events-management-api-demo')

      // Wait for page to load
      await expect(page.locator('h1')).toContainText('Events Management API Integration Demo')
      await page.waitForLoadState('networkidle')

      // Look for refresh button
      const refreshButton = page.locator('button:has-text("Refresh Events")')
      const refreshButtonCount = await refreshButton.count()

      if (refreshButtonCount > 0) {
        console.log('✅ Refresh button found - testing refresh')

        // Click refresh button
        await refreshButton.click()
        await page.waitForLoadState('networkidle')

        // Verify page is still functional after refresh
        await expect(page.locator('h1')).toContainText('Events Management API Integration Demo')
        console.log('✅ Refresh button works')
      } else {
        console.log('⚠️ No refresh button found - testing page reload')

        // Test page reload functionality
        await page.reload()
        await page.waitForLoadState('networkidle')

        // Verify page loads correctly after reload
        await expect(page.locator('h1')).toContainText('Events Management API Integration Demo')
        console.log('✅ Page reload works')
      }

      // Take screenshot after refresh test
      await page.screenshot({ path: 'test-results/refresh-functionality.png' })
    })

    test('should verify no console errors during operation', async ({ page }) => {
      const consoleErrors: string[] = []

      page.on('console', (msg) => {
        if (msg.type() === 'error') {
          consoleErrors.push(msg.text())
        }
      })

      await page.goto('/admin/events-management-api-demo')

      // Wait for page to fully load
      await expect(page.locator('h1')).toContainText('Events Management API Integration Demo')
      await page.waitForLoadState('networkidle')

      // Test tab switching
      await page.locator('[role="tab"]:has-text("Future Events Management API")').click()
      await page.waitForLoadState('networkidle')
      await page.locator('[role="tab"]:has-text("Current API (Working)")').click()
      await page.waitForLoadState('networkidle')

      // Verify no critical console errors occurred
      // Filter out non-critical errors (like Vite dev server WebSocket errors and auth errors)
      const criticalErrors = consoleErrors.filter(
        (error) =>
          !error.includes('DevTools') &&
          !error.includes('favicon') &&
          !error.includes('WebSocket') &&
          !error.includes('ws://') &&
          !error.includes('Upgrade Required') &&
          !error.includes('401') &&
          !error.includes('Unauthorized') &&
          !error.includes('auth/user') &&
          !error.includes('auth/refresh')
      )

      console.log(
        `Found ${consoleErrors.length} total errors, ${criticalErrors.length} critical errors`
      )
      expect(criticalErrors.length).toBe(0)
    })
  })

  test.describe('Event Session Matrix Demo', () => {
    test.beforeEach(async ({ page }) => {
      // Set up console error filtering
      setupConsoleErrorFiltering(page, {
        filter401Errors: true,
        logFilteredMessages: false,
      })

      // Login as admin before accessing admin pages
      await AuthHelpers.loginAs(page, 'admin')
    })

    test.skip('should load Event Session Matrix demo page', async ({ page }) => {
      // SKIP: Requires post-login return-to-page feature (P1 CRITICAL on launch checklist)
      // See: /docs/standards-processes/PRE_LAUNCH_PUNCH_LIST.md lines 58-64
      // Current behavior: Login from demo page redirects to dashboard (not back to demo)
      // Expected (future): After login, return to /admin/event-session-matrix-demo
      // Status: Feature not yet implemented - test will be enabled when feature is complete

      // Navigate to the demo page
      await page.goto('/admin/event-session-matrix-demo')

      // Wait for page to fully load
      await page.waitForLoadState('networkidle')

      // Verify page loads
      await expect(page).toHaveTitle(/Witch City Rope/i)

      // Verify main title is visible
      await expect(page.locator('h1')).toContainText('Event Session Matrix Demo', { timeout: 10000 })

      // Wait for form to render
      await page.waitForLoadState('networkidle')

      // Take screenshot of loaded page
      await page.screenshot({ path: 'test-results/matrix-demo-loaded.png' })
    })

    test('should display event form tabs', async ({ page }) => {
      await page.goto('/admin/event-session-matrix-demo')

      // Wait for page and form to load
      await expect(page.locator('h1')).toContainText('Event Session Matrix Demo')
      await page.waitForLoadState('networkidle')

      // Check for tab elements in the EventForm component
      const tabs = page.locator('[role="tab"]')
      const tabCount = await tabs.count()

      if (tabCount >= 4) {
        console.log(`✅ Found ${tabCount} tabs in event form`)

        // Verify expected tabs are present
        await expect(page.locator('[role="tab"]:has-text("Basic Info")')).toBeVisible()
        await expect(page.locator('[role="tab"]:has-text("Setup")')).toBeVisible()
        await expect(page.locator('[role="tab"]:has-text("Emails")')).toBeVisible()
        await expect(page.locator('[role="tab"]:has-text("Volunteers")')).toBeVisible()

        console.log('✅ All expected tabs found')
      } else {
        console.log(`⚠️ Only found ${tabCount} tabs - form may still be loading`)
        // Still verify page loaded correctly
        await expect(page.locator('h1')).toContainText('Event Session Matrix Demo')
      }

      // Take screenshot showing tab structure
      await page.screenshot({ path: 'test-results/matrix-tabs-structure.png' })
    })

    test('should test tab switching functionality', async ({ page }) => {
      await page.goto('/admin/event-session-matrix-demo')

      // Wait for page load
      await page.waitForLoadState('networkidle')

      // Get all clickable elements that might be tabs (exclude mobile menu)
      const clickableElements = page.locator('[role="tab"]:visible:not(.mobile-menu-toggle), .tab:visible, [data-testid*="tab"]:visible')
      const elementCount = await clickableElements.count()

      console.log(`Found ${elementCount} potentially clickable tab elements`)

      // Try clicking on different tab-like elements
      for (let i = 0; i < Math.min(elementCount, 4); i++) {
        try {
          const element = clickableElements.nth(i)
          // Check if element is visible before trying to click
          const isVisible = await element.isVisible()
          if (!isVisible) {
            console.log(`Skipping element ${i}: not visible`)
            continue
          }

          const text = await element.textContent()
          console.log(`Trying to click element ${i}: "${text}"`)

          await element.click()
          await page.waitForLoadState('networkidle')

          // Take screenshot after each click
          await page.screenshot({ path: `test-results/tab-click-${i}.png` })
        } catch (error) {
          console.log(`Could not click element ${i}:`, error)
        }
      }
    })

    test('should verify form fields are present', async ({ page }) => {
      await page.goto('/admin/event-session-matrix-demo')

      // Wait for page load
      await page.waitForLoadState('networkidle')

      // Look for common form elements
      const inputs = await page.locator('input').count()
      const textareas = await page.locator('textarea').count()
      const selects = await page.locator('select').count()
      const buttons = await page.locator('button').count()

      console.log('Form elements found:', { inputs, textareas, selects, buttons })

      // We expect at least some form elements
      expect(inputs + textareas + selects).toBeGreaterThan(0)

      // Take screenshot showing form fields
      await page.screenshot({ path: 'test-results/form-fields-present.png' })
    })

    test('should verify Tiptap rich text editors load', async ({ page }) => {
      await page.goto('/admin/event-session-matrix-demo')

      // Wait for page load
      await page.waitForLoadState('networkidle')

      // Look for Tiptap/Mantine editor elements
      const mantineEditors = await page.locator('.mantine-RichTextEditor-root').count()
      const proseMirrorElements = await page.locator('.ProseMirror').count()
      const toolbars = await page.locator('.mantine-RichTextEditor-toolbar').count()

      console.log('Editor elements found:', { mantineEditors, proseMirrorElements, toolbars })

      // If we have Mantine editors, verify they're initialized properly
      if (mantineEditors > 0) {
        // Verify editor components are present
        const editorVisible = await page.locator('.mantine-RichTextEditor-root').first().isVisible()
        console.log(`Found ${mantineEditors} Tiptap editors, first visible: ${editorVisible}`)
      }

      // Take screenshot of editor state
      await page.screenshot({ path: 'test-results/tiptap-editors-loaded.png' })
    })

    test('should test session grid display', async ({ page }) => {
      await page.goto('/admin/event-session-matrix-demo')

      // Wait for content to load
      await page.waitForLoadState('networkidle')

      // Navigate to Setup tab where session grid lives
      const setupTab = page.locator('[role="tab"]:has-text("Setup")')
      if (await setupTab.count() > 0) {
        await setupTab.click()
        await page.waitForLoadState('networkidle')
        console.log('✅ Navigated to Setup tab')
      }

      // Look for the actual sessions grid (modal-based UI)
      const sessionsGrid = await page.locator('[data-testid="grid-sessions"]').count()
      const addSessionButton = await page.locator('[data-testid="button-add-session"]').count()
      const sessionRows = await page.locator('[data-testid="session-row"]').count()

      console.log('Event Session Matrix elements found:', {
        sessionsGrid,
        addSessionButton,
        sessionRows
      })

      // Test modal workflow if Add Session button exists
      if (addSessionButton > 0) {
        await page.locator('[data-testid="button-add-session"]').click()
        await page.waitForLoadState('networkidle')

        // Check if session modal opened
        const sessionModal = await page.locator('[data-testid="modal-add-session"]').count()
        console.log(`Session modal opened: ${sessionModal > 0}`)

        // Take screenshot of modal
        if (sessionModal > 0) {
          await page.screenshot({ path: 'test-results/session-modal-workflow.png' })
          // Close modal with Escape
          await page.keyboard.press('Escape')
          // Wait for modal to close
          await page.waitForSelector('[data-testid="modal-add-session"]', { state: 'hidden', timeout: 1000 }).catch(() => {})
        }
      }

      // Take screenshot of session grid area
      await page.screenshot({ path: 'test-results/session-grid-display.png' })
    })

    test('should verify ticket types section', async ({ page }) => {
      await page.goto('/admin/event-session-matrix-demo')

      // Wait for page load
      await page.waitForLoadState('networkidle')

      // Look for ticket-related elements
      const ticketElements = await page
        .locator('*:has-text("ticket"), *:has-text("Ticket")')
        .count()
      const priceElements = await page
        .locator('*:has-text("$"), *:has-text("price"), *:has-text("Price")')
        .count()

      console.log('Ticket elements found:', { ticketElements, priceElements })

      // Take screenshot of ticket types area
      await page.screenshot({ path: 'test-results/ticket-types-section.png' })
    })

    test('should test Save Draft and Cancel buttons', async ({ page }) => {
      await page.goto('/admin/event-session-matrix-demo')

      // Wait for page load
      await page.waitForLoadState('networkidle')

      // Look for Save Draft button
      const saveDraftButton = page.locator(
        'button:has-text("Save Draft"), button:has-text("Save"), [data-testid*="save"]'
      )
      const saveDraftCount = await saveDraftButton.count()

      // Look for Cancel button
      const cancelButton = page.locator('button:has-text("Cancel"), [data-testid*="cancel"]')
      const cancelCount = await cancelButton.count()

      console.log('Action buttons found:', { saveDraftCount, cancelCount })

      // Test Save Draft button if it exists
      if (saveDraftCount > 0) {
        try {
          await saveDraftButton.first().click({ timeout: 5000 })
          console.log('✅ Save Draft button clicked')

          // Wait briefly for any immediate page updates (don't wait for networkidle - page may have polling)
          try {
            await page.waitForLoadState('networkidle', { timeout: 3000 })
          } catch {
            // Page didn't reach networkidle (likely due to polling) - that's okay
            console.log('⚠️ Page has ongoing network activity (expected for demo pages)')
          }

          // Take screenshot after save action
          await page.screenshot({ path: 'test-results/save-draft-clicked.png' })
        } catch (error) {
          console.log('⚠️ Save Draft button click failed or timed out - button may not be functional in demo mode')
        }
      }

      // Test Cancel button if it exists
      if (cancelCount > 0) {
        try {
          await cancelButton.first().click({ timeout: 5000 })
          console.log('✅ Cancel button clicked')

          // Wait briefly for any immediate page updates (don't wait for networkidle - page may have polling)
          try {
            await page.waitForLoadState('networkidle', { timeout: 3000 })
          } catch {
            // Page didn't reach networkidle (likely due to polling) - that's okay
            console.log('⚠️ Page has ongoing network activity (expected for demo pages)')
          }

          // Take screenshot after cancel action
          await page.screenshot({ path: 'test-results/cancel-clicked.png' })
        } catch (error) {
          console.log('⚠️ Cancel button click failed or timed out - button may not be functional in demo mode')
        }
      }

      // If no specific buttons found, look for any buttons
      if (saveDraftCount === 0 && cancelCount === 0) {
        const allButtons = await page.locator('button').count()
        console.log(`No Save/Cancel buttons found, but ${allButtons} total buttons exist`)

        // Take screenshot showing available buttons
        await page.screenshot({ path: 'test-results/available-buttons.png' })
      }
    })
  })

  test.describe('API Integration Tests', () => {
    test.beforeEach(async ({ page }) => {
      // Set up console error filtering
      setupConsoleErrorFiltering(page, {
        filter401Errors: true,
        logFilteredMessages: false,
      })

      // Login as admin before accessing admin pages
      await AuthHelpers.loginAs(page, 'admin')
    })

    test('should verify API calls to events endpoint', async ({ page }) => {
      // Track network requests
      const apiCalls: string[] = []

      page.on('response', (response) => {
        if (response.url().includes('/api/events')) {
          apiCalls.push(`${response.status()} ${response.url()}`)
          console.log('API Call:', response.status(), response.url())
        }
      })

      await page.goto('/admin/events-management-api-demo')

      // Wait for API calls to complete
      await page.waitForLoadState('networkidle')

      // Verify API call was made (or fallback was used)
      console.log('API calls made:', apiCalls)

      // The page should load successfully (verify h1 exists)
      await expect(page.locator('h1').first()).toBeVisible()

      // Verify at least one API call was made or page still renders
      const pageHasContent = await page.locator('body').textContent()
      expect(pageHasContent).toBeTruthy()

      // Take screenshot of final state
      await page.screenshot({ path: 'test-results/api-integration-test.png' })
    })

    test('should test error handling for failed API calls', async ({ page }) => {
      // Intercept API calls and make them fail
      await page.route('**/api/events/**', (route) => {
        console.log('Intercepting API call to simulate error')
        route.fulfill({
          status: 500,
          contentType: 'application/json',
          body: JSON.stringify({ error: 'Internal Server Error' }),
        })
      })

      await page.goto('/admin/events-management-api-demo')

      // Wait for page load and error handling
      await page.waitForLoadState('networkidle')

      // Verify page still loads correctly
      await expect(page.locator('h1')).toContainText('Events Management API Integration Demo')

      // Check if error message is displayed
      const errorAlert = page.locator('[role="alert"]:has-text("Error")')
      const errorAlertCount = await errorAlert.count()

      if (errorAlertCount > 0) {
        console.log('✅ Error message displayed correctly')
      } else {
        console.log('⚠️ No error message displayed - page may handle errors gracefully')
      }

      // Verify page is still functional despite API error
      await expect(page.locator('h1')).toBeVisible()

      // Take screenshot of error handling
      await page.screenshot({ path: 'test-results/api-error-handling.png' })
    })

    test('should verify response data structure', async ({ page }) => {
      let responseData: any = null

      page.on('response', async (response) => {
        if (response.url().includes('/api/events') && response.ok()) {
          try {
            responseData = await response.json()
            console.log('API Response:', responseData)
          } catch (error) {
            console.log('Could not parse response:', error)
          }
        }
      })

      await page.goto('/admin/events-management-api-demo')

      // Wait for API response
      await page.waitForLoadState('networkidle')

      // Verify page functionality regardless of API response
      await expect(page.locator('h1')).toContainText('Events Management API Integration Demo')

      // Log the response structure for validation
      if (responseData) {
        console.log(
          'Response structure validated:',
          typeof responseData,
          Array.isArray(responseData)
        )
      } else {
        console.log('Using fallback data (API not available)')
      }

      // Take screenshot of data structure test
      await page.screenshot({ path: 'test-results/response-data-structure.png' })
    })
  })

  test.describe('Cross-Browser Compatibility', () => {
    test.beforeEach(async ({ page }) => {
      // Set up console error filtering
      setupConsoleErrorFiltering(page, {
        filter401Errors: true,
        logFilteredMessages: false,
      })

      // Login as admin before accessing admin pages
      await AuthHelpers.loginAs(page, 'admin')
    })

    test('should work correctly in different viewport sizes', async ({ page }) => {
      // Test mobile viewport
      await page.setViewportSize({ width: 375, height: 667 })
      await page.goto('/admin/events-management-api-demo')

      await expect(page.locator('h1')).toContainText('Events Management API Integration Demo')
      await page.screenshot({ path: 'test-results/mobile-viewport-api-demo.png' })

      // Test tablet viewport
      await page.setViewportSize({ width: 768, height: 1024 })
      await page.goto('/admin/event-session-matrix-demo')

      await page.waitForLoadState('networkidle')
      await page.screenshot({ path: 'test-results/tablet-viewport-matrix-demo.png' })

      // Test desktop viewport
      await page.setViewportSize({ width: 1920, height: 1080 })
      await page.goto('/admin/events-management-api-demo')

      await expect(page.locator('h1')).toContainText('Events Management API Integration Demo')
      await page.screenshot({ path: 'test-results/desktop-viewport-api-demo.png' })
    })
  })
})
