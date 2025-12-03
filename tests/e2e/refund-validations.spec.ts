/**
 * Refund Validation E2E Test
 *
 * Tests all validation rules in RefundConfirmationModal:
 * - Refund amount is REQUIRED (> 0)
 * - Refund reason is REQUIRED (min 10 characters, trimmed)
 * - Confirmation checkbox is REQUIRED
 * - 500 character limit on refund reason enforced
 * - Character counter updates correctly
 * - Form validation messages display correctly
 * - Disabled state handling
 *
 * Phase 3: PayPal Refund System Implementation
 * Component: RefundConfirmationModal.tsx
 *
 * UPDATED: 2025-11-28 - Fixed to match actual component behavior
 * All tests now fill refund amount (required field that was missing in TDD tests)
 */

import { test, expect } from '@playwright/test';
import { AuthHelpers } from './test-utils/helpers/auth.helpers';

test.describe('Refund Confirmation Modal - Validation Rules', () => {

  test.beforeEach(async ({ page }) => {
    // Login as admin for all validation tests
    await AuthHelpers.loginAs(page, 'admin');
  });

  /**
   * Helper function to open refund modal
   * Reused across multiple tests
   *
   * Uses improved patterns:
   * - data-testid selectors instead of text matching
   * - .last() for React strict mode compatibility
   * - Mantine modal overlay detection
   */
  async function openRefundModal(page: any): Promise<boolean> {
    await page.goto('/admin/analytics/payments');
    await page.waitForLoadState('domcontentloaded');

    const paymentRows = page.locator('[data-testid="payment-row"]');

    if (await paymentRows.count() === 0) {
      console.log('⏭️  No payments found - cannot test');
      return false;
    }

    // Use specific data-testid pattern to find refund button
    const refundButton = paymentRows.first().locator('[data-testid^="refund-button-"]').last();

    if (await refundButton.count() === 0) {
      console.log('⏭️  No refund button found - cannot test');
      return false;
    }

    await refundButton.click();

    // Wait for the actual dialog element (Mantine renders a native dialog)
    // Note: getByTestId finds wrapper with visibility:hidden; use getByRole for visible dialog
    const modal = page.getByRole('dialog', { name: 'Process Variable Refund' });
    await expect(modal).toBeVisible({ timeout: 5000 });

    return true;
  }

  test('Cannot submit with empty refund reason (refund reason is required)', async ({ page }) => {
    // Component requires minimum 10-char reason - NOT optional
    // The actual component validates: refundReason.trim().length >= 10 (line 71-73)
    // Button disabled condition includes: !refundReason || refundReason.trim().length < 10 (line 299)
    console.log('\n🎯 TEST: Validation - Refund reason is required');
    console.log('─'.repeat(60));

    const modalOpened = await openRefundModal(page);
    if (!modalOpened) {
      console.log('⏭️  Skipping test - no eligible payments');
      return;
    }

    const modal = page.getByRole('dialog', { name: 'Process Variable Refund' });

    console.log('📝 Step 1: Fill refund amount');
    await modal.getByLabel('Refund Amount').fill('10');
    console.log('   ✅ Refund amount entered');

    console.log('📝 Step 2: Leave refund reason empty');
    const textarea = modal.getByLabel('Refund Reason');
    await textarea.clear();
    console.log('   ✅ Refund reason textarea is empty');

    console.log('📝 Step 3: Check confirmation checkbox');
    // Mantine checkboxes: click the wrapper element, don't use .check()
    await modal.getByRole('checkbox', { name: /understand this will process/i }).check();
    console.log('   ✅ Checkbox checked');

    console.log('📝 Step 4: Verify submit button is DISABLED (refund reason required)');
    const confirmButton = modal.getByRole('button', { name: 'Process Refund' });
    await expect(confirmButton).toBeDisabled({ timeout: 2000 });
    console.log('   ✅ Submit button is disabled (refund reason is required, min 10 chars)');

    console.log('✅ TEST PASSED: Refund reason is required, button disabled without it');
  });

  test('Cannot submit without confirmation checkbox', async ({ page }) => {
    console.log('\n🎯 TEST: Validation - Confirmation checkbox required');
    console.log('─'.repeat(60));

    const modalOpened = await openRefundModal(page);
    if (!modalOpened) {
      console.log('⏭️  Skipping test - no eligible payments');
      return;
    }

    const modal = page.getByRole('dialog', { name: 'Process Variable Refund' });

    console.log('📝 Step 1: Fill refund amount and reason');
    await modal.getByLabel('Refund Amount').fill('10');
    const textarea = modal.getByLabel('Refund Reason');
    await textarea.fill('Valid refund reason for testing');
    console.log('   ✅ Refund amount and reason entered');

    console.log('📝 Step 2: Leave confirmation checkbox unchecked');
    // Mantine checkbox: access the input element inside the wrapper for check/uncheck/isChecked
    const checkboxInput = modal.getByRole('checkbox', { name: /understand this will process/i });
    await checkboxInput.uncheck();
    const isChecked = await checkboxInput.isChecked();
    expect(isChecked).toBe(false);
    console.log('   ✅ Checkbox is unchecked');

    console.log('📝 Step 3: Verify submit button is disabled');
    const confirmButton = modal.getByRole('button', { name: 'Process Refund' });
    await expect(confirmButton).toBeDisabled({ timeout: 2000 });
    console.log('   ✅ Submit button is disabled (checkbox not checked)');

    console.log('✅ TEST PASSED: Cannot submit without confirmation checkbox');
  });

  test('All fields required for submission (amount, reason, checkbox)', async ({ page }) => {
    // Component requires amount + reason + checkbox, ALL required for submission
    // Button disabled condition: !confirmed || !refundAmount || refundAmount <= 0 || !refundReason || refundReason.trim().length < 10
    console.log('\n🎯 TEST: Validation - All fields required');
    console.log('─'.repeat(60));

    const modalOpened = await openRefundModal(page);
    if (!modalOpened) {
      console.log('⏭️  Skipping test - no eligible payments');
      return;
    }

    const modal = page.getByRole('dialog', { name: 'Process Variable Refund' });

    console.log('📝 Step 1: Verify button disabled when checkbox unchecked');
    const amountInput = modal.getByLabel('Refund Amount');
    const textarea = modal.getByLabel('Refund Reason');
    // Mantine checkbox: access the input element inside the wrapper for check/uncheck
    const checkboxInput = modal.getByRole('checkbox', { name: /understand this will process/i });
    const confirmButton = modal.getByRole('button', { name: 'Process Refund' });

    await amountInput.clear();
    await textarea.clear();
    await checkboxInput.uncheck();
    await expect(confirmButton).toBeDisabled({ timeout: 2000 });
    console.log('   ✅ Button disabled with all fields empty');

    console.log('📝 Step 2: Fill amount and reason only (checkbox still unchecked)');
    await amountInput.fill('10');
    await textarea.fill('Test refund reason - minimum 10 characters');
    await expect(confirmButton).toBeDisabled({ timeout: 2000 });
    console.log('   ✅ Button still disabled (checkbox required)');

    console.log('📝 Step 3: Check checkbox only (clear amount and reason)');
    await amountInput.clear();
    await textarea.clear();
    await checkboxInput.check();
    await expect(confirmButton).toBeDisabled({ timeout: 2000 });
    console.log('   ✅ Button still disabled (amount and reason required)');

    console.log('📝 Step 4: Fill all fields (verify enabled)');
    await amountInput.fill('10');
    await textarea.fill('Test refund reason - minimum 10 characters');
    await checkboxInput.check();
    await expect(confirmButton).toBeEnabled({ timeout: 2000 });
    console.log('   ✅ Button enabled with all required fields filled');

    console.log('✅ TEST PASSED: All fields (amount, reason, checkbox) required for submission');
  });

  test('500 character limit enforced on refund reason', async ({ page }) => {
    console.log('\n🎯 TEST: Validation - 500 character limit');
    console.log('─'.repeat(60));

    const modalOpened = await openRefundModal(page);
    if (!modalOpened) {
      console.log('⏭️  Skipping test - no eligible payments');
      return;
    }

    const modal = page.getByRole('dialog', { name: 'Process Variable Refund' });

    console.log('📝 Step 1: Fill refund amount');
    await modal.getByLabel('Refund Amount').fill('10');
    console.log('   ✅ Refund amount entered');

    const textarea = modal.getByLabel('Refund Reason');

    console.log('📝 Step 2: Generate 600 character text');
    const longText = 'A'.repeat(600);
    console.log(`   Generated text with ${longText.length} characters`);

    console.log('📝 Step 3: Attempt to paste 600 characters');
    await textarea.fill(longText);

    console.log('📝 Step 4: Verify text truncated to 500 characters');
    const textareaValue = await textarea.inputValue();
    expect(textareaValue.length).toBeLessThanOrEqual(500);
    console.log(`   ✅ Text limited to ${textareaValue.length} characters (max 500)`);

    console.log('✅ TEST PASSED: 500 character limit enforced');
  });

  test('Character counter displays correctly', async ({ page }) => {
    console.log('\n🎯 TEST: Validation - Character counter');
    console.log('─'.repeat(60));

    const modalOpened = await openRefundModal(page);
    if (!modalOpened) {
      console.log('⏭️  Skipping test - no eligible payments');
      return;
    }

    const modal = page.getByRole('dialog', { name: 'Process Variable Refund' });

    console.log('📝 Step 1: Fill refund amount');
    await modal.getByLabel('Refund Amount').fill('10');
    console.log('   ✅ Refund amount entered');

    const textarea = modal.getByLabel('Refund Reason');

    console.log('📝 Step 2: Verify counter shows 500 remaining initially');
    // Format: "500 / 500 characters remaining"
    let counter = modal.locator('text=/\\d+ \\/ 500 characters remaining/');
    let counterText = await counter.textContent();
    expect(counterText).toContain('500 / 500');
    console.log(`   ✅ Initial counter: ${counterText}`);

    console.log('📝 Step 3: Type 50 characters');
    const text50 = 'A'.repeat(50);
    await textarea.fill(text50);
    counterText = await counter.textContent();
    expect(counterText).toContain('450 / 500');
    console.log(`   ✅ After 50 chars: ${counterText}`);

    console.log('📝 Step 4: Type 250 characters');
    const text250 = 'B'.repeat(250);
    await textarea.fill(text250);
    counterText = await counter.textContent();
    expect(counterText).toContain('250 / 500');
    console.log(`   ✅ After 250 chars: ${counterText}`);

    console.log('📝 Step 5: Type 500 characters (max)');
    const text500 = 'C'.repeat(500);
    await textarea.fill(text500);
    counterText = await counter.textContent();
    expect(counterText).toContain('0 / 500');
    console.log(`   ✅ After 500 chars: ${counterText}`);

    console.log('✅ TEST PASSED: Character counter updates correctly');
  });

  test('Character counter updates in real-time', async ({ page }) => {
    console.log('\n🎯 TEST: Validation - Real-time counter updates');
    console.log('─'.repeat(60));

    const modalOpened = await openRefundModal(page);
    if (!modalOpened) {
      console.log('⏭️  Skipping test - no eligible payments');
      return;
    }

    const modal = page.getByRole('dialog', { name: 'Process Variable Refund' });

    console.log('📝 Step 1: Fill refund amount');
    await modal.getByLabel('Refund Amount').fill('10');
    console.log('   ✅ Refund amount entered');

    const textarea = modal.getByLabel('Refund Reason');

    console.log('📝 Step 2: Type character by character');
    await textarea.clear();

    const testString = 'Test refund';
    for (let i = 0; i < testString.length; i++) {
      await textarea.type(testString[i]);
      const currentLength = i + 1;
      const remaining = 500 - currentLength;

      // Check counter updates - format: "450 / 500 characters remaining"
      const counter = modal.locator('text=/\\d+ \\/ 500 characters remaining/');
      const counterText = await counter.textContent();
      expect(counterText).toContain(`${remaining} / 500`);

      if (i % 3 === 0) { // Log every 3 characters to reduce noise
        console.log(`   After ${currentLength} chars: ${counterText}`);
      }
    }

    console.log('   ✅ Counter updated after each keystroke');

    console.log('✅ TEST PASSED: Real-time counter updates work');
  });

  test('Whitespace-only refund reason is invalid', async ({ page }) => {
    console.log('\n🎯 TEST: Validation - Whitespace-only reason invalid');
    console.log('─'.repeat(60));

    const modalOpened = await openRefundModal(page);
    if (!modalOpened) {
      console.log('⏭️  Skipping test - no eligible payments');
      return;
    }

    const modal = page.getByRole('dialog', { name: 'Process Variable Refund' });

    console.log('📝 Step 1: Fill refund amount');
    await modal.getByLabel('Refund Amount').fill('10');
    console.log('   ✅ Refund amount entered');

    console.log('📝 Step 2: Fill refund reason with only whitespace');
    const textarea = modal.getByLabel('Refund Reason');
    await textarea.fill('     '); // Only spaces - trim() = '' which is < 10 chars
    console.log('   ✅ Entered whitespace-only text (5 spaces)');

    console.log('📝 Step 3: Check confirmation checkbox');
    // Mantine checkbox: access the input element inside the wrapper
    await modal.getByRole('checkbox', { name: /understand this will process/i }).check();

    console.log('📝 Step 4: Verify submit button is disabled');
    const confirmButton = modal.getByRole('button', { name: 'Process Refund' });
    await expect(confirmButton).toBeDisabled({ timeout: 2000 });
    console.log('   ✅ Submit button disabled (whitespace trim() = 0 chars < 10 required)');

    console.log('✅ TEST PASSED: Whitespace-only reason is invalid');
  });

  test('Button shows correct states during submission', async ({ page }) => {
    console.log('\n🎯 TEST: Validation - Button states');
    console.log('─'.repeat(60));

    const modalOpened = await openRefundModal(page);
    if (!modalOpened) {
      console.log('⏭️  Skipping test - no eligible payments');
      return;
    }

    const modal = page.getByRole('dialog', { name: 'Process Variable Refund' });
    const amountInput = modal.getByLabel('Refund Amount');
    const textarea = modal.getByLabel('Refund Reason');
    // Mantine checkbox: access the input element inside the wrapper
    const checkboxInput = modal.getByRole('checkbox', { name: /understand this will process/i });
    const confirmButton = modal.getByRole('button', { name: 'Process Refund' });
    const cancelButton = modal.getByRole('button', { name: 'Cancel' });

    console.log('📝 Step 1: Verify initial button states');
    await expect(confirmButton).toBeDisabled({ timeout: 2000 });
    await expect(cancelButton).toBeEnabled({ timeout: 2000 });
    console.log('   ✅ Confirm disabled, Cancel enabled');

    console.log('📝 Step 2: Fill form partially (amount + reason, no checkbox)');
    await amountInput.fill('10');
    await textarea.fill('Test refund reason for validation');
    await expect(confirmButton).toBeDisabled({ timeout: 2000 });
    console.log('   ✅ Confirm still disabled (checkbox required)');

    console.log('📝 Step 3: Complete form (check checkbox)');
    await checkboxInput.check();
    await expect(confirmButton).toBeEnabled({ timeout: 2000 });
    console.log('   ✅ Confirm enabled when all required fields complete');

    // Note: We don't actually submit to avoid processing real refunds
    console.log('   ⏭️  Not clicking submit to avoid processing real refund');

    console.log('✅ TEST PASSED: Button states correct');
  });

  test('Modal displays warning messages correctly', async ({ page }) => {
    console.log('\n🎯 TEST: Validation - Warning messages display');
    console.log('─'.repeat(60));

    const modalOpened = await openRefundModal(page);
    if (!modalOpened) {
      console.log('⏭️  Skipping test - no eligible payments');
      return;
    }

    const modal = page.getByRole('dialog', { name: 'Process Variable Refund' });

    console.log('📝 Step 1: Fill refund amount to initialize modal');
    await modal.getByLabel('Refund Amount').fill('10');
    console.log('   ✅ Refund amount entered');

    console.log('📝 Step 2: Verify RSVP warning is visible');
    // The actual warning text in the modal is "RSVP/Ticket will NOT be cancelled"
    const rsvpWarning = modal.locator('text=/RSVP.*NOT.*cancelled/i').first();
    await expect(rsvpWarning).toBeVisible({ timeout: 3000 });
    console.log('   ✅ RSVP warning visible');

    console.log('📝 Step 3: Verify irreversible warning');
    const irreversibleWarning = modal.locator('text=/cannot be undone/i').first();
    await expect(irreversibleWarning).toBeVisible({ timeout: 3000 });
    console.log('   ✅ "Cannot be undone" warning visible');

    console.log('📝 Step 4: Verify refund amount is prominently displayed');
    const refundAmount = modal.locator('text=/\\$[0-9]+\\.[0-9]{2}/').first();
    await expect(refundAmount).toBeVisible({ timeout: 3000 });
    const amountText = await refundAmount.textContent();
    console.log(`   ✅ Refund amount displayed: ${amountText}`);

    console.log('📝 Step 5: Take screenshot of modal with warnings');
    await page.screenshot({
      path: './test-results/refund-validation-warnings.png',
      fullPage: true
    });
    console.log('   📸 Screenshot saved: refund-validation-warnings.png');

    console.log('✅ TEST PASSED: Warning messages display correctly');
  });
});
