import { test, expect } from '@playwright/test';
import { AuthHelpers } from './helpers/auth.helpers';

// SKIP: This test requires specific test data (user "TestUser1759967282014") that doesn't exist
// in the database. This was a diagnostic test for a specific scenario (notes after stage advancement).
// TODO: Either create test data fixture or rewrite to use dynamic data from existing applications
test.skip('Verify notes appear after stage advancement', async ({ page }) => {
  // Login as admin using AuthHelpers
  await AuthHelpers.loginAs(page, 'admin');

  // Navigate to vetting page
  await page.goto('http://localhost:5173/admin/vetting');
  await page.waitForLoadState('networkidle');

  // Find and click on the test application
  const testApp = page.locator('tr:has-text("TestUser1759967282014")').first();
  await testApp.click();
  await page.waitForLoadState('networkidle');

  // Wait for application detail to load
  await page.waitForSelector('[data-testid="application-title"]', { timeout: 10000 });

  // Take screenshot of full page
  await page.screenshot({ path: '/home/chad/repos/witchcityrope/test-results/vetting-notes-debug.png', fullPage: true });

  // Check if notes section exists
  const notesSection = page.locator('text=Admin Notes');
  await expect(notesSection).toBeVisible();

  // Count notes displayed - look for reviewer names
  const notes = page.locator('text=RopeMaster');
  const noteCount = await notes.count();
  console.log(`Found ${noteCount} notes on the page`);

  // Check for specific note content
  const approvalNote = page.locator('text=/approved for interview/i');
  const approvalExists = await approvalNote.count();
  console.log(`Approval notes found: ${approvalExists}`);

  // Output to console
  console.log(`✅ Notes section visible: ${await notesSection.isVisible()}`);
  console.log(`✅ Note count on page: ${noteCount}`);
});
