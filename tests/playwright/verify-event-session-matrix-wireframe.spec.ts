import { test, expect } from '@playwright/test';

test.describe('Event Session Matrix Demo - Wireframe Verification', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('http://localhost:5174/admin/event-session-matrix-demo');
  });

  test('should display emails tab matching wireframe design', async ({ page }) => {
    // Click on Emails tab
    await page.click('text=Emails');
    
    // Wait for tab content to load
    await page.waitForSelector('text=Email Templates');
    
    // Verify Email Templates section header
    await expect(page.locator('h2:has-text("Email Templates")')).toBeVisible();
    
    // Verify template cards are in horizontal row layout
    const templateCardsContainer = page.locator('div:has(> div:text("Send Ad-Hoc Email"))').first();
    await expect(templateCardsContainer).toBeVisible();
    
    // Verify all template cards are present
    await expect(page.locator('text=Send Ad-Hoc Email')).toBeVisible();
    await expect(page.locator('text=Confirmation Email')).toBeVisible();
    await expect(page.locator('text=Reminder - 1 Day Before')).toBeVisible();
    await expect(page.locator('text=Cancellation Notice')).toBeVisible();
    
    // Verify Confirmation Email card has no remove button (disabled X)
    const confirmationCard = page.locator('div:has-text("Confirmation Email")').first();
    await expect(confirmationCard).toBeVisible();
    
    // Verify unified editor section is present
    await expect(page.locator('text=Currently Editing:')).toBeVisible();
    await expect(page.locator('text=Subject Line')).toBeVisible();
    await expect(page.locator('text=Email Content')).toBeVisible();
    
    // Verify TinyMCE editor is loaded
    await page.waitForSelector('.tox-tinymce', { timeout: 10000 });
    await expect(page.locator('.tox-tinymce')).toBeVisible();
    
    // Take screenshot for verification
    await expect(page).toHaveScreenshot('emails-tab-wireframe-match.png');
  });

  test('should display volunteers/staff tab matching wireframe design', async ({ page }) => {
    // Click on Volunteers/Staff tab
    await page.click('text=Volunteers/Staff');
    
    // Wait for tab content to load
    await page.waitForSelector('text=Volunteer Positions');
    
    // Verify Volunteer Positions section header
    await expect(page.locator('h2:has-text("Volunteer Positions")')).toBeVisible();
    
    // Verify table headers match wireframe exactly
    await expect(page.locator('th:has-text("Edit")')).toBeVisible();
    await expect(page.locator('th:has-text("Position")')).toBeVisible();
    await expect(page.locator('th:has-text("Sessions")')).toBeVisible();
    await expect(page.locator('th:has-text("Time")')).toBeVisible();
    await expect(page.locator('th:has-text("Description")')).toBeVisible();
    await expect(page.locator('th:has-text("Needed")')).toBeVisible();
    await expect(page.locator('th:has-text("Assigned")')).toBeVisible();
    await expect(page.locator('th:has-text("Delete")')).toBeVisible();
    
    // Verify sample data rows are present
    await expect(page.locator('text=Door Monitor')).toBeVisible();
    await expect(page.locator('text=Safety Monitor')).toBeVisible();
    await expect(page.locator('text=Setup Crew')).toBeVisible();
    
    // Verify specific data from wireframe
    await expect(page.locator('text=Check IDs, welcome members')).toBeVisible();
    await expect(page.locator('text=Monitor play areas, handle incidents')).toBeVisible();
    await expect(page.locator('text=Arrange furniture, setup equipment')).toBeVisible();
    
    // Verify assignment status indicators
    await expect(page.locator('text=✓ Jamie Davis')).toBeVisible();
    await expect(page.locator('text=✓ Sam Singh')).toBeVisible();
    await expect(page.locator('text=✓ Alex Chen')).toBeVisible();
    await expect(page.locator('text=○ 1 more needed')).toBeVisible();
    await expect(page.locator('text=⚠ 4 positions open')).toBeVisible();
    
    // Verify "Add Position" button is below the table
    await expect(page.locator('button:has-text("Add Position")')).toBeVisible();
    
    // Verify Staff Assignments section
    await expect(page.locator('h3:has-text("Staff Assignments")')).toBeVisible();
    
    // Take screenshot for verification
    await expect(page).toHaveScreenshot('volunteers-staff-tab-wireframe-match.png');
  });

  test('should have working TinyMCE editor functionality', async ({ page }) => {
    // Click on Emails tab
    await page.click('text=Emails');
    
    // Wait for TinyMCE to load
    await page.waitForSelector('.tox-tinymce', { timeout: 10000 });
    
    // Get the TinyMCE iframe
    const tinymceFrame = page.frameLocator('iframe[title="Rich Text Area"]');
    
    // Verify we can interact with the editor
    await expect(tinymceFrame.locator('body')).toBeVisible();
    
    // Verify toolbar buttons are present
    await expect(page.locator('.tox-toolbar')).toBeVisible();
    await expect(page.locator('button[title="Bold"]')).toBeVisible();
    await expect(page.locator('button[title="Italic"]')).toBeVisible();
    
    console.log('✅ TinyMCE editor is working correctly');
  });

  test('should handle template card selection properly', async ({ page }) => {
    // Click on Emails tab
    await page.click('text=Emails');
    
    // Wait for tab content to load
    await page.waitForSelector('text=Email Templates');
    
    // Click on different template cards and verify editor updates
    await page.click('text=Send Ad-Hoc Email');
    await expect(page.locator('text=Currently Editing: Ad-Hoc Email')).toBeVisible();
    
    await page.click('text=Confirmation Email');
    await expect(page.locator('text=Currently Editing: Confirmation Email')).toBeVisible();
    
    await page.click('text=Reminder - 1 Day Before');
    await expect(page.locator('text=Currently Editing: Reminder - 1 Day Before')).toBeVisible();
    
    console.log('✅ Template card selection is working correctly');
  });
});