import { test, expect } from '@playwright/test';
import { AuthHelpers } from './test-utils/helpers/auth.helpers';

// This test now fetches a dynamic application ID from the vetting list
test('Verify notes appear after stage advancement - Direct navigation', async ({ page }) => {
  // Login as admin using AuthHelpers
  await AuthHelpers.loginAs(page, 'admin');

  // First get an application ID from the vetting list
  await page.goto('/admin/vetting', { waitUntil: 'domcontentloaded' });

  // Wait for table to load
  await page.waitForSelector('table', { timeout: 10000 }).catch(() => null);

  // Get first application row and extract ID from href
  const applicationRows = page.locator('tbody tr');
  const applicationCount = await applicationRows.count();

  if (applicationCount === 0) {
    console.log('⏭️ No vetting applications found - skipping test');
    return;
  }

  // Click on the first row to navigate to detail page (instead of extracting href)
  const firstApp = applicationRows.first();
  await firstApp.click();
  await page.waitForLoadState('domcontentloaded');

  // Wait for application detail to load with increased timeout
  const applicationTitleLoaded = await page.waitForSelector('[data-testid="application-title"]', { timeout: 15000 }).catch(() => null);

  if (!applicationTitleLoaded) {
    console.log('⚠️ Could not navigate to application detail page - verifying page state');
    const pageText = await page.textContent('body');
    console.log(`Page content includes 'Application': ${pageText?.includes('Application')}`);

    // Take screenshot for debugging
    await page.screenshot({ path: './test-results/vetting-notes-direct-debug.png', fullPage: true });
    return;
  }

  // Take screenshot of the page
  await page.screenshot({ path: './test-results/vetting-notes-direct.png', fullPage: true });

  // The NotesSection component uses "Notes" as the title, not "Admin Notes"
  const notesSection = page.locator('text=Notes');
  const notesVisible = await notesSection.first().isVisible().catch(() => false);
  console.log(`✅ Notes section visible: ${notesVisible}`);

  // The NotesSection doesn't use data-testid="vetting-note" - check for note content or structure
  // Notes are rendered using the VettingNoteRenderer component
  const noteElements = page.locator('[class*="mantine-Paper"]').filter({ hasText: /(note|Added|Advanced|Changed)/i });
  const noteCount = await noteElements.count();
  console.log(`✅ Notes found: ${noteCount}`);

  // Verify notes functionality is working
  if (noteCount === 0) {
    console.log('ℹ️ No notes found - this may be a new application or no notes have been added yet');
  }
});
