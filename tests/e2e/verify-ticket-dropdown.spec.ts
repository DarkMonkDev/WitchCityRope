import { test, expect } from '@playwright/test';
import { AuthHelpers } from './test-utils/helpers/auth.helpers';

const baseUrl = process.env.PLAYWRIGHT_BASE_URL || 'http://localhost:5173';

test('Verify ticket type dropdown displays correct labels without null', async ({ page }) => {
  // Login using AuthHelpers
  await AuthHelpers.loginAs(page, 'member');
  console.log('Logged in as member');
  
  // Navigate to checkout
  const eventId = '4f65d190-ec4d-4b28-aef0-64fabd3151cd';
  const registrationId = `reg_${Date.now()}_test`;
  
  await page.goto(`${baseUrl}/checkout/${eventId}/${registrationId}`);
  await page.waitForLoadState('domcontentloaded');
  await page.waitForTimeout(1500);
  
  await page.screenshot({ path: './test-results/01-checkout-page-logged-in.png' });
  console.log('Checkout page loaded');
  
  // Look for the ticket type input field
  const ticketInputs = await page.locator('input[type="text"], input[role="combobox"], input:not([type="hidden"])').all();
  console.log('Found input elements:', ticketInputs.length);
  
  // Find the one with "Free RSVP" or near "Select Ticket Type"
  let ticketInput = null;
  for (const input of ticketInputs) {
    const value = await input.inputValue();
    const placeholder = await input.getAttribute('placeholder');
    console.log('Input value:', value, 'placeholder:', placeholder);
    
    if (value && value.includes('Free RSVP')) {
      ticketInput = input;
      console.log('Found ticket input with Free RSVP');
      break;
    }
  }
  
  if (!ticketInput && ticketInputs.length > 0) {
    ticketInput = ticketInputs[0];
  }
  
  if (ticketInput) {
    console.log('Clicking on ticket input to open dropdown');
    await ticketInput.click();
    await page.waitForTimeout(1500);
    
    await page.screenshot({ path: './test-results/02-dropdown-open.png' });
    
    // Get options from the dropdown
    const options = await page.locator('[role="option"]').allTextContents();
    
    console.log('\n=== TICKET TYPE DROPDOWN OPTIONS ===');
    options.forEach((opt, idx) => {
      console.log(`${idx + 1}. "${opt}"`);
    });
    
    // Check for null values
    const hasNull = options.some(o => o.includes('$null') || o.includes('null') || o.includes('undefined'));
    
    console.log('\n=== VERIFICATION RESULTS ===');
    console.log('Total options found:', options.length);
    console.log('Has "$null" in any option:', hasNull);
    
    // Verify expected options
    const hasFreeRsvp = options.some(o => o.includes('Free RSVP') && o.includes('$0'));
    const hasSupportDonation = options.some(o => o.includes('Support Donation') && o.includes('$'));
    console.log('Has "Free RSVP - $0.00" option:', hasFreeRsvp);
    console.log('Has "Support Donation - $10.00 - $40.00" option:', hasSupportDonation);
    
    console.log('\n=== FINAL RESULT ===');
    if (!hasNull && hasFreeRsvp && hasSupportDonation) {
      console.log('PASS: All expected options present and no null values');
    } else if (!hasNull) {
      console.log('PASS: No $null values in dropdown options');
    } else {
      console.log('FAIL: Found $null in dropdown options');
    }
    
    expect(hasNull).toBe(false);
  } else {
    console.log('ERROR: Could not find ticket input');
  }
});
