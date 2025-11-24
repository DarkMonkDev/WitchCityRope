import { test, expect } from '@playwright/test';

/**
 * Manual Login Page Inspection
 * This test will inspect the actual DOM structure to find the correct selectors
 */

test('inspect login page DOM structure', async ({ page }) => {
  console.log('=== MANUAL LOGIN PAGE INSPECTION ===');
  
  await page.goto('http://localhost:5173/login');
  await page.waitForLoadState('networkidle');
  
  // Take screenshot of current state
  await page.screenshot({ path: '/home/chad/repos/witchcityrope/test-results/manual-inspection-page.png' });
  
  // Get page content and log it
  const pageHTML = await page.content();
  console.log('=== PAGE HTML STRUCTURE ===');
  
  // Look for input elements
  const inputs = await page.locator('input').all();
  console.log(`Total input elements found: ${inputs.length}`);
  
  for (let i = 0; i < inputs.length; i++) {
    const input = inputs[i];
    const type = await input.getAttribute('type');
    const name = await input.getAttribute('name');
    const id = await input.getAttribute('id');
    const className = await input.getAttribute('class');
    const placeholder = await input.getAttribute('placeholder');
    
    console.log(`Input ${i + 1}: type="${type}" name="${name}" id="${id}" class="${className}" placeholder="${placeholder}"`);
  }
  
  // Look for buttons
  const buttons = await page.locator('button').all();
  console.log(`Total button elements found: ${buttons.length}`);
  
  for (let i = 0; i < buttons.length; i++) {
    const button = buttons[i];
    const type = await button.getAttribute('type');
    const className = await button.getAttribute('class');
    const textContent = await button.textContent();
    
    console.log(`Button ${i + 1}: type="${type}" class="${className}" text="${textContent?.trim()}"`);
  }
  
  // Check if there are any forms
  const forms = await page.locator('form').all();
  console.log(`Total form elements found: ${forms.length}`);
  
  // Get the current URL
  const currentUrl = page.url();
  console.log(`Current URL: ${currentUrl}`);
  
  // Check for any error messages or loading states
  const bodyText = await page.locator('body').textContent();
  console.log(`Page contains "loading": ${bodyText?.toLowerCase().includes('loading')}`);
  console.log(`Page contains "error": ${bodyText?.toLowerCase().includes('error')}`);
  
  // Try to find the login form specifically
  const emailInputs = await page.locator('input').all();
  const passwordInputs = await page.locator('input[type="password"]').all();
  const loginButtons = await page.locator('button').all();
  
  console.log('=== SPECIFIC ELEMENT ANALYSIS ===');
  console.log(`Email-type inputs: ${emailInputs.length}`);
  console.log(`Password inputs: ${passwordInputs.length}`);
  console.log(`Buttons: ${loginButtons.length}`);
  
  // Final screenshot
  await page.screenshot({ path: '/home/chad/repos/witchcityrope/test-results/manual-inspection-final.png' });
  
  console.log('=== INSPECTION COMPLETE ===');
});