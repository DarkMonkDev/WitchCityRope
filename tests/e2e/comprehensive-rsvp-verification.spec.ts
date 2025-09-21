import { test, expect, Page } from '@playwright/test'
import { AuthHelper, quickLogin } from './helpers/auth.helper'

/**
 * COMPREHENSIVE RSVP VERIFICATION TEST
 *
 * This test captures screenshots of EACH scenario to provide PROOF of what's actually
 * displayed vs what we think is working. This is critical for debugging the user's
 * reported issues with RSVP counts showing incorrectly.
 *
 * User Reports:
 * 1. Public Events Page - Wrong capacity numbers displayed
 * 2. Admin Events List - NO tickets/RSVPs in capacity column (should show 2/40 for Rope Social)
 * 3. Admin Event Details RSVP Tab - Shows NO RSVPs (should show 2)
 * 4. Public Event Details - RSVP status issues
 * 5. User Dashboard - RSVP count shows 0 (should show 1)
 */

test.describe('RSVP Verification - Visual Evidence Collection', () => {
  let consoleErrors: string[] = []
  let jsErrors: string[] = []
  let apiResponses: { url: string, status: number, data?: any }[] = []

  test.beforeEach(async ({ page }) => {
    // Reset error arrays
    consoleErrors = []
    jsErrors = []
    apiResponses = []

    // Monitor JavaScript errors (page crashes) - CRITICAL per lessons learned
    page.on('pageerror', error => {
      jsErrors.push(error.toString())
      console.log(`🚨 JavaScript Error: ${error.toString()}`)
    })

    // Monitor console errors (component failures) - CRITICAL per lessons learned
    page.on('console', msg => {
      if (msg.type() === 'error') {
        const errorText = msg.text()
        consoleErrors.push(errorText)

        // Specifically catch date/time errors that crash components
        if (errorText.includes('RangeError') || errorText.includes('Invalid time value')) {
          console.log(`🚨 CRITICAL: Date/Time error detected: ${errorText}`)
        }
      }
    })

    // Monitor API responses to capture actual data being returned
    page.on('response', async response => {
      const url = response.url()
      if (url.includes('/api/')) {
        try {
          const data = await response.json()
          apiResponses.push({
            url,
            status: response.status(),
            data
          })
          console.log(`📡 API Response: ${response.status()} ${url}`)
          if (url.includes('events') || url.includes('participation')) {
            console.log(`📊 API Data:`, JSON.stringify(data, null, 2))
          }
        } catch (e) {
          apiResponses.push({
            url,
            status: response.status()
          })
        }
      }
    })
  })

  test.afterEach(async ({ page }) => {
    // Check for errors BEFORE validating content - CRITICAL per lessons learned
    if (jsErrors.length > 0) {
      console.log(`🚨 CRITICAL: Page has JavaScript errors that crash functionality: ${jsErrors.join('; ')}`)
    }

    if (consoleErrors.length > 0) {
      // Check for critical date/time errors
      const criticalErrors = consoleErrors.filter(error =>
        error.includes('RangeError') || error.includes('Invalid time value')
      )
      if (criticalErrors.length > 0) {
        console.log(`🚨 CRITICAL: Page has date/time errors that crash components: ${criticalErrors.join('; ')}`)
      }
    }

    // Log API responses for debugging
    console.log(`📊 Total API calls captured: ${apiResponses.length}`)
  })

  test.beforeAll(async ({ request }) => {
    // API health pre-check prevents wasted test time - CRITICAL per lessons learned
    const response = await request.get('http://localhost:5655/health')
    expect(response.ok()).toBeTruthy()
    const health = await response.json()
    expect(health.status).toBe('Healthy')
  })

  test('1. Public Events Page - Capture Event Card Capacity Display', async ({ page }) => {
    console.log('📸 Testing Public Events Page capacity display...')

    await page.goto('http://localhost:5173/events')
    await page.waitForLoadState('networkidle')
    await page.waitForTimeout(2000) // Allow React components to render

    // Check for errors FIRST before content validation - CRITICAL per lessons learned
    if (jsErrors.length > 0 || consoleErrors.length > 0) {
      throw new Error(`Navigation failed with errors: JS errors: ${jsErrors.length}, Console errors: ${consoleErrors.length}`)
    }

    // Check for user-visible connection problems - CRITICAL per lessons learned
    const connectionErrors = await page.locator('text=/Connection Problem|Failed to load|Error loading/i').count()
    if (connectionErrors > 0) {
      const errorText = await page.locator('text=/Connection Problem/i').first().textContent()
      throw new Error(`Page shows connection error: ${errorText}`)
    }

    // Take screenshot of public events page showing capacity numbers
    await page.screenshot({
      path: 'test-results/public-events-capacity-display.png',
      fullPage: true
    })

    // Find event cards and capture their capacity display
    const eventCards = page.locator('[data-testid*="event-card"], .event-card, .card')
    const eventCount = await eventCards.count()
    console.log(`📊 Found ${eventCount} event cards`)

    if (eventCount > 0) {
      // Screenshot first few event cards individually
      for (let i = 0; i < Math.min(eventCount, 3); i++) {
        const card = eventCards.nth(i)
        const cardTitle = await card.locator('h2, h3, .title, [data-testid*="title"]').first().textContent() || `Event ${i + 1}`

        await card.screenshot({
          path: `test-results/event-card-${i + 1}-${cardTitle.replace(/[^a-zA-Z0-9]/g, '-')}.png`
        })

        // Log capacity information
        const capacityText = await card.locator('text=/capacity|spots|available|full/i').allTextContents()
        console.log(`🎯 Event "${cardTitle}" capacity info: ${capacityText.join(', ')}`)
      }
    }

    // Look specifically for "Rope Social" event
    const ropeSocialCard = page.locator('text=/Rope Social/i').first()
    if (await ropeSocialCard.count() > 0) {
      const ropeSocialParent = ropeSocialCard.locator('..').locator('..')
      await ropeSocialParent.screenshot({
        path: 'test-results/rope-social-public-display.png'
      })

      const capacityInfo = await ropeSocialParent.locator('text=/\d+.*\d+|spots|capacity/i').allTextContents()
      console.log(`🎯 Rope Social capacity info on public page: ${capacityInfo.join(', ')}`)
    }
  })

  test('2. Admin Events List - Capture Tickets/Capacity Column', async ({ page }) => {
    console.log('📸 Testing Admin Events List capacity column...')

    // Login as admin first
    await quickLogin(page, 'admin')

    // Navigate to admin events list
    await page.goto('http://localhost:5173/admin/events')
    await page.waitForLoadState('networkidle')
    await page.waitForTimeout(2000) // Allow React components to render

    // Check for errors FIRST - CRITICAL per lessons learned
    if (jsErrors.length > 0 || consoleErrors.length > 0) {
      throw new Error(`Admin events navigation failed with errors`)
    }

    // Check for 404/Not Found BEFORE content validation - CRITICAL per lessons learned
    const errorCount = await page.locator('text=/404|Not Found|Access Denied/i').count()
    if (errorCount > 0) {
      throw new Error(`Page shows error instead of expected content`)
    }

    // Take screenshot of admin events list
    await page.screenshot({
      path: 'test-results/admin-events-list-capacity-column.png',
      fullPage: true
    })

    // Look for events table/list
    const eventsTable = page.locator('table, .table, [data-testid*="events-table"], [data-testid*="events-list"]')
    if (await eventsTable.count() > 0) {
      await eventsTable.screenshot({
        path: 'test-results/admin-events-table-detail.png'
      })

      // Look for capacity/tickets column headers
      const capacityHeaders = await page.locator('th, .header').locator('text=/capacity|tickets|rsvp/i').allTextContents()
      console.log(`📊 Admin events table headers with capacity: ${capacityHeaders.join(', ')}`)

      // Look for Rope Social row specifically
      const ropeSocialRow = page.locator('tr').filter({ hasText: /Rope Social/i })
      if (await ropeSocialRow.count() > 0) {
        await ropeSocialRow.screenshot({
          path: 'test-results/rope-social-admin-row.png'
        })

        const rowData = await ropeSocialRow.allTextContents()
        console.log(`🎯 Rope Social admin row data: ${rowData.join(' | ')}`)
      }
    }

    // Look for card layout if no table
    const eventCards = page.locator('[data-testid*="admin-event"], .admin-event-card')
    if (await eventCards.count() > 0) {
      await eventCards.first().screenshot({
        path: 'test-results/admin-event-cards-layout.png'
      })
    }
  })

  test('3. Admin Event Details - RSVP Tab Content', async ({ page }) => {
    console.log('📸 Testing Admin Event Details RSVP Tab...')

    // Login as admin first
    await quickLogin(page, 'admin')

    // First get the event ID for Rope Social by calling the API directly
    const eventsResponse = await page.request.get('http://localhost:5655/api/events')
    const events = await eventsResponse.json()
    console.log(`📊 API returned ${events.length} events`)

    const ropeSocial = events.find((e: any) => e.title?.includes('Rope Social'))
    if (!ropeSocial) {
      throw new Error('Could not find Rope Social event in API response')
    }

    console.log(`🎯 Found Rope Social event: ${ropeSocial.id} - ${ropeSocial.title}`)

    // Navigate to admin event details page
    await page.goto(`http://localhost:5173/admin/events/${ropeSocial.id}`)
    await page.waitForLoadState('networkidle')
    await page.waitForTimeout(3000) // Allow admin page to fully load

    // Check for errors FIRST - CRITICAL per lessons learned
    if (jsErrors.length > 0 || consoleErrors.length > 0) {
      throw new Error(`Admin event details navigation failed with errors`)
    }

    // Take screenshot of initial admin event details page
    await page.screenshot({
      path: 'test-results/admin-event-details-initial.png',
      fullPage: true
    })

    // Look for and click on RSVPs/Tickets tab
    const rsvpTab = page.locator('text=/rsvp|tickets|participants|attendees/i').filter({ hasText: /tab|button/ }).first()
    if (await rsvpTab.count() === 0) {
      // Try alternative selectors for tabs
      const allTabs = page.locator('[role="tab"], .tab, [data-testid*="tab"]')
      const tabCount = await allTabs.count()
      console.log(`📊 Found ${tabCount} tabs on admin event details page`)

      for (let i = 0; i < tabCount; i++) {
        const tabText = await allTabs.nth(i).textContent()
        console.log(`🔍 Tab ${i + 1}: "${tabText}"`)

        if (tabText?.toLowerCase().includes('rsvp') || tabText?.toLowerCase().includes('ticket')) {
          await allTabs.nth(i).click()
          break
        }
      }
    } else {
      await rsvpTab.click()
    }

    await page.waitForTimeout(2000) // Allow tab content to load

    // Take screenshot of RSVP tab content
    await page.screenshot({
      path: 'test-results/admin-rsvp-tab-content.png',
      fullPage: true
    })

    // Get participation data directly from API
    const participationResponse = await page.request.get(`http://localhost:5655/api/admin/events/${ropeSocial.id}/participations`)
    const participationData = await participationResponse.json()
    console.log(`📊 API participation data for Rope Social: ${JSON.stringify(participationData, null, 2)}`)

    // Look for RSVP list/table content
    const rsvpContent = page.locator('[data-testid*="rsvp"], [data-testid*="participation"], .rsvp-list, .participants-list')
    if (await rsvpContent.count() > 0) {
      await rsvpContent.screenshot({
        path: 'test-results/rsvp-content-detail.png'
      })

      const rsvpText = await rsvpContent.allTextContents()
      console.log(`🎯 RSVP content text: ${rsvpText.join(' | ')}`)
    }

    // Check for empty states
    const emptyStates = await page.locator('text=/no rsvp|no participants|no attendees|empty/i').allTextContents()
    if (emptyStates.length > 0) {
      console.log(`⚠️ Empty state messages: ${emptyStates.join(', ')}`)
    }
  })

  test('4. Public Event Details - RSVP Status and Button', async ({ page }) => {
    console.log('📸 Testing Public Event Details RSVP status...')

    // Login as admin first (to test authenticated RSVP display)
    await quickLogin(page, 'admin')

    // Get Rope Social event ID
    const eventsResponse = await page.request.get('http://localhost:5655/api/events')
    const events = await eventsResponse.json()
    const ropeSocial = events.find((e: any) => e.title?.includes('Rope Social'))

    if (!ropeSocial) {
      throw new Error('Could not find Rope Social event')
    }

    // Navigate to public event details
    await page.goto(`http://localhost:5173/events/${ropeSocial.id}`)
    await page.waitForLoadState('networkidle')
    await page.waitForTimeout(2000)

    // Check for errors FIRST - CRITICAL per lessons learned
    if (jsErrors.length > 0 || consoleErrors.length > 0) {
      throw new Error(`Public event details navigation failed with errors`)
    }

    // Take screenshot of public event details page
    await page.screenshot({
      path: 'test-results/public-event-rsvp-status.png',
      fullPage: true
    })

    // Look for RSVP status and buttons on the right side
    const rsvpSection = page.locator('.rsvp, [data-testid*="rsvp"], .sidebar, .event-actions')
    if (await rsvpSection.count() > 0) {
      await rsvpSection.screenshot({
        path: 'test-results/public-event-rsvp-section.png'
      })
    }

    // Look for RSVP button/status
    const rsvpButtons = page.locator('button').filter({ hasText: /rsvp|cancel|confirmed|register/i })
    const buttonCount = await rsvpButtons.count()
    console.log(`📊 Found ${buttonCount} RSVP-related buttons`)

    for (let i = 0; i < buttonCount; i++) {
      const buttonText = await rsvpButtons.nth(i).textContent()
      console.log(`🔘 RSVP Button ${i + 1}: "${buttonText}"`)
    }

    // Check user's RSVP status via API
    const rsvpStatusResponse = await page.request.get(`http://localhost:5655/api/participation/${ropeSocial.id}`)
    if (rsvpStatusResponse.ok()) {
      const rsvpStatus = await rsvpStatusResponse.json()
      console.log(`📊 User's RSVP status via API: ${JSON.stringify(rsvpStatus, null, 2)}`)
    }

    // Test Cancel RSVP button functionality if it exists
    const cancelButton = page.locator('button').filter({ hasText: /cancel.*rsvp|cancel.*registration/i })
    if (await cancelButton.count() > 0) {
      console.log(`🔘 Found Cancel RSVP button - testing functionality...`)

      // Take screenshot before clicking
      await page.screenshot({
        path: 'test-results/before-cancel-rsvp.png',
        fullPage: true
      })

      await cancelButton.click()
      await page.waitForTimeout(2000) // Allow for API call and UI update

      // Take screenshot after clicking
      await page.screenshot({
        path: 'test-results/after-cancel-rsvp.png',
        fullPage: true
      })
    }
  })

  test('5. User Dashboard - RSVP Count Card', async ({ page }) => {
    console.log('📸 Testing User Dashboard RSVP count...')

    // Login as admin
    await quickLogin(page, 'admin')

    // Navigate to dashboard
    await page.goto('http://localhost:5173/dashboard')
    await page.waitForLoadState('networkidle')
    await page.waitForTimeout(3000) // Allow dashboard to fully load

    // Check for errors FIRST - CRITICAL per lessons learned
    if (jsErrors.length > 0 || consoleErrors.length > 0) {
      throw new Error(`Dashboard navigation failed with errors`)
    }

    // Check for 404/Not Found BEFORE content validation - CRITICAL per lessons learned
    const errorCount = await page.locator('text=/404|Not Found|Access Denied/i').count()
    if (errorCount > 0) {
      throw new Error(`Dashboard shows error instead of expected content`)
    }

    // Take screenshot of full dashboard
    await page.screenshot({
      path: 'test-results/dashboard-rsvp-count.png',
      fullPage: true
    })

    // Look for RSVP count card/widget
    const rsvpCountElements = page.locator('text=/rsvp|event|upcoming/i').locator('..')
    const rsvpCount = await rsvpCountElements.count()
    console.log(`📊 Found ${rsvpCount} elements containing RSVP/event text`)

    // Screenshot dashboard cards/widgets
    const dashboardCards = page.locator('.card, .widget, [data-testid*="card"], [data-testid*="widget"]')
    const cardCount = await dashboardCards.count()
    console.log(`📊 Found ${cardCount} dashboard cards/widgets`)

    if (cardCount > 0) {
      for (let i = 0; i < Math.min(cardCount, 5); i++) {
        const card = dashboardCards.nth(i)
        const cardText = await card.textContent()

        if (cardText?.toLowerCase().includes('rsvp') || cardText?.toLowerCase().includes('event')) {
          await card.screenshot({
            path: `test-results/dashboard-rsvp-card-${i + 1}.png`
          })
          console.log(`🎯 Dashboard RSVP card ${i + 1}: "${cardText?.substring(0, 100)}..."`)
        }
      }
    }

    // Look for specific numbers/counts
    const numberElements = page.locator('text=/\\d+/')
    const numberCount = await numberElements.count()
    console.log(`📊 Found ${numberCount} elements with numbers`)

    // Get user's actual RSVPs via API for comparison
    const userRsvpsResponse = await page.request.get('http://localhost:5655/api/user/rsvps')
    if (userRsvpsResponse.ok()) {
      const userRsvps = await userRsvpsResponse.json()
      console.log(`📊 User's actual RSVPs via API: ${JSON.stringify(userRsvps, null, 2)}`)
    }
  })

  test('6. API Response Verification - Capture Actual Data', async ({ page }) => {
    console.log('📡 Testing API responses directly...')

    // Test /api/events endpoint
    const eventsResponse = await page.request.get('http://localhost:5655/api/events')
    expect(eventsResponse.ok()).toBeTruthy()
    const eventsResponseData = await eventsResponse.json()

    // Handle the response structure correctly
    const events = eventsResponseData.success ? eventsResponseData.data : eventsResponseData

    console.log('📊 /api/events response:')
    if (Array.isArray(events)) {
      events.forEach((event: any, index: number) => {
        console.log(`  Event ${index + 1}: ${event.title} - Capacity: ${event.capacity || 'N/A'}, Current: ${event.currentParticipants || 'N/A'}`)
      })
    } else {
      console.log('⚠️ Events response is not an array:', events)
    }

    // Find Rope Social event and test its participation endpoint
    const ropeSocial = events.find((e: any) => e.title?.includes('Rope Social'))
    if (ropeSocial) {
      const participationResponse = await page.request.get(`http://localhost:5655/api/admin/events/${ropeSocial.id}/participations`)

      if (participationResponse.ok()) {
        const participations = await participationResponse.json()
        console.log(`📊 /api/admin/events/${ropeSocial.id}/participations response:`)
        console.log(JSON.stringify(participations, null, 2))
      } else {
        console.log(`❌ Participation endpoint failed: ${participationResponse.status()}`)
      }

      // Test user's participation status
      const userParticipationResponse = await page.request.get(`http://localhost:5655/api/participation/${ropeSocial.id}`)
      if (userParticipationResponse.ok()) {
        const userParticipation = await userParticipationResponse.json()
        console.log(`📊 /api/participation/${ropeSocial.id} response:`)
        console.log(JSON.stringify(userParticipation, null, 2))
      } else {
        console.log(`❌ User participation endpoint failed: ${userParticipationResponse.status()}`)
      }
    }

    // Create a summary file with all API responses
    const apiSummary = {
      timestamp: new Date().toISOString(),
      events: events,
      ropeSocialParticipation: ropeSocial ? await page.request.get(`http://localhost:5655/api/admin/events/${ropeSocial.id}/participations`).then(r => r.json()).catch(() => null) : null,
      userParticipation: ropeSocial ? await page.request.get(`http://localhost:5655/api/participation/${ropeSocial.id}`).then(r => r.json()).catch(() => null) : null
    }

    // Write API summary to file (can be manually saved for debugging)
    console.log('📄 Complete API Summary:')
    console.log(JSON.stringify(apiSummary, null, 2))
  })

  test('7. Console Errors Monitoring - Capture All JavaScript Issues', async ({ page }) => {
    console.log('🐛 Testing for console errors across key pages...')

    const pagesToTest = [
      { name: 'Public Events', url: 'http://localhost:5173/events' },
      { name: 'Dashboard', url: 'http://localhost:5173/dashboard', needsAuth: true },
      { name: 'Admin Events', url: 'http://localhost:5173/admin/events', needsAuth: true }
    ]

    for (const pageInfo of pagesToTest) {
      console.log(`🔍 Testing ${pageInfo.name} for console errors...`)

      if (pageInfo.needsAuth) {
        await quickLogin(page, 'admin')
      }

      await page.goto(pageInfo.url)
      await page.waitForLoadState('networkidle')
      await page.waitForTimeout(3000) // Allow full page load

      // Take screenshot if there are errors
      if (consoleErrors.length > 0 || jsErrors.length > 0) {
        await page.screenshot({
          path: `test-results/console-errors-${pageInfo.name.replace(/\s+/g, '-').toLowerCase()}.png`,
          fullPage: true
        })
      }

      console.log(`  ${pageInfo.name} - JS Errors: ${jsErrors.length}, Console Errors: ${consoleErrors.length}`)
    }

    // Summary of all errors found
    const allErrors = {
      jsErrors: jsErrors,
      consoleErrors: consoleErrors,
      summary: {
        totalJSErrors: jsErrors.length,
        totalConsoleErrors: consoleErrors.length,
        criticalDateTimeErrors: consoleErrors.filter(e => e.includes('RangeError') || e.includes('Invalid time value')).length
      }
    }

    console.log('🐛 Error Summary:')
    console.log(JSON.stringify(allErrors, null, 2))
  })
})