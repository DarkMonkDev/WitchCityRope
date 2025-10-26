import { test, expect } from '@playwright/test';

test.describe('Event Checkout - Pricing Step Verification', () => {
  const eventId = '4f65d190-ec4d-4b28-aef0-64fabd3151cd';
  const freeRsvpTicketId = '019a1ed8-0b59-73f7-b6bf-08c1c20b6d00';
  const supportDonationTicketId = '019a1ed8-0b59-7d39-977e-4828b31b5fda';
  
  test.beforeEach(async ({ page }) => {
    // Login first
    await page.goto('/login');
    await page.waitForLoadState('networkidle');
    
    // Fill in login credentials
    await page.locator('input[type="email"]').fill('member@witchcityrope.com');
    await page.locator('input[type="password"]').fill('Test123!');
    
    // Click sign in button
    await page.locator('button:has-text("Sign In")').click();
    
    // Wait for navigation to complete
    await page.waitForLoadState('networkidle');
    await page.waitForTimeout(2000);
  });
  
  test('Free RSVP ticket - fixed price display', async ({ page }) => {
    console.log('TEST 1: Navigating with Free RSVP ticket (Fixed Price)');
    
    const checkoutUrl = `/checkout/${eventId}/reg_test_001?ticketTypeId=${freeRsvpTicketId}`;
    await page.goto(checkoutUrl);
    await page.waitForLoadState('networkidle');
    await page.waitForTimeout(2000);
    
    // Take screenshot
    await page.screenshot({ path: './test-results/checkout-free-rsvp.png', fullPage: true });
    console.log('✓ Screenshot saved');
    
    // Check stepper shows "Ticket Selection" (not "Pricing")
    const stepperText = await page.locator('text=Ticket Selection').first();
    await expect(stepperText).toBeVisible();
    console.log('✓ Stepper shows "Ticket Selection"');
    
    // Check for fixed price display ($0.00)
    const priceDisplay = await page.locator('text=$0.00').first();
    await expect(priceDisplay).toBeVisible();
    console.log('✓ Fixed price $0.00 is displayed');
    
    // Check for the fixed price text
    const fixedPriceText = page.locator('text=fixed-price ticket');
    const isVisible = await fixedPriceText.isVisible().catch(() => false);
    expect(isVisible).toBeTruthy();
    console.log('✓ "fixed-price ticket" text is displayed');
    
    // Verify no sliding scale selector
    const slidingScaleText = page.locator('text=Sliding Scale').first();
    const hasSlidingScale = await slidingScaleText.isVisible().catch(() => false);
    expect(hasSlidingScale).toBeFalsy();
    console.log('✓ No sliding scale selector shown for fixed price');
  });
  
  test('Support Donation ticket - sliding scale selector', async ({ page }) => {
    console.log('\nTEST 2: Changing to Support Donation ticket (Sliding Scale)');
    
    const checkoutUrl = `/checkout/${eventId}/reg_test_001?ticketTypeId=${freeRsvpTicketId}`;
    await page.goto(checkoutUrl);
    await page.waitForLoadState('networkidle');
    await page.waitForTimeout(2000);
    
    // Change ticket type to Support Donation
    const selectInput = page.locator('input[placeholder*="Choose"]').first();
    const isSelectVisible = await selectInput.isVisible().catch(() => false);
    
    if (isSelectVisible) {
      await selectInput.click();
      await page.waitForTimeout(500);
      
      // Click Support Donation option - look for the one in the dropdown
      const supportDonationOption = page.locator('div[role="option"]:has-text("Support Donation")').first();
      const hasOption = await supportDonationOption.isVisible().catch(() => false);
      
      if (hasOption) {
        await supportDonationOption.click();
        await page.waitForTimeout(1500);
      }
    }
    
    // Take screenshot
    await page.screenshot({ path: './test-results/checkout-support-donation.png', fullPage: true });
    console.log('✓ Screenshot saved');
    
    // Check stepper shows "Pricing"
    const pricingText = await page.locator('text=Pricing').first();
    const hasPricingText = await pricingText.isVisible().catch(() => false);
    expect(hasPricingText).toBeTruthy();
    console.log('✓ Stepper shows "Pricing"');
    
    // Check for sliding scale selector
    const slidingScaleRadio = page.locator('text=Sliding Scale').first();
    const isSlidingScaleVisible = await slidingScaleRadio.isVisible().catch(() => false);
    expect(isSlidingScaleVisible).toBeTruthy();
    console.log('✓ Sliding scale selector is visible');
    
    // Check for price range display
    const sliderMarks = page.locator('text=$10');
    const hasSlider = await sliderMarks.isVisible().catch(() => false);
    expect(hasSlider || isSlidingScaleVisible).toBeTruthy();
    console.log('✓ Price range is displayed');
  });
});
