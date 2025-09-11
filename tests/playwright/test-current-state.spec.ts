import { test, expect } from '@playwright/test';

test('Screenshot current Event Session Matrix Demo page', async ({ page }) => {
  // Navigate to the demo page
  await page.goto('http://localhost:5174/admin/event-session-matrix-demo');
  
  // Wait for page to load
  await page.waitForTimeout(3000);
  
  // Take a screenshot of the current state
  await page.screenshot({ 
    path: 'current-state-before-changes.png', 
    fullPage: true 
  });
  
  // Check what tabs are visible
  const tabs = await page.locator('[role="tab"]').count();
  console.log('Number of tabs visible:', tabs);
  
  // Check for TipTap vs TinyMCE
  const tipTapEditors = await page.locator('.ProseMirror').count();
  const tinyMCEEditors = await page.locator('.tox-tinymce').count();
  
  console.log('TipTap editors found:', tipTapEditors);
  console.log('TinyMCE editors found:', tinyMCEEditors);
  
  // List all visible tabs
  const tabTexts = await page.locator('[role="tab"]').allTextContents();
  console.log('Tab texts:', tabTexts);
  
  // Check for specific tab content
  const emailsTabExists = await page.locator('text=Emails').count();
  const volunteersTabExists = await page.locator('text=Volunteers').count();
  const staffTabExists = await page.locator('text=Staff').count();
  
  console.log('Emails tab exists:', emailsTabExists > 0);
  console.log('Volunteers/Staff tab exists:', volunteersTabExists > 0 || staffTabExists > 0);
});