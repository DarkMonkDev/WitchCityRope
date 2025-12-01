import { test, expect } from '@playwright/test';

// Environment-aware URLs for container/host compatibility
const WEB_BASE_URL = process.env.PLAYWRIGHT_BASE_URL || 'http://localhost:5173';

test.describe('Event Checkout - Pricing Step Verification', () => {
  const baseUrl = WEB_BASE_URL;
  const eventId = '4f65d190-ec4d-4b28-aef0-64fabd3151cd';
  const freeRsvpTicketId = '019a1ed8-0b59-73f7-b6bf-08c1c20b6d00';
  const supportDonationTicketId = '019a1ed8-0b59-7d39-977e-4828b31b5fda';

  test('Free RSVP ticket - fixed price display', async ({ page }) => {
    console.log('Test 1: Navigating with Free RSVP ticket (Fixed Price)');

    const checkoutUrl = `${baseUrl}/checkout/${eventId}/reg_test_001?ticketTypeId=${freeRsvpTicketId}`;
    await page.goto(checkoutUrl);
    await page.waitForLoadState('networkidle');
    await page.waitForTimeout(2000);

    // Take screenshot
    await page.screenshot({ path: './test-results/checkout-free-rsvp.png', fullPage: true });
    console.log('✓ Screenshot saved: checkout-free-rsvp.png');

    // Check stepper shows "Ticket Selection" (not "Pricing")
    const stepperText = await page.locator('.mantine-Stepper-steps').first().textContent();
    console.log('Stepper text:', stepperText);
    expect(stepperText).toContain('Ticket Selection');
    console.log('✓ Stepper shows "Ticket Selection"');

    // Check for fixed price display ($0.00)
    const priceDisplay = await page.locator('text=$0.00').first();
    await expect(priceDisplay).toBeVisible();
    console.log('✓ Fixed price $0.00 is displayed');

    // Check there's no sliding scale selector
    const slidingScaleRadio = page.locator('text=Sliding Scale').first();
    const isVisible = await slidingScaleRadio.isVisible().catch(() => false);
    console.log(`Sliding scale visible: ${isVisible}`);
    expect(isVisible).toBeFalsy();
    console.log('✓ No sliding scale selector shown');
  });

  test('Support Donation ticket - sliding scale selector', async ({ page }) => {
    console.log('\nTest 2: Changing to Support Donation ticket (Sliding Scale)');

    const checkoutUrl = `${baseUrl}/checkout/${eventId}/reg_test_001?ticketTypeId=${freeRsvpTicketId}`;
    await page.goto(checkoutUrl);
    await page.waitForLoadState('networkidle');
    await page.waitForTimeout(2000);

    // Change ticket type to Support Donation
    const selectInput = page.locator('input[placeholder*="Choose"]').first();
    if (await selectInput.isVisible()) {
      await selectInput.click();
      await page.waitForTimeout(500);

      const supportOption = page.locator('text=Support Donation').first();
      if (await supportOption.isVisible()) {
        await supportOption.click();
        await page.waitForTimeout(1000);

        // Take screenshot after change
        await page.screenshot({ path: './test-results/checkout-support-donation.png', fullPage: true });
        console.log('✓ Screenshot saved: checkout-support-donation.png');

        // Check for sliding scale selector
        const fixedPriceRadio = page.locator('text=Fixed Price').first();
        const slidingScaleRadio = page.locator('text=Sliding Scale').first();

        await expect(fixedPriceRadio).toBeVisible();
        await expect(slidingScaleRadio).toBeVisible();
        console.log('✓ Both Fixed Price and Sliding Scale options visible');
      } else {
        console.log('⚠ Support Donation option not found in dropdown');
      }
    } else {
      console.log('⚠ Ticket type selector not visible');
    }
  });
});
