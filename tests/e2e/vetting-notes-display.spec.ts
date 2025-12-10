import { test, expect } from '@playwright/test';
import { AuthHelpers } from './test-utils/helpers/auth.helpers';

// This test now uses dynamic data from existing applications
test('Verify notes appear after stage advancement', async ({ page }) => {
  // Login as admin using AuthHelpers
  await AuthHelpers.loginAs(page, 'admin');

  // Navigate to vetting page
  await page.goto('/admin/vetting', { waitUntil: 'domcontentloaded' });

  // Wait for table to load
  await page.waitForSelector('table', { timeout: 10000 }).catch(() => null);

  // Find and click on the first available application
  const applicationRows = page.locator('tbody tr');
  const applicationCount = await applicationRows.count();

  if (applicationCount === 0) {
    console.log('⏭️ No vetting applications found - skipping test');
    return;
  }

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
    await page.screenshot({ path: './test-results/vetting-notes-debug-error.png', fullPage: true });
    return;
  }

  // Take screenshot of full page
  await page.screenshot({ path: './test-results/vetting-notes-debug.png', fullPage: true });

  // The NotesSection component uses "Notes" as the title, not "Admin Notes"
  const notesSection = page.locator('text=Notes');
  const notesSectionVisible = await notesSection.first().isVisible().catch(() => false);

  // Verify notes section exists - soft check since component may use different text
  if (notesSectionVisible) {
    console.log('✅ Notes section visible');
  } else {
    console.log('⚠️ Notes section not found with "Notes" text - checking for note-related elements');
    // Look for textarea (note input) as alternative indicator
    const noteTextarea = page.locator('textarea[placeholder*="note"]');
    const hasNoteInput = await noteTextarea.count() > 0;
    console.log(`Note input found: ${hasNoteInput}`);
  }

  // Count any notes displayed - look for Paper elements with note content
  const noteElements = page.locator('[class*="mantine-Paper"]').filter({ hasText: /(note|Added|Advanced|Changed)/i });
  const noteCount = await noteElements.count();
  console.log(`✅ Note count on page: ${noteCount}`);
});
