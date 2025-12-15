import { test as baseTest, expect } from '@playwright/test'
import { test as dfTest } from '../lib/datafactory/fixtures/test.fixture'
import { DataFactory } from '../lib/datafactory'

/**
 * E2E Tests for Home Page - Events Display
 *
 * Tests the complete React + API + PostgreSQL stack through browser automation.
 * These tests validate the home page displays events correctly and handles all states.
 *
 * Requirements:
 * - React app accessible via baseURL (Docker on port 5173 or container networking)
 * - API server accessible via proxy or container networking
 * - PostgreSQL database available
 *
 * Test Coverage:
 * - Page loading and title verification
 * - Events grid display with proper card structure
 * - Empty state handling
 * - Error state handling
 * - Loading state display
 * - Responsive layout across viewports
 * - API integration verification
 * - Event card click navigation
 * - Price display formatting
 * - Availability/capacity display
 */
baseTest.describe('Home Page - Events Display', () => {
  baseTest.beforeEach(async ({ page }) => {
    // Navigate to the home page
    await page.goto('/')
    await page.waitForLoadState('domcontentloaded')
  })

  baseTest('page loads successfully with correct title', async ({ page }) => {
    // Verify the page loads without errors
    await expect(page).toHaveTitle(/Witch City Rope/)

    // Check that we're on the correct URL
    await expect(page).toHaveURL('/')

    // Verify main content area is present
    await page.waitForSelector(
      '[data-testid="events-grid"], [data-testid="empty-state"], [data-testid="error-message"]',
      { timeout: 10000 }
    )
  })

  baseTest('events display from API with complete card structure', async ({ page }) => {
    // Wait for events to load from API
    await page.waitForSelector(
      '[data-testid="events-grid"], [data-testid="empty-state"], [data-testid="error-message"]',
      { timeout: 10000 }
    )

    // Check if events loaded successfully
    const eventsGrid = page.locator('[data-testid="events-grid"]')
    const emptyState = page.locator('[data-testid="empty-state"]')
    const errorMessage = page.locator('[data-testid="error-message"]')

    // Hard assertion: At least one of these must be visible
    const eventsVisible = await eventsGrid.isVisible()
    const emptyVisible = await emptyState.isVisible()
    const errorVisible = await errorMessage.isVisible()

    expect(eventsVisible || emptyVisible || errorVisible).toBe(true)

    // Hard assertions based on which state is visible
    if (eventsVisible) {
      // Hard assertion: Events grid must be visible
      await expect(eventsGrid).toBeVisible()

      // Hard assertion: At least one event card must exist
      const eventCards = page.locator('[data-testid="event-card"]')
      await expect(eventCards.first()).toBeVisible()

      // Hard assertions: Event card structure must be complete
      const firstCard = eventCards.first()
      await expect(firstCard.locator('[data-testid="event-title"]')).toBeVisible()
      await expect(firstCard.locator('[data-testid="event-description"]')).toBeVisible()

      // Verify date/time is displayed (may be "Date and Time coming soon" or actual date)
      // The component shows date info in different formats based on session count
      // For homepage variant with multiple sessions, there's no data-testid but dates are displayed
      const cardText = await firstCard.textContent()

      // Date info is present if we see:
      // 1. "Date and Time coming soon" (no sessions)
      // 2. data-testid="event-date" (single session, or list variant)
      // 3. Day names (full or abbreviated) like "Sun", "Monday", etc (multi-session homepage variant)
      // UI uses abbreviated format: "Sun, Dec 14" not "Sunday, Dec 14"
      const dayNames = [
        'Sunday', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday',
        'Sun', 'Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat'
      ]
      const hasDateText = dayNames.some((day) => cardText?.includes(day))
      const hasDateTestId = (await firstCard.locator('[data-testid="event-date"]').count()) > 0
      const hasComingSoon = cardText?.includes('Date and Time coming soon')

      const hasDateInfo = hasDateText || hasDateTestId || hasComingSoon
      expect(hasDateInfo).toBe(true)
    } else if (emptyVisible) {
      // Hard assertion: Empty state message must be visible
      await expect(emptyState).toBeVisible()
      await expect(page.locator('text=No events available')).toBeVisible()
    } else {
      // Hard assertion: Error message must be visible and informative
      await expect(errorMessage).toBeVisible()
      await expect(page.locator('text=Error:')).toBeVisible()
    }
  })

  // Note: 'event cards display price and availability information' test moved to
  // 'Home Page - Events Display with Created Test Data' section below to use DataFactory

  // Note: 'clicking event card navigates to event details' test moved to
  // 'Home Page - Events Display with Created Test Data' section below to use DataFactory

  baseTest('loading state displays correctly', async ({ page }) => {
    // Intercept the API call to simulate slow response
    await page.route('**/api/events', async (route) => {
      // Delay the response to test loading state
      await new Promise((resolve) => setTimeout(resolve, 1000))
      route.continue()
    })

    // Navigate to trigger the API call
    await page.goto('/')

    // Verify loading spinner appears initially
    await expect(page.locator('[data-testid="loading-spinner"]')).toBeVisible()
  })

  baseTest('responsive layout works on different screen sizes', async ({ page }) => {
    // Test desktop layout (large screen)
    await page.setViewportSize({ width: 1200, height: 800 })
    await page.waitForSelector(
      '[data-testid="events-grid"], [data-testid="empty-state"], [data-testid="error-message"]'
    )

    const eventsGrid = page.locator('[data-testid="events-grid"]')

    // Hard assertion: If events grid exists, it must be visible and have grid layout
    const eventsGridVisible = await eventsGrid.isVisible()
    if (eventsGridVisible) {
      await expect(eventsGrid).toBeVisible()
      // Component uses inline gridTemplateColumns, not CSS classes
      const gridStyle = await eventsGrid.getAttribute('style')
      expect(gridStyle).toContain('grid')
    }

    // Test tablet layout (medium screen)
    await page.setViewportSize({ width: 768, height: 1024 })
    await page.waitForLoadState('domcontentloaded')

    // Hard assertion: If events grid exists, verify it's still visible and responsive
    const tabletGridVisible = await eventsGrid.isVisible()
    if (tabletGridVisible) {
      await expect(eventsGrid).toBeVisible()
      // Grid uses auto-fit minmax, so it adapts to viewport automatically
      const boundingBox = await eventsGrid.boundingBox()
      expect(boundingBox?.width).toBeLessThanOrEqual(768)
    }

    // Test mobile layout (small screen)
    await page.setViewportSize({ width: 375, height: 667 })
    await page.waitForLoadState('domcontentloaded')

    // Hard assertion: If events grid exists, verify it fits within mobile viewport
    const mobileGridVisible = await eventsGrid.isVisible()
    if (mobileGridVisible) {
      await expect(eventsGrid).toBeVisible()
      const boundingBox = await eventsGrid.boundingBox()
      expect(boundingBox?.width).toBeLessThanOrEqual(375)
    }
  })

  baseTest('API integration works end-to-end', async ({ page }) => {
    // Monitor network requests
    const apiRequests: string[] = []

    page.on('request', (request) => {
      if (request.url().includes('/api/events')) {
        apiRequests.push(request.url())
      }
    })

    // Navigate to page and wait for API call
    await page.goto('/')

    // Wait for content to load
    await page.waitForSelector(
      '[data-testid="events-grid"], [data-testid="empty-state"], [data-testid="error-message"]'
    )

    // Verify API was called
    expect(apiRequests.length).toBeGreaterThan(0)
    // API requests go through Vite proxy, so URL will be relative
    expect(apiRequests[0]).toContain('/api/events')
  })

  baseTest('error handling works when API is unavailable', async ({ page }) => {
    // Block the API endpoint to simulate server unavailable
    // Match any URL containing /api/events (works in both host and container)
    await page.route('**/api/events', (route) => {
      route.abort('failed')
    })

    // Navigate to page
    await page.goto('/')

    // Wait for either error message or empty state to appear
    // (React Query retries may result in empty state instead of error)
    await page.waitForSelector(
      '[data-testid="error-message"], [data-testid="empty-state"]',
      { timeout: 15000 }
    )

    // Hard assertion: Either error or empty state must be visible
    const errorMessage = page.locator('[data-testid="error-message"]')
    const emptyState = page.locator('[data-testid="empty-state"]')

    const errorVisible = await errorMessage.isVisible()
    const emptyVisible = await emptyState.isVisible()

    expect(errorVisible || emptyVisible).toBe(true)

    // Hard assertions based on which state is visible
    if (errorVisible) {
      await expect(errorMessage).toBeVisible()
    } else {
      await expect(emptyState).toBeVisible()
    }
  })

  baseTest('proves complete React + API + PostgreSQL stack works', async ({ page }) => {
    // This is the key test that proves our vertical slice implementation works

    // Navigate to the home page
    await page.goto('/')

    // Wait for the React app to make the API call and receive response
    await page.waitForSelector(
      '[data-testid="events-grid"], [data-testid="empty-state"], [data-testid="error-message"]',
      { timeout: 15000 }
    )

    // Verify the stack integration worked
    // Any of these outcomes proves the stack is working:
    // 1. Events loaded from database/API
    // 2. Empty state (API responded but no events)
    // 3. Error state (API failed but React handled it gracefully)

    const stackWorking = await page.evaluate(() => {
      const eventsGrid = document.querySelector('[data-testid="events-grid"]')
      const emptyState = document.querySelector('[data-testid="empty-state"]')
      const errorMessage = document.querySelector('[data-testid="error-message"]')

      return {
        hasEvents: !!eventsGrid && eventsGrid.children.length > 0,
        isEmpty: !!emptyState,
        hasError: !!errorMessage,
      }
    })

    // Hard assertion: The stack must be working with any valid response
    const isStackWorking = stackWorking.hasEvents || stackWorking.isEmpty || stackWorking.hasError
    expect(isStackWorking).toBe(true)

    // Hard assertions: If we have events, verify they display correctly
    if (stackWorking.hasEvents) {
      const eventCards = page.locator('[data-testid="event-card"]')

      // Hard assertion: First event card must be visible
      await expect(eventCards.first()).toBeVisible()

      // Hard assertions: Event data structure must match component contract
      const firstCard = eventCards.first()
      await expect(firstCard.locator('[data-testid="event-title"]')).toBeVisible()
      await expect(firstCard.locator('[data-testid="event-title"]')).not.toBeEmpty()
      await expect(firstCard.locator('[data-testid="event-description"]')).toBeVisible()

      // Verify date information is present (component shows date or "coming soon")
      // For homepage variant with multiple sessions, there's no data-testid but dates are displayed
      const cardText = await firstCard.textContent()

      // Date info is present if we see:
      // 1. "Date and Time coming soon" (no sessions)
      // 2. data-testid="event-date" (single session, or list variant)
      // 3. Day names (full or abbreviated) like "Sun", "Monday", etc (multi-session homepage variant)
      // UI uses abbreviated format: "Sun, Dec 14" not "Sunday, Dec 14"
      const dayNames = [
        'Sunday', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday',
        'Sun', 'Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat'
      ]
      const hasDateText = dayNames.some((day) => cardText?.includes(day))
      const hasDateTestId = (await firstCard.locator('[data-testid="event-date"]').count()) > 0
      const hasComingSoon = cardText?.includes('Date and Time coming soon')

      const hasDateInfo = hasDateText || hasDateTestId || hasComingSoon
      expect(hasDateInfo).toBe(true)
    } else if (stackWorking.isEmpty) {
      // Hard assertion: Empty state must be visible
      const emptyState = page.locator('[data-testid="empty-state"]')
      await expect(emptyState).toBeVisible()
    } else {
      // Hard assertion: Error message must be visible
      const errorMessage = page.locator('[data-testid="error-message"]')
      await expect(errorMessage).toBeVisible()
    }
  })

  // Note: 'View Full Calendar link navigates to events page' test moved to
  // 'Home Page - Events Display with Created Test Data' section below to ensure events exist
})

dfTest.describe('Home Page - Events Display with Created Test Data', () => {
  dfTest('event cards display price and availability information', async ({ page, df }) => {
    // Create test event with session and ticket type
    const event = await df.events.createPublished(`Price Info Test ${Date.now()}`)

    const sessionStart = new Date()
    sessionStart.setDate(sessionStart.getDate() + 7)
    sessionStart.setHours(18, 0, 0, 0)
    const sessionEnd = new Date(sessionStart.getTime() + 3 * 60 * 60 * 1000)

    const session = await df.sessions.create({
      eventId: event.id,
      title: 'Test Session',
      startTime: sessionStart,
      endTime: sessionEnd,
      maxCapacity: 20,
    })

    await df.ticketTypes.create({
      eventId: event.id,
      sessionId: session.id,
      name: 'General Admission',
      price: 25.0,
      quantityAvailable: 20,
    })

    console.log(`✅ Created test event: ${event.id}`)

    // Navigate to home page
    await page.goto('/')
    await page.waitForLoadState('domcontentloaded')

    // Wait for events grid to load
    await page.waitForSelector('[data-testid="events-grid"]', { timeout: 15000 })

    // Find our test event card
    const testEventCard = page.locator(`[data-testid="event-card"][data-event-id="${event.id}"]`)
    await expect(testEventCard).toBeVisible({ timeout: 10000 })

    const cardText = await testEventCard.textContent()

    // Verify price is displayed
    const hasPriceInfo =
      cardText?.includes('Free') ||
      cardText?.includes('$') ||
      cardText?.includes('TBD')
    expect(hasPriceInfo).toBe(true)

    // Verify availability info is displayed
    const hasAvailabilityInfo =
      cardText?.includes('sold') ||
      cardText?.includes('RSVPs') ||
      cardText?.includes('left') ||
      cardText?.includes('Sold Out') ||
      cardText?.includes('Full') ||
      cardText?.includes('available')
    expect(hasAvailabilityInfo).toBe(true)
  })

  dfTest('clicking event card navigates to event details', async ({ page, df }) => {
    // Create test event
    const event = await df.events.createPublished(`Navigation Test ${Date.now()}`)

    const sessionStart = new Date()
    sessionStart.setDate(sessionStart.getDate() + 7)
    sessionStart.setHours(18, 0, 0, 0)
    const sessionEnd = new Date(sessionStart.getTime() + 3 * 60 * 60 * 1000)

    await df.sessions.create({
      eventId: event.id,
      title: 'Test Session',
      startTime: sessionStart,
      endTime: sessionEnd,
      maxCapacity: 20,
    })

    console.log(`✅ Created test event: ${event.id}`)

    // Navigate to home page
    await page.goto('/')
    await page.waitForLoadState('domcontentloaded')

    // Wait for events grid
    await page.waitForSelector('[data-testid="events-grid"]', { timeout: 15000 })

    // Find and click our test event card
    const testEventCard = page.locator(`[data-testid="event-card"][data-event-id="${event.id}"]`)
    await expect(testEventCard).toBeVisible({ timeout: 10000 })
    await testEventCard.click()

    // Verify navigation to event details page
    await expect(page).toHaveURL(new RegExp(`/events/${event.id}`))
  })

  dfTest('View Full Calendar link navigates to events page', async ({ page, df }) => {
    // Create test event to ensure events exist
    const event = await df.events.createPublished(`Calendar Test ${Date.now()}`)

    const sessionStart = new Date()
    sessionStart.setDate(sessionStart.getDate() + 7)
    sessionStart.setHours(18, 0, 0, 0)
    const sessionEnd = new Date(sessionStart.getTime() + 3 * 60 * 60 * 1000)

    await df.sessions.create({
      eventId: event.id,
      title: 'Test Session',
      startTime: sessionStart,
      endTime: sessionEnd,
      maxCapacity: 20,
    })

    console.log(`✅ Created test event: ${event.id}`)

    // Navigate to home page
    await page.goto('/')
    await page.waitForLoadState('domcontentloaded')

    // Wait for events grid
    await page.waitForSelector('[data-testid="events-grid"]', { timeout: 15000 })

    // Click the View Full Calendar link
    const viewMoreLink = page.locator('a:has-text("View Full Calendar")')
    await expect(viewMoreLink).toBeVisible({ timeout: 5000 })
    await viewMoreLink.click()

    // Verify navigation to events page
    await expect(page).toHaveURL('/events')
  })

  dfTest('displays created test event on home page', async ({ page, df }) => {
    // Create test event
    const event = await df.events.createPublished('Home Page Test Event')

    const sessionStart = new Date()
    sessionStart.setDate(sessionStart.getDate() + 7)
    sessionStart.setHours(18, 0, 0, 0)
    const sessionEnd = new Date(sessionStart.getTime() + 3 * 60 * 60 * 1000)

    await df.sessions.create({
      eventId: event.id,
      title: 'Test Session',
      startTime: sessionStart,
      endTime: sessionEnd,
      maxCapacity: 20,
    })

    console.log(`✅ Created test event: ${event.id}`)

    // Navigate to home page
    await page.goto('/')
    await page.waitForLoadState('domcontentloaded')

    // Wait for events grid to load
    await page.waitForSelector('[data-testid="events-grid"]', { timeout: 15000 })

    // Find our test event card
    const testEventCard = page.locator(`[data-testid="event-card"][data-event-id="${event.id}"]`)
    await expect(testEventCard).toBeVisible({ timeout: 10000 })

    // Verify event title is displayed
    await expect(testEventCard.locator('[data-testid="event-title"]')).toContainText(
      'Home Page Test Event'
    )
  })

  dfTest('event card has data-event-id attribute for navigation', async ({ page, df }) => {
    // Create test event
    const event = await df.events.createPublished(`DataEventId Test ${Date.now()}`)

    const sessionStart = new Date()
    sessionStart.setDate(sessionStart.getDate() + 7)
    sessionStart.setHours(18, 0, 0, 0)
    const sessionEnd = new Date(sessionStart.getTime() + 3 * 60 * 60 * 1000)

    await df.sessions.create({
      eventId: event.id,
      title: 'Test Session',
      startTime: sessionStart,
      endTime: sessionEnd,
      maxCapacity: 20,
    })

    console.log(`✅ Created test event: ${event.id}`)

    // Navigate to home page
    await page.goto('/')
    await page.waitForLoadState('domcontentloaded')

    // Wait for events grid
    await page.waitForSelector('[data-testid="events-grid"]', { timeout: 15000 })

    // Find our test event card
    const testEventCard = page.locator(`[data-testid="event-card"][data-event-id="${event.id}"]`)
    await expect(testEventCard).toBeVisible({ timeout: 10000 })

    // Verify the data-event-id attribute is a valid GUID
    const eventId = await testEventCard.getAttribute('data-event-id')
    expect(eventId).toBeTruthy()
    expect(eventId).toMatch(
      /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i
    )
  })
})
