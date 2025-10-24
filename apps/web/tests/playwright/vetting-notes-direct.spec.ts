import { test, expect } from '@playwright/test';
import { AuthHelpers } from './helpers/auth.helpers';

// SKIP: This test requires a specific application ID (0199c639-c0bb-75b0-a215-5afd4f725900) and
// specific element data-testid="application-title" that may not exist or match current UI structure.
// This was a diagnostic test for verifying notes after stage advancement.
// TODO: Either create test data fixture or rewrite to use dynamic data from existing applications
test.skip('Verify notes appear after stage advancement - Direct navigation', async ({ page }) => {
  // Login as admin using AuthHelpers
  await AuthHelpers.loginAs(page, 'admin');

  // Navigate directly to vetting application detail
  await page.goto('http://localhost:5173/admin/vetting/0199c639-c0bb-75b0-a215-5afd4f725900');
  await page.waitForLoadState('networkidle');

  // Wait for application detail to load
  await page.waitForSelector('[data-testid="application-title"]', { timeout: 10000 });

  // Take screenshot of the page
  await page.screenshot({ path: '/home/chad/repos/witchcityrope/test-results/vetting-notes-direct.png', fullPage: true });

  // Check if notes section exists
  const notesSection = page.locator('text=Admin Notes');
  const notesVisible = await notesSection.isVisible();
  console.log(`✅ Notes section visible: ${notesVisible}`);

  // Count notes with reviewer name "RopeMaster"
  const reviewerNotes = page.locator('text=RopeMaster');
  const noteCount = await reviewerNotes.count();
  console.log(`✅ Notes with reviewer "RopeMaster": ${noteCount}`);

  // Look for the specific simplified note text the backend should generate
  const approvalNote = page.locator('text=/Approved for interview/i');
  const approvalCount = await approvalNote.count();
  console.log(`✅ "Approved for interview" notes: ${approvalCount}`);

  // Log all text content in notes area
  const allNotesText = await page.locator('[data-testid="add-note-textarea"]').locator('..').textContent();
  console.log(`Notes area content:\n${allNotesText}`);

  // Expected: Should have 3 notes (one for each stage advancement)
  expect(noteCount).toBeGreaterThan(0);
});
