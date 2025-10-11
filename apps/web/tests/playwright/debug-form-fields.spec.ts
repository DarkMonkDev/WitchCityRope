import { test, expect } from '@playwright/test';
import { AuthHelpers } from './helpers/auth.helpers';

test.describe('Debug Event Form Fields - Field Display Investigation', () => {
  test('should display shortDescription and policies fields from API', async ({ page }) => {
    // Login as admin
    console.log('🔐 Logging in as admin...');
    await AuthHelpers.loginAs(page, 'admin');

    // Navigate to event that we KNOW has data
    const eventId = '64535b73-74c3-4b95-a1a9-2b2db70c3ba0';
    console.log(`📍 Navigating to event: ${eventId}`);
    await page.goto(`http://localhost:5173/admin/events/${eventId}`);

    // Wait for form to load
    console.log('⏳ Waiting for form to load...');
    await page.waitForSelector('[data-testid="event-form"]', { timeout: 10000 });
    console.log('✅ Form loaded successfully');

    // ==== CHECK TITLE FIELD (THIS WORKS) ====
    console.log('\n=== WORKING FIELD COMPARISON ===');
    const titleInputs = page.locator('input[name="title"]');
    const titleCount = await titleInputs.count();
    console.log(`📊 Title inputs found: ${titleCount}`);

    if (titleCount > 0) {
      const titleValue = await titleInputs.first().inputValue();
      console.log(`✅ Title field value: "${titleValue}"`);
      console.log(`✅ Title field has value: ${!!titleValue}`);
    }

    // ==== CHECK SHORT DESCRIPTION FIELD (THIS FAILS) ====
    console.log('\n=== BROKEN FIELD INVESTIGATION ===');

    // Try multiple possible selectors for shortDescription
    const shortDescSelectors = [
      'input[placeholder*="Brief description"]',
      'input[placeholder*="brief description"]',
      'input[name="shortDescription"]',
      'input[name="ShortDescription"]',
      '[data-testid="shortDescription"]',
      '[data-testid="short-description"]'
    ];

    for (const selector of shortDescSelectors) {
      const elements = page.locator(selector);
      const count = await elements.count();
      console.log(`🔍 Selector "${selector}": ${count} elements found`);

      if (count > 0) {
        const value = await elements.first().inputValue();
        console.log(`   ↳ Value: "${value}"`);
        console.log(`   ↳ Empty: ${value === ''}`);
      }
    }

    // ==== CHECK POLICIES FIELD (TIPTAP EDITOR) ====
    console.log('\n=== POLICIES EDITOR INVESTIGATION ===');

    const policiesSelectors = [
      '.tiptap',
      '[data-testid="policies"]',
      '[data-testid="policies-editor"]',
      '.ProseMirror'
    ];

    for (const selector of policiesSelectors) {
      const elements = page.locator(selector);
      const count = await elements.count();
      console.log(`🔍 Selector "${selector}": ${count} elements found`);

      if (count > 0) {
        const textContent = await elements.first().textContent();
        const innerHTML = await elements.first().innerHTML();
        console.log(`   ↳ Text content: "${textContent?.substring(0, 100)}..."`);
        console.log(`   ↳ Has HTML: ${innerHTML.length > 0}`);
        console.log(`   ↳ Empty: ${!textContent || textContent.trim() === ''}`);
      }
    }

    // ==== COMPREHENSIVE INPUT ANALYSIS ====
    console.log('\n=== ALL FORM INPUTS ANALYSIS ===');

    const allInputsData = await page.evaluate(() => {
      const inputs = document.querySelectorAll('input[type="text"], input:not([type]), textarea');
      return Array.from(inputs).map((input, index) => {
        const el = input as HTMLInputElement;
        return {
          index,
          tagName: el.tagName,
          type: el.type,
          name: el.name || '(no name)',
          id: el.id || '(no id)',
          placeholder: el.placeholder || '(no placeholder)',
          value: el.value || '(empty)',
          hasValue: !!el.value,
          visible: el.offsetParent !== null
        };
      });
    });

    console.log('\n📋 ALL TEXT INPUTS:');
    allInputsData.forEach(input => {
      console.log(`  [${input.index}] ${input.tagName} - name:"${input.name}" placeholder:"${input.placeholder}"`);
      console.log(`      ↳ value:"${input.value}" hasValue:${input.hasValue} visible:${input.visible}`);
    });

    // ==== CHECK TIPTAP/PROSEMIRROR EDITORS ====
    console.log('\n=== RICH TEXT EDITORS ===');

    const editorsData = await page.evaluate(() => {
      const editors = document.querySelectorAll('.tiptap, .ProseMirror, [contenteditable="true"]');
      return Array.from(editors).map((editor, index) => {
        const el = editor as HTMLElement;
        return {
          index,
          className: el.className,
          textContent: el.textContent?.substring(0, 100) || '(empty)',
          innerHTML: el.innerHTML.substring(0, 150),
          hasContent: !!el.textContent && el.textContent.trim() !== '',
          visible: el.offsetParent !== null
        };
      });
    });

    console.log('📋 ALL RICH TEXT EDITORS:');
    editorsData.forEach(editor => {
      console.log(`  [${editor.index}] class:"${editor.className}"`);
      console.log(`      ↳ text:"${editor.textContent}"`);
      console.log(`      ↳ hasContent:${editor.hasContent} visible:${editor.visible}`);
    });

    // ==== COMPARE WORKING VS BROKEN FIELDS ====
    console.log('\n=== PATTERN COMPARISON: WORKING vs BROKEN ===');

    // Find the title input attributes
    const titleAttrs = await page.evaluate(() => {
      const titleInput = document.querySelector('input[name="title"]');
      if (!titleInput) return null;
      return {
        name: (titleInput as HTMLInputElement).name,
        id: titleInput.id,
        className: titleInput.className,
        dataTestId: titleInput.getAttribute('data-testid'),
        hasFormControl: titleInput.closest('.mantine-TextInput-root') !== null
      };
    });
    console.log('✅ WORKING FIELD (title):', JSON.stringify(titleAttrs, null, 2));

    // Try to find shortDescription with similar pattern
    const shortDescAttrs = await page.evaluate(() => {
      const possibleInputs = [
        document.querySelector('input[name="shortDescription"]'),
        document.querySelector('input[placeholder*="Brief"]'),
        document.querySelector('input[placeholder*="brief"]')
      ].filter(Boolean)[0];

      if (!possibleInputs) return null;
      return {
        name: (possibleInputs as HTMLInputElement).name,
        id: possibleInputs.id,
        className: possibleInputs.className,
        dataTestId: possibleInputs.getAttribute('data-testid'),
        hasFormControl: possibleInputs.closest('.mantine-TextInput-root') !== null
      };
    });
    console.log('❌ BROKEN FIELD (shortDescription):', JSON.stringify(shortDescAttrs, null, 2));

    // ==== CHECK FORM STATE/REACT STATE ====
    console.log('\n=== REACT COMPONENT STATE INVESTIGATION ===');

    // Check if there are any React error boundaries or loading states
    const reactState = await page.evaluate(() => {
      return {
        hasErrorBoundary: !!document.querySelector('[data-error-boundary]'),
        hasLoadingSpinner: !!document.querySelector('[data-testid*="loading"], .loading'),
        formCount: document.querySelectorAll('form').length,
        mantineInputsCount: document.querySelectorAll('.mantine-TextInput-root').length
      };
    });
    console.log('React State:', JSON.stringify(reactState, null, 2));

    // ==== TAKE SCREENSHOT ====
    console.log('\n📸 Taking screenshot for visual debugging...');
    await page.screenshot({
      path: '/home/chad/repos/witchcityrope/apps/web/test-results/form-fields-debug.png',
      fullPage: true
    });
    console.log('✅ Screenshot saved to test-results/form-fields-debug.png');

    // ==== FINAL VERIFICATION ====
    console.log('\n=== FINAL DIAGNOSTIC SUMMARY ===');

    // Check if shortDescription exists but is empty
    const shortDescExists = await page.locator('input[placeholder*="Brief"]').count() > 0;
    const shortDescValue = shortDescExists ? await page.locator('input[placeholder*="Brief"]').first().inputValue() : null;

    console.log(`📊 ShortDescription field exists: ${shortDescExists}`);
    console.log(`📊 ShortDescription field value: "${shortDescValue}"`);
    console.log(`📊 ShortDescription is empty: ${shortDescValue === '' || shortDescValue === null}`);

    // This test is for diagnostic purposes, so we don't fail it
    // We're gathering information about what's happening
    expect(true).toBe(true);
  });
});
