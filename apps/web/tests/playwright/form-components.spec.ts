import { test, expect } from '@playwright/test'

test.describe('Form Components Test Page', () => {
  test('Form Components Test Page loads without Mantine import errors', async ({ page }) => {
    // Navigate to the form components test page
    await page.goto('http://localhost:5173/form-test')
    
    // Wait for the page to load completely
    await page.waitForLoadState('networkidle')
    
    // Check that the page loads without errors
    await expect(page).toHaveTitle('Vite + React + TS')
    
    // Verify the page doesn't show any import error overlays
    const errorOverlay = page.locator('[data-vite-dev-id="error-overlay"]')
    await expect(errorOverlay).not.toBeVisible()
    
    // Check that React content is rendered (look for the root div to be populated)
    const rootDiv = page.locator('#root')
    await expect(rootDiv).not.toBeEmpty()
    
    // Wait a bit more to ensure no late-loading import errors
    await page.waitForTimeout(2000)
    
    // Verify no console errors related to Mantine imports
    const logs = []
    page.on('console', msg => {
      if (msg.type() === 'error') {
        logs.push(msg.text())
      }
    })
    
    // Refresh to capture any console errors
    await page.reload()
    await page.waitForLoadState('networkidle')
    
    // Check that there are no import-related errors
    const importErrors = logs.filter(log => 
      log.includes('@mantine/core') || 
      log.includes('Failed to resolve import') ||
      log.includes('Does the file exist?')
    )
    
    expect(importErrors).toHaveLength(0)
  })
  
  test('Form Components Test Page renders Mantine components', async ({ page }) => {
    // Navigate to the form components test page
    await page.goto('http://localhost:5173/form-test')
    
    // Wait for the page to load completely
    await page.waitForLoadState('networkidle')
    
    // Wait for React to render
    await page.waitForTimeout(1000)
    
    // Check if any Mantine-styled elements are present
    // Look for Mantine CSS classes or data attributes
    const mantineElements = await page.locator('[class*="mantine"], [data-mantine-"], .m-')
    
    // We expect at least some Mantine elements to be present if the components are working
    // This is a basic smoke test - the exact count may vary
    const elementCount = await mantineElements.count()
    
    // If Mantine is working, we should see some styled elements
    expect(elementCount).toBeGreaterThan(0)
  })
})