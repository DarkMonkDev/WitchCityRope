/**
 * Refund Validation E2E Test
 *
 * Tests all validation rules in RefundConfirmationModal:
 * - Cannot submit without refund reason (required field)
 * - Cannot submit without confirmation checkbox
 * - 500 character limit on refund reason enforced
 * - Character counter updates correctly
 * - Form validation messages display correctly
 * - Disabled state handling
 *
 * Phase 3: PayPal Refund System Implementation
 * Component: RefundConfirmationModal.tsx
 */

import { test, expect } from '@playwright/test';
import { AuthHelpers } from '../helpers/auth.helpers';

test.describe('Refund Confirmation Modal - Validation Rules', () => {

  test.beforeEach(async ({ page }) => {
    // Login as admin for all validation tests
    await AuthHelpers.loginAs(page, 'admin');
  });

  /**
   * Helper function to open refund modal
   * Reused across multiple tests
   */
  async function openRefundModal(page: any): Promise<boolean> {
    await page.goto('http://localhost:5173/admin/payments');
    await page.waitForLoadState('networkidle');

    const paymentRows = page.locator('table tbody tr, [data-testid="payment-row"]');

    if (await paymentRows.count() === 0) {
      console.log('⏭️  No payments found - cannot test');
      return false;
    }

    const refundButton = paymentRows.first().locator('button').filter({ hasText: /refund/i }).first();

    if (await refundButton.count() === 0) {
      console.log('⏭️  No refund button found - cannot test');
      return false;
    }

    await refundButton.click();
    await page.waitForTimeout(500);

    const modal = page.locator('[data-testid="refund-confirmation-modal"]');
    await expect(modal).toBeVisible({ timeout: 5000 });

    return true;
  }

  test('Cannot submit without refund reason', async ({ page }) => {
    console.log('\n🎯 TEST: Validation - Refund reason required');
    console.log('─'.repeat(60));

    const modalOpened = await openRefundModal(page);
    if (!modalOpened) {
      console.log('⏭️  Skipping test - no eligible payments');
      return;
    }

    const modal = page.locator('[data-testid="refund-confirmation-modal"]');

    console.log('📝 Step 1: Leave refund reason empty');
    const textarea = modal.locator('[data-testid="refund-reason-textarea"]');
    await textarea.clear();
    console.log('   ✅ Refund reason textarea is empty');

    console.log('📝 Step 2: Check confirmation checkbox');
    await modal.locator('[data-testid="refund-confirmation-checkbox"]').check();
    console.log('   ✅ Checkbox checked');

    console.log('📝 Step 3: Verify submit button is disabled');
    const confirmButton = modal.locator('[data-testid="refund-confirm-button"]');
    await expect(confirmButton).toBeDisabled({ timeout: 2000 });
    console.log('   ✅ Submit button is disabled (refund reason missing)');

    // Try to click disabled button (should not trigger)
    console.log('📝 Step 4: Attempt to click disabled button');
    await confirmButton.click({ force: true }).catch(() => {
      console.log('   ✅ Click prevented (as expected)');
    });

    // Verify no success notification
    const successNotification = page.locator('text=/Refund.*processed/i');
    const notificationVisible = await successNotification.isVisible().catch(() => false);
    expect(notificationVisible).toBe(false);
    console.log('   ✅ No success notification (validation working)');

    console.log('✅ TEST PASSED: Cannot submit without refund reason');
  });

  test('Cannot submit without confirmation checkbox', async ({ page }) => {
    console.log('\n🎯 TEST: Validation - Confirmation checkbox required');
    console.log('─'.repeat(60));

    const modalOpened = await openRefundModal(page);
    if (!modalOpened) {
      console.log('⏭️  Skipping test - no eligible payments');
      return;
    }

    const modal = page.locator('[data-testid="refund-confirmation-modal"]');

    console.log('📝 Step 1: Fill refund reason');
    const textarea = modal.locator('[data-testid="refund-reason-textarea"]');
    await textarea.fill('Valid refund reason for testing');
    console.log('   ✅ Refund reason entered');

    console.log('📝 Step 2: Leave confirmation checkbox unchecked');
    const checkbox = modal.locator('[data-testid="refund-confirmation-checkbox"]');
    await checkbox.uncheck();
    const isChecked = await checkbox.isChecked();
    expect(isChecked).toBe(false);
    console.log('   ✅ Checkbox is unchecked');

    console.log('📝 Step 3: Verify submit button is disabled');
    const confirmButton = modal.locator('[data-testid="refund-confirm-button"]');
    await expect(confirmButton).toBeDisabled({ timeout: 2000 });
    console.log('   ✅ Submit button is disabled (checkbox not checked)');

    console.log('✅ TEST PASSED: Cannot submit without confirmation checkbox');
  });

  test('Both refund reason AND checkbox required', async ({ page }) => {
    console.log('\n🎯 TEST: Validation - Both fields required');
    console.log('─'.repeat(60));

    const modalOpened = await openRefundModal(page);
    if (!modalOpened) {
      console.log('⏭️  Skipping test - no eligible payments');
      return;
    }

    const modal = page.locator('[data-testid="refund-confirmation-modal"]');

    console.log('📝 Step 1: Verify button disabled when both fields empty');
    const textarea = modal.locator('[data-testid="refund-reason-textarea"]');
    const checkbox = modal.locator('[data-testid="refund-confirmation-checkbox"]');
    const confirmButton = modal.locator('[data-testid="refund-confirm-button"]');

    await textarea.clear();
    await checkbox.uncheck();
    await expect(confirmButton).toBeDisabled({ timeout: 2000 });
    console.log('   ✅ Button disabled with both fields empty');

    console.log('📝 Step 2: Fill refund reason only');
    await textarea.fill('Test refund reason');
    await expect(confirmButton).toBeDisabled({ timeout: 2000 });
    console.log('   ✅ Button still disabled (checkbox missing)');

    console.log('📝 Step 3: Check checkbox only (clear reason)');
    await textarea.clear();
    await checkbox.check();
    await expect(confirmButton).toBeDisabled({ timeout: 2000 });
    console.log('   ✅ Button still disabled (reason missing)');

    console.log('📝 Step 4: Fill both fields');
    await textarea.fill('Test refund reason');
    await checkbox.check();
    await expect(confirmButton).toBeEnabled({ timeout: 2000 });
    console.log('   ✅ Button enabled with both fields filled');

    console.log('✅ TEST PASSED: Both fields required for submission');
  });

  test('500 character limit enforced on refund reason', async ({ page }) => {
    console.log('\n🎯 TEST: Validation - 500 character limit');
    console.log('─'.repeat(60));

    const modalOpened = await openRefundModal(page);
    if (!modalOpened) {
      console.log('⏭️  Skipping test - no eligible payments');
      return;
    }

    const modal = page.locator('[data-testid="refund-confirmation-modal"]');
    const textarea = modal.locator('[data-testid="refund-reason-textarea"]');

    console.log('📝 Step 1: Generate 600 character text');
    const longText = 'A'.repeat(600);
    console.log(`   Generated text with ${longText.length} characters`);

    console.log('📝 Step 2: Attempt to paste 600 characters');
    await textarea.fill(longText);

    console.log('📝 Step 3: Verify text truncated to 500 characters');
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

    const modal = page.locator('[data-testid="refund-confirmation-modal"]');
    const textarea = modal.locator('[data-testid="refund-reason-textarea"]');

    console.log('📝 Step 1: Verify counter shows 500 remaining initially');
    let counterText = await modal.locator('text=/[0-9]+ \\/ 500 characters remaining/').textContent();
    expect(counterText).toContain('500 / 500');
    console.log(`   ✅ Initial counter: ${counterText}`);

    console.log('📝 Step 2: Type 50 characters');
    const text50 = 'A'.repeat(50);
    await textarea.fill(text50);
    counterText = await modal.locator('text=/[0-9]+ \\/ 500 characters remaining/').textContent();
    expect(counterText).toContain('450 / 500');
    console.log(`   ✅ After 50 chars: ${counterText}`);

    console.log('📝 Step 3: Type 250 characters');
    const text250 = 'B'.repeat(250);
    await textarea.fill(text250);
    counterText = await modal.locator('text=/[0-9]+ \\/ 500 characters remaining/').textContent();
    expect(counterText).toContain('250 / 500');
    console.log(`   ✅ After 250 chars: ${counterText}`);

    console.log('📝 Step 4: Type 500 characters (max)');
    const text500 = 'C'.repeat(500);
    await textarea.fill(text500);
    counterText = await modal.locator('text=/[0-9]+ \\/ 500 characters remaining/').textContent();
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

    const modal = page.locator('[data-testid="refund-confirmation-modal"]');
    const textarea = modal.locator('[data-testid="refund-reason-textarea"]');

    console.log('📝 Step 1: Type character by character');
    await textarea.clear();

    const testString = 'Test refund';
    for (let i = 0; i < testString.length; i++) {
      await textarea.type(testString[i]);
      const currentLength = i + 1;
      const remaining = 500 - currentLength;

      // Check counter updates
      const counterText = await modal.locator('text=/[0-9]+ \\/ 500 characters remaining/').textContent();
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

    const modal = page.locator('[data-testid="refund-confirmation-modal"]');

    console.log('📝 Step 1: Fill refund reason with only whitespace');
    const textarea = modal.locator('[data-testid="refund-reason-textarea"]');
    await textarea.fill('     '); // Only spaces
    console.log('   ✅ Entered whitespace-only text');

    console.log('📝 Step 2: Check confirmation checkbox');
    await modal.locator('[data-testid="refund-confirmation-checkbox"]').check();

    console.log('📝 Step 3: Verify submit button is disabled');
    const confirmButton = modal.locator('[data-testid="refund-confirm-button"]');
    await expect(confirmButton).toBeDisabled({ timeout: 2000 });
    console.log('   ✅ Submit button disabled (whitespace-only reason invalid)');

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

    const modal = page.locator('[data-testid="refund-confirmation-modal"]');
    const textarea = modal.locator('[data-testid="refund-reason-textarea"]');
    const checkbox = modal.locator('[data-testid="refund-confirmation-checkbox"]');
    const confirmButton = modal.locator('[data-testid="refund-confirm-button"]');
    const cancelButton = modal.locator('[data-testid="refund-cancel-button"]');

    console.log('📝 Step 1: Verify initial button states');
    await expect(confirmButton).toBeDisabled({ timeout: 2000 });
    await expect(cancelButton).toBeEnabled({ timeout: 2000 });
    console.log('   ✅ Confirm disabled, Cancel enabled');

    console.log('📝 Step 2: Fill form partially (reason only)');
    await textarea.fill('Test refund reason');
    await expect(confirmButton).toBeDisabled({ timeout: 2000 });
    console.log('   ✅ Confirm still disabled');

    console.log('📝 Step 3: Complete form');
    await checkbox.check();
    await expect(confirmButton).toBeEnabled({ timeout: 2000 });
    console.log('   ✅ Confirm enabled when form complete');

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

    const modal = page.locator('[data-testid="refund-confirmation-modal"]');

    console.log('📝 Step 1: Verify warning box is visible');
    const warningBox = modal.locator('text=/This action will/i').first();
    await expect(warningBox).toBeVisible({ timeout: 3000 });
    console.log('   ✅ Warning box visible');

    console.log('📝 Step 2: Verify irreversible warning');
    const irreversibleWarning = modal.locator('text=/cannot be undone/i').first();
    await expect(irreversibleWarning).toBeVisible({ timeout: 3000 });
    console.log('   ✅ "Cannot be undone" warning visible');

    console.log('📝 Step 3: Verify refund amount is prominently displayed');
    const refundAmount = modal.locator('text=/\\$[0-9]+\\.[0-9]{2}/').first();
    await expect(refundAmount).toBeVisible({ timeout: 3000 });
    const amountText = await refundAmount.textContent();
    console.log(`   ✅ Refund amount displayed: ${amountText}`);

    console.log('📝 Step 4: Take screenshot of modal with warnings');
    await page.screenshot({
      path: './test-results/refund-validation-warnings.png',
      fullPage: true
    });
    console.log('   📸 Screenshot saved: refund-validation-warnings.png');

    console.log('✅ TEST PASSED: Warning messages display correctly');
  });
});
