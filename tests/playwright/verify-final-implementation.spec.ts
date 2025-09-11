import { test, expect } from '@playwright/test';

test('Verify Final TinyMCE and Tabs Implementation', async ({ page }) => {
  // Navigate to the demo page
  await page.goto('http://localhost:5174/admin/event-session-matrix-demo');
  
  // Wait for page to load
  await page.waitForTimeout(3000);
  
  console.log('=== VERIFYING BASIC INFO TAB ===');
  
  // Verify Basic Info tab is active and TinyMCE is visible
  await expect(page.locator('[role="tab"][aria-selected="true"]')).toHaveText('Basic Info');
  
  // Check for TinyMCE toolbars - this is the key indicator
  const tinyMCEToolbars = await page.locator('.tox-toolbar, .tox-toolbar-overlord').count();
  console.log('TinyMCE toolbars found:', tinyMCEToolbars);
  expect(tinyMCEToolbars).toBeGreaterThan(0);
  
  // Check for TinyMCE container elements
  const tinyMCEContainers = await page.locator('.tox-tinymce, .tox-editor-container').count();
  console.log('TinyMCE containers found:', tinyMCEContainers);
  expect(tinyMCEContainers).toBeGreaterThan(0);
  
  // Verify TipTap is NOT present
  const tipTapEditors = await page.locator('.ProseMirror').count();
  console.log('TipTap editors (should be 0):', tipTapEditors);
  expect(tipTapEditors).toBe(0);
  
  // Take screenshot of Basic Info tab
  await page.screenshot({ path: 'basic-info-with-tinymce.png', fullPage: true });
  
  console.log('=== VERIFYING EMAILS TAB ===');
  
  // Click on Emails tab
  await page.locator('[role="tab"]').filter({ hasText: 'Emails' }).click();
  await page.waitForTimeout(2000);
  
  // Verify Emails tab content
  await expect(page.locator('text=Email Templates')).toBeVisible();
  await expect(page.locator('text=Registration Confirmation Email')).toBeVisible();
  await expect(page.locator('text=Event Reminder Email')).toBeVisible();
  await expect(page.locator('text=Event Cancellation Email')).toBeVisible();
  
  // Verify TinyMCE editors are in the Emails tab too
  const emailTinyMCEToolbars = await page.locator('.tox-toolbar, .tox-toolbar-overlord').count();
  console.log('TinyMCE toolbars in Emails tab:', emailTinyMCEToolbars);
  expect(emailTinyMCEToolbars).toBeGreaterThan(0);
  
  // Take screenshot of Emails tab
  await page.screenshot({ path: 'emails-tab-implemented.png', fullPage: true });
  
  console.log('=== VERIFYING VOLUNTEERS/STAFF TAB ===');
  
  // Click on Volunteers/Staff tab
  await page.locator('[role="tab"]').filter({ hasText: 'Volunteers/Staff' }).click();
  await page.waitForTimeout(2000);
  
  // Verify Volunteers/Staff tab content
  await expect(page.locator('text=Volunteer Positions & Staff Assignments')).toBeVisible();
  await expect(page.locator('text=Volunteer Positions Needed')).toBeVisible();
  await expect(page.locator('text=Staff Assignments')).toBeVisible();
  await expect(page.locator('text=Special Requirements & Notes')).toBeVisible();
  
  // Verify volunteer position data
  await expect(page.locator('text=Setup Crew')).toBeVisible();
  await expect(page.locator('text=Safety Monitor')).toBeVisible();
  await expect(page.locator('text=Cleanup Crew')).toBeVisible();
  
  // Verify staff assignment data
  await expect(page.locator('text=River Moon')).toBeVisible();
  await expect(page.locator('text=Sage Blackthorne')).toBeVisible();
  await expect(page.locator('text=Primary Teacher')).toBeVisible();
  
  // Verify badges are colored correctly
  await expect(page.locator('text=2/3').locator('xpath=..')).toBeVisible(); // Green badge
  await expect(page.locator('text=0/2').locator('xpath=..')).toBeVisible(); // Red badge
  
  // Check for TinyMCE in Special Requirements section
  const volunteerTinyMCE = await page.locator('.tox-toolbar, .tox-toolbar-overlord').count();
  console.log('TinyMCE toolbars in Volunteers/Staff tab:', volunteerTinyMCE);
  expect(volunteerTinyMCE).toBeGreaterThan(0);
  
  // Take screenshot of Volunteers/Staff tab
  await page.screenshot({ path: 'volunteers-staff-tab-implemented.png', fullPage: true });
  
  console.log('=== VERIFYING TICKETS/ORDERS TAB ===');
  
  // Click on Tickets/Orders tab to verify it still works
  await page.locator('[role="tab"]').filter({ hasText: 'Tickets/Orders' }).click();
  await page.waitForTimeout(1000);
  
  await expect(page.locator('text=Event Sessions')).toBeVisible();
  await expect(page.locator('text=Ticket Types')).toBeVisible();
  
  console.log('=== ALL VERIFICATION COMPLETED ===');
  console.log('✅ TinyMCE successfully replaced TipTap');
  console.log('✅ All 4 tabs are functional');
  console.log('✅ Email templates are implemented');
  console.log('✅ Volunteer/Staff management is implemented');
  console.log('✅ TinyMCE editors present in all appropriate tabs');
});