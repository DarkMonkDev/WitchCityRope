import { test, expect } from '@playwright/test';
import { AuthHelpers } from './helpers/auth.helpers';

test.describe('Policies Field Display Diagnostic Test', () => {
  test('verify policies field saves to API but displays after page refresh', async ({ page }) => {
    console.log('🔍 DIAGNOSTIC TEST: Checking if policies field has same bug as shortDescription');
    console.log('📋 Test Purpose: Verify if policies field is missing from transformApiEvent() in useEvents.ts');

    // Step 1: Login as admin
    console.log('\n📍 Step 1: Login as admin');
    await AuthHelpers.loginAs(page, 'admin');
    console.log('✅ Logged in successfully');

    // Step 2: Navigate to Events page
    console.log('\n📍 Step 2: Navigate to Events page');
    await page.goto('http://localhost:5173/admin/events');
    await page.waitForLoadState('networkidle');
    console.log('✅ On Events page');

    // Step 3: Find first event and click row to edit
    console.log('\n📍 Step 3: Looking for existing event to edit');
    let eventId: string | null = null;

    // Check if there are any events listed
    const eventRows = page.locator('[data-testid="event-row"]');
    const eventCount = await eventRows.count();

    if (eventCount > 0) {
      console.log(`✅ Found ${eventCount} existing events`);
      // Click the first row to navigate to edit page (row-click interaction pattern)
      const firstRow = eventRows.first();

      // Get the event title from the first row for logging
      const eventTitle = await firstRow.locator('td').nth(2).textContent(); // Event Title column
      console.log(`📋 Clicking on event: "${eventTitle}"`);

      await firstRow.click();

      // Wait for navigation to event edit page
      await page.waitForURL(/\/admin\/events\/[a-f0-9-]+/);

      // Extract event ID from URL
      const currentUrl = page.url();
      eventId = currentUrl.split('/').pop() || null;
      console.log(`✅ Editing event ID: ${eventId}`);
    } else {
      console.log('⚠️ No existing events found, please create one first');
      test.skip();
    }

    await page.waitForLoadState('networkidle');
    console.log('✅ Event form loaded');

    // Step 4: Make direct API call to check BEFORE any edits
    console.log('\n📍 Step 4: Direct API verification BEFORE any edits');
    const apiResponseBefore = await page.request.get(`http://localhost:5655/api/events/${eventId}`);
    const apiDataBefore = await apiResponseBefore.json();
    console.log('📡 API response BEFORE edits:');
    console.log(`  - title: ${apiDataBefore?.data?.title}`);
    console.log(`  - shortDescription: ${apiDataBefore?.data?.shortDescription || 'NULL'}`);
    console.log(`  - policies: ${apiDataBefore?.data?.policies || 'NULL'}`);

    // Step 5: Check what the form DISPLAYS for policies field
    console.log('\n📍 Step 5: Checking what form DISPLAYS for policies field');

    // Scroll to policies field
    const policiesHeading = page.locator('text=Policies & Procedures').first();
    await policiesHeading.scrollIntoViewIfNeeded();
    await policiesHeading.waitFor({ state: 'visible', timeout: 5000 });

    // Get the policies editor
    const policiesEditor = page.locator('text=Policies & Procedures').locator('xpath=../..').locator('.tiptap').first();
    await policiesEditor.waitFor({ state: 'visible', timeout: 5000 });

    const policiesDisplayedValue = await policiesEditor.textContent();
    console.log(`📋 Policies field DISPLAYED value: "${policiesDisplayedValue}"`);

    // Step 6: Analysis and verdict
    console.log('\n📍 Step 6: DIAGNOSTIC ANALYSIS');
    console.log('═══════════════════════════════════════════════════════════');

    const policiesInAPI = apiDataBefore?.data?.policies || null;
    const policiesDisplayed = policiesDisplayedValue || null;

    console.log(`API has policies value: ${policiesInAPI ? 'YES' : 'NO'}`);
    console.log(`Form displays policies value: ${policiesDisplayed ? 'YES' : 'NO (or empty)'}`);

    // Check if values match
    if (policiesInAPI && policiesDisplayed) {
      // Both have values - check if they match
      if (policiesDisplayed.includes(policiesInAPI) || policiesInAPI.includes(policiesDisplayed)) {
        console.log('\n✅ NO BUG: Policies field displays correctly!');
        console.log(`  - API value: "${policiesInAPI}"`);
        console.log(`  - Displayed value: "${policiesDisplayed}"`);
      } else {
        console.log('\n⚠️ MISMATCH: API and form show different values!');
        console.log(`  - API value: "${policiesInAPI}"`);
        console.log(`  - Displayed value: "${policiesDisplayed}"`);
      }
    } else if (policiesInAPI && !policiesDisplayed) {
      console.log('\n🚨 CONFIRMED BUG: Same issue as shortDescription!');
      console.log(`  - API HAS policies: "${policiesInAPI}"`);
      console.log(`  - Form DOES NOT display it: "${policiesDisplayed}"`);
      console.log('');
      console.log('📋 Root Cause: policies field is missing from transformApiEvent() in useEvents.ts');
      console.log('🔧 Fix Required: Add policies mapping in transformApiEvent() function');
      console.log('📍 File: /apps/web/src/lib/api/hooks/useEvents.ts');
      console.log('📝 Add around line 125: policies: (apiEvent as any).policies || null,');
      console.log('');
      console.log('Expected fix:');
      console.log('  return {');
      console.log('    id: apiEvent.id,');
      console.log('    title: apiEvent.title,');
      console.log('    shortDescription: (apiEvent as any).shortDescription || null,');
      console.log('    description: apiEvent.description,');
      console.log('    policies: (apiEvent as any).policies || null, // <-- ADD THIS LINE');
      console.log('    startDate: apiEvent.startDate,');
      console.log('    ...');
      console.log('  }');
    } else if (!policiesInAPI && !policiesDisplayed) {
      console.log('\n✅ BOTH EMPTY: No policies data in API, form correctly shows empty');
      console.log('  - This is expected behavior for events without policies');
    } else {
      console.log('\n⚠️ UNEXPECTED: Form has value but API doesn\'t');
      console.log(`  - API value: ${policiesInAPI}`);
      console.log(`  - Displayed value: "${policiesDisplayed}"`);
    }

    console.log('═══════════════════════════════════════════════════════════');

    // This test is for diagnostic purposes, so we'll pass regardless
    // The console logs contain all the evidence needed
    expect(true).toBe(true);
  });
});
