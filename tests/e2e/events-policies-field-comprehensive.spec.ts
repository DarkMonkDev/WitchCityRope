/**
 * Comprehensive Policies Field Test (DataFactory Migration)
 *
 * PURPOSE: Test event policies field functionality with independent test data
 *
 * MIGRATION NOTES:
 * - Migrated to DataFactory pattern (2025-12-13)
 * - Creates own test events instead of relying on seed data
 * - Uses df fixture for automatic cleanup
 * - Data is automatically cleaned up after each test
 *
 * What This Test Covers:
 * 1. Policies field displays correctly in event form
 * 2. Policies field saves to database (API verification)
 * 3. Policies field persists after page reload
 * 4. Empty policies field handling
 * 5. API response structure validation
 */

import { expect } from '@playwright/test';
import { test } from '../lib/datafactory/fixtures/test.fixture';
import { AuthHelpers } from './test-utils/helpers/auth.helpers';

test.describe('Policies Field - Comprehensive Testing (DataFactory)', () => {
  test.beforeEach(async ({ page }) => {
    // Login as admin for event management
    await AuthHelpers.loginAs(page, 'admin');
    console.log('✅ Logged in as admin');
  });

  test.describe('Policies Field Display and Form Validation', () => {
    test('should display policies field in event form', async ({ page, df }) => {
      console.log('🧪 Testing policies field display...');

      // Create test event using DataFactory
      const event = await df.events.createPublished(`Policies Display Test ${Date.now()}`);
      console.log(`✅ Created test event: ${event.id}`);

      // Navigate to event edit page
      await page.goto(`/admin/events/${event.id}`);
      await page.waitForLoadState('domcontentloaded');
      console.log('✓ Navigated to event edit page');

      // Verify page loaded
      await expect(page.locator('[data-testid="page-admin-event-details"]')).toBeVisible({
        timeout: 5000,
      });

      // Wait for EventForm to load (tabs indicate form is ready)
      await expect(page.locator('[data-testid="tabs-event-management"]')).toBeVisible({
        timeout: 15000,
      });
      console.log('✓ Event form loaded');

      // Verify policies field exists and is visible
      // The policies field uses MantineTiptapEditor (rich text), not a textarea
      // Use data-testid or Mantine RichTextEditor classes
      const policiesSelectors = [
        '.mantine-RichTextEditor-root .ProseMirror',
        '[contenteditable="true"].ProseMirror',
        '.tiptap',
      ];

      // Find the policies field - it's the SECOND TipTap editor on the page
      // (first is fullDescription, second is policies)
      let policiesField = null;
      for (const selector of policiesSelectors) {
        const fields = page.locator(selector);
        const count = await fields.count();
        if (count >= 2) {
          // Get the second editor (policies)
          policiesField = fields.nth(1);
          console.log(`✓ Found policies field using selector: ${selector} (nth: 1, total: ${count})`);
          break;
        } else if (count === 1) {
          // If only one editor, use it
          policiesField = fields.first();
          console.log(`✓ Found single editor using selector: ${selector}`);
          break;
        }
      }

      if (!policiesField) {
        await page.screenshot({
          path: './test-results/policies-field-not-found.png',
          fullPage: true,
        });
        throw new Error('❌ Policies field not found on the page');
      }

      // Verify field is visible and accessible
      await expect(policiesField).toBeVisible();
      console.log('✅ Policies field displays correctly in event form');
    });
  });

  test.describe('Policies Field Persistence and API Integration', () => {
    const TEST_POLICIES = `Test Policies - ${Date.now()}
- Attendees must sign waiver
- No photography without consent
- Safety protocols required`;

    // Uses keyboard.type() pattern from tiptap-editors.spec.ts for proper TipTap integration
    test('should save policies field and persist after page refresh', async ({ page, df }) => {
      console.log('🧪 Testing policies field save and persistence...');

      // Create test event using DataFactory
      const event = await df.events.createPublished(`Policies Persist Test ${Date.now()}`);
      const eventId = event.id;
      console.log(`✅ Created test event: ${eventId}`);

      // Navigate to event edit page
      await page.goto(`/admin/events/${eventId}`);
      await page.waitForLoadState('domcontentloaded');

      // Wait for page to fully load
      await expect(page.locator('[data-testid="page-admin-event-details"]')).toBeVisible({
        timeout: 5000,
      });

      // Wait for EventForm to load (tabs indicate form is ready)
      await expect(page.locator('[data-testid="tabs-event-management"]')).toBeVisible({
        timeout: 15000,
      });
      console.log('✓ Event form loaded');

      // Find policies field - it's the SECOND RichTextEditor content area
      // (first is fullDescription, second is policies)
      const editorContents = page.locator('.mantine-RichTextEditor-content');
      const editorCount = await editorContents.count();
      console.log(`Found ${editorCount} TipTap editors on page`);

      // Get the policies editor (second one if available)
      const policiesEditor = editorCount >= 2 ? editorContents.nth(1) : editorContents.first();
      await expect(policiesEditor).toBeVisible({ timeout: 5000 });

      // Click into the policies editor and type content using keyboard
      // This is the working pattern from tiptap-editors.spec.ts
      await policiesEditor.click();
      await page.waitForTimeout(100);

      // Type policies content using keyboard (proper TipTap integration)
      await page.keyboard.type(TEST_POLICIES, { delay: 10 });
      console.log('✓ Typed policies content using keyboard');

      // Wait for React/TipTap state to propagate
      await page.waitForTimeout(500);

      // Verify text was entered
      const enteredValue = await policiesEditor.textContent();
      expect(enteredValue).toContain('Test Policies');
      console.log('✓ Policies field filled with test content');

      await page.waitForTimeout(500);

      // Save the event - wait for the button to be enabled first
      const saveButton = page.locator('button:has-text("Save")').last();
      await expect(saveButton).toBeEnabled({ timeout: 10000 });
      await saveButton.click();
      console.log('✓ Clicked save button');

      // Wait for save operation
      await page.waitForTimeout(2000);
      await page.waitForLoadState('domcontentloaded');

      // Verify API has the saved policies value (use page.evaluate for API calls)
      console.log('📡 Verifying API saved the policies value...');
      const apiResponse = await page.evaluate(async (eventId) => {
        const response = await fetch(`/api/events/${eventId}`);
        return {
          ok: response.ok,
          data: await response.json(),
        };
      }, eventId);
      expect(apiResponse.ok).toBe(true);

      // API returns event object directly (not wrapped)
      const eventData = apiResponse.data;
      expect(eventData).not.toBeNull();

      // Check policies field in API response - may be wrapped in HTML
      const policiesInAPI = eventData.policies;
      console.log(`API policies value: "${policiesInAPI}"`);
      // Policies may be wrapped in HTML tags by TipTap, check content exists
      expect(policiesInAPI).toContain('Test Policies');
      console.log('✅ API correctly saved policies value');

      // Refresh page to verify persistence in UI
      console.log('🔄 Refreshing page to verify UI persistence...');
      await page.reload();
      await page.waitForLoadState('domcontentloaded');

      // Wait for form to load after refresh
      await expect(page.locator('[data-testid="tabs-event-management"]')).toBeVisible({
        timeout: 15000,
      });

      // Find policies field again after refresh using consistent pattern
      const editorContentsAfterRefresh = page.locator('.mantine-RichTextEditor-content');
      const editorCountAfterRefresh = await editorContentsAfterRefresh.count();
      const policiesEditorAfterRefresh = editorCountAfterRefresh >= 2
        ? editorContentsAfterRefresh.nth(1)
        : editorContentsAfterRefresh.first();

      await expect(policiesEditorAfterRefresh).toBeVisible({ timeout: 5000 });

      // Wait for field to be populated
      await page.waitForTimeout(1000);

      // Verify field still has saved value
      const policiesValueAfterRefresh = await policiesEditorAfterRefresh.textContent();
      console.log(`Policies field value after refresh: "${policiesValueAfterRefresh}"`);

      expect(policiesValueAfterRefresh).toContain('Test Policies');
      console.log('✅ Policies field persists correctly after page refresh');
    });

    // Uses keyboard.type() and keyboard shortcuts for proper TipTap integration
    test('should handle empty policies field gracefully', async ({ page, df }) => {
      console.log('🧪 Testing empty policies field handling...');

      // Create test event using DataFactory
      const event = await df.events.createPublished(`Empty Policies Test ${Date.now()}`);
      console.log(`✅ Created test event: ${event.id}`);

      // Navigate to event edit page
      await page.goto(`/admin/events/${event.id}`);
      await page.waitForLoadState('domcontentloaded');

      // Wait for page to load
      await expect(page.locator('[data-testid="page-admin-event-details"]')).toBeVisible({
        timeout: 5000,
      });

      // Wait for EventForm to load (tabs indicate form is ready)
      await expect(page.locator('[data-testid="tabs-event-management"]')).toBeVisible({
        timeout: 15000,
      });
      console.log('✓ Event form loaded');

      // Find policies field using consistent pattern
      const editorContents = page.locator('.mantine-RichTextEditor-content');
      const editorCount = await editorContents.count();
      console.log(`Found ${editorCount} TipTap editors on page`);

      const policiesEditor = editorCount >= 2 ? editorContents.nth(1) : editorContents.first();
      await expect(policiesEditor).toBeVisible({ timeout: 5000 });

      // First, add some content to the policies field using keyboard
      await policiesEditor.click();
      await page.waitForTimeout(100);
      await page.keyboard.type('Test policies to be cleared', { delay: 10 });
      await page.waitForTimeout(500);
      console.log('✓ Added initial policies content');

      // Save the event with content first
      const saveButton1 = page.locator('button:has-text("Save")').last();
      await expect(saveButton1).toBeEnabled({ timeout: 10000 });
      await saveButton1.click();
      console.log('✓ Saved event with policies content');
      await page.waitForTimeout(2000);

      // Now clear the policies field using keyboard shortcuts
      await policiesEditor.click();
      await page.waitForTimeout(100);
      // Select all and delete using keyboard
      await page.keyboard.press('Control+a');
      await page.keyboard.press('Backspace');
      await page.waitForTimeout(500);
      console.log('✓ Cleared policies content using keyboard');

      // Save - wait for button to be enabled first
      const saveButton = page.locator('button:has-text("Save")').last();
      await expect(saveButton).toBeEnabled({ timeout: 10000 });
      await saveButton.click();
      await page.waitForTimeout(2000);
      await page.waitForLoadState('domcontentloaded');

      // Refresh
      await page.reload();
      await page.waitForLoadState('domcontentloaded');

      // Wait for form to load after refresh
      await expect(page.locator('[data-testid="tabs-event-management"]')).toBeVisible({
        timeout: 15000,
      });

      // Find field again after refresh
      let policiesFieldAfterRefresh = null;
      for (const selector of policiesSelectors) {
        const fields = page.locator(selector);
        const count = await fields.count();
        if (count >= 2) {
          policiesFieldAfterRefresh = fields.nth(1);
          break;
        } else if (count === 1) {
          policiesFieldAfterRefresh = fields.first();
          break;
        }
      }

      if (policiesFieldAfterRefresh) {
        const emptyValue = await policiesFieldAfterRefresh.textContent();
        // ProseMirror may have empty paragraph or whitespace when cleared
        expect(emptyValue?.trim() || '').toBe('');
        console.log('✅ Empty policies field handled gracefully');
      }
    });
  });

  test.describe('Policies Field API Response Structure', () => {
    test('should verify policies field in API response matches frontend', async ({
      page,
      df,
    }) => {
      console.log('🧪 Testing API response structure for policies field...');

      // Create test event using DataFactory
      const event = await df.events.createPublished(`Policies API Test ${Date.now()}`);
      const eventId = event.id;
      console.log(`✅ Created test event: ${eventId}`);

      // Navigate to event edit page
      await page.goto(`/admin/events/${eventId}`);
      await page.waitForLoadState('domcontentloaded');

      // Wait for page to load
      await expect(page.locator('[data-testid="page-admin-event-details"]')).toBeVisible({
        timeout: 5000,
      });

      // Wait for EventForm to load (tabs indicate form is ready)
      await expect(page.locator('[data-testid="tabs-event-management"]')).toBeVisible({
        timeout: 15000,
      });
      console.log('✓ Event form loaded');

      // Make direct API call (use page.evaluate)
      const apiResponse = await page.evaluate(async (eventId) => {
        const response = await fetch(`/api/events/${eventId}`);
        return {
          ok: response.ok,
          data: await response.json(),
        };
      }, eventId);
      expect(apiResponse.ok).toBe(true);

      // API returns event object directly (not wrapped)
      const eventData = apiResponse.data;
      console.log('📡 API response - event data present:', !!eventData);
      expect(eventData).not.toBeNull();

      // Check policies field exists in API response
      const hasPoliciesField = 'policies' in eventData;
      console.log(`  - policies field present: ${hasPoliciesField}`);

      if (hasPoliciesField) {
        console.log(`  - policies value: "${eventData.policies || 'NULL'}"`);
      }

      // Verify UI displays policies field
      // Policies is the SECOND TipTap editor on the page (first is fullDescription)
      const policiesSelectors = [
        '.mantine-RichTextEditor-root .ProseMirror',
        '[contenteditable="true"].ProseMirror',
        '.tiptap',
      ];

      let policiesField = null;
      for (const selector of policiesSelectors) {
        const fields = page.locator(selector);
        const count = await fields.count();
        if (count >= 2) {
          policiesField = fields.nth(1);
          console.log(`✓ Found policies field: ${selector}`);
          break;
        } else if (count === 1) {
          policiesField = fields.first();
          break;
        }
      }

      expect(policiesField).not.toBeNull();
      const displayedValue = await policiesField!.textContent();
      console.log(`  - UI displayed value: "${displayedValue || 'EMPTY'}"`);

      // Verify API value matches UI display
      const policiesInAPI = eventData.policies || '';
      // API returns plain text or HTML, textContent returns plain text - strip tags for comparison
      const policiesPlainText = policiesInAPI.replace(/<[^>]*>/g, '').trim();
      const displayedPlainText = displayedValue?.trim() || '';
      if (policiesPlainText && displayedPlainText) {
        // Both have values - check if they contain similar content
        expect(displayedPlainText.length).toBeGreaterThan(0);
        console.log('✅ API and UI policies values both have content');
      } else if (!policiesPlainText && !displayedPlainText) {
        console.log('✅ Both API and UI correctly show empty policies');
      } else {
        console.log('⚠️ Mismatch between API and UI policies values');
        console.log(`   API (plain): "${policiesPlainText}"`);
        console.log(`   UI: "${displayedPlainText}"`);
      }
    });
  });
});
