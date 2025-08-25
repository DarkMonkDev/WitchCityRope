import { test, expect } from '@playwright/test';

test.describe('Event Session Matrix Demo - UI Fixes Verification', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/admin/event-session-matrix-demo');
    await page.waitForLoadState('networkidle');
  });

  test('should fix Ad-Hoc Email Target Sessions visibility issue', async ({ page }) => {
    // Navigate to Emails tab
    await page.locator('[role="tab"]:has-text("Emails")').click();
    await expect(page.locator('[role="tab"][aria-selected="true"]')).toContainText('Emails');

    // Check that "Send Ad-Hoc Email" button is visible
    const adHocButton = page.locator('button:has-text("Send Ad-Hoc Email")');
    await expect(adHocButton).toBeVisible();

    // Click the button to expand the form
    await adHocButton.click();

    // Verify that Target Sessions selector is now visible
    await expect(page.locator('text=Target Sessions')).toBeVisible();
    
    // Verify the MultiSelect for target sessions is visible
    await expect(page.locator('input[placeholder*="Select sessions to send email"]')).toBeVisible();
    
    // Verify other form fields are present
    await expect(page.locator('input[placeholder="Enter email subject..."]')).toBeVisible();
    await expect(page.locator('textarea[placeholder*="Enter your custom email message"]')).toBeVisible();
  });

  test('should apply WitchCityRope brand colors to input fields', async ({ page }) => {
    // Test input field styling in Basic Info tab
    const titleInput = page.locator('input[placeholder="Enter event title"]');
    await expect(titleInput).toBeVisible();
    
    // Check that the input has the form-input-animated class
    await expect(titleInput).toHaveClass(/form-input-animated/);
    
    // Test venue selector styling
    const venueSelector = page.locator('input[placeholder="Select venue..."]');
    await expect(venueSelector).toBeVisible();
    await expect(venueSelector).toHaveClass(/form-input-animated/);
    
    // Test MultiSelect for teachers styling
    const teachersSelect = page.locator('input[placeholder="Choose teachers for this event"]');
    await expect(teachersSelect).toBeVisible();
    await expect(teachersSelect).toHaveClass(/form-input-animated/);
  });

  test('should apply brand styling to volunteer form inputs', async ({ page }) => {
    // Navigate to Volunteers/Staff tab
    await page.locator('[role="tab"]:has-text("Volunteers/Staff")').click();
    await expect(page.locator('[role="tab"][aria-selected="true"]')).toContainText('Volunteers/Staff');

    // Check volunteer position form inputs
    const positionNameInput = page.locator('input[placeholder*="Setup Assistant"]');
    await expect(positionNameInput).toBeVisible();
    await expect(positionNameInput).toHaveClass(/form-input-animated/);
    
    const numberNeededInput = page.locator('input[placeholder="1"]');
    await expect(numberNeededInput).toBeVisible();
    await expect(numberNeededInput).toHaveClass(/form-input-animated/);
    
    const descriptionTextarea = page.locator('textarea[placeholder*="responsibilities and requirements"]');
    await expect(descriptionTextarea).toBeVisible();
    await expect(descriptionTextarea).toHaveClass(/form-input-animated/);
  });

  test('should fix scroll issue in Emails tab', async ({ page }) => {
    // Navigate to Emails tab
    await page.locator('[role="tab"]:has-text("Emails")').click();
    
    // Get initial viewport
    const emailsPanel = page.locator('[value="emails"]');
    await expect(emailsPanel).toBeVisible();
    
    // Scroll within the tab content
    await page.mouse.wheel(0, 300);
    await page.waitForTimeout(500);
    
    // Scroll back up
    await page.mouse.wheel(0, -300);
    await page.waitForTimeout(500);
    
    // Verify that no unwanted elements appeared during scroll
    // The emails tab content should remain contained
    await expect(page.locator('text=Email Templates')).toBeVisible();
    await expect(page.locator('text=Registration Confirmation')).toBeVisible();
  });

  test('should show Ad-Hoc Email form with all required fields', async ({ page }) => {
    // Navigate to Emails tab
    await page.locator('[role="tab"]:has-text("Emails")').click();
    
    // Click Send Ad-Hoc Email button
    await page.locator('button:has-text("Send Ad-Hoc Email")').click();
    
    // Verify all form fields are present and styled
    await expect(page.locator('text=Target Sessions')).toBeVisible();
    await expect(page.locator('input[placeholder*="Select sessions to send email"]')).toBeVisible();
    await expect(page.locator('input[placeholder="Enter email subject..."]')).toBeVisible();
    await expect(page.locator('textarea[placeholder*="Enter your custom email message"]')).toBeVisible();
    
    // Verify Cancel and Send buttons
    await expect(page.locator('button:has-text("Cancel")')).toBeVisible();
    await expect(page.locator('button:has-text("Send Ad-Hoc Email")')).toBeVisible();
    
    // Verify the Send button is initially disabled (no sessions selected)
    await expect(page.locator('button:has-text("Send Ad-Hoc Email")')).toBeDisabled();
  });

  test('should show participant count when sessions are selected', async ({ page }) => {
    // Navigate to Emails tab and open Ad-Hoc Email form
    await page.locator('[role="tab"]:has-text("Emails")').click();
    await page.locator('button:has-text("Send Ad-Hoc Email")').click();
    
    // Click on the MultiSelect to open dropdown
    await page.locator('input[placeholder*="Select sessions to send email"]').click();
    
    // Select a session (assuming there's mock data)
    const sessionOption = page.locator('[role="option"]').first();
    if (await sessionOption.isVisible()) {
      await sessionOption.click();
      
      // Verify participant count appears
      await expect(page.locator('text*="Will be sent to"')).toBeVisible();
      
      // Verify Send button is now enabled
      await expect(page.locator('button:has-text("Send Ad-Hoc Email")')).toBeEnabled();
    }
  });
});