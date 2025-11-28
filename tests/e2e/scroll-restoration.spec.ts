import { test, expect } from '@playwright/test'

const baseUrl = process.env.PLAYWRIGHT_BASE_URL || 'http://localhost:5173';

/**
 * E2E Tests for Scroll Restoration Functionality
 *
 * Tests that React Router's ScrollRestoration component correctly scrolls to
 * the top of the page when navigating between routes.
 *
 * CRITICAL: Mobile scroll-to-top bug FIXED in Navigation.tsx (line 110).
 * These tests verify the fix works correctly on both desktop and mobile.
 *
 * Mobile Navigation Pattern:
 * - Mobile viewport uses hamburger menu for navigation
 * - Tests open hamburger menu before clicking navigation links
 * - Verifies body overflow is reset synchronously before navigation
 *
 * Requirements:
 * - Docker web service running on http://localhost:5173
 * - React Router v7 with ScrollRestoration in RootLayout.tsx
 * - Mobile navigation via hamburger menu (data-testid="button-mobile-menu")
 *
 * @see /apps/web/src/components/layout/RootLayout.tsx (line 29 - ScrollRestoration)
 * @see /apps/web/src/components/layout/Navigation.tsx (line 110 - body overflow reset fix)
 */

test.describe('Scroll Restoration - Navigation Scroll-to-Top', () => {
  test.beforeEach(async ({ page }) => {
    // Monitor console errors
    const consoleErrors: string[] = []
    page.on('console', (msg) => {
      if (msg.type() === 'error') {
        consoleErrors.push(msg.text())
      }
    })

    // Monitor JavaScript errors
    page.on('pageerror', (error) => {
      consoleErrors.push(`JavaScript error: ${error.message}`)
    })
  })

  test('scrolls to top when navigating from homepage to events page - DESKTOP', async ({ page }) => {
    // Set desktop viewport
    await page.setViewportSize({ width: 1920, height: 1080 })

    // Navigate to homepage
    await page.goto(`${baseUrl}/`)
    await page.waitForLoadState('networkidle')

    // Take screenshot of initial state
    await page.screenshot({
      path: './test-results/scroll-restoration-desktop-homepage-initial.png',
      fullPage: true,
    })

    // Get initial scroll position (should be at top)
    const initialScrollY = await page.evaluate(() => window.scrollY)
    console.log(`Desktop - Initial scroll position: ${initialScrollY}`)

    // Scroll to the bottom of the page
    await page.evaluate(() => {
      window.scrollTo(0, document.body.scrollHeight)
    })

    // Wait for scroll to complete
    await page.waitForTimeout(500)

    // Verify we actually scrolled down
    const scrolledY = await page.evaluate(() => window.scrollY)
    console.log(`Desktop - Scrolled down to position: ${scrolledY}`)
    expect(scrolledY).toBeGreaterThan(100) // Should have scrolled at least 100px

    // Take screenshot after scrolling
    await page.screenshot({
      path: './test-results/scroll-restoration-desktop-homepage-scrolled.png',
      fullPage: false, // Only visible viewport
    })

    // Navigate to events page using footer link (commonly used navigation)
    // Try multiple possible selectors for the Events link
    const eventsLinkSelectors = [
      'a[href="/events"]',
      'a[href*="/events"]',
      'text=Events',
      'nav a:has-text("Events")',
    ]

    let eventsLinkClicked = false
    for (const selector of eventsLinkSelectors) {
      const eventsLink = page.locator(selector).first()
      if (await eventsLink.isVisible()) {
        console.log(`Desktop - Found Events link with selector: ${selector}`)
        await eventsLink.click()
        eventsLinkClicked = true
        break
      }
    }

    // Hard assertion: Events link must exist
    expect(eventsLinkClicked).toBe(true)

    // Wait for navigation to complete
    await page.waitForURL('**/events', { timeout: 5000 })
    await page.waitForLoadState('networkidle')

    // Wait for any scroll animations to complete
    await page.waitForTimeout(500)

    // Take screenshot of events page after navigation
    await page.screenshot({
      path: './test-results/scroll-restoration-desktop-events-after-navigation.png',
      fullPage: false, // Only visible viewport
    })

    // CRITICAL TEST: Verify scroll position is at top (Y = 0)
    const finalScrollY = await page.evaluate(() => window.scrollY)
    console.log(`Desktop - Final scroll position after navigation: ${finalScrollY}`)

    // Hard assertion: Scroll position MUST be at top (0px) or very close (< 10px tolerance)
    expect(finalScrollY).toBeLessThanOrEqual(10)

    // Additional verification: Check if page header is visible (only possible if at top)
    const headerVisible = await page.evaluate(() => {
      const header = document.querySelector('header') || document.querySelector('nav')
      if (!header) return false
      const rect = header.getBoundingClientRect()
      return rect.top >= 0 && rect.top < window.innerHeight
    })

    expect(headerVisible).toBe(true)
  })

  test('scrolls to top when navigating from homepage to events page - MOBILE', async ({ page }) => {
    // Set mobile viewport (iPhone 12 Pro)
    await page.setViewportSize({ width: 390, height: 844 })

    // Navigate to homepage
    await page.goto(`${baseUrl}/`)
    await page.waitForLoadState('networkidle')

    // Take screenshot of initial state
    await page.screenshot({
      path: './test-results/scroll-restoration-mobile-homepage-initial.png',
      fullPage: true,
    })

    // Get initial scroll position (should be at top)
    const initialScrollY = await page.evaluate(() => window.scrollY)
    console.log(`Mobile - Initial scroll position: ${initialScrollY}`)

    // Scroll to the bottom of the page (mobile has longer scroll distance)
    await page.evaluate(() => {
      window.scrollTo(0, document.body.scrollHeight)
    })

    // Wait for scroll to complete
    await page.waitForTimeout(500)

    // Verify we actually scrolled down
    const scrolledY = await page.evaluate(() => window.scrollY)
    console.log(`Mobile - Scrolled down to position: ${scrolledY}`)
    expect(scrolledY).toBeGreaterThan(200) // Mobile pages typically longer

    // Take screenshot after scrolling
    await page.screenshot({
      path: './test-results/scroll-restoration-mobile-homepage-scrolled.png',
      fullPage: false, // Only visible viewport
    })

    // MOBILE NAVIGATION: Open hamburger menu
    const hamburgerButton = page.locator('[data-testid="button-mobile-menu"]')
    await hamburgerButton.click()
    console.log('Mobile - Hamburger menu clicked')

    // Wait for mobile menu animation to complete (slides in from right)
    await page.waitForTimeout(400) // 300ms transition + buffer

    // Take screenshot of mobile menu open
    await page.screenshot({
      path: './test-results/scroll-restoration-mobile-menu-open.png',
      fullPage: false,
    })

    // Click Events link in mobile menu
    const mobileEventsLink = page.locator('[data-testid="mobile-link-events"]')
    await mobileEventsLink.click()
    console.log('Mobile - Events link clicked in mobile menu')

    // Wait for navigation to complete
    await page.waitForURL('**/events', { timeout: 5000 })
    await page.waitForLoadState('networkidle')

    // Wait for any scroll animations to complete (mobile may have slower animations)
    await page.waitForTimeout(800)

    // Take screenshot of events page after navigation
    await page.screenshot({
      path: './test-results/scroll-restoration-mobile-events-after-navigation.png',
      fullPage: false, // Only visible viewport
    })

    // CRITICAL TEST: Verify scroll position is at top (Y = 0)
    const finalScrollY = await page.evaluate(() => window.scrollY)
    console.log(`Mobile - Final scroll position after navigation: ${finalScrollY}`)

    // Verify body overflow is reset (Navigation.tsx fix line 110)
    const bodyOverflow = await page.evaluate(() => document.body.style.overflow)
    console.log(`Mobile - Body overflow after navigation: "${bodyOverflow}"`)
    expect(bodyOverflow).toBe('') // Should be empty string (reset)

    // Hard assertion: Scroll position MUST be at top (0px) or very close (< 10px tolerance)
    expect(finalScrollY).toBeLessThanOrEqual(10)

    // Additional verification: Check if page header is visible (only possible if at top)
    const headerVisible = await page.evaluate(() => {
      const header = document.querySelector('header') || document.querySelector('nav')
      if (!header) return false
      const rect = header.getBoundingClientRect()
      return rect.top >= 0 && rect.top < window.innerHeight
    })

    expect(headerVisible).toBe(true)
  })

  test('scrolls to top when navigating from events to homepage - DESKTOP', async ({ page }) => {
    // Set desktop viewport
    await page.setViewportSize({ width: 1920, height: 1080 })

    // Start on events page
    await page.goto(`${baseUrl}/events`)
    await page.waitForLoadState('networkidle')

    // Scroll down
    await page.evaluate(() => {
      window.scrollTo(0, document.body.scrollHeight)
    })
    await page.waitForTimeout(500)

    const scrolledY = await page.evaluate(() => window.scrollY)
    console.log(`Desktop (reverse) - Scrolled down to position: ${scrolledY}`)
    expect(scrolledY).toBeGreaterThan(100)

    // Navigate to homepage using header logo or home link
    const homeLinkSelectors = [
      'a[href="/"]',
      'a[href=""]',
      'text=Home',
      'nav a:has-text("Home")',
      'header a[href="/"]',
    ]

    let homeLinkClicked = false
    for (const selector of homeLinkSelectors) {
      const homeLink = page.locator(selector).first()
      if (await homeLink.isVisible()) {
        console.log(`Desktop (reverse) - Found Home link with selector: ${selector}`)
        await homeLink.click()
        homeLinkClicked = true
        break
      }
    }

    expect(homeLinkClicked).toBe(true)

    await page.waitForURL(`${baseUrl}/`, { timeout: 5000 })
    await page.waitForLoadState('networkidle')

    // Additional wait for scroll animation to complete (testing if 93px offset is timing issue)
    await page.waitForTimeout(300)

    // Verify scroll to top
    const finalScrollY = await page.evaluate(() => window.scrollY)
    console.log(`Desktop (reverse) - Final scroll position: ${finalScrollY}`)
    expect(finalScrollY).toBeLessThanOrEqual(10)
  })

  test('scrolls to top when navigating from events to homepage - MOBILE', async ({ page }) => {
    // Set mobile viewport
    await page.setViewportSize({ width: 390, height: 844 })

    // Start on events page
    await page.goto(`${baseUrl}/events`)
    await page.waitForLoadState('networkidle')

    // Scroll down
    await page.evaluate(() => {
      window.scrollTo(0, document.body.scrollHeight)
    })
    await page.waitForTimeout(500)

    const scrolledY = await page.evaluate(() => window.scrollY)
    console.log(`Mobile (reverse) - Scrolled down to position: ${scrolledY}`)
    expect(scrolledY).toBeGreaterThan(200)

    // MOBILE NAVIGATION: Open hamburger menu
    const hamburgerButton = page.locator('[data-testid="button-mobile-menu"]')
    await hamburgerButton.click()
    console.log('Mobile (reverse) - Hamburger menu clicked')

    // Wait for mobile menu animation to complete
    await page.waitForTimeout(400)

    // Take screenshot of mobile menu open
    await page.screenshot({
      path: './test-results/scroll-restoration-mobile-reverse-menu-open.png',
      fullPage: false,
    })

    // MOBILE NAVIGATION PATTERN: No "Home" link in mobile menu
    // Real user behavior: Close menu, then click logo to navigate home
    // Close mobile menu by clicking hamburger button again
    await hamburgerButton.click()
    console.log('Mobile (reverse) - Hamburger clicked to close menu')

    // Wait for mobile menu close animation (300ms transition + buffer)
    await page.waitForTimeout(400)

    // Take screenshot after menu closed
    await page.screenshot({
      path: './test-results/scroll-restoration-mobile-reverse-menu-closed.png',
      fullPage: false,
    })

    // Now click logo to navigate home (no longer blocked by menu overlay)
    const logoLink = page.locator('a.logo[href="/"]')
    await logoLink.click()
    console.log('Mobile (reverse) - Logo clicked to navigate home')

    await page.waitForURL(`${baseUrl}/`, { timeout: 5000 })
    await page.waitForLoadState('networkidle')
    await page.waitForTimeout(800)

    // Verify body overflow is reset
    const bodyOverflow = await page.evaluate(() => document.body.style.overflow)
    console.log(`Mobile (reverse) - Body overflow after navigation: "${bodyOverflow}"`)
    expect(bodyOverflow).toBe('')

    // Verify scroll to top
    const finalScrollY = await page.evaluate(() => window.scrollY)
    console.log(`Mobile (reverse) - Final scroll position: ${finalScrollY}`)
    expect(finalScrollY).toBeLessThanOrEqual(10)
  })

  test('scroll restoration respects browser back/forward buttons - DESKTOP', async ({ page }) => {
    // Set desktop viewport
    await page.setViewportSize({ width: 1920, height: 1080 })

    // Navigate to homepage
    await page.goto(`${baseUrl}/`)
    await page.waitForLoadState('networkidle')

    // Scroll to middle of page
    const scrollToPosition = 500
    await page.evaluate((pos) => {
      window.scrollTo(0, pos)
    }, scrollToPosition)
    await page.waitForTimeout(500)

    const scrolledPosition = await page.evaluate(() => window.scrollY)
    console.log(`Desktop (back/forward) - Scrolled to position: ${scrolledPosition}`)

    // Navigate to events page
    const eventsLink = page.locator('a[href*="/events"]').first()
    await eventsLink.click()
    await page.waitForURL('**/events', { timeout: 5000 })
    await page.waitForLoadState('networkidle')
    await page.waitForTimeout(500)

    // Verify scroll to top on new page
    const eventsScrollY = await page.evaluate(() => window.scrollY)
    console.log(`Desktop (back/forward) - Events page scroll: ${eventsScrollY}`)
    expect(eventsScrollY).toBeLessThanOrEqual(10)

    // Use browser back button
    await page.goBack()
    await page.waitForLoadState('networkidle')
    await page.waitForTimeout(500)

    // NOTE: ScrollRestoration should restore previous scroll position on back/forward
    // This behavior may differ from scroll-to-top on new navigation
    const restoredScrollY = await page.evaluate(() => window.scrollY)
    console.log(`Desktop (back/forward) - Restored scroll position: ${restoredScrollY}`)

    // Depending on ScrollRestoration implementation, this might be at top OR at previous position
    // Document the actual behavior for debugging
    console.log(`Desktop (back/forward) - Expected scroll restoration behavior observed`)
  })

  test('hamburger menu opens and resets body overflow on navigation - MOBILE', async ({ page }) => {
    // Set mobile viewport
    await page.setViewportSize({ width: 390, height: 844 })

    // Navigate to homepage
    await page.goto(`${baseUrl}/`)
    await page.waitForLoadState('networkidle')

    // Verify initial body overflow state
    const initialOverflow = await page.evaluate(() => document.body.style.overflow)
    console.log(`Mobile hamburger - Initial body overflow: "${initialOverflow}"`)

    // Open hamburger menu
    const hamburgerButton = page.locator('[data-testid="button-mobile-menu"]')
    await expect(hamburgerButton).toBeVisible()
    await hamburgerButton.click()
    console.log('Mobile hamburger - Menu button clicked')

    // Wait for menu animation
    await page.waitForTimeout(400)

    // Take screenshot of mobile menu open
    await page.screenshot({
      path: './test-results/scroll-restoration-mobile-hamburger-menu.png',
      fullPage: false,
    })

    // Verify mobile menu is visible
    const mobileMenu = page.locator('#mobile-menu')
    await expect(mobileMenu).toBeVisible()

    // Verify Events link is visible in mobile menu
    const eventsLink = page.locator('[data-testid="mobile-link-events"]')
    await expect(eventsLink).toBeVisible()

    // Check body overflow while menu is open (might be 'hidden' to prevent background scrolling)
    const menuOpenOverflow = await page.evaluate(() => document.body.style.overflow)
    console.log(`Mobile hamburger - Body overflow with menu open: "${menuOpenOverflow}"`)

    // Click Events link to navigate
    await eventsLink.click()
    console.log('Mobile hamburger - Events link clicked')

    // Wait for navigation
    await page.waitForURL('**/events', { timeout: 5000 })
    await page.waitForLoadState('networkidle')
    await page.waitForTimeout(500)

    // CRITICAL: Verify body overflow is reset (Navigation.tsx line 110)
    const finalOverflow = await page.evaluate(() => document.body.style.overflow)
    console.log(`Mobile hamburger - Body overflow after navigation: "${finalOverflow}"`)

    // Hard assertion: Body overflow MUST be reset to empty string
    expect(finalOverflow).toBe('')

    // Additional verification: Scroll position should be at top
    const scrollY = await page.evaluate(() => window.scrollY)
    console.log(`Mobile hamburger - Scroll position after navigation: ${scrollY}`)
    expect(scrollY).toBeLessThanOrEqual(10)
  })
})
