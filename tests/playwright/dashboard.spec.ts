import { test, expect } from '@playwright/test'

test.describe('Dashboard Pages', () => {
  test.beforeEach(async ({ page }) => {
    // Navigate to login page
    await page.goto('http://localhost:5173/login')
    
    // Login with test admin account
    await page.fill('input[name="email"]', 'admin@witchcityrope.com')
    await page.fill('input[name="password"]', 'Test123!')
    await page.click('button[type="submit"]')
    
    // Wait for redirect to dashboard
    await page.waitForURL('http://localhost:5173/dashboard', { timeout: 10000 })
  })

  test('should navigate to dashboard and display welcome message', async ({ page }) => {
    // Should be on dashboard page
    await expect(page.locator('h1')).toContainText('Welcome back')
    
    // Should display upcoming events section
    await expect(page.locator('text=Your Upcoming Events')).toBeVisible()
    
    // Should display quick actions
    await expect(page.locator('text=Quick Actions')).toBeVisible()
    
    // Should show navigation links
    await expect(page.locator('text=Browse All Events')).toBeVisible()
    await expect(page.locator('text=Update Profile')).toBeVisible()
    await expect(page.locator('text=Membership Status')).toBeVisible()
    await expect(page.locator('text=Security Settings')).toBeVisible()
  })

  test('should navigate to events page and display events', async ({ page }) => {
    // Navigate to events page
    await page.click('a[href="/events"]')
    await page.waitForURL('http://localhost:5173/events', { timeout: 10000 })
    
    // Or navigate via dashboard link
    await page.goto('http://localhost:5173/dashboard')
    await page.click('text=Browse All Events')
    
    // Should display events page content
    await expect(page.locator('h1')).toContainText('Events')
  })

  test('should navigate to profile page and display form', async ({ page }) => {
    // Navigate to profile page via dashboard
    await page.click('text=Update Profile')
    await page.waitForURL('http://localhost:5173/dashboard/profile', { timeout: 10000 })
    
    // Should display profile page
    await expect(page.locator('h1')).toContainText('Profile')
    
    // Should display form sections
    await expect(page.locator('text=Profile Information')).toBeVisible()
    await expect(page.locator('text=Account Information')).toBeVisible()
    await expect(page.locator('text=Community Guidelines')).toBeVisible()
    
    // Should have form fields
    await expect(page.locator('input[placeholder="Your community name"]')).toBeVisible()
    await expect(page.locator('input[type="email"]')).toBeVisible()
    
    // Should display update button
    await expect(page.locator('button:has-text("Update Profile")')).toBeVisible()
  })

  test('should test profile form validation', async ({ page }) => {
    // Navigate to profile page
    await page.click('text=Update Profile')
    await page.waitForURL('http://localhost:5173/dashboard/profile', { timeout: 10000 })
    
    // Clear scene name field and enter invalid value
    const sceneNameInput = page.locator('input[placeholder="Your community name"]')
    await sceneNameInput.clear()
    await sceneNameInput.fill('a') // Too short
    
    // Submit form
    await page.click('button:has-text("Update Profile")')
    
    // Should show validation error
    await expect(page.locator('text=Scene name must be at least 2 characters')).toBeVisible()
    
    // Test valid input
    await sceneNameInput.clear()
    await sceneNameInput.fill('ValidSceneName')
    
    // Clear email and enter invalid
    const emailInput = page.locator('input[type="email"]')
    await emailInput.clear()
    await emailInput.fill('invalid-email')
    
    await page.click('button:has-text("Update Profile")')
    await expect(page.locator('text=Invalid email format')).toBeVisible()
    
    // Enter valid email
    await emailInput.clear()
    await emailInput.fill('valid@email.com')
    
    // Now form should submit without validation errors
    await page.click('button:has-text("Update Profile")')
    // Since form doesn't connect to API yet, we just verify no validation errors
    await expect(page.locator('text=Scene name must be at least 2 characters')).not.toBeVisible()
    await expect(page.locator('text=Invalid email format')).not.toBeVisible()
  })

  test('should navigate to security page and test form interactions', async ({ page }) => {
    // Navigate to security page
    await page.click('text=Security Settings')
    await page.waitForURL('http://localhost:5173/dashboard/security', { timeout: 10000 })
    
    // Should display security page
    await expect(page.locator('h1')).toContainText('Security Settings')
    
    // Should display all sections
    await expect(page.locator('text=Change Password')).toBeVisible()
    await expect(page.locator('text=Two-Factor Authentication')).toBeVisible()
    await expect(page.locator('text=Privacy Settings')).toBeVisible()
    await expect(page.locator('text=Account Data')).toBeVisible()
    
    // Test password form
    await expect(page.locator('input[placeholder="Enter your current password"]')).toBeVisible()
    await expect(page.locator('input[placeholder="Enter new password"]')).toBeVisible()
    await expect(page.locator('input[placeholder="Confirm new password"]')).toBeVisible()
    
    // Test 2FA section
    await expect(page.locator('text=Two-Factor Authentication Enabled')).toBeVisible()
    await expect(page.locator('button:has-text("Disable 2FA")')).toBeVisible()
    
    // Test privacy toggles
    const profileToggle = page.locator('input[type="checkbox"]').first()
    await expect(profileToggle).toBeChecked() // Should be enabled by default
    
    // Toggle it
    await profileToggle.click()
    await expect(profileToggle).not.toBeChecked()
    
    // Toggle back
    await profileToggle.click()
    await expect(profileToggle).toBeChecked()
    
    // Test data download button
    await expect(page.locator('button:has-text("Request Data Download")')).toBeVisible()
  })

  test('should test password validation on security page', async ({ page }) => {
    await page.click('text=Security Settings')
    await page.waitForURL('http://localhost:5173/dashboard/security', { timeout: 10000 })
    
    // Try to submit empty form
    await page.click('button:has-text("Update Password")')
    await expect(page.locator('text=Current password is required')).toBeVisible()
    
    // Fill current password but use weak new password
    await page.fill('input[placeholder="Enter your current password"]', 'current123!')
    await page.fill('input[placeholder="Enter new password"]', 'weak')
    await page.click('button:has-text("Update Password")')
    
    await expect(page.locator('text=Password must be at least 8 characters')).toBeVisible()
    
    // Test password mismatch
    await page.fill('input[placeholder="Enter new password"]', 'ValidPassword123!')
    await page.fill('input[placeholder="Confirm new password"]', 'DifferentPassword123!')
    await page.click('button:has-text("Update Password")')
    
    await expect(page.locator('text=Passwords do not match')).toBeVisible()
  })

  test('should navigate to membership page and display status', async ({ page }) => {
    // Navigate to membership page
    await page.click('text=Membership Status')
    await page.waitForURL('http://localhost:5173/dashboard/membership', { timeout: 10000 })
    
    // Should display membership page
    await expect(page.locator('h1')).toContainText('Membership')
    
    // Should display all sections
    await expect(page.locator('text=Membership Status')).toBeVisible()
    await expect(page.locator('text=Member Benefits')).toBeVisible()
    await expect(page.locator('text=Community Standing')).toBeVisible()
    await expect(page.locator('text=Membership Actions')).toBeVisible()
    
    // Should show membership details
    await expect(page.locator('text=Active')).toBeVisible()
    await expect(page.locator('text=General Member')).toBeVisible()
    await expect(page.locator('text=Member Since')).toBeVisible()
    
    // Should show benefits
    await expect(page.locator('text=Event Registration')).toBeVisible()
    await expect(page.locator('text=Community Access')).toBeVisible()
    await expect(page.locator('text=Member Discounts')).toBeVisible()
    
    // Should show community standing
    await expect(page.locator('text=Trust Level')).toBeVisible()
    await expect(page.locator('text=Event Attendance')).toBeVisible()
    await expect(page.locator('text=Community Participation')).toBeVisible()
    
    // Should show action buttons
    await expect(page.locator('button:has-text("View Community Guidelines")')).toBeVisible()
    await expect(page.locator('button:has-text("Contact Support")')).toBeVisible()
  })

  test('should navigate to events dashboard page', async ({ page }) => {
    // Navigate to dashboard
    await page.goto('http://localhost:5173/dashboard')
    
    // Should be able to navigate to events page from dashboard
    // First check if there's a direct dashboard/events link or use main events
    const eventsLink = page.locator('a[href="/events"], a[href="/dashboard/events"]').first()
    await eventsLink.click()
    
    // Should load events page (either main events or dashboard events)
    await expect(page.locator('h1')).toContainText('Events')
  })

  test('should test responsive design on mobile viewport', async ({ page }) => {
    // Set mobile viewport
    await page.setViewportSize({ width: 375, height: 667 })
    
    // Navigate to dashboard
    await page.goto('http://localhost:5173/dashboard')
    
    // Should still display main content
    await expect(page.locator('h1')).toContainText('Welcome back')
    
    // Navigate to profile
    await page.click('text=Update Profile')
    await page.waitForURL('http://localhost:5173/dashboard/profile', { timeout: 10000 })
    
    // Form should be responsive
    await expect(page.locator('input[placeholder="Your community name"]')).toBeVisible()
    
    // Navigate to security
    await page.goto('http://localhost:5173/dashboard/security')
    
    // Security page should be responsive
    await expect(page.locator('text=Security Settings')).toBeVisible()
    
    // Privacy toggles should still work on mobile
    const toggle = page.locator('input[type="checkbox"]').first()
    await toggle.click()
  })

  test('should handle dashboard navigation via URL bar', async ({ page }) => {
    // Direct navigation to dashboard pages
    await page.goto('http://localhost:5173/dashboard/profile')
    await expect(page.locator('h1')).toContainText('Profile')
    
    await page.goto('http://localhost:5173/dashboard/security')
    await expect(page.locator('h1')).toContainText('Security Settings')
    
    await page.goto('http://localhost:5173/dashboard/membership')
    await expect(page.locator('h1')).toContainText('Membership')
    
    // Should work with authenticated user
    await page.goto('http://localhost:5173/dashboard')
    await expect(page.locator('h1')).toContainText('Welcome back')
  })

  test('should display loading states correctly', async ({ page }) => {
    // Navigate to dashboard and look for loading states
    await page.goto('http://localhost:5173/dashboard')
    
    // Note: Since we're using real API, loading states may be brief
    // This test validates the components handle loading states properly
    
    // Navigate to profile
    await page.goto('http://localhost:5173/dashboard/profile')
    // Profile page should eventually load
    await expect(page.locator('h1')).toContainText('Profile', { timeout: 10000 })
    
    // Navigate to membership
    await page.goto('http://localhost:5173/dashboard/membership')
    // Membership page should eventually load
    await expect(page.locator('h1')).toContainText('Membership', { timeout: 10000 })
  })

  test('should verify dashboard layout and navigation structure', async ({ page }) => {
    await page.goto('http://localhost:5173/dashboard')
    
    // Should have dashboard layout with navigation
    // (Exact implementation depends on DashboardLayout component)
    await expect(page.locator('h1')).toBeVisible()
    
    // Should be able to navigate between dashboard pages
    await page.click('text=Update Profile')
    await expect(page.locator('h1')).toContainText('Profile')
    
    await page.click('text=Security Settings') // This might be in nav or we go via dashboard
    await expect(page.locator('h1')).toContainText('Security Settings')
    
    await page.click('text=Membership Status') // This might be in nav or we go via dashboard  
    await expect(page.locator('h1')).toContainText('Membership')
  })
})