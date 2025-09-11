import { test, expect } from '@playwright/test';

test.describe('EventForm Visual Verification', () => {
  test.beforeEach(async ({ page }) => {
    // Navigate to the event form test page
    await page.goto('http://localhost:5174/event-form-test');
  });

  test('EventForm matches wireframe - Basic Info tab', async ({ page }) => {
    // Wait for the test page to load - use first h1 specifically
    await expect(page.locator('h1').first()).toContainText('Event Form - Wireframe Test');
    
    // Look for the EventForm component
    await expect(page.locator('[data-testid="event-form"]')).toBeVisible();
    
    // Verify the Basic Info tab is active by default
    await expect(page.locator('.mantine-Tabs-tab[data-active="true"]')).toContainText('Basic Info');
    
    // Check for TinyMCE editors
    await expect(page.locator('.tox-tinymce')).toHaveCount(2); // Full Description and Policies
    
    // Verify button styling is correct
    const saveButton = page.locator('button:has-text("Save Draft")');
    await expect(saveButton).toBeVisible();
    
    // Take screenshot of Basic Info tab
    await page.screenshot({ path: 'test-results/event-form-basic-info.png', fullPage: true });
  });

  test('EventForm Emails tab with TinyMCE', async ({ page }) => {
    // Wait for the test page to load - use first h1 specifically
    await expect(page.locator('h1').first()).toContainText('Event Form - Wireframe Test');
    
    // Click on Emails tab
    await page.locator('.mantine-Tabs-tab:has-text("Emails")').click();
    
    // Wait for the tab to become active
    await expect(page.locator('.mantine-Tabs-tab[data-active="true"]')).toContainText('Emails');
    
    // Check for TinyMCE editor in emails tab
    await expect(page.locator('.tox-tinymce')).toBeVisible();
    
    // Verify buttons have proper sizing
    const saveChangesButton = page.locator('button:has-text("Save Changes")');
    const sendEmailButton = page.locator('button:has-text("Send Email")');
    const addTemplateButton = page.locator('button:has-text("Add Email Template")');
    
    await expect(saveChangesButton.or(sendEmailButton)).toBeVisible();
    await expect(addTemplateButton).toBeVisible();
    
    // Take screenshot of Emails tab
    await page.screenshot({ path: 'test-results/event-form-emails.png', fullPage: true });
  });

  test('EventForm Volunteers tab structure', async ({ page }) => {
    // Wait for the test page to load - use first h1 specifically
    await expect(page.locator('h1').first()).toContainText('Event Form - Wireframe Test');
    
    // Click on Volunteers tab
    await page.locator('.mantine-Tabs-tab:has-text("Volunteers")').click();
    
    // Wait for the tab to become active
    await expect(page.locator('.mantine-Tabs-tab[data-active="true"]')).toContainText('Volunteers');
    
    // Verify volunteer positions table is present
    await expect(page.locator('h2:has-text("Volunteer Positions")')).toBeVisible();
    await expect(page.locator('table')).toBeVisible();
    
    // Verify volunteer position management form is present
    await expect(page.locator('h3:has-text("Volunteer Position Management")')).toBeVisible();
    await expect(page.locator('h4:has-text("Add New Position")')).toBeVisible();
    
    // Verify Add Position button
    const addPositionButton = page.locator('button:has-text("Add Position")');
    await expect(addPositionButton).toBeVisible();
    
    // Take screenshot of Volunteers tab
    await page.screenshot({ path: 'test-results/event-form-volunteers.png', fullPage: true });
  });

  test('EventForm button text is not cut off', async ({ page }) => {
    // Wait for the test page to load - use first h1 specifically
    await expect(page.locator('h1').first()).toContainText('Event Form - Wireframe Test');
    
    // Check Basic Info tab buttons
    const saveButton = page.locator('button:has-text("Save Draft")');
    const cancelButton = page.locator('button:has-text("Cancel")');
    
    await expect(saveButton).toBeVisible();
    await expect(cancelButton).toBeVisible();
    
    // Check button dimensions
    const saveButtonBox = await saveButton.boundingBox();
    const cancelButtonBox = await cancelButton.boundingBox();
    
    expect(saveButtonBox?.height).toBeGreaterThanOrEqual(48); // Minimum height
    expect(cancelButtonBox?.height).toBeGreaterThanOrEqual(48);
    
    // Check Emails tab buttons
    await page.locator('.mantine-Tabs-tab:has-text("Emails")').click();
    
    const addTemplateButton = page.locator('button:has-text("Add Email Template")');
    await expect(addTemplateButton).toBeVisible();
    
    const addTemplateBox = await addTemplateButton.boundingBox();
    expect(addTemplateBox?.height).toBeGreaterThanOrEqual(48);
    expect(addTemplateBox?.width).toBeGreaterThanOrEqual(160);
    
    // Check Volunteers tab buttons
    await page.locator('.mantine-Tabs-tab:has-text("Volunteers")').click();
    
    const addPositionButton = page.locator('button:has-text("Add Position")');
    await expect(addPositionButton).toBeVisible();
    
    const addPositionBox = await addPositionButton.boundingBox();
    expect(addPositionBox?.height).toBeGreaterThanOrEqual(48);
    expect(addPositionBox?.width).toBeGreaterThanOrEqual(140);
  });
});