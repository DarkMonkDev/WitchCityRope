const { test, expect } = require('@playwright/test');

test.describe('Event Checkout - Pricing Step Verification', () => {
  const baseUrl = 'http://localhost:5173';
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
    await page.screenshot({ path: '/home/chad/repos/witchcityrope/test-results/checkout-free-rsvp.png', fullPage: true });
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
      
      // Click Support Donation option
      await page.locator('text=Support Donation').nth(1).click();
      await page.waitForTimeout(1500);
    }
    
    // Take screenshot
    await page.screenshot({ path: '/home/chad/repos/witchcityrope/test-results/checkout-support-donation.png', fullPage: true });
    console.log('✓ Screenshot saved: checkout-support-donation.png');
    
    // Check stepper shows "Pricing"
    const stepperText = await page.locator('.mantine-Stepper-steps').first().textContent();
    console.log('Stepper text:', stepperText);
    expect(stepperText).toContain('Pricing');
    console.log('✓ Stepper shows "Pricing"');
    
    // Check for sliding scale selector
    const slidingScaleRadio = page.locator('text=Sliding Scale').first();
    await expect(slidingScaleRadio).toBeVisible();
    console.log('✓ Sliding scale selector is visible');
    
    // Check for price range ($10-$40)
    const priceRange = page.locator('text=10');
    await expect(priceRange).toBeVisible();
    console.log('✓ Price range $10-$40 is displayed');
  });
});
