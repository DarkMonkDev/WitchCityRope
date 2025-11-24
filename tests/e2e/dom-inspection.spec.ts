import { test, expect } from '@playwright/test';

test('Inspect login form DOM structure', async ({ page }) => {
  await page.goto('http://localhost:5173/login');
  
  // Wait for page to load
  await page.waitForTimeout(2000);
  
  // Take screenshot
  await page.screenshot({ path: '/home/chad/repos/witchcityrope/test-results/login-form-inspection.png' });
  
  // Get all input elements
  const inputs = await page.locator('input').all();
  console.log(`Found ${inputs.length} input elements`);
  
  for (let i = 0; i < inputs.length; i++) {
    const input = inputs[i];
    const type = await input.getAttribute('type');
    const placeholder = await input.getAttribute('placeholder');
    const name = await input.getAttribute('name');
    const id = await input.getAttribute('id');
    const className = await input.getAttribute('class');
    
    console.log(`Input ${i + 1}:
      - type: ${type}
      - placeholder: ${placeholder}
      - name: ${name}
      - id: ${id}
      - class: ${className?.substring(0, 100)}...
    `);
  }
  
  // Get all button elements
  const buttons = await page.locator('button').all();
  console.log(`Found ${buttons.length} button elements`);
  
  for (let i = 0; i < buttons.length; i++) {
    const button = buttons[i];
    const type = await button.getAttribute('type');
    const text = await button.textContent();
    const className = await button.getAttribute('class');
    
    console.log(`Button ${i + 1}:
      - type: ${type}
      - text: ${text}
      - class: ${className?.substring(0, 100)}...
    `);
  }
  
  // Print page HTML structure for form
  const formHtml = await page.locator('form').innerHTML();
  console.log('Form HTML structure:');
  console.log(formHtml);
});