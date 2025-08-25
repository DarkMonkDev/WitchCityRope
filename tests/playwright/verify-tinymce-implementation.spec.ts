import { test, expect } from '@playwright/test';

test('Verify TinyMCE Implementation and Tab Functionality', async ({ page }) => {
  // Navigate to the demo page
  await page.goto('http://localhost:5174/admin/event-session-matrix-demo');
  
  // Wait for page to fully load
  await page.waitForTimeout(3000);
  
  console.log('=== TESTING BASIC INFO TAB ===');
  
  // Verify Basic Info tab is active by default
  await expect(page.locator('[role="tab"][aria-selected="true"]')).toHaveText('Basic Info');
  
  // Check for TinyMCE editors (should find TinyMCE, not TipTap)
  const tinyMCEEditors = await page.locator('iframe[id*="tinymce"]').count();
  const tipTapEditors = await page.locator('.ProseMirror').count();
  
  console.log('TinyMCE editors found:', tinyMCEEditors);
  console.log('TipTap editors found:', tipTapEditors);
  
  // TinyMCE should be present, TipTap should not be
  expect(tinyMCEEditors).toBeGreaterThan(0);
  expect(tipTapEditors).toBe(0);
  
  console.log('=== TESTING EMAILS TAB ===');
  
  // Click on Emails tab
  await page.locator('[role="tab"]').filter({ hasText: 'Emails' }).click();
  await page.waitForTimeout(1000);
  
  // Verify Emails tab content
  await expect(page.locator('text=Email Templates')).toBeVisible();
  await expect(page.locator('text=Registration Confirmation Email')).toBeVisible();
  await expect(page.locator('text=Event Reminder Email')).toBeVisible();
  await expect(page.locator('text=Event Cancellation Email')).toBeVisible();
  
  // Check for TinyMCE editors in Email tab
  const emailTinyMCEEditors = await page.locator('iframe[id*="tinymce"]').count();
  console.log('TinyMCE editors in Emails tab:', emailTinyMCEEditors);
  expect(emailTinyMCEEditors).toBeGreaterThan(0);
  
  console.log('=== TESTING VOLUNTEERS/STAFF TAB ===');
  
  // Click on Volunteers/Staff tab
  await page.locator('[role="tab"]').filter({ hasText: 'Volunteers/Staff' }).click();
  await page.waitForTimeout(1000);
  
  // Verify Volunteers/Staff tab content
  await expect(page.locator('text=Volunteer Positions & Staff Assignments')).toBeVisible();
  await expect(page.locator('text=Volunteer Positions Needed')).toBeVisible();
  await expect(page.locator('text=Staff Assignments')).toBeVisible();
  await expect(page.locator('text=Special Requirements & Notes')).toBeVisible();
  
  // Check for volunteer positions table
  await expect(page.locator('text=Setup Crew')).toBeVisible();
  await expect(page.locator('text=Safety Monitor')).toBeVisible();
  await expect(page.locator('text=Cleanup Crew')).toBeVisible();
  
  // Check for staff assignments
  await expect(page.locator('text=River Moon')).toBeVisible();
  await expect(page.locator('text=Sage Blackthorne')).toBeVisible();
  await expect(page.locator('text=Primary Teacher')).toBeVisible();
  
  console.log('=== TESTING TICKETS/ORDERS TAB ===');
  
  // Click on Tickets/Orders tab to verify it still works
  await page.locator('[role="tab"]').filter({ hasText: 'Tickets/Orders' }).click();
  await page.waitForTimeout(1000);
  
  await expect(page.locator('text=Event Sessions')).toBeVisible();
  await expect(page.locator('text=Ticket Types')).toBeVisible();
  
  // Take screenshot of final state
  await page.screenshot({ 
    path: 'after-tinymce-implementation.png', 
    fullPage: true 
  });
  
  console.log('=== ALL TESTS COMPLETED ===');
  console.log('✅ TinyMCE successfully replaced TipTap');
  console.log('✅ All tabs are functional');
  console.log('✅ Email templates implemented');
  console.log('✅ Volunteer/Staff management implemented');
});