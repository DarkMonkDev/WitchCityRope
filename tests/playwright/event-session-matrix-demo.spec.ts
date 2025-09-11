import { test, expect } from '@playwright/test';

test.describe('Event Session Matrix Demo', () => {
  test.beforeEach(async ({ page }) => {
    // Navigate to the demo page
    await page.goto('/admin/event-session-matrix-demo');
    
    // Wait for the page to load
    await page.waitForLoadState('networkidle');
  });

  test('should display the demo page with correct title', async ({ page }) => {
    // Check page title
    await expect(page).toHaveTitle(/Vite \+ React \+ TS/);
    
    // Check the main heading
    await expect(page.locator('h1')).toContainText('Event Session Matrix Demo');
  });

  test('should display all four tabs', async ({ page }) => {
    // Check all tabs are present
    await expect(page.locator('[role="tab"]')).toHaveCount(4);
    
    // Check tab names
    await expect(page.locator('[role="tab"]:nth-child(1)')).toContainText('Basic Info');
    await expect(page.locator('[role="tab"]:nth-child(2)')).toContainText('Tickets/Orders');
    await expect(page.locator('[role="tab"]:nth-child(3)')).toContainText('Emails');
    await expect(page.locator('[role="tab"]:nth-child(4)')).toContainText('Volunteers/Staff');
  });

  test('should load with Basic Info tab active', async ({ page }) => {
    // Check that Basic Info tab is active by default
    await expect(page.locator('[role="tab"][aria-selected="true"]')).toContainText('Basic Info');
    
    // Check that event type toggles are visible
    await expect(page.locator('text=Class')).toBeVisible();
    await expect(page.locator('text=Social Event')).toBeVisible();
  });

  test('should have TinyMCE editors instead of TipTap', async ({ page }) => {
    // Check that TinyMCE iframes are present (TinyMCE creates iframes)
    const iframes = page.locator('iframe[id^="tiny-react"]');
    
    // Should have at least 2 TinyMCE editors (Full Description and Policies)
    await expect(iframes).toHaveCount(2);
    
    // Verify no TipTap elements are present
    await expect(page.locator('.ProseMirror')).toHaveCount(0);
    await expect(page.locator('[data-tiptap-editor]')).toHaveCount(0);
  });

  test('should switch between tabs correctly', async ({ page }) => {
    // Click on Tickets/Orders tab
    await page.locator('[role="tab"]:has-text("Tickets/Orders")').click();
    await expect(page.locator('[role="tab"][aria-selected="true"]')).toContainText('Tickets/Orders');
    await expect(page.locator('h2:has-text("Event Sessions")')).toBeVisible();
    
    // Click on Emails tab
    await page.locator('[role="tab"]:has-text("Emails")').click();
    await expect(page.locator('[role="tab"][aria-selected="true"]')).toContainText('Emails');
    await expect(page.locator('h2:has-text("Email Templates")')).toBeVisible();
    await expect(page.locator('text=Registration Confirmation')).toBeVisible();
    
    // Click on Volunteers/Staff tab
    await page.locator('[role="tab"]:has-text("Volunteers/Staff")').click();
    await expect(page.locator('[role="tab"][aria-selected="true"]')).toContainText('Volunteers/Staff');
    await expect(page.locator('h2:has-text("Volunteer Positions")')).toBeVisible();
    await expect(page.locator('text=Add Volunteer Position')).toBeVisible();
  });

  test('should display event sessions grid in Tickets/Orders tab', async ({ page }) => {
    // Go to Tickets/Orders tab
    await page.locator('[role="tab"]:has-text("Tickets/Orders")').click();
    
    // Check for session data
    await expect(page.locator('text=S1')).toBeVisible();
    await expect(page.locator('text=S2')).toBeVisible();  
    await expect(page.locator('text=S3')).toBeVisible();
    await expect(page.locator('text=Fundamentals Day')).toBeVisible();
  });

  test('should display ticket types grid in Tickets/Orders tab', async ({ page }) => {
    // Go to Tickets/Orders tab
    await page.locator('[role="tab"]:has-text("Tickets/Orders")').click();
    
    // Check for ticket type data
    await expect(page.locator('text=Full 3-Day Series Pass')).toBeVisible();
    await expect(page.locator('text=Day 1: Fundamentals Only')).toBeVisible();
    await expect(page.locator('text=Weekend Pass (Days 2-3)')).toBeVisible();
  });

  test('should display email templates in Emails tab', async ({ page }) => {
    // Go to Emails tab
    await page.locator('[role="tab"]:has-text("Emails")').click();
    
    // Check for email templates
    await expect(page.locator('text=Registration Confirmation')).toBeVisible();
    await expect(page.locator('text=Event Reminder')).toBeVisible();
    await expect(page.locator('text=Cancellation Notice')).toBeVisible();
    await expect(page.locator('text=Send Custom Email to Registrants')).toBeVisible();
    
    // Check for Preview/Edit buttons
    await expect(page.locator('button:has-text("Preview")')).toHaveCount(3);
    await expect(page.locator('button:has-text("Edit")')).toHaveCount(3);
  });

  test('should display volunteer positions in Volunteers/Staff tab', async ({ page }) => {
    // Go to Volunteers/Staff tab
    await page.locator('[role="tab"]:has-text("Volunteers/Staff")').click();
    
    // Check for add volunteer position form
    await expect(page.locator('text=Add Volunteer Position')).toBeVisible();
    await expect(page.locator('input[placeholder*="Setup Assistant"]')).toBeVisible();
    await expect(page.locator('button:has-text("Add Position")')).toBeVisible();
    
    // Check for existing positions
    await expect(page.locator('text=Setup Assistant')).toBeVisible();
    await expect(page.locator('text=Greeter')).toBeVisible(); 
    await expect(page.locator('text=Cleanup Helper')).toBeVisible();
    
    // Check for Manage/Edit buttons
    await expect(page.locator('button:has-text("Manage")')).toHaveCount(3);
    await expect(page.locator('button:has-text("Edit")')).toHaveCount(3);
  });

  test('should have functional form controls', async ({ page }) => {
    // Test event type selection
    await page.locator('text=Social Event').click();
    // Social event card should be selected (this would change styling)
    
    // Test text input
    await page.locator('input[placeholder="Enter event title"]').fill('Test Event');
    await expect(page.locator('input[placeholder="Enter event title"]')).toHaveValue('Test Event');
    
    // Test venue selection
    await page.locator('input[placeholder="Select venue..."]').click();
    await expect(page.locator('text=Main Studio')).toBeVisible();
  });

  test('should have working form submission', async ({ page }) => {
    // Fill out required fields
    await page.locator('input[placeholder="Enter event title"]').fill('Test Event Title');
    await page.locator('input[placeholder="Brief description for cards and grid views"]').fill('Test description');
    
    // Select venue
    await page.locator('input[placeholder="Select venue..."]').click();
    await page.locator('text=Main Studio').click();
    
    // Click Save Draft button
    await page.locator('button:has-text("Save Draft")').click();
    
    // Should show loading state
    await expect(page.locator('button:has-text("Saving...")')).toBeVisible();
    
    // Wait for success notification (from mock implementation)
    await expect(page.locator('text=Event session matrix data saved successfully')).toBeVisible({ timeout: 5000 });
  });

  test('should handle cancel action', async ({ page }) => {
    // Click Cancel button
    await page.locator('button:has-text("Cancel")').click();
    
    // Should show cancellation message
    await expect(page.locator('text=Form Cancelled')).toBeVisible();
    await expect(page.locator('button:has-text("Show Form Again")')).toBeVisible();
    
    // Click to show form again
    await page.locator('button:has-text("Show Form Again")').click();
    await expect(page.locator('h1:has-text("Event Session Matrix Demo")')).toBeVisible();
  });
});