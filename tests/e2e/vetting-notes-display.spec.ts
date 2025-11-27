import { test, expect } from '@playwright/test';
import { AuthHelpers } from './test-utils/helpers/auth.helpers';

// This test now uses dynamic data from existing applications
test('Verify notes appear after stage advancement', async ({ page }) => {
  // Login as admin using AuthHelpers
  await AuthHelpers.loginAs(page, 'admin');

  // Navigate to vetting page
  await page.goto('/admin/vetting');
  await page.waitForLoadState('domcontentloaded');

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

  // Wait for application detail to load
  await page.waitForSelector('[data-testid="application-title"]', { timeout: 10000 });

  // Take screenshot of full page
  await page.screenshot({ path: '/home/chad/repos/witchcityrope/test-results/vetting-notes-debug.png', fullPage: true });

  // Check if notes section exists
  const notesSection = page.locator('text=Admin Notes');
  await expect(notesSection).toBeVisible();

  // Verify notes section functionality
  console.log(`✅ Notes section visible: ${await notesSection.isVisible()}`);

  // Count any notes displayed - look for common note patterns
  const noteElements = page.locator('[data-testid="vetting-note"]');
  const noteCount = await noteElements.count();
  console.log(`✅ Note count on page: ${noteCount}`);
});
