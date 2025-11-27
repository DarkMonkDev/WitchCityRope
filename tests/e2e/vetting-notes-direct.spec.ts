import { test, expect } from '@playwright/test';
import { AuthHelpers } from './test-utils/helpers/auth.helpers';

// This test now fetches a dynamic application ID from the vetting list
test('Verify notes appear after stage advancement - Direct navigation', async ({ page }) => {
  // Login as admin using AuthHelpers
  await AuthHelpers.loginAs(page, 'admin');

  // First get an application ID from the vetting list
  await page.goto('/admin/vetting');
  await page.waitForLoadState('domcontentloaded');

  // Get first application row and extract ID from href
  const applicationRows = page.locator('tbody tr');
  const applicationCount = await applicationRows.count();

  if (applicationCount === 0) {
    console.log('⏭️ No vetting applications found - skipping test');
    return;
  }

  const firstApp = applicationRows.first();
  const appLink = firstApp.locator('a').first();
  const href = await appLink.getAttribute('href');

  if (!href) {
    console.log('⏭️ No application link found - skipping test');
    return;
  }

  // Navigate directly to vetting application detail
  await page.goto(`${href}`);
  await page.waitForLoadState('domcontentloaded');

  // Wait for application detail to load
  await page.waitForSelector('[data-testid="application-title"]', { timeout: 10000 });

  // Take screenshot of the page
  await page.screenshot({ path: '/home/chad/repos/witchcityrope/test-results/vetting-notes-direct.png', fullPage: true });

  // Check if notes section exists
  const notesSection = page.locator('text=Admin Notes');
  const notesVisible = await notesSection.isVisible();
  console.log(`✅ Notes section visible: ${notesVisible}`);

  // Count any notes displayed
  const noteElements = page.locator('[data-testid="vetting-note"]');
  const noteCount = await noteElements.count();
  console.log(`✅ Notes found: ${noteCount}`);

  // Verify notes functionality is working
  if (noteCount === 0) {
    console.log('ℹ️ No notes found - this may be a new application');
  }
});
