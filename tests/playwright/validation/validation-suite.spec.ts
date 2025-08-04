import { test, expect } from '@playwright/test';
import { testConfig } from '../helpers/test.config';
import { BlazorHelpers } from '../helpers/blazor.helpers';
import { AuthHelpers } from '../helpers/auth.helpers';

test.describe('Comprehensive Validation Test Suite', () => {
  test.describe('Identity Pages Validation', () => {
    test('Forgot Password validation', async ({ page }) => {
      console.log('\n🔑 TESTING FORGOT PASSWORD VALIDATION\n');
      
      await page.goto(`${testConfig.baseUrl}/Identity/Account/ForgotPassword`, {
        waitUntil: 'networkidle'
      });
      
      // Test 1: Empty email
      console.log('📍 Empty email submission');
      let submitBtn = await page.locator('.forgot-password-btn, button[type="submit"]').first();
      await submitBtn.click();
      await page.waitForTimeout(1000);
      
      const errors = await page.locator('.wcr-field-validation, .wcr-validation-message').all();
      console.log(`✅ Found ${errors.length} validation errors`);
      expect(errors.length).toBeGreaterThan(0);
      await page.screenshot({ path: 'test-results/validation-tests/forgot-password-empty.png' });
      
      // Test 2: Invalid email
      console.log('\n📍 Invalid email format');
      const emailInput = await page.locator('#Input_Email').first();
      await emailInput.fill('not-an-email');
      await page.keyboard.press('Tab');
      await page.waitForTimeout(500);
      await page.screenshot({ path: 'test-results/validation-tests/forgot-password-invalid.png' });
      
      // Test 3: Valid submission
      console.log('\n📍 Valid email submission');
      await emailInput.clear();
      await emailInput.fill('test@example.com');
      await submitBtn.click();
      await page.waitForTimeout(2000);
      
      const successAlert = await page.locator('.alert-success').first();
      console.log(`✅ Success message ${await successAlert.isVisible() ? 'shown' : 'not shown'}`);
      await page.screenshot({ path: 'test-results/validation-tests/forgot-password-success.png' });
    });

    test('Reset Password validation', async ({ page }) => {
      console.log('\n🔐 TESTING RESET PASSWORD VALIDATION\n');
      
      // Test with a dummy reset code
      await page.goto(`${testConfig.baseUrl}/Identity/Account/ResetPassword?code=dummycode`, {
        waitUntil: 'networkidle'
      });
      
      // Check if page loads with code
      const hasForm = await page.locator('form').first().isVisible();
      if (hasForm) {
        console.log('✅ Reset password form loaded');
        
        // Test 1: Empty form
        console.log('\n📍 Empty form submission');
        let submitBtn = await page.locator('.reset-password-btn, button[type="submit"]').first();
        await submitBtn.click();
        await page.waitForTimeout(1000);
        
        const errors = await page.locator('.wcr-field-validation, .wcr-validation-message').all();
        console.log(`✅ Found ${errors.length} validation errors`);
        await page.screenshot({ path: 'test-results/validation-tests/reset-password-empty.png' });
        
        // Test 2: Password mismatch
        console.log('\n📍 Password mismatch');
        const passwordInputs = await page.locator('input[type="password"]').all();
        if (passwordInputs.length >= 2) {
          await passwordInputs[0].fill('NewPass123!');
          await passwordInputs[1].fill('DifferentPass123!');
          await page.keyboard.press('Tab');
          await page.waitForTimeout(500);
          await page.screenshot({ path: 'test-results/validation-tests/reset-password-mismatch.png' });
        }
      } else {
        console.log('❌ No reset code provided - showing error');
        await page.screenshot({ path: 'test-results/validation-tests/reset-password-no-code.png' });
      }
    });

    test('Change Password validation', async ({ page }) => {
      console.log('\n🔄 TESTING CHANGE PASSWORD VALIDATION\n');
      
      // First login
      await AuthHelpers.login(page);
      await page.waitForTimeout(2000);
      
      // Navigate to change password
      await page.goto(`${testConfig.baseUrl}/change-password`, {
        waitUntil: 'networkidle'
      });
      
      const hasForm = await page.locator('form').first().isVisible();
      if (hasForm) {
        console.log('✅ Change password form loaded');
        
        // Test 1: Empty form
        console.log('\n📍 Empty form submission');
        let submitBtn = await page.locator('.change-password-btn, button[type="submit"]').first();
        await submitBtn.click();
        await page.waitForTimeout(1000);
        
        const errors = await page.locator('.wcr-field-validation, .wcr-validation-message').all();
        console.log(`✅ Found ${errors.length} validation errors`);
        await page.screenshot({ path: 'test-results/validation-tests/change-password-empty.png' });
        
        // Test 2: Wrong current password
        console.log('\n📍 Wrong current password');
        const passwordInputs = await page.locator('input[type="password"]').all();
        if (passwordInputs.length >= 3) {
          await passwordInputs[0].fill('WrongPassword123!');
          await passwordInputs[1].fill('NewPassword123!');
          await passwordInputs[2].fill('NewPassword123!');
          
          await submitBtn.click();
          await page.waitForTimeout(2000);
          
          const errorAlert = await page.locator('.alert-danger').first();
          console.log(`✅ Error message ${await errorAlert.isVisible() ? 'shown' : 'not shown'}`);
          await page.screenshot({ path: 'test-results/validation-tests/change-password-wrong-current.png' });
        }
      } else {
        console.log('❌ Not authenticated - redirected to login');
      }
    });

    test('Manage Email validation', async ({ page }) => {
      console.log('\n📧 TESTING MANAGE EMAIL VALIDATION\n');
      
      await AuthHelpers.login(page);
      
      // Navigate to manage email
      await page.goto(`${testConfig.baseUrl}/manage-email`, {
        waitUntil: 'networkidle'
      });
      
      const hasForm = await page.locator('form').first().isVisible();
      if (hasForm) {
        console.log('✅ Manage email form loaded');
        
        // Check current email display
        const currentEmailSection = await page.locator('.current-email-section').first();
        if (await currentEmailSection.isVisible()) {
          console.log('✅ Current email section displayed');
          await page.screenshot({ path: 'test-results/validation-tests/manage-email-current.png' });
        }
        
        // Test 1: Empty new email
        console.log('\n📍 Empty new email submission');
        let submitBtn = await page.locator('.change-email-btn, button[type="submit"]').first();
        await submitBtn.click();
        await page.waitForTimeout(1000);
        
        const errors = await page.locator('.wcr-field-validation, .wcr-validation-message').all();
        console.log(`✅ Found ${errors.length} validation errors`);
        await page.screenshot({ path: 'test-results/validation-tests/manage-email-empty.png' });
        
        // Test 2: Invalid email format
        console.log('\n📍 Invalid email format');
        const emailInput = await page.locator('#Input_Email').first();
        await emailInput.fill('not-valid-email');
        await page.keyboard.press('Tab');
        await page.waitForTimeout(500);
        await page.screenshot({ path: 'test-results/validation-tests/manage-email-invalid.png' });
        
        // Test 3: Duplicate email
        console.log('\n📍 Duplicate email check');
        await emailInput.clear();
        await emailInput.fill('admin@witchcityrope.com');
        await submitBtn.click();
        await page.waitForTimeout(2000);
        
        const errorAlert = await page.locator('.alert-danger').first();
        console.log(`✅ Duplicate email error ${await errorAlert.isVisible() ? 'shown' : 'not shown'}`);
        await page.screenshot({ path: 'test-results/validation-tests/manage-email-duplicate.png' });
      }
    });

    test('Manage Profile validation', async ({ page }) => {
      console.log('\n👤 TESTING MANAGE PROFILE VALIDATION\n');
      
      await AuthHelpers.login(page);
      
      // Navigate to manage profile
      await page.goto(`${testConfig.baseUrl}/manage-profile`, {
        waitUntil: 'networkidle'
      });
      
      const hasForm = await page.locator('form').first().isVisible();
      if (hasForm) {
        console.log('✅ Manage profile form loaded');
        
        // Check profile sections
        const profileSections = await page.locator('.profile-section').all();
        console.log(`✅ Found ${profileSections.length} profile sections`);
        await page.screenshot({ path: 'test-results/validation-tests/manage-profile-loaded.png' });
        
        // Test 1: Invalid phone number
        console.log('\n📍 Invalid phone number format');
        const phoneInput = await page.locator('input[placeholder*="(123)"]').first();
        if (await phoneInput.isVisible()) {
          await phoneInput.fill('invalid-phone');
          await page.keyboard.press('Tab');
          await page.waitForTimeout(500);
          await page.screenshot({ path: 'test-results/validation-tests/manage-profile-invalid-phone.png' });
        }
        
        // Test 2: Long pronouns/name
        console.log('\n📍 Testing field length limits');
        const pronounsInput = await page.locator('input[placeholder*="they/them"]').first();
        if (await pronounsInput.isVisible()) {
          const longText = 'a'.repeat(100);
          await pronounsInput.fill(longText);
          await page.keyboard.press('Tab');
          await page.waitForTimeout(500);
        }
        
        // Test 3: Valid submission
        console.log('\n📍 Valid profile update');
        if (await phoneInput.isVisible()) {
          await phoneInput.clear();
          await phoneInput.fill('(555) 123-4567');
        }
        
        const pronouncedNameInput = await page.locator('input[placeholder*="pronounced"]').first();
        if (await pronouncedNameInput.isVisible()) {
          await pronouncedNameInput.fill('Test Pronunciation');
        }
        
        if (await pronounsInput.isVisible()) {
          await pronounsInput.clear();
          await pronounsInput.fill('they/them');
        }
        
        let submitBtn = await page.locator('.update-profile-btn, button[type="submit"]').first();
        await submitBtn.click();
        await page.waitForTimeout(2000);
        
        const successAlert = await page.locator('.alert-success').first();
        console.log(`✅ Success message ${await successAlert.isVisible() ? 'shown' : 'not shown'}`);
        await page.screenshot({ path: 'test-results/validation-tests/manage-profile-success.png' });
        
        // Check account status display
        const statusGrid = await page.locator('.status-grid').first();
        if (await statusGrid.isVisible()) {
          console.log('✅ Account status information displayed');
        }
      }
    });

    test('Delete Personal Data validation', async ({ page }) => {
      console.log('\n🗑️ TESTING DELETE PERSONAL DATA VALIDATION\n');
      
      await AuthHelpers.login(page);
      
      // Navigate to delete personal data page
      await page.goto(`${testConfig.baseUrl}/delete-personal-data`, {
        waitUntil: 'networkidle'
      });
      
      const hasForm = await page.locator('form').first().isVisible();
      if (hasForm) {
        console.log('✅ Delete personal data form loaded');
        
        // Check warning banner
        const warningBanner = await page.locator('.warning-banner').first();
        if (await warningBanner.isVisible()) {
          console.log('✅ Warning banner displayed');
          await page.screenshot({ path: 'test-results/validation-tests/delete-data-warning.png' });
        }
        
        // Check deletion info
        const deletionList = await page.locator('.deletion-list li').all();
        console.log(`✅ Found ${deletionList.length} items in deletion list`);
        
        // Test 1: Empty form submission
        console.log('\n📍 Empty form submission');
        let submitBtn = await page.locator('.delete-btn, button[type="submit"]').first();
        await submitBtn.click();
        await page.waitForTimeout(1000);
        
        const errors = await page.locator('.wcr-field-validation, .wcr-validation-message').all();
        console.log(`✅ Found ${errors.length} validation errors`);
        await page.screenshot({ path: 'test-results/validation-tests/delete-data-empty.png' });
        
        // Test 2: Wrong password
        console.log('\n📍 Testing password validation');
        const passwordInput = await page.locator('#Input_Password').first();
        if (await passwordInput.isVisible()) {
          await passwordInput.fill('WrongPassword123!');
          
          // Check the confirmation checkbox
          const checkbox = await page.locator('input[type="checkbox"]').first();
          if (await checkbox.isVisible()) {
            await checkbox.check();
          }
          
          await submitBtn.click();
          await page.waitForTimeout(2000);
          
          const errorAlert = await page.locator('.alert-danger').first();
          console.log(`✅ Password error ${await errorAlert.isVisible() ? 'shown' : 'not shown'}`);
          await page.screenshot({ path: 'test-results/validation-tests/delete-data-wrong-password.png' });
        }
        
        // Test 3: Cancel button
        console.log('\n📍 Testing cancel button');
        const cancelBtn = await page.locator('.cancel-btn').first();
        if (await cancelBtn.isVisible()) {
          console.log('✅ Cancel button found - not clicking to stay on page');
        }
      }
    });

    test('Login with 2FA validation', async ({ page }) => {
      console.log('\n🔐 TESTING LOGIN WITH 2FA VALIDATION\n');
      
      // Navigate directly to 2FA page
      await page.goto(`${testConfig.baseUrl}/login-2fa`, {
        waitUntil: 'networkidle'
      });
      
      const currentUrl = page.url();
      if (currentUrl.includes('/login-2fa')) {
        console.log('✅ 2FA page loaded (test mode)');
        
        // Test 1: Empty code
        console.log('\n📍 Empty code submission');
        let submitBtn = await page.locator('.twofa-btn, button[type="submit"]').first();
        await submitBtn.click();
        await page.waitForTimeout(1000);
        
        const errors = await page.locator('.wcr-field-validation, .wcr-validation-message').all();
        console.log(`✅ Found ${errors.length} validation errors`);
        await page.screenshot({ path: 'test-results/validation-tests/2fa-empty.png' });
        
        // Test 2: Invalid code format
        console.log('\n📍 Invalid code format');
        const codeInput = await page.locator('input[inputmode="numeric"]').first();
        if (await codeInput.isVisible()) {
          await codeInput.fill('123'); // Too short
          await page.keyboard.press('Tab');
          await page.waitForTimeout(500);
          await page.screenshot({ path: 'test-results/validation-tests/2fa-invalid-length.png' });
        }
        
        // Test 3: Recovery code section
        console.log('\n📍 Testing recovery code section');
        const recoveryDetails = await page.locator('.recovery-details').first();
        if (await recoveryDetails.isVisible()) {
          await recoveryDetails.click();
          await page.waitForTimeout(500);
          
          const recoveryInput = await page.locator('.recovery-content input[type="text"]').first();
          if (await recoveryInput.isVisible()) {
            console.log('✅ Recovery code section expanded');
            await page.screenshot({ path: 'test-results/validation-tests/2fa-recovery-expanded.png' });
          }
        }
      } else {
        console.log('❌ Redirected to login - 2FA not active or no active session');
      }
    });
  });
});