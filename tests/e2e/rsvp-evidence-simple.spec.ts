import { test, expect, Page } from '@playwright/test'

/**
 * SIMPLIFIED RSVP EVIDENCE COLLECTION
 *
 * This test focuses on capturing screenshots and API data without authentication
 * to provide PROOF of what's actually displayed vs what we think is working.
 *
 * This will help debug the user's reported RSVP count issues.
 */

test.describe('RSVP Evidence Collection - Simple', () => {
  let consoleErrors: string[] = []
  let jsErrors: string[] = []

  test.beforeEach(async ({ page }) => {
    // Reset error arrays
    consoleErrors = []
    jsErrors = []

    // Monitor JavaScript errors
    page.on('pageerror', error => {
      jsErrors.push(error.toString())
      console.log(`🚨 JavaScript Error: ${error.toString()}`)
    })

    // Monitor console errors
    page.on('console', msg => {
      if (msg.type() === 'error') {
        const errorText = msg.text()
        consoleErrors.push(errorText)
        console.log(`🚨 Console Error: ${errorText}`)
      }
    })
  })

  test('1. API Response Analysis - Get Actual RSVP Data', async ({ page }) => {
    console.log('📡 Testing API responses to see actual RSVP counts...')

    // Test /api/events endpoint
    const eventsResponse = await page.request.get('http://localhost:5655/api/events')
    expect(eventsResponse.ok()).toBeTruthy()
    const eventsResponseData = await eventsResponse.json()

    // Handle the response structure correctly
    const events = eventsResponseData.success ? eventsResponseData.data : eventsResponseData

    console.log('📊 Raw API response structure:')
    console.log(JSON.stringify(eventsResponseData, null, 2))

    if (Array.isArray(events)) {
      console.log(`📊 Found ${events.length} events`)

      events.forEach((event: any, index: number) => {
        console.log(`\n📅 Event ${index + 1}: ${event.title}`)
        console.log(`   - Capacity: ${event.capacity || 'N/A'}`)
        console.log(`   - Current Attendees: ${event.currentAttendees || 'N/A'}`)
        console.log(`   - Current RSVPs: ${event.currentRSVPs || 'N/A'}`)
        console.log(`   - Current Tickets: ${event.currentTickets || 'N/A'}`)
        console.log(`   - Is Published: ${event.isPublished}`)
      })

      // Look specifically for Rope Social event
      const ropeSocialEvents = events.filter((e: any) => e.title?.toLowerCase().includes('rope social') || e.title?.toLowerCase().includes('social'))
      if (ropeSocialEvents.length > 0) {
        console.log('\n🎯 ROPE SOCIAL EVENTS FOUND:')
        ropeSocialEvents.forEach((event: any, index: number) => {
          console.log(`\n   Rope Social ${index + 1}: ${event.title}`)
          console.log(`   - ID: ${event.id}`)
          console.log(`   - Capacity: ${event.capacity}`)
          console.log(`   - Current Attendees: ${event.currentAttendees}`)
          console.log(`   - Current RSVPs: ${event.currentRSVPs}`)
          console.log(`   - Current Tickets: ${event.currentTickets}`)
          console.log(`   - Sessions: ${event.sessions?.length || 0}`)
          console.log(`   - Ticket Types: ${event.ticketTypes?.length || 0}`)
        })
      } else {
        console.log('\n⚠️ No Rope Social events found')
        console.log('Available events:')
        events.forEach((e: any) => console.log(`   - ${e.title}`))
      }
    } else {
      console.log('⚠️ Events response is not an array:', events)
    }
  })

  test('2. Public Events Page - Capture What Users See', async ({ page }) => {
    console.log('📸 Capturing public events page as users see it...')

    await page.goto('/events')
    await page.waitForLoadState('domcontentloaded')
    await page.waitForTimeout(3000) // Allow React components to render

    // Check for errors FIRST
    if (jsErrors.length > 0 || consoleErrors.length > 0) {
      console.log(`⚠️ Page has errors - JS errors: ${jsErrors.length}, Console errors: ${consoleErrors.length}`)
    }

    // Take full page screenshot
    await page.screenshot({
      path: 'test-results/public-events-full-page.png',
      fullPage: true
    })

    // Look for event elements
    const eventElements = await page.locator('[data-testid*="event"], .event-card, .card, .event').count()
    console.log(`📊 Found ${eventElements} event-related elements`)

    // Try different selectors to find event cards
    const possibleSelectors = [
      '[data-testid*="event"]',
      '.event-card',
      '.card',
      '[class*="event"]',
      '[class*="Card"]'
    ]

    for (const selector of possibleSelectors) {
      const count = await page.locator(selector).count()
      if (count > 0) {
        console.log(`✅ Found ${count} elements with selector: ${selector}`)

        // Take screenshot of first few elements
        for (let i = 0; i < Math.min(count, 3); i++) {
          const element = page.locator(selector).nth(i)
          const text = await element.textContent()

          await element.screenshot({
            path: `test-results/event-element-${selector.replace(/[^a-zA-Z0-9]/g, '_')}-${i}.png`
          })

          console.log(`📸 Element ${i} text preview: ${text?.substring(0, 100)}...`)
        }
      }
    }

    // Check for any text containing numbers (potential capacity displays)
    const numberTexts = await page.locator('text=/\\d+/').allTextContents()
    console.log(`📊 Found ${numberTexts.length} elements with numbers:`)
    numberTexts.slice(0, 10).forEach((text, i) => {
      console.log(`   ${i + 1}: "${text.trim()}"`)
    })
  })

  test('3. Event Details Page - Pick First Available Event', async ({ page }) => {
    console.log('📸 Testing event details page...')

    // First get events from API
    const eventsResponse = await page.request.get('http://localhost:5655/api/events')
    const eventsResponseData = await eventsResponse.json()
    const events = eventsResponseData.success ? eventsResponseData.data : eventsResponseData

    if (Array.isArray(events) && events.length > 0) {
      const firstEvent = events[0]
      console.log(`📅 Testing first event: ${firstEvent.title} (ID: ${firstEvent.id})`)

      // Navigate to event details
      await page.goto(`/events/${firstEvent.id}`)
      await page.waitForLoadState('domcontentloaded')
      await page.waitForTimeout(3000)

      // Take screenshot
      await page.screenshot({
        path: 'test-results/event-details-page.png',
        fullPage: true
      })

      // Look for RSVP/capacity related elements
      const rsvpElements = await page.locator('text=/rsvp|capacity|spots|available|register|ticket/i').count()
      console.log(`📊 Found ${rsvpElements} RSVP/capacity related elements`)

      // Look for buttons
      const buttons = await page.locator('button').count()
      console.log(`📊 Found ${buttons} buttons`)

      for (let i = 0; i < Math.min(buttons, 5); i++) {
        const button = page.locator('button').nth(i)
        const buttonText = await button.textContent()
        console.log(`🔘 Button ${i + 1}: "${buttonText?.trim()}"`)
      }

      // Check for any forms
      const forms = await page.locator('form').count()
      console.log(`📊 Found ${forms} forms`)
    }
  })

  test('4. Dashboard Page Access Check', async ({ page }) => {
    console.log('📸 Testing dashboard page access...')

    // Try to access dashboard directly (should redirect to login if not authenticated)
    await page.goto('/dashboard')
    await page.waitForLoadState('domcontentloaded')
    await page.waitForTimeout(2000)

    const currentUrl = page.url()
    console.log(`📍 Dashboard access resulted in URL: ${currentUrl}`)

    // Take screenshot regardless of where we end up
    await page.screenshot({
      path: 'test-results/dashboard-access-result.png',
      fullPage: true
    })

    if (currentUrl.includes('login')) {
      console.log('✅ Correctly redirected to login page (authentication working)')
    } else if (currentUrl.includes('dashboard')) {
      console.log('⚠️ Reached dashboard without authentication (potential security issue)')
    } else {
      console.log(`⚠️ Unexpected redirect to: ${currentUrl}`)
    }
  })

  test('5. Admin Pages Access Check', async ({ page }) => {
    console.log('📸 Testing admin pages access...')

    const adminUrls = [
      '/admin',
      '/admin/events'
    ]

    for (const url of adminUrls) {
      console.log(`📍 Testing: ${url}`)

      await page.goto(url)
      await page.waitForLoadState('domcontentloaded')
      await page.waitForTimeout(2000)

      const finalUrl = page.url()
      const urlPath = new URL(url).pathname

      await page.screenshot({
        path: `test-results/admin-access-${urlPath.replace(/\//g, '_')}.png`,
        fullPage: true
      })

      console.log(`   - Requested: ${url}`)
      console.log(`   - Final URL: ${finalUrl}`)

      if (finalUrl.includes('login')) {
        console.log('   ✅ Correctly redirected to login (security working)')
      } else if (finalUrl === url) {
        console.log('   ⚠️ Direct access allowed (potential security issue)')
      }
    }
  })

  test('6. Console Error Summary', async ({ page }) => {
    console.log('🐛 Testing for console errors across key pages...')

    const pagesToTest = [
      { name: 'home', url: '/' },
      { name: 'events', url: '/events' },
      { name: 'login', url: '/login' }
    ]

    const allErrors: any = {
      pageErrors: {},
      summary: {
        totalJSErrors: 0,
        totalConsoleErrors: 0,
        pagesWithErrors: 0
      }
    }

    for (const pageInfo of pagesToTest) {
      console.log(`🔍 Testing ${pageInfo.name} page for errors...`)

      // Reset error counts for this page
      const pageJSErrors: string[] = []
      const pageConsoleErrors: string[] = []

      page.removeAllListeners('pageerror')
      page.removeAllListeners('console')

      page.on('pageerror', error => pageJSErrors.push(error.toString()))
      page.on('console', msg => {
        if (msg.type() === 'error') {
          pageConsoleErrors.push(msg.text())
        }
      })

      await page.goto(pageInfo.url)
      await page.waitForLoadState('domcontentloaded')
      await page.waitForTimeout(2000)

      // Record errors for this page
      allErrors.pageErrors[pageInfo.name] = {
        jsErrors: [...pageJSErrors],
        consoleErrors: [...pageConsoleErrors],
        url: pageInfo.url
      }

      if (pageJSErrors.length > 0 || pageConsoleErrors.length > 0) {
        await page.screenshot({
          path: `test-results/errors-${pageInfo.name}.png`,
          fullPage: true
        })

        allErrors.summary.pagesWithErrors++
      }

      allErrors.summary.totalJSErrors += pageJSErrors.length
      allErrors.summary.totalConsoleErrors += pageConsoleErrors.length

      console.log(`   - JS Errors: ${pageJSErrors.length}`)
      console.log(`   - Console Errors: ${pageConsoleErrors.length}`)
    }

    console.log('\n🐛 Error Summary:')
    console.log(JSON.stringify(allErrors, null, 2))
  })
})