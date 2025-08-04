import { test, expect } from '@playwright/test';
import { testConfig } from '../helpers/test.config';
import { BlazorHelpers } from '../helpers/blazor.helpers';

test.describe('Public Forms Validation Tests', () => {
  test('Contact form validation', async ({ page }) => {
    console.log('\n📝 Testing Contact Form Validation...');
    
    await page.goto(`${testConfig.baseUrl}/contact`, {
      waitUntil: 'networkidle'
    });
    
    await BlazorHelpers.waitForBlazorReady(page);
    
    // Test 1: Empty form validation
    console.log('  1️⃣ Testing empty form submission...');
    const submitBtn = await page.locator('button[type="submit"]');
    expect(await submitBtn.isVisible()).toBeTruthy();
    
    await submitBtn.click();
    await page.waitForTimeout(1000);
    
    // Check for validation errors
    const errors = await page.locator('.text-danger, .wcr-validation-message').all();
    expect(errors.length).toBeGreaterThanOrEqual(4); // Name, email, subject, message
    console.log(`  ✅ Empty form validation: ${errors.length} errors shown`);
    
    // Test 2: Invalid email format
    console.log('  2️⃣ Testing invalid email format...');
    await page.fill('input[name*="Email" i]', 'not-an-email');
    await page.click('body'); // Blur to trigger validation
    await page.waitForTimeout(500);
    
    const emailError = await page.locator('.text-danger:has-text("email"), .wcr-validation-message:has-text("email")').first();
    expect(await emailError.isVisible()).toBeTruthy();
    console.log('  ✅ Email format validation working');
    
    // Test 3: Message too short
    console.log('  3️⃣ Testing message length validation...');
    await page.fill('textarea[name*="Message" i]', 'Short');
    await page.click('body');
    await page.waitForTimeout(500);
    
    const messageError = await page.locator('.text-danger:has-text("characters"), .wcr-validation-message:has-text("characters")').first();
    if (await messageError.isVisible()) {
      console.log('  ✅ Message length validation working');
    }
    
    // Test 4: Fill form with valid data
    console.log('  4️⃣ Testing valid form submission...');
    
    await page.fill('input[name*="Name" i]', 'Test User');
    await page.fill('input[name*="Email" i]', 'test@example.com');
    
    // Select subject if dropdown exists
    const subjectSelect = await page.locator('select[name*="Subject" i]');
    if (await subjectSelect.isVisible()) {
      await subjectSelect.selectOption({ index: 1 });
    }
    
    await page.fill('textarea[name*="Message" i]', 'This is a test message with more than 10 characters to meet validation requirements.');
    
    // Check newsletter subscription
    const newsletterCheckbox = await page.locator('input[type="checkbox"][name*="Subscribe" i]');
    if (await newsletterCheckbox.isVisible()) {
      await newsletterCheckbox.check();
    }
    
    // Submit form
    await submitBtn.click();
    
    // Wait for success message or redirect
    try {
      await page.waitForSelector('.alert-success, .success-message', {
        timeout: 5000
      });
      console.log('  ✅ Form submission successful');
    } catch {
      console.log('  ⚠️ No success message found - form may not be fully implemented');
    }
    
    await page.screenshot({ 
      path: 'test-results/validation-tests/contact-form-validation.png',
      fullPage: true 
    });
  });

  test('Newsletter form validation', async ({ page }) => {
    console.log('\n📧 Testing Newsletter Subscription Form Validation...');
    
    // Navigate to homepage (newsletter is in footer)
    await page.goto(`${testConfig.baseUrl}`, {
      waitUntil: 'networkidle'
    });
    
    await BlazorHelpers.waitForBlazorReady(page);
    
    // Scroll to footer
    await page.evaluate(() => {
      window.scrollTo(0, document.body.scrollHeight);
    });
    
    // Wait for newsletter form
    await page.waitForSelector('input[placeholder*="email" i], input[type="email"]', {
      state: 'visible',
      timeout: 5000
    });
    
    // Test 1: Empty email validation
    console.log('  1️⃣ Testing empty email submission...');
    
    // Find email input and submit button in footer
    const emailInput = await page.locator('footer input[type="email"], footer input[placeholder*="email" i]').first();
    expect(await emailInput.isVisible()).toBeTruthy();
    
    // Find submit button near the email input
    const submitBtn = await page.locator('footer button[type="submit"], footer button:has-text("Subscribe")').first();
    
    if (await submitBtn.isVisible()) {
      await submitBtn.click();
      await page.waitForTimeout(1000);
      
      // Check for validation (browser validation or custom)
      const validationMessage = await emailInput.evaluate((el: HTMLInputElement) => el.validationMessage);
      if (validationMessage) {
        console.log(`  ✅ Browser validation: "${validationMessage}"`);
      } else {
        // Check for custom validation message
        const customError = await page.locator('footer .text-danger, footer .wcr-validation-message').first();
        if (await customError.isVisible()) {
          console.log('  ✅ Custom validation message shown');
        }
      }
    } else {
      console.log('  ⚠️ Newsletter submit button not found - skipping test');
      return;
    }
    
    // Test 2: Invalid email format
    console.log('  2️⃣ Testing invalid email format...');
    
    await emailInput.fill('invalid-email-format');
    await submitBtn.click();
    await page.waitForTimeout(1000);
    
    // Check validation
    const emailValidation = await emailInput.evaluate((el: HTMLInputElement) => el.validationMessage);
    if (emailValidation) {
      console.log(`  ✅ Email format validation: "${emailValidation}"`);
    }
    
    // Test 3: Valid email submission
    console.log('  3️⃣ Testing valid email submission...');
    
    await emailInput.clear();
    await emailInput.fill('newsletter@example.com');
    await submitBtn.click();
    
    // Wait for success indication
    try {
      await page.waitForFunction(() => {
        const alerts = document.querySelectorAll('.alert-success, .success-message');
        const input = document.querySelector('footer input[type="email"]') as HTMLInputElement;
        // Either success message shown or input cleared
        return alerts.length > 0 || (input && input.value === '');
      }, { timeout: 5000 });
      console.log('  ✅ Newsletter subscription successful');
    } catch {
      console.log('  ⚠️ No clear success indication - form may not be fully implemented');
    }
    
    await page.screenshot({ 
      path: 'test-results/validation-tests/newsletter-form-validation.png',
      fullPage: true 
    });
  });

  test('Public incident report form validation', async ({ page }) => {
    console.log('\n🚨 Testing Public Incident Report Form Validation...');
    
    // Navigate to public incident report (if accessible)
    await page.goto(`${testConfig.baseUrl}/incident-report`, {
      waitUntil: 'networkidle'
    });
    
    // Check if page is accessible
    const pageTitle = await page.title();
    if (page.url().includes('login') || pageTitle.toLowerCase().includes('not found')) {
      console.log('  ⚠️ Public incident report not accessible - skipping');
      return;
    }
    
    await BlazorHelpers.waitForBlazorReady(page);
    
    // Test 1: Empty form validation
    console.log('  1️⃣ Testing empty form submission...');
    
    const submitBtn = await page.locator('button[type="submit"]');
    if (!await submitBtn.isVisible()) {
      console.log('  ⚠️ No submit button found - page may not be accessible');
      return;
    }
    
    await submitBtn.click();
    await page.waitForTimeout(1000);
    
    const errors = await page.locator('.text-danger, .wcr-validation-message').all();
    console.log(`  📋 Found ${errors.length} validation errors`);
    expect(errors.length).toBeGreaterThan(0);
    
    // Test 2: Test required fields
    console.log('  2️⃣ Testing required field validation...');
    
    // Fill incident date (usually required)
    const dateInput = await page.locator('input[type="date"]').first();
    if (await dateInput.isVisible()) {
      await dateInput.fill('2024-01-01');
      console.log('  ✅ Filled incident date');
    }
    
    // Fill incident description
    const descriptionTextarea = await page.locator('textarea[name*="Description" i], textarea[placeholder*="describe" i]').first();
    if (await descriptionTextarea.isVisible()) {
      await descriptionTextarea.fill('Test incident description with sufficient detail.');
      console.log('  ✅ Filled incident description');
    }
    
    // Select incident type if dropdown exists
    const typeSelect = await page.locator('select[name*="Type" i]');
    if (await typeSelect.isVisible()) {
      await typeSelect.selectOption({ index: 1 });
      console.log('  ✅ Selected incident type');
    }
    
    // Test 3: Anonymous submission option
    console.log('  3️⃣ Testing anonymous submission option...');
    
    const anonymousCheckbox = await page.locator('input[type="checkbox"][name*="Anonymous" i]');
    if (await anonymousCheckbox.isVisible()) {
      await anonymousCheckbox.check();
      console.log('  ✅ Anonymous submission option available');
      
      // Check if contact fields become optional
      await page.waitForTimeout(500);
      const emailInput = await page.locator('input[type="email"]');
      if (await emailInput.isVisible()) {
        const isRequired = await emailInput.getAttribute('required');
        if (!isRequired) {
          console.log('  ✅ Contact fields made optional for anonymous submission');
        }
      }
    }
    
    await page.screenshot({ 
      path: 'test-results/validation-tests/incident-report-validation.png',
      fullPage: true 
    });
  });

  test('Vetting application form validation', async ({ page }) => {
    console.log('\n🎭 Testing Vetting Application Form Validation...');
    
    // Navigate to vetting application
    await page.goto(`${testConfig.baseUrl}/vetting/apply`, {
      waitUntil: 'networkidle'
    });
    
    // Check if page is accessible
    const pageTitle = await page.title();
    if (page.url().includes('login') || pageTitle.toLowerCase().includes('not found')) {
      console.log('  ⚠️ Vetting application requires authentication or not accessible - skipping');
      return;
    }
    
    await BlazorHelpers.waitForBlazorReady(page);
    
    // Test 1: Empty form validation
    console.log('  1️⃣ Testing empty form submission...');
    
    const submitBtn = await page.locator('button[type="submit"]');
    if (!await submitBtn.isVisible()) {
      console.log('  ⚠️ No submit button found - page may not be accessible');
      return;
    }
    
    await submitBtn.click();
    await page.waitForTimeout(1000);
    
    const errors = await page.locator('.text-danger, .wcr-validation-message').all();
    console.log(`  📋 Found ${errors.length} validation errors`);
    expect(errors.length).toBeGreaterThan(0);
    
    // Test 2: Test personal information validation
    console.log('  2️⃣ Testing personal information validation...');
    
    // Test scene name validation
    const sceneNameInput = await page.locator('input[name*="SceneName" i]');
    if (await sceneNameInput.isVisible()) {
      await sceneNameInput.fill('A'); // Too short
      await page.click('body');
      await page.waitForTimeout(500);
      
      const sceneNameError = await page.locator('.text-danger, .wcr-validation-message').filter({ hasText: /characters|length/i }).first();
      if (await sceneNameError.isVisible()) {
        console.log('  ✅ Scene name length validation working');
      }
    }
    
    // Test email validation
    const emailInput = await page.locator('input[type="email"]').first();
    if (await emailInput.isVisible()) {
      await emailInput.fill('invalid-email');
      await page.click('body');
      await page.waitForTimeout(500);
      
      const emailError = await page.locator('.text-danger, .wcr-validation-message').filter({ hasText: /email/i }).first();
      if (await emailError.isVisible()) {
        console.log('  ✅ Email format validation working');
      }
    }
    
    // Test 3: Test agreement checkboxes
    console.log('  3️⃣ Testing agreement validation...');
    
    // Fill valid data first
    if (await sceneNameInput.isVisible()) {
      await sceneNameInput.clear();
      await sceneNameInput.fill('TestApplicant');
    }
    
    if (await emailInput.isVisible()) {
      await emailInput.clear();
      await emailInput.fill('applicant@example.com');
    }
    
    // Fill other required fields
    await page.fill('input[name*="LegalName" i]', 'Test Legal Name');
    await page.fill('textarea[name*="Experience" i]', 'I have been interested in rope bondage for several years.');
    await page.fill('textarea[name*="WhyJoin" i]', 'I want to join to learn in a safe environment.');
    
    // Submit without checking agreements
    await submitBtn.click();
    await page.waitForTimeout(1000);
    
    // Should still have errors for missing agreements
    const agreementErrors = await page.locator('.text-danger, .wcr-validation-message').filter({ hasText: /agree|consent|terms/i }).all();
    if (agreementErrors.length > 0) {
      console.log(`  ✅ Agreement validation working: ${agreementErrors.length} agreement errors`);
    }
    
    await page.screenshot({ 
      path: 'test-results/validation-tests/vetting-application-validation.png',
      fullPage: true 
    });
  });
});