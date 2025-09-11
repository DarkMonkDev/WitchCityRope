import { test, expect } from '@playwright/test';

test('Screenshot current Event Session Matrix Demo page', async ({ page }) => {
  // Navigate to the demo page
  await page.goto('http://localhost:5174/admin/event-session-matrix-demo');
  
  // Wait for page to load
  await page.waitForTimeout(2000);
  
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
  
  // List all visible text to understand current state
  const allText = await page.locator('body').textContent();
  console.log('Page content includes:', allText?.substring(0, 500));
});